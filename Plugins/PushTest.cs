// Requires: PushAPI

using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PushTest", "Wulf/lukespragg", 0.1)]
    [Description("Push API test plugin")]
    public class PushTest : CovalencePlugin
    {
        [PluginReference]
        private Plugin PushAPI;

        [Command("push.test")]
        private void TestCommand(IPlayer player, string command, string[] args)
        {
            Action<bool> callback = response =>
            {
                if (!player.IsConnected) return;
                player.Reply(response ? "Push test was a success!" : "Push test failed :(");
            };

            PushAPI.Call("PushMessage", "This is a test push", "This is a test of the Push API!", "high", callback);
        }
    }
}
