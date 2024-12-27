[<AutoOpen>]
module Microsoft.Extensions.DependencyInjection.Default

#nowarn "0020" // remove ignore warning

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Diagnostics.HealthChecks
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open OpenTelemetry
open OpenTelemetry.Metrics
open OpenTelemetry.Trace

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
type IHostApplicationBuilder with
    member builder.AddServiceDefaults() =
        builder.ConfigureOpenTelemetry()

        builder.AddDefaultHealthChecks()

        builder.Services.AddServiceDiscovery()

        builder.Services.ConfigureHttpClientDefaults(fun http ->
            http.AddStandardResilienceHandler()
            http.AddServiceDiscovery() |> ignore
        )

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(fun options ->
        //     options.AllowedSchemes <- ["https"]
        // )

        builder

    member builder.ConfigureOpenTelemetry() =
        builder.Logging.AddOpenTelemetry(fun logging ->
            logging.IncludeFormattedMessage <- true
            logging.IncludeScopes <- true
        )

        builder.Services
            .AddOpenTelemetry()
            .WithMetrics(fun metrics -> metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation() |> ignore)
            .WithTracing(fun tracing ->
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    //.AddGrpcClientInstrumentation()  // Uncomment to enable gRPC instrumentation
                    .AddHttpClientInstrumentation()
                |> ignore
            )

        builder.AddOpenTelemetryExporters()

    member builder.AddOpenTelemetryExporters() =
        let useOtlpExporter = not (System.String.IsNullOrWhiteSpace(builder.Configuration.["OTEL_EXPORTER_OTLP_ENDPOINT"]))

        if useOtlpExporter then
            builder.Services.AddOpenTelemetry().UseOtlpExporter() |> ignore

    member builder.AddDefaultHealthChecks() =
        builder.Services
            .AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", (fun () -> HealthCheckResult.Healthy()), [ "live" ])
        |> ignore


type WebApplication with
    member app.MapDefaultEndpoints() =
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if app.Environment.IsDevelopment() then
            app.MapHealthChecks("/health")
            app.MapHealthChecks("/alive", HealthCheckOptions(Predicate = fun r -> r.Tags.Contains("live"))) |> ignore

        app
