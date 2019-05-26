using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace nhitomi
{
    public static class Program
    {
        static Task Main() =>
            new HostBuilder()
                .ConfigureAppConfiguration(Startup.Configure)
                .ConfigureServices(Startup.ConfigureServices)
                .RunConsoleAsync();
    }
}