using PandApache3.src.Core.LoggingAndMonitoring;
using PandApache3.src.Core.RequestHandling;
using System.Net;
using System.Text;

namespace PandApache3.src.Core.ErrorHandling
{
    public class ErrorHandler
    {
        public static async Task<HttpResponse> HandleErrorAsync(Exception exception)
        {
            Logger.Instance.LogError($"An error occurred: {exception.Message}");

            // You can implement custom logic to handle different types of exceptions here
            // For simplicity, this example returns a generic error response
            return await GenerateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred while processing the request.");
        }

        private static async Task<HttpResponse> GenerateErrorResponse(HttpStatusCode statusCode, string errorMessage)
        {
            var response = new HttpResponse((int)statusCode);
            response.AddHeader("Content-Type", "text/plain");
            response.Body = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));

            return response;
        }
    }
}
