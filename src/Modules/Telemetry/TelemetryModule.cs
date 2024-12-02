
using PandApache3.src.Core;
using PandApache3.src.Core.Configuration;
using PandApache3.src.Core.Module;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Modules.Telemetry
{
    public class TelemetryModule : IModule
    {
        private static TelemetryModule _instance;
        private static AsyncLocal<ModuleConfiguration> _current = new AsyncLocal<ModuleConfiguration>();

        private TaskScheduler _taskScheduler;
        public TaskFactory TaskFactory { get; }
        public Telemetry TelemetryCollector { get; set; }
        public ModuleConfiguration ModuleInfo { get; set; }
        private CancellationTokenSource _cancellationTokenSource;
        public TelemetryModule(TaskScheduler taskScheduler)
        {

            _taskScheduler = taskScheduler;
            _cancellationTokenSource = new CancellationTokenSource();
            TaskFactory = new TaskFactory(_taskScheduler);

            bool moduleInfoExist = false;
            foreach (ModuleConfiguration moduleInfo in ServerConfiguration.Instance.Modules)
            {
                if (moduleInfo.Type == ModuleType.Telemetry)
                {
                    moduleInfoExist = true;
                    ModuleInfo = moduleInfo;
                }
            }

            if (!moduleInfoExist)
            {
                ModuleInfo = new ModuleConfiguration("Telemetry")
                {
                    isEnable = true,
                };
            }
        }

        public async Task StartAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            ExecutionContext.Current.Logger.LogInfo("Starting Telemetry module");
            Server.Instance.CancellationTokens.Add("telemetry", _cancellationTokenSource);
            TelemetryCollector = new Telemetry();
        }

        public async Task RunAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            ExecutionContext.Current.Logger.LogInfo("Running Telemetry module");

            while (_cancellationTokenSource.IsCancellationRequested == false)
            {
                await TaskFactory.StartNew(() => TelemetryCollector.CollectMetricsAsync(30));

            }
        }

        public async Task StopAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            ExecutionContext.Current.Logger.LogInfo("Stopping Telemetry module");
        }

        public bool isEnable()
        {
            return ModuleInfo.isEnable;
        }

    }
}
