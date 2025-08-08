using PossiLicence.Interfaces;
using PossiLicence.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


public class PayTRService : IPayTRService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PayTRService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<PayTRTokenResponse> GetTokenAsync(PayTRTokenRequest request)
    {
        var merchantKey = _configuration["PayTR:MerchantKey"];
        var merchantSalt = _configuration["PayTR:MerchantSalt"];

        // Generate hash
        request.paytr_token = GenerateHash(request, merchantKey, merchantSalt);

        // Convert to form data
        var formData = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", request.merchant_id),
            new("user_ip", request.user_ip),
            new("merchant_oid", request.merchant_oid),
            new("email", request.email),
            new("payment_amount", request.payment_amount.ToString()),
            new("currency", request.currency),
            new("user_basket", request.user_basket),
            new("no_installment", request.no_installment.ToString()),
            new("max_installment", request.max_installment.ToString()),
            new("user_name", request.user_name),
            new("user_address", request.user_address),
            new("user_phone", request.user_phone),
            new("merchant_ok_url", request.merchant_ok_url),
            new("merchant_fail_url", request.merchant_fail_url),
            new("test_mode", request.test_mode.ToString()),
            new("debug_on", request.debug_on.ToString()),
            new("timeout_limit", request.timeout_limit.ToString()),
            new("lang", request.lang),
            new("paytr_token", request.paytr_token)
        };

        var formContent = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(_configuration["PayTR:ApiUrl"], formContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<PayTRTokenResponse>(responseContent);
    }

    public string GenerateHash(PayTRTokenRequest request, string merchantKey, string merchantSalt)
    {
        var hashString = $"{request.merchant_id}{request.user_ip}{request.merchant_oid}{request.email}" +
                        $"{request.payment_amount}{request.user_basket}{request.no_installment}" +
                        $"{request.max_installment}{request.currency}{request.test_mode}{merchantSalt}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashString));
        return Convert.ToBase64String(hashBytes);
    }

    public bool ValidateCallback(PayTRCallback callback, string merchantKey, string merchantSalt)
    {
        var hashString = $"{callback.merchant_oid}{merchantSalt}{callback.status}{callback.total_amount}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashString));
        var calculatedHash = Convert.ToBase64String(hashBytes);

        return calculatedHash == callback.hash;
    }
}