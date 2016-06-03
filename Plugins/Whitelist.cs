﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Whitelist", "Wulf/lukespragg", "2.0.0", ResourceId = 1932)]
    [Description("Restricts server access to whitelisted players and Steam group members")]

    class Whitelist : CovalencePlugin
    {
        // Do NOT edit this file, instead edit Whitelist.json in oxide/config and Whitelist.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Initialization

        const string permWhitelist = "whitelist.allowed";

        void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permWhitelist, this);
            if (!string.IsNullOrEmpty(steamGroup)) GetGroupMembers();
        }

        #endregion

        #region Configuration

        bool adminExcluded;
        string steamGroup;

        protected override void LoadDefaultConfig()
        {
            Config["AdminExcluded"] = adminExcluded = GetConfig("AdminExcluded", true);
            Config["SteamGroup"] = steamGroup = GetConfig("SteamGroup", "OxideMod");
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string> { { "NotWhitelisted", "You are not whitelisted" } };
            lang.RegisterMessages(messages, this);
        }

        #endregion

        #region Steam Group

        static readonly Regex Regex = new Regex(@"<steamID64>(?<steamid>.+)</steamID64>");
        readonly List<string> members = new List<string>();

        void GetGroupMembers()
        {
            var url = $"http://steamcommunity.com/groups/{steamGroup}/memberslistxml/?xml=1";

            // Get Steam group members
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    Puts("Checking for Steam group members failed! (" + code + ")");
                    Puts("Retrying in 5 seconds...");
                    timer.Once(5f, GetGroupMembers);
                    return;
                }

                foreach (Match match in Regex.Matches(response))
                {
                    // Convert Steam ID to ulong format
                    var id = match.Groups["steamid"].Value;

                    // Check if list contains Steam ID
                    if (members.Contains(id)) continue;

                    // Add Steam ID to list
                    members.Add(id);
                }
            }, this);
        }

        #endregion

        #region Whitelist Check

        object CanUserLogin(string name, string id) => !IsWhitelisted(id) ? Lang("NotWhitelisted", id) : null;

        bool IsWhitelisted(string id) => adminExcluded && IsAdmin(id) || members.Contains(id) || HasPermission(id, permWhitelist);

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        bool IsAdmin(string id) => permission.UserHasGroup(id, "admin");

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}