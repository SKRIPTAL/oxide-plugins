﻿/*
TODO:
- Finish implementing GUI adjusment commands and player preferences
- Add guiInfo dictionary for storing GUI references for players
- Add ground check instead of using random X vector
- Add command to adjust color and transparency?
*/

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
#if RUST
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries;
using UnityEngine;
#endif

namespace Oxide.Plugins
{
    [Info("Unstuck", "Wulf/lukespragg", "0.1.0", ResourceId = 0)]
    [Description("Enables players to get easily get unstuck from places")]

    class Unstuck : CovalencePlugin
    {
        #region Initialization

        double anchorMinX = 0.3442;
        double anchorMinY = 0.11;
        double anchorMaxX = 0.64;
        double anchorMaxY = 0.15;

        readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();
#if RUST
        readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("Unstuck");
        Dictionary<string, string> playerPrefs = new Dictionary<string, string>();
#endif
        const string permUse = "unstuck.use";

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permUse, this);

#if RUST
            cmdlib.AddConsoleCommand("unstuck.run", this, "UnstuckConsole");
            cmdlib.AddConsoleCommand("unstuck.adjust", this, "UnstuckAdjust");
            playerPrefs = dataFile.ReadObject<Dictionary<string, string>>();

            foreach (var player in BasePlayer.activePlayerList)
            {
                UnstuckButton(player.UserIDString);
                AdjustMenu(player.UserIDString);
            }
#endif
        }

        #endregion

        #region Configuration

        //bool buildCheck;
        bool usePermissions;
        int cooldown;
#if RUST
        bool guiButton;
        string guiColor;
        string guiPosition;
#endif

        protected override void LoadDefaultConfig()
        {
            //Config["BuildCheck"] = buildCheck = GetConfig("BuildCheck", true);
            Config["Cooldown"] = cooldown = GetConfig("Cooldown", 10);
            Config["UsePermissions"] = usePermissions = GetConfig("UsePermissions", true);
#if RUST
            Config["GuiButton"] = guiButton = GetConfig("GuiButton", true);
            Config["GuiColor"] = guiColor = GetConfig("GuiColor", "0.8 0.8 0.8 0.2");
            Config["GuiPosition"] = guiPosition = GetConfig("GuiPosition", "0.2 0.018, 0.3 0.077");
#endif
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Cooldown"] = "Please wait a bit before using '{0}' again",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["Stuck"] = "Stuck?"
            }, this);
        }

        #endregion

        #region Unstuck Command

        [Command("unstuck", "stuck")]
        void UnstuckCommand(IPlayer player, string command, string[] args)
        {
            if (usePermissions && !HasPermission(player.Id, permUse)/* || buildCheck && !player.CanBuild()*/)
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (!cooldowns.ContainsKey(player.Id)) cooldowns.Add(player.Id, 0f);
            if (cooldown != 0 && cooldowns[player.Id] + cooldown > Interface.Oxide.Now)
            {
                player.Reply(Lang("Cooldown", player.Id, command));
                return;
            }

#if RUST
            /*var countdown = cooldown;
            timer.Repeat(1f, cooldown, () =>
            {
                countdown = countdown - 1;
                UnstuckButton(player.Id, countdown.ToString());
                if (countdown == 0) timer.Once(1f, () => UnstuckButton(player.Id));
            });*/
#endif

            var pos = player.Position();
            player.Teleport(pos.X + 2, pos.Y + 2, pos.Z);

#if RUST
            var basePlayer = BasePlayer.Find(player.Id);
            lastPosition[player.Id] = basePlayer.transform.position;
            CuiHelper.DestroyUi(basePlayer, unstuck);
            cooldowns[player.Id] = Interface.Oxide.Now;
#endif
        }

        #endregion

