/*
 * TODO: Add optional GUI icons to indicate what is enabled/disabled
 * TODO: Add option for PvP during airdrop or heli (or both) event only
 * TODO: Add option to allow player vs. player damage during safe time
 * TODO: Add option to only allow damage for offline players
 * TODO: Add option to only allow purge on weekends/weekdays, select days
 * TODO: Add option to protect player (not building or loot) in own cupboard radius
 * TODO: Add support for Clans, Friends, and other sharing plugins
 * TODO: Finish implementing pre-purge warning countdown
 */

using System;
using System.Collections.Generic;
using Facepunch;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Purge", "Wulf/lukespragg", "1.2.3", ResourceId = 1102)]
    [Description("Allows damage and killing only between specific in-game hours")]
    public class Purge : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class PurgePeriod
        {
            public string Begin;
            public string End;
        }

        public class PurgeRule
        {
            public bool AnimalDamage;
            public bool BarricadeDamage;
            public bool HeliDamage;
            public bool LootDamage;
            public bool SelfDamage;
            public bool StructureDamage;
            public bool TrapDamage;
            public bool TurretDamage;
            public bool WorldDamage;
        }

        /*protected override void LoadDefaultConfig()
        {
            // Settings
            TimeSpan.TryParse(GetConfig("Purge Time (24-hour format)", "Begin (00:00:00)", "18:00:00"), out purgeBegin);
            TimeSpan.TryParse(GetConfig("Purge Time (24-hour format)", "End (00:00:00)", "06:00:00"), out purgeEnd);
            //warningPeriod = GetConfig("Warning Seconds (0 - 60)", 10);
        }*/

        public class Configuration
        {
            [JsonProperty(PropertyName = "Real-time (true/false)")]
            public bool RealTime;

            [JsonProperty(PropertyName = "Purge rules")]
            public PurgeRule PurgeRules;

            [JsonProperty(PropertyName = "Safe rules")]
            public PurgeRule SafeRules;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    PurgeRules = new PurgeRule
                    {
                        AnimalDamage = true,
                        BarricadeDamage = true,
                        HeliDamage = true,
                        LootDamage = true,
                        SelfDamage = true,
                        StructureDamage = true,
                        TrapDamage = true,
                        TurretDamage = true,
                        WorldDamage = true
                    },
                    SafeRules = new PurgeRule
                    {
                        AnimalDamage = false,
                        BarricadeDamage = false,
                        HeliDamage = false,
                        LootDamage = false,
                        SelfDamage = false,
                        StructureDamage = false,
                        TrapDamage = false,
                        TurretDamage = false,
                        WorldDamage = false
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.RealTime == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurgeEnded"] = "Purge has ended! PvP disabled",
                ["PurgeStarted"] = "Purge has begun! PvP enabled"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurgeEnded"] = "Purge est terminée ! PvP désactivé",
                ["PurgeStarted"] = "Purge a commencé ! PvP activé"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurgeEnded"] = "Säuberung ist beendet! PvP deaktiviert",
                ["PurgeStarted"] = "Purge hat begonnen! PvP aktiviert"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurgeEnded"] = "Продувка закончился! PvP отключен",
                ["PurgeStarted"] = "Продувка началась! PvP включен"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PurgeEnded"] = "¡Ha terminado la purga! PvP desactivado",
                ["PurgeStarted"] = "¡Ha comenzado la purga! PvP activado"
            }, this, "es");
        }

        #endregion

        #region Initialization

        private const string permAllow = "purge.allow";
        private const string permProtect = "purge.protect";

        private void Init()
        {
            permission.RegisterPermission(permAllow, this);
            permission.RegisterPermission(permProtect, this);
        }

        #endregion

        #region Purging

        /*bool PurgeWarning
        {
            get
            {
                var totalSeconds = purgeBegin.Subtract(server.Time.TimeOfDay).TotalSeconds;
                return totalSeconds > 0 && totalSeconds <= (warningPeriod * ConVar.Server.tickrate);
            }
        }*/

        bool PurgeTime
        {
            get
            {
                var time = realTime ? DateTime.Now.TimeOfDay : server.Time.TimeOfDay;
                return purgeBegin < purgeEnd ? time >= purgeBegin && time < purgeEnd : time >= purgeBegin || time < purgeEnd;
            }
        }

        void OnTick()
        {
            /*if (!purgeActive && PurgeWarning && !warningStarted)
            {
                warningStarted = true;
                var countdown = warningPeriod - 1;
                timer.Repeat((warningPeriod * ConVar.Server.tickrate) / 60, warningPeriod, () =>
                {
                    PrintWarning(countdown.ToString());
                    if (countdown == 0) warningStarted = false;
                    Puts($"Purge commencing in {countdown}...");
                    countdown--;
                });
                return;
            }*/

            if (PurgeTime && !purgeActive)
            {
                Puts(Lang("PurgeStarted"));
                Broadcast("PurgeStarted");
                purgeActive = true;
            }
            else if (!PurgeTime && purgeActive)
            {
                Puts(Lang("PurgeEnded"));
                Broadcast("PurgeEnded");
                purgeActive = false;
            }
        }

        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            var target = entity.PrefabName;
            var attacker = info.Initiator;

            if (purgeActive)
            {
                var player = entity.ToPlayer();
                if (player != null && !permission.UserHasPermission(player.UserIDString, permProtect)) return;

                if (purgeAnimal && (target.Contains("animals") || target.Contains("corpse") || (attacker != null && attacker.name.Contains("animals")))) return;
                if (purgeHeli && (entity is BaseHelicopter || attacker is BaseHelicopter)) return;
                if (purgeLoot && (target.Contains("loot") || target.Contains("box") || target.Contains("barrel"))) return;
                if (purgeMonumentBarricade && target.Contains("door barricades")) return;
                if (purgeStructure && attacker != null && (entity is Barricade || entity is BuildingBlock || entity is Door || entity is SimpleBuildingBlock)) return;
                if (purgeTrap && entity is BasePlayer && (attacker is Barricade || attacker is BaseTrap || attacker.PrefabName.Contains("wall.external.high"))) return;
                if (purgeTurret && (entity is AutoTurret || attacker is AutoTurret || entity is FlameTurret || attacker is FlameTurret)) return;
                if (purgeWorld && (entity is BasePlayer && attacker == null)) return;
            }
            else
            {
                var owner = attacker?.ToPlayer();
                if (owner != null && entity.OwnerID == owner.userID) return;

                if (safeAnimal && (target.Contains("animals") || target.Contains("corpse") || (attacker != null && attacker.name.Contains("animals")))) return;
                if (safeHeli && (entity is BaseHelicopter || attacker is BaseHelicopter)) return;
                if (safeLoot && (target.Contains("loot") || target.Contains("box") || target.Contains("barrel"))) return;
                if (safeMonumentBarricade && target.Contains("door barricades")) return;
                if (safeSelf && entity == attacker) return;
                if (safeStructure && attacker != null && (entity is Barricade || entity is BuildingBlock || entity is Door || entity is SimpleBuildingBlock)) return;
                if (safeTrap && entity is BasePlayer && (attacker is Barricade || attacker is BaseTrap || attacker.PrefabName.Contains("wall.external.high"))) return;
                if (safeTurret && (entity is AutoTurret || attacker is AutoTurret || entity is FlameTurret || attacker is FlameTurret)) return; 
                if (safeWorld && (entity is BasePlayer && attacker == null)) return;
            }

            info.damageTypes = new DamageTypeList();
            info.PointStart = Vector3.zero;
            info.HitMaterial = 0;
        }

        #endregion

        #region Helpers

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void Broadcast(string key, params object[] args)
        {
            foreach (var player in players.Connected) player.Message(Lang(key, player.Id, args));
        }

        #endregion
    }
}
