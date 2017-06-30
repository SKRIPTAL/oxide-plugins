using System.Collections.Generic;using Oxide.Core;using Oxide.Core.Libraries.Covalence;namespace Oxide.Plugins
{
    [Info("Announcer", "Wulf/lukespragg", "0.2.0", ResourceId = 0)]
    [Description("Broadcasts customizable messages when players join/quit")]

    class Announcer : CovalencePlugin
    {
        #region Initialization

        void Init() => LoadDefaultMessages();#if HURTWORLD
        void OnServerInitialized() => GameManager.Instance.ServerConfig.ChatConnectionMessagesEnabled = false;
#endif        #endregion

        #region Localization
        void LoadDefaultMessages()        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {                ["PlayerJoined"] = "{0} joined the survivors",                ["PlayerQuit"] = "{0} abandoned the survivors"            }, this);        }

        #endregion

        #region Broadcast Messages

        void OnUserConnected(IPlayer player) => server.Broadcast(Lang("PlayerJoined", player.Id, player.Name.Sanitize()));

        void OnUserDisconnected(IPlayer player) => server.Broadcast(Lang("PlayerQuit", player.Id, player.Name.Sanitize()));

#if HURTWORLD
        bool OnConnectionNotice() => true;
        bool OnDisconnectionNotice() => true;
#endif
        #endregion

        #region Helpers

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
