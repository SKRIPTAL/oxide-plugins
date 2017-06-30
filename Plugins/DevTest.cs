#define DEBUG

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
#if RUST
using UnityEngine;
#endif

namespace Oxide.Plugins
{
    [Info("DevTest", "Oxide Team", "0.1.0", ResourceId = 0)]
    [Description("Tests all of the available Oxide hooks and provides useful helpers")]

    class DevTest : CovalencePlugin
    {
        #region Hook Verification

        Dictionary<string, bool> hooksRemaining = new Dictionary<string, bool>();
        int hookCount;
        int hooksVerified;

        public void HookCalled(string hook)
        {
            if (!hooksRemaining.ContainsKey(hook)) return;

            hookCount--;
            hooksVerified++;
            PrintWarning($"{hook} is working");
            hooksRemaining.Remove(hook);
            PrintWarning(hookCount == 0 ? "All hooks verified!" : $"{hooksVerified} hooks verified, {hookCount} hooks remaining");
        }

        #endregion

        #region Plugin Hooks (universal)

        private void Init()
        {
            hookCount = hooks.Count;
            hooksRemaining = hooks.Keys.ToDictionary(k => k, k => true);
            PrintWarning("{0} hook to test!", hookCount);

            HookCalled("Init");
        }

        protected override void LoadDefaultConfig() => HookCalled("LoadDefaultConfig");

        private void Loaded() => HookCalled("Loaded");

        private void Unloaded() => HookCalled("Unloaded");

        private void OnFrame() => HookCalled("OnFrame");

        #endregion

        #region Server Hooks (universal)

        private void OnServerInitialized()
        {
            PrintWarning($"{server.Name} at {server.Address}:{server.Port}");
            PrintWarning($"Oxide {OxideMod.Version} for {covalence.Game} {server.Version}");
            PrintWarning($"Server language detected as: {server.Language.TwoLetterISOLanguageName}");
            PrintWarning($"World time is: {server.Time.ToString("h:mm tt").ToLower()}");
            PrintWarning($"World date is: {server.Time}");

            HookCalled("OnServerInitialized");
        }

        private void OnServerSave()
        {
            server.Broadcast("Server is saving, hang tight!");
            PrintWarning("Server is saving...");

            HookCalled("OnServerSave");
        }

        private void OnServerShutdown()
        {
            server.Broadcast("Server is going offline, stash yo' loot!");

            HookCalled("OnServerShutdown");
        }

        #endregion

        #region Player Hooks (covalence)

        private object CanUserLogin(string name, string id, string ip)
        {
            PrintWarning($"{name} ({id}) at {ip} is attempting to login");

            HookCalled("CanUserLogin");
            return null;
        }

        private void OnUserApproved(string name, string id, string ip)
        {
            PrintWarning($"{name} ({id}) at {ip} has been approved");

            HookCalled("OnUserApproved");
        }

        private object OnUserChat(IPlayer player, string message)
        {
            PrintWarning($"{player.Name} said: {message}");

            HookCalled("OnUserChat");
            return null;
        }

        private object OnUserCommand(IPlayer player, string command, string[] args)
        {
            PrintWarning($"{player.Name} ({player.Id}) ran command: {command} {string.Join(" ", args)}");

            HookCalled("OnUserCommand");
            return null;
        }

        private void OnUserConnected(IPlayer player)
        {
            PrintWarning($"{player.Name} ({player.Id}) connected from {player.Address}");
            if (player.IsAdmin) PrintWarning($"{player.Name} is admin");
            PrintWarning($"{player.Name} is {(player.IsBanned ? "banned" : "not banned")}");
            PrintWarning($"{player.Name} ({player.Id}) language detected as {player.Language}");

            server.Broadcast($"Welcome {player.Name} to {server.Name}!");
            foreach (var target in players.Connected) target.Message($"Look out... {player.Name} is coming to get you!");

            HookCalled("OnUserConnected");
        }

        private void OnUserDisconnected(IPlayer player, string reason)
        {
            PrintWarning($"{player.Name} ({player.Id}) disconnected for: {reason ?? "Unknown"}");
            server.Broadcast($"{player.Name} has abandoned us... free loot!");

            HookCalled("OnUserDisconnected");
        }

