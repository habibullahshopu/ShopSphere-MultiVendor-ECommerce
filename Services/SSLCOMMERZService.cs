using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopSphere.Models;

namespace ShopSphere.Services
{
    public class SSLCOMMERZService : ISSLCOMMERZService
    {
        private readonly HttpClient _httpClient;
        private readonly SSLCOMMERZSettings _settings;

        public SSLCOMMERZService(
            HttpClient httpClient,
            IOptions<SSLCOMMERZSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }


        // =========================================================
        // CREATE SSLCOMMERZ PAYMENT SESSION
        // =========================================================

        public async Task<SSLCOMMERZSessionResult>
            CreatePaymentSessionAsync(
                Order order,
                Payment payment)
        {
            try
            {
                if (order == null)
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message = "Order information is missing."
                    };
                }

                if (payment == null)
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message = "Payment information is missing."
                    };
                }

                if (string.IsNullOrWhiteSpace(_settings.StoreId))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "SSLCOMMERZ Store ID is not configured."
                    };
                }

                if (string.IsNullOrWhiteSpace(
                        _settings.StorePassword))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "SSLCOMMERZ Store Password is not configured."
                    };
                }

                if (string.IsNullOrWhiteSpace(payment.TranId))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "Payment transaction ID is missing."
                    };
                }

                // SSLCOMMERZ transaction ID maximum length = 30
                if (payment.TranId.Length > 30)
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "Transaction ID exceeds the SSLCOMMERZ 30 character limit."
                    };
                }


                // =====================================================
                // CALLBACK URLS
                // =====================================================

                // Browser callback URLs
                // These can remain on localhost during development.
                var successUrl =
                    BuildReturnUrl(
                        "/Customer/Payment/Success");

                var failUrl =
                    BuildReturnUrl(
                        "/Customer/Payment/Fail");

                var cancelUrl =
                    BuildReturnUrl(
                        "/Customer/Payment/Cancel");


                // =====================================================
                // PUBLIC IPN URL
                // =====================================================

                // IMPORTANT:
                // SSLCOMMERZ server-to-server IPN cannot reach
                // https://localhost:7272.
                //
                // Therefore IPN uses the separate public IPNUrl
                // configured in appsettings.json.
                var ipnUrl =
                    _settings.IPNUrl?.Trim();


                if (string.IsNullOrWhiteSpace(ipnUrl))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "SSLCOMMERZ IPN URL is not configured."
                    };
                }


                // =====================================================
                // PAYMENT REQUEST DATA
                // =====================================================

                var formData =
                    new Dictionary<string, string>
                    {
                        // Store
                        ["store_id"] =
                            _settings.StoreId,

                        ["store_passwd"] =
                            _settings.StorePassword,

                        // Payment
                        ["total_amount"] =
                            order.TotalAmount.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture),

                        ["currency"] =
                            "BDT",

                        ["tran_id"] =
                            payment.TranId,

                        // Callback URLs
                        ["success_url"] =
                            successUrl,

                        ["fail_url"] =
                            failUrl,

                        ["cancel_url"] =
                            cancelUrl,

                        // IMPORTANT:
                        // This now goes to the public ngrok URL.
                        ["ipn_url"] =
                            ipnUrl,

                        // Customer
                        ["cus_name"] =
                            Limit(
                                order.CustomerName,
                                50),

                        ["cus_email"] =
                            Limit(
                                order.CustomerEmail,
                                50),

                        ["cus_add1"] =
                            Limit(
                                order.ShippingAddress,
                                50),

                        ["cus_city"] =
                            Limit(
                                order.City,
                                50),

                        ["cus_postcode"] =
                            Limit(
                                order.PostalCode,
                                30),

                        ["cus_country"] =
                            "Bangladesh",

                        ["cus_phone"] =
                            Limit(
                                order.PhoneNumber,
                                20),

                        // Shipping
                        ["shipping_method"] =
                            "YES",

                        ["ship_name"] =
                            Limit(
                                order.CustomerName,
                                50),

                        ["ship_add1"] =
                            Limit(
                                order.ShippingAddress,
                                50),

                        ["ship_city"] =
                            Limit(
                                order.City,
                                50),

                        ["ship_postcode"] =
                            Limit(
                                order.PostalCode,
                                30),

                        ["ship_country"] =
                            "Bangladesh",

                        // Product
                        ["product_name"] =
                            "ShopSphere Order",

                        ["product_category"] =
                            "E-commerce",

                        ["product_profile"] =
                            "general",

                        // Additional information
                        ["product_amount"] =
                            order.Subtotal.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture),

                        ["value_a"] =
                            order.Id.ToString(),

                        ["value_b"] =
                            payment.TranId
                    };


                // =====================================================
                // SEND REQUEST
                // =====================================================

                using var content =
                    new FormUrlEncodedContent(
                        formData);

                using var response =
                    await _httpClient.PostAsync(
                        _settings.SessionUrl,
                        content);


                var responseBody =
                    await response.Content
                        .ReadAsStringAsync();


                // =====================================================
                // HTTP ERROR
                // =====================================================

                if (!response.IsSuccessStatusCode)
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            $"SSLCOMMERZ returned HTTP {(int)response.StatusCode}."
                    };
                }


                if (string.IsNullOrWhiteSpace(
                        responseBody))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "SSLCOMMERZ returned an empty response."
                    };
                }


                // =====================================================
                // PARSE JSON RESPONSE
                // =====================================================

                Dictionary<string, JsonElement>? json;

                try
                {
                    json =
                        JsonSerializer.Deserialize<
                            Dictionary<string, JsonElement>>(
                            responseBody,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "SSLCOMMERZ returned an invalid JSON response."
                    };
                }


                if (json == null)
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = false,
                        Message =
                            "Invalid SSLCOMMERZ gateway response."
                    };
                }


                // =====================================================
                // READ RESPONSE VALUES
                // =====================================================

                var status =
                    GetString(
                        json,
                        "status");

                var gatewayUrl =
                    GetString(
                        json,
                        "GatewayPageURL");

                var sessionKey =
                    GetString(
                        json,
                        "sessionkey");

                var gatewayTranId =
                    GetString(
                        json,
                        "tran_id");

                var failedReason =
                    GetString(
                        json,
                        "failedreason");


                // =====================================================
                // SESSION CREATED SUCCESSFULLY
                // =====================================================

                if (string.Equals(
                        status,
                        "SUCCESS",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !string.IsNullOrWhiteSpace(
                        gatewayUrl))
                {
                    return new SSLCOMMERZSessionResult
                    {
                        Success = true,

                        GatewayPageUrl =
                            gatewayUrl,

                        SessionKey =
                            sessionKey,

                        TransactionId =
                            string.IsNullOrWhiteSpace(
                                gatewayTranId)
                                ? payment.TranId
                                : gatewayTranId,

                        Message =
                            "SSLCOMMERZ payment session created successfully."
                    };
                }


                // =====================================================
                // SESSION CREATION FAILED
                // =====================================================

                return new SSLCOMMERZSessionResult
                {
                    Success = false,

                    SessionKey =
                        sessionKey,

                    TransactionId =
                        gatewayTranId ??
                        payment.TranId,

                    Message =
                        string.IsNullOrWhiteSpace(
                            failedReason)
                            ? "Unable to create SSLCOMMERZ payment session."
                            : failedReason
                };
            }
            catch (TaskCanceledException)
            {
                return new SSLCOMMERZSessionResult
                {
                    Success = false,
                    Message =
                        "Connection to SSLCOMMERZ timed out. Please try again."
                };
            }
            catch (HttpRequestException)
            {
                return new SSLCOMMERZSessionResult
                {
                    Success = false,
                    Message =
                        "Unable to connect to SSLCOMMERZ. Please try again later."
                };
            }
            catch
            {
                return new SSLCOMMERZSessionResult
                {
                    Success = false,
                    Message =
                        "An unexpected error occurred while connecting to SSLCOMMERZ."
                };
            }
        }


        // =========================================================
        // VALIDATE TRANSACTION
        // =========================================================

        public async Task<SSLCOMMERZValidationResult?>
            ValidateTransactionAsync(
                string valId)
        {
            if (string.IsNullOrWhiteSpace(valId))
                return null;


            try
            {
                var requestUrl =
                    $"{_settings.ValidationUrl}" +
                    $"?val_id={Uri.EscapeDataString(valId)}" +
                    $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                    $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
                    $"&format=json";


                using var response =
                    await _httpClient.GetAsync(
                        requestUrl);


                var body =
                    await response.Content
                        .ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                    return null;


                if (string.IsNullOrWhiteSpace(body))
                    return null;


                Dictionary<string, JsonElement>? result;

                try
                {
                    result =
                        JsonSerializer.Deserialize<
                            Dictionary<string, JsonElement>>(
                            body,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                }
                catch
                {
                    return null;
                }


                if (result == null)
                    return null;


                // =====================================================
                // AMOUNT
                // =====================================================

                decimal.TryParse(
                    GetString(
                        result,
                        "amount"),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var amount);


                // =====================================================
                // RISK LEVEL
                // =====================================================

                int.TryParse(
                    GetString(
                        result,
                        "risk_level"),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var riskLevel);


                // =====================================================
                // CURRENCY
                // =====================================================

                var currency =
                    GetString(
                        result,
                        "currency_type")
                    ??
                    GetString(
                        result,
                        "currency");


                // =====================================================
                // RETURN VALIDATION RESULT
                // =====================================================

                return new SSLCOMMERZValidationResult
                {
                    Status =
                        GetString(
                            result,
                            "status"),

                    TranId =
                        GetString(
                            result,
                            "tran_id"),

                    ValId =
                        GetString(
                            result,
                            "val_id"),

                    Amount =
                        amount,

                    BankTranId =
                        GetString(
                            result,
                            "bank_tran_id"),

                    CardType =
                        GetString(
                            result,
                            "card_type"),

                    Currency =
                        currency,

                    APIConnect =
                        GetString(
                            result,
                            "APIConnect"),

                    RiskLevel =
                        riskLevel
                };
            }
            catch
            {
                return null;
            }
        }


        // =========================================================
        // BUILD BROWSER CALLBACK URL
        // =========================================================

        private string BuildReturnUrl(
            string path)
        {
            return
                $"{_settings.BaseReturnUrl.TrimEnd('/')}" +
                $"{path}";
        }


        // =========================================================
        // LIMIT STRING LENGTH
        // =========================================================

        private static string Limit(
            string? value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;


            value = value.Trim();


            return value.Length <= maxLength
                ? value
                : value.Substring(
                    0,
                    maxLength);
        }


        // =========================================================
        // READ JSON VALUE
        // =========================================================

        private static string? GetString(
            Dictionary<string, JsonElement> json,
            string key)
        {
            if (!json.TryGetValue(
                    key,
                    out var element))
            {
                return null;
            }


            return element.ValueKind switch
            {
                JsonValueKind.String =>
                    element.GetString(),

                JsonValueKind.Number =>
                    element.ToString(),

                JsonValueKind.True =>
                    "true",

                JsonValueKind.False =>
                    "false",

                JsonValueKind.Null =>
                    null,

                _ =>
                    element.ToString()
            };
        }
    }
}