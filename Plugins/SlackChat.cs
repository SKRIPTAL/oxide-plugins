// Requires: Slack

using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

/*
 * TODO:
 * Add option to send keywords to separate channel
 */

namespace Oxide.Plugins
{
    [Info("SlackChat", "Wulf/lukespragg", "0.1.2", ResourceId = 1953)]
    [Description("Sends all chat messages or keyword-based chat to Slack channel")]

    class SlackChat : CovalencePlugin
    {
        #region Initialization

        [PluginReference]
        Plugin Slack;

        bool keywordsOnly;

        List<object> keywords;
        string channel;
        string style;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["KeywordsOnly"] = keywordsOnly = GetConfig("KeywordsOnly", false);

            // Settings
            Config["Channel"] = channel = GetConfig("Channel", "general");
            Config["Keywords"] = keywords = GetConfig("Keywords", new List<object> { "admin", "cheat" });
            Config["Style"] = style = GetConfig("Style", "fancy");

            SaveConfig();
        }

        void Init() => LoadDefaultConfig();

        #endregion

        #region Chat Sending

        void OnUserChat(IPlayer player, string message)
        {
            var keyword = keywords.Any(o => message.Contains((string)o));
            if (keywordsOnly && !keyword) return;
            Slack.Call(style.ToLower() == "fancy" ? "FancyMessage" : "SimpleMessage", message, player, channel);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
    }
}
