using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hipster.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddOpenApiDocument(options =>
            {
                options.Title = "Hipster.Api";
            });

            services.AddDbContext<DatabaseContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("Database"))
            );

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors(o =>
            {
                o.WithOrigins("*");
                o.AllowAnyHeader();
                o.AllowAnyMethod();
            });

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
