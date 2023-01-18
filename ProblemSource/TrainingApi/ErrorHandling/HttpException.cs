
namespace TrainingApi.ErrorHandling
{
    public class HttpException : Exception
    {
        public HttpException(string message) : this(message, StatusCodes.Status500InternalServerError)
        { }
        public HttpException(string message, int status) : base(message)
        {
            StatusCode = status;
        }

        public int StatusCode { get; }
    }
}
