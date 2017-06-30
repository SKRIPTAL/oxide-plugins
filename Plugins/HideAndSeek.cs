/*
 * TODO: Add cooldown option for taunting
 * TODO: Add option for picking which taunts are allowed?
 * TODO: Add option to only taunt prop's effect(s)
 * TODO: Figure out why Hurt() isn't working for damage passing
 * TODO: Move taunt GUI button to better position
 * TODO: Unselect active item if selected (make sure to restore fully)
 * TODO: Whitelist objects to block bad prefabs
 * TODO: Make taunt prefabs more dynamic?
 * TODO: Hiding as a tree causes prefab spawn warnings, block?
 * TODO: Add options for how many lives to give? Limit 1, last man standing?
 * TODO: Find a way for props to move, maybe not spectate mode; CanNetworkTo?
 * TODO: Avoid changing views if possible, related to ^
 * TODO: Thirst and hunger are not consistent when hiding/unhiding
 */

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hide and Seek", "Wulf/lukespragg", "0.2.0", ResourceId = 1421)]
    [Description("The classic game(mode) of hide and seek, as props")]
    public class HideAndSeek : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            // TODO: List<string> RestrictedProps
            // TODO: List<string> RestrictedTaunts

            [JsonProperty(PropertyName = "Allow taunt sounds (true/false)")]
            public bool AllowTaunts;

            [JsonProperty(PropertyName = "Allow any taunt sounds (true/false)")]
            public bool AllowAnyTaunts;

            [JsonProperty(PropertyName = "Remove player corpses (true/false)")]
            public bool RemoveCorpses;

            [JsonProperty(PropertyName = "Show prop gibs (true/false)")]
            public bool ShowPropGibs;

            [JsonProperty(PropertyName = "Hide from distance (3 feet)")]
            public int HideFromDistance;

            [JsonProperty(PropertyName = "Taunt cooldown (seconds)")]
            public int TauntCooldown;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    AllowTaunts = true,
                    AllowAnyTaunts = true,
                    RemoveCorpses = true,
                    ShowPropGibs = true,
                    HideFromDistance = 3,
                    TauntCooldown = 30
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.HideFromDistance == null)
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

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["AlreadyHidden"] = "You are already hidden",
                ["AlreadyUnhidden"] = "You are already unhidden",
                ["Hiding"] = "You are hiding... shhh!",
                ["NotAProp"] = "You are not a prop!",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotHiding"] = "You are no longer hiding, run!"
            }, this);
        }

        #endregion
 
        #region Initialization

        #region Taunt Prefabs

        private static readonly string[] animalTaunts = new[]
        {
            "animals/bear/attack1",
            "animals/bear/attack2",
            "animals/bear/bite",
            "animals/bear/breathe-1",
            "animals/bear/breathing",
            "animals/bear/death",
            "animals/bear/roar1",
            "animals/bear/roar2",
            "animals/bear/roar3",
            "animals/boar/attack1",
            "animals/boar/attack2",
            "animals/boar/flinch1",
            "animals/boar/flinch2",
            "animals/boar/scream",
            "animals/chicken/attack1",
            "animals/chicken/attack2",
            "animals/chicken/attack3",
            "animals/chicken/cluck1",
            "animals/chicken/cluck2",
            "animals/chicken/cluck3",
            "animals/horse/attack",
            "animals/horse/flinch1",
            "animals/horse/flinch2",
            "animals/horse/heavy_breath",
            "animals/horse/snort",
            "animals/horse/whinny",
            "animals/horse/whinny_large",
            "animals/rabbit/attack1",
            "animals/rabbit/attack2",
            "animals/rabbit/run",
            "animals/rabbit/walk",
            "animals/stag/attack1",
            "animals/stag/attack2",
            "animals/stag/death1",
            "animals/stag/death2",
            "animals/stag/flinch1",
            "animals/stag/scream",
            "animals/wolf/attack1",
            "animals/wolf/attack2",
            "animals/wolf/bark",
            "animals/wolf/breathe",
            "animals/wolf/howl1",
            "animals/wolf/howl2",
            "animals/wolf/run_attack",
        };
        private static readonly string[] buildingTaunts = new[]
        {
            "barricades/damage",
            "beartrap/arm",
            "beartrap/fire",
            //"bucket_drop_debris",
            "build/frame_place",
            //"build/promote_metal",
            //"build/promote_stone",
            //"build/promote_toptier",
            //"build/promote_wood",
            "build/repair",
            "build/repair_failed",
            "build/repair_full",
            "building/fort_metal_gib",
            "building/metal_sheet_gib",
            "building/stone_gib",
            "building/thatch_gib",
            "building/wood_gib",
            "door/door-metal-impact",
            "door/door-metal-knock",
            "door/door-wood-impact",
            "door/door-wood-knock",
            "door/lock.code.denied",
            "door/lock.code.lock",
            "door/lock.code.unlock",
            "door/lock.code.updated",
        };
        private static readonly string[] otherTaunts = new[]
        {
            //"entities/helicopter/heli_explosion",
            //"entities/helicopter/rocket_airburst_explosion",
            //"entities/helicopter/rocket_explosion",
            //"entities/helicopter/rocket_fire",
            "entities/loot_barrel/gib",
            "entities/loot_barrel/impact",
            "entities/tree/tree-impact",
            //"fire/fire_v2",
            //"fire/fire_v3",
            //"fire_explosion",
            //"gas_explosion_small",
            "gestures/cameratakescreenshot",
            "headshot",
            "headshot_2d",
            "hit_notify",
            /*"impacts/additive/explosion",
            "impacts/blunt/clothflesh/clothflesh1",
            "impacts/blunt/concrete/concrete1",
            "impacts/blunt/metal/metal1",
            "impacts/blunt/wood/wood1",
            "impacts/bullet/clothflesh/clothflesh1",
            "impacts/bullet/concrete/concrete1",
            "impacts/bullet/dirt/dirt1",
            "impacts/bullet/forest/forest1",
            "impacts/bullet/metal/metal1",
            "impacts/bullet/metalore/bullet_impact_metalore",
            "impacts/bullet/path/path1",
            "impacts/bullet/rock/bullet_impact_rock",
            "impacts/bullet/sand/sand1",
            "impacts/bullet/snow/snow1",
            "impacts/bullet/tundra/bullet_impact_tundra",
            "impacts/bullet/wood/wood1",
            "impacts/slash/concrete/slash_concrete_01",
            "impacts/slash/metal/metal1",
            "impacts/slash/metal/metal2",
            "impacts/slash/metalore/slash_metalore_01",
            "impacts/slash/rock/slash_rock_01",
            "impacts/slash/wood/wood1",*/
            "item_break",
            "player/beartrap_clothing_rustle",
            "player/beartrap_scream",
            "player/groundfall",
            "player/howl",
            //"player/onfire",
            "repairbench/itemrepair",
        };
        private static readonly string[] weaponTaunts = new[]
        {
            "ricochet/ricochet1",
            "ricochet/ricochet2",
            "ricochet/ricochet3",
            "ricochet/ricochet4",
            //"survey_explosion",
            //"weapons/c4/c4_explosion",
            "weapons/rifle_jingle1",
            "weapons/survey_charge/survey_charge_stick",
            "weapons/vm_machete/attack-1",
            "weapons/vm_machete/attack-2",
            "weapons/vm_machete/attack-3",
            "weapons/vm_machete/deploy",
            "weapons/vm_machete/hit"
        };

        #endregion

        private static System.Random random = new System.Random();
        private Dictionary<IPlayer, BaseEntity> props = new Dictionary<IPlayer, BaseEntity>();

        private const string permAllow = "hideandseek.allow";

        private void Init()
        {
            permission.RegisterPermission(permAllow, this);

            //foreach (var player in props.Values) TauntButton(player, null);
        }

        #endregion

        #region Prop Flags

        private void SetPropFlags(BasePlayer player)
        {
            // Remove admin/developer flags
            if (player.IsAdmin) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
            if (player.IsDeveloper) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, false);

            // Change to third-person view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.EyesViewmode, false);
        }

        private void UnsetPropFlags(BasePlayer player)
        {
            // Change to normal view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.EyesViewmode, false);

            // Restore admin/developer flags
            if (player.net.connection.authLevel > 0) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            if (DeveloperList.IsDeveloper(player)) player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);
        }

        #endregion

        #region Player Hiding

        private class PropEntity : MonoBehaviour
        {
            public IPlayer player;
            public BaseEntity prop;
            public Dictionary<IPlayer, BaseEntity> props;

            private void Awake()
            {
                player = GetComponent<IPlayer>();
                prop = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/loot_barrel_2.prefab", new Vector3(), new Quaternion(), true);
                props.Add(player, prop);
            }
        }

        private void HidePlayerNew(IPlayer player)
        {
            if (props.ContainsKey(player)) props.Remove(player);

            var basePlayer = player.Object as BasePlayer;
            var ray = new Ray(basePlayer.eyes.position, basePlayer.eyes.HeadForward());
            var entity = FindObject(ray, config.HideFromDistance);
            if (entity == null || props.ContainsKey(player)) return;

            // Hide active item
            if (basePlayer.GetActiveItem() != null)
            {
                var heldEntity = basePlayer.GetActiveItem().GetHeldEntity() as HeldEntity;
                heldEntity?.SetHeld(false);
            }

            // Create the prop entity
            PrintWarning(entity.name);
            var prop = GameManager.server.CreateEntity(entity.name, basePlayer.transform.position, basePlayer.transform.rotation);
            prop.SendMessage("SetDeployedBy", basePlayer, SendMessageOptions.DontRequireReceiver);
            prop.SendMessage("InitializeItem", entity, SendMessageOptions.DontRequireReceiver);
            /*var rigidBody = prop.gameObject.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
            rigidBody.useGravity = true;
            rigidBody.mass = 1f;
            rigidBody.drag = 0.1f;
            rigidBody.angularDrag = 0.1f;
            rigidBody.interpolation = RigidbodyInterpolation.None;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;*/
            prop.SetParent(null);
            prop.Spawn();
            props.Add(player, prop);
            //prop.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Make the player invisible
            basePlayer.SetParent(prop);
            //GameObject.Destroy(basePlayer.gameObject);
            /*basePlayer.model = prop.model;
            basePlayer.gameObject.AwakeFromInstantiate();
            var component = basePlayer.gameObject.GetComponent<Model>();
            basePlayer.SetModel(component);
            basePlayer.gameManager.Retire(basePlayer.gameObject);*/
            //basePlayer.gameObject.layer = 10;
            basePlayer.CancelInvoke("MetabolismUpdate");
            basePlayer.CancelInvoke("InventoryUpdate");

            //TauntButton(basePlayer, null);
            player.Reply(Lang("Hiding", player.Id));
        }

        private void HidePlayer(IPlayer player)
        {
            if (props.ContainsKey(player)) props.Remove(player);

            var basePlayer = player.Object as BasePlayer;
            var ray = new Ray(basePlayer.eyes.position, basePlayer.eyes.HeadForward());
            var entity = FindObject(ray, config.HideFromDistance);
            if (entity == null || props.ContainsKey(player)) return;

            // Hide active item
            if (basePlayer.GetActiveItem() != null)
            {
                var heldEntity = basePlayer.GetActiveItem().GetHeldEntity() as HeldEntity;
                heldEntity?.SetHeld(false);
            }

            // Create the prop entity
            var prop = GameManager.server.CreateEntity(entity.name, basePlayer.transform.position, basePlayer.transform.rotation);
            prop.SendMessage("SetDeployedBy", basePlayer, SendMessageOptions.DontRequireReceiver);
            prop.SendMessage("InitializeItem", entity, SendMessageOptions.DontRequireReceiver);
            prop.Spawn();
            props.Add(player, prop);

            // Make the player invisible
            basePlayer.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
            basePlayer.gameObject.SetLayerRecursive(10);
            basePlayer.CancelInvoke("MetabolismUpdate");
            basePlayer.CancelInvoke("InventoryUpdate");
            SetPropFlags(basePlayer);

            //TauntButton(basePlayer, null);
            player.Reply(Lang("Hiding", player.Id));
        }

        #endregion

        #region Player Unhiding

        private void UnhidePlayer(IPlayer player)
        {
            if (!props.ContainsKey(player)) return;

            var basePlayer = player.Object as BasePlayer;

            // Unhide active item
            if (basePlayer.GetActiveItem() != null)
            {
                var heldEntity = basePlayer.GetActiveItem().GetHeldEntity() as HeldEntity;
                heldEntity?.SetHeld(true);
            }

            // Make the player visible
            basePlayer.SetParent(null);
            basePlayer.metabolism.Reset();
            basePlayer.InvokeRepeating("InventoryUpdate", 1f, 0.1f * Random.Range(0.99f, 1.01f));
            //basePlayer.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            basePlayer.gameObject.SetLayerRecursive(17);
            //basePlayer.gameObject.SetActive(false);
            //UnsetPropFlags(basePlayer);

            // Remove the prop entity
            var prop = props[player];
            if (!prop.IsDestroyed) prop.Kill(config.ShowPropGibs ? BaseNetworkable.DestroyMode.Gib : BaseNetworkable.DestroyMode.None);
            props.Remove(player);

            //CuiHelper.DestroyUi(basePlayer, tauntPanel); // TODO
            player.Reply(Lang("NotHiding", player.Id));
        }

        #endregion

        #region Chat Commands

        [Command("hide")]
        private void HideCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAllow))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (props.ContainsKey(player) && basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating))
            {
                player.Reply(Lang("AlreadyHidden", player.Id));
                return;
            }

            //HidePlayer(player);
            HidePlayerNew(player);
        }

        [Command("unhide")]
        private void UnhideCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAllow))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (!props.ContainsKey(player) && !basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating))
            {
                player.Reply(Lang("AlreadyUnhidden", player.Id));
                return;
            }

            UnhidePlayer(player);
        }

        #endregion

        #region Prop Taunting

        [Command("taunt")]
        private void TauntCommand(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (!props.ContainsKey(player) && !basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.Spectating))
            {
                player.Reply(Lang("NotAProp", player.Id));
                return;
            }

            var taunt = otherTaunts[random.Next(otherTaunts.Length)]; // TODO: Implement taunt restrictions
            Effect.server.Run($"assets/bundled/prefabs/fx/{taunt}.prefab", basePlayer.transform.position, Vector3.zero);
        }

        #endregion

        #region Damage Passing

        private object OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (entity is BasePlayer) return null;

            if (!props.ContainsValue(entity))
            {
                var attacker = info.Initiator as BasePlayer;
                attacker?.Hurt(info.damageTypes.Total());
                return true;
            };

            /*var propPlayer = props[entity].Object as BasePlayer; // TODO: Fix this
            if (propPlayer.health <= 1)
            {
                propPlayer.Die();
                return null;
            }

            propPlayer.InitializeHealth(propPlayer.health - info.damageTypes.Total(), 100f);*/
            return true;
        }

        #endregion

        #region Death Handling

        private void OnEntityDeath(BaseEntity entity)
        {
            // Check for prop entity/player
            var basePlayer = entity.ToPlayer();
            if (basePlayer == null) return;

            var player = players.FindPlayerById(basePlayer.UserIDString);
            if (!props.ContainsKey(player)) return;

            // Get the prop entity
            UnhidePlayer(player);
            basePlayer.RespawnAt(basePlayer.transform.position, basePlayer.transform.rotation); // TODO: Implement respawn limitations

            // Remove the prop entity
            var prop = props[player];
            if (!prop.IsDestroyed) prop.Kill(BaseNetworkable.DestroyMode.Gib);

            // TODO: Kill player if prop dies
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            // Remove all corpses
            if (config.RemoveCorpses && entity.ShortPrefabName.Equals("player_corpse")) entity.KillMessage();
        }

        #endregion

        #region Spectate Blocking

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.Connection != null && arg.cmd.Name == "spectate") return true;
            return null;
        }

        private object OnPlayerInput(BasePlayer player, InputState input)
        {
            var iplayer = players.FindPlayerById(player.UserIDString);
            if (iplayer == null) return null;

            if (!props.ContainsKey(iplayer) /*&& !player.IsSpectating()*/ && input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                HidePlayerNew(iplayer);
            else if (props.ContainsKey(iplayer) /*&& player.IsSpectating()*/ && input.WasJustPressed(BUTTON.FIRE_SECONDARY))
                UnhidePlayer(iplayer);
            else if (props.ContainsKey(iplayer) /*&& player.IsSpectating()*/ && input.WasJustPressed(BUTTON.JUMP) || input.WasJustPressed(BUTTON.DUCK))
                return true;

            return null;
        }

        #endregion

        #region Prop Restoring

        private void OnUserConnected(IPlayer player)
        {
            if (!props.ContainsKey(player)) return;

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer.IsSleeping()) basePlayer.EndSleeping();
            SetPropFlags(basePlayer);
        }

        #endregion

        #region GUI Button

        string tauntPanel;

        private void TauntButton(BasePlayer player, string text)
        {
            var elements = new CuiElementContainer();
            tauntPanel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.0 0.0 0.0 0.0" },
                RectTransform = { AnchorMin = "0.026 0.037", AnchorMax = "0.075 0.10" }
            }, "Hud", "taunt");
            elements.Add(new CuiElement
            {
                Parent = tauntPanel,
                Components =
                {
                    new CuiRawImageComponent { Url = "http://i.imgur.com/28fdPww.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.0 0.0", AnchorMax = "1.0 1.0" }
                }
            });
            elements.Add(new CuiButton
            {
                Button = { Command = $"taunt", Color = "0.0 0.0 0.0 0.0" },
                RectTransform = { AnchorMin = "0.026 0.037", AnchorMax = "0.075 0.10" },
                Text = { Text = "" }
            });
            CuiHelper.DestroyUi(player, tauntPanel);
            CuiHelper.AddUi(player, elements);
        }

        #endregion

        #region Cleanup Props

        private void Unload()
        {
            var propList = props.Keys.ToList();
            foreach (var prop in propList) UnhidePlayer(prop);
            //foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, tauntPanel);
        }

        #endregion

        #region Helper Methods

        private static BaseEntity FindObject(Ray ray, float distance)
        {
            RaycastHit hit;
            return !Physics.Raycast(ray, out hit, distance) ? null : hit.GetEntity();
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
