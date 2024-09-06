using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using PandApache3.src.LoggingAndMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{
    public class TelemetryModule : IModule
    {
        private static TelemetryModule _instance;

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
                ModuleInfo defaultInfo = new ModuleInfo("Telemetry")
                {
                    isEnable = true,
                };
            }
        }

        public async Task StartAsync()
        {
            Logger.LogInfo("Starting Telemetry module");
            Server.Instance.CancellationTokens.Add("telemetry", _cancellationTokenSource);
            TelemetryCollector = new Telemetry();
        }

        public async Task RunAsync()
        {
            Logger.LogInfo("Running Telemetry module");

            while (_cancellationTokenSource.IsCancellationRequested == false)
            {
                await TaskFactory.StartNew(() => TelemetryCollector.CollectMetricsAsync(30));
                
            }
        }

        public async Task StopAsync()
        {
            Logger.LogInfo("Stopping Telemetry module");
        }

        public bool isEnable()
        {
            return ModuleInfo.isEnable;
        }
    }
}
