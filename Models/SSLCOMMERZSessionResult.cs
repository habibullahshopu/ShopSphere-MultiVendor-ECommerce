namespace ShopSphere.Models
{
    public class SSLCOMMERZSessionResult
    {
        public bool Success { get; set; }

        public string? GatewayPageUrl { get; set; }

        public string? SessionKey { get; set; }

        public string? Message { get; set; }

        public string? TransactionId { get; set; }
    }
}