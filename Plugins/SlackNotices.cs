// Requires: Slack

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

/*
 * TODO:
 * Add option to send all server info every X seconds
 */

namespace Oxide.Plugins
{
    [Info("SlackNotices", "Wulf/lukespragg", "0.1.2", ResourceId = 1957)]
    [Description("Sends connection and disconnection notices to Slack channel")]

    class SlackNotices : CovalencePlugin
    {
        #region Initialization

        [PluginReference]
        Plugin Slack;

        bool connections;
        bool disconnections;
        string channel;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Connections"] = connections = GetConfig("Connections", true);
            Config["Disconnections"] = disconnections = GetConfig("Disconnections", true);

            // Settings
            Config["Channel"] = channel = GetConfig("Channel", "");

            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();

            if (!connections) Unsubscribe("OnUserConnected");
            if (!disconnections) Unsubscribe("OnUserDisconnected");
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Connected"] = "*{0}* _({1})_ connected",
                ["Disconnected"] = "*{0}* _({1})_ disconnected, reason: {2}",
                ["Unknown"] = "Unknown"
            }, this);
        }

        #endregion

        #region Game Hooks

        void OnUserConnected(IPlayer player) => Slack.Call("Message", Lang("Connected", null, player.Name.Sanitize(), player.Id), channel);

        void OnUserDisconnected(IPlayer player, string reason)
        {
            reason = reason.Equals("Disconnected") ? Lang("Unknown") : reason;
            Slack.Call("Message", Lang("Disconnected", null, player.Name.Sanitize(), player.Id, reason), channel);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
