using Microsoft.AspNetCore.Mvc;

namespace TactiqAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok", message = "Tactiq API is running!", timestamp = DateTime.UtcNow });
    }
}
