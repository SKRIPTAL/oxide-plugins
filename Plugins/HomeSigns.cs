using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("HomeSigns", "Wulf/lukespragg", "0.1.1", ResourceId = 1455)]
    [Description("Allows players to only place signs where they can build")]

    class HomeSigns : RustPlugin
    {
        // TODO: More languages
        void Init() => lang.RegisterMessages(new Dictionary<string, string> { ["NotAllowed"] = "Building blocked!" }, this);

        object CanBuild(Planner plan, Construction prefab)
        {
            var player = plan.GetOwnerPlayer();
            if (!prefab.hierachyName.StartsWith("sign.") || player.HasPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege)) return null;
            Player.Reply(player, lang.GetMessage("NotAllowed", this, player.UserIDString));
            return false;
        }
    }
}
