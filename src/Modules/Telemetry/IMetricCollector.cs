using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Modules.Telemetry
{
    public interface IMetricCollector
    {
        public double GetTelemetryValue(string metricName, int samples = 4, int delay = 1000, bool convertToMB = false);

        public List<string> MetricKeys();
    }
}
