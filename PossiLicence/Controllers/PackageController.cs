using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;
using PossiLicence.Dtos;
using PossiLicence.Entityies;

namespace PossiLicence.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController(DBContext _dbContext) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetPackages()
        {
            try
            {
                var packages = await _dbContext.PackageTypes
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.CreateAt)
                    .Select(x => new
                    {
                        x.Id,
                        x.Caption,
                        x.MonthCount,
                        x.DayCount,
                        x.Price,
                        x.Description,
                        x.CreateAt,
                        Duration = x.DayCount.HasValue ?
                            $"{x.MonthCount} ay + {x.DayCount} gün" :
                            $"{x.MonthCount} ay",
                        FormattedPrice = x.Price.ToString("N2") + " ₺"
                    })
                    .ToListAsync();

                return Ok(packages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Paketler yüklenirken hata oluştu.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackage(Guid id)
        {
            try
            {
                var package = await _dbContext.PackageTypes
                    .Where(x => x.Id == id && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (package == null)
                    return NotFound(new { message = "Paket bulunamadı." });

                return Ok(package);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Paket bilgileri alınırken hata oluştu.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] CreatePackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var package = new PackageTypeDbEntity
                {
                    Caption = request.Caption.ToUpper(),
                    MonthCount = request.MonthCount,
                    DayCount = request.DayCount,
                    Price = request.Price,
                    Description = request.Description.ToUpper(),
                    IsDeleted = false
                };

                _dbContext.PackageTypes.Add(package);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = "Paket başarıyla eklendi.",
                    package = new
                    {
                        package.Id,
                        package.Caption,
                        package.MonthCount,
                        package.DayCount,
                        package.Price,
                        package.Description,
                        package.CreateAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Paket eklenirken hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Package/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] UpdatePackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var package = await _dbContext.PackageTypes
                    .Where(x => x.Id == id && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (package == null)
                    return NotFound(new { message = "Paket bulunamadı." });

                package.Caption = request.Caption.ToUpper();
                package.MonthCount = request.MonthCount;
                package.DayCount = request.DayCount;
                package.Price = request.Price;
                package.Description = request.Description.ToUpper();

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Paket başarıyla güncellendi.", package });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Paket güncellenirken hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Package/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            try
            {
                var package = await _dbContext.PackageTypes
                    .Where(x => x.Id == id && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (package == null)
                    return NotFound(new { message = "Paket bulunamadı." });

                // Soft delete
                package.IsDeleted = true;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Paket başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Paket silinirken hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Package/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetPackageStats()
        {
            try
            {
                var total = await _dbContext.PackageTypes.CountAsync(x => !x.IsDeleted);
                var totalRevenue = await _dbContext.PackageTypes
                    .Where(x => !x.IsDeleted)
                    .SumAsync(x => x.Price);

                return Ok(new
                {
                    total,
                    totalRevenue
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler alınırken hata oluştu.", error = ex.Message });
            }
        }
    }
}