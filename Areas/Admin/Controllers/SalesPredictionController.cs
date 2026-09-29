using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SalesPredictionController : Controller
    {
        private readonly SalesDataService _salesDataService;
        private readonly IAISalesPredictionService _aiSalesPredictionService;

        public SalesPredictionController(
            SalesDataService salesDataService,
            IAISalesPredictionService aiSalesPredictionService)
        {
            _salesDataService = salesDataService;
            _aiSalesPredictionService = aiSalesPredictionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var salesHistory =
                await _salesDataService.GetSalesHistoryAsync(30);

            ViewBag.SalesHistory = salesHistory;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            var salesHistory =
                await _salesDataService.GetSalesHistoryAsync(30);

            ViewBag.SalesHistory = salesHistory;

            if (salesHistory.Count == 0)
            {
                ViewBag.Message =
                    "Not enough paid sales data available for prediction.";

                return View("Index");
            }

            var input = new SalesPredictionInput
            {
                SalesHistory = salesHistory
            };

            SalesPredictionResult prediction;

            try
            {
                prediction =
                    await _aiSalesPredictionService
                        .PredictSalesAsync(input);
            }
            catch (Exception)
            {
                prediction = new SalesPredictionResult
                {
                    Trend = "Unavailable",
                    Explanation =
                        "Sales prediction is temporarily unavailable. " +
                        "Please try again later."
                };
            }

            return View("Index", prediction);
        }
    }
}