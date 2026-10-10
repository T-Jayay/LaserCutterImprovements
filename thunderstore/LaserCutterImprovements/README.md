# Laser Cutter Improvements

Changes to the Engineer's new alternative primary, the Laser Cutter: quality of life fixes for its range, its beam and its turret hitbox, and one optional balance change.

## Quality of life fixes
Each of these fixes something that is unclear or gets in the way in the game's Laser Cutter.

- **Range indicator.** *In the game:* it is hard to see how far the Laser Cutter reaches (50 m). *With this mod:* while your beam reaches its maximum range without hitting anything, a faint orange band of its range sphere is shown ahead of you, with an orange line on the ground where the range ends. It fades out as soon as the beam hits something. `RangeIndicator`.
- **Beam stops at its target.** *In the game:* the visible beam stops at an enemy it damages, but goes on past an ally it heals, even though the attack stopped there. That misleads you about what the beam is doing. *With this mod:* your visible beam ends where the attack actually stops. `BeamStopsAtTarget`.
- **Turret hitbox size.** *In the game:* you can aim clearly away from an allied Engineer turret and the beam still catches on it and heals it instead of hitting the enemy, because the turret's hitbox for the laser (the beam is about 3 m wide) is much bigger than the turret looks. *With this mod:* `TurretHitboxScale` shrinks allied Engineer turrets' hitbox for your Laser Cutter only. Enemies, drones, MUL-T and other players' attacks keep the game's hitboxes. The default, 100%, is the game's size.

## Balance change (off by default)
- **Heal and damage.** In the game, healing a mechanical ally (turret, drone, MUL-T) stops the beam there. If you expect the beam to carry on and also damage what is behind it, `HealAndDamage` makes it do that: your beam heals the ally and goes on to damage what is behind it. This changes the balance of the Laser Cutter, so it is off by default.

## Settings
In `BepInEx/config/revor.LaserCutterImprovements.cfg`, or in game under Settings → Mod Options → **LaserCutterImprovements** when [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) is installed. Changes apply immediately.
- `RangeIndicator` (default `true`): show the range indicator.
- `BeamStopsAtTarget` (default `true`): end the visible beam where the attack stops.
- `TurretHitboxScale` (default `1`, the game's size; from `0.1` to `1`): the size of allied Engineer turrets' hitbox for your Laser Cutter.
- `HealAndDamage` (default `false`): heal mechanical allies without stopping the beam. Changes the balance.

## Multiplayer
Client-side: only you need it, and you can join any lobby with or without it. In this game the player who fires the Laser Cutter decides what it hits, so the settings change your own beam for everyone: others see your heals and damage as usual. The visible beam and the range indicator only change on your screen; other Engineers' lasers look as in the game.

Source and bug reports: [github.com/T-Jayay/LaserCutterImprovements](https://github.com/T-Jayay/LaserCutterImprovements).
