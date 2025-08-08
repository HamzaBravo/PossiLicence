
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;
using PossiLicence.Entityies;
using PossiLicence.Interfaces;
using PossiLicence.Models;
using System.Text.Json;

[Route("payment")]
public class PaymentController : Controller
{
    private readonly DBContext _dbContext;
    private readonly IPayTRService _payTRService;
    private readonly IConfiguration _configuration;

    public PaymentController(DBContext dbContext, IPayTRService payTRService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _payTRService = payTRService;
        _configuration = configuration;
    }


    // GET: payment/{uniqId}
    [HttpGet("{uniqId}")]
    public async Task<IActionResult> Index(int uniqId)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.UniqId == uniqId);

        if (company == null)
            return NotFound("Firma bulunamadı.");

        var packages = await _dbContext.PackageTypes
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Price)
            .ToListAsync();

        ViewBag.Company = company;
        ViewBag.Packages = packages;

        return View();
    }

    // POST: payment/process
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromForm] PaymentProcessRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.UniqId == request.CompanyUniqId);

        var package = await _dbContext.PackageTypes
            .FirstOrDefaultAsync(x => x.Id == request.PackageId && !x.IsDeleted);

        if (company == null || package == null)
            return BadRequest("Geçersiz firma veya paket.");

        // Create unique order ID
        var merchantOid = $"ORDER_{DateTime.Now:yyyyMMddHHmmss}_{company.UniqId}_{Guid.NewGuid():N}";

        // User basket (required by PayTR)
        var userBasket = JsonSerializer.Serialize(new[]
        {
            new { name = package.Caption, price = package.Price.ToString("F2"), quantity = 1 }
        });

        var payTRRequest = new PayTRTokenRequest
        {
            merchant_id = _configuration["PayTR:MerchantId"],
            user_ip = GetUserIP(),
            merchant_oid = merchantOid,
            email = request.Email,
            payment_amount = (int)(package.Price * 100), // Convert to cents
            user_basket = userBasket,
            user_name = request.FullName,
            user_address = request.Address,
            user_phone = request.Phone,
            merchant_ok_url = $"{Request.Scheme}://{Request.Host}/payment/success",
            merchant_fail_url = $"{Request.Scheme}://{Request.Host}/payment/fail",
            test_mode = _configuration.GetValue<bool>("PayTR:IsTestMode") ? 1 : 0
        };

        var tokenResponse = await _payTRService.GetTokenAsync(payTRRequest);

        if (tokenResponse.status != "success")
        {
            ViewBag.Error = tokenResponse.reason ?? "Ödeme başlatılamadı.";
            return View("Error");
        }

        // Save pending payment to database
        var pendingPayment = new CompanyPurchaseDbEntity
        {
            CompanyId = company.Id,
            PackageTypeId = package.Id,
            Status = null, // Pending
            Description = $"Ödeme işlemi başlatıldı. Sipariş No: {merchantOid}, Müşteri: {request.FullName}"
        };

        _dbContext.CompanyPurchases.Add(pendingPayment);
        await _dbContext.SaveChangesAsync();

        ViewBag.Token = tokenResponse.token;
        ViewBag.Company = company;
        ViewBag.Package = package;
        ViewBag.CustomerInfo = request;

        return View("Payment");
    }

    private string GetUserIP()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "127.0.0.1";
    }
}
