/*
TODO:
- Show blocked message via centered GUI overlay
- Move items back to inventory from Research Table
- Add option to remove all deployed Research Tables
- Add option to remove Research Tables in inventory
- Add option to disable crafting of Research Table
- Add permission support to bypass blocks
- Add new hook for when crafting is first started
*/

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NoResearch", "Wulf/lukespragg", "0.1.1", ResourceId = 0)]
    [Description("Blocks item researching completely")]

    class NoResearch : RustPlugin
    {
        void Init()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoResearching"] = "Researching items is not allowed!",
                ["NoResearchTables"] = "Crafting research tables is not allowed!"
            }, this);
        }

        object OnItemResearch(Item item, BasePlayer player)
        {
            Player.Reply(player, lang.GetMessage("NoResearching", this, player.UserIDString));
            return true;
        }
    }
}