        private void OnUserRespawn(IPlayer player)
        {
            PrintWarning($"{player.Name} is respawning now");

            HookCalled("OnUserRespawn");
        }

        private void OnUserRespawned(IPlayer player)
        {
            PrintWarning($"{player.Name} respawned at {player.Position()}");

            HookCalled("OnUserRespawned");
        }

        private void OnUserSpawn(IPlayer player)
        {
            PrintWarning($"{player.Name} is spawning now");

            HookCalled("OnUserSpawn");
        }

        private void OnUserSpawned(IPlayer player)
        {
            PrintWarning($"{player.Name} spawned at {player.Position()}");

            HookCalled("OnUserSpawned");
        }

        #endregion

#if HIDEHOLDOUT

        #region Server Hooks

        private void OnServerCommand(string command)
        {
            HookCalled("OnServerCommand");
        }

        #endregion

        #region Player Hooks

        private void OnChatCommand(PlayerInfos player, string command)
        {
            HookCalled("OnChatCommand");
        }

        private void OnPlayerDeath(PlayerInfos player)
        {
            HookCalled("OnPlayerDeath");
        }

        #endregion

#endif

#if HURTWORLD

        #region Entity Hooks

        private void OnEntitySpawned(NetworkViewData data)
        {
            HookCalled("OnEntitySpawned");
        }

        #endregion

        #region Player Hooks

        private void OnChatCommand(PlayerSession session, string command)
        {
            HookCalled("OnChatCommand");
        }

        private void OnPlayerConnected(PlayerSession session)
        {
            HookCalled("OnPlayerConnected");
        }

        private object OnPlayerChat(PlayerSession session, string message)
        {
            HookCalled("OnPlayerChat");

            return true;
        }

        private void OnPlayerDisconnected(PlayerSession session)
        {
            HookCalled("OnPlayerDisconnected");
        }

        private object OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            HookCalled("OnPlayerDeath");

            return null;
        }

        private bool CanCraft(PlayerSession session, CrafterServer crafter)
        {
            HookCalled("CanCraft");

            return true;
        }

        private bool CanUseMachine(PlayerSession session, BaseMachine<DrillMachine> machine) // TODO: Generalize
        {
            HookCalled("CanUseMachine");

            return true;
        }

        private void OnPlayerRespawn(PlayerSession session)
        {
            HookCalled("OnPlayerRespawn");
        }

        private void OnPlayerSpawn(PlayerSession session)
        {
            HookCalled("OnPlayerSpawn");
        }

        private void OnUserApprove(PlayerSession session)
        {
            HookCalled("OnUserApprove");
        }

        private void OnPlayerInput(PlayerSession session, InputControls input)
        {
            HookCalled("OnPlayerInput");
        }

        #endregion

        #region Structure Hooks

        private bool CanUseDoubleDoor(PlayerSession session, DoubleDoorServer door)
        {
            HookCalled("CanUseDoubleDoor");

            return true;
        }

        private bool CanUseGarageDoor(PlayerSession session, GarageDoorServer door)
        {
            HookCalled("CanUseGarageDoor");

            return true;
        }

        private bool CanUsePillboxDoor(PlayerSession session, DoorPillboxServer door)
        {
            HookCalled("CanUsePillboxDoor");

            return true;
        }

        private bool CanUseSingleDoor(PlayerSession session, DoorSingleServer door)
        {
            HookCalled("CanUseSingleDoor");

            return true;
        }

        void OnDoubleDoorUsed(DoubleDoorServer door, PlayerSession session)
        {
            HookCalled("OnDoubleDoorUsed");
        }

        void OnGarageDoorUsed(GarageDoorServer door, PlayerSession session)
        {
            HookCalled("OnGarageDoorUsed");
        }

        void OnSingleDoorUsed(DoorSingleServer door, PlayerSession session)
        {
            HookCalled("OnSingleDoorUsed");
        }

        #endregion

        #region Vehicle Hooks

        private object CanEnterVehicle(PlayerSession session, VehiclePassenger vehicle)
        {
            HookCalled("CanEnterVehicle");

            return null;
        }

        private bool CanExitVehicle(PlayerSession session, VehiclePassenger vehicle)
        {
            HookCalled("CanExitVehicle");

            return true;
        }

