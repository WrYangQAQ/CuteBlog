namespace CuteBlogSystem.Enum
{
    public enum ResponseCode
    {
        None = 0,

        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
        Conflict = 409,

        InvalidCredentials = 1001,
        UserNotFound = 1002,
        UserNameAlreadyExists = 1003,
        EmailAlreadyExists = 1004,

        FileMissing = 2001,
        FileTooLarge = 2002,
        InvalidFileType = 2003,
        InvalidFileContent = 2004,

        UpdateFailed = 3001,
        UploadFailed = 3002,
        RegisterFailed = 3003
    }
}
