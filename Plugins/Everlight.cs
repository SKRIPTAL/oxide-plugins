// TODO: Add support for candle hats and miner hats
// TODO: Check if search lights and ceiling lights work

using System.ComponentModel;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Everlight", "Wulf/lukespragg", "3.0.0", ResourceId = 2170)]
    [Description("Creates infinite fuel and light from configured objects")]
    public class Everlight : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Campfires (true/false)", DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool Campfires;

            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Furnaces (true/false)", DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool Furnaces;

            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Lanterns (true/false)", DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool Lanterns;

            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Refineries (true/false)", DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool Refineries;
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
                LogWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Fuel Magic

        private object OnFindBurnable(BaseOven oven)
        {
            if (oven?.fuelType == null) return null;

            PrintWarning(oven.panelName);

            if (oven.panelName == "campfire" && !config.Campfires) return null;
            else if (oven.panelName.Contains("furnace") && !config.Furnaces) return null;
            else if (oven.panelName == "lantern" && !config.Lanterns) return null;
            else if (oven.panelName.Contains("refinery") && !config.Refineries) return null;

            return ItemManager.CreateByItemID(oven.fuelType.itemid, 1);
        }

        #endregion
    }
}
