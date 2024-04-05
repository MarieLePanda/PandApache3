using pandapache.src.RequestHandling;

namespace pandapache.src.Middleware
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context);
    }

}
