using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Services
{
    public class SalesDataService
    {
        private readonly ApplicationDbContext _context;

        public SalesDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SalesHistoryItem>> GetSalesHistoryAsync(
            int days = 30)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var salesHistory = await _context.Orders
                .Where(o =>
                    o.CreatedAt >= startDate &&
                    o.PaymentStatus == "Paid")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SalesHistoryItem
                {
                    Date = g.Key,

                    SalesAmount = g.Sum(o => o.TotalAmount),

                    OrderCount = g.Count(),

                    ItemsSold = _context.OrderItems
                        .Where(oi =>
                            g.Select(o => o.Id)
                             .Contains(oi.OrderId))
                        .Sum(oi => oi.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return salesHistory;
        }
    }
}