# LaserCutterImprovements

A Risk of Rain 2 (BepInEx) mod for the Engineer's Laser Cutter, its new alternative primary: quality of life fixes (a range indicator at maximum range, a visible beam that ends where the attack stops, a smaller hitbox for allied Engineer turrets for your own beam) and one optional balance change (healing mechanical allies without stopping the beam).

- Store page: [revoreverse/LaserCutterImprovements](https://thunderstore.io/c/riskofrain2/p/revoreverse/LaserCutterImprovements/). Its source, [thunderstore/LaserCutterImprovements/README.md](thunderstore/LaserCutterImprovements/README.md), describes every feature and setting for players; this file is for development.
- [Modding guide](https://github.com/T-Jayay/DroneImprovements/blob/main/docs/MODDING_GUIDE.md) (in the DroneImprovements repo): how this mod, [DroneImprovements](https://github.com/T-Jayay/DroneImprovements), [SprintImprovements](https://github.com/T-Jayay/SprintImprovements), [MarkAllSeen](https://github.com/T-Jayay/MarkAllSeen) and [AllyClarity](https://github.com/T-Jayay/AllyClarity) are set up, built, tested and published.
- License: [the Unlicense](LICENSE) (public domain).

## Building

You need the [.NET SDK](https://dotnet.microsoft.com/download) 8 or later. The game, BepInEx, Risk of Options and the MonoMod hook types (`MMHOOK.RoR2`) are referenced from NuGet (the game's publicized reference assemblies, `RiskOfRain2.GameLibs`, come from the [BepInEx feed](https://nuget.bepinex.dev/)), so the first build needs internet access but not the game.

```
dotnet build LaserCutterImprovements.sln -c Release
```

Each build copies `LaserCutterImprovements.dll` and its PDB into a mod manager profile for testing, as `<ProfileDir>\BepInEx\plugins\LaserCutterImprovements\`, if that profile exists. The test profile needs BepInExPack and HookGenPatcher (which generates `MMHOOK_RoR2.dll` when the game starts), and Risk of Options for the in-game settings (players can use the mod without it). Set these properties on the command line or as environment variables:

| Property | Default | Example |
|---|---|---|
| `ProfileDir` | Thunderstore Mod Manager's `Test` profile: `%APPDATA%\Thunderstore Mod Manager\DataFolder\RiskOfRain2\profiles\Test` | an r2modman profile named `Dev`: `-p:ProfileDir="C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\Dev"` |
| `DeployToProfile` | `true` | `-p:DeployToProfile=false` builds without deploying |

Close the game before building: it keeps the plugin locked, so the copy fails. The build takes the version from `thunderstore/LaserCutterImprovements/manifest.json` and embeds `thunderstore/LaserCutterImprovements/icon.png` for the Risk of Options mod list.

## Releasing

1. Set the new version in `thunderstore/LaserCutterImprovements/manifest.json` (`version_number`) and in `PluginVersion` in `src/LaserCutterImprovements/LaserCutterImprovementsPlugin.cs`, and list the changes under `## <version>` in `thunderstore/LaserCutterImprovements/CHANGELOG.md`. An uploaded version can't be changed, so any change, even to the manifest alone, needs a new version.
2. Commit.
3. `python tools/package.py` (Python 3.8 or later) checks the package against Thunderstore's rules and the version against `PluginVersion` and the changelog, builds the mod and writes `dist/LaserCutterImprovements-<version>.zip`. It refuses to package uncommitted changes or to overwrite an existing zip; `--force` skips both checks, for test builds only.
4. Upload the zip at https://thunderstore.io/package/create/ with the team **revoreverse** and the community Risk of Rain 2, and tick the **AI Generated** category.

The full checklist, including the first push to GitHub, is under Publishing in the [modding guide](https://github.com/T-Jayay/DroneImprovements/blob/main/docs/MODDING_GUIDE.md#publishing).

## How it works

The Laser Cutter is the entity state `EntityStates.Engi.EngiWeapon.EngiLaserCutter` (its subclasses `EngiLaserCutter2` and `EngiLaserCutter3` are the later, stronger stages). Its values come from the entity state configurations: 50 m range (`laserDistance`) and a 1.5 m radius (`laserRadius`).

- **The beam's attack.** `FireLaser` fires a `BulletAttack` with `smartCollision` (a ray, plus a sphere cast of `laserRadius` against characters), on the owner only (`isAuthority`); a client sends the hits to the server through the game's own bullet message. It stops at the first character it hits. Hitting a mechanical ally of the same team calls `GlobalEventManager.OnHitAll` on the server, where the damage macro `EngiLaserCutter.DamageMacroEngiHealMechanical` heals it (with no friendly fire there is no damage to reject). An IL hook in `FireLaser` gives a player's `BulletAttack` this mod's filter and hit callbacks just before it is fired (`LaserHooks.cs`, `LaserHits.cs`). AI lasers stay as in the game.
- **`TurretHitboxScale`.** The filter calls the game's default filter, then, for an allied `EngiTurretBody` or `EngiWalkerTurretBody`, checks the hit against the turret's hitbox scaled around each hurtbox's centre. The game's hit region is every point within `laserRadius` of a hurtbox; scaling it by `s` around a centre is the same as scaling the ray by `1/s` around that centre, so the turret counts as hit when the inversely scaled ray passes within `laserRadius` of one of its hurtboxes. The distance from a segment to a collider is found by alternating `Collider.ClosestPoint` and the closest point on the segment, which converges for convex colliders. At 1 the filter does nothing extra. A rejected turret doesn't stop the beam.
- **`HealAndDamage`.** The hit callback calls the game's default one (effects, damage or the heal on the server) and then lets the beam go on past a mechanical ally of the same team, the same test as the damage macro.
- **`BeamStopsAtTarget` and `RangeIndicator`.** The visible beam's end is placed every frame by `UpdateLaser` with a thin `Util.CharacterRaycast`, which misses what the beam's radius hits. An `On.` hook after it, for the local player's own laser only (`isAuthority` and player-controlled; other players' settings aren't known), fires a probe `BulletAttack` set up like the real one, with the same filter and a hit callback that only records where the beam stops, deals no damage and plays no effects (`LaserHits.Probe`). The beam's end is moved there; if it stops nowhere, `RangeIndicator` is shown.
- **`RangeIndicator.cs`** is a component added to the player's body the first time it is shown. It draws a band of the range sphere around the aim direction (50 degrees wide, from 15 degrees above the aim down to straight below) as a mesh rebuilt each frame it is shown, with a copy of a material made from the game's `Hopoo Games/FX/Cloud Remap` shader (found with `LegacyShaderAPI.Find`; alpha-blended, drawn on both sides, with a white texture, no colour ramp and no clouds, so it is plain light orange). Vertex alpha keeps the band faint and fades its sides and top. A `LineRenderer` on a second object traces where the sphere meets the ground: for every 2.5 degrees of yaw across the band, it walks down the sphere's vertical circle in 10-degree steps with `Physics.Linecast` on the world layer and takes the first hit (each yaw starts 20 degrees above where the one before found ground, to keep it cheap), and the line is the run of yaws around the aim direction that have ground. Both fade out together over 0.15 s when they haven't been shown in a frame.
- Client-side: no content and no network messages, so it isn't on the game's network mod list.
- The hook types come from NuGet `MMHOOK.RoR2` (compile only); players get `MMHOOK_RoR2.dll` from HookGenPatcher, a manifest dependency. Risk of Options is optional (a soft dependency): `RiskOfOptionsCompat.cs` is only used when it is installed.

## Icon

`tools/make_icon.py` (needs Pillow) draws the store icon `thunderstore/LaserCutterImprovements/icon.png`. Its helpers are copied from DroneImprovements' `tools/make_icons.py`; keep the copies identical.
