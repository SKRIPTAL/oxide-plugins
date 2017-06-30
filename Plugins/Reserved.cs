/*
 * TODO:
 * Add command to see who has reserved.slot permission
 * Add option to make reserved slots be used instead of player slots when player slots available
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Reserved", "Wulf/lukespragg", "1.1.0", ResourceId = 0)]
    [Description("Allows players with permission to always be able to connect")]

    class Reserved : CovalencePlugin
    {
        #region Initialization

        const string permSlot = "reserved.slot";
        bool autoAdminSlots;
        bool dynamicSlots;
        bool ignorePlayerLimit;
        bool kickHighPing;
        bool kickForReserved;
        int reservedSlots;

        protected override void LoadDefaultConfig()
        {
            Config["AutoAdminSlots"] = autoAdminSlots = GetConfig("AutoAdminSlots", true);
            Config["DynamicSlots"] = dynamicSlots = GetConfig("DynamicSlots", false);
            Config["IgnorePlayerLimit"] = ignorePlayerLimit = GetConfig("IgnorePlayerLimit", false);
            Config["KickHighPing"] = kickHighPing = GetConfig("KickHighPing", true);
            Config["KickForReserved"] = kickForReserved = GetConfig("KickForReserved", false);
            Config["ReservedSlots"] = reservedSlots = GetConfig("ReservedSlots", 5);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permSlot, this);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["KickedForReserved"] = "Kicked for reserved slot",
                ["ReservedSlotsOnly"] = "Only reserved slots available",
                ["SlotsNowAvailable"] = "{0} slot(s) now available"
            }, this);
        }

        #endregion

        #region

        void OnServerInitialized()
        {
            if (!dynamicSlots) return;

            var slotCount = players.All.Count(player => player.HasPermission(permSlot));
            Config["ReservedSlots"] = slotCount;
            SaveConfig();

            Puts(Lang("SlotsNowAvailable").Replace("{total}", slotCount.ToString()));
        }

        #endregion

        #region Reserved Check

        string CheckForSlots(int currentPlayers, int maxPlayers, string id)
        {
            if ((currentPlayers + reservedSlots) >= maxPlayers && !permission.UserHasPermission(id, "reserved.slot"))
                return Lang("ReservedSlotsOnly", id);

            //if (ignorePlayerLimit) return null;
            /*if (currentPlayers >= maxPlayers)
            {
                // TODO: Kick random player with no reserved slot
                var targets = players.Online.ToArray();
                var target = players[(new Random()).Next(0, targets.Length)];
                if (!target.HasPermission("reserved.slot")) target.Kick(Lang("KickedForReserved", target.Id));

                return null;
            }*/

            return null;
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
}
