# Graph Report - Assets/_Project  (2026-04-18)

## Corpus Check
- Large corpus: 60 files · ~4,709,353 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder, or use --no-semantic to run AST-only.

## Summary
- 406 nodes · 682 edges · 19 communities detected
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 63 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_InventorySystem|InventorySystem]]
- [[_COMMUNITY_HUDManager|HUDManager]]
- [[_COMMUNITY_Remy Character|Remy Character]]
- [[_COMMUNITY_EffectSystem|EffectSystem]]
- [[_COMMUNITY_ItemWorldObject|ItemWorldObject]]
- [[_COMMUNITY_AnimatorDebugger|AnimatorDebugger]]
- [[_COMMUNITY_HUD|HUD]]
- [[_COMMUNITY_RoleSelectionUI|RoleSelectionUI]]
- [[_COMMUNITY_PlayerController|PlayerController]]
- [[_COMMUNITY_RoleAbilityController|RoleAbilityController]]
- [[_COMMUNITY_LobbyManager|LobbyManager]]
- [[_COMMUNITY_UISetupEditor|UISetupEditor]]
- [[_COMMUNITY_Remy Hair Gloss Texture|Remy Hair Gloss Texture]]
- [[_COMMUNITY_Ch10 Material Slot 1002 (ClothingOutfit)|Ch10 Material Slot 1002 (Clothing/Outfit)]]
- [[_COMMUNITY_LobbySetupEditor|LobbySetupEditor]]
- [[_COMMUNITY_DebugPanel|DebugPanel]]
- [[_COMMUNITY_PlayerRole.cs|PlayerRole.cs]]
- [[_COMMUNITY_ProjectFPS.Inventory|ProjectFPS.Inventory]]
- [[_COMMUNITY_ProjectFPS.Inventory|ProjectFPS.Inventory]]

## God Nodes (most connected - your core abstractions)
1. `HUDManager` - 35 edges
2. `HUD` - 34 edges
3. `PlayerController` - 28 edges
4. `EffectSystem` - 26 edges
5. `RoleSelectionUI` - 23 edges
6. `RoleAbilityController` - 23 edges
7. `InventorySystem` - 22 edges
8. `LobbyManager` - 20 edges
9. `ItemWorldObject` - 17 edges
10. `PlayerInteraction` - 16 edges

## Surprising Connections (you probably didn't know these)
- `LobbyManager` --semantically_similar_to--> `PlayerSpawner`  [INFERRED] [semantically similar]
  Assets/_Project/Scripts/Network/LobbyManager.cs → Assets/_Project/Scripts/Network/PlayerSpawner.cs
- `HUDManager` --semantically_similar_to--> `HUD`  [INFERRED] [semantically similar]
  Assets/_Project/Scripts/UI/HUDManager.cs → Assets/_Project/Scripts/UI/HUD.cs
- `DebugPanelHUD` --semantically_similar_to--> `DebugPanel`  [INFERRED] [semantically similar]
  Assets/_Project/Scripts/UI/DebugPanelHUD.cs → Assets/_Project/Scripts/UI/DebugPanel.cs
- `UISetupEditor` --semantically_similar_to--> `LobbySetupEditor`  [INFERRED] [semantically similar]
  Assets/_Project/Scripts/Editor/UISetupEditor.cs → Assets/_Project/Scripts/Editor/LobbySetupEditor.cs
- `RoleType (HUDManager enum)` --semantically_similar_to--> `PlayerRole`  [INFERRED] [semantically similar]
  Assets/_Project/Scripts/UI/HUDManager.cs → Assets/_Project/Scripts/Roles/PlayerRole.cs