        private void OnEnterVehicle(PlayerSession session, VehiclePassenger vehicle)
        {
            HookCalled("OnEnterVehicle");
        }

        private void OnExitVehicle(PlayerSession session, VehiclePassenger vehicle)
        {
            HookCalled("OnExitVehicle");
        }

        #endregion

#endif

#if REIGNOFKINGS

        #region Entity Hooks

        private void OnEntityHealthChange(CodeHatch.Networking.Events.Entities.EntityDamageEvent e)
        {
            HookCalled("OnEntityHealthChange");
        }

        private void OnEntityDeath(CodeHatch.Networking.Events.Entities.EntityDeathEvent e)
        {
            HookCalled("OnEntityDeath");
        }

        #endregion

        #region Player Hooks

        void OnChatCommand(CodeHatch.Engine.Networking.Player player, string command, string[] args)
        {
            PrintWarning("OnChatCommand works!");
        }

        #endregion

        #region Structure Hooks

        private void OnCubePlacement(CodeHatch.Blocks.Networking.Events.CubePlaceEvent evt)
        {
            HookCalled("OnCubePlacement");
        }

        private void OnCubeTakeDamage(CodeHatch.Blocks.Networking.Events.CubeDamageEvent evt)
        {
            HookCalled("OnCubeTakeDamage");
        }

        private void OnCubeDestroyed(CodeHatch.Blocks.Networking.Events.CubeDestroyEvent evt)
        {
            HookCalled("OnCubeDestroyed");
        }

        #endregion

#endif

#if RUST

        #region Server Hooks

        private void OnNewSave(string name) => HookCalled("OnNewSave");

        private object OnRconConnection(System.Net.IPEndPoint ip)
        {
            PrintWarning($"{ip.Address} connected via RCON on port {ip.Port}");

            HookCalled("OnRconConnection");
            return null;
        }

