﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Cornucopia", "Wulf/lukespragg", "1.2.0", ResourceId = 1264)]
    [Description("")]
    public class Cornucopia : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            //[JsonProperty(PropertyName = "Maximum build height")]
            //public int MaxBuildHeight;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        private class CornuConfig
        {
            public CornuConfig()
            {
                // Animals
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/chicken.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/horse.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/boar.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/stag.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/wolf.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/rust.ai/agents/bear.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });

                // Ore nodes
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });

                // Silver barrels
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_barrel_1.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Brown barrels
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_barrel_2.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Oil Drums
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/oil_barrel.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Trashcans
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_trash.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Trash piles (food)
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/loot/trash-pile-1.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Weapon crates
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Box crates
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });
            }

            // Refresh Interval in minutes
            public int RefreshMinutes = 15;

            // Apply the loot fix to prevent stacked rad town loot crates
            public bool ApplyLootFix = true;

            // Run the cycle on start
            public bool RefreshOnStart = false;

            // If true, any item that has a maximum will be prevented from spawning outside of the Cornucopia spawn cycle
            //public bool MaxSpawnBlock = true;

            public List<CornuConfigItem> Animals = new List<CornuConfigItem>();
            public List<CornuConfigItem> Ores = new List<CornuConfigItem>();
            public List<CornuConfigItem> Loots = new List<CornuConfigItem>();
        }

        private class CornuConfigItem
        {
            public string Prefab;
            public int Min;
            public int Max;
            public bool IgnoreIrridiated;
            public bool DeleteEmtpy;
        }

        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            if (player.IsAdmin)
            {
                sb.Append("  · ").AppendLine("<color=lime>/cdump</color> (<color=orange>cornu.dump</color>) for RCON stats"); // TODO: Localize
                sb.Append("  · ").AppendLine("<color=lime>/cspawn</color> (<color=orange>cornu.spawn</color>) adjusts resources");
                sb.Append("  · ").AppendLine("<color=lime>/cfixloot</color> (<color=orange>cornu.fixloot</color>) loot box stacking fix");
                sb.Append("  · ").Append("<color=lime>/cpurge</color> (<color=orange>cornu.purge</color>) deletes ALL resources");
            }
            player.ChatMessage(sb.ToString());
        }

        private void OnServerInitialized() => timer.Every(config.RefreshMinutes * 60, OnTimer);

        private void OnTimer()
        {
            if (config.ApplyLootFix) FixLoot();

            //spawnBlock = false; // TODO: Use or remove
            try
            {
                MainSpawnCycle();
            }
            finally
            {
                //spawnBlock = config.MaxSpawnBlock; // TODO: Use or remove
            }
        }
 
        private Dictionary<string, IGrouping<string, BaseEntity>> GetAnimals()
        {
            // bear, boar, chicken, horse, stag, wolf

            return Resources.FindObjectsOfTypeAll<BaseNpc>()
                .Where(c => c.isActiveAndEnabled).Cast<BaseEntity>()
                .GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c);
        }

        private Dictionary<string, int> GetCollectibles()
        {
            // hemp, metalore-2, mushroom-cluster-1, mushroom-cluster-2, mushroom-cluster-3, mushroom-cluster-4,
            // mushroom-cluster-5, mushroom-cluster-6, sulfurore-3, stone-1, wood

            return Resources.FindObjectsOfTypeAll<CollectibleEntity>()
                .Where(c => c.isActiveAndEnabled && !c.ShortPrefabName.Contains("hemp") && !c.ShortPrefabName.Contains("mushroom"))
                .GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c.Count());
        }

        Dictionary<string, IGrouping<string, BaseEntity>> GetLootContainers()
        {
            // crate_normal, crate_normal_2, loot_barrel_1, loot_barrel_2, loot_trash

            return Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled)
                .Cast<BaseEntity>()
                .GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c);
        }
 
        private Dictionary<string, IGrouping<string, BaseEntity>> GetOreNodes()
        {
            // metal-ore, stone-ore, sulfur-ore

            return Resources.FindObjectsOfTypeAll<ResourceEntity>()
                .Where(c => /*c.name.StartsWith("autospawn") &&*/ c.isActiveAndEnabled).Cast<BaseEntity>()
                .GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c);
        }

        private void DumpSpawns(Dictionary<string, int> entities)
        {
            foreach (var t in entities) Puts($"{t.Key.PadRight(50)} {t.Value}");
        }

        private void DumpSpawns(Dictionary<string, IGrouping<string, BaseEntity>> entities)
        {
            foreach (var t in entities) Puts($"{t.Key.PadRight(50)} {t.Value.Count()}");
        }

        private void DumpEntities()
        {
            Puts("= COLLECTIBLES ================");
            DumpSpawns(GetCollectibles());
            Puts("= NODES =======================");
            DumpSpawns(GetOreNodes());
            Puts("= CONTAINERS ==================");
            DumpSpawns(GetLootContainers());
            Puts("= ANIMALS =====================");
            DumpSpawns(GetAnimals());
        }

        #region Commands

        [Command("cornu.dump", "cdump")]
        private void DumpCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsAdmin) DumpEntities();
        }

        [Command("cornu.spawn", "cspawn")]
        private void SpawnCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsAdmin) MainSpawnCycle();
        }

        [Command("cornu.fixloot", "cfixloot")]
        private void FixLootCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsAdmin) FixLoot(player);
        }

        [Command("cornu.purge", "cpurge")]
        private void PurgeCommand(IPlayer player, string command, string[] args)
        {
            if (player.IsAdmin) Purge();
        }

        [Command("cornu.test", "ctest")]
        private void TestCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            var toto = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled && c.ShortPrefabName.Contains("trash-pile"));

            Puts($"{toto.First().PrefabName}");
        }

        #endregion

        private void SubCycle(IEnumerable<BaseEntity> entities, IEnumerable<CornuConfigItem> limits, List<CollectibleEntity> collectibles, ref bool aborted)
        {
            foreach (var spawn in limits.Where(s => s.Min != -1 && s.Max != -1))
            {
                var matches = entities.Where(r => r.PrefabName == spawn.Prefab);

                if (matches.Count() < spawn.Min && spawn.Min != -1)
                    if (!aborted) BatchSpawn(matches.Count(), spawn.Min, spawn.Prefab, collectibles, ref aborted);
                else if (matches.Count() > spawn.Max && spawn.Max != -1)
                    PopulationControl(matches, spawn.Max);

                var deleted = 0;
                if (spawn.DeleteEmtpy)
                {
                    foreach (var match in matches.OfType<LootContainer>())
                    {
                        if (!match.inventory.itemList.Any())
                        {
                            match.Kill();
                            deleted++;
                        }
                    }
                }
                if (deleted > 0) Puts($"Deleted {deleted} empty {spawn.Prefab}");
            }
        }

        private void MainSpawnCycle()
        {
            var doAnimals = config.Animals.Any(a => a.Min != -1 || a.Max != -1);
            var doOres = config.Ores.Any(a => a.Min != -1 || a.Max != -1);
            var doLoots = config.Loots.Any(a => a.Min != -1 || a.Max != -1);

            if (!doAnimals && !doOres && !doLoots)
            {
                Puts("Nothing to process, skipping main spawn cycle");
                return;
            }

            var collectibles = Resources.FindObjectsOfTypeAll<CollectibleEntity>().Where(c => c.isActiveAndEnabled).ToList();
            var aborted = false;

            if (doAnimals)
            {
                SubCycle(Resources.FindObjectsOfTypeAll<BaseNpc>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), config.Animals, collectibles, ref aborted);
            }

            if (doOres)
            {
                SubCycle(Resources.FindObjectsOfTypeAll<ResourceEntity>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), config.Ores, collectibles, ref aborted);
            }

            if (doLoots)
            {
                SubCycle(Resources.FindObjectsOfTypeAll<LootContainer>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), config.Loots, collectibles, ref aborted);
            }
        }

        private void PopulationControl(IEnumerable<BaseEntity> matches, int cap)
        {
            if (cap < 0 || matches.Count() < cap || matches.Count() == 0) return;

            var toDelete = matches.Count() - cap;
            var killed = 0;
            var shortPrefabName = matches.First().ShortPrefabName;

            while (killed != toDelete)
            {
                var idx = Random.Range(0, matches.Count() - 1);
                var match = matches.ElementAt(idx);
                if (!match.enabled) continue;

                match.enabled = false;
                match.Kill();
                killed++;
            }

            Puts($"Destroying {toDelete}X {shortPrefabName}!");
        }

        private void BatchSpawn(int current, int wanted, string prefab, List<CollectibleEntity> collectibles, ref bool aborted)
        {
            if (aborted) return;

            var toSpawn = wanted - current;
            if (toSpawn <= 0) return;

            if (toSpawn > collectibles.Count)
            {
                Puts($"Could not find enough collectibles to complete the spawn cycle (this is normal after a server restart, it takes time!)");
                toSpawn = collectibles.Count;
                aborted = true;
            }

            Puts($"Spawning {toSpawn}X {prefab}!");
            for (var i = 0; i < toSpawn; i++)
                ReplaceCollectibleWithSomething(prefab, collectibles);
        }

        private void ReplaceCollectibleWithSomething(string prefabName, List<CollectibleEntity> collectibles)
        {
            // Pick a collectible that we did not replace yet and remove it from the list
            var pick = Random.Range(0, collectibles.Count - 1);
            var spawnToReplace = collectibles.ElementAt(pick);
            collectibles.RemoveAt(pick);

            // save the position
            var position = spawnToReplace.transform.position;

            // delete the collectible (we are replacing it)
            spawnToReplace.Kill();

            var entity = GameManager.server.CreateEntity(prefabName, position, new Quaternion(0, 0, 0, 0));
            if (entity == null)
            {
                Puts($"Tried to spawn {prefabName} but entity could not be spawned.");
                return;
            }

            entity.name = prefabName;
            entity.Spawn();
        }

        private void FixLoot(IPlayer player = null)
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled && c.ShortPrefabName.StartsWith("crate")).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z).ToList();
            var count = spawns.Count;
            var racelimit = count * count;
            var antirace = 0;
            var deleted = 0;

            for (var i = 0; i < count; i++)
            {
                var box = spawns[i];
                var pos = GetBoxPos(box);

                if (++antirace > racelimit)
                {
                    Puts("Race condition detected ?! report to author");
                    return;
                }

                var next = i + 1;
                while (next < count)
                {
                    var box2 = spawns[next];
                    var pos2 = GetBoxPos(box2);
                    var distance = Vector2.Distance(pos, pos2);

                    if (++antirace > racelimit)
                    {
                        Puts("Race condition detected ?! report to author");
                        return;
                    }

                    if (distance < 5)
                    {
                        spawns.RemoveAt(next);
                        count--;
                        box2.Kill();
                        deleted++;
                    }
                    else break;
                }
            }

            if (deleted > 0) Puts($"Deleted {deleted} stacked loot boxes (out of {count})");
            player?.Reply($"Deleted {deleted} stacked loot boxes (out of {count})");
        }

        private void Purge()
        {
            // Delete all spawnables
            var ores = GetOreNodes();
            foreach (var grp in ores.Values)
                foreach (var ore in grp) ore.Kill();

            var loots = GetLootContainers();
            foreach (var grp in loots.Values)
                foreach (var loot in grp) loot.Kill();

            var animals = GetAnimals();
            foreach (var grp in animals.Values)
                foreach (var animal in grp) animal.Kill();
        }

        /*private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!spawnBlock) return;

            var controlled = config.Animals.Union(config.Ores).Union(config.Loots);
            var prefab = entity.PrefabName;
            if (controlled.Any(c => c.Prefab == prefab && c.Max != -1))
            {
                entity.Kill();
                //Puts($"BLOCKED OnEntitySpawned {entity.ShortPrefabName}");
            }
        }*/

        #region Helpers

        private Vector2 GetBoxPos(LootContainer box) => new Vector2(box.transform.position.x, box.transform.position.z);

        #endregion
    }
}
