using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace RedeemBot; 

public class Config {
    public string Token { get; set; }
    public ulong DebugGuild { get; set; }
    public List<ServerConfig> Servers { get; set; } = new();
}

public class ServerConfig {
    public ulong GuildID { get; set; }
    public string SellerKey { get; set; }
}

public class ConfigHelper {
    
    public static Config? Get() {
        if (!File.Exists("config.json")) return null;
        var content = File.ReadAllText("config.json");
        if (string.IsNullOrEmpty(content)) return null;
        return JsonConvert.DeserializeObject<Config>(content);
    }
    
    public static void Save(Config config) {
        if(config == null) throw new ArgumentNullException(nameof(config));
        File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
    }
    
}