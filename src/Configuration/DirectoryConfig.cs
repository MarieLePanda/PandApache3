using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Configuration
{
    public class DirectoryConfig
    {
        public string Path { get; set; }
        public string AuthType { get; set; }
        public string AuthName { get; set; }
        public string AuthUserFile { get; set; }
        public string Require { get; set; }
    }

}
