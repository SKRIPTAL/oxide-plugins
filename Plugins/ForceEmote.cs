using System.Collections.Generic;
using Emotes;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("ForceEmote", "Wulf/lukespragg", "0.2.1", ResourceId = 1684)]
    [Description("Force another player to do an emote on command")]
    public class ForceEmote : CovalencePlugin
    {
        #region Initialization
         
        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: {0} <name or id> <emote>",
                ["Emotes"] = "Available emotes: bird, point, surrender, facepalm, handsup, salute, airhump",
                ["ForcedPlayer"] = "You forced {0} to use the {1} emote!",
                ["NoEmote"] = "You must provide an available emote",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["PlayerForced"] = "{0} forced you use the {1} emote!",
                ["PlayerNotFound"] = "Player '{0}' was not found"
            }, this);
        }

        private const string permUse = "forceemote.use";

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }

        #endregion

        #region Command

        [Command("force")]
        private void Command(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permUse))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length == 0)
            {
                player.Reply(Lang("CommandUsage", player.Id, command));
                return;
            }

            if (args[0] == "emotes")
            {
                player.Reply(Lang("Emotes", player.Id));
                return;
            }

            var target = covalence.Players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                player.Reply(Lang("PlayerNotFound", player.Id, args[0]));
                return;
            }

            if (args.Length != 0)
            {
                var emote = args[1];

                if (emote.Length == 0)
                {
                    player.Reply(Lang("NoEmote", player.Id));
                    return;
                }

                ForceEmotes(target, player, emote);
            }
        }

        #endregion

        #region Emotes

        private void ForceEmotes(IPlayer target, IPlayer player, string emote)
        {
            var targetSession = target.Object as PlayerSession;
            var eManager = targetSession?.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
            if (eManager == null) return;

            switch (emote)
            {
                case "airhump":
                    eManager.BeginEmoteServer(EEmoteType.Airhump);
                    break;
                case "bird":
                    eManager.BeginEmoteServer(EEmoteType.Bird);
                    break;
                case "facepalm":
                    eManager.BeginEmoteServer(EEmoteType.Facepalm);
                    break;
                case "handsup":
                    eManager.BeginEmoteServer(EEmoteType.HandsUp);
                    break;
                case "point":
                    eManager.BeginEmoteServer(EEmoteType.Point);
                    break;
                case "salute":
                    eManager.BeginEmoteServer(EEmoteType.Salute);
                    break;
                case "surrender":
                    eManager.BeginEmoteServer(EEmoteType.Surrender);
                    break;
            }

            target.Message(Lang("PlayerForced", target.Id, player.Name.Sanitize(), emote));
            player.Message(Lang("ForcedPlayer", player.Id, target.Name.Sanitize(), emote));
        }

        #endregion

        #region Helpers

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
