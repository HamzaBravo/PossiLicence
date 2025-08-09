
using Microsoft.AspNetCore.Authorization;
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


    // PaymentController.cs - Index methodunu güncelle
    [HttpGet("{uniqId}")]
    public async Task<IActionResult> Index(int uniqId)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.UniqId == uniqId);

        if (company == null)
            return NotFound("Firma bulunamadı.");

        // Firma'ya tanımlı paketleri getir
        List<Guid> allowedPackageIds = new List<Guid>();

        if (!string.IsNullOrEmpty(company.Packages))
        {
            try
            {
                var packageStrings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(company.Packages);
                allowedPackageIds = packageStrings.Where(p => Guid.TryParse(p, out _))
                                                .Select(p => Guid.Parse(p))
                                                .ToList();
            }
            catch
            {
                // JSON parse hatası durumunda boş liste
            }
        }

        var packages = await _dbContext.PackageTypes
            .Where(x => !x.IsDeleted && allowedPackageIds.Contains(x.Id))
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
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Form verileri eksik veya hatalı." });
            }

            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.UniqId == request.CompanyUniqId);

            var package = await _dbContext.PackageTypes
                .FirstOrDefaultAsync(x => x.Id == request.PackageId && !x.IsDeleted);

            if (company == null)
            {
                return Json(new { success = false, message = "Firma bulunamadı." });
            }

            if (package == null)
            {
                return Json(new { success = false, message = "Seçilen paket bulunamadı." });
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var randomId = Guid.NewGuid().ToString("N")[..8]; // İlk 8 karakter, tire yok
            var merchantOid = $"ORDER{timestamp}{company.UniqId}{randomId}";


            // User basket (required by PayTR)
            var basketItems = new[]
              {
            new
                {
                    name = package.Caption,
                    price = (package.Price * 100).ToString("F0"), // Kuruş cinsinden, ondalık yok
                    quantity = "1"
                }
            };

            var userBasket = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(basketItems)));

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
                merchant_ok_url = _configuration["PayTR:SuccessUrl"],
                merchant_fail_url = _configuration["PayTR:FailUrl"],
                test_mode = _configuration.GetValue<bool>("PayTR:IsTestMode") ? 1 : 0
            };

            var tokenResponse = await _payTRService.GetTokenAsync(payTRRequest);

            if (tokenResponse.status != "success")
            {
                return Json(new
                {
                    success = false,
                    message = "Ödeme sistemi hatası: " + (tokenResponse.reason ?? "Bilinmeyen hata")
                });
            }

            // Save pending payment to database
            var pendingPayment = new CompanyPurchaseDbEntity
            {
                CompanyId = company.Id,
                PackageTypeId = package.Id,
                Status = null, // Pending
                Description = $"Ödeme işlemi başlatıldı. Sipariş No: {merchantOid}, Müşteri: {request.FullName}"
            };

            await _dbContext.CompanyPurchases.AddAsync(pendingPayment);
            await _dbContext.SaveChangesAsync();


            var order = new OrderDbEntity
            {
                CompanyId = company.Id,
                PackageId = package.Id,
                OrderNumber = merchantOid,
                CompanyPurchasesId = pendingPayment.Id,
                IpAdress = GetUserIP(),
            };

            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();


            return Json(new
            {
                success = true,
                token = tokenResponse.token,
                message = "Ödeme sayfası hazırlanıyor..."
            });
        }
        catch (Exception ex)
        {
            // Log the error (burada bir logging sistemi kullanmalısınız)
            Console.WriteLine($"Payment process error: {ex.Message}");

            return Json(new
            {
                success = false,
                message = "Sistem hatası oluştu. Lütfen tekrar deneyin."
            });
        }
    }


    // POST: payment/callback - PayTR'den gelen bildirim
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromForm] PayTRCallback callback)
    {
        try
        {
            var merchantKey = _configuration["PayTR:MerchantKey"];
            var merchantSalt = _configuration["PayTR:MerchantSalt"];

            // Hash doğrulaması
            if (!_payTRService.ValidateCallback(callback, merchantKey, merchantSalt))
            {
                return BadRequest("Invalid hash");
            }


            var orders = await _dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == callback.merchant_oid);


            // Firma ve pending payment'ı bul
            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == orders.CompanyId);

            var pendingPayment = await _dbContext.CompanyPurchases
                .Where(x => x.CompanyId == company.Id && x.Status == null)
                .OrderByDescending(x => x.CreateAt)
                .FirstOrDefaultAsync();

            if (company == null || pendingPayment == null)
            {
                return BadRequest("Company or payment not found");
            }

            var package = await _dbContext.PackageTypes
                .FirstOrDefaultAsync(x => x.Id == pendingPayment.PackageTypeId);

            if (package == null)
            {
                return BadRequest("Package not found");
            }

            // Ödeme durumunu güncelle
            if (callback.status == "success")
            {
                // Başarılı ödeme
                pendingPayment.Status = true;
                pendingPayment.Description += $" | BAŞARILI: {callback.merchant_oid} - {callback.total_amount} {callback.currency}";

                // Firma lisans süresini güncelle
                DateTime newEndDate;
                if (company.EndDate.HasValue && company.EndDate > DateTime.Now)
                {
                    // Mevcut aktif paket var, üzerine ekle
                    newEndDate = company.EndDate.Value.AddMonths(package.MonthCount);
                }
                else
                {
                    // Yeni paket veya süresi dolmuş
                    newEndDate = DateTime.Now.AddMonths(package.MonthCount);
                }

                if (package.DayCount.HasValue)
                {
                    newEndDate = newEndDate.AddDays(package.DayCount.Value);
                }

                company.EndDate = newEndDate;

                await _dbContext.SaveChangesAsync();

                // PayTR'e OK cevabı
                return Ok("OK");
            }
            else
            {
                // Başarısız ödeme
                pendingPayment.Status = false;
                pendingPayment.Description += $" | BAŞARISIZ: {callback.failed_reason_msg} ({callback.failed_reason_code})";

                await _dbContext.SaveChangesAsync();

                return Ok("OK");
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Payment callback error: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET: payment/success - Başarılı ödeme sonrası yönlendirme
    [HttpGet("success")]
    [AllowAnonymous]
    public async Task<IActionResult> Success(string merchant_oid)
    {
        if (string.IsNullOrEmpty(merchant_oid))
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(x => x.OrderNumber == merchant_oid);

            var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == order.CompanyId);


            var latestPayment = await _dbContext.CompanyPurchases
                .Where(x => x.CompanyId == company.Id)
                .OrderByDescending(x => x.CreateAt)
                .FirstOrDefaultAsync();

            var package = await _dbContext.PackageTypes
                .FirstOrDefaultAsync(x => x.Id == latestPayment.PackageTypeId);

            ViewBag.Company = company;
            ViewBag.Package = package;
            ViewBag.Payment = latestPayment;
            ViewBag.OrderId = merchant_oid;


            return View();
        }
        catch
        {
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: payment/fail - Başarısız ödeme sonrası yönlendirme
    [HttpGet("fail")]
    [AllowAnonymous]
    public async Task<IActionResult> Fail(string merchant_oid)
    {
        if (string.IsNullOrEmpty(merchant_oid))
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Sipariş bilgilerini parse et
            var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == merchant_oid);

            var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == order.CompanyId);

            ViewBag.Company = company;
            ViewBag.OrderId = merchant_oid;


            return View();
        }
        catch
        {
            return RedirectToAction("Index", "Home");
        }
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
