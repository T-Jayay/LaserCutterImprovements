using BepInEx.Configuration;

namespace LaserCutterImprovements
{
    /// <summary>
    /// The settings in BepInEx/config/revor.LaserCutterImprovements.cfg. Each player's own settings apply to their
    /// own Laser Cutter, immediately. Section and key names are stored in players' config files: renaming or moving
    /// one resets that setting for everyone, so they must never change.
    /// </summary>
    internal static class PluginConfig
    {
        private const string Section = "General";

        public static ConfigEntry<bool> RangeIndicator { get; private set; }
        public static ConfigEntry<bool> BeamStopsAtTarget { get; private set; }
        public static ConfigEntry<float> TurretHitboxScale { get; private set; }
        public static ConfigEntry<bool> HealAndDamage { get; private set; }

        public static void Init(ConfigFile config)
        {
            RangeIndicator = config.Bind(Section, "RangeIndicator", true,
                "While your Laser Cutter reaches its maximum range without hitting anything, show a faint part of "
                + "its range sphere ahead of you and a line on the ground where the range ends.");
            BeamStopsAtTarget = config.Bind(Section, "BeamStopsAtTarget", true,
                "Your Laser Cutter's beam ends where the attack stops (for example at an ally it heals) instead of "
                + "continuing past it.");
            TurretHitboxScale = config.Bind(Section, "TurretHitboxScale", 1f,
                new ConfigDescription(
                    "The size of allied Engineer turrets' hitbox for your Laser Cutter, so the beam heals a turret "
                    + "only when you aim at it, not at an enemy next to it. 1 = the game's (100%), 0.5 = half. "
                    + "Enemies, drones and MUL-T keep the game's hitbox.",
                    new AcceptableValueRange<float>(0.1f, 1f)));
            HealAndDamage = config.Bind(Section, "HealAndDamage", false,
                "Your Laser Cutter heals a mechanical ally (turrets, drones, MUL-T) and goes on through it to "
                + "damage what is behind it. In the game, healing an ally stops the beam there.");
        }
    }
}
