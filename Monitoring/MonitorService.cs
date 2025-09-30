using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Reflection;
using System.Net;

namespace Monitoring
{
    public static class MonitorService
    {

        public static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        public static TracerProvider TracerProvider;
        public static ActivitySource ActivitySource = new ActivitySource(ServiceName, "1.0.0");

        public static ILogger Log => Serilog.Log.Logger;

        static MonitorService()
            {
            //Open Telemetry
            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddConsoleExporter()
                .AddZipkinExporter(config =>
                {
                    config.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                })
                .AddSource(ActivitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
                .Build();


            //Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.Seq("http://seq:5341")
                .CreateLogger();
        }
    }
}
