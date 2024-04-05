
namespace pandapache.src.Routing
{
    public class Route
    {
        public string UrlPattern { get; }
        public string ControllerName { get; }
        public string ActionName { get; }

        public Route(string urlPattern, string controllerName, string actionName)
        {
            UrlPattern = urlPattern;
            ControllerName = controllerName;
            ActionName = actionName;
        }
    }
}