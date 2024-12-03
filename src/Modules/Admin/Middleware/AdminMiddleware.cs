using pandapache.src;
using PandApache3.src.Core;
using PandApache3.src.Core.Configuration;
using PandApache3.src.Core.LoggingAndMonitoring;
using PandApache3.src.Core.Middleware;
using PandApache3.src.Core.Module;
using PandApache3.src.Core.RequestHandling;
using PandApache3.src.Core.ResponseGeneration;
using PandApache3.src.Modules.Telemetry;
using System.Diagnostics;
using System.Text;
using ExecutionContext = PandApache3.src.Core.Module.ExecutionContext;

namespace PandApache3.src.Modules.Admin.Middleware
{
    public class AdminMiddleware : IMiddleware
    {
        private readonly Func<HttpContext, Task> _next;
        private readonly IFileManager _fileManager;

        public AdminMiddleware(Func<HttpContext, Task> next, IFileManager fileManager)
        {
            _next = next;
            _fileManager = fileManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ExecutionContext.Current.Logger.LogDebug("Admin middleware");

            Request request = context.Request;

            if (request.Verb == "GET")
            {
                string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;
                if (request.Path.ToLower().StartsWith(adminURL + "/monitor"))
                {
                    context.Response = getMonitor(request);
                }
                else if (request.Path.ToLower().StartsWith(adminURL))
                    context.Response = await getAdminAsync(request);

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

        private async Task<HttpResponse> getAdminAsync(Request request)
        {
            ExecutionContext.Current.Logger.LogInfo($"Request for admin: {request.Path}");
            HttpResponse response;
            string adminURL = ServerConfiguration.Instance.AdminDirectory.URL;
            ExecutionContext.Current.Logger.LogDebug($"URL for adminsitration: {adminURL}");
            if (request.Path.ToLower().Equals(adminURL + "/status"))
            {
                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(Server.Instance.Status))
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
                        ExecutionContext.Current.Logger.LogDebug($"key and value: <{item.Key}/{item.Value}>");
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
                //Task.Run(() => Server.Instance.StoppAsync(false));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("I told you to not do that, your action will be reported to Microsoft!!!!"))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/restart"))
            {
                Task.Run(() => Server.Instance.StoppAsync(true));

                response = new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes("Restarting...."))
                };
            }
            else if (request.Path.ToLower().Equals(adminURL + "/logs"))
            {
                StringBuilder logs = new StringBuilder();

                foreach (var logEntry in ((Logger)Logger.Instance).GetLogHistory())
                {
                    logs.Append(logEntry.ToString() + "\n");
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
                        string scriptExtension = string.Empty;
                        if (ServerConfiguration.Instance.Platform.Equals("WIN"))
                            scriptExtension = ".ps1";
                        else
                            scriptExtension = ".sh";

                        string bodyScript = string.Empty;
                        foreach (string script in Directory.GetFiles(scriptsDirectory))
                        {
                            FileInfo fileInfo = new FileInfo(script);
                            if (fileInfo.Extension.Equals(scriptExtension))
                                bodyScript += $"{fileInfo.Name}\n";
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
            else if (request.Path.ToLower().StartsWith(adminURL))
            {
                response = await getAdminPage(request);
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
            TelemetryModule telemetryModule = Server.Instance.GetModule<TelemetryModule>(ModuleType.Telemetry);

            if (request.Path.ToLower().StartsWith(adminURL + "/monitor/cpu"))
            {
                var cpuUsage = telemetryModule.TelemetryCollector.GetCpuUsagePercentage();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(cpuUsage.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/availablememory"))
            {
                var availableMemory = telemetryModule.TelemetryCollector.GetAvailableMemoryMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(availableMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/privatememory"))
            {
                var privateMemory = telemetryModule.TelemetryCollector.GetPrivateMemoryUsageMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(privateMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/virtualmemory"))
            {
                var virtualMemory = telemetryModule.TelemetryCollector.GetVirtualMemoryUsageMB();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(virtualMemory.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/cputime"))
            {
                var cpuTime = telemetryModule.TelemetryCollector.GetProcessCpuTime();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(cpuTime.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskread"))
            {
                var diskRead = telemetryModule.TelemetryCollector.GetDiskReadBytesPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskRead.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskwrite"))
            {
                var diskWrite = telemetryModule.TelemetryCollector.GetDiskWriteBytesPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskWrite.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/diskqueue"))
            {
                var diskQueue = telemetryModule.TelemetryCollector.GetDiskQueueLength();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(diskQueue.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/networkreceived"))
            {
                //error
                var networkReceived = telemetryModule.TelemetryCollector.GetNetworkBytesReceivedPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(networkReceived.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/networksent"))
            {
                //error
                var networkSent = telemetryModule.TelemetryCollector.GetNetworkBytesSentPerSecond();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(networkSent.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/gccollections"))
            {
                var gcCollections = telemetryModule.TelemetryCollector.GetGCCollectionCount();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(gcCollections.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/gcheapsize"))
            {
                var gcHeapSize = telemetryModule.TelemetryCollector.GetGCHeapSizeBytes();
                return new HttpResponse(200)
                {
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(gcHeapSize.ToString()))
                };
            }
            else if (request.Path.ToLower().StartsWith(adminURL + "/monitor/metric"))
            {
                if (request.queryParameters.Count > 0)
                {
                    string metricName = request.queryParameters["name"];
                    Queue<KeyValuePair<DateTime, double>> values = telemetryModule.TelemetryCollector._metrics[metricName];
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(values);
                    HttpResponse response = new HttpResponse(200)
                    {
                        Body = new MemoryStream(Encoding.UTF8.GetBytes(json))
                    };
                    response.AddHeader("Content-Type", "application/json");
                    return response;
                }

                return new HttpResponse(404);

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

                ExecutionContext.Current.Logger.LogInfo($"Execute script with {terminal}");
                ExecutionContext.Current.Logger.LogInfo($"Script information {argumentList}");
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
                        Body = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(scriptResult))),
                    };
                    response.AddHeader("Content-Type", "application/json");
                }
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"Error with script execution {ex.Message}");
                response = new HttpResponse(500);
            }

            return response;
        }

        private async Task<HttpResponse> getAdminPage(Request request)
        {

            if (request.Verb.ToUpper().Equals("GET"))
            {
                return await GetHandlerAsync(request);
            }
            else
            {
                return new HttpResponse(404);
            }
        }

        private async Task<HttpResponse> GetHandlerAsync(Request request)
        {

            try
            {
                bool AuthNeeded = false;
                string mainDirectory = ServerConfiguration.Instance.RootDirectory;
                string filePath = Path.Combine(mainDirectory, Utils.GetFilePath(request.Path));
                if (_fileManager.Exists(filePath))
                {
                    string fileExtension = Path.GetExtension(filePath).Substring(1).ToLowerInvariant();
                    string mimeType = fileExtension switch
                    {
                        // Images
                        "jpg" or "jpeg" => "image/jpeg",
                        "png" => "image/png",
                        "gif" => "image/gif",
                        "svg" => "image/svg+xml",
                        "bmp" => "image/bmp",
                        "webp" => "image/webp",
                        "ico" => "image/x-icon",

                        // Documents textuels
                        "txt" => "text/plain",
                        "html" or "htm" => "text/html",
                        "css" => "text/css",
                        "js" => "application/javascript",
                        "json" => "application/json",
                        "xml" => "text/xml",
                        "woff" => "application/font-woff",
                        "woff2" => "font/woff2",


                        // Audio
                        "mp3" => "audio/mpeg",
                        "wav" => "audio/wav",
                        "ogg" => "audio/ogg",
                        "midi" => "audio/midi",

                        // Vidéo
                        "mp4" => "video/mp4",
                        "avi" => "video/x-msvideo",
                        "mov" => "video/quicktime",
                        "webm" => "video/webm",

                        // Archives
                        "zip" => "application/zip",
                        "rar" => "application/vnd.rar",
                        "7z" => "application/x-7z-compressed",

                        // Documents Office et formats ouverts
                        "pdf" => "application/pdf",
                        "doc" or "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "xls" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ppt" or "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        "odt" => "application/vnd.oasis.opendocument.text",
                        "ods" => "application/vnd.oasis.opendocument.spreadsheet",

                        // Cas par défaut si l'extension n'est pas reconnue
                        _ => null
                    };

                    if (mimeType != null)
                    {
                        byte[] data = await File.ReadAllBytesAsync(filePath);
                        HttpResponse httpResponse = new HttpResponse(200)
                        {
                            Body = new MemoryStream(data)
                        };
                        httpResponse.AddHeader("Content-Type", mimeType);
                        httpResponse.AddHeader("Content-Length", httpResponse.Body.Length.ToString());
                        return httpResponse;
                    }
                    else
                    {
                        Console.WriteLine("Mine type not implemented");
                        ExecutionContext.Current.Logger.LogError("Mine type not implemented");
                    }
                }
            }
            catch (Exception ex)
            {
                ExecutionContext.Current.Logger.LogError($"An error occurred with a GET request: {ex.Message}");
                return new HttpResponse(500);
            }

            return new HttpResponse(404);

        }

    }

}
