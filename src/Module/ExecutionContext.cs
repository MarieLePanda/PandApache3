using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{
    public static class ExecutionContext
    {
        private static AsyncLocal<ModuleInfo> _current = new AsyncLocal<ModuleInfo>();

        public static ModuleInfo Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
