using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Healer", "Wulf/lukespragg", "2.5.0", ResourceId = 658)]
    [Description("Allows players with permission to heal themselves or others")]
    public class Healer : CovalencePlugin
    {
        #region Initialization

        private readonly Hash<string, float> cooldowns = new Hash<string, float>();

        private const string permAll = "healer.all";
        private const string permOthers = "healer.others";
        private const string permSelf = "healer.self";

        private int cooldown;
        private int maxAmount;

        protected override void LoadDefaultConfig()
        {
            // Settings
            Config["Cooldown (Seconds, 0 to Disable)"] = cooldown = GetConfig("Cooldown (Seconds, 0 to Disable)", 30);
            Config["Maximum Heal Amount (1 - Infinity)"] = maxAmount = GetConfig("Maximum Heal Amount (1 - Infinity)", 100);

            SaveConfig();
        }

        private void Init()
        {
            permission.RegisterPermission(permOthers, this);
            permission.RegisterPermission(permSelf, this);
        }

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: {0} <amount> <name or id> (target optional)",
                ["Cooldown"] = "Wait a bit before attempting to use '{0}' again",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["PlayerNotFound"] = "Player '{0}' was not found",
                ["PlayerWasHealed"] = "{0} was healed {1}",
                ["PlayersHealed"] = "All players have been healed {0}!",
                ["YouWereHealed"] = "You were healed {0}"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Utilisation : {0} <montant> <nom ou id> (objectif en option)",
                ["Cooldown"] = "Attendre un peu avant de tenter de réutiliser « {0} »",
                ["NotAllowed"] = "Vous n’êtes pas autorisé à utiliser la commande « {0} »",
                ["PlayerNotFound"] = "Player « {0} » n’a pas été trouvée",
                ["PlayerWasHealed"] = "{0} a été guéri {1}",
                ["PlayersHealed"] = "Tous les joueurs ont été guéris {0} !",
                ["YouWereHealed"] = "Vous avez été guéri {0}"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Verwendung: {0} <Betrag> <Name oder Id> (Ziel optional)",
                ["Cooldown"] = "Noch ein bisschen warten Sie, bevor Sie '{0}' wieder verwenden",
                ["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["PlayerNotFound"] = "Player '{0}' wurde nicht gefunden",
                ["PlayerWasHealed"] = "{0} wurde geheilt {1}",
                ["PlayersHealed"] = "Alle Spieler sind geheilt worden {0}!",
                ["YouWereHealed"] = "Sie wurden geheilt {0}"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Использование: {0} <сумма> <имя или id> (цель необязательно)",
                ["Cooldown"] = "Подождите немного, прежде чем использовать «{0}» снова",
                ["NotAllowed"] = "Нельзя использовать команду «{0}»",
                ["PlayerNotFound"] = "Игрок «{0}» не найден",
                ["PlayerWasHealed"] = "{0} был исцелен {1}",
                ["PlayersHealed"] = "Все игроки были исцелены {0}!",
                ["YouWereHealed"] = "Вы были зарубцевавшиеся {0}"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Uso: {0} <cantidad> <nombre o id> (destino opcional)",
                ["Cooldown"] = "Esperar un poco antes de intentar volver a utilizar '{0}'",
                ["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["PlayerNotFound"] = "Jugador '{0}' no se encontró",
                ["PlayerWasHealed"] = "{0} es {1} curado",
                ["PlayersHealed"] = "Todos los jugadores han sido sanados {0}!",
                ["YouWereHealed"] = "Fuiste sanado {0}"
            }, this, "es");
        }

        #endregion

        #region Healing

        private void Heal(IPlayer player, float amount)
        {
#if RUST
            var basePlayer = player.Object as BasePlayer;
            basePlayer.metabolism.bleeding.value = 0;
            basePlayer.metabolism.calories.value += amount;
            basePlayer.metabolism.dirtyness.value = 0;
            basePlayer.metabolism.hydration.value += amount;
            basePlayer.metabolism.oxygen.value = 1;
            basePlayer.metabolism.poison.value = 0;
            basePlayer.metabolism.radiation_level.value = 0;
            basePlayer.metabolism.radiation_poison.value = 0;
            basePlayer.metabolism.wetness.value = 0;
            basePlayer.StopWounded();
#endif
            player.Heal(amount);
        }

        #endregion

        #region Heal Command

        [Command("heal")]
        private void HealCommand(IPlayer player, string command, string[] args)
        {
            float amount;
            if (args.Length > 0) float.TryParse(args[0], out amount);
            if (amount > maxAmount || amount.Equals(0)) amount = maxAmount;

            IPlayer target;
            if (args.Length >= 2) target = players.FindPlayer(args[1]); // TODO: Use FindPlayers and and handle multiple players
            else if (args.Length == 1) target = players.FindPlayer(args[0]);
            else target = player;

            if ((Equals(target, player) && !player.HasPermission(permSelf)) || !player.HasPermission(permOthers))
            {
                Reply(player, "NotAllowed", player.Id, command);
                return;
            }

            if (args.Length == 0 && target.Id == "server_console")
            {
                Reply(player, "CommandUsage", player.Id, command);
                return;
            }

            if (target.Id == "server_console" || !target.IsConnected)
            {
                var name = args.Length >= 2 ? args[1] : args.Length == 1 ? args[0] : "";
                Reply(player, "PlayerNotFound", player.Id, name);
                return;
            }

            if (player.Id != "server_console")
            {
                if (!cooldowns.ContainsKey(player.Id)) cooldowns.Add(player.Id, 0f);
                if (cooldown != 0 && cooldowns[player.Id] + cooldown > Interface.Oxide.Now)
                {
                    Reply(player, "Cooldown", player.Id, command);
                    return;
                }
            }

            if (amount > maxAmount || amount.Equals(0)) amount = maxAmount;

            Heal(target, amount);
            cooldowns[player.Id] = Interface.Oxide.Now;
            Reply(target, "YouWereHealed", player.Id, amount); // TODO: Only show amount to max health

            if (!Equals(target, player)) Reply(player, "PlayerWasHealed", player.Id, target.Name.Sanitize(), amount);
        }

        #endregion

        #region Heal All Command

        [Command("healall")]
        private void HealAllCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permOthers))
            {
                Reply(player, "NotAllowed", player.Id, command);
                return;
            }

            if (player.Id != "server_console")
            {
                if (!cooldowns.ContainsKey(player.Id)) cooldowns.Add(player.Id, 0f);
                if (cooldown != 0 && cooldowns[player.Id] + cooldown > Interface.Oxide.Now)
                {
                    Reply(player, "Cooldown", player.Id, command);
                    return;
                }
            }

            float amount;
            if (args.Length > 0) float.TryParse(args[0], out amount);
            if (amount > maxAmount || amount.Equals(0)) amount = maxAmount;

            foreach (var target in players.Connected.Where(t => t.Health < t.MaxHealth))
            {
                Heal(target, amount);
                cooldowns[player.Id] = Interface.Oxide.Now;
                Reply(target, "YouWereHealed", amount); // TODO: Only show amount to max health
            }

            Reply(player, "PlayersHealed", amount);
        }

        #endregion

        #region Helpers

        private T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        private void Reply(IPlayer player, string langKey, params object[] args) => player.Reply(lang.GetMessage(langKey, this, player.Id), args);

        #endregion
    }
}
