using System;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("QuickSmelt", "Wulf/lukespragg", "1.3.0", ResourceId = 1067)]
    [Description("Increases the speed of the furnace smelting")]

    class QuickSmelt : RustPlugin
    {
        #region Initialization

        const string permAllow = "quicksmelt.allow";

        bool overcookMeat;
        bool usePermissions;

        float byproductModifier;
        int byproductPercent;
        float cookedModifier;
        int cookedPercent;
        int fuelUsageModifier;
        float smeltedModifier;
        int smeltedPercent;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["OvercookMeat"] = overcookMeat = GetConfig("OvercookMeat", false);
            Config["UsePermissions"] = usePermissions = GetConfig("UsePermissions", false);

            // Settings
            Config["ByproductModifier"] = byproductModifier = GetConfig("ByproductModifier", 1f);
            Config["ByproductPercent"] = byproductPercent = GetConfig("ByproductPercent", 50);
            Config["CookedModifier"] = cookedModifier = GetConfig("CookedModifier", 1f);
            Config["CookedPercent"] = cookedPercent = GetConfig("CookedPercent", 100);
            Config["FuelUsageModifier"] = fuelUsageModifier = GetConfig("FuelUsageModifier", 1);
            Config["SmeltedModifier"] = smeltedModifier = GetConfig("SmeltedModifier", 1f);
            Config["SmeltedPercent"] = smeltedPercent = GetConfig("SmeltedPercent", 100);

            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permAllow, this);
        }

        void OnServerInitialized()
        {
            var wood = ItemManager.FindItemDefinition("wood");
            var burnable = wood?.GetComponent<ItemModBurnable>();
            if (burnable != null)
            {
                burnable.byproductAmount = 1;
                burnable.byproductChance = 0.5f;
            }

            if (overcookMeat) return;

            var itemDefinitions = ItemManager.itemList;
            foreach (var item in itemDefinitions)
            {
                if (!item.shortname.Contains(".cooked")) continue;
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null) cookable.highTemp = 150;
            }
        }

        void Unload()
        {
            var itemDefinitions = ItemManager.itemList;
            foreach (var item in itemDefinitions)
            {
                if (!item.shortname.Contains(".cooked")) continue;
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null) cookable.highTemp = 250;
            }
        }

        #endregion

        #region Smelting Magic

        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            // TODO: Add options for campfire and/or furnace
            if (usePermissions && !permission.UserHasPermission(oven.OwnerID.ToString(), permAllow)) return;

            fuel.amount -= fuelUsageModifier - 1;

            burnable.byproductAmount = 1 * (int)byproductModifier; // TODO: Add check to prevent negative
            burnable.byproductChance = (100 - byproductPercent) / 100f; // TODO: Add check to set max of 100 and min of 0

            for (var i = 0; i < oven.inventorySlots; i++)
            {
                try
                {
                    var slotItem = oven.inventory.GetSlot(i);
                    if (slotItem == null || !slotItem.IsValid()) continue;

                    // TODO: Add options for smelting and/or cooking
                    var cookable = slotItem.info.GetComponent<ItemModCookable>();
                    if (cookable == null) continue;

                    if (slotItem.info.shortname.EndsWith(".cooked")) continue;

                    var consumptionAmount = (int)Math.Ceiling(cookedModifier * (Random.Range(0f, 1f) <= cookedPercent ? 1 : 0)); // TODO: The logic here is odd
                    var inFurnaceAmount = slotItem.amount;
                    if (inFurnaceAmount < consumptionAmount) consumptionAmount = inFurnaceAmount;

                    consumptionAmount = TakeFromInventorySlot(oven.inventory, slotItem.info.itemid, consumptionAmount, i);

                    if (consumptionAmount <= 0) continue;
                    var cookedItem = ItemManager.Create(cookable.becomeOnCooked, cookable.amountOfBecome * consumptionAmount);
                    if (!cookedItem.MoveToContainer(oven.inventory)) cookedItem.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
                }
                catch (InvalidOperationException) { }
            }
        }

        int TakeFromInventorySlot(ItemContainer container, int itemId, int amount, int slot)
        {
            var item = container.GetSlot(slot);
            if (item.info.itemid != itemId) return 0;

            if (item.amount > amount)
            {
                item.MarkDirty();
                item.amount -= amount;
                return amount;
            }

            amount = item.amount;
            item.RemoveFromContainer();
            return amount;
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
    }
}
