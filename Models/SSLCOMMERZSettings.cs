namespace ShopSphere.Models
{
    public class SSLCOMMERZSettings
    {
        public string StoreId { get; set; } = string.Empty;

        public string StorePassword { get; set; } = string.Empty;

        public bool IsSandbox { get; set; } = true;

        public string SessionUrl { get; set; } = string.Empty;

        public string ValidationUrl { get; set; } = string.Empty;

        // Browser callback URLs
        // Example:
        // https://localhost:7272
        public string BaseReturnUrl { get; set; } = string.Empty;

        // Public URL used by SSLCOMMERZ server for IPN
        // Example:
        // https://designed-albatross-overrule.ngrok-free.dev/Customer/Payment/IPN
        public string IPNUrl { get; set; } = string.Empty;
    }
}