﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Password", "Wulf/lukespragg", "2.0.0", ResourceId = 428)]
    [Description("Requires players to enter a password to connect or play on the server")]
    public class Password : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Server password")]
            public string ServerPassword;

            [JsonProperty(PropertyName = "Password attempts")]
            public int PasswordAttempts;

            [JsonProperty(PropertyName = "Grace period (seconds)")]
            public int GracePeriod;

            [JsonProperty(PropertyName = "Password command (true/false)")]
            public bool PasswordCommand;

            [JsonProperty(PropertyName = "Password names (true/false)")]
            public bool PasswordNames;

            [JsonProperty(PropertyName = "Freeze unauthorized (true/false)")]
            public bool FreezeUnauthorized;

            [JsonProperty(PropertyName = "Mute unauthorized (true/false)")]
            public bool MuteUnauthorized;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    ServerPassword = "oxide",
                    PasswordAttempts = 3,
                    GracePeriod = 30,
                    PasswordCommand = true,
                    PasswordNames = false,
                    FreezeUnauthorized = true,
                    MuteUnauthorized = true
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.ServerPassword == null) LoadDefaultConfig();
            }
            catch
            {
                LogWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAlias"] = "pass",
                ["MaximumAttempts"] = "You've exhausted the maximum password attempts ({0})",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotFastEnough"] = "You did not enter a password fast enough ({0} seconds)",
                ["PasswordAccepted"] = "Server password accepted, welcome!",
                ["PasswordChanged"] = "Server password has been changed to: {0}",
                ["PasswordCurrently"] = "Server password is currently set to: {0}",
                ["PasswordInvalid"] = "Server password provided is invalid or none given",
                ["PasswordRequired"] = "Please enter the server password with /password PASSWORD"
            }, this);

            /* TODO: Additional language examples
            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {

            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {

            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {

            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {

            }, this, "es");
            */
        }

        #endregion

        #region Initialization

        private readonly Dictionary<string, Timer> freezeTimers = new Dictionary<string, Timer>();
        private readonly Dictionary<string, int> attempts = new Dictionary<string, int>();
        private readonly HashSet<string> authorized = new HashSet<string>();

        private const string permBypass = "password.bypass";

        private void OnServerInitialized()
        {
            AddCommandAliases("CommandAlias", "PasswordCommand");
            AddCovalenceCommand("password", "PasswordCommand");
            AddCovalenceCommand("server.password", "ServerPasswordCommand");
            permission.RegisterPermission(permBypass, this);

            if (!config.PasswordNames) Unsubscribe("CanUseLogin");
            if (!config.PasswordCommand) Unsubscribe("OnUserConnected");
            if (!config.MuteUnauthorized)
            {
                Unsubscribe("OnBetterChat");
                Unsubscribe("OnUserChat");

            }

            foreach (var player in players.Connected) authorized.Add(player.Id);
        }

        #endregion

        #region Password Names

        private object CanUserLogin(string name, string id, string ip)
        {
            if (permission.UserHasPermission(id, permBypass) || authorized.Contains(id) || name.Contains(config.ServerPassword))
            {
                if (!authorized.Contains(id)) authorized.Add(id);
                return true;
            }

            return Lang("PasswordInvalid", id);
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            if (!authorized.Contains((data["Player"] as IPlayer).Id)) return true;
            return null;
        }


        private object OnUserChat(IPlayer player, string message)
        {
            if (!authorized.Contains(player.Id))
            {
                player.Message(Lang("PasswordRequired", player.Id));
                return true;
            }

            var color = player.IsAdmin ? "#aaff55" : "#55aaff";
            var formatted = covalence.FormatText($"[{color}]{player.Name}[/#]: {message}");

            foreach (var target in players.Connected.Where(t => authorized.Contains(t.Id)))
            {
#if RUST
                target.Command("chat.add", player.Id, formatted, 1.0);
#else
                target.Message(formatted);
#endif
            }
            Log($"[Chat] {player.Name}: {message}");
            return true;
        }

        #endregion

        #region Password Command

        private void OnUserConnected(IPlayer player)
        {
            if (!authorized.Contains(player.Id))
            {
                player.Message(Lang("PasswordRequired", player.Id));

                timer.Once(config.GracePeriod, () => player.Kick(Lang("NotFastEnough", player.Id, config.GracePeriod)));

                if (config.FreezeUnauthorized)
                {
                    var pos = player.Position();
                    freezeTimers[player.Id] = timer.Every(0.01f, () =>
                    {
                        if (!player.IsConnected || authorized.Contains(player.Id)) freezeTimers[player.Id].Destroy();
                        else player.Teleport(pos.X, pos.Y, pos.Z);
                    });
                }
            }
        }
        private void PasswordCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1 || args[0] != config.ServerPassword)
            {
                if (attempts.ContainsKey(player.Id) && attempts[player.Id] + 1 >= config.PasswordAttempts)
                {
                    player.Kick(Lang("MaximumAttempts", player.Id, config.PasswordAttempts));
                    return;
                }

                player.Reply(Lang("PasswordInvalid", player.Id));
                if (attempts.ContainsKey(player.Id)) attempts[player.Id] += 1;
                else attempts.Add(player.Id, 1);
                return;
            }

            authorized.Add(player.Id);
            player.Reply(Lang("PasswordAccepted", player.Id));
        }

        #endregion

        #region Password Setting

        private void ServerPasswordCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(Lang("PasswordCurrently", player.Id, config.ServerPassword));
                return;
            }

            config.ServerPassword = args[0].Sanitize();
            player.Reply(Lang("PasswordChanged", player.Id, config.ServerPassword));
            SaveConfig();
        }

        #endregion

        #region Helpers

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void AddCommandAliases(string key, string command)
        {
            foreach (var language in lang.GetLanguages(this))
            {
                var messages = lang.GetMessages(language, this);
                foreach (var message in messages.Where(m => m.Key.StartsWith(key))) AddCovalenceCommand(message.Value, command);
            }
        }

        #endregion
    }
}
