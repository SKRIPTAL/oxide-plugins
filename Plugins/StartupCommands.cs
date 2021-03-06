﻿using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("StartupCommands", "Wulf/lukespragg", "1.0.4", ResourceId = 774)]
    [Description("Automatically runs configured commands on server startup")]

    class StartupCommands : CovalencePlugin
    {
        #region Initialization

        const string permAdmin = "startupcommands.admin";

        List<object> commands;

        protected override void LoadDefaultConfig()
        {
            Config["Commands"] = commands = GetConfig("Commands", new List<object> { "version", "oxide.version" });

            SaveConfig();
        }

        void OnServerInitialized()        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permAdmin, this);

            foreach (var command in commands) server.Command(command.ToString());
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAdded"] = "Command '{0}' was added to the startup command list",
                ["CommandListed"] = "Command '{0}' is already in the startup command list",
                ["CommandNotListed"] = "Command '{0}' is not in the startup command list",
                ["CommandRemoved"] = "Command '{0}' was removed from the startup command list",
                ["CommandUsage"] = "Usage: {0} <add | remove | list> <command>",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["StartupCommands"] = "Startup commands"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAdded"] = "Commande « {0} » a été ajouté à la liste de commande de démarrage",
                ["CommandListed"] = "Commande « {0} » est déjà dans la liste de commande de démarrage",
                ["CommandNotListed"] = "Commande « {0} » n’est pas dans la liste de commande de démarrage",
                ["CommandRemoved"] = "Commande « {0} » a été supprimé de la liste de commande de démarrage",
                ["CommandUsage"] = "Utilisation : {0} <add | remove | list> <commande>",
                ["NotAllowed"] = "Vous n’êtes pas autorisé à utiliser la commande « {0} »",
                ["StartupCommands"] = "Commandes de démarrage"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAdded"] = "Befehl '{0}' wurde auf der Startliste Befehl hinzugefügt",
                ["CommandListed"] = "Befehl '{0}' ist bereits in der Startliste Befehl",
                ["CommandNotListed"] = "Befehl '{0}' ist nicht in der Startliste Befehl",
                ["CommandRemoved"] = "Befehl '{0}' wurde aus der Startliste Befehl entfernt",
                ["CommandUsage"] = "Verbrauch: {0} <add | remove | list> <befehl>",
                ["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["StartupCommands"] = "Startbefehle"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAdded"] = "Команда «{0}» был добавлен в список команд запуска",
                ["CommandListed"] = "Команда «{0}» уже находится в списке команд запуска",
                ["CommandNotListed"] = "Команда «{0}» не включен в список команд запуска",
                ["CommandRemoved"] = "Команда «{0}» был удален из списка команд запуска",
                ["CommandUsage"] = "Использование: {0} <add | remove | list> <команда>",
                ["NotAllowed"] = "Нельзя использовать команду «{0}»",
                ["StartupCommands"] = "При запуске команды"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandAdded"] = "Comando '{0}' se añadió a la lista de comandos de inicio",
                ["CommandListed"] = "Comando '{0}' ya está en la lista de comandos de inicio",
                ["CommandNotListed"] = "Comando '{0}' no está en la lista de comandos de inicio",
                ["CommandRemoved"] = "Comando '{0}' se quitó de la lista de comandos de inicio",
                ["CommandUsage"] = "Uso: {0} <add | remove | list> <comando>",
                ["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["StartupCommands"] = "Comandos de inicio de"
            }, this, "es");
        }

        #endregion

        #region Commands

        [Command("autocmd", "startcmd", "startupcmd")]
        void StartupCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length < 1 || args.Length < 2 && args[0] != "list")
            {
                player.Reply(Lang("CommandUsage", player.Id, command));
                return;
            }

            var argCommand = string.Join(" ", args.Skip(1).Select(v => v.ToString()).ToArray());
            switch (args[0])
            {
                case "add":
                    if (commands.Contains(argCommand))
                    {
                        player.Reply(Lang("CommandListed", player.Id, argCommand));
                        break;
                    }

                    commands.Add(argCommand);
                    Config["Commands"] = commands;
                    SaveConfig();

                    player.Reply(Lang("CommandAdded", player.Id, argCommand));
                    break;

                case "remove":
                    if (!commands.Contains(argCommand))
                    {
                        player.Reply(Lang("CommandNotListed", player.Id, argCommand));
                        break;
                    }

                    commands.Remove(argCommand);
                    Config["Commands"] = commands;
                    SaveConfig();

                    player.Reply(Lang("CommandRemoved", player.Id, argCommand));
                    break;

                case "list":
                    player.Reply(Lang("StartupCommands", player.Id) + ": " + string.Join(", ", commands.Cast<string>().ToArray()));
                    break;

                default:
                    player.Reply(Lang("CommandUsage", player.Id, command));
                    break;
            }
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
