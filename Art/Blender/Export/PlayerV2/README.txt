Lich Player V2 - character art update
=====================================

Files (Art/Blender/Export/PlayerV2/)
  SK_Lich_PlayerV2.fbx                 skinned mesh + armature (no animation)
  T_LichV2_Cloth_BaseColor.png         sRGB
  T_LichV2_Cloth_Normal.png            OpenGL (+Y), Normal Map
  T_LichV2_Cloth_MetallicSmoothness.png  R = metallic, A = smoothness (linear)
  T_LichV2_Cloth_AO.png                linear
  T_LichV2_Armour_BaseColor.png / _Normal / _MetallicSmoothness / _AO / _Emission
  T_LichV2_Bone_BaseColor.png   / _Normal / _MetallicSmoothness / _AO / _Emission
  Source: Art/Blender/Lich_PlayerV2.blend

Materials -> textures (URP Lit)
  M_LichV2_Cloth   hood, cloak layers, tunic, bandages     BaseColor, Normal, MetallicSmoothness, AO
  M_LichV2_Armour  plates, trim, leather, iron, back sigil BaseColor, Normal, MetallicSmoothness, AO, Emission (magenta)
  M_LichV2_Bone    skull, spine, ribs, spikes, claws, eyes BaseColor, Normal, MetallicSmoothness, AO, Emission (green)

Stats
  Triangles: 51,336   Vertices: 26,585   Material slots: 3   Max bone influences: 4
  Bones: 33 (identical names, hierarchy and rest pose to Lich_Character.fbx). Bones added: none.

Unity import (Model tab)
  Scale Factor 1, Convert Units on, Bake Axis Conversion on, Mesh Compression Off, Read/Write off.
  Rig: Humanoid. Either Create From This Model or Copy From the existing Lich_Character avatar
  (same skeleton, so existing animations and the Animator Controller keep working).
  Materials: extract, then set each to URP Lit.

Texture importer
  BaseColor, Emission: sRGB on. Normal: Texture Type = Normal Map.
  MetallicSmoothness, AO: sRGB off. Max Size 2048.

URP Lit setup
  Base Map = BaseColor, Metallic Map = MetallicSmoothness (Smoothness Source: Metallic Alpha),
  Normal Map = Normal, Occlusion Map = AO.
  Armour/Bone: enable Emission, Emission Map = Emission, HDR colour white at intensity ~4
  (the glow on the eyes, spine runes and back sigil is meant to bloom).
  Cloth: Render Face = Both (hood and cloak hems are single sheets in places).

Prefab notes
  Weapon sockets (Socket_Weapon_R / Socket_Weapon_L) must be re-parented under
  mixamorig:RightHand / mixamorig:LeftHand on the new model, same local offsets as before.

Notes
  Skeleton: 33 bones, mixamorig: naming, A-pose rest identical to Lich_Character.fbx (checked bone by bone,
  no differences). Humanoid mapping is unchanged: Spine1 = Chest, Spine2 = UpperChest.
  Pivot at the feet centre, model faces -Z in Unity after import (same as the original).
  The AO maps are not wired in the .blend preview material (Blender has no AO input); use them as
  the URP Occlusion Map.
  Bounds: about 1.95 m tall at the tips of the back spikes, 1.34 m wide across the hands in the rest pose.
