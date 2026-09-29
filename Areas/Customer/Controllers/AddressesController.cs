using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class AddressesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // LIST ADDRESSES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var addresses = await _context.CustomerAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(addresses);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CustomerAddress address)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // UserId comes from the logged-in user,
            // not from the form.
            ModelState.Remove(nameof(CustomerAddress.UserId));

            if (!ModelState.IsValid)
            {
                return View(address);
            }

            // Set ownership from the authenticated user.
            address.UserId = userId;
            address.CreatedAt = DateTime.UtcNow;

            // Check whether the customer already has an address.
            var hasAddress = await _context.CustomerAddresses
                .AnyAsync(a => a.UserId == userId);

            // The first address automatically becomes default.
            if (!hasAddress)
            {
                address.IsDefault = true;
            }

            // If this address is default,
            // make all other addresses non-default.
            if (address.IsDefault)
            {
                await ClearDefaultAddressAsync(userId);
            }

            _context.CustomerAddresses.Add(address);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Address added successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CustomerAddress address)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (id != address.Id)
            {
                return NotFound();
            }

            // UserId is controlled by the server,
            // so remove its validation error.
            ModelState.Remove(nameof(CustomerAddress.UserId));

            if (!ModelState.IsValid)
            {
                return View(address);
            }

            // Only allow editing an address belonging
            // to the currently logged-in customer.
            var existingAddress =
                await _context.CustomerAddresses
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.UserId == userId);

            if (existingAddress == null)
            {
                return NotFound();
            }

            // Update editable fields.
            existingAddress.FullName =
                address.FullName;

            existingAddress.PhoneNumber =
                address.PhoneNumber;

            existingAddress.AddressLine =
                address.AddressLine;

            existingAddress.City =
                address.City;

            existingAddress.State =
                address.State;

            existingAddress.PostalCode =
                address.PostalCode;

            existingAddress.Country =
                address.Country;

            // If the customer selects this address
            // as default, clear other defaults first.
            if (address.IsDefault)
            {
                await ClearDefaultAddressAsync(
                    userId,
                    id);
            }

            existingAddress.IsDefault =
                address.IsDefault;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Address updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Only delete the address belonging
            // to the current customer.
            var address =
                await _context.CustomerAddresses
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            var wasDefault =
                address.IsDefault;

            _context.CustomerAddresses.Remove(address);

            await _context.SaveChangesAsync();

            // If the deleted address was default,
            // make the newest remaining address default.
            if (wasDefault)
            {
                var newDefault =
                    await _context.CustomerAddresses
                        .Where(a =>
                            a.UserId == userId)
                        .OrderByDescending(
                            a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;

                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] =
                "Address removed successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // SET DEFAULT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Only allow setting an address belonging
            // to the current customer as default.
            var address =
                await _context.CustomerAddresses
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            // Remove default from all other addresses.
            await ClearDefaultAddressAsync(
                userId,
                id);

            address.IsDefault = true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Default address updated.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CLEAR DEFAULT ADDRESS
        // =========================================================

        private async Task ClearDefaultAddressAsync(
            string userId,
            int? exceptAddressId = null)
        {
            var query =
                _context.CustomerAddresses
                    .Where(a =>
                        a.UserId == userId &&
                        a.IsDefault);

            // When editing/setting a specific address,
            // keep that address untouched.
            if (exceptAddressId.HasValue)
            {
                query = query.Where(a =>
                    a.Id != exceptAddressId.Value);
            }

            var defaultAddresses =
                await query.ToListAsync();

            foreach (var item in defaultAddresses)
            {
                item.IsDefault = false;
            }
        }
    }
}