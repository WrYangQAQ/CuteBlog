using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        [HttpGet("dashboard")]
        public IActionResult GetAllInformationAboutArticle()
        {
            return Ok();
        }
    }
}
