using CuteBlogSystem.Enum;

namespace CuteBlogSystem.DTO
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public ResponseCode Code { get; set; }
        public ApiResponse(bool success, string message, object data = null, ResponseCode code = ResponseCode.None)
        {
            Success = success;
            Message = message;
            Data = data;
            Code = code;
        }
    }
}
