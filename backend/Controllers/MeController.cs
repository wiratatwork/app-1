using System.Security.Claims;
using App1.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App1.Backend.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserInfoDto> GetMe()
    {
        var user = new UserInfoDto
        {
            Subject = User.FindFirstValue("sub") ?? string.Empty,
            Username = User.FindFirstValue("preferred_username"),
            Email = User.FindFirstValue("email"),
            Name = User.FindFirstValue("name"),
        };

        return Ok(user);
    }
}
