using CuteBlogSystem.DTO;
using CuteBlogSystem.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CuteBlogSystem.Config
{
    public class BaseController : ControllerBase
    {
        public BaseController() { }

        // 根据 ResponseCode 转换为对应的 HTTP 状态码，省去 Controller 中大量的 if-else 语句
        private int ToHttpStatusCode(ResponseCode code)
        {
            return code switch
            {
                ResponseCode.Success => StatusCodes.Status200OK,

                ResponseCode.BadRequest => StatusCodes.Status400BadRequest,
                ResponseCode.Unauthorized => StatusCodes.Status401Unauthorized,
                ResponseCode.Forbidden => StatusCodes.Status403Forbidden,
                ResponseCode.NotFound => StatusCodes.Status404NotFound,
                ResponseCode.Conflict => StatusCodes.Status409Conflict,

                ResponseCode.InvalidCredentials => StatusCodes.Status401Unauthorized,
                ResponseCode.UserNotFound => StatusCodes.Status404NotFound,
                ResponseCode.UserNameAlreadyExists => StatusCodes.Status409Conflict,
                ResponseCode.EmailAlreadyExists => StatusCodes.Status409Conflict,

                ResponseCode.FileMissing => StatusCodes.Status400BadRequest,
                ResponseCode.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
                ResponseCode.InvalidFileType => StatusCodes.Status400BadRequest,
                ResponseCode.InvalidFileContent => StatusCodes.Status400BadRequest,
                ResponseCode.InvalidInput => StatusCodes.Status400BadRequest,

                ResponseCode.UpdateFailed => StatusCodes.Status500InternalServerError,
                ResponseCode.UploadFailed => StatusCodes.Status500InternalServerError,
                ResponseCode.RegisterFailed => StatusCodes.Status400BadRequest,

                ResponseCode.ArticleNotFound => StatusCodes.Status404NotFound,
                ResponseCode.TagsNotFound => StatusCodes.Status404NotFound,
                ResponseCode.TagAlreadyExists => StatusCodes.Status409Conflict,
                ResponseCode.CategoryNotFound => StatusCodes.Status404NotFound,
                ResponseCode.CategoryAlreadyExists => StatusCodes.Status409Conflict,

                ResponseCode.InternalError => StatusCodes.Status500InternalServerError,
                ResponseCode.None => StatusCodes.Status500InternalServerError,

                _ => StatusCodes.Status500InternalServerError
            };
        }

        // 根据状态码自动返回对应的 HTTP 响应，简化 Controller 代码
        protected IActionResult ReturnResponse(ApiResponse response)
        {
            // 成功时直接返回 200 OK
            if (response.Success)
            {
                return Ok(response);
            }

            // 失败时根据 ResponseCode 映射 HTTP 状态码
            int statusCode = ToHttpStatusCode(response.Code);
            return StatusCode(statusCode, response);
        }
    }
}