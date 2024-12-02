
namespace PandApache3.src.Core.Module
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
