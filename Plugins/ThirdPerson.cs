using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ThirdPerson", "Wulf/lukespragg", "0.2.0", ResourceId = 1424)]
    [Description("Allows any player with permission to use third-person view")]
    public class ThirdPerson : RustPlugin
    {
        #region Initialization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string> { ["NotAllowed"] = "You are not allowed to use the '{0}' command" }, this);
        }

        private const string permAllow = "thirdperson.toggle";

        private void Init()
        {
            permission.RegisterPermission(permAllow, this);
        }

        #endregion

        #region View Handling

        [ChatCommand("view")] // TODO: Localize
        private void ViewCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permAllow))
            {
                Player.Reply(player, Lang("NotAllowed", player.UserIDString, command));
                return;
            }

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, !player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode));
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (config.OnConnection) player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
        }

        #endregion

        #region Helpers

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
