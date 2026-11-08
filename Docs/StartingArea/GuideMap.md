# Cinder Sanctuary: starting map guide map

![Guide map](GuideMap.png)

Scope: **the sanctuary only**. It is one compact bowl (about 50 × 80 m walkable) on a 128 m terrain, closed by hills and cliffs on every side. There is nothing else around it. Every number here comes from `generate.py`, which writes the terrain heights, the texture painting, the grass and every prop placement.

## Flow

```
[1] Entry Nook (spawn) ──► [2] Tollgate Court ──► [3] Cinder Amphitheater ──► [4] Sanctuary Gate (sealed)
```

| # | Zone | Centre (x, z) | Purpose | Key pieces |
|---|------|---------------|---------|-----------|
| 1 | **Entry Nook** (spawn) | (0, 33) | The player starts facing the arch. The shrine, the gate and the tower are framed straight ahead. | 2 standing torches, slab path, broken pillar, rubble, a dead tree |
| 2 | **Tollgate Court** | (0, 19) | Pinch point: a 4.5 m arch set in a line of ruin walls that closes the bowl. | Tollgate, 6 ruin walls, 2 banners, 2 braziers, a processional slab path, an abandoned toll post (barrel, crates, sacks, amphora) |
| 3 | **Cinder Amphitheater** | (0, 0) | The arena. The shrine sits at the centre with about 19 m of clear floor. | Amphi_Floor + CinderShrine, 7 tier segments (0/30/120/150/180/210/330°), collapsed tiers at 60° (rubble), 4 pillars, weeping guardians at the gate, 4 candle sets |
| 4 | **Sanctuary Gate** | (0, −24) | The promise of what comes next. It is sealed for now, and its frame is set into the cliff. | SanctuaryGate + 2 closed leaves, 2 × Cliff_A flanks, a cliff spire, 2 braziers, **Cinder Tower** (pier on a hilltop with a brazier beacon) |

Openings in the amphitheater ring: **north** (75–105°) from the court, **south** (225–315°) to the gate, and a narrow gap at 60° where the tiers collapsed.

## Landmarks

- **Macro:** the Cinder Tower beacon above the gate. You see it through the arch from the spawn, and it stays on the skyline inside the arena.
- **Meso:** the tollgate arch with its banners and braziers, the shrine glow and the gate braziers.
- **Micro:** candles around the shrine, torches at the entry.

## Terrain

- **Terrain object:**
  - Position (−64, −10, −62), size 128 × 100 × 128.
  - Heightmap 257 (0.5 m per sample), alphamap 256, detail 256.
  - Asset: `Assets/World/Terrain/CinderHollow_TerrainData.asset`.
- **Walkable areas:**
  - Entry nook, court, arena, gate approach and gate court are flat or nearly flat, at y 0 (arena floor −0.12).
- **Slopes:**
  - Around those areas the ground rises in noisy hills, 5–30 m high.
  - The south wall behind the gate is the steepest and tallest. The tower hill sits behind it at about y 32.
- **Bowl rim:**
  - A round ring 36–62 m out from the centre rises another ~36–46 m.
  - It closes the bowl and hides the terrain edges.

### Texture layers

| Layer | Where |
|------|-------|
| Grass007 | Base for flat ground |
| Ground037 (dirt) | Noise patches, scree at the foot of slopes, worn ring around the arena floor, path fringe |
| Gravel043 | Processional path from the entry to the gate |
| Rock064 | Slopes above about 31–43° and high ground |
| Moss002 | Up the slopes and on high ground |
| Ground068 (dark soil) | Noise patches |

Grass detail (`T_GrassBlades.png`) is painted only on flat, grass-dominant ground. It stays off the paths, the arena floor and the wall line.

## Lighting

- **Sun:** low golden-hour sun from the south-west, with warm, thick fog.
- **Point lights (10 active):** shrine glow, tower beacon, gate wash, and the braziers and torches. Candles rely on their emissive material only.

## Scene organisation

`StartArea/` contains `01_Entry`, `02_TollgateCourt`, `03_CinderAmphitheater`, `04_SanctuaryGate`, `05_Boundary`, `06_Scatter` and `Lighting`. Inside `PlayerRig`, the `Player` stands at the spawn (0, 0.98, 34.5) facing south, with `Main Camera` 3.5 m behind.

**Render hygiene:**
- No mesh is fully buried in the terrain.
- The gate flank cliffs and two boundary rocks are 20–30% visible on purpose, as rock outcrops.
- Scatter keeps clear of every big mesh's footprint.

To change the layout, edit `generate.py`, run `python generate.py _generated`, and re-import the outputs in Unity.
