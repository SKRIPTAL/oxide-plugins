// Reference: Rust.Workshop

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Facepunch.Steamworks;
using Newtonsoft.Json;
using Rust;

namespace Oxide.Plugins
{
    [Info("Exporter", "The Oxide Team", 1.0)]
    [Description("Exports game data for the Oxide API Docs")]
    public class Exporter : RustPlugin
    {
        private static Exporter instance;

        private static DateTime dateTime;

        private readonly FieldInfo skins2 = typeof(ItemDefinition).GetField("_skins2", BindingFlags.NonPublic | BindingFlags.Instance);

        private void OnServerInitialized()
        {
            instance = this;
            webrequest.EnqueueGet("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", ReadScheme, this);
        }

        #region Items list

        [ConsoleCommand("export.items")]
        void ExportItems()
        {
            var export = new ExportLogger("ItemListDocs");
            var items = ItemManager.itemList;
            var itemList = items.OrderBy(x => x.shortname).ToList();

            // Item List: http://docs.oxidemod.org/rust/#item-list
            export.Log("# Item List");
            export.Log("");
            export.Log("| Item Id       | Item Name                    | Item Shortname           |");
            export.Log("|---------------|------------------------------|--------------------------|");

            foreach (var item in itemList)
            {
                var displayname = item.displayName.english.Replace("\t", "").Replace("\r", "").Replace("\n", "");
                var idSpacer = Fillup(item.itemid.ToString(), 14);
                var nameSpacer = Fillup(displayname, 29);
                var snameSpacer = Fillup(item.shortname, 25);

                export.Log($"| {item.itemid}{idSpacer}| {displayname}{nameSpacer}| {item.shortname}{snameSpacer}|");
            }
        }

        #endregion

        #region Skins list

        private class ItemSkin
        {
            public readonly int Id;
            public readonly string Name;

            public ItemSkin(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        [ConsoleCommand("export.skins")]
        void ExportSkins()
        {
            var export = new ExportLogger("ItemSkinsDocs");
            var items = ItemManager.itemList;
            var itemList = items.OrderBy(x => x.displayName.english).ToList();

            // Item Skins: http://docs.oxidemod.org/rust/#item-skins
            export.Log("# Item Skins");

            foreach (var item in itemList)
            {
                var displayname = item.displayName.english.Replace("\t", "").Replace("\r", "").Replace("\n", "");
                var skins = GetSkins(item);
                if (skins.Count == 0) continue;
                export.Log("");
                export.Log($"## {displayname} ({item.shortname})");
                export.Log("| Skin Id      | Skin name                         |");
                export.Log("|--------------|-----------------------------------|");

                foreach (var skin in skins.OrderBy(x => x.Name))
                {
                    var idSpacer = Fillup(skin.Id.ToString(), 13);
                    var nameSpacer = Fillup(skin.Name, 34);
                    export.Log($"| {skin.Id}{idSpacer}| {skin.Name}{nameSpacer}|");
                }
            }
        }

        #endregion

        #region Prefab List

        [ConsoleCommand("export.prefabs")]
        void ExportPrefabs()
        {
            var export = new ExportLogger("PrefabListDocs");
            // Prefab List: http://docs.oxidemod.org/rust/#prefab-list
            export.Log("# Prefab List");

            foreach (var str in GameManifest.Current.pooledStrings.OrderBy(x => x.str))
            {
                if (!str.str.StartsWith("assets/")) continue;

                // Autospawn: assets/bundled/prefabs/autospawn/
                // FX: assets/bundled/prefabs/fx/
                // Content: assets/content/
                // Prefabs: assets/prefabs/
                // Third Party: assets/standard assets/third party/

                //var prefab = str.str.Substring(str.str.LastIndexOf("/", StringComparison.Ordinal) + 1).Replace(".prefab", "");

                export.Log($"| {str.str} |");
            }
        }

        #endregion

        #region Helpers

        private class ExportLogger
        {
            private readonly string name;

            public ExportLogger(string name)
            {
                this.name = name;
            }

            public void Log(string line) => instance.LogToFile(name, line, instance);

        }

        private string Fillup(string value, int chars)
        {
            var retval = string.Empty;
            for (var i = 0; i < chars - value.Length; i++)
                retval += " ";
            return retval;
        }

        private void ReadScheme(int code, string response)
        {
            if (response != null && code == 200)
            {
                var schema = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response);
                var defs = new List<Inventory.Definition>();
                foreach (var item in schema.items)
                {
                    if (string.IsNullOrEmpty(item.itemshortname)) continue;
                    var steamItem = Global.SteamServer.Inventory.CreateDefinition((int)item.itemdefid);
                    steamItem.Name = item.name;
                    steamItem.SetProperty("itemshortname", item.itemshortname);
                    steamItem.SetProperty("workshopid", item.workshopid);
                    steamItem.SetProperty("workshopdownload", item.workshopdownload);
                    defs.Add(steamItem);
                }

                Global.SteamServer.Inventory.Definitions = defs.ToArray();

                foreach (var item in ItemManager.itemList)
                    skins2.SetValue(item, Global.SteamServer.Inventory.Definitions.Where(x => (x.GetStringProperty("itemshortname") == item.shortname) && !string.IsNullOrEmpty(x.GetStringProperty("workshopdownload"))).ToArray());

                Puts($"Loaded {Global.SteamServer.Inventory.Definitions.Length} approved workshop skins.");
            }
            else
            {
                PrintWarning($"Failed to load approved workshop skins... Error {code}");
            }
        }

        private List<ItemSkin> GetSkins(ItemDefinition def)
        {
            var skins = new List<ItemSkin>();
            skins.AddRange(def.skins.Select(skin => new ItemSkin(skin.id, skin.invItem.displayName.english)));
            skins.AddRange(def.skins2.Select(skin => new ItemSkin(skin.GetProperty<int>("workshopid"), skin.Name)));
            return skins;
        }

        #endregion
    }
}
