using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Easy Research", "Tricky", "1.0.1")]
    [Description("Adds a few new features to Research System")]

    public class EasyResearch : RustPlugin
    {
        #region Plugin Reference
        [PluginReference]
        private Plugin PopupNotifications;
        #endregion

        #region Config
        ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Blocked Items")]
            public List<string> Blocked { get; set; }

            [JsonProperty(PropertyName = "Block Research if player already unlocked the blueprint")]
            public bool AlreadyUnlocked { get; set; }

            [JsonProperty(PropertyName = "Chat Prefix")]
            public string ChatPrefix { get; set; }

            [JsonProperty(PropertyName = "Use Popup Notifications")]
            public bool Popup { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Blocked = new List<string>
                {
                    "shortname"
                },
                AlreadyUnlocked = true,
                ChatPrefix = "<color=green>Easy Research</color>: ",
                Popup = false
            };
            SaveConfig(config);
        }

        void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AlreadyUnlocked"] = "You already unlocked this blueprint",
                ["BlockedItem"] = "Researching this item is blocked on this server"
            }, this);
        }
        #endregion

        #region Init
        void Init()
        {
            LoadConfigVariables();
        }
        #endregion

        #region ResearchHook
        private object CanResearchItem(BasePlayer player, Item targetItem)
        {
            if (configData.AlreadyUnlocked && player.blueprints.HasUnlocked(targetItem.info))
            {
                string message = lang.GetMessage("AlreadyUnlocked", this, player.UserIDString);

                if (configData.Popup)
                {
                    PopupNotifications?.Call("CreatePopupNotification", message);
                }
                else
                {
                    player.ChatMessage(configData.ChatPrefix + message);
                }
                return true;
            }

            if (configData.Blocked.Contains(targetItem.info.shortname))
            {
                string message = lang.GetMessage("BlockedItem", this, player.UserIDString);

                if (configData.Popup)
                {
                    PopupNotifications?.Call("CreatePopupNotification", message);
                }
                else
                {
                    player.ChatMessage(configData.ChatPrefix + message);
                }
                return true;
            }

            return null;
        }
        #endregion
    }
}
