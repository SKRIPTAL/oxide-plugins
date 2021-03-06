﻿// TODO: Combine messages when multiple supported plugins for one type are in use
// TODO: Improve item stealing chances, very very rare to get an item right now

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Robbery", "Wulf/lukespragg", "4.1.0", ResourceId = 736)]
    [Description("Players can steal money, points, and/or items from other players")]
    public class Robbery : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Allow item stealing (true/false)")]
            public bool ItemStealing;

            [JsonProperty(PropertyName = "Allow money stealing (true/false)")]
            public bool MoneyStealing;

            [JsonProperty(PropertyName = "Allow point stealing (true/false)")]
            public bool PointStealing;

            [JsonProperty(PropertyName = "Clan protection (true/false)")]
            public bool ClanProtection;

            [JsonProperty(PropertyName = "Friend protection (true/false)")]
            public bool FriendProtection;
 
            [JsonProperty(PropertyName = "Maximum chance of stealing an item (0 - 100)")]
            public int MaxChanceItem;

            [JsonProperty(PropertyName = "Maximum chance of stealing money (0 - 100)")]
            public int MaxChanceMoney;

            [JsonProperty(PropertyName = "Maximum chance of stealing points (0 - 100)")]
            public int MaxChancePoints;
 
            [JsonProperty(PropertyName = "Usage cooldown (seconds, 0 to disable)")]
            public int UsageCooldown;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    ItemStealing = true,
                    MoneyStealing = true,
                    PointStealing = true,
                    ClanProtection = false,
                    FriendProtection = false,
                    MaxChanceItem = 25,
                    MaxChanceMoney = 50,
                    MaxChancePoints = 50,
                    UsageCooldown = 30
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.MoneyStealing == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Initialization
 
       [PluginReference]
        private Plugin Clans, Economics, EventManager, Factions, Friends, RustIO, ServerRewards, UEconomics, ZoneManager;

        private readonly Hash<string, float> cooldowns = new Hash<string, float>();
        private static System.Random random = new System.Random();

        private const string permKilling = "robbery.killing";
        private const string permMugging = "robbery.mugging";
        private const string permPickpocket = "robbery.pickpocket";
        private const string permProtection = "robbery.protection";

        private void Init()
        {
            permission.RegisterPermission(permKilling, this);
            permission.RegisterPermission(permMugging, this);
            permission.RegisterPermission(permPickpocket, this);
            permission.RegisterPermission(permProtection, this);
        }

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CanBeSeen"] = "You can't pickpocket right now, you were seen",
                ["CantHoldItem"] = "You can't pickpocket while holding an item",
                ["Cooldown"] = "Wait a bit before attempting to steal again",
                ["IsClanmate"] = "You can't steal from a clanmate",
                ["IsFriend"] = "You can't steal from a friend",
                ["IsProtected"] = "You can't steal from a protected player",
                ["NoLootZone"] = "You can't steal from players in this zone",
                ["StoleItem"] = "You stole {0} {1} from {2}!",
                ["StoleMoney"] = "You stole ${0} from {1}!",
                ["StoleNothing"] = "You stole pocket lint from {0}!",
                ["StolePoints"] = "You stole {0} points from {1}!"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CanBeSeen"] = "Vous ne pouvez pas pickpocket, dès maintenant, vous ont été vus",
                ["CantHoldItem"] = "Vous ne pouvez pas pickpocket tout en maintenant un élément",
                ["Cooldown"] = "Attendre un peu avant de tenter de voler à nouveau",
                ["IsClanmate"] = "Ce joueur est dans votre clan, vous ne pouvez pas lui voler",
                ["IsFriend"] = "Ce joueur est votre ami, vous ne pouvez pas lui voler",
                ["IsProtected"] = "Ce joueur est protecté, vous ne pouvez pas lui voler",
                ["NoLootZone"] = "Vous ne pouvez pas voler dans cette zone",
                ["StoleItem"] = "Vous avez volé {0} {1} de {2}!",
                ["StoleMoney"] = "Vous avez volé €{0} de {1} !",
                ["StoleNothing"] = "Vouz n'avez pas volé rien de {0} !",
                ["StolePoints"] = "Vous avez volé {0} points de {1} !"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CanBeSeen"] = "Sie können nicht Taschendieb schon jetzt, Sie wurden gesehen",
                ["CantHoldItem"] = "Du kannst nicht Taschendieb beim Halten eines Elements",
                ["Cooldown"] = "Noch ein bisschen warten Sie, bevor Sie versuchen, wieder zu stehlen",
                ["IsClanmate"] = "Sie können nicht von einem Clan-Mate stehlen",
                ["IsFriend"] = "Sie können nicht von einem Freund stehlen",
                ["IsProtected"] = "Sie können nicht von einem geschützten Spieler stehlen",
                ["NoLootZone"] = "Sie können von Spielern in dieser Zone nicht stehlen",
                ["StoleItem"] = "Sie hablen {0} {1} von {2} gestohlen!",
                ["StoleMoney"] = "Sie haben €{0} von {1} gestohlen!",
                ["StoleNothing"] = "Sie haben nichts von {0} gestohlen!",
                ["StolePoints"] = "Sie haben {0} Punkte von {1} gestohlen!"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CanBeSeen"] = "Вы не можете карманник прямо сейчас, вы были замечены",
                ["CantHoldItem"] = "Вы не можете карманник удерживая элемент",
                ["Cooldown"] = "Подождите немного, прежде чем снова украсть",
                ["IsClanmate"] = "Вы не можете украсть из клана мат",
                ["IsFriend"] = "Вы не можете украсть от друга",
                ["IsProtected"] = "Вы не можете украсть из защищенного плеера.",
                ["NoLootZone"] = "Вы не можете украсть у игроков в этой зоне",
                ["StoleItem"] = "Вы украли {0} {1} из {2}!",
                ["StoleMoney"] = "Вы украли ₽{0} из {1}!",
                ["StoleNothing"] = "Вы украли ничего от {0}!",
                ["StolePoints"] = "Вы украли {0} точек с {1}!"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CanBeSeen"] = "No se puede robar ahora, fueron vistos",
                ["CantHoldItem"] = "No a carterista manteniendo un elemento",
                ["Cooldown"] = "Esperar un poco antes de intentar robar otra vez",
                ["IsClanmate"] = "Esto jugador está en tu clan, no puedes robarle",
                ["IsFriend"] = "Esto jugador está tu amigo, no puedes robarle",
                ["IsProtected"] = "Esto jugador está protegido, no puedes robarle",
                ["NoLootZone"] = "No puedes robar nadie en esta zona",
                ["StoleItem"] = "Has robado {0} {1} de {2}!",
                ["StoleMoney"] = "Has robado ${0} de {1}!",
                ["StoleNothing"] = "No has robado nada de {0}!",
                ["StolePoints"] = "Has robado {0} puntos de {1}!"
            }, this, "es");
        }

        #endregion

        #region Point Stealing

        private void StealPoints(BasePlayer victim, BasePlayer attacker)
        {
            var chance = random.NextDouble() * (config.MaxChancePoints / 100f);

            // ServerRewards plugin support - http://oxidemod.org/plugins/serverrewards.1751/
            if (ServerRewards != null)
            {
                var balance = ServerRewards.Call("CheckPoints", victim.userID) ?? 0;
                var points = Math.Floor((int)balance * chance);

                if (points > 0)
                {
                    ServerRewards.Call("TakePoints", victim.userID, points);
                    ServerRewards.Call("AddPoints", attacker.userID, points);
                    Player.Reply(attacker, Lang("StolePoints", attacker.UserIDString, points, victim.displayName));
                }
                else
                    Player.Reply(attacker, Lang("StoleNothing", attacker.UserIDString, victim.displayName));
            }
        }

        #endregion

        #region Money Stealing

        private void StealMoney(BasePlayer victim, BasePlayer attacker)
        {
            var chance = random.NextDouble() * (config.MaxChanceMoney / 100f);

            // Economics plugin support - http://oxidemod.org/plugins/economics.717/
            if (Economics != null)
            {
                var balance = (double)Economics.Call("GetPlayerMoney", victim.userID);
                var money = Math.Floor(balance * chance);

                if (money > 0)
                {
                    Economics.Call("Transfer", victim.userID, attacker.userID, money);
                    Player.Reply(attacker, Lang("StoleMoney", attacker.UserIDString, money, victim.displayName));
                }
                else
                    Player.Reply(attacker, Lang("StoleNothing", attacker.UserIDString, victim.displayName));
            }

            // UEconomics plugin support - http://oxidemod.org/plugins/ueconomics.2129/
            if (UEconomics != null)
            {
                var balance = (int)UEconomics.Call("GetPlayerMoney", victim.UserIDString);
                var money = Math.Floor(balance * chance);

                if (money > 0)
                {
                    UEconomics.Call("Withdraw", victim.UserIDString, money);
                    UEconomics.Call("Deposit", attacker.UserIDString, money);
                    Player.Reply(attacker, Lang("StoleMoney", attacker.UserIDString, money, victim.displayName));
                }
                else
                    Player.Reply(attacker, Lang("StoleNothing", attacker.UserIDString, victim.displayName));
            }
        }

        #endregion

        #region Item Stealing

        private void StealItem(BasePlayer victim, BasePlayer attacker)
        {
            var victimInv = victim.inventory.containerMain;
            var attackerInv = attacker.inventory.containerMain;
            if (victimInv == null || attackerInv == null) return;

            var chance = random.NextDouble() * (config.MaxChanceItem / 100f);
            var item = victimInv.GetSlot(random.Next(1, victimInv.capacity));
            if (item != null && !attackerInv.IsFull() && chance > 0)
            {
                item.MoveToContainer(attackerInv);
                Player.Reply(attacker, Lang("StoleItem", attacker.UserIDString, item.amount, item.info.displayName.english, victim.displayName));
            }
            else
                Player.Reply(attacker, Lang("StoleNothing", attacker.UserIDString, victim.displayName));
        }

        #endregion

        #region Zone Checks

        bool InNoLootZone(BasePlayer victim, BasePlayer attacker)
        {
            // Event Manager plugin support - http://oxidemod.org/plugins/event-manager.740/
            if (EventManager != null)
            {
                if (!((bool)EventManager.Call("isPlaying", victim))) return false;
                Player.Reply(attacker, Lang("NoLootZone", attacker.UserIDString));
                return true;
            }

            // Zone Manager plugin support - http://oxidemod.org/plugins/zones-manager.739/
            if (ZoneManager != null)
            {
                var noLooting = Enum.Parse(ZoneManager.GetType().GetNestedType("ZoneFlags"), "NoPlayerLoot", true);
                if (!((bool)ZoneManager.Call("HasPlayerFlag", victim, noLooting))) return false;
                Player.Reply(attacker, Lang("NoLootZone", attacker.UserIDString));
                return true;
            }

            return false;
        }

        #endregion

        #region Friend Checks

        bool IsFriend(BasePlayer victim, BasePlayer attacker)
        {
            // Friends plugin support - http://oxidemod.org/plugins/friends-api.686/
            if (config.FriendProtection && Friends != null)
            {
                // Check if victim is friend of attacker
                if (!((bool)Friends.Call("AreFriends", attacker.userID, victim.userID))) return false;
                Player.Reply(attacker, Lang("IsFriend", attacker.UserIDString));
                return true;
            }

            // Rust:IO plugin support - http://oxidemod.org/extensions/rust-io.768/
            if (config.FriendProtection && RustIO != null)
            {
                // Check if victim is friend of attacker
                if (!((bool)RustIO.Call("HasFriend", attacker.UserIDString, victim.UserIDString))) return false;
                Player.Reply(attacker, Lang("IsFriend", attacker.UserIDString));
                return true;
            }

            return false;
        }

        #endregion

        #region Clan Checks

        private bool IsClanmate(BasePlayer victim, BasePlayer attacker)
        {
            // Clans plugin support - http://oxidemod.org/plugins/rust-io-clans.842/
            if (config.ClanProtection && Clans != null)
            {
                var victimClan = (string)Clans.Call("GetClanOf", victim.UserIDString);
                var attackerClan = (string)Clans.Call("GetClanOf", attacker.UserIDString);
                if (victimClan == null || attackerClan == null || !victimClan.Equals(attackerClan)) return false;
                Player.Reply(attacker, Lang("IsClanmate", attacker.UserIDString));
                return true;
            }

            // Factions plugin support - http://oxidemod.org/plugins/factions.1919/
            if (config.ClanProtection && Factions != null)
            {
                if (!((bool)Factions.Call("CheckSameFaction", attacker.userID, victim.userID))) return false;
                Player.Reply(attacker, Lang("IsClanmate", attacker.UserIDString));
                return true;
            }

            return false;
        }

        #endregion

        #region Killing

        private void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            var victim = entity.ToPlayer();
            var attacker = info?.Initiator.ToPlayer();
            if (victim == null || attacker == null || victim.Equals(attacker)) return;

            if (!permission.UserHasPermission(attacker.UserIDString, permKilling)) return;
            if (permission.UserHasPermission(victim.UserIDString, permProtection)) return;
            if (InNoLootZone(victim, attacker) || IsFriend(victim, attacker) || IsClanmate(victim, attacker)) return;

            if (config.MoneyStealing) StealMoney(victim, attacker);
            if (config.PointStealing) StealPoints(victim, attacker);
        }

        #endregion

        #region Mugging

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            var victim = entity?.ToPlayer();
            var attacker = info?.Initiator?.ToPlayer();
            if (victim == null || attacker == null || victim.Equals(attacker)) return;
            if (info.IsProjectile()) return;

            if (!permission.UserHasPermission(attacker.UserIDString, permMugging)) return;
            if (permission.UserHasPermission(victim.UserIDString, permProtection)) return;
            if (InNoLootZone(victim, attacker) || IsFriend(victim, attacker) || IsClanmate(victim, attacker)) return;

            if (!cooldowns.ContainsKey(attacker.UserIDString)) cooldowns.Add(attacker.UserIDString, 0f);
            if (config.UsageCooldown != 0 && cooldowns[attacker.UserIDString] + config.UsageCooldown > Interface.Oxide.Now)
            {
                Player.Reply(attacker, Lang("Cooldown", attacker.UserIDString));
                return;
            }

            if (config.ItemStealing) StealItem(victim, attacker);
            if (config.MoneyStealing) StealMoney(victim, attacker);
            if (config.PointStealing) StealPoints(victim, attacker);

            cooldowns[attacker.UserIDString] = Interface.Oxide.Now;
        }

        #endregion

        #region Pickpocketing

        private void OnPlayerInput(BasePlayer attacker, InputState input)
        {
            if (!input.WasJustPressed(BUTTON.USE)) return;
            if (!permission.UserHasPermission(attacker.UserIDString, permPickpocket)) return;

            var ray = new Ray(attacker.eyes.position, attacker.eyes.HeadForward());
            var entity = FindObject(ray, 1);
            var victim = entity?.ToPlayer();
            if (victim == null) return;

            if (permission.UserHasPermission(victim.UserIDString, permProtection)) return;
            if (InNoLootZone(victim, attacker) || IsFriend(victim, attacker) || IsClanmate(victim, attacker)) return;

            var victimToAttacker = (attacker.transform.position - victim.transform.position).normalized;
            if (Vector3.Dot(victimToAttacker, victim.eyes.HeadForward().normalized) > 0)
            {
                Player.Reply(attacker, Lang("CanBeSeen", attacker.UserIDString));
                return;
            }

            if (attacker.GetActiveItem()?.GetHeldEntity() != null)
            {
                Player.Reply(attacker, Lang("CantHoldItem", attacker.UserIDString));
                return;
            }

            if (!cooldowns.ContainsKey(attacker.UserIDString)) cooldowns.Add(attacker.UserIDString, 0f);
            if (config.UsageCooldown != 0 && cooldowns[attacker.UserIDString] + config.UsageCooldown > Interface.Oxide.Now)
            {
                Player.Reply(attacker, Lang("Cooldown", attacker.UserIDString));
                return;
            }

            if (config.ItemStealing) StealItem(victim, attacker);
            if (config.MoneyStealing) StealMoney(victim, attacker);
            if (config.PointStealing) StealPoints(victim, attacker);

            cooldowns[attacker.UserIDString] = Interface.Oxide.Now;
        }

        #endregion

        #region Helpers

        private static BaseEntity FindObject(Ray ray, float distance)
        {
            RaycastHit hit;
            return Physics.Raycast(ray, out hit, distance) ? hit.GetEntity() : null;
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
