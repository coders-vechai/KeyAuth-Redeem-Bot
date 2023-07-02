using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace RedeemBot.Modules; 

public class Redeemer : InteractionModuleBase<SocketInteractionContext>{
    public InteractionService Commands { get; set; }

    private CommandHandler _handler;
    private Config _config;

    public Redeemer(CommandHandler handler)
    {
        _handler = handler;
        _config = ConfigHelper.Get()!;
    }
    
    [SlashCommand("setup", "setup the redeem embed")]
    public async Task Setup(ITextChannel channel)
    {
        if (!HasRole(Context.User)) {
            await RespondAsync("You don't have the `perms` role", ephemeral:true);
            return;
        }

        if (!_config.Servers.Exists(x => x.GuildID == Context.Guild.Id)) {
            await RespondWithModalAsync<AskForSeller>("seller_modal");
            return;
        }
        
        var server = _config.Servers.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
        if(server == null) {
            await RespondAsync("Could not find this server config, please contact server owner", ephemeral:true);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle("Redeem your license here")
            .WithDescription("Click the button below to redeem your license\nMake sure to enable DMs from the server")
            .WithColor(Color.Blue)
            .Build();

        var components = new ComponentBuilder();
        components.WithButton("Redeem", customId:"redeem", ButtonStyle.Primary);
        
        await channel.SendMessageAsync(embed:embed, components:components.Build());
        await RespondAsync("Setup complete", ephemeral: true);
    }

    [SlashCommand("setseller", "set the seller key")]
    public async Task SetSeller([Required] string sellerkey) {
        if(!HasRole(Context.User)) {
            await RespondAsync("You don't have the `perms` role", ephemeral:true);
            return;
        }
        
        if(!ValidKey(sellerkey)) {
            await RespondAsync("Invalid seller key", ephemeral:true);
            return;
        }
        
        var server = _config.Servers.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
        if(server == null) {
            _config.Servers.Add(new ServerConfig {
                GuildID = Context.Guild.Id,
                SellerKey = sellerkey
            });
        }
        else {
            server.SellerKey = sellerkey;
        }

        ConfigHelper.Save(_config);
        _config = ConfigHelper.Get()!;
        await RespondAsync("Successfully set seller key");
    }

    [ComponentInteraction("redeem")]
    public async Task Redeem() {
        await RespondWithModalAsync<RedeemKey>("redeem_modal");
    }

    [ModalInteraction("redeem_modal")]
    public async Task redeemModal(RedeemKey reMod) {
        var serverCfg = _config.Servers.FirstOrDefault(x => x.GuildID == Context.Guild.Id);
        
        if(serverCfg == null) {
            await RespondAsync("Could not find this server config, please contact server owner", ephemeral:true);
            return;
        }

        await RespondAsync("You will receive results in your DMs!", ephemeral:true);
        
        IUserMessage originalMsg;
        try {
            originalMsg = await Context.User.SendMessageAsync("Redeeming your key...");
        } catch { return; }

        var embed = new EmbedBuilder();
        var success = CreateUser(serverCfg.SellerKey, reMod.Key, reMod.Username, reMod.Password, out var message);
        
        embed.Title = success ? "Success" : "Error";
        embed.Color = success ? Color.Green : Color.Red;
        embed.AddField("Response from API", "```" + message + "```");
        embed.WithCurrentTimestamp();
        
        await originalMsg.ModifyAsync(new Action<MessageProperties>(x => {
            x.Content = "";
            x.Embed = embed.Build();
        }));
    }
    
    [ModalInteraction("seller_modal")]
    public async Task SellerModal(AskForSeller sellerMod) {
        if(!ValidKey(sellerMod.SellerKey)) {
            await RespondAsync("Invalid seller key", ephemeral:true);
            return;
        }
        
        _config.Servers.Add(new ServerConfig {
            GuildID = Context.Guild.Id,
            SellerKey = sellerMod.SellerKey
        });
        
        ConfigHelper.Save(_config);
        _config = ConfigHelper.Get()!;
        await RespondAsync("Successfully setup bot, please run the command again to setup the embed");
    }
    
    private bool ValidKey(string key) {
        var response = _client.GetAsync($"https://keyauth.win/api/seller/?sellerkey={key}&type=fetchallkeys&format=txt").Result;
        return response.IsSuccessStatusCode;
    }

    private bool CreateUser(string sellerKey, string key, string username, string password, out string message) {
        var response = _client.GetAsync($"https://keyauth.win/api/seller/?sellerkey={sellerKey}&type=activate&user={username}&key={key}&password={password}").Result;
        dynamic json = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result!)!;
        
        message = json["message"];
        return json["success"];
    }
    
    private bool HasRole(SocketUser user) {
        if (user is SocketGuildUser gUser) {
            var roles = gUser.Roles.ToList();
            return roles.Exists(x => x.Name == "perms");
        }
        return false;
    }

    public class AskForSeller : IModal {
        public string Title => "Seller Key";
        [InputLabel("Seller Key")]
        [ModalTextInput("sellerinput", TextInputStyle.Short, placeholder:"Enter your seller key", maxLength:40)]
        public string SellerKey { get; set; }
    }
    
    public class RedeemKey : IModal {
        public string Title => "Redeem Key";
        [InputLabel("Key")]
        [ModalTextInput("keyinput", TextInputStyle.Short, placeholder:"Enter your key")]
        public string Key { get; set; }
        
        [InputLabel("Username")]
        [ModalTextInput("userinput", TextInputStyle.Short, placeholder:"Enter your username")]
        public string Username { get; set; }
        
        [InputLabel("Password")]
        [ModalTextInput("passinput", TextInputStyle.Short, placeholder:"Enter your password")]
        public string Password { get; set; }
    }
    
    private HttpClient _client = new();
}
