namespace Oxide.Plugins
{
    [Info("WakeUp", "Wulf/lukespragg", "0.1.1", ResourceId = 1487)]
    [Description("Automatically wakes players up from sleep")]
    public class WakeUp : CovalencePlugin
    {
        private object OnPlayerSleep() => true;
    }
}
