
namespace CommonLibs
{

    public class ReturnData<T>
    {
        public ErrorCodeEnum ErrorCode;
        public T Data;

        public ReturnData()
        {
            ErrorCode = ErrorCodeEnum.ResponseError;
            Data = default(T);
        }
        public ReturnData(T data)
        {
            ErrorCode = data != null ? ErrorCodeEnum.Success : ErrorCodeEnum.NotExists;
            Data = data;
        }
        public ReturnData(ErrorCodeEnum error)
        {
            ErrorCode = error;
            Data = default(T);
        }
        public ReturnData(ErrorCodeEnum error, T data)
        {
            ErrorCode = error;
            Data = data;
        }
        //public ReturnData(System.Net.Http.HttpResponseMessage response)
        //{
        //    if (response.StatusCode == System.Net.HttpStatusCode.NotExists)
        //        ErrorCode = ErrorCodeEnum.NotExists;
        //    else if (response.IsSuccessStatusCode)
        //        ErrorCode = ErrorCodeEnum.Success;
        //    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        //        ErrorCode = ErrorCodeEnum.WrongTokenOrTimeout;
        //    else
        //        ErrorCode = ErrorCodeEnum.Unknown;
        //}
    }

}
