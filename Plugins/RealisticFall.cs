using System;

namespace Oxide.Plugins
{
    [Info("RealisticFall", "Wulf/lukespragg", "2.0.2", ResourceId = 855)]
    [Description("Modifies the maximum fall height for player deaths")]

    class RealisticFall : RustPlugin
    {
        #region Initialization

        int maxFallFeet;

        protected override void LoadDefaultConfig() => Config["MaxFallFeet"] = maxFallFeet = GetConfig("MaxFallFeet", 12);

        void Init() => LoadDefaultConfig();

        #endregion

        #region Damage Modification

        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player == null || info == null) return;

            if (info.damageTypes.Total() <= 0) return;
            var damageType = info.damageTypes.GetMajorityDamageType();
            if (damageType != Rust.DamageType.Fall) return;

            var oldDamage = info.damageTypes.Total();
            var newDamage = (player.Health() / maxFallFeet) * (oldDamage * 0.35f);

            info.damageTypes.Set(damageType, newDamage);
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
    }
}
