using CustomersMVC.CustomersAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomersMVC
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
            // Localization: This adds the localization service. This service is also required for the ViewLocalization
            //               and DataAnnotationsLocalization. The ResourcePath sets the base location of the resources to
            //               a folder called resources.
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            //services.AddMvc()
            //  .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            // .AddDataAnnotationsLocalization();

            services.AddControllersWithViews();

            // Configuration: Here we add the HomeControllerOptions and set it to the Configurations HomeControllerOptions section
            //                which in this case came from the appsettings.json files. This is a convenient way to pass configuration
            //                options around using dependency injection. It also helps scope just the options that apply to logical
            //                parts of the application to those parts of the application versus passing all configuration to all logical
            //                parts of the application.
            services.Configure<HomeControllerOptions>(Configuration.GetSection("HomeControllerOptions"));

            // Register a ResiliencePoliciesFactory and then use it to create
            // and register the Policy[] to be used
            services.AddTransient<ResiliencePoliciesFactory>();
            services.AddSingleton(sp =>
                sp.GetRequiredService<ResiliencePoliciesFactory>().CreatePolicies());

            // Add the HttpClient used to access other services
            services.AddSingleton(CreateHttpClient());

            // Add the CustomersApiService into the dependency container
            services.AddSingleton<CustomersAPIService>();

            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            //app.UseAuthorization();
            //app.UseMiddleware()
            app.UseRequestCorrelation();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        /// Get's the URL from CustomersAPIService:URL setting in appsettings.json
        /// </summary>

        private string GetCustomersAPIUrl()
        {
            var endpoint = Configuration["CustomersAPIService:Url"];
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException("CustomerAPIService",
                                                "Need to specify CustomerAPIService in appsettings.json");
            }

            return endpoint;
        }

        /// <summary>
        /// Creates an HTTPClient with the appsettings.json Url
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(GetCustomersAPIUrl())
            };

            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AnalyzerStatusCheck");

            return client;
        }
    }
}
