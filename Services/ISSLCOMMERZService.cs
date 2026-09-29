using ShopSphere.Models;

namespace ShopSphere.Services
{
    public interface ISSLCOMMERZService
    {
        Task<SSLCOMMERZSessionResult> CreatePaymentSessionAsync(
            Order order,
            Payment payment);

        Task<SSLCOMMERZValidationResult?> ValidateTransactionAsync(
            string valId);
    }
}