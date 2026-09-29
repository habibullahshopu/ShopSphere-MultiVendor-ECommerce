using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InventoryPredictionController : Controller
    {
        private readonly InventoryDataService _inventoryDataService;
        private readonly IAIInventoryPredictionService
            _aiInventoryPredictionService;

        public InventoryPredictionController(
            InventoryDataService inventoryDataService,
            IAIInventoryPredictionService aiInventoryPredictionService)
        {
            _inventoryDataService = inventoryDataService;
            _aiInventoryPredictionService =
                aiInventoryPredictionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var inventoryData =
                await _inventoryDataService
                    .GetInventoryDataAsync(30);

            ViewBag.InventoryData = inventoryData;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            var inventoryData =
                await _inventoryDataService
                    .GetInventoryDataAsync(30);

            ViewBag.InventoryData = inventoryData;

            if (inventoryData.Count == 0)
            {
                ViewBag.Message =
                    "No active products are available " +
                    "for inventory prediction.";

                return View("Index");
            }

            var input = new InventoryPredictionInput
            {
                Products = inventoryData
            };

            List<InventoryPredictionResult> predictions;

            try
            {
                predictions =
                    await _aiInventoryPredictionService
                        .PredictInventoryAsync(input);
            }
            catch (Exception)
            {
                predictions = inventoryData
                    .Select(product =>
                        new InventoryPredictionResult
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            CurrentStock = product.CurrentStock,
                            PredictedDailySales = 0,
                            EstimatedDaysUntilStockout = 0,
                            RiskLevel = "Unavailable",
                            RecommendedRestockQuantity = 0,
                            Explanation =
                                "AI inventory prediction is " +
                                "temporarily unavailable."
                        })
                    .ToList();
            }

            return View("Index", predictions);
        }
    }
}