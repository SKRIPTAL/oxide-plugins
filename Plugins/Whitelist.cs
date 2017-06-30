/*
 * TODO: Add command to toggle whitelist on/off
 * TODO: Add option to only whitelist when no admin are online
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Whitelist", "Wulf/lukespragg", "3.3.0", ResourceId = 1932)]
    [Description("Restricts server access to whitelisted players only")]
    public class Whitelist : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Admin excluded (true/false)")]
            public bool AdminExcluded;

            [JsonProperty(PropertyName = "Reset on restart (true/false)")]
            public bool ResetOnRestart;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    AdminExcluded = true,
                    ResetOnRestart = false
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.AdminExcluded == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
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
                ["AddedToWhitelist"] = "'{0}' has been added to the whitelist",
                //["CommandUsage"] = "Usage: {0} <name or id>",
                //["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotWhitelisted"] = "You are not whitelisted",
                ["PlayerNotFound"] = "No players were found using '{0}'",
                ["PlayersFound"] = "Multiple players were found, please specify: {0}",
                ["RemovedFromWhitelist"] = "'{0}' has been removed from the whitelist",
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Utilisation : {0} <nom ou id> <permission>",
                //["NotAllowed"] = "Vous n’êtes pas autorisé à utiliser la commande « {0} »",
                ["NotWhitelisted"] = "Vous n’êtes pas dans la liste blanche",
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Verbrauch: {0} <name oder id>",
                //["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["NotWhitelisted"] = "Du bist nicht zugelassenen",
                //["PlayerNotFound"] = "Keine Spieler wurden durch '{0}' gefunden",
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Использование: {0} <имя или идентификатор",
                //["NotAllowed"] = "Нельзя использовать команду «{0}»",
                ["NotWhitelisted"] = "Вы не можете",
                //["PlayerNotFound"] = "Игроки не были найдены с помощью '{0}'",
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Uso: {0} <nombre o id> <permiso>",
                //["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["NotWhitelisted"] = "No estás en lista blanca",
                //["PlayerNotFound"] = "No hay jugadores se encontraron con '{0}'",
            }, this, "es");
        }

        #endregion

        #region Initialization

        private const string permAdmin = "whitelist.admin";
        private const string permAllow = "whitelist.allow";

        private bool enabled = true;

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAllow, this);

            AddCommandAliases("CommandAlias", "WhitelistCommand");
            AddCovalenceCommand("whitelist", "WhitelistCommand");

            if (!config.ResetOnRestart)
            {
                foreach (var name in permission.GetGroups())
                    if (permission.GroupHasPermission(name, permAllow)) permission.RevokeGroupPermission(name, permAllow);
                foreach (var id in permission.GetPermissionUsers(permAllow))
                    permission.RevokeUserPermission(Regex.Replace(id, "[^0-9]", ""), permAllow);
            }
        }

        #endregion

        #region Whitelisting

        private bool IsWhitelisted(string id)
        {
            var player = players.FindPlayerById(id);
            return player != null && config.AdminExcluded && player.IsAdmin || permission.UserHasPermission(id, permAllow);
        }

        private object CanUserLogin(string name, string id) => !IsWhitelisted(id) ? Lang("NotWhitelisted", id) : null;

        #endregion

        #region Commands

        private void WhitelistCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin))
            {
                Message(player, "NotAllowed", command);
                return;
            }

            if (args.Length < 2)
            {
                Message(player, "CommandUsage", command);
                return;
            }

            var foundPlayers = players.FindPlayers(args[1]).ToArray();
            if (foundPlayers.Length > 1)
            {
                Message(player, "PlayersFound", string.Join(", ", foundPlayers.Select(p => p.Name).ToArray()));
                return;
            }

            var target = foundPlayers.Length == 1 ? foundPlayers[0] : null;
            if (target == null)
            {
                Message(player, "PlayerNotFound", args[1]);
                return;
            }

            var targetName = $"{target.Name.Sanitize()} ({target.Id})";
            var subCommand = args[0].ToLower();
            switch (subCommand)
            {
                case "+":
                case "add":
                    permission.GrantUserPermission(target.Id, permAllow, this);
                    Message(player, "AddedToWhitelist", targetName);
                    break;

                case "-":
                case "remove":
                    permission.RevokeUserPermission(target.Id, permAllow);
                    Message(player, "RemovedFromWhitelist", targetName);
                    break;

                case "on":
                case "enable":
                    enabled = true;
                    Message(player, "WhitelistEnabled");
                    break;

                case "off":
                case "disable":
                    enabled = false;
                    Message(player, "WhitelistDisabled");
                    break;

                default:
                    Message(player, "UsageHelp");
                    break;
            }
        }

        #endregion

        #region Helpers

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void Message(IPlayer player, string key, params object[] args) => player.Message(lang.GetMessage(key, this, player.Id), args);

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
