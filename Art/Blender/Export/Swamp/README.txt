BROKEN OATH - SWAMP (Arena 3): THE ROOTBOUND boss, its hazards, and the swamp environment kit
=========================================================================================

Sources:  Art/Blender/Rootbound.blend           (boss + hazards)
          Art/Blender/BrokenOath_SwampKit.blend  (environment kit)
Units: metres. FBX: -Z forward, Y up, FBX units scale, no leaf bones, primary bone axis Y, secondary X.
Textures: URP Lit sets. MetallicSmoothness = R metal, A smoothness. Normal maps are OpenGL (Unity default).
Pivots sit on the ground (z = 0) unless a note below says otherwise.


1. BOSS  (Export/Swamp/Boss)
----------------------------
File                      Tris    Size (m)          Materials                              Rig
SK_Rootbound.fbx          37207   8.3 x 7.5 x 7.4   M_Rootbound_Bark / _Moss / _Heart      56 bones, Generic
SM_Rootbound_Heart.fbx     2208   0.7 x 0.6 x 0.6   M_Rootbound_Heart                      (standalone heart / pickup / VFX)
SK_RootTendril.fbx         3285   2.4 x 2.3 x 5.4   M_RootHazard_Bark                      10 bones, Generic
SK_GraspingTree.fbx        5874   8.8 x 4.1 x 9.3   M_RootHazard_Bark, M_Rootbound_Heart   15 bones, Generic
SM_RootSpike_A.fbx          543   1.3 x 1.8 x 1.9   M_RootHazard_Bark                      + 1 UCX
SM_RootSpike_B.fbx          861   1.6 x 1.8 x 2.6   M_RootHazard_Bark                      + 1 UCX
SM_RootSpike_C.fbx         1101   1.6 x 1.9 x 3.4   M_RootHazard_Bark                      + 1 UCX
SM_RootSpike_Burst.fbx      480   3.0 x 3.0 x 0.1   M_RootBurst_Decal (alpha clip)         ground crack/burst under spikes
SM_RootWall_A.fbx          6481   5.0 x 2.7 x 4.8   M_RootHazard_Bark                      + 1 UCX
SM_RootWall_B.fbx          6505   5.0 x 2.7 x 5.1   M_RootHazard_Bark                      + 1 UCX
SM_RootWall_C.fbx          6553   5.0 x 2.7 x 5.8   M_RootHazard_Bark                      + 1 UCX

Material -> texture set (Boss folder, *_BaseColor / _Normal / _MetallicSmoothness / _AO / _Emission):
  M_Rootbound_Bark   -> T_Rootbound_Bark_*    2K   emission = faint amber cracks, intensity ~1.2
  M_Rootbound_Moss   -> T_Rootbound_Moss_*    1K   alpha clip (cutoff 0.5), Render Face = Both, no emission map
  M_Rootbound_Heart  -> T_Rootbound_Heart_*   1K   amber heart, emission intensity 2-4 (needs the glow or it reads as marble)
  M_RootHazard_Bark  -> T_RootHazard_Bark_*   2K   emission ~1.0
  M_RootBurst_Decal  -> T_RootBurst_*         1K   alpha clip, emission ~1.0

Sockets (bones on SK_Rootbound, parent VFX / hitboxes to them):
  Socket_Head, Socket_Claw_L, Socket_Claw_R, Socket_Heart

Boss clips (30 fps; length in frames; impact = frame the hit should land on):
  Clip                     Frames  Impact  Loop   Notes
  A_Rootbound_Dormant         120    -     yes    half-submerged wait pose (root bone 2.4 m down)
  A_Rootbound_Emerge          120    -     no     rises from under the water, ends in a roar
  A_Rootbound_Idle             90    -     yes
  A_Rootbound_SlamL            66    27    no     left claw slam
  A_Rootbound_SlamR            66    27    no     right claw slam
  A_Rootbound_Sweep            80    38    no     horizontal arm sweep
  A_Rootbound_Summon           95    52    no     spawn tendrils / spikes on the impact frame
  A_Rootbound_Regen            90    -     yes    heart pulses (heart bone scales) - vulnerable/heal window
  A_Rootbound_Stagger          45    -     no
  A_Rootbound_Submerge         60    -     no     sinks fully under (root bone ~6.9 m down)
  A_Rootbound_Death           150    -     no
  A_Rootbound_DeadPose         30    -     yes    hold after Death

Hazard clips:
  A_RootTendril_Erupt          30    9     no     bursts out of the ground (overshoot on f9)
  A_RootTendril_Sway           60    -     yes
  A_RootTendril_Whip           45    22    no
  A_RootTendril_Retract        24    -     no
  A_GraspingTree_Idle          90    -     yes
  A_GraspingTree_Slam          50    33    no     both hands slam the ground
  A_GraspingTree_Recover       50    -     no

