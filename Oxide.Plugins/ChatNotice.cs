using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Chat Notice", "Tricky", "0.0.2")]
    [Description("Plays a sound effect for the receiver on player chat")]

    public class ChatNotice : CovalencePlugin
    {
        #region Plugin References
        [PluginReference]
        private Plugin PrivateMessages, Clans;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Require Permission")]
            public bool RequirePermission = false;

            [JsonProperty(PropertyName = "Sound Prefab")]
            public string SoundPrefab = "assets/bundled/prefabs/fx/invite_notice.prefab";

            [JsonProperty(PropertyName = "Use Cooldown")]
            public bool UseCooldown = true;

            [JsonProperty(PropertyName = "Cooldown (seconds)")]
            public int Cooldown = 60;

            [JsonProperty(PropertyName = "Use PM")]
            public bool PM = true;

            [JsonProperty(PropertyName = "Use Clan Chat")]
            public bool ClanChat = true;

            [JsonProperty(PropertyName = "Use Global Chat")]
            public bool GlobalChat = false;
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

        #region Classes and Stored Data
        private const string perm = "chatnotice.use";
        private Cooldown cooldown = new Cooldown();

        private class Cooldown
        {
            public List<ulong> PM;
            public List<ulong> Clan;
            public List<ulong> Global;

            public Cooldown()
            {
                PM = new List<ulong>();
                Clan = new List<ulong>();
                Global = new List<ulong>();
            }
        }

        private enum ChatType
        {
            PM,
            Clan,
            Global
        }
        #endregion

        #region Oxide Hooks
        private void Init() => permission.RegisterPermission(perm, this);

        private void Loaded()
        {
            if (config.PM)
            {
                if(PrivateMessages == null)
                {
                    PrintWarning("PrivateMessages could not be found! Get it at https://umod.org/plugins/private-messages");
                }
                else
                {
                    if (PrivateMessages.Author != "MisterPixie")
                    {
                        PrintWarning("This version of PrivateMessages isn't supported! Get it at https://umod.org/plugins/private-messages");
                    }
                    else if (PrivateMessages.Version < new VersionNumber(1, 0, 2))
                    {
                        PrintWarning("Only v1.0.2 or above is supported to work with Private Messages");
                    }
                }
            }

            if (config.ClanChat)
            {
                if (Clans == null)
                {
                    PrintWarning("Clans could not be found! Get it at https://umod.org/plugins/clans");
                }
                else
                {
                    if (Clans.Author != "k1lly0u")
                    {
                        PrintWarning("This version of Clans isn't supported! Get it at https://umod.org/plugins/clans");
                    }
                    else if (Clans.Version < new VersionNumber(0, 1, 52))
                    {
                        PrintWarning("Only v0.1.52 or above is supported to work with Clans");
                    }
                }
            }
        }
        #endregion

        #region Chat Hooks
        private void OnPMProcessed(IPlayer player, IPlayer target, string message)
        {
            if (!config.PM)
                return;

            if (config.RequirePermission && !target.HasPermission(perm))
                return;

            RunEffect(target, ChatType.PM);
        }

        private void OnClanChat(IPlayer player, string message)
        {
            if (!config.ClanChat)
                return;

            var clan = GetClanOf(player);
            if (clan == null)
                return;

            var members = (JArray) clan["members"];
            if (members == null)
                return;

            members.ToList().ForEach(x => 
            {
                //var member = BasePlayer.FindByID(Convert.ToUInt64(x));
                var member = players.FindPlayerById(x.ToString());
                if (member == null) return;
                if (config.RequirePermission && !member.HasPermission(perm)) return;

                RunEffect(member, ChatType.Clan);
            });
        }

        private void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (!config.GlobalChat)
                return;

            var player = (BasePlayer) arg.Connection.player;
            if (player == null)
                return;

            var activeplayers = players.Connected.ToList();
            if (activeplayers == null)
                return;

            activeplayers.ForEach(target =>
            {
                if (config.RequirePermission && !target.HasPermission(perm)) return;
                if (target.Id == player.UserIDString) return;

                RunEffect(target, ChatType.Global);
            });
        }
        #endregion

        #region Effect Management
        private void RunEffect(IPlayer iplayer, ChatType type)
        {
            var player = (BasePlayer) iplayer.Object;
            if (player == null)
                return;

            switch (type)
            {
                case ChatType.PM:
                    if (config.UseCooldown && cooldown.PM.Contains(player.userID)) return;
                    cooldown.PM.Add(player.userID);
                    timer.In(config.Cooldown, () => cooldown.PM.Remove(player.userID));
                    break;
                case ChatType.Clan:
                    if (config.UseCooldown && cooldown.Clan.Contains(player.userID)) return;
                    cooldown.Clan.Add(player.userID);
                    timer.In(config.Cooldown, () => cooldown.Clan.Remove(player.userID));
                    break;
                case ChatType.Global:
                    if (config.UseCooldown && cooldown.Global.Contains(player.userID)) return;
                    cooldown.Global.Add(player.userID);
                    timer.In(config.Cooldown, () => cooldown.Global.Remove(player.userID));
                    break;
            }

            Effect.server.Run(config.SoundPrefab, player.transform.position);
        }
        #endregion

        #region Helpers
        private JObject GetClanOf(IPlayer player)
        {
            var tag = (string) Clans?.Call("GetClanOf", player);
            if (tag == null)
                return null;

            var clan = (JObject) Clans?.Call("GetClan", tag);
        
            return clan;
        }
        #endregion
    }
}