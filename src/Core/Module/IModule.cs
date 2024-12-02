

namespace PandApache3.src.Core.Module
{

    public interface IModule
    {
        Task StartAsync();

        Task RunAsync();

        Task StopAsync();

        bool isEnable();


    }
}
