/*
TODO:
- Add option to hide as a deployed entity of choice
- Add command cooldown option
- Add daily limit option
- Add option to now show other vanished for mods?
- Add option to vanish on connection if have permission
- Add option to vanish when sleeping if have permission
- Add AppearWhileRunning option (player.IsRunning())
- Add AppearWhenDamaged option (player.IsWounded())
- Add options for where to position status indicator
- Add restoring after reconnection (datafile/static dictionary)
- Fix 'CanUseWeapon' only visually hiding item; find a better way
- Fix CUI overlay overlapping HUD elements/inventory (if possible)
- Fix player becoming visible when switching weapons? (need to verify)
- Prevent animals/NPCs from detecting player while vanished
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Rust;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vanish", "Wulf/lukespragg", "0.4.0", ResourceId = 1420)]
    [Description("Allows players with permission to become truly invisible")]
    public class Vanish : CovalencePlugin
    {
        #region Initialization

        private const string effectPrefab = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab"; // TODO: Config option

        private const string permAlwaysHidden = "vanish.alwayshidden";
        private const string permDamageBuilds = "vanish.damagebuilds";
        private const string permInvulnerable = "vanish.invulnerable";
        private const string permHurtAnimals = "vanish.attackanimals";
        private const string permHurtPlayers = "vanish.attackplayers";
        private const string permTeleport = "vanish.teleport";
        //const string permWeapons = "vanish.abilities.weapons";
        private const string permUse = "vanish.use";

        private void Init()
        {
            permission.RegisterPermission(permUse, this);

            Unsubscribe(nameof(CanBeTargeted));
            Unsubscribe(nameof(CanNetworkTo));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerSleepEnded));
        }

        #endregion

        #region Configuration

        private bool showEffect;
        private bool showIndicator;
        private bool showOverlay;
        private bool visibleAdmin;
        //private bool visibleHurt;
        //private bool visibleRunning;
        private float vanishTimeout;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Show Effect / Sound (true/false)"] = showEffect = GetConfig("Show Effect / Sound (true/false)", true);
            Config["Show Visual Indicator (true/false)"] = showIndicator = GetConfig("Show Visual Indicator (true/false)", true);
            Config["Show Visual Overlay (true/false)"] = showOverlay = GetConfig("Show Visual Overlay (true/false)", false);
            Config["Visible to Admin (true/false)"] = visibleAdmin = GetConfig("Visible to Admin (true/false)", true);
            //Config["Visible When Hurt (true/false)"] = visibleHurt = GetConfig("Visible When Hurt (true/false)", false);
            //Config["Visible While Running (true/false)"] = visibleRunning = GetConfig("Visible While Running (true/false)", false);

            // Settings
            Config["Vanish Timeout (Seconds, 0 to Disable)"] = vanishTimeout = GetConfig("Vanish Timeout (Seconds, 0 to Disable)", 0f);
        }

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CantDamageBuilds"] = "You can't damage buildings while vanished",
                ["CantHurtAnimals"] = "You can't hurt animals while vanished",
                ["CantHurtPlayers"] = "You can't hurt players while vanished",
                ["CantUseTeleport"] = "You can't teleport while vanished",
                ["NoPermission"] = "Sorry, you can't use 'vanish' right now",
                ["VanishDisabled"] = "You are no longer invisible!",
                ["VanishEnabled"] = "You have vanished from sight...",
                ["VanishTimedOut"] = "Vanish timeout reached!"
            }, this);
        }

        #endregion

        #region Data Storage

        private class OnlinePlayer
        {
            public BasePlayer Player;
            public bool IsInvisible;
        }

        [OnlinePlayers]
        private Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        #endregion

        #region Commands

        [Command("vanish")]
        private void VanishCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permUse))
            {
                player.Reply(Lang("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null)
            {
                // TODO: Only for players message
                return;
            }

            if (showEffect) Effect.server.Run(effectPrefab, basePlayer.transform.position);
            if (IsInvisible(basePlayer))
                Reappear(basePlayer);
            else
                Disappear(basePlayer);
        }

        #endregion

        #region Vanishing Act

        private void Disappear(BasePlayer player)
        {
            var connections = new List<Connection>();
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                if (player == basePlayer || !basePlayer.IsConnected) continue;
                if (visibleAdmin && IsAdmin(basePlayer)) continue;
                connections.Add(basePlayer.net.connection);
            }

            if (Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                Net.sv.write.EntityID(player.net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(connections));
            }

            var item = player.GetActiveItem();
            if (item?.GetHeldEntity() != null && Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                Net.sv.write.EntityID(item.GetHeldEntity().net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(connections));
            }

            var held = player.GetHeldEntity();
            if (item?.GetHeldEntity() != null && Net.sv.write.Start())
            {
                Net.sv.write.PacketID(Network.Message.Type.EntityDestroy);
                Net.sv.write.EntityID(held.net.ID);
                Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                Net.sv.write.Send(new SendInfo(connections));
            }

            if (showOverlay || showIndicator) VanishGui(player);

            if (vanishTimeout > 0f) timer.Once(vanishTimeout, () =>
            {
                if (!onlinePlayers[player].IsInvisible) return;

                PrintToChat(player, Lang("VanishTimedOut", player.UserIDString));
                Reappear(player);
            });

            PrintToChat(player, Lang("VanishEnabled", player.UserIDString));
            onlinePlayers[player].IsInvisible = true;

            Subscribe(nameof(CanBeTargeted));
            Subscribe(nameof(CanNetworkTo));
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnPlayerSleepEnded));
        }

        private object CanBeTargeted(BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (player != null && IsInvisible(player)) return false;

            return null;
        }

        private object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            var player = entity as BasePlayer ?? (entity as HeldEntity)?.GetOwnerPlayer();
            if (player == null || target == null || player == target) return null;
            if (visibleAdmin && IsAdmin(target)) return null;
            if (IsInvisible(player)) return false;

            return null;
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (IsInvisible(player)) Disappear(player);
            // TODO: Notify if still vanished
        }

        #endregion

        #region Reappearing Act

        private void Reappear(BasePlayer player)
        {
            onlinePlayers[player].IsInvisible = false;
            player.SendNetworkUpdate();
            player.GetActiveItem()?.GetHeldEntity()?.SendNetworkUpdate();

            string gui;
            if (guiInfo.TryGetValue(player.userID, out gui)) CuiHelper.DestroyUi(player, gui);

            PrintToChat(player, Lang("VanishDisabled", player.UserIDString));
            if (onlinePlayers.Values.Count(p => p.IsInvisible) <= 0) Unsubscribe(nameof(CanNetworkTo));
        }

        #endregion

        #region GUI Indicator/Overlay

        private Dictionary<ulong, string> guiInfo = new Dictionary<ulong, string>();

        private void VanishGui(BasePlayer player)
        {
            string gui;
            if (guiInfo.TryGetValue(player.userID, out gui)) CuiHelper.DestroyUi(player, gui);

            var elements = new CuiElementContainer();
            guiInfo[player.userID] = CuiHelper.GetGuid();

            if (showIndicator)
            {
                elements.Add(new CuiElement
                {
                    Name = guiInfo[player.userID],
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 0.3", Url = "http://i.imgur.com/Gr5G3YI.png" }, // TODO: Add config options
                        new CuiRectTransformComponent { AnchorMin = "0.175 0.017",  AnchorMax = "0.22 0.08" } // TODO: Add config options
                    }
                });
            }

            if (showOverlay)
            {
                elements.Add(new CuiElement
                {
                    Name = guiInfo[player.userID],
                    Components =
                    {
                        new CuiRawImageComponent { Sprite = "assets/content/ui/overlay_freezing.png" }, // TODO: Add config options
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } // TODO: Add config options
                    }
                });
            }

            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Damage Blocking

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var player = (info?.Initiator as BasePlayer) ?? entity as BasePlayer;
            if (player == null || !player.IsConnected || !onlinePlayers[player].IsInvisible) return null;

            // Block damage to animals
            if (entity is BaseNpc)
            {
                if (!HasPerm(player.UserIDString, permHurtAnimals)) return null;

                Player.Message(player, Lang("CantHurtAnimals", player.UserIDString));
                return true;
            }

            // Block damage to builds
            if (!(entity is BasePlayer))
            {
                if (!HasPerm(player.UserIDString, permDamageBuilds)) return null;

                Player.Message(player, Lang("CantDamageBuilds", player.UserIDString));
                return true;
            }

            // Block damage to players
            if (info?.Initiator is BasePlayer)
            {
                if (!HasPerm(player.UserIDString, permHurtPlayers)) return null;

                Player.Message(player, Lang("CantHurtPlayers", player.UserIDString));
                return true;
            }

            // Block damage to self
            if (HasPerm(player.UserIDString, permInvulnerable))
            {
                info.damageTypes = new DamageTypeList();
                info.HitMaterial = 0;
                info.PointStart = Vector3.zero;
                return true;
            }

            return null;
        }

        #endregion

        #region Weapon Blocking

        /*private void OnPlayerTick(BasePlayer player)
        {
            if (onlinePlayers[player].IsInvisible && !CanUseWeapons && player.GetActiveItem() != null)
            {
                var heldEntity = player.GetActiveItem().GetHeldEntity() as HeldEntity;
                heldEntity?.SetHeld(false);
            }
        }*/

        #endregion

        #region Teleport Blocking

        private object CanTeleport(BasePlayer player)
        {
            if (onlinePlayers[player] == null) return null;
            return onlinePlayers[player].IsInvisible && !HasPerm(player.UserIDString, permTeleport) ? Lang("CantUseTeleport", player.UserIDString) : null;
        }

        #endregion

        #region Cleanup

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                string gui;
                if (guiInfo.TryGetValue(player.userID, out gui)) CuiHelper.DestroyUi(player, gui);
            }
        }

        #endregion

        #region Helpers

        private T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));

        private static void Reply(IPlayer player, string message, params object[] args) => player.Reply(string.Format(message, args));

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private bool IsAdmin(BasePlayer player) => permission.UserHasGroup(player.UserIDString, "admin") || player.net?.connection?.authLevel > 0;

        private bool IsInvisible(BasePlayer player) => onlinePlayers[player]?.IsInvisible ?? false;

        #endregion
    }
}
