using PandApache3.src.Core.Configuration;
using PandApache3.src.Core.Middleware;
using PandApache3.src.Core.Module;
using PandApache3.src.Core.RequestHandling;
using PandApache3.src.Core.ResponseGeneration;
using PandApache3.src.Modules.Admin.Middleware;
using PandApache3.src.Modules.Telemetry;
using PandApache3.src.Modules.Web;
using PandApache3.src.Modules.Web.Middleware;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Core
{
    public class Server
    {
        public string Status;
        public Dictionary<ModuleType, IModule> Modules = new Dictionary<ModuleType, IModule>();
        public Dictionary<string, Func<HttpContext, Task>> Pipelines = new Dictionary<string, Func<HttpContext, Task>>();
        public Dictionary<string, CancellationTokenSource> CancellationTokens = new Dictionary<string, CancellationTokenSource>();
        public readonly CancellationTokenSource CancellationTokenSource;
        private static AsyncLocal<ModuleConfiguration> _current = new AsyncLocal<ModuleConfiguration>();
        public ModuleConfiguration ModuleInfo;
        public IFileManager fileManager;
        private static Server _instance;


        private int _retry = 1;
        private readonly object _lock = new object();
        private Server()
        {
            Status = "PandApache3 is stopped";
            CancellationTokenSource = new CancellationTokenSource();
            ModuleInfo = new ModuleConfiguration("Server");
        }


        public static Server Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Server();
                return _instance;
            }
        }

        public T GetModule<T>(ModuleType moduleType) where T : IModule
        {
            return (T)Modules[moduleType];
        }

        public void Init()
        {
            ExecutionContext.Current = ModuleInfo;
            //ModuleInfo.Logger.LogLevel = ServerConfiguration.Instance.LogLevel;
            //ModuleInfo.Logger.LogLevel = ServerConfiguration.Instance.LogLevel;

            //Clean previous list in case of restart
            Pipelines.Clear();
            Modules.Clear();
            CancellationTokens.Clear();

            fileManager = FileManagerFactory.Instance();

            //Create pipeline
            TerminalMiddleware terminalMiddleware = new TerminalMiddleware();
            RoutingMiddleware routingMiddleware = new RoutingMiddleware(terminalMiddleware.InvokeAsync, fileManager);
            DirectoryMiddleware directoryMiddleware = new DirectoryMiddleware(routingMiddleware.InvokeAsync);
            AuthenticationMiddleware authenticationMiddleware = new AuthenticationMiddleware(directoryMiddleware.InvokeAsync);
            LoggerMiddleware loggerMiddleware = new LoggerMiddleware(authenticationMiddleware.InvokeAsync);
            Pipelines.Add("web", loggerMiddleware.InvokeAsync);


            AdminMiddleware adminMiddleware = new AdminMiddleware(terminalMiddleware.InvokeAsync, fileManager);
            DirectoryMiddleware adminDirectoryMiddleware = new DirectoryMiddleware(adminMiddleware.InvokeAsync);
            AuthenticationMiddleware adminAuthenticationMiddleware = new AuthenticationMiddleware(adminDirectoryMiddleware.InvokeAsync);
            LoggerMiddleware adminLoggerMiddleware = new LoggerMiddleware(adminAuthenticationMiddleware.InvokeAsync);

            Pipelines.Add("admin", adminLoggerMiddleware.InvokeAsync);

            //Create task scheduler
            TaskScheduler telemetryTaskScheduler = new ResourceTaskScheduler("TelemetryScheduler", ServerConfiguration.Instance.TelemetryThreadNumber);
            TaskScheduler webTaskScheduleur = new ResourceTaskScheduler("WebScheduler", ServerConfiguration.Instance.WebThreadNumber);
            TaskScheduler adminTaskScheduleur = new ResourceTaskScheduler("AdminScheduler", ServerConfiguration.Instance.AdminThreadNumber);

            //Create module

            TelemetryModule telemetryModule = new TelemetryModule(telemetryTaskScheduler);
            ConnectionManagerModule webModule = new ConnectionManagerModule(ModuleType.Web, Pipelines["web"], webTaskScheduleur);
            ConnectionManagerModule adminbModule = new ConnectionManagerModule(ModuleType.Admin, Pipelines["admin"], adminTaskScheduleur);



            Modules.Add(ModuleType.Telemetry, telemetryModule);
            Modules.Add(ModuleType.Web, webModule);
            Modules.Add(ModuleType.Admin, adminbModule);

            foreach (var moduleKey in Modules.Keys)
            {
                if (!Modules[moduleKey].isEnable())
                {
                    ExecutionContext.Current.Logger.LogWarning($"Module {moduleKey} disabled");
                    Modules.Remove(moduleKey);
                }
            }
        }

        public async Task StartAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            Status = "PandApache3 is starting";
            ExecutionContext.Current.Logger.LogInfo($"{Status}");

            CancelModuleToken();

            foreach (var moduleName in Modules.Keys)
            {
                await Modules[moduleName].StartAsync();
            }

            Status = "PandApache3 is started";
        }

        public async Task RunAsync()
        {
            ExecutionContext.Current = ModuleInfo;

            Status = "PandApache3 is up and running!";
            ExecutionContext.Current.Logger.LogInfo($"{Status}");
            List<Task> tasks = new List<Task>();
            foreach (var moduleName in Modules.Keys)
            {
                tasks.Add(Task.Run(() => Modules[moduleName].RunAsync()));
            }

            await Task.WhenAll(tasks);

        }

        public async Task StoppAsync(bool isRestart = false)
        {
            ExecutionContext.Current = ModuleInfo;

            if (!Monitor.TryEnter(_lock))
            {
                ExecutionContext.Current.Logger.LogDebug($"Thread (Thread ID: {Thread.CurrentThread.ManagedThreadId}) could not acquire the lock and will exit.");
                ExecutionContext.Current.Logger.LogInfo("Server is already stopping");
                return;
            }

            lock (_lock)
            {
                Thread currentThread = Thread.CurrentThread;
                ExecutionContext.Current.Logger.LogDebug($"Thread (Thread ID: {currentThread.ManagedThreadId}) run the function StopServerAsync");


                Status = "PandApache3 is stopping";
                ExecutionContext.Current.Logger.LogInfo($"{Status}");

                CancelModuleToken();

                foreach (var moduleName in Modules.Keys)
                {
                    Modules[moduleName].StopAsync();
                }

                // Get all threads in the current process


                Status = "PandApache3 is stopped";
                ExecutionContext.Current.Logger.LogInfo($"{Status}");

                if (isRestart == false)
                {
                    CancellationTokenSource.Cancel();
                    ExecutionContext.Current.Logger.LogInfo($"Token server: {CancellationTokenSource.Token} canceled");

                }
            }

            ExecutionContext.Current.Logger.LogDebug("Get out of the lock");
            Monitor.Exit(_lock);
        }


        private void CancelModuleToken()
        {
            foreach (var tokenName in CancellationTokens.Keys)
            {
                if (tokenName.Equals("server"))
                {
                    continue;
                }
                else
                {
                    ExecutionContext.Current.Logger.LogInfo($"Cancel previous {tokenName} token: {CancellationTokens[tokenName].Token}");
                    CancellationTokens[tokenName].Cancel();
                }
            }
        }
    }
}
