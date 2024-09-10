using pandapache.src.RequestHandling;
using PandApache3.src.LoggingAndMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Module
{

    public interface IModule
    {
        Task StartAsync();

        Task RunAsync();

        Task StopAsync();

        bool isEnable();


    }
}
