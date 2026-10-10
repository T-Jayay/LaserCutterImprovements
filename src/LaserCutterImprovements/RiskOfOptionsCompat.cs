using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace LaserCutterImprovements
{
    /// <summary>
    /// In-game settings through Risk of Options, a soft dependency. Its types appear only inside the body of the
    /// non-inlined <see cref="Init"/>, so they are only loaded when it is installed.
    /// </summary>
    internal static class RiskOfOptionsCompat
    {
        public const string Guid = "com.rune580.riskofoptions";

        // The store icon, embedded from thunderstore/LaserCutterImprovements/icon.png by Directory.Build.targets.
        private const string IconResource = "LaserCutterImprovements.icon.png";

        public static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(Guid);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            ModSettingsManager.SetModDescription("Changes to the Engineer's Laser Cutter: a range indicator at "
                + "maximum range, a beam that ends where it hits, a smaller turret hitbox for your beam, and healing "
                + "allies while damaging what is behind them. Only your own Laser Cutter changes, and nobody else "
                + "needs the mod.");
            ModSettingsManager.SetModIcon(EmbeddedSprites.Load(IconResource));

            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.RangeIndicator));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.BeamStopsAtTarget));
            ModSettingsManager.AddOption(new StepSliderOption(PluginConfig.TurretHitboxScale, new StepSliderConfig
            {
                min = 0.1f,
                max = 1f,
                increment = 0.05f,
                FormatString = "{0:0%}",
            }));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.HealAndDamage));
        }
    }
}
