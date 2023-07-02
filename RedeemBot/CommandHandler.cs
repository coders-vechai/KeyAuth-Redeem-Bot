using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace RedeemBot; 

public class CommandHandler {
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;
    private Config _config;
    
    public CommandHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services) {
        _client = client;
        _handler = handler;
        _services = services;
        _config = ConfigHelper.Get()!;
    }
    
    public async Task InitializeAsync()
    {
        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _handler.Log += LogAsync;

        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions commands
        _client.InteractionCreated += HandleInteraction;
    }
    
    private async Task LogAsync(LogMessage log)
        => Console.WriteLine(log);

    private async Task ReadyAsync()
    {
        if (Program.IsDebug())
            await _handler.RegisterCommandsToGuildAsync(_config.DebugGuild, true);
        else
            await _handler.RegisterCommandsGloballyAsync(true);
    }
    
    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            var result = await _handler.ExecuteCommandAsync(context, _services);
            
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        break;
                }
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
}