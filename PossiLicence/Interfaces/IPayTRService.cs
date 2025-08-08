using PossiLicence.Models;

namespace PossiLicence.Interfaces;

public interface IPayTRService
{
    Task<PayTRTokenResponse> GetTokenAsync(PayTRTokenRequest request);
    string GenerateHash(PayTRTokenRequest request, string merchantKey, string merchantSalt);
    bool ValidateCallback(PayTRCallback callback, string merchantKey, string merchantSalt);
}
