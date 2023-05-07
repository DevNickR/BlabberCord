using BlabberCord.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlabberCord
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Load configuration
            IConfiguration configuration = LoadConfiguration();

            // Set up dependency injection
            IServiceCollection services = ConfigureServices(configuration);

            // Add the services and create the service provider
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Initialize and run the Discord service
            var discordService = serviceProvider.GetRequiredService<DiscordService>();
            var discordToken = configuration["Discord:Token"];
            if (string.IsNullOrEmpty(discordToken)) throw new ArgumentNullException("Please set \"Discord:Token\" in appsettings.json or environment variables to continue");

            await discordService.StartAsync(discordToken);

            // Block this task until the program is closed
            await Task.Delay(-1);
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(optional: true)
                ;

            return builder.Build();
        }

        private static IServiceCollection ConfigureServices(IConfiguration configuration)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites
            }))
                .AddSingleton(new CommandService(new CommandServiceConfig { LogLevel = LogSeverity.Debug }))
                .AddSingleton(configuration)
                .AddLogging(builder => builder
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddConsole()
                )
                .AddSingleton<DiscordService>()
                .AddSingleton<GptService>()
                .AddSingleton(new PersonaService("./Personas"))
                ;

            return services;
        }
    }
}
