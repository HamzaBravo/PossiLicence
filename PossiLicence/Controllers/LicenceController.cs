using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;

namespace PossiLicence.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LicenceController(DBContext _dBContext) : ControllerBase
{
    [HttpGet("checked-licance")]
    public async Task<IActionResult> CheckedLicanceAsync(int companyId)
    {
        var datetime = DateTime.Now;

        var company = await _dBContext.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.UniqId == companyId);

        if (company is null)
            return StatusCode(StatusCodes.Status204NoContent, "Firma Bulunamadı");

        if (company.EndDate is null)
            return StatusCode(StatusCodes.Status204NoContent, "Firma Daha Önce Satın Alım Yapmamış.");

        if (company.EndDate < datetime)
            return StatusCode(StatusCodes.Status204NoContent, "Hizmet Süresi Sona Ermiş");

        return StatusCode(StatusCodes.Status200OK, "Hizmet Süresi Devam Ediyor");
    }
}
