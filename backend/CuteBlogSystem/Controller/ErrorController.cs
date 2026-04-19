using CuteBlogSystem.Config;
using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Controller
{
    [ApiController]
    public class ErrorController : BaseController
    {
        [Route("/error")]
        public IActionResult HandleError()
        {
            var context = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var exception = context?.Error;
            var response = new ApiResponse
            (
                false,
                "服务器异常！",
                code:ResponseCode.InternalError
            );
            return ReturnResponse(response);
        }
    }
}