#if RUST
        #region Stuck Check

        readonly Hash<string, Vector3> lastPosition = new Hash<string, Vector3>();
        readonly Dictionary<string, Timer> posTimer = new Dictionary<string, Timer>();

        void StuckCheck(BasePlayer player)
        {
            posTimer.Add(player.UserIDString, timer.Repeat(60f, 0, () =>
            {
                var position = player.transform.position;
                if (lastPosition[player.UserIDString].Equals(position))
                {
                    if (player.inventory.crafting.queue.Count == 0 && !player.IsDead() && !player.IsSleeping())
                    {
                        UnstuckButton(player.UserIDString);
                        return;
                    }
                }

                lastPosition[player.UserIDString] = position;
            }));
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (guiButton)
            {
                lastPosition[player.UserIDString] = player.transform.position;
                StuckCheck(player);
            }
        }

        void OnPlayerDisconnected(BasePlayer player) => ResetPlayer(player.UserIDString);

        void ResetPlayer(string id)
        {
            if (posTimer.ContainsKey(id))
            {
                posTimer[id].Destroy();
                posTimer.Remove(id);
            }
            if (lastPosition.ContainsKey(id)) lastPosition.Remove(id);
        }

        #endregion

        #region Unstuck Button

        string unstuck;

        void UnstuckButton(string id, string text = null)
        {
            var basePlayer = BasePlayer.Find(id);
            var elements = new CuiElementContainer();
            unstuck = elements.Add(new CuiButton
            {
                Button = { Command = $"unstuck.run {id}", Color = guiColor },
                RectTransform =
                {
                    AnchorMin = string.Concat(anchorMinX, ' ', anchorMinY),
                    AnchorMax = string.Concat(anchorMaxX, ' ', anchorMaxY)
                },
                Text = { Text = (text ?? Lang("Stuck", id)), FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, "Hud", "Unstuck");
            CuiHelper.DestroyUi(basePlayer, unstuck);
            CuiHelper.AddUi(basePlayer, elements);
        }

        void UnstuckConsole(ConsoleSystem.Arg arg)
        {
            var player = players.FindPlayer(arg.GetString(0));
            if (player != null) UnstuckCommand(player, "unstuck", null);
        }

        #endregion

        #region Adjustment Menu

        string adjust;

        readonly Dictionary<string, string> guiInfo = new Dictionary<string, string>();

        void AdjustMenu(string id)
        {
            var basePlayer = BasePlayer.Find(id);
            var elements = new CuiElementContainer();
            guiInfo[id] = CuiHelper.GetGuid();

            var panel = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" } }, "Hud", "AdjustPanel");
            adjust = elements.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1.1"
                },
                Text = { Text = "Use the arrows to adjust the position", FontSize = 40, Align = TextAnchor.MiddleCenter }
            }, panel);

            // ↖ - upper left
            // ↗ - upper right
            // ↙ - bottom left
            // ↘ - bottom right

            // Up arrow
            adjust = elements.Add(new CuiButton
            {
                Button = { Command = $"unstuck.adjust up {id}", Color = guiColor },
                RectTransform =
                {
                    AnchorMin = "0.472 0.67",
                    AnchorMax = "0.512 0.72"
                },
                Text = { Text = "↑", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, panel);

            // Down arrow
            adjust = elements.Add(new CuiButton
            {
                Button = { Command = $"unstuck.adjust down {id}", Color = guiColor },
                RectTransform =
                {
                    AnchorMin = "0.472 0.61",
                    AnchorMax = "0.512 0.66"
                },
                Text = { Text = "↓", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, panel);

            // Left arrow
            adjust = elements.Add(new CuiButton
            {
                Button = { Command = $"unstuck.adjust left {id}", Color = guiColor },
                RectTransform =
                {
                    AnchorMin = "0.425 0.61",
                    AnchorMax = "0.465 0.66"
                },
                Text = { Text = "←", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, panel);

            // Right arrow
            adjust = elements.Add(new CuiButton
            {
                Button = { Command = $"unstuck.adjust right {id}", Color = guiColor },
                RectTransform =
                {
                    AnchorMin = "0.519 0.61",
                    AnchorMax = "0.56 0.66"
                },
                Text = { Text = "→", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, panel);

            // Destroy existing UI
            string gui;
            if (guiInfo.TryGetValue(id, out gui)) CuiHelper.DestroyUi(basePlayer, gui);

            CuiHelper.DestroyUi(basePlayer, "AdjustPanel");
            CuiHelper.DestroyUi(basePlayer, adjust);
            CuiHelper.AddUi(basePlayer, elements);
        }

        void UnstuckAdjust(ConsoleSystem.Arg arg)
        {
            var action = arg.Args[0];
            var id = arg.Args[1];

            if (action == "up") { anchorMinY += 0.005; anchorMaxY += 0.005; }
            if (action == "down") { anchorMinY -= 0.005; anchorMaxY -= 0.005; }
            if (action == "left") { anchorMinX -= 0.005; anchorMaxX -= 0.005; }
            if (action == "right") { anchorMinX += 0.005; anchorMaxX += 0.005; }
            UnstuckButton(id);

            if (action == "save")
            {
                if (!playerPrefs.ContainsKey(id)) playerPrefs.Add(id, string.Empty);
                playerPrefs[id] = string.Concat(anchorMinX, ' ', anchorMinY, ' ', anchorMaxX, ' ', anchorMaxY);
                dataFile.WriteObject(playerPrefs);
            }
        }

        #endregion

        #region Cleanup

        void OnUserPermissonGranted(string id, string perm)
        {
            if (guiButton && perm == permUse) UnstuckButton(id);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                // Destroy existing UI
                string gui;
                if (guiInfo.TryGetValue(player.UserIDString, out gui)) CuiHelper.DestroyUi(player, gui);

                CuiHelper.DestroyUi(player, "AdjustPanel");
                CuiHelper.DestroyUi(player, adjust);
                CuiHelper.DestroyUi(player, unstuck);
            }
        }

        #endregion
#endif

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        #endregion
    }
}
