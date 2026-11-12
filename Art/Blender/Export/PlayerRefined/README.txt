Lich Player - Refined
=====================

Source file: Art/Blender/Lich_PlayerRefined.blend
Base model:  Assets/Character/MainPlayerModel/Assets/Character/Lich_Character.fbx (unchanged, rig reused as-is)

FILES
-----
SK_Lich_PlayerRefined.fbx                  skinned mesh + armature (no animation)
T_LichRefined_Armour_BaseColor.png         sRGB
T_LichRefined_Armour_Normal.png            linear, OpenGL (Y+)
T_LichRefined_Armour_MetallicSmoothness.png linear, R = metallic, A = smoothness
T_LichRefined_Armour_AO.png                linear
T_LichRefined_Cloth_BaseColor.png
T_LichRefined_Cloth_Normal.png
T_LichRefined_Cloth_MetallicSmoothness.png
T_LichRefined_Cloth_AO.png
T_LichRefined_Bone_BaseColor.png
T_LichRefined_Bone_Normal.png
T_LichRefined_Bone_MetallicSmoothness.png
T_LichRefined_Bone_AO.png
T_LichRefined_Bone_Emission.png            sRGB (green eye glow + faint runes on spine/back spikes)
T_LichRefined_Gear_BaseColor.png
T_LichRefined_Gear_Normal.png
T_LichRefined_Gear_MetallicSmoothness.png
T_LichRefined_Gear_AO.png
T_LichRefined_Gear_Emission.png            sRGB (magenta sigil, reliquary, chest marks, oath token)
All textures 2048 x 2048 PNG.

MATERIALS -> TEXTURES (one UV set, "UVMap")
--------------------------------------------
M_LichRefined_Armour  blued steel plates, silver trim, rivets, buckles      BaseColor / Normal / MetallicSmoothness / AO
M_LichRefined_Cloth   hood, coat strips, sash, trousers, bandage wraps      BaseColor / Normal / MetallicSmoothness / AO
M_LichRefined_Bone    skull, ribs, spine, back spikes, claws, skin, eyes     BaseColor / Normal / MetallicSmoothness / AO / Emission
M_LichRefined_Gear    leather belt, pouches, straps, gloves, iron shackle
                      and chain, reliquary, sigil                           BaseColor / Normal / MetallicSmoothness / AO / Emission

STATS
-----
Triangles: 86,237 (budget 90,000)
Vertices:  44,321
Materials: 4
Skeleton:  33 bones, identical to Lich_Character.fbx (names, hierarchy, rest pose, orientation, scale, order).
Skinning:  max 4 influences per vertex, weights normalised, no unweighted vertices.

TEXTURE NOTES
-------------
- BaseColor, Normal, MetallicSmoothness and Emission were baked at 2048.
- AO was baked at 1024 (denoised/blurred) and upscaled to 2048 so all delivered maps share one size.
- BaseColor has no AO multiplied in; use the AO map in the Occlusion slot.

UNITY IMPORT (URP Lit)
----------------------
Model (SK_Lich_PlayerRefined.fbx):
- Rig: Animation Type = Humanoid. Avatar Definition = Copy From Other Avatar using the existing
  Lich_Character avatar, or Create From This Model (bone mapping is identical).
- Scale Factor 1, Convert Units on. Import Animation off.
- Materials: Location = Use External Materials (Legacy) or extract, then wire the maps below.
- Normals: Import. Tangents: Calculate Mikktspace.

Textures:
- *_BaseColor, *_Emission: Texture Type Default, sRGB on.
- *_Normal: Texture Type Normal map.
- *_MetallicSmoothness, *_AO: sRGB off.
- Max Size 2048, compression High Quality (BC7) for MetallicSmoothness (alpha used).

Material (URP/Lit, Workflow Metallic, Opaque):
- Base Map            = *_BaseColor, colour white
- Metallic Map        = *_MetallicSmoothness, Smoothness Source = Metallic Alpha, Smoothness slider 1
- Normal Map          = *_Normal, scale 1
- Occlusion Map       = *_AO, strength 1
- Emission (Bone, Gear only): enable, Emission Map = *_Emission, Emission Color white with HDR
  intensity about 2-4 (eyes should glow faintly in shadow; the runes are meant to stay subtle).
  A Bloom volume makes the eyes and sigil read at gameplay distance.
