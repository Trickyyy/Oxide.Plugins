using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Server Chat", "Tricky", "1.1.5")]
    [Description("Replaces the default server chat icon and prefix")]

    public class ServerChat : RustPlugin
    {
        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Chat Icon (SteamID64)")]
            public ulong ChatIcon = 0;

            [JsonProperty(PropertyName = "Title")]
            public string Title = "Server";

            [JsonProperty(PropertyName = "Title Color")]
            public string TitleColor = "white";

            [JsonProperty(PropertyName = "Title Size")]
            public int TitleSize = 15;

            [JsonProperty(PropertyName = "Message Color")]
            public string MessageColor = "white";

            [JsonProperty(PropertyName = "Message Size")]
            public int MessageSize = 15;

            [JsonProperty(PropertyName = "Messages to not modify", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> NoModify = new List<string>
            {
                "gave",
                "restarting"
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region OnServerHook
        private object OnServerMessage(string message)
        {
            string title = $"<color={config.TitleColor}><size={config.TitleSize}>{config.Title}</size></color>";
            string msg = $"<color={config.MessageColor}><size={config.MessageSize}>{message}</size></color>";

            foreach (var text in config.NoModify)
            {
                if (message.Contains(text))
                    return null;
            }

            Server.Broadcast(msg, title, config.ChatIcon);
            return true;
        }
        #endregion
    }
}