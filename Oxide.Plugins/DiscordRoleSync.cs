using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;

namespace Oxide.Plugins
{
    [Info("Discord Role Sync", "Tricky", "0.0.2")]
    [Description("Syncs Discord roles with Oxide groups")]

    public class DiscordRoleSync : CovalencePlugin
    {
        #region Declared
        [DiscordClient]
        private DiscordClient Client;
        #endregion

        #region Plugin Reference
        [PluginReference]
        private Plugin DiscordAuth;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;

            [JsonProperty(PropertyName = "Role Setup", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<RoleInfo> RoleSetup = new List<RoleInfo>
            {
                new RoleInfo
                {
                    OxideGroup = "default",
                    DiscordRole = "Member"
                }
            };
        }

        private class RoleInfo
        {
            public string OxideGroup;
            public string DiscordRole;
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

        #region Oxide Hooks
        private void Loaded()
        {
            if(DiscordAuth == null)
                Puts("This Plugin requires Discord Auth, get it at https://umod.org/plugins/discord-auth");

            if (config.BotToken != string.Empty)
                Discord.CreateClient(this, config.BotToken);
        }

        private void Unload()
        {
            Discord.CloseClient(Client);
        }

        private void OnUserGroupAdded(string id, string groupName)
        {
            for (var i = 0; i < config.RoleSetup.Count; i++)
            {
                if (config.RoleSetup[i].OxideGroup == groupName)
                    ManageRole(id, config.RoleSetup[i].DiscordRole);

            }
        }

        private void OnUserGroupRemoved(string id, string groupName)
        {
            for (var i = 0; i < config.RoleSetup.Count; i++)
            {
                if (config.RoleSetup[i].OxideGroup == groupName)
                    ManageRole(id, config.RoleSetup[i].DiscordRole, true);

            }
        }
        #endregion

        #region Role Management
        private void ManageRole(string id, string roleName, bool removeRole = false)
        {
            var discorduserId = GetDiscord(id);
            if (discorduserId == null)
                return;

            var roleId = GetRoleIDByName(roleName);
            if(roleId == null)
            {
                Puts($"Couldn't find {roleName} discord role.");
                return;
            }

            if(!removeRole)
                Client.DiscordServer.AddGuildMemberRole(Client, discorduserId, roleId);
            else
                Client.DiscordServer.RemoveGuildMemberRole(Client, discorduserId, roleId);
        }
        #endregion

        #region Helpers
        private string GetDiscord(string id) => (string)DiscordAuth?.Call("API_GetDiscord", id);

        private string GetRoleIDByName(string roleName)
        {
            foreach (var role in Client.DiscordServer.roles)
            {
                if (role.name == roleName)
                    return role.id;
            }

            return null;
        }
        #endregion
    }
}
