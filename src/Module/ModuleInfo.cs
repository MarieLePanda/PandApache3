using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{
    public class ModuleInfo
    {
        public ModuleType Type;
        public string Name;
        public bool isEnable;

        public ModuleInfo(string name)
        {
            // Assigne le nom du module
            Name = name;

            // Détermine et assigne le type de module
            if (Enum.TryParse(name, true, out ModuleType moduleType))
            {
                Type = moduleType;
            }
            else
            {
                var validTypes = string.Join(", ", Enum.GetNames(typeof(ModuleType)));
                throw new ArgumentException($"The module name '{name}' is not valid. Valid names are: {validTypes}.");
            }

        }
    }
}
