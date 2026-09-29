using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Services
{
    public class InventoryDataService
    {
        private readonly ApplicationDbContext _context;

        public InventoryDataService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryProductData>>
            GetInventoryDataAsync(int days = 30)
        {
            var startDate =
                DateTime.UtcNow.Date.AddDays(-days);

            // Get active products
            var products = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync();

            if (products.Count == 0)
            {
                return new List<InventoryProductData>();
            }

            // First get IDs of paid orders from the
            // selected analysis period.
            var paidOrderIds = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.CreatedAt >= startDate &&
                    o.PaymentStatus == "Paid")
                .Select(o => o.Id)
                .ToListAsync();

            // Calculate product-wise quantity sold
            // from those paid orders.
            var soldData = new Dictionary<int, int>();

            if (paidOrderIds.Count > 0)
            {
                soldData = await _context.OrderItems
                    .AsNoTracking()
                    .Where(oi =>
                        paidOrderIds.Contains(oi.OrderId))
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ItemsSold = g.Sum(oi => oi.Quantity)
                    })
                    .ToDictionaryAsync(
                        x => x.ProductId,
                        x => x.ItemsSold);
            }

            var result = products
                .Select(product =>
                {
                    var itemsSold =
                        soldData.TryGetValue(
                            product.Id,
                            out var sold)
                                ? sold
                                : 0;

                    return new InventoryProductData
                    {
                        ProductId = product.Id,

                        ProductName = product.Name,

                        CategoryName =
                            product.Category?.Name,

                        CurrentStock =
                            product.StockQuantity,

                        ItemsSold =
                            itemsSold,

                        DaysAnalyzed =
                            days
                    };
                })
                .OrderBy(x => x.CurrentStock)
                .ThenByDescending(x => x.ItemsSold)
                .ToList();

            return result;
        }
    }
}