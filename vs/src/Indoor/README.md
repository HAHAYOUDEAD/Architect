# Indoor and Balance

Hooks into Architect: `ArcMain` (`IndoorFeatures`, `MaterialWeights.Apply`), `Settings` (two sections), `Localization.json` (`ARC_Indoor_*`, `ARC_Balance_*`).

`BuildingTracker` finds the building the player is in (floor tiles with the same `interiorClusterID`) and updates everything 4 times a second.

| File | Purpose |
|---|---|
| `BuildingAnalyzer` | Walls, roof, doors and windows of a building; its sound level and heat profile |
| `SoundInsulation` | Outdoor sound occlusion and wind volume |
| `SnowKillers` | Particle killer boxes over building interiors |
| `SnowHider` | Hides weather snow while inside with doors closed |
| `LightFixes` | Shadows for lights inside, fire flicker |
| `DynamicTemperatureBridge` | Optional DynamicTemperature integration (reflection, no build reference) |
| `Balance/MaterialWeights` | Weight of logs and planks |
| `Balance/BuildingCosts` | Log and plank cost of each part |

After a game update, check the killer struct offsets in `SnowKillers` and the wind parameter ids in `SoundInsulation`.
