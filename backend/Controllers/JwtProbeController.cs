using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App1.Backend.Controllers;

[ApiController]
[Route("api/jwt")]
public sealed class JwtProbeController : ControllerBase
{
    [HttpGet("probe")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Probe()
    {
        return Ok(new { message = "JWT valid", subject = User.FindFirst("sub")?.Value });
    }
}
