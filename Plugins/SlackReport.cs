// Requires: Slack

using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("SlackReport", "Wulf/lukespragg", "0.1.3", ResourceId = 1954)]
    [Description("Sends reports to Slack via in-game /report command")]

    class SlackReport : CovalencePlugin
    {
        #region Initialization

        [PluginReference]
        Plugin Slack;

        const string permReport = "slackreport.use";

        //string botName;
        string channel;

        protected override void LoadDefaultConfig()
        {
            //Config["BotName"] = botName = GetConfig("BotName", "Report");
            Config["Channel"] = channel = GetConfig("Channel", "");
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permReport, this);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "Sorry, you're not allowed to use 'report'",
                ["ReportFailed"] = "Your report failed to send, please nofity an admin!",
                ["ReportSent"] = "Thank you, your report has been sent!"
            }, this);
        }

        #endregion

        #region Reporting

        [Command("report")]
        void ReportCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permReport))
            {
                player.Reply(Lang("NotAllowed", player.Id));
                return;
            }

            Action<bool> callback = response =>
            {
                if (!player.IsConnected) return;
                player.Reply(response ? Lang("ReportSent", player.Id) : Lang("ReportFailed", player.Id));
            };
            Slack.Call("TicketMessage", string.Join(" ", args), player, channel, callback);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
