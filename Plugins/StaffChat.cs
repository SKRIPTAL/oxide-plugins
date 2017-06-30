using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Staff Chat", "Twisted", "1.0.0", ResourceId = 0)]
    [Description("Private chat for staff members and other VIP players")]
    public class StaffChat : CovalencePlugin
    {
        private void Init()
        {
            AddCovalenceCommand(new[] { "admin", "mod", "vip" }, "ChatCommand");
            permission.RegisterPermission("staffchat.admin", this);
            permission.RegisterPermission("staffchat.mod", this);
            permission.RegisterPermission("staffchat.vip", this);
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["PrefixADMIN"] = "[#aaff55][Admin Chat][/#]",
                ["PrefixMOD"] = "[#ffaa55][Mod Chat][/#]",
                ["PrefixVIP"] = "[#aa55ff][VIP Chat][/#]"
            }, this);
        }

        private void ChatCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission($"staffchat.{command}"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            GroupMessage(player, string.Join(" ", args), command);
        }

        private void GroupMessage(IPlayer sender, string message, string command)
        {
            foreach (var target in players.Connected.Where(t => t.IsConnected && t.HasPermission($"staffchat.{command}")))
                target.Message($"{covalence.FormatText(lang.GetMessage($"Prefix{command.ToUpper()}", this, target.Id))} {sender.Name}: {message}");
        }
    }
}
