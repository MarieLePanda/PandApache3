using pandapache.src.Configuration;
using PandApache3.src.LoggingAndMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{
    public class ModuleConfiguration
    {
        private TaskScheduler _taskScheduler;
        public ModuleType Type;
        public string Name;
        public bool isEnable;
        public TaskFactory TaskFactory { get; }
        public VirtualLogger Logger;

        public ModuleConfiguration(string name)
        {
            // Assigne le nom du module
            Name = name;

            // Détermine et assigne le type de module
            if (Enum.TryParse(name, true, out ModuleType moduleType))
            {
                Type = moduleType;
                Logger = new VirtualLogger(name);
            }
            else
            {
                var validTypes = string.Join(", ", Enum.GetNames(typeof(ModuleType)));
                throw new ArgumentException($"The module name '{name}' is not valid. Valid names are: {validTypes}.");
            }

        }
    }
}
