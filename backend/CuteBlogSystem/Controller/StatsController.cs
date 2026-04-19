using CuteBlogSystem.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : BaseController
    {
        [HttpGet("dashboard")]
        public IActionResult GetAllInformationAboutArticle()
        {
            return Ok();
        }
    }
}
