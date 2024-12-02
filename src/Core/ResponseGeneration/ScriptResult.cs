using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Core.ResponseGeneration
{
    public class ScriptResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string ErrorOutput { get; set; }
        public ScriptResult() { }
    }
}
