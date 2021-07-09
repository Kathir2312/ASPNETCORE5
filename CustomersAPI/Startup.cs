using CustomersAPI.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using RequestCorrelation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Serilog;
namespace CustomersAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CustomersAPI", Version = "v1" });
            });

            services.AddDbContext<EFCustomersDataProvider>(options =>
                options.UseInMemoryDatabase("CustomerDataDb"));

            services.AddScoped<ICustomersDataProvider, EFCustomersDataProvider>();
            services.AddSingleton(new ResourceManager("CustomersAPI.Resources.Controllers.CustomersController",
                typeof(Startup).GetTypeInfo().Assembly));
                
                //new ResourceManager(("CustomersAPI.Resources.Controllers.CustomersController",
                                  //typeof(Startup)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {

            var timingLogger = loggerFactory.CreateLogger("CustomersAPI.Startup.TimingMiddleware");

            app.Use(async (HttpContext context, Func<Task> next) =>
            {
                // Middleware: Logic to run regarding the request prior to invoking more middleware.
                //             This logic should be followed by either a call to next() (to invoke the next
                //             piece of middleware) or to context.Response.WriteAsync/SendFileAsync
                //             without calling next() (which will end the middleware pipeline and begin
                //             travelling back up the middleware stack.
                var timer = new Stopwatch();
                timer.Start();

                // Middleware: Calling the next delegate will invoke the next piece of middleware
                await next();

                // Middleware: Code after 'next' will usually run after another piece of middleware
                //             has written a response, so context.Response should not be written to here.
                timer.Stop();
                timingLogger.LogInformation("Request to {RequestMethod}:{RequestPath} processed in {ElapsedMilliseconds} ms", context.Request.Method, context.Request.Path, timer.ElapsedMilliseconds);
            });

            var supportedCultures = new[]
            {
                    // Localization: Notice that neutral cultures (like 'es') are
                    //               listed after specific cultures. This best practice
                    //               ensures that if a particular culture request could
                    //               be satisifed by either a supported specific culture
                    //               or a supported neutral culture, the specific culture
                    //               will be preferred.
                    new CultureInfo("en-US"),
                    new CultureInfo("fr-FR"),
                    new CultureInfo("es"),
              };

            var requestLocalizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),

                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,

                // UI strings that we have localized.
                SupportedUICultures = supportedCultures


            };


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CustomersAPI v1"));
            }


            app.UseRequestLocalization(requestLocalizationOptions);

            //app.UserSe();
            app.UseMiddleware<RequestCorrelationMiddleware>();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
