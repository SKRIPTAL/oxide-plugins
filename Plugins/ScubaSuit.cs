using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ScubaSuit", "Wulf/lukespragg", "2.0.0", ResourceId = 1876)]
    [Description("Protects player from drowning and cold damage while swimming")]
    public class ScubaSuit : CovalencePlugin
    {
        #region Initialization

        private const string permUse = "scubasuit.use";
        private ConfigData config;

        public class ConfigData
        {
            public List<ItemInfo> Cold { get; set; }
            public List<ItemInfo> Drown { get; set; }
            //public bool DamageArmor { get; set; } = false;
            //public float ArmorDamageAmount { get; set; } = 0.0f;

            public class ItemInfo
            {
                public float suit { get; set; } = 0.8f;
                public float chest { get; set; } = 0.2f;
                public float gloves { get; set; } = 0.05f;
                public float head { get; set; } = 0.3f;
                public float pants { get; set; } = 0.2f;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<ConfigData>();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData
            {
                //bool damageArmor = false;
                //float armorDamageAmount = 0.0f;
                Cold = new List<ConfigData.ItemInfo>
                {
                    new ConfigData.ItemInfo { suit = 0.8f, chest = 0.2f, gloves = 0.05f, head = 0.3f, pants = 0.2f }
                },
                Drown = new List<ConfigData.ItemInfo>
                {
                    new ConfigData.ItemInfo { suit = 0.8f, chest = 0.2f, gloves = 0.05f, head = 0.3f, pants = 0.2f }
                }
            };
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        private void Init()
        {
            config = Config.ReadObject<ConfigData>();
            permission.RegisterPermission(permUse, this);
        }

        #endregion

        #region Damage handling

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (!hitinfo.hasDamage) return;

            float deduction;
            //var armorDamaged = false;

            if (hitinfo.damageTypes?.Get(Rust.DamageType.Cold) > 0.0f && entity.ToPlayer().IsSwimming())
            {
                deduction = GetDamageDeduction(entity.ToPlayer(), Rust.DamageType.Cold);
                var newdamage = GetScaledDamage(hitinfo.damageTypes.Get(Rust.DamageType.Cold), deduction);
                hitinfo.damageTypes.Set(Rust.DamageType.Cold, newdamage);
                //armorDamaged = true;
            }

            if (hitinfo.damageTypes?.Get(Rust.DamageType.Drowned) > 0.0f)
            {
                deduction = GetDamageDeduction(entity.ToPlayer(), Rust.DamageType.Drowned);
                var newdamage = GetScaledDamage(hitinfo.damageTypes.Get(Rust.DamageType.Drowned), deduction);
                hitinfo.damageTypes.Set(Rust.DamageType.Drowned, newdamage);
                //armorDamaged = true;
            }

            /*if (armorDamaged && damageArmor)
            {
                foreach (var item in entity.ToPlayer().inventory.containerWear.itemList)
                    if (item.info.name.ToLower().Contains("hazmat")) item.condition = item.condition - armorDamageAmount;
            }*/
        }

        private float GetDamageDeduction(BasePlayer player, Rust.DamageType damageType)
        {
            var deduction = 0.0f;
            foreach (var item in player.inventory.containerWear.itemList)
            {
                if (item.isBroken) continue;

                if (item.info.name.ToLower().Contains("hazmat_suit.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.8f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.8f;
                }
                if (item.info.name.ToLower().Contains("hazmat_helmet.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.3f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.3f;
                }
                if (item.info.name.ToLower().Contains("hazmat_jacket.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.2f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.2f;
                }
                if (item.info.name.ToLower().Contains("hazmat_pants.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.2f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.2f;
                }
                if (item.info.name.ToLower().Contains("hazmat_gloves.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.05f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.05f;
                }
                if (item.info.name.ToLower().Contains("hazmat_boots.item"))
                {
                    if (damageType == Rust.DamageType.Drowned) deduction += 0.05f;
                    if (damageType == Rust.DamageType.Cold) deduction += 0.05f;
                }
            }
            return deduction;
        }

        private float GetScaledDamage(float current, float deduction)
        {
            var newDamage = current - (current * deduction);
            if (newDamage < 0.0f) newDamage = 0.0f;
            return newDamage;
        }

        #endregion
    }
}
