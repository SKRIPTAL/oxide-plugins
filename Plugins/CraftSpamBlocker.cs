// TODO: Fix resources not being given back for multiple items in queue

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("CraftSpamBlocker", "Wulf/lukespragg", "0.3.0", ResourceId = 1805)]
    [Description("Prevents items from being crafted if the player's inventory is full")]

    class CraftSpamBlocker : RustPlugin
    {
        void Init()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["InventoryFull"] = "Item was not crafted, inventory is full!"
            }, this);
        }

        object CanCraft(ItemCraftTask task)
        {
            var player = task.owner;
            var inventory = player.inventory;
            var hasSpace = false;

            if (!inventory.containerMain.IsFull() || !inventory.containerBelt.IsFull()) return null;

            foreach (var item in inventory.containerMain.itemList)
            {
                if (item.info.name != task.blueprint.targetItem.name) continue;
                if (item.amount < item.MaxStackable()) hasSpace = true;
            }

            foreach (var item in inventory.containerBelt.itemList)
            {
                if (item.info.name != task.blueprint.targetItem.name) continue;
                if (item.amount < item.MaxStackable()) hasSpace = true;
            }

            if (hasSpace) return null;

            var crafter = player.inventory.crafting;
            NextTick(() =>
            {
                if (task.amount > 0) crafter.CancelAll(true);
                else crafter.CancelTask(task.taskUID, true);
            });

            foreach (var i in task.takenItems) player.inventory.GiveItem(i);

            SendReply(player, lang.GetMessage("InventoryFull", this, player.UserIDString));
            return true;
        }

        object OnItemCraft(ItemCraftTask task) => CanCraft(task);
        object OnItemCraftFinished(ItemCraftTask task) => CanCraft(task);
    }
}
