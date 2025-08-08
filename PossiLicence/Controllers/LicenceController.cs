using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PossiLicence.Context;

namespace PossiLicence.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LicenceController(DBContext _dBContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Get()
    {
        try
        {
            var licences = await _dBContext.CompanyPurchases.ToListAsync();
            return Ok(licences);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving data: {ex.Message}");
        }
    }
}
