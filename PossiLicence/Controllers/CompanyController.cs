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

    // CompanyController.cs - GetCompanies methodunu güncelle
    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var datetime = DateTime.Now;
        try
        {
            var currentAdminId = GetCurrentAdminId();
            if (currentAdminId == null)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var currentAdmin = await _dbContext.Admins.FindAsync(currentAdminId);
            if (currentAdmin == null)
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });

            IQueryable<CompanyDbEntity> companiesQuery = _dbContext.Companies.AsNoTracking();

            // SuperAdmin değilse sadece kendi eklediği firmaları görsün
            if (!currentAdmin.IsSuperAdmin)
            {
                companiesQuery = companiesQuery.Where(x => x.AdminId == currentAdminId);
            }

            var companies = await companiesQuery
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

            // Admin ID'sini al (şuanki kullanıcıdan)
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out Guid adminId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı." });
            }

            // İzin kontrolü ekle
            if (!await HasPermission("add_company"))
                return StatusCode(403, new { message = "Firma ekleme yetkiniz bulunmamaktadır." });


            var company = new CompanyDbEntity
            {
                AdminId = adminId, // Yeni eklenen
                UniqId = uniqId,
                CompanyName = request.CompanyName.ToUpper(),
                FullName = request.FullName.ToUpper(),
                PhoneNumber = request.PhoneNumber,
                Notes = request.Notes ?? string.Empty, // Yeni eklenen
                Packages = System.Text.Json.JsonSerializer.Serialize(request.PackageIds), // Yeni eklenen
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
                    company.Notes,
                    company.EndDate,
                    company.CreateAt,
                    AssignedPackagesCount = request.PackageIds.Count
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

            // İzin kontrolü - JSON response döndür
            if (!await HasPermission("edit_company"))
                return StatusCode(403, new { message = "Firma düzenleme yetkiniz bulunmamaktadır." });

            var company = await _dbContext.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            // Sadece kendi eklediği firmaları güncelleyebilsin (SuperAdmin hariç)
            var currentAdminId = GetCurrentAdminId();
            if (!await IsSuperAdmin() && company.AdminId != currentAdminId)
            {
                return StatusCode(403, new { message = "Bu firmayı düzenleme yetkiniz bulunmamaktadır." });
            }

            company.CompanyName = request.CompanyName.ToUpper();
            company.FullName = request.FullName.ToUpper();
            company.PhoneNumber = request.PhoneNumber;
            company.Notes = request.Notes ?? string.Empty;
            company.Packages = System.Text.Json.JsonSerializer.Serialize(request.PackageIds);

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
            // İzin kontrolü - JSON response döndür
            if (!await HasPermission("delete_company"))
                return StatusCode(403, new { message = "Firma silme yetkiniz bulunmamaktadır." });

            var company = await _dbContext.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            // Sadece kendi eklediği firmaları silebilsin (SuperAdmin hariç)
            var currentAdminId = GetCurrentAdminId();
            if (!await IsSuperAdmin() && company.AdminId != currentAdminId)
            {
                return StatusCode(403, new { message = "Bu firmayı silme yetkiniz bulunmamaktadır." });
            }

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
            var currentAdminId = GetCurrentAdminId();
            if (currentAdminId == null)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var currentAdmin = await _dbContext.Admins.FindAsync(currentAdminId);
            if (currentAdmin == null)
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });

            IQueryable<CompanyDbEntity> companiesQuery = _dbContext.Companies;

            // SuperAdmin değilse sadece kendi eklediği firmaları say
            if (!currentAdmin.IsSuperAdmin)
            {
                companiesQuery = companiesQuery.Where(x => x.AdminId == currentAdminId);
            }

            var total = await companiesQuery.CountAsync();
            var active = await companiesQuery.CountAsync(x => x.EndDate.HasValue && x.EndDate >= datetime);
            var expired = await companiesQuery.CountAsync(x => x.EndDate.HasValue && x.EndDate < datetime);
            var noPackage = await companiesQuery.CountAsync(x => !x.EndDate.HasValue);

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
            var currentAdminId = GetCurrentAdminId();
            if (currentAdminId == null)
                return Unauthorized(new { message = "Geçersiz kullanıcı." });

            var currentAdmin = await _dbContext.Admins.FindAsync(currentAdminId);
            if (currentAdmin == null)
                return Unauthorized(new { message = "Kullanıcı bulunamadı." });

            // Önce company purchases'ları alalım
            var purchases = await _dbContext.CompanyPurchases
                .Include(p => p.CompanyId) // Navigation property değil, sadece ID
                .OrderByDescending(x => x.CreateAt)
                .Take(50) // Önce 50 tane alalım, sonra filtreleriz
                .ToListAsync();

            // Şimdi company bilgilerini alalım
            var companyIds = purchases.Select(p => p.CompanyId).Distinct().ToList();
            var companies = await _dbContext.Companies
                .Where(c => companyIds.Contains(c.Id))
                .ToListAsync();

            // Memory'de birleştirelim
            var activities = purchases
                .Join(companies,
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
                          AssignmentType = purchase.Description.Contains("Admin panelden") ? "Admin Ataması" : "Online Satın Alma",
                          CompanyAdminId = company.AdminId
                      });

            // SuperAdmin değilse sadece kendi firmalarının aktivitelerini görsün
            if (!currentAdmin.IsSuperAdmin)
            {
                activities = activities.Where(x => x.CompanyAdminId == currentAdminId);
            }

            var result = activities
                .OrderByDescending(x => x.CreateAt)
                .Take(15)
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Son aktiviteler alınırken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPut("{id}/packages")]
    public async Task<IActionResult> UpdateCompanyPackages(Guid id, [FromBody] UpdateCompanyPackagesRequest request)
    {
        try
        {
            var company = await _dbContext.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            // JSON formatında kaydet
            company.Packages = System.Text.Json.JsonSerializer.Serialize(request.PackageIds);

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Firma paket yetkileri güncellendi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Güncelleme sırasında hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("{id}/packages")]
    public async Task<IActionResult> GetCompanyPackages(Guid id)
    {
        try
        {
            var company = await _dbContext.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { message = "Firma bulunamadı." });

            List<string> packageIds = new List<string>();

            if (!string.IsNullOrEmpty(company.Packages))
            {
                packageIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(company.Packages);
            }

            return Ok(new { packageIds });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Paket bilgileri alınırken hata oluştu.", error = ex.Message });
        }
    }


    // CompanyController.cs'ye helper method ekle (zaten var ama güncelleyelim)
    private Guid? GetCurrentAdminId()
    {
        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (adminIdClaim != null && Guid.TryParse(adminIdClaim.Value, out Guid adminId))
            return adminId;
        return null;
    }

    private async Task<bool> IsSuperAdmin()
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null) return false;

        var admin = await _dbContext.Admins.FindAsync(adminId);
        return admin?.IsSuperAdmin == true;
    }

    // İzin kontrolü için helper method
    private async Task<bool> HasPermission(string permission)
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null) return false;

        var admin = await _dbContext.Admins.FindAsync(adminId);
        if (admin?.IsSuperAdmin == true) return true; // SuperAdmin her şeyi yapabilir

        if (string.IsNullOrEmpty(admin?.Permissions)) return false;

        try
        {
            var permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(admin.Permissions);
            return permissions.Contains(permission);
        }
        catch
        {
            return false;
        }
    }
}