namespace PandApache3.src.Core.Configuration
{
    public class DirectoryConfig
    {
        public string Type { get; set; }
        public string Path { get; set; }
        public string URL { get; set; }
        public string AuthType { get; set; }
        public string AuthName { get; set; }
        public string AuthUserFile { get; set; }
        public string Require { get; set; }
        public List<string> AllowedMethods { get; set; }
    }

}
