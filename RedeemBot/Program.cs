using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RedeemBot;

class Program {
    private readonly IServiceProvider _services;
    private static Config _config;
    
    private readonly DiscordSocketConfig _socketConfig = new() {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
        AlwaysDownloadUsers = true,
    };
    
    public Program()
    {
        _services = new ServiceCollection()
            .AddSingleton(_socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .BuildServiceProvider();
    }
    
    static void Main(string[] args) {
        if(ConfigHelper.Get() == null) {
            SetupConfig();
        }

        _config = ConfigHelper.Get()!;
        new Program().RunAsync()
            .GetAwaiter()
            .GetResult();
    }
    
    public async Task RunAsync()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;

        await _services.GetRequiredService<CommandHandler>()
            .InitializeAsync();

        await client.LoginAsync(TokenType.Bot, _config.Token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private async Task LogAsync(LogMessage message)
        => Console.WriteLine(message.ToString());

    static void SetupConfig() {
        ulong debugGuild = 0;
        string token = "";

        do {
            Console.Write("Enter your bot token: ");
            token = Console.ReadLine()!;
            Console.Write("Enter your debug guild ID: ");
            ulong.TryParse(Console.ReadLine(), out debugGuild);

        }while(string.IsNullOrEmpty(token) || debugGuild == 0);
        
        var config = new Config {
            Token = token,
            DebugGuild = debugGuild
        };
        ConfigHelper.Save(config);
    }
    
    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}