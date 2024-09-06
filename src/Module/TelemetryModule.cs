using pandapache.src.Configuration;
using PandApache3.src.LoggingAndMonitoring;


namespace PandApache3.src.Module
{
    public class TelemetryModule : IModule
    {
        private static TelemetryModule _instance;
        private static AsyncLocal<ModuleInfo> _current = new AsyncLocal<ModuleInfo>();

        private VirtualLogger Logger;
        private TaskScheduler _taskScheduler;
        public TaskFactory TaskFactory { get; }
        public Telemetry TelemetryCollector { get; set; }
        public ModuleInfo ModuleInfo { get; set; }
        private CancellationTokenSource _cancellationTokenSource;
        public TelemetryModule(TaskScheduler taskScheduler)
        {

            _taskScheduler = taskScheduler;
            _cancellationTokenSource = new CancellationTokenSource();
            TaskFactory = new TaskFactory(_taskScheduler);
            Logger = new VirtualLogger("TelemetryLogger", "debug");

            bool moduleInfoExist = false;
            foreach (ModuleInfo moduleInfo in ServerConfiguration.Instance.Modules)
            {
                if (moduleInfo.Type == ModuleType.Telemetry)
                {
                    moduleInfoExist = true;
                    ModuleInfo = moduleInfo;
                }
            }

            if (!moduleInfoExist)
            {
                ModuleInfo = new ModuleInfo("Telemetry")
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

        VirtualLogger IModule.Logger()
        {
            return Logger;
        }
    }
}
