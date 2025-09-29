using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Reflection;

namespace Monitoring
{
    public static class MonitorService
    {

        public static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "UnknownService";
        public static TracerProvider TracerProvider;
        public static ActivitySource ActivitySource = new ActivitySource(ServiceName);

        public static ILogger Log => Serilog.Log.Logger;

        static MonitorService()
            {
            //Open Telemetry
            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddConsoleExporter()
                .AddSource(ActivitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
                .Build();


            //Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
        }
    }
}
