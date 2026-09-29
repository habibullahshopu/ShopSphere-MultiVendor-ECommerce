#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.Text.Json;

namespace ShopSphere.Services
{
    public class OpenAIInventoryPredictionService
        : IAIInventoryPredictionService
    {
        private readonly ResponsesClient _client;
        private readonly string _model;

        public OpenAIInventoryPredictionService(
            string apiKey,
            string model)
        {
            _client = new ResponsesClient(apiKey);

            _model = string.IsNullOrWhiteSpace(model)
                ? "gpt-5.2"
                : model;
        }

        public async Task<List<InventoryPredictionResult>>
            PredictInventoryAsync(
                InventoryPredictionInput input)
        {
            if (input == null ||
                input.Products == null ||
                input.Products.Count == 0)
            {
                return new List<InventoryPredictionResult>();
            }

            var productsJson =
                JsonSerializer.Serialize(
                    input.Products,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            var prompt =
                "You are an AI inventory forecasting assistant " +
                "for an e-commerce marketplace.\n\n" +

                "Analyze the provided product inventory and recent " +
                "sales data.\n\n" +

                "For every product, predict inventory risk and " +
                "recommended restocking quantity.\n\n" +

                "Return ONLY a valid JSON array.\n\n" +

                "Each item must use exactly this structure:\n" +
                "[\n" +
                "  {\n" +
                "    \"productId\": number,\n" +
                "    \"productName\": \"string\",\n" +
                "    \"currentStock\": number,\n" +
                "    \"predictedDailySales\": number,\n" +
                "    \"estimatedDaysUntilStockout\": number,\n" +
                "    \"riskLevel\": \"High, Medium, or Low\",\n" +
                "    \"recommendedRestockQuantity\": number,\n" +
                "    \"explanation\": \"short explanation\"\n" +
                "  }\n" +
                "]\n\n" +

                "Rules:\n" +
                "- Return one result for every input product.\n" +
                "- Never invent product IDs.\n" +
                "- Never invent product names.\n" +
                "- Predicted daily sales must never be negative.\n" +
                "- Stockout days must never be negative.\n" +
                "- Recommended restock quantity must never be negative.\n" +
                "- Risk level must be exactly High, Medium, or Low.\n" +
                "- Use recent sales data to estimate demand.\n" +
                "- If there are no recent sales, use Low risk unless " +
                "the current stock is extremely low.\n" +
                "- Return JSON only. Do not include explanations outside JSON.\n\n" +

                "Inventory and sales data:\n" +
                productsJson;

            try
            {
                var response =
                    await _client.CreateResponseAsync(
                        _model,
                        prompt);

                var output =
                    response.Value.GetOutputText();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return CreateUnavailableResults(
                        input.Products);
                }

                output = CleanJsonOutput(output);

                try
                {
                    var results =
                        JsonSerializer.Deserialize<
                            List<InventoryPredictionResult>>(
                                output,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                    if (results == null ||
                        results.Count == 0)
                    {
                        return CreateUnavailableResults(
                            input.Products);
                    }

                    foreach (var result in results)
                    {
                        if (result.PredictedDailySales < 0)
                        {
                            result.PredictedDailySales = 0;
                        }

                        if (result.EstimatedDaysUntilStockout < 0)
                        {
                            result.EstimatedDaysUntilStockout = 0;
                        }

                        if (result.RecommendedRestockQuantity < 0)
                        {
                            result.RecommendedRestockQuantity = 0;
                        }

                        result.RiskLevel =
                            NormalizeRiskLevel(
                                result.RiskLevel);

                        if (string.IsNullOrWhiteSpace(
                                result.Explanation))
                        {
                            result.Explanation =
                                "AI generated this inventory forecast " +
                                "using current stock and recent sales data.";
                        }
                    }

                    return results;
                }
                catch
                {
                    return CreateUnavailableResults(
                        input.Products);
                }
            }
            catch
            {
                return CreateUnavailableResults(
                    input.Products);
            }
        }

        private static string CleanJsonOutput(
            string output)
        {
            output = output.Trim();

            // Remove markdown code fences if AI returns:
            // ```json
            // [...]
            // ```
            if (output.StartsWith("```"))
            {
                var firstNewLine =
                    output.IndexOf('\n');

                if (firstNewLine >= 0)
                {
                    output =
                        output[(firstNewLine + 1)..];
                }

                if (output.EndsWith("```"))
                {
                    output =
                        output[..^3];
                }
            }

            output = output.Trim();

            // If extra text appears before the JSON array,
            // keep only the JSON array.
            var startIndex =
                output.IndexOf('[');

            var endIndex =
                output.LastIndexOf(']');

            if (startIndex >= 0 &&
                endIndex > startIndex)
            {
                output =
                    output.Substring(
                        startIndex,
                        endIndex - startIndex + 1);
            }

            return output.Trim();
        }

        private static string NormalizeRiskLevel(
            string? riskLevel)
        {
            if (string.IsNullOrWhiteSpace(riskLevel))
            {
                return "Low";
            }

            var risk =
                riskLevel.Trim();

            if (string.Equals(
                    risk,
                    "High",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "High";
            }

            if (string.Equals(
                    risk,
                    "Medium",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Medium";
            }

            if (string.Equals(
                    risk,
                    "Low",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Low";
            }

            return "Low";
        }

        private static List<InventoryPredictionResult>
            CreateUnavailableResults(
                List<InventoryProductData> products)
        {
            return products
                .Select(product =>
                    new InventoryPredictionResult
                    {
                        ProductId =
                            product.ProductId,

                        ProductName =
                            product.ProductName,

                        CurrentStock =
                            product.CurrentStock,

                        PredictedDailySales = 0,

                        EstimatedDaysUntilStockout = 0,

                        RiskLevel = "Unavailable",

                        RecommendedRestockQuantity = 0,

                        Explanation =
                            "AI inventory prediction is currently " +
                            "unavailable."
                    })
                .ToList();
        }
    }
}

#pragma warning restore OPENAI001