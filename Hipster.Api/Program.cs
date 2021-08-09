using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Hipster.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseHeroku()
                        .UseSentry(o =>
                        {
                            o.TracesSampleRate = 1;
                        });
                });
    }
}
