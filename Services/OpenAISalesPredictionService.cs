#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.Text.Json;

namespace ShopSphere.Services
{
    public class OpenAISalesPredictionService
        : IAISalesPredictionService
    {
        private readonly ResponsesClient _client;
        private readonly string _model;

        public OpenAISalesPredictionService(
            string apiKey,
            string model)
        {
            _client = new ResponsesClient(apiKey);

            _model = string.IsNullOrWhiteSpace(model)
                ? "gpt-5.2"
                : model;
        }

        public async Task<SalesPredictionResult> PredictSalesAsync(
            SalesPredictionInput input)
        {
            if (input == null ||
                input.SalesHistory == null ||
                input.SalesHistory.Count == 0)
            {
                return new SalesPredictionResult
                {
                    Trend = "Insufficient Data",
                    Explanation =
                        "Not enough sales history for prediction."
                };
            }

            var historyJson = JsonSerializer.Serialize(
                input.SalesHistory,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var prompt =
                "You are an AI sales forecasting assistant for an " +
                "e-commerce marketplace.\n\n" +

                "Analyze the provided historical sales data and " +
                "predict the next sales period.\n\n" +

                "Return ONLY valid JSON using exactly this format:\n" +

                "{\n" +
                "  \"predictedSales\": number,\n" +
                "  \"predictedOrders\": number,\n" +
                "  \"predictedItemsSold\": number,\n" +
                "  \"trend\": \"Increasing, Decreasing, or Stable\",\n" +
                "  \"explanation\": \"short explanation\"\n" +
                "}\n\n" +

                "Predicted values must never be negative.\n\n" +

                "Historical sales data:\n" +
                historyJson;

            try
            {
                var response = await _client.CreateResponseAsync(
                    _model,
                    prompt);

                var output = response.Value.GetOutputText();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return new SalesPredictionResult
                    {
                        Trend = "Unknown",
                        Explanation =
                            "AI returned no prediction."
                    };
                }

                try
                {
                    var result =
                        JsonSerializer.Deserialize<SalesPredictionResult>(
                            output,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (result == null)
                    {
                        return new SalesPredictionResult
                        {
                            Trend = "Unknown",
                            Explanation =
                                "Could not parse AI prediction."
                        };
                    }

                    // ==========================================
                    // PREDICTION VALIDATION
                    // ==========================================

                    if (result.PredictedSales < 0)
                    {
                        result.PredictedSales = 0;
                    }

                    if (result.PredictedOrders < 0)
                    {
                        result.PredictedOrders = 0;
                    }

                    if (result.PredictedItemsSold < 0)
                    {
                        result.PredictedItemsSold = 0;
                    }

                    // ==========================================
                    // TREND VALIDATION
                    // ==========================================

                    if (string.IsNullOrWhiteSpace(result.Trend))
                    {
                        result.Trend = "Unknown";
                    }
                    else
                    {
                        var trend =
                            result.Trend.Trim();

                        if (!string.Equals(
                                trend,
                                "Increasing",
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(
                                trend,
                                "Decreasing",
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(
                                trend,
                                "Stable",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            result.Trend = "Unknown";
                        }
                        else
                        {
                            result.Trend = trend;
                        }
                    }

                    // ==========================================
                    // EXPLANATION FALLBACK
                    // ==========================================

                    if (string.IsNullOrWhiteSpace(
                            result.Explanation))
                    {
                        result.Explanation =
                            "AI generated a sales prediction based on " +
                            "the available historical sales data.";
                    }

                    return result;
                }
                catch
                {
                    return new SalesPredictionResult
                    {
                        Trend = "Unknown",
                        Explanation =
                            "AI prediction could not be processed."
                    };
                }
            }
            catch (Exception)
            {
                return new SalesPredictionResult
                {
                    Trend = "Unavailable",
                    Explanation =
                        "AI sales prediction is currently unavailable. " +
                        "Please check the OpenAI API configuration and try again."
                };
            }
        }
    }
}

#pragma warning restore OPENAI001