        private void OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> dictionary)
        {
            PrintWarning($"{dictionary.Count} entiries loaded from save");

            HookCalled("OnSaveLoad");
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;
            if (player != null) PrintWarning($"{player.displayName} ({player.UserIDString}) ran command: {arg.cmd.FullName} {arg.FullString}");

            HookCalled("OnServerCommand");
            return null;
        }

        private void OnTerrainInitialized() => HookCalled("OnTerrainInitialized");

        private void OnTick() => HookCalled("OnTick");

        #endregion

        #region Player Hooks

        private bool CanBypassQueue(Network.Connection connection)
        {
            PrintWarning($"Can {connection.username} ({connection.userid}) bypass the queue? Yes.");

            HookCalled("CanBypassQueue");
            return true;
        }

        private bool CanEquipItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.containerBelt.playerOwner;
            PrintWarning($"Can {player.displayName} ({player.UserIDString}) equip {item.info.displayName.english}? Yes.");

            HookCalled("CanEquipItem");
            return true;
        }

        private bool CanWearItem(PlayerInventory inventory, Item item)
        {
            var player = inventory.containerWear.playerOwner;
            PrintWarning($"Can {player.displayName} ({player.UserIDString}) wear {item.info.displayName.english}? Yes.");

            HookCalled("CanWearItem");
            return true;
        }

        private void OnFindSpawnPoint() => HookCalled("OnFindSpawnPoint");

        private object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            HookCalled("OnPlayerAttack");
            return null;
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return null;

            PrintWarning($"{player.displayName} said: {arg.GetString(0)}");

            HookCalled("OnPlayerChat");
            return null;
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            //PrintWarning($"{player.displayName} sent input: {input.current}");

            HookCalled("OnPlayerInput");
        }

        private object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity entity, float delta)
        {
            var player = entity as BasePlayer;
            if (player == null) return null;

            //PrintWarning($"{player.displayName} health: {player.health}, thirst: {metabolism.hydration.value}, calories: {metabolism.calories.value}," +
            //             $"dirty: {metabolism.isDirty}, poisoned: {(metabolism.poison.value.Equals(1) ? "true" : "false")}");

            HookCalled("OnRunPlayerMetabolism");
            return null;
        }

        #endregion

        #region Entity Hooks

        private bool CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            HookCalled("CanPickupEntity");
            return false;
        }

        private void OnAirdrop(CargoPlane plane, UnityEngine.Vector3 location)
        {
            PrintWarning($"Airdrop incoming via plane {plane.net.ID}, target: {location}");

            HookCalled("OnAirdrop");
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            HookCalled("OnEntityTakeDamage");
        }

        private void OnEntityBuilt(Planner planner, UnityEngine.GameObject go)
        {
            HookCalled("OnEntityBuilt");
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            // TODO: Print player died
            // TODO: Automatically respawn admin after X time

            HookCalled("OnEntityDeath");
        }

        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            HookCalled("OnEntityEnter");
        }

        private void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        {
            HookCalled("OnEntityLeave");
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            HookCalled("OnEntitySpawned");
        }

        private void OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            HookCalled("OnOvenToggle");
        }

        #endregion

        #region Item Hooks

        private void OnItemCraft(ItemCraftTask item)
        {
            // TODO: Print item crafting

            HookCalled("OnItemCraft");
        }

        private void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            // TODO: Print item deployed

            HookCalled("OnItemDeployed");
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            PrintWarning($"{player.displayName} picked up {item.info.displayName.english}");

            HookCalled("OnCollectiblePickup");
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            // TODO: Print item added

            HookCalled("OnItemAddedToContainer");
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            // TODO: Print item removed

            HookCalled("OnItemRemovedToContainer");
        }

        private void OnConsumableUse(Item item)
        {
            // TODO: Print consumable item used

            HookCalled("OnConsumableUse");
        }

        private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            // TODO: Print fuel consumed

            HookCalled("OnConsumeFuel");
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            // TODO: Print item to be gathered

            HookCalled("OnDispenserGather");
        }

        private void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            // TODO: Print item to be gathered

            HookCalled("OnCropGather");
        }

        private void OnSurveyGather(SurveyCharge survey, Item item)
        {
            HookCalled("OnSurveyGather");
        }

        private void OnQuarryEnabled() => HookCalled("OnQuarryEnabled");

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            HookCalled("OnQuarryGather");
        }

        private void OnTrapArm(BearTrap trap)
        {
            HookCalled("OnTrapArm");
        }

        private void OnTrapSnapped(BaseTrapTrigger trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapSnapped");
        }

        private void OnTrapTrigger(BaseTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapTrigger");
        }

        #endregion

        #region Sign Hooks

        private bool CanLockSign(BasePlayer player, Signage sign)
        {
            HookCalled("CanLockSign");
            return true;
        }

        private bool CanUnlockSign(BasePlayer player, Signage sign)
        {
            HookCalled("CanUnlockSign");
            return true;
        }

        private bool CanUpdateSign(BasePlayer player, Signage sign)
        {
            HookCalled("CanUpdateSign");
            return true;
        }

        private void OnSignLocked(Signage sign, BasePlayer player)
        {
            HookCalled("OnSignLocked");
        }

        private object OnSignUpdate(Signage sign, BasePlayer player)
        {
            HookCalled("OnSignUpdate");
            return null;
        }

        private void OnSignUpdated(Signage sign, BasePlayer player)
        {
            HookCalled("OnSignUpdated");
        }

        #endregion

        #region Structure Hooks

        private void CanUseLock(BasePlayer player, BaseLock @lock)
        {
            HookCalled("CanUseLock");
        }

        private void OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnStructureDemolish");
        }

        private void OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            HookCalled("OnStructureRepair");
        }

        private void OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnStructureRotate");
        }

        private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            HookCalled("OnStructureUpgrade");
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            HookCalled("OnHammerHit");
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectileShoot)
        {
            HookCalled("OnWeaponFired");
        }

        private void OnMeleeThrown(BasePlayer player, Item item)
        {
            HookCalled("OnMeleeThrown");
        }

        private void OnItemThrown(BasePlayer player, BaseEntity entity)
        {
            HookCalled("OnItemThrown");
        }

        private void OnDoorClosed(Door door, BasePlayer player)
        {
            HookCalled("OnDoorClosed");
        }

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            HookCalled("OnDoorOpened");
        }

        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            HookCalled("OnCupboardAuthorize");
        }

        private void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            HookCalled("OnCupboardDeauthorize");
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            HookCalled("OnRocketLaunched");
        }

        private void OnTrapArm(BearTrap trap, BasePlayer player)
        {
            HookCalled("OnTrapArm");
        }

        private void OnTrapDisarm(Landmine trap, BasePlayer player)
        {
            HookCalled("OnTrapDisarm");
        }

        private void OnTrapSnapped(BearTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapSnapped");
        }

        private void OnTrapTrigger(BearTrap trap, UnityEngine.GameObject go)
        {
            HookCalled("OnTrapTrigger");
        }

        #endregion

        #region Vending Hooks

        private object CanAdministerVending(VendingMachine vending, BasePlayer player)
        {
            HookCalled("CanAdministerVending");
            return null;
        }

        private object CanUseVending(VendingMachine vending, BasePlayer player)
        {
            HookCalled("CanUseVending");
            return null;
        }

        private void OnAddVendingOffer(VendingMachine vending, BasePlayer player) => HookCalled("OnAddVendingOffer");

        private void OnBuyVendingItem(VendingMachine vending, BasePlayer player) => HookCalled("OnBuyVendingItem");

        private void OnDeleteVendingOffer(VendingMachine vending, BasePlayer player) => HookCalled("OnDeleteVendingOffer");

        private void OnOpenVendingAdmin(VendingMachine vending, BasePlayer player) => HookCalled("OnOpenVendingAdmin");

        private void OnOpenVendingShop(VendingMachine vending, BasePlayer player) => HookCalled("OnOpenVendingShop");

        private void OnRefreshVendingStock(VendingMachine vending, BasePlayer player) => HookCalled("OnRefreshVendingStock");

        private object OnRotateVendingMachine(VendingMachine vending, BasePlayer player)
        {
            HookCalled("OnRotateVendingMachine");
            return null;
        }

        private void OnToggleVendingBroadcast(VendingMachine vending, BasePlayer player) => HookCalled("OnToggleVendingBroadcast");

        private object OnVendingTransaction(VendingMachine vending, BasePlayer player)
        {
            HookCalled("OnVendingTransaction");
            return null;
        }

        #endregion

        [Command("dev.entityi")]
        void EntityInfoCommand(IPlayer player, string command, string[] args)
        {
            var entity = FindEntity(player.Object as BasePlayer, 3);
            if (entity != null) player.Reply($"Prefab: {entity.PrefabName}\nPrefab ID: {entity.prefabID.ToString()}\nNetwork ID: {entity.net.ID}");
        }

        [Command("dev.entity")]
        void EntityCommand(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;

            foreach (var str in GameManifest.Current.pooledStrings)
            {
                if (!str.str.StartsWith("assets/prefabs/") || !str.str.EndsWith(".prefab") || str.str.Contains("effects/") || str.str.Contains(".item")) continue;
                if (!str.str.Contains(args[0])) continue;
                LogWarning(str.str);

                if (str.str.Contains("building core"))
                {
                    var block = (BuildingBlock)GameManager.server.CreateEntity(str.str);
                    block.transform.position = basePlayer.transform.position;
                    block.transform.rotation = basePlayer.transform.rotation;
                    block.gameObject.SetActive(true);
                    block.blockDefinition = PrefabAttribute.server.Find<Construction>(block.prefabID);
                    block.Spawn();
                    block.SetGrade(BuildingGrade.Enum.Metal);
                    block.SetHealthToMax();
                    block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
                else
                {
                    var entity = GameManager.server.CreateEntity(str.str, basePlayer.transform.position, basePlayer.transform.rotation);
                    entity.Spawn();
                }
            }
        }

        [Command("dev.heli")]
        void HeliCommand(IPlayer player, string command, string[] args)
        {
            var entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion());
            if (entity != null)
            {
                var heli = entity.GetComponent<PatrolHelicopterAI>();
                heli.SetInitialDestination((player.Object as BasePlayer).transform.position + new Vector3(0f, 10f, 0f), 0.25f);
                entity.Spawn();
            }
        }

        static readonly FieldInfo viewAngles = typeof(BasePlayer).GetField("viewAngles", (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static));

        [Command("dev.npc")]
        void NpcCommand(IPlayer player, string command, string[] args)
        {
            var pos = (player.Object as BasePlayer).transform.position;
            var entity = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", new Vector3(pos.x, pos.y, pos.z - 20), new Quaternion());
            var npc = entity as BasePlayer;
            if (npc != null)
            {
                npc.Spawn();
                npc.displayName = "Bob";
                //viewAngles.SetValue(npc, viewAngles.eulerAngles);
                npc.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                npc.syncPosition = true;
                npc.stats = new PlayerStatistics(npc);
                npc.userID = 70000000000000000L;
                npc.UserIDString = npc.userID.ToString();
                npc.MovePosition(pos);
                npc.eyes = npc.eyes ?? npc.GetComponent<PlayerEyes>();
                var newEyes = pos + new Vector3(0, 1.6f, 0);
                npc.eyes.position.Set(newEyes.x, newEyes.y, newEyes.z);
                npc.EndSleeping();
                //if (locomotion != null) Destroy(locomotion);
                //locomotion = npc.gameObject.AddComponent<HumanLocomotion>();
                //if (trigger != null) Destroy(trigger);
                //trigger = npc.gameObject.AddComponent<HumanTrigger>();
                //timer.Every(1f, () => npc.MovePosition(new Vector3(pos.x, pos.y, pos.z + 5)));
            }
        }

        [Command("dev.plane")]
        void PlaneCommand(IPlayer player, string command, string[] args)
        {
            var entity = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(), new Quaternion());
            entity?.Spawn();
        }

        [Command("dev.supply")]
        void SupplyDropCommand(IPlayer player, string command, string[] args)
        {
            var pos = new Vector3(player.Position().X, player.Position().Y + 15, player.Position().Z);
            var entity = GameManager.server.CreateEntity("assets/prefabs/misc/supply drop/supply_drop.prefab", pos, new Quaternion());
            entity?.Spawn();
        }

        [Command("dev.remove")]
        void RemoveCommand(IPlayer player, string command, string[] args)
        {
            var entity = FindEntity(player.Object as BasePlayer, 3);
            entity?.Kill(BaseNetworkable.DestroyMode.Gib);
        }

        [Command("dev.removeall")]
        void RemoveAllCommand(IPlayer player, string command, string[] args)
        {
            var entities = UnityEngine.Object.FindObjectsOfType<BaseEntity>();
            foreach (var entity in entities) { if (entity is BasePlayer) continue; entity.Kill(BaseNetworkable.DestroyMode.Gib); }
        }

        BaseEntity FindEntity(BasePlayer player, float distance)
        {
            RaycastHit hit;
            var ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            return Physics.Raycast(ray, out hit, distance) ? hit.GetEntity() : null;
        }

