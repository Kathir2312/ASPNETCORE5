using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomersAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            try
            {
                Log.Information("Application starts up..........");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unexpected end of application,...");
                    }
            finally{
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    //webBuilder.ConfigureAppConfiguration(ConfigureAppConfiguration);
                    //webBuilder.ConfigureLogging(ConfigureLogging);
                });

        private static void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder configBuilder)
        {
            var env = context.HostingEnvironment;

            // Configuration: In this method we are adding configuration from different sources including environment
            //                variables, .json files and a List of KeyValue pairs from memory. The configuration will be
            //                added in order which means that any duplicate settings in the environment variables
            //                will override any already added settings. This allows options like the environment to be used
            //                to alter configuration based on environment.
            configBuilder

                // Configuration: add some configuration from an in memory collection
                .AddInMemoryCollection(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Setting1", "Setting1Value"),
                    new KeyValuePair<string, string>("Setting2", "Setting2Value")
                })

                // Configuration: add configuration from some optional .json files
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)

                // Configuration: add some configuration from environment variables
                .AddEnvironmentVariables();
        }
        private static void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder loggingBuilder)
        {
            var env = context.HostingEnvironment;
            var config = context.Configuration;

            // Logging: The ILoggerFactory is used to keep track of all the different loggers that have been added. When
            //          using dependency injection to registered and can be accessed through dependency injection using the
            //          ILogger interface. Here there are three loggers that are added to the loggerFactory instance.
            loggingBuilder.AddDebug();

            // Logging: Some third-party logger's like Serilog also have logging support for ASP.NET Core. The Serilog.Extensions.Logging,
            //          Serilog.Settings.Configuration and Serilog.Sinks.Console packages have been added to this project. These
            //          packages allow the application to use Serilog, set the configuration and then display it using Literate in
            //          the console output. The below code creates a logger using the Serilog configuration in the AppSettings.json
            //          file and then adds the logger to the LoggerFactory.
            if (env.EnvironmentName != "DEV")
            {
                // Logging: Console loggers are very useful for debugging, but not performant and should be omitted in
                //          production. See https://blogs.msdn.microsoft.com/webdev/2017/04/26/asp-net-core-logging/ for
                //          logging into Azure App Service
            }
            else
            {
                var serilogLogger = new LoggerConfiguration()

                                    // This loads serilog sink information from configuration
                                    .ReadFrom.Configuration(config)

                                    // This will send serilog diagnostics to AppInsights as traces (correlated with whichever request
                                    // was being processed when the diagnostic was logged). This could also be done via a config file,
                                    // but is done here since it is easier to use an instrumentation key from configuration this way.
                                    // The CustomersMVC project demonstrates how to send logs directly to AppInsights without using Serilog.
                                    .WriteTo.ApplicationInsightsTraces(config["ApplicationInsights:InstrumentationKey"])
                                    .CreateLogger();

                loggingBuilder.AddSerilog(serilogLogger);
            }
        }
    }
}
