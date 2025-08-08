using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;

namespace PossiLicence.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LicenceController(DBContext _dBContext) : ControllerBase
{
    [HttpGet("checked-licence")]
    public async Task<IActionResult> CheckedLicenceAsync(int companyId)
    {
        var datetime = DateTime.Now;

        if (companyId.ToString().Length != 5 ||!int.TryParse(companyId.ToString(), out int parsedId))
            return StatusCode(404, new { message = "Geçersiz firma id si" });
     
        var company = await _dBContext.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.UniqId == companyId);

        if (company is null)
            return StatusCode(404, new { message = "Firma Bulunamadı" });

        if (company.EndDate is null)
            return StatusCode(422, new { message = "Firma Daha Önce Satın Alım Yapmamış." });


        if (company.EndDate < datetime)
            return StatusCode(422, new { message = "Hizmet Süresi Sona Ermiş" });


        return StatusCode(StatusCodes.Status200OK, "Hizmet Süresi Devam Ediyor");
    }
}