## Hyperedges (group relationships)
- **Role System: RoleManager broadcasts RoleData changes to RoleAbilityController, HUD, and RoleSelectionUI** — rolemanager_rolemanager, roleabilitycontroller_roleabilitycontroller, hud_hud, roleselectionui_roleselectionui, roledata_roledata [EXTRACTED 0.95]
- **Player Health Pipeline: PlayerState fires OnHealthChanged consumed by HUD and EffectSystem revival** — playerstate_playerstate, playerstate_onhealthchanged, hud_hud [EXTRACTED 0.90]
- **Debug Toolset: RigidbodyWatcher, AnimatorDebugger, DebugPanel, and DebugPanelHUD form a suite of runtime diagnostics** — rigidbodywatcher_rigidbodywatcher, animatordebugger_animatordebugger, debugpanel_debugpanel, debugpanelhud_debugpanelhud [INFERRED 0.85]
- **Potion Effect Application Pipeline** — itemdata_itemdata, itemworldobject_itemworldobject, effectsystem_effectsystem, activeeffect_activeeffect, potiontype_potiontype [EXTRACTED 0.95]
- **Resource Harvest and Ritual Victory Flow** — inventorysystem_inventorysystem, ritualzone_ritualzone, resourcesystem_resourcesystem [EXTRACTED 0.95]
- **Player Movement and State Integration** — playercontroller_playercontroller, effectsystem_effectsystem, roleabilitycontroller_roleabilitycontroller, playerstate_playerstate [EXTRACTED 0.90]
- **Remy Shoes Complete Texture Set (Normal, Diffuse, Specular, Gloss)** — remy_shoes_normal_texture, remy_shoes_diffuse_texture, remy_shoes_specular_texture, remy_shoes_gloss_texture [EXTRACTED 0.90]
- **Remy Hair Texture Set (Normal)** — remy_hair_normal_texture [EXTRACTED 0.90]
- **Remy Character Texture Collection (Chunk 3)** — remy_shoes_normal_texture, remy_shoes_diffuse_texture, remy_shoes_specular_texture, remy_shoes_gloss_texture, remy_hair_normal_texture [EXTRACTED 0.90]
- **Remy Hair Texture Set (Gloss, Specular, Diffuse, Opacity)** — remy_hair_gloss_texture, remy_hair_specular_texture, remy_hair_diffuse_texture, remy_hair_opacity_texture [EXTRACTED 0.90]
- **Remy Full Texture Set - Chunk 04 (Hair + Body Normal)** — remy_hair_gloss_texture, remy_hair_specular_texture, remy_hair_diffuse_texture, remy_hair_opacity_texture, remy_body_normal_texture [EXTRACTED 0.90]
- **Remy Body Full PBR Texture Set** — remy_body_diffuse_texture, remy_body_specular_texture, remy_body_gloss_texture, remy_body_opacity_texture [EXTRACTED 0.97]
- **Remy Top Clothing Texture Set** — remy_top_normal_texture [EXTRACTED 0.90]
- **Remy Character Complete Texture Set (Chunk 5)** — remy_body_diffuse_texture, remy_body_specular_texture, remy_body_gloss_texture, remy_body_opacity_texture, remy_top_normal_texture [EXTRACTED 0.95]
- **Remy Top Texture Set (Diffuse + Gloss + Specular)** — remy_top_diffuse_texture, remy_top_gloss_texture, remy_top_specular_texture, remy_top_garment [EXTRACTED 1.00]
- **Remy Bottom Texture Set (Diffuse + Normal)** — remy_bottom_diffuse_texture, remy_bottom_normal_texture, remy_bottom_garment [EXTRACTED 1.00]
- **Remy Complete Outfit Texture Set** — remy_top_diffuse_texture, remy_top_gloss_texture, remy_top_specular_texture, remy_bottom_diffuse_texture, remy_bottom_normal_texture, remy_top_garment, remy_bottom_garment, remy_character [EXTRACTED 0.95]
- **Remy Bottom PBR Texture Set** — remy_bottom_gloss_texture, remy_bottom_specular_texture, remy_bottom_material [EXTRACTED 1.00]
- **Ch10 Material 1001 PBR Texture Set** — ch10_1001_normal_texture, ch10_1001_glossiness_texture, ch10_1001_specular_texture, ch10_1001_material [EXTRACTED 1.00]
- **Chunk 07 All Texture Maps** — remy_bottom_gloss_texture, remy_bottom_specular_texture, ch10_1001_normal_texture, ch10_1001_glossiness_texture, ch10_1001_specular_texture [EXTRACTED 0.90]
- **Ch10 Material 1001 Texture Set (Body Skin)** — ch10_1001_diffuse, ch10_material_1001 [EXTRACTED 1.00]
- **Ch10 Material 1002 Texture Set (Clothing/Outfit)** — ch10_1002_diffuse, ch10_1002_normal, ch10_1002_specular, ch10_1002_glossiness, ch10_material_1002 [EXTRACTED 1.00]
- **Ch10 Complete PBR Texture Set** — ch10_1001_diffuse, ch10_1002_diffuse, ch10_1002_normal, ch10_1002_specular, ch10_1002_glossiness, ch10_material_1001, ch10_material_1002, ch10_character [INFERRED 0.95]

## Communities

### Community 0 - "InventorySystem"
Cohesion: 0.06
Nodes (9): InventorySystem, ProjectFPS.Inventory, PlayerInteraction.OnInteractionPrompt (event), PlayerInteraction, ProjectFPS.Player, ProjectFPS.Inventory, ResourceSystem, ProjectFPS.Inventory (+1 more)

