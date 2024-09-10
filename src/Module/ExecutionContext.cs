using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{
    public static class ExecutionContext
    {
        private static AsyncLocal<ModuleConfiguration> _current = new AsyncLocal<ModuleConfiguration>();

        public static ModuleConfiguration Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}
