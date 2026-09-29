namespace ShopSphere.Models
{
    public class SSLCOMMERZValidationResult
    {
        public string? Status { get; set; }

        public string? TranId { get; set; }

        public string? ValId { get; set; }

        public decimal Amount { get; set; }

        public string? BankTranId { get; set; }

        public string? CardType { get; set; }

        public string? Currency { get; set; }

        public string? APIConnect { get; set; }

        public int RiskLevel { get; set; }
    }
}