### Community 1 - "HUDManager"
Cohesion: 0.1
Nodes (5): DebugPanelHUD, ProjectFPS.UI, HUDManager, ProjectFPS.UI, UISetupEditor

### Community 2 - "Remy Character"
Cohesion: 0.07
Nodes (37): Blue/Purple Tangent-Space Color Palette, Face Surface Detail (eyes, nose, mouth, ears), Normal Map Type, PBR Specular Workflow, Remy Body Diffuse Map, Remy Body Gloss Map, Remy Body Material, Remy Body Mesh (head, torso, arms, hands, feet, teeth, eyes) (+29 more)

### Community 3 - "EffectSystem"
Cohesion: 0.1
Nodes (8): ActiveEffect, ProjectFPS.Inventory, EffectSystem, ProjectFPS.Inventory, PlayerState.OnHealthChanged (event), PlayerState, ProjectFPS.Player, PotionType

### Community 4 - "ItemWorldObject"
Cohesion: 0.09
Nodes (10): ItemData, ProjectFPS.Inventory, ItemDebugger, ProjectFPS.Inventory, ItemPickup, ProjectFPS.Inventory, ItemType, ItemWorldObject (+2 more)

### Community 5 - "AnimatorDebugger"
Cohesion: 0.09
Nodes (9): AnimatorDebugger, ProjectFPS.Player, FPSBodyHider, ProjectFPS.Player, MonoBehaviour, PlayerSpawner, ProjectFPS.Network, ProjectFPS.Player (+1 more)

### Community 6 - "HUD"
Cohesion: 0.12
Nodes (2): HUD, ProjectFPS.UI

### Community 7 - "RoleSelectionUI"
Cohesion: 0.14
Nodes (5): RoleManager.OnRoleChanged (event), ProjectFPS.Roles, RoleManager, ProjectFPS.UI, RoleSelectionUI

### Community 8 - "PlayerController"
Cohesion: 0.15
Nodes (3): NetworkBehaviour, PlayerController, ProjectFPS.Player

### Community 9 - "RoleAbilityController"
Cohesion: 0.15
Nodes (6): RoleType (HUDManager enum), PlayerRole, ProjectFPS.Player, RoleAbilityController, ProjectFPS.Roles, RoleData

### Community 10 - "LobbyManager"
Cohesion: 0.17
Nodes (3): LobbyManager, ProjectFPS.Network, LobbySetupEditor

### Community 11 - "UISetupEditor"
Cohesion: 0.22
Nodes (2): ProjectFPS.Editor, UISetupEditor

### Community 12 - "Remy Hair Gloss Texture"
Cohesion: 0.22
Nodes (13): Black and White Color Palette, Diffuse Map Type, Gloss Map Type, Grayscale Color Palette, Hair Strand / Vertical Streak Pattern, Opacity Map Type, Remy Hair Diffuse Texture, Remy Hair Gloss Texture (+5 more)

### Community 13 - "Ch10 Material Slot 1002 (Clothing/Outfit)"
Cohesion: 0.24
Nodes (12): Ch10 Material 1001 Diffuse Map, Ch10 Material 1001 Glossiness Map, Ch10 Material 1001 Set, Ch10 Material 1001 Normal Map, Ch10 Material 1001 Specular Map, Ch10 Material 1002 Diffuse Map, Ch10 Material 1002 Glossiness Map, Ch10 Material 1002 Normal Map (+4 more)

### Community 14 - "LobbySetupEditor"
Cohesion: 0.33
Nodes (2): LobbySetupEditor, ProjectFPS.Editor

### Community 15 - "DebugPanel"
Cohesion: 0.36
Nodes (2): DebugPanel, ProjectFPS.UI

### Community 16 - "PlayerRole.cs"
Cohesion: 1.0
Nodes (1): ProjectFPS.Roles

### Community 17 - "ProjectFPS.Inventory"
Cohesion: 1.0
Nodes (1): ProjectFPS.Inventory

### Community 18 - "ProjectFPS.Inventory"
Cohesion: 1.0
Nodes (1): ProjectFPS.Inventory

## Knowledge Gaps
- **32 isolated node(s):** `RoleType (HUDManager enum)`, `Remy Top Normal Map`, `ProjectFPS.Network`, `ProjectFPS.Network`, `ProjectFPS.UI` (+27 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `PlayerRole.cs`** (2 nodes): `PlayerRole.cs`, `ProjectFPS.Roles`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ProjectFPS.Inventory`** (2 nodes): `PotionType.cs`, `ProjectFPS.Inventory`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ProjectFPS.Inventory`** (2 nodes): `ItemType.cs`, `ProjectFPS.Inventory`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.