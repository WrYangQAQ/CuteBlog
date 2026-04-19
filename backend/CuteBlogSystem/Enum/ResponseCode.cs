namespace CuteBlogSystem.Enum
{
    public enum ResponseCode
    {
        None = 0,
        Success = 200,

        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
        Conflict = 409,
        Forbidden = 403,

        InvalidCredentials = 1001,
        UserNotFound = 1002,
        UserNameAlreadyExists = 1003,
        EmailAlreadyExists = 1004,

        FileMissing = 2001,
        FileTooLarge = 2002,
        InvalidFileType = 2003,
        InvalidFileContent = 2004,
        InvalidInput = 2005,

        UpdateFailed = 3001,
        UploadFailed = 3002,
        RegisterFailed = 3003,

        ArticleNotFound = 4001,
        TagsNotFound = 4002,
        TagAlreadyExists = 4003,
        CategoryNotFound = 4004,
        CategoryAlreadyExists = 4005,

        InternalError = 5000,

    }
}
