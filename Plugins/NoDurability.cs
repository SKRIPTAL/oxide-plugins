namespace Oxide.Plugins
{
    [Info("NoDurability", "Wulf/lukespragg", "2.0.0", ResourceId = 0)]
    [Description("Players with permission have no durability on every item")]

    class NoDurability : CovalencePlugin
    {
        const string permAllow = "nodurability.allow";

        void Init()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            permission.RegisterPermission(permAllow, this);
        }

        #if RUST
        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item?.GetOwnerPlayer();
            if (player == null) return;
            if (permission.UserHasPermission(player.UserIDString, permAllow)) item.condition = item.maxCondition;
            //Puts($"{item.info.shortname} was damaged by: {amount} | Condition is: {item.condition}/{item.maxCondition}");
        }
        #endif
    }
}
