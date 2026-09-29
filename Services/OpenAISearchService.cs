#pragma warning disable OPENAI001

using OpenAI.Responses;
using System.Text.Json;

namespace ShopSphere.Services
{
    public class OpenAISearchService : IAISearchService
    {
        private readonly ResponsesClient _client;
        private readonly string _model;

        public OpenAISearchService(
            string apiKey,
            string model)
        {
            _client = new ResponsesClient(apiKey);

            _model = string.IsNullOrWhiteSpace(model)
                ? "gpt-5.2"
                : model;
        }

        public async Task<AIProductSearchQuery>
            UnderstandSearchAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new AIProductSearchQuery();
            }

            var prompt =
                "You are an AI search assistant for an " +
                "e-commerce marketplace.\n\n" +

                "Understand the customer's search query and " +
                "extract useful product search filters.\n\n" +

                "Return ONLY valid JSON.\n\n" +

                "JSON format:\n" +
                "{\n" +
                "  \"keyword\": \"product keyword or null\",\n" +
                "  \"category\": \"category name or null\",\n" +
                "  \"minPrice\": number or null,\n" +
                "  \"maxPrice\": number or null\n" +
                "}\n\n" +

                "Rules:\n" +
                "- Do not invent products or categories.\n" +
                "- If a value is unknown, return null.\n" +
                "- Prices must be numeric.\n" +
                "- minPrice must not be greater than maxPrice.\n" +
                "- Return JSON only, without explanations.\n\n" +

                "Customer query:\n" +
                searchText.Trim();

            try
            {
                var response = await _client.CreateResponseAsync(
                    _model,
                    prompt);

                var output =
                    response.Value.GetOutputText();

                if (string.IsNullOrWhiteSpace(output))
                {
                    return FallbackSearch(searchText);
                }

                output = CleanJsonOutput(output);

                try
                {
                    var result =
                        JsonSerializer.Deserialize<
                            AIProductSearchQuery>(
                                output,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                    if (result == null)
                    {
                        return FallbackSearch(searchText);
                    }

                    if (result.MinPrice.HasValue &&
                        result.MaxPrice.HasValue &&
                        result.MinPrice.Value >
                        result.MaxPrice.Value)
                    {
                        (result.MinPrice, result.MaxPrice) =
                            (result.MaxPrice, result.MinPrice);
                    }

                    return result;
                }
                catch
                {
                    return FallbackSearch(searchText);
                }
            }
            catch
            {
                return FallbackSearch(searchText);
            }
        }

        private static string CleanJsonOutput(
            string output)
        {
            output = output.Trim();

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

            return output.Trim();
        }

        private static AIProductSearchQuery
            FallbackSearch(string searchText)
        {
            return new AIProductSearchQuery
            {
                Keyword = searchText.Trim()
            };
        }
    }
}

#pragma warning restore OPENAI001