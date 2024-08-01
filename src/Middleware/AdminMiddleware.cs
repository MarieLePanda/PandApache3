using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.RequestHandling;
using PandApache3.src.LoggingAndMonitoring;
using PandApache3.src.ResponseGeneration;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace pandapache.src.Middleware
{
    public class AdminMiddleware : IMiddleware
    {
        private readonly Func<HttpContext, Task> _next;

        public AdminMiddleware(Func<HttpContext, Task> next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Logger.LogDebug("Admin middleware");

            Request request = context.Request;

            if (request.Verb == "GET")
            {
                string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;
                if (request.Path.ToLower().StartsWith(adminURL + "/monitor"))
                {
                    context.Response = getMonitor(request);
                }
                else if (request.Path.ToLower().StartsWith(adminURL))
                    context.Response = getAdmin(request);
                
                else
                {
                    context.Response = new HttpResponse(404);
                }

            }
            else if (request.Verb == "POST")
            {
                context.Response = postAdmin(context);
            }

            await _next(context);
        }

        private HttpResponse getAdmin(Request request)
        {
            Logger.LogInfo($"Request for admin: {request.Path}");
            HttpResponse response;
            string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;
            Logger.LogDebug($"URL for adminsitration: {adminURL}");
            if (request.Path.ToLower().Equals(adminURL + "/status"))
            {
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(Server.STATUS))
                };

            }
            else if (request.Path.ToLower().Equals(adminURL + "/reload"))
            {
                ServerConfiguration.Instance.ReloadConfiguration();
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Configuration reloaded"))
                };

            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/config"))
            {
                if (request.queryParameters.Count > 0)
                {
                    foreach (var item in request.queryParameters)
                    {
                        Logger.LogDebug($"key and value: <{item.Key}/{item.Value}>");
                        ServerConfiguration.Instance.MapConfiguration(item.Key, item.Value);
                    }

                    ServerConfiguration.Instance.Export(Path.Combine(ServerConfiguration.Instance._configurationPath, "PandApache3.conf"));
                    
                    response = new HttpResponse(200);
                }
                else
                {
                    response = new HttpResponse(200)
                    {
                        Body = new MemoryStream(Encoding.UTF8.GetBytes(ServerConfiguration.Instance.ExportJSON()))
                    };

                }
            }
            else if (request.Path.ToLower().Equals(adminURL + "/stop"))
            {
                Task.Run(() => Server.StoppingServerAsync(false));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Stopping...."))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/restart"))
            {
                Task.Run(() => Server.StoppingServerAsync(true));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Restarting...."))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/logs"))
            {
                StringBuilder logs = new StringBuilder();

                foreach(var logEntry in Logger.GetLogHistory())
                {
                    logs.Append(logEntry.Message + "\n");
                }
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(logs.ToString()))
                };
            }

            else if (request.Path.ToLower().StartsWith(adminURL + "/script"))
            {

                if (ServerConfiguration.Instance.AdminScript)
                {
                    string scriptsDirectory = ServerConfiguration.Instance.AdminDirectory.Path;

                    if (request.queryParameters.Count > 0)
                    {

                        response = RunScript(scriptsDirectory, request.queryParameters);
                    }
                    else
                    {
                        string bodyScript = "Here the list of script on the PandApache3 server:\n";
                        foreach (string script in Directory.GetFiles(scriptsDirectory))
                        {
                            FileInfo fileInfo = new FileInfo(script);
                            bodyScript += $"\t- {fileInfo.Name}\n";
                        }

                        response = new HttpResponse(200)
                        {
                            Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyScript))
                        };
                    }
                }
                else
                {
                    response = new HttpResponse(403)
                    {
                        Body = new MemoryStream(Encoding.UTF8.GetBytes("Admin script execution not allowed on the server"))
                    };
                }

            }
            else
            {
                response = new HttpResponse(404)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Oh oh, you should not have seen that"))
                };
            }
            return response;
        }

        private HttpResponse postAdmin(HttpContext context)
        {
            if (context.Request.Headers["Content-Type"] != null && context.Request.Headers["Content-Type"].StartsWith("multipart/form-data"))
            {
                string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;
                string scriptsDirectory = ServerConfiguration.Instance.AdminDirectory.Path;

                if (context.Request.Path.ToLower().StartsWith(adminURL + "/script"))
                {
                    if (ServerConfiguration.Instance.AdminScript)
                        return RequestParser.UploadHandler(context.Request, true);
                    else
                        return new HttpResponse(403);
                }
            }

            return new HttpResponse(404);

        }

        private HttpResponse getMonitor(Request request)
        {
            string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;

            if (request.Path.ToLower().StartsWith(adminURL + "/monitor/cpu"))
            {
                var cpuUsage = Server.TELEMETRY.GetCpuUsagePercentage();
                return new HttpResponse(200)
                { 
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(cpuUsage.ToString())) 
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/availablememory"))
            {
                var availableMemory = Server.TELEMETRY.GetAvailableMemoryMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(availableMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/privatememory"))
            {
                var privateMemory = Server.TELEMETRY.GetPrivateMemoryUsageMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(privateMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/virtualmemory"))
            {
                var virtualMemory = Server.TELEMETRY.GetVirtualMemoryUsageMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(virtualMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/cputime"))
            {
                var cpuTime = Server.TELEMETRY.GetProcessCpuTime();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(cpuTime.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskread"))
            {
                var diskRead = Server.TELEMETRY.GetDiskReadBytesPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskRead.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskwrite"))
            {
                var diskWrite = Server.TELEMETRY.GetDiskWriteBytesPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskWrite.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskqueue"))
            {
                var diskQueue = Server.TELEMETRY.GetDiskQueueLength();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskQueue.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/networkreceived"))
            {
                //error
                var networkReceived = Server.TELEMETRY.GetNetworkBytesReceivedPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(networkReceived.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/networksent"))
            {
                //error
                var networkSent = Server.TELEMETRY.GetNetworkBytesSentPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(networkSent.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/gccollections"))
            {
                var gcCollections = Server.TELEMETRY.GetGCCollectionCount();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(gcCollections.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/gcheapsize"))
            {
                var gcHeapSize = Server.TELEMETRY.GetGCHeapSizeBytes();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(gcHeapSize.ToString()))
                };
            }
            else
            {
                return new HttpResponse(404);
            }

        }
        private HttpResponse RunScript(string scriptDirectory, Dictionary<string, string> queryParameters)
        {
            //Execute script
            HttpResponse response = null;
            string terminal = string.Empty;
            if (ServerConfiguration.Instance.Platform.Equals("WIN"))
                terminal = "powershell.exe";
            else
                terminal = "/bin/bash";

            string argumentList = $"{Path.Combine(scriptDirectory, queryParameters["name"])}";
            foreach (var item in queryParameters)
            {
                if (item.Key != "name")
                {
                    argumentList += $" {item.Value}";
                }
            }
            var processInfo = new ProcessStartInfo
            {
                FileName = terminal,
                Arguments = argumentList,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            try
            {

                Logger.LogInfo($"Execute script with {terminal}");
                Logger.LogInfo($"Script information {argumentList}");
                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    process.WaitForExit();
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();

                    ScriptResult scriptResult = new ScriptResult
                    {
                        ExitCode = process.ExitCode,
                        StandardOutput = standardOutput,
                        ErrorOutput = standardError
                    };


                    response = new HttpResponse(200)
                    {
                        Body = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(scriptResult)))
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error with script execution {ex.Message}");
                response = new HttpResponse(500);
            }

            return response;
        }


    }

}
