using BepInEx;
using BepInEx.Logging;

namespace LaserCutterImprovements
{
    /// <summary>
    /// Changes to the Engineer's Laser Cutter primary, for the player's own beam: a range indicator when the beam
    /// reaches its maximum range (<see cref="RangeIndicator"/>), the visible beam ending where the attack stops, a
    /// smaller hitbox for allied Engineer turrets and healing a mechanical ally without stopping the beam
    /// (<see cref="LaserHits"/>), all hooked in <see cref="LaserHooks"/>. Client-side: the game lets the owner of the
    /// laser decide what it hits, so there are no network messages and the mod stays off the network mod list.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(RiskOfOptionsCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class LaserCutterImprovementsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "revor.LaserCutterImprovements";
        public const string PluginName = "LaserCutterImprovements";
        public const string PluginVersion = "1.0.0"; // tools/package.py checks this against the manifest

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Init(Config);
            LaserHooks.Init();
            if (RiskOfOptionsCompat.IsInstalled)
            {
                RiskOfOptionsCompat.Init();
            }
        }
    }
}