#endif

#if SEVENDAYS

        #region Server Hooks

        private void OnServerCommand(ClientInfo client, string[] args)
        {
            HookCalled("OnServerCommand");
        }

        #endregion

        #region Entity Hooks

        private void OnAirdrop(UnityEngine.Vector3 location)
        {
            HookCalled("OnAirdrop");
        }

        private void OnEntitySpawned(Entity entity)
        {
            HookCalled("OnEntitySpawned");
        }

        private void OnEntityTakeDamage(EntityAlive entity, DamageSource source)
        {
            HookCalled("OnEntityTakeDamage");
        }

        private void OnEntityDeath(Entity entity, DamageResponse response)
        {
            HookCalled("OnEntityDeath");
        }

        #endregion

        #region Player Hooks

        private void OnPlayerConnected(ClientInfo client)
        {
            HookCalled("OnPlayerConnected");
        }

        #endregion

#endif

#if THEFOREST

        #region Player Hooks

        private void OnPlayerChat(ChatEvent evt)
        {
            /*if (evt.Message.Contains("save"))
            {
                var gameObject = UnityEngine.GameObject.Find("PlayerPlanePosition");
                if (gameObject) TheForest.Utils.LocalPlayer.CamFollowHead.planePos = gameObject.transform;

                LevelSerializer.SaveGame("Game");
                LevelSerializer.Checkpoint();
                LogWarning("Server has been saved!");
            }*/

            HookCalled("OnPlayerChat");
        }

        #endregion

#endif

#if UNTURNED

        #region Server Hooks

        private void OnServerCommand(Steamworks.CSteamID steamId, string command, string arg)
        {
            HookCalled("OnServerCommand");
        }

        #endregion

#endif
    }
}
