namespace Oxide.Plugins
{
    [Info("No Give Notices", "Wulf/lukespragg", "0.1.0", ResourceId = 2336)]
    [Description("Prevents admin item giving notices from showing in the chat")]
    public class NoGiveNotices : RustPlugin
    {
        private object OnServerMessage(string message, string name)
        {
            return message.Contains("gave") && name == "SERVER" ? (object)true : null;
        }
    }
}
