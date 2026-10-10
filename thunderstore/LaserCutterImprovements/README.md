# Laser Cutter Improvements

Changes to the Engineer's new alternative primary, the Laser Cutter: see its range, see what the beam really hits, and stop it from healing a turret next to you instead of hitting the enemy you aim at.

## Features
- **Range indicator.** The Laser Cutter reaches 50 m. While your beam reaches that far without hitting anything, a faint orange band of its range sphere is shown ahead of you, with an orange line on the ground where the range ends. It fades out as soon as the beam hits something.
- **Beam stops at its target.** The game draws the beam with a thin line, but the attack is about 3 m wide, so the beam can heal an ally or hit an enemy while the visible beam goes on past it. With this mod your beam ends where the attack actually stops.
- **Turret hitbox for your beam.** Because of that width, an allied Engineer turret within about 1.5 m of your aim, or right next to you, takes the hit and gets healed instead of the enemy you aim at. `TurretHitboxScale` shrinks allied Engineer turrets' hitbox for your Laser Cutter only. Enemies, drones, MUL-T and other players' attacks keep the game's hitboxes.
- **Heal and damage.** In the game, healing a mechanical ally (turret, drone, MUL-T) stops the beam there. With `HealAndDamage` on, your beam heals the ally and goes on to damage what is behind it.

## Settings
In `BepInEx/config/revor.LaserCutterImprovements.cfg`, or in game under Settings → Mod Options → **LaserCutterImprovements** when [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) is installed. Changes apply immediately.
- `RangeIndicator` (default `true`): show the range indicator.
- `BeamStopsAtTarget` (default `true`): end the visible beam where the attack stops.
- `TurretHitboxScale` (default `1`, the game's size; from `0.1` to `1`): the size of allied Engineer turrets' hitbox for your Laser Cutter.
- `HealAndDamage` (default `false`): heal mechanical allies without stopping the beam.

## Multiplayer
Client-side: only you need it, and you can join any lobby with or without it. In this game the player who fires the Laser Cutter decides what it hits, so the settings change your own beam for everyone: others see your heals and damage as usual. The visible beam and the range indicator only change on your screen; other Engineers' lasers look as in the game.

Source and bug reports: [github.com/T-Jayay/LaserCutterImprovements](https://github.com/T-Jayay/LaserCutterImprovements).