Vertical movement (rising/sinking) lives on the "root" bone inside the clips. Keep Apply Root Motion OFF
and leave the GameObject where it is.


2. ENVIRONMENT KIT  (Export/Swamp/Kit)
--------------------------------------
Six 2K material sets. Every mesh uses only these:
  M_Swamp_Bark      -> T_Swamp_Bark_*        tree / root / log bark
  M_Swamp_WoodMetal -> T_Swamp_WoodMetal_*   strip atlas: rotten planks (left 60%), rusted iron (right 40%)
  M_Swamp_Peat      -> T_Swamp_Peat_*        dark peat / turf tops
  M_Swamp_Mud       -> T_Swamp_Mud_*         wet mud (high smoothness)
  M_Swamp_Stone     -> T_Swamp_Stone_*       mossy stone (walls, statues, standing stones, rocks)
  M_Swamp_Foliage   -> T_Swamp_Foliage_*     alpha-clip atlas: moss, vines, reeds, grass, lily pads, mushroom caps,
                                             glowing caps, twigs, banner cloth, bones, rope, lantern glass
                                             (+ T_Swamp_Foliage_Emission)

Asset                       Tris  Size (m) X x Y x H   UCX   Notes
SM_SwampTree_A              2957  6.2 x 5.8 x 9.5        2   hero tree, moss + canopy cards baked in
SM_SwampTree_B              2042  5.3 x 4.7 x 8.2        2
SM_SwampTree_C              3818  7.1 x 6.9 x 12.1       2   tallest
SM_SwampTree_D              1994  4.4 x 4.3 x 6.5        2   dead / snapped
SM_MangroveRoots_A          2836  5.7 x 4.9 x 4.5        1
SM_MangroveRoots_B          2954  7.0 x 6.2 x 2.1        1
SM_Stump_A                   742  2.3 x 2.3 x 1.3        1
SM_Stump_B                  1085  2.6 x 2.8 x 1.9        1
SM_Stump_C                  1128  3.1 x 3.5 x 1.1        1
SM_Log_A                     756  5.0 x 1.3 x 0.8        1
SM_Log_B                    1386  7.3 x 2.2 x 1.9        1
SM_Log_Hollow                746  4.5 x 1.6 x 1.0        1
SM_HangingMoss_Short          48  0.8 x 0.8 x 1.0        -   pivot at the TOP (hang from branches)
SM_HangingMoss_Medium         80  0.8 x 0.8 x 2.0        -   pivot at the top
SM_HangingMoss_Long          168  0.8 x 0.8 x 3.1        -   pivot at the top
SM_CanopyCards_A              30  3.2 x 4.3 x 3.3        -
SM_CanopyCards_B              48  4.4 x 4.2 x 3.3        -
SM_Reeds_A                    42  1.1 x 1.4 x 1.6        -
SM_Reeds_B                    72  2.2 x 1.9 x 2.2        -
SM_LilyPads_A                 18  3.1 x 3.2 x 0.0        -   lay on the water surface
SM_GrassTuft_A                36  1.0 x 1.0 x 0.7        -
SM_GrassTuft_B                72  1.5 x 1.6 x 0.9        -
SM_Mushrooms_A              1299  1.3 x 0.9 x 0.5        -   shelf fungus on a log chunk
SM_Mushrooms_B               826  0.6 x 0.5 x 0.2        -
SM_Mushrooms_C              1239  1.4 x 1.2 x 0.7        -   glowing amber caps (emission)
SM_BogRock_A                 600  2.0 x 2.0 x 1.2        1
SM_BogRock_B                 616  2.9 x 2.5 x 1.9        1
SM_BogRock_C                 606  4.3 x 3.1 x 2.6        1
SM_BogRock_D                 606  2.2 x 1.6 x 2.5        1
SM_MudMound_A                608  4.5 x 3.4 x 2.3        1
SM_MudBank_A                 936  6.0 x 3.2 x 1.9        3   shoreline piece, sink partly into the water
SM_MudBank_B                 936  5.1 x 3.6 x 1.9        3
SM_SunkenWall_A             1162  4.3 x 2.8 x 3.1        3
SM_SunkenWall_B              678  3.7 x 1.9 x 2.1        3
SM_SunkenWall_C             1130  5.0 x 2.9 x 3.5        3
SM_SunkenColumn              600  2.9 x 2.3 x 4.0        2   leaning column + broken drum
SM_KnightStatue_Sunken      1600  1.7 x 2.0 x 2.9        2   kneeling knight, half sunk
SM_BannerPole_Broken         581  1.1 x 1.4 x 4.4        1   torn banner (foliage atlas)
SM_CartWreck                1108  3.2 x 4.6 x 1.9        2
SM_Debris_A                  318  2.4 x 3.9 x 1.1        1   broken spears / boards
SM_Debris_B                  422  1.3 x 1.3 x 0.5        1   helmet + shield
SM_Debris_C                  394  1.7 x 1.5 x 2.7        -   spear stuck upright with helmet
SM_SkeletonRoots_A          1421  1.8 x 2.1 x 1.7        -   skeleton grown into roots
SM_SkeletonRoots_B          1476  2.7 x 2.4 x 1.4        -
SM_Rowboat_Wrecked           416  2.6 x 3.6 x 1.8        1
SM_Walkway_Straight         1182  1.9 x 4.5 x 1.6        1   deck at +0.5 m; 4 m long; snaps along Unity -Z
SM_Walkway_Broken           1036  2.2 x 4.5 x 1.7        2   same snapping, planks missing
SM_Walkway_Ramp              940  1.8 x 4.2 x 1.5        1   0.5 m at the pivot, down to ground level at the far end
SM_PostRope_A                473  0.3 x 2.2 x 1.8        2   two posts + slack rope
SM_LanternPost_A             454  0.3 x 1.0 x 3.1        1   gallows post with cage lantern
SM_LanternPost_B             452  0.4 x 0.9 x 2.8        1   bent post with lantern
SM_CageLantern_Hanging       396  0.3 x 0.3 x 1.1        -   pivot at the chain TOP (hang it)
SM_Signpost_Rotten           203  1.6 x 0.7 x 2.7        1
SM_StandingStone_A          1218  2.9 x 2.1 x 4.7        1   arena ring menhir, roots wrapped
SM_StandingStone_B          1320  2.9 x 2.7 x 3.9        1
SM_AltarSlab                1838  7.4 x 5.9 x 3.0        2   peat island + altar slab for the arena centre (5 materials)

