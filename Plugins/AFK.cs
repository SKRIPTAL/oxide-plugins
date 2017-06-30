﻿using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("AFK", "Wulf/lukespragg", "1.1.6", ResourceId = 1922)]
    [Description("Kicks players that are AFK (away from keyboard) for too long")]

    class AFK : CovalencePlugin
    {
        #region Initialization

        const string permExcluded = "afk.excluded";

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permExcluded, this);
        }

        void OnServerInitialized()
        {
            foreach (var player in players.Connected) AfkCheck(player);
        }

        #endregion

        #region Configuration

        int afkLimitMinutes;
        bool kickAfkPlayers;
        //bool warnBeforeKick;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["KickAfkPlayers"] = kickAfkPlayers = GetConfig("KickAfkPlayers", true);
            //Config["WarnBeforeKick"] = warnBeforeKick = GetConfig("WarnBeforeKick", true);

            // Settings
            Config["AfkLimitMinutes"] = afkLimitMinutes = GetConfig("AfkLimitMinutes", 10);

            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["KickedForAfk"] = "You were kicked for being AFK for {0} minutes",
                //["NoLongerAfk"] = "You are no longer AFK",
                //["YouWentAfk"] = "You went AFK"
            }, this);
        }

        #endregion

        #region AFK Checking

        void OnUserConnected(IPlayer player) => AfkCheck(player);

        readonly Hash<string, GenericPosition> lastPosition = new Hash<string, GenericPosition>();
        readonly Dictionary<string, Timer> afkTimer = new Dictionary<string, Timer>();

        void AfkCheck(IPlayer player)
        {
            if (player.HasPermission(permExcluded)) return;

            ResetPlayer(player.Id);
            lastPosition[player.Id] = player.Position();

            afkTimer.Add(player.Id, timer.Every(afkLimitMinutes * 60, () =>
            {
                if (!IsPlayerAfk(player)) return;

                //player.Message(Lang("YouWentAfk", player.Id));

                if (kickAfkPlayers)
                {
                    // TODO: Send timed message/warning to player before kick

                    player.Kick(Lang("KickedForAfk", player.Id, afkLimitMinutes));
                }
            }));
        }

        bool IsPlayerAfk(IPlayer player)
        {
            if (!player.IsConnected) return false;

            var last = lastPosition[player.Id];
            var current = player.Position();
#if DEBUG
            PrintWarning($"Last position: {last}");
            PrintWarning($"Current position: {current}");
            PrintWarning($"Positions equal: {last.X.Equals(current.X)}");
#endif
            if (last.X.Equals(current.X)) return true;
            lastPosition[player.Id] = current;

            return false;
        }

        void OnUserDisconnected(IPlayer player) => ResetPlayer(player.Id);

        void ResetPlayer(string id)
        {
            if (afkTimer.ContainsKey(id))
            {
                afkTimer[id].Destroy();
                afkTimer.Remove(id);
            }
            if (lastPosition.ContainsKey(id)) lastPosition.Remove(id);
        }

        void Unload()
        {
            foreach (var player in players.Connected) ResetPlayer(player.Id);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
