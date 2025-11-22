using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Enrichers;
using System.Diagnostics;
using System.Reflection;

namespace Shared
{
    public static class MonitorService
    {
        public static readonly string ServiceName =
            Assembly.GetEntryAssembly()?.GetName().Name ??
            Assembly.GetExecutingAssembly().GetName().Name ??
            "UnknownService";

        public static readonly ActivitySource ActivitySource = new(ServiceName, "1.0.0");
        private static TracerProvider TracerProvider;
        public static ILogger Log => Serilog.Log.Logger;


        // --- Prometheus metrics ---
        public static readonly Counter RequestsTotal =
            Metrics.CreateCounter($"{ServiceName.ToLower()}_requests_total", "Total HTTP requests.");
        public static readonly Counter CacheHits =
            Metrics.CreateCounter($"{ServiceName.ToLower()}_cache_hits_total", "Redis cache hits.");
        public static readonly Counter CacheMisses =
            Metrics.CreateCounter($"{ServiceName.ToLower()}_cache_misses_total", "Redis cache misses.");

        static MonitorService()
        {
            // --- Serilog (console + Seq) ---
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Seq("http://seq:5341")
                .CreateLogger();

            Log.Information("[MonitorService] Initialized for {ServiceName}", ServiceName);

            // --- Zipkin tracing ---
            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddConsoleExporter()
                .AddZipkinExporter(config =>
                {
                    config.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                })
                .AddSource(ActivitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
                .SetSampler(new AlwaysOnSampler())
                .Build();

            Log.Information("[MonitorService] Zipkin tracing active for {ServiceName}", ServiceName);
        }

        // --- Metric helpers ---
        public static void RecordRequest() => RequestsTotal.Inc();
        public static void RecordCacheHit() => CacheHits.Inc();
        public static void RecordCacheMiss() => CacheMisses.Inc();
    }
}
