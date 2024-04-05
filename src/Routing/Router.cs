
namespace pandapache.src.Routing
{
    public class Router
    {
        private readonly List<Route> routes = new List<Route>();

        public void AddRoute(Route route)
        {
            routes.Add(route);
        }

        public (string controllerName, string actionName) RouteRequest(string path)
        {
            foreach (var route in routes)
            {
                if (IsPathMatching(route.UrlPattern, path))
                {
                    return (route.ControllerName, route.ActionName);
                }
            }

            throw new Exception($"No route matched the path: {path}");
        }

        private bool IsPathMatching(string pattern, string path)
        {
            // Basic pattern matching logic (for simplicity)
            // You can implement more sophisticated pattern matching here
            return pattern == path;
        }
    }
}