Walkway snapping: walkways run from the pivot toward Unity -Z (Blender -Y). Each piece is 4 m long,
so the next piece goes at local (0, 0, -4). Deck height is 0.5 m above the pivot.


3. UNITY NOTES
--------------
Model import
  - Scale Factor 1, Convert Units ON. Static meshes: Rig = None; skinned: Rig = Generic (NOT Humanoid).
  - Generate Lightmap UVs ON for the static kit pieces you lightmap (trees, banks, walls, altar).
  - Materials: Location = Use External Materials (Legacy) or remap to URP/Lit materials built from the texture sets.
  - Animation: import the clips from each SK_ FBX. Loop Time ON only for the clips marked "yes" above
    (Dormant, Idle, Regen, DeadPose, Tendril Sway, GraspingTree Idle). Anim compression: Optimal is fine.
  - Impact frames: add an Animation Event on the impact frame (e.g. SlamL f27) that turns on the damage
    hitbox / spawns VFX. Frame / 30 = event time in seconds.

Textures
  - *_Normal -> Texture Type: Normal map.
  - *_MetallicSmoothness, *_AO -> sRGB OFF.  *_BaseColor, *_Emission -> sRGB ON.
  - URP/Lit: Metallic Map = *_MetallicSmoothness, Smoothness Source = Metallic Alpha, Occlusion = *_AO.

Alpha clip (M_Swamp_Foliage, M_Rootbound_Moss, M_RootBurst_Decal)
  - Alpha Clipping ON, threshold 0.5, Render Face = Both. Leave Surface Type Opaque (do not use Transparent).

Emission (URP intensity; HDR colour = white x intensity, emission map in the slot)
  - M_Rootbound_Heart  2-4 (pulse it during Regen)
  - M_Rootbound_Bark   ~1.2 (faint crack glow)
  - M_RootHazard_Bark / M_RootBurst_Decal  ~1.0
  - M_Swamp_Foliage    1-2 (glow mushrooms + lantern glass). Add a small warm point light
                       (amber, ~1.5 m range) inside lanterns. Amber is the only warm accent in the zone.

Collision
  - UCX_<AssetName>_NN children are convex collision hulls. In the prefab: add a MeshCollider (Convex ON)
    using that mesh and DISABLE / remove its MeshRenderer. Thin foliage, mushrooms and small props have no
    UCX on purpose (no collision, or add a simple primitive if needed).
  - The boss and hazards: build capsule/box colliders on bones (Socket_* for hits). Spikes and root walls
    carry UCX hulls; raise them from below the ground for the arena close-off effect.

Mood reference: black water, dark peat, sickly green-teal fog (exponential fog, low density),
desaturated tonemapping; only warm accents are the heart, the cracks, the lanterns and the glowing caps.
