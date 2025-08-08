using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using PossiLicence.Context;
using PossiLicence.Dtos;
using PossiLicence.Entityies;
using PossiLicence.Extensions;

namespace PossiLicence.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CompanyController(DBContext _dbContext) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var datetime = DateTime.Now;
        try
        {
            var companies = await _dbContext.Companies
                .AsNoTracking()
                .OrderByDescending(x => x.CreateAt)
                .Select(x => new
                {
                    x.Id,
                    x.UniqId,
                    x.CompanyName,
                    x.FullName,
                    x.PhoneNumber,
                    x.EndDate,
                    x.CreateAt,
                    Status = x.EndDate.HasValue ? (x.EndDate >= datetime ? "Aktif" : "Süresi Dolmuş") : "Paket Yok"
                })
                .ToListAsync();

            return Ok(companies);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Firmalar yüklenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        try
        {
            var company = await _dbContext.Companies.FindAsync(id);

            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            return Ok(company);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Firma bilgileri alınırken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var uniqId = await CompanyExtensions.GenerateUniqueIdAsync(_dbContext);

            var company = new CompanyDbEntity
            {
                UniqId = uniqId,
                CompanyName = request.CompanyName.ToUpper(),
                FullName = request.FullName.ToUpper(),
                PhoneNumber = request.PhoneNumber,
                EndDate = null // Başlangıçta paket yok
            };

            _dbContext.Companies.Add(company);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Firma başarıyla eklendi.",
                company = new
                {
                    company.Id,
                    company.UniqId,
                    company.CompanyName,
                    company.FullName,
                    company.PhoneNumber,
                    company.EndDate,
                    company.CreateAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Firma eklenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var company = await _dbContext.Companies.FindAsync(id);

            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            company.CompanyName = request.CompanyName.ToUpper();
            company.FullName = request.FullName.ToUpper();
            company.PhoneNumber = request.PhoneNumber;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Firma başarıyla güncellendi.", company });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Firma güncellenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        try
        {
            var company = await _dbContext.Companies.FindAsync(id);

            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            _dbContext.Companies.Remove(company);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Firma başarıyla silindi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Firma silinirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetCompanyStats()
    {
        var datetime = DateTime.Now;
        try
        {
            var total = await _dbContext.Companies.CountAsync();
            var active = await _dbContext.Companies.CountAsync(x => x.EndDate.HasValue && x.EndDate >= datetime);
            var expired = await _dbContext.Companies.CountAsync(x => x.EndDate.HasValue && x.EndDate < datetime);
            var noPackage = await _dbContext.Companies.CountAsync(x => !x.EndDate.HasValue);

            return Ok(new
            {
                total,
                active,
                expired,
                noPackage
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "İstatistikler alınırken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPost("{companyId}/assign-package")]
    public async Task<IActionResult> AssignPackageToCompany(Guid companyId, [FromBody] AssignPackageRequest request)
    {
        var datetime = DateTime.Now;
        try
        {
            var company = await _dbContext.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            var package = await _dbContext.PackageTypes
                .Where(x => x.Id == request.PackageId && !x.IsDeleted)
                .FirstOrDefaultAsync();

            if (package == null)
                return NotFound(new { message = "Paket bulunamadı." });

            DateTime startDate;
            DateTime endDate;
            string durationInfo;

            // Eğer firma'nın mevcut EndDate'i var ve gelecekte ise, ona ekle
            if (company.EndDate.HasValue && company.EndDate > datetime)
            {
                // Mevcut paketi var ve aktif, yeni paketi mevcut bitiş tarihine ekle
                startDate = company.EndDate.Value;
                endDate = startDate.AddMonths(package.MonthCount);

                if (package.DayCount.HasValue)
                    endDate = endDate.AddDays(package.DayCount.Value);

                durationInfo = $"Mevcut paket bitiş tarihi ({startDate:dd.MM.yyyy}) üzerine eklendi";
            }
            else
            {
                // Mevcut paketi yok veya süresi dolmuş, bugünden başlat
                endDate = datetime.AddMonths(package.MonthCount);

                if (package.DayCount.HasValue)
                    endDate = endDate.AddDays(package.DayCount.Value);

                durationInfo = company.EndDate.HasValue
                    ? "Süresi dolmuş paket yenilendi"
                    : "Yeni paket ataması";
            }

            var previousEndDate = company.EndDate;
            company.EndDate = endDate;

            var adminUserName = User.Identity.Name ?? "Admin";

            var purchase = new CompanyPurchaseDbEntity
            {
                CompanyId = company.Id,
                PackageTypeId = package.Id,
                Status = true,
                Description = $"Admin panelden {adminUserName} kullanıcısı tarafından {package.Caption} paketi atandı. " +
                             $"Süre: {(package.DayCount.HasValue ? $"{package.MonthCount} ay + {package.DayCount} gün" : $"{package.MonthCount} ay")}. " +
                             $"{durationInfo}. " +
                             $"Önceki bitiş: {(previousEndDate?.ToString("dd.MM.yyyy") ?? "Paket yok")}, " +
                             $"Yeni bitiş: {endDate:dd.MM.yyyy}"
            };

            await _dbContext.CompanyPurchases.AddAsync(purchase);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Paket başarıyla atandı.",
                endDate = endDate,
                previousEndDate = previousEndDate,
                packageName = package.Caption,
                duration = package.DayCount.HasValue ?
                    $"{package.MonthCount} ay + {package.DayCount} gün" :
                    $"{package.MonthCount} ay",
                assignedBy = adminUserName,
                startDate = datetime,
                isExtension = company.EndDate.HasValue && company.EndDate > DateTime.Now,
                durationInfo = durationInfo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Paket atanırken hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("{companyId}/purchase-history")]
    public async Task<IActionResult> GetCompanyPurchaseHistory(Guid companyId)
    {
        try
        {
            var purchases = await _dbContext.CompanyPurchases
                .Where(x => x.CompanyId == companyId)
                .Join(_dbContext.PackageTypes,
                      purchase => purchase.PackageTypeId,
                      package => package.Id,
                      (purchase, package) => new
                      {
                          purchase.Id,
                          purchase.CreateAt,
                          purchase.Status,
                          purchase.Description,
                          PackageName = package.Caption,
                          PackagePrice = package.Price,
                          Duration = package.DayCount.HasValue ?
                              $"{package.MonthCount} ay + {package.DayCount} gün" :
                              $"{package.MonthCount} ay",
                          AssignmentType = purchase.Description.Contains("Admin panelden") ? "Admin Ataması" : "Online Satın Alma",
                          IsAdminAssignment = purchase.Description.Contains("Admin panelden")
                      })
                .OrderByDescending(x => x.CreateAt)
                .ToListAsync();

            return Ok(purchases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Satın alma geçmişi alınırken hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("recent-activities")]
    public async Task<IActionResult> GetRecentActivities()
    {
        try
        {
            var activities = await _dbContext.CompanyPurchases
                .Join(_dbContext.Companies,
                      purchase => purchase.CompanyId,
                      company => company.Id,
                      (purchase, company) => new
                      {
                          purchase.CreateAt,
                          CompanyName = company.CompanyName,
                          CompanyUniqId = company.UniqId,
                          CompanyEndDate = company.EndDate,
                          purchase.Description,
                          purchase.Status,
                          IsAdminAssignment = purchase.Description.Contains("Admin panelden"),
                          AssignmentType = purchase.Description.Contains("Admin panelden") ? "Admin Ataması" : "Online Satın Alma"
                      })
                .OrderByDescending(x => x.CreateAt)
                .Take(15) // Son 15 aktiviteyi gösterelim
                .ToListAsync();

            return Ok(activities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Son aktiviteler alınırken hata oluştu.", error = ex.Message });
        }
    }
}