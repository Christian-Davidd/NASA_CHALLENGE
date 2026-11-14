"""Build the natural Mars play scene, its terrain art, and review previews."""
from pathlib import Path
import math
import random
import re
import uuid
from PIL import Image

PROJECT = Path(__file__).resolve().parents[5]
ASSETS = PROJECT / "Assets"
PACK = ASSETS / "Art" / "Environment" / "MarsNaturalLandscape01"
SCENE = ASSETS / "Scenes" / "Mars_Natural_Landscape_01.unity"
SCENE_META = SCENE.with_name(SCENE.name + ".meta")
TEMPLATE = ASSETS / "Scenes" / "SampleScene.unity"
PPU = 448.0
MAP_W, MAP_H = 108.0, 72.0
PREVIEW = PROJECT / "Mars_Natural_Landscape_01_preview.png"
DETAIL_PREVIEW = PROJECT / "Mars_Natural_Landscape_01_detail.png"
PREFABS = PACK / "Prefabs"

guid_by_path = {}
for meta in PACK.rglob("*.png.meta"):
    m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
    if m:
        guid_by_path[meta.with_suffix("").relative_to(PACK).as_posix()] = m.group(1)
CHARACTERS = PROJECT / "Assets" / "Art" / "Characters" / "AstronautaZ2" / "Sprites"
sprite_guid = {}
for meta in CHARACTERS.glob("*.png.meta"):
    m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
    if m:
        sprite_guid[meta.name[:-5]] = m.group(1)

nodes, by_name = [], {}

def node(name, parent=None, pos=(0, 0), sprite=None, scale=(1, 1), order=0,
         draw=0, size=None, alpha=1.0, rotation=0, collider=None,
         collider_offset=(0,0), rigidbody=False, script=None, interactable=None, tag="Untagged"):
    n = dict(name=name, parent=parent, pos=pos, sprite=sprite, scale=scale,
             order=order, draw=draw, size=size, alpha=alpha, rotation=rotation,
             collider=collider, collider_offset=collider_offset, rigidbody=rigidbody, script=script,
             interactable=interactable, tag=tag)
    nodes.append(n)
    by_name[name] = n
    return n

def group(name, parent=None):
    return node(name, parent)

def asset(name, parent, rel, pos, target=None, order=0, alpha=1.0,
          rotation=0, collider=None, draw=0):
    rel = rel.replace("\\", "/")
    path = PACK / rel
    with Image.open(path) as im:
        natural = (im.width / PPU, im.height / PPU)
    if target:
        scale = (1.0, 1.0) if draw == 2 else (target[0] / natural[0], target[1] / natural[1])
    else:
        scale = (1.0, 1.0)
    if draw == 2 and target:
        natural = target
    local_collider = None
    if collider:
        local_collider = (collider[0] / scale[0], collider[1] / scale[1])
    layer_order = order + round(-pos[1] * 3) if order >= 0 else order
    return node(name, parent, pos, rel, scale, layer_order, draw, natural,
                alpha, rotation, local_collider)

world = group("Mars_Natural_Landscape_01")
ground = group("Ground", world)
regions = group("Regional_Ground_Variations", ground)
details = group("Surface_Details", ground)
landforms = group("Geological_Formations", world)
clusters = group("Rock_Fields", world)
individuals = group("Individual_Rocks", world)
bounds = group("Exploration_Bounds", world)

# A map-sized natural floor with large organic color transitions, never a tile grid.
base = asset("Regolith_Map_Base", ground, "Ground/landscape_regolith_base.png",
             (0, 0), target=(MAP_W, MAP_H), order=-100)

# Natural surface regions. Reusable small tiles and detail overlays stay separate.
asset("Rough_Regolith_Southwest", regions, "Ground/Regions/rocky_regolith_region.png", (-31.0, -23.0),
      target=(28.0, 21.0), alpha=.86, order=-80)
asset("Exposed_Bedrock_South", regions, "Ground/Regions/sedimentary_bedrock_region.png", (18.0, -27.0),
      target=(26.0, 16.0), alpha=.76, order=-83)
asset("Wind_Sculpted_Northeast", regions, "Ground/Regions/wind_ripple_region.png", (31.0, 27.0),
      target=(30.0, 19.0), alpha=.78, order=-78)
asset("Dust_Basin_Northwest", regions, "Ground/dust_deposit_patch.png", (-15.0, 28.0),
      target=(26.0, 17.0), alpha=.39, order=-77)

# Broad landforms follow the supplied overhead references: eroded rims, sedimentary
# shelves, wind-built bedforms, channels, escarpments and boulder concentrations.
asset("Impact_Crater_Eroded_NW", landforms, "Geology/Formations/eroded_impact_crater.png",
      (-49.0, 1.0), target=(17.0, 15.0), order=0)
asset("Sedimentary_Ridge_West", landforms, "Geology/Formations/wind_cut_rock_ridge.png",
      (-5.0, 31.0), target=(29.0, 11.0), rotation=-17, order=4, collider=(20.0, 3.0))
asset("Barchan_Dune_Field_NE", landforms, "Geology/Formations/barchan_dune_field.png",
      (34.0, 29.0), target=(23.0, 13.0), rotation=-9, order=2)
asset("Dry_Erosion_Channel_SW", landforms, "Geology/Formations/dry_erosion_channel.png",
      (-25.0, -13.0), target=(19.0, 11.0), rotation=20, order=5)
asset("Terraced_Mesa_Southwest", landforms, "Geology/Formations/mesa_layered_terrace.png",
      (-41.0, -25.0), target=(19.0, 12.0), rotation=7, order=3, collider=(14.0, 5.5))
foothills = asset("Layered_Foothill_Massif", landforms, "Geology/Formations/mountain_foothills_complex.png",
      (12.0, 11.5), target=(31.0, 21.0), rotation=-7, order=5, collider=(20.0, 8.0))
foothills['interactable'] = {"title": "Estratos del macizo", "description": "Capas sedimentarias y bloques angulosos expuestos. Muestra geológica registrada."}
mountain_range = asset("Stair_Stepped_Mountain_Range", landforms, "Geology/Formations/stair_stepped_mountain_ridge.png",
      (-32.0, 21.0), target=(27.0, 13.0), rotation=-11, order=3, collider=(19.0, 3.0))
mountain_range['interactable'] = {"title": "Escarpe escalonado", "description": "Frente de erosión con terrazas rocosas. Muestra geológica registrada."}

# Dense rockfall occupies several uneven patches and leaves a low-stone walking basin
# through the centre for later player navigation.
asset("Boulder_Fall_Southeast", clusters, "Geology/Clusters/concentrated_rockfall_field.png",
      (31.0, -25.0), target=(16.0, 11.0), rotation=-14, order=8, collider=(10.0, 5.0))
asset("Buried_Boulders_North", clusters, "Geology/Clusters/cluster_buried_boulders.png",
      (3.0, 34.0), target=(11.0, 7.5), rotation=9, order=5, collider=(6.5, 3.4))
asset("Rock_Scree_West", clusters, "Geology/Clusters/cluster_buried_boulders.png",
      (-48.0, -6.0), target=(9.0, 9.0), rotation=-23, order=7, collider=(6.0, 6.0))
talus = asset("Landslide_Talus_Apron", clusters, "Geology/Clusters/landslide_talus_boulder_apron.png",
      (-4.0, -25.0), target=(22.0, 17.0), rotation=16, order=7, collider=(15.0, 6.0))
talus['interactable'] = {"title": "Depósito de bloques", "description": "Acumulación de rocas al pie de un escarpe. Muestra geológica registrada."}

# Ground markings stay small and low contrast so they read as weathering rather than props.
detail_assets = [
    ("Wind_Scour_01", "Ground/Details/wind_scour_detail.png", (-38.0, 2.0), (9.0, 6.0), -13, .73),
    ("Wind_Scour_02", "Ground/Details/wind_scour_detail.png", (15.0, 25.0), (9.0, 5.5), 27, .68),
    ("Fine_Fractures_01", "Ground/Details/fractures_fine.png", (-7.0, -10.0), (8.5, 6.6), -19, .54),
    ("Fine_Fractures_02", "Ground/Details/fractures_fine.png", (18.0, -9.0), (7.4, 5.8), 11, .48),
    ("Embedded_Pebbles_Central", "Ground/Details/embedded_pebble_scatter.png", (0.0, -3.0), (12.0, 8.0), 17, .76),
    ("Dust_Streak_Central_North", "Ground/Details/wind_scour_detail.png", (-3.0, 14.0), (11.0, 4.5), -4, .42),
]
for name, rel, pos, size, rotation, alpha in detail_assets:
    asset(name, details, rel, pos, target=size, order=-8, alpha=alpha, rotation=rotation)

# Small and medium rocks are spatially clumped in geological patches, with fewer
# exposed in the navigable central basin. Deterministic seed keeps regeneration stable.
rng = random.Random(140126)
rock_assets = [
    "Geology/Rocks/stone_small_angular.png",
    "Geology/Rocks/stone_small_round.png",
    "Geology/Rocks/rock_medium_stratified.png",
    "Geology/Rocks/boulder_angular_layered.png",
    "Geology/Rocks/boulder_weathered.png",
    "Geology/Rocks/rock_plate_fragment.png",
]
patch_centres = [(-48,-27),(-43,1),(-34,26),(-9,31),(20,32),(48,26),(49,0),(46,-27),(5,-32),(-21,-28),(-49,17),(32,-12),(-5,3),(24,4)]
reserved_zones = [
    (-49,1,11),(-32,21,13),(-5,31,9),(34,29,12),(-25,-13,9),
    (45,-6,8),(-41,-25,10),(17,10.5,15),(31,-25,11),(3,34,6),
    (-48,-6,6),(-4,-25,12),(12,11.5,15),(0,-5,5.5),
    (-52,-14,4.5),(-36,-6,4),(10,34,4.5),(51,17,4.2),(50,-33,4.5),(-18,-33,4.2),(-52,32,4)
]
placed_rocks=[]
for i in range(168):
    # Mix broad-area sampling with independent rockfall patches to avoid visible
    # rows, repeated clusters and an unnaturally empty central corridor.
    if rng.random() < .58:
        cx, cy = rng.choice(patch_centres)
        spread = rng.uniform(3.0, 8.2)
        x = max(-52.0, min(52.0, rng.gauss(cx, spread)))
        y = max(-34.0, min(34.0, rng.gauss(cy, spread*.78)))
    else:
        x, y = rng.uniform(-52,52), rng.uniform(-34,34)
    if any(math.hypot(x-zx,y-zy)<radius for zx,zy,radius in reserved_zones):
        continue
    # Keep the east landing apron level and intentionally unoccupied.
    if 22.0 <= x <= 53.0 and -20.5 <= y <= 2.5:
        continue
    r = rng.random()
    idx = 0 if r < .26 else 1 if r < .48 else 2 if r < .75 else 3 if r < .88 else 4 if r < .96 else 5
    rel = rock_assets[idx]
    factor = rng.uniform(.72, 1.24) if idx < 2 else (rng.uniform(.7, 1.02) if idx == 2 else rng.uniform(.72, 1.05))
    world_w = (2.2 if idx < 2 else (3.8 if idx == 2 else 5.2)) * factor
    world_h = world_w * rng.uniform(.62, .87)
    if any(math.hypot(x-px,y-py)<(world_w+pw)*.34+1.2 for px,py,pw in placed_rocks):
        continue
    placed_rocks.append((x,y,world_w))
    # Every freestanding rock blocks the player. Small stones use a slightly
    # inset collider so their visible edges do not make navigation feel sticky.
    coll = (world_w*.58, world_h*.44) if idx < 2 else (world_w*.69, world_h*.48)
    asset(f"Scattered_Rock_{i+1:03d}", individuals, rel, (x,y), target=(world_w,world_h),
          rotation=rng.randrange(0,360), order=10, collider=coll)

giant_rocks=[
    ("Mega_Boulder_01", "Geology/Rocks/giant_angular_block.png",(-52,-14),(5.8,4.5),27),
    ("Mega_Boulder_02", "Geology/Rocks/giant_layered_block.png",(-36,-6),(5.0,4.1),-18),
    ("Mega_Boulder_03", "Geology/Rocks/giant_split_boulder.png",(10,34),(5.8,4.1),14),
    ("Mega_Boulder_04", "Geology/Rocks/giant_angular_block.png",(51,17),(5.5,4.3),-31),
    ("Mega_Boulder_05", "Geology/Rocks/giant_layered_block.png",(50,-33),(5.9,4.5),13),
    ("Mega_Boulder_06", "Geology/Rocks/giant_split_boulder.png",(-18,-33),(5.4,3.8),-24),
    ("Mega_Boulder_07", "Geology/Rocks/giant_angular_block.png",(-52,32),(5.0,4.2),9),
]
for name,rel,pos,size,rotation in giant_rocks:
    asset(name,individuals,rel,pos,target=size,rotation=rotation,order=13,
          collider=(size[0]*.76,size[1]*.58))

# The playable astronaut uses the existing Z-2 views and side poses plus new
# front/back foot-cycle sprites generated from the same vector masters.
player_script_fields = {
    "frontIdle": "char_astronauta_frente_neutro_z2_v01.png",
    "backIdle": "char_astronauta_espalda_neutro_z2_v01.png",
    "leftIdle": "char_astronauta_lado_izquierdo_neutro_z2_v01.png",
    "rightIdle": "char_astronauta_lado_derecho_neutro_z2_v01.png",
    "leftWalkA": "char_astronauta_lado_izquierdo_caminar_01_z2_v01.png",
    "leftWalkB": "char_astronauta_lado_izquierdo_caminar_02_z2_v01.png",
    "rightWalkA": "char_astronauta_lado_derecho_caminar_01_z2_v01.png",
    "rightWalkB": "char_astronauta_lado_derecho_caminar_02_z2_v01.png",
    "frontWalkA": "char_astronauta_frente_caminar_01_z2_v01.png",
    "frontWalkB": "char_astronauta_frente_caminar_02_z2_v01.png",
    "backWalkA": "char_astronauta_espalda_caminar_01_z2_v01.png",
    "backWalkB": "char_astronauta_espalda_caminar_02_z2_v01.png",
    "leftInteract": "char_astronauta_lado_izquierdo_interactuar_z2_v01.png",
    "rightInteract": "char_astronauta_lado_derecho_interactuar_z2_v01.png",
}
player_fields = {key: f"{{fileID: 21300000, guid: {sprite_guid[name]}, type: 3}}"
                 for key,name in player_script_fields.items()}
player_fields.update({"moveSpeed":"3.2", "interactionRadius":"1.6", "interactionPoseDuration":"0.7"})
player = node("Astronaut_Player", world, (0.0,-5.5),
    "Characters/char_astronauta_frente_neutro_z2_v01.png", size=(640/PPU,960/PPU), order=25,
    collider=(.72,.42), collider_offset=(0,.21), rigidbody=True,
    script={"guid":"8a463eda0f7a4f20ac86f9dcc2d85cc3", "fields":player_fields}, tag="Player")

# Hard scene edges; geology and the centre remain individually editable.
for name, pos, size in [
    ("Boundary_West",(-MAP_W/2,0),(.34,MAP_H)),
    ("Boundary_East",(MAP_W/2,0),(.34,MAP_H)),
    ("Boundary_North",(0,MAP_H/2),(MAP_W,.34)),
    ("Boundary_South",(0,-MAP_H/2),(MAP_W,.34)),
]:
    node(name, bounds, pos, collider=size)

def q(v):
    return f"{v:.6f}".rstrip("0").rstrip(".") if isinstance(v, float) else str(v)

next_id = 910000000
for n in nodes:
    n['go_id']=next_id; next_id+=1
    n['transform_id']=next_id; next_id+=1
    n['renderer_id']=None; n['collider_id']=None
    n['rigidbody_id']=None; n['script_id']=None; n['interactable_id']=None
    if n['sprite']:
        n['renderer_id']=next_id; next_id+=1
    if n['collider']:
        n['collider_id']=next_id; next_id+=1
    if n['rigidbody']:
        n['rigidbody_id']=next_id; next_id+=1
    if n['script']:
        n['script_id']=next_id; next_id+=1
    if n['interactable']:
        n['interactable_id']=next_id; next_id+=1

children={}
for n in nodes:
    children.setdefault(id(n['parent']) if n['parent'] else None,[]).append(n)

def render_node(n, root_order):
    comps=[n['transform_id']]
    if n['renderer_id']: comps.append(n['renderer_id'])
    if n['collider_id']: comps.append(n['collider_id'])
    if n['rigidbody_id']: comps.append(n['rigidbody_id'])
    if n['script_id']: comps.append(n['script_id'])
    if n['interactable_id']: comps.append(n['interactable_id'])
    s=f"--- !u!1 &{n['go_id']}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n"
    for cid in comps: s+=f"  - component: {{fileID: {cid}}}\n"
    s+=(f"  m_Layer: 0\n  m_Name: {n['name']}\n  m_TagString: {n['tag']}\n"
        "  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n")
    px,py=n['pos']; sx,sy=n['scale']
    ang=math.radians(n['rotation']); rz=math.sin(ang/2); rw=math.cos(ang/2)
    father=n['parent']['transform_id'] if n['parent'] else 0
    kids=children.get(id(n),[])
    s+=f"--- !u!4 &{n['transform_id']}\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  serializedVersion: 2\n"
    s+=f"  m_LocalRotation: {{x: 0, y: 0, z: {q(rz)}, w: {q(rw)}}}\n  m_LocalPosition: {{x: {q(px)}, y: {q(py)}, z: 0}}\n"
    s+=f"  m_LocalScale: {{x: {q(sx)}, y: {q(sy)}, z: 1}}\n  m_ConstrainProportionsScale: 0\n  m_Children:\n"
    for child in kids: s+=f"  - {{fileID: {child['transform_id']}}}\n"
    s+=f"  m_Father: {{fileID: {father}}}\n  m_RootOrder: {root_order}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: {q(n['rotation'])}}}\n"
    if n['sprite']:
        rel=n['sprite']
        guid=sprite_guid[rel.split("/",1)[1]] if rel.startswith("Characters/") else guid_by_path[rel]
        w,h=n['size']
        s+=f"--- !u!212 &{n['renderer_id']}\nSpriteRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  m_Enabled: 1\n  m_CastShadows: 0\n  m_ReceiveShadows: 0\n  m_DynamicOccludee: 1\n  m_StaticShadowCaster: 0\n  m_MotionVectors: 1\n  m_LightProbeUsage: 1\n  m_ReflectionProbeUsage: 1\n  m_RayTracingMode: 0\n  m_RayTraceProcedural: 0\n  m_RayTracingAccelStructBuildFlagsOverride: 0\n  m_RayTracingAccelStructBuildFlags: 1\n  m_SmallMeshCulling: 1\n  m_RenderingLayerMask: 1\n  m_RendererPriority: 0\n  m_Materials:\n  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}\n  m_StaticBatchInfo:\n    firstSubMesh: 0\n    subMeshCount: 0\n  m_StaticBatchRoot: {{fileID: 0}}\n  m_ProbeAnchor: {{fileID: 0}}\n  m_LightProbeVolumeOverride: {{fileID: 0}}\n  m_ScaleInLightmap: 1\n  m_ReceiveGI: 1\n  m_PreserveUVs: 0\n  m_IgnoreNormalsForChartDetection: 0\n  m_ImportantGI: 0\n  m_StitchLightmapSeams: 1\n  m_SelectedEditorRenderState: 0\n  m_MinimumChartSize: 4\n  m_AutoUVMaxDistance: 0.5\n  m_AutoUVMaxAngle: 89\n  m_LightmapParameters: {{fileID: 0}}\n  m_SortingLayerID: 0\n  m_SortingLayer: 0\n  m_SortingOrder: {n['order']}\n  m_Sprite: {{fileID: 21300000, guid: {guid}, type: 3}}\n  m_Color: {{r: 1, g: 1, b: 1, a: {q(n['alpha'])}}}\n  m_FlipX: 0\n  m_FlipY: 0\n  m_DrawMode: {n['draw']}\n  m_Size: {{x: {q(w)}, y: {q(h)}}}\n  m_AdaptiveModeThreshold: 0.5\n  m_SpriteTileMode: 0\n  m_WasSpriteAssigned: 1\n  m_MaskInteraction: 0\n  m_SpriteSortPoint: 1\n"
    if n['collider_id']:
        cx,cy=n['collider']
        cox,coy=n['collider_offset']
        s+=f"--- !u!61 &{n['collider_id']}\nBoxCollider2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  m_Enabled: 1\n  m_Density: 1\n  m_Material: {{fileID: 0}}\n  m_IsTrigger: 0\n  m_UsedByEffector: 0\n  m_UsedByComposite: 0\n  m_Offset: {{x: {q(cox)}, y: {q(coy)}}}\n  m_SpriteTilingProperty:\n    border: {{x: 0, y: 0, z: 0, w: 0}}\n    pivot: {{x: 0, y: 0}}\n    oldSize: {{x: 0, y: 0}}\n    newSize: {{x: 0, y: 0}}\n    adaptiveTilingThreshold: 0\n    drawMode: 0\n    adaptiveTiling: 0\n  m_AutoTiling: 0\n  serializedVersion: 2\n  m_Size: {{x: {q(cx)}, y: {q(cy)}}}\n  m_EdgeRadius: 0\n"
    if n['rigidbody_id']:
        s+=f"--- !u!50 &{n['rigidbody_id']}\nRigidbody2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  m_BodyType: 0\n  m_Simulated: 1\n  m_UseFullKinematicContacts: 0\n  m_UseAutoMass: 0\n  m_Mass: 1\n  m_LinearDamping: 5\n  m_AngularDamping: 0.05\n  m_GravityScale: 0\n  m_Material: {{fileID: 0}}\n  m_IncludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n  m_ExcludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n  m_Interpolate: 1\n  m_SleepingMode: 1\n  m_CollisionDetection: 0\n  m_Constraints: 4\n"
    if n['script_id']:
        data=n['script']
        fields="".join(f"  {key}: {value}\n" for key,value in data['fields'].items())
        s+=f"--- !u!114 &{n['script_id']}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {data['guid']}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n{fields}"
    if n['interactable_id']:
        data=n['interactable']
        s+=f"--- !u!114 &{n['interactable_id']}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {n['go_id']}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: 61b6d4e3f6df4c69ad08a27749d1d83f, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n  displayName: {data['title']}\n  result: {data['description']}\n"
    return s

scene_meta_guid=uuid.uuid4().hex
if SCENE_META.exists():
    found=re.search(r"^guid: ([0-9a-f]{32})$",SCENE_META.read_text(encoding="utf-8"),re.M)
    if found: scene_meta_guid=found.group(1)
template=TEMPLATE.read_text(encoding="utf-8")
template=template.replace("m_SceneGUID: 00000000000000000000000000000000", "m_SceneGUID: "+scene_meta_guid)
template=template.replace("m_BackGroundColor: {r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}", "m_BackGroundColor: {r: 0.62, g: 0.43, b: 0.30, a: 1}")
template=re.sub(r"orthographic size: [^\n]+", "orthographic size: 8.5", template)
template=template.replace("  - component: {fileID: 519420030}\n  m_Layer: 0\n  m_Name: Main Camera",
    "  - component: {fileID: 519420030}\n  - component: {fileID: 899999999}\n  m_Layer: 0\n  m_Name: Main Camera")
docs=[template.rstrip()]
docs.append(f"--- !u!114 &899999999\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 519420028}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: aa245e742f8a45a8a3bd36f30dd4f8a6, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n  target: {{fileID: {player['transform_id']}}}\n  damping: 6\n  mapSize: {{x: 108, y: 72}}\n")
root_order=2
for n in children.get(None,[]):
    docs.append(render_node(n,root_order)); root_order+=1
for n in nodes:
    if n['parent']:
        docs.append(render_node(n,children[id(n['parent'])].index(n)))
SCENE.write_text("\n".join(docs)+"\n",encoding="utf-8")
SCENE_META.write_text("fileFormatVersion: 2\nguid: "+scene_meta_guid+"\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding="utf-8")

# High-quality external previews are rendered directly from the scene's source sprites.
view_w,view_h=MAP_W,MAP_H
out_w,out_h=1800,1200
px_per_unit=out_w/view_w
canvas=Image.new("RGBA",(out_w,out_h),(185,135,98,255))
def paste_clipped(dest,img,xy):
    x,y=xy; left=max(0,x); top=max(0,y); right=min(dest.width,x+img.width); bottom=min(dest.height,y+img.height)
    if left>=right or top>=bottom:return
    dest.alpha_composite(img.crop((left-x,top-y,right-x,bottom-y)),(left,top))
def composite_node(n,dest,clip=None):
    if not n['sprite']:return
    source_path = CHARACTERS/n['sprite'].split("/",1)[1] if n['sprite'].startswith("Characters/") else PACK/n['sprite']
    src=Image.open(source_path).convert("RGBA")
    sx,sy=n['scale']; w=max(1,round(src.width/PPU*sx*px_per_unit)); h=max(1,round(src.height/PPU*sy*px_per_unit))
    if n['draw']==2 and n['size']:
        tilew=max(1,round(src.width/PPU*px_per_unit)); tileh=max(1,round(src.height/PPU*px_per_unit))
        tw=max(1,round(n['size'][0]*px_per_unit)); th=max(1,round(n['size'][1]*px_per_unit))
        tiled=Image.new("RGBA",(tw+tilew,th+tileh),(0,0,0,0))
        for yy in range(0,th+tileh,tileh):
            for xx in range(0,tw+tilew,tilew):
                tile=src.resize((tilew,tileh),Image.Resampling.BICUBIC)
                tiled.alpha_composite(tile,(xx,yy))
        img=tiled.crop((0,0,tw,th)); w,h=tw,th
    else:
        img=src.resize((w,h),Image.Resampling.LANCZOS)
    if n['alpha']<1:
        img.putalpha(img.getchannel("A").point(lambda v:int(v*n['alpha'])))
    if n['rotation']:
        img=img.rotate(n['rotation'],resample=Image.Resampling.BICUBIC,expand=True)
    ox,oy=n['pos']; cx=round((ox+view_w/2)*px_per_unit); cy=round((view_h/2-oy)*px_per_unit)
    if clip:
        # Detail view crops from a chosen world-space centre after drawing the full map.
        paste_clipped(dest,img,(cx-img.width//2,cy-img.height//2))
    else:
        paste_clipped(dest,img,(cx-img.width//2,cy-img.height//2))

for n in sorted((n for n in nodes if n['sprite']),key=lambda n:n['order']):
    composite_node(n,canvas)
canvas.convert("RGB").save(PREVIEW,quality=96)

# Close-up at the player spawn, showing the astronaut against the walkable terrain.
crop_center=(0.0,-5.5)
crop_w,crop_h=500,281
cx=round((crop_center[0]+view_w/2)*px_per_unit); cy=round((view_h/2-crop_center[1])*px_per_unit)
left=max(0,min(out_w-crop_w,cx-crop_w//2)); top=max(0,min(out_h-crop_h,cy-crop_h//2))
canvas.convert("RGB").crop((left,top,left+crop_w,top+crop_h)).resize((1600,900),Image.Resampling.LANCZOS).save(DETAIL_PREVIEW,quality=96)

# Reusable single-object prefabs accompany the editable scene placement.
PREFABS.mkdir(parents=True,exist_ok=True)
prefab_names=[
    "Impact_Crater_Eroded_NW","Sedimentary_Ridge_West","Barchan_Dune_Field_NE",
    "Dry_Erosion_Channel_SW","Terraced_Mesa_Southwest",
    "Boulder_Fall_Southeast","Buried_Boulders_North","Rock_Scree_West",
    "Layered_Foothill_Massif",
    "Stair_Stepped_Mountain_Range","Landslide_Talus_Apron",
]
prefab_header="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
for index,name in enumerate(prefab_names):
    prefab=dict(by_name[name])
    prefab['name']=name.replace('_NW','').replace('_SW','').replace('_NE','').replace('_SE','')
    prefab['parent']=None
    prefab['pos']=(0,0)
    prefab['order']=0
    base_id=1300000000+index*10
    prefab['go_id']=base_id
    prefab['transform_id']=base_id+1
    prefab['renderer_id']=base_id+2 if prefab['sprite'] else None
    prefab['collider_id']=base_id+3 if prefab['collider'] else None
    prefab['rigidbody_id']=None
    prefab['script_id']=None
    prefab['interactable_id']=None
    prefab_path=PREFABS/(prefab['name']+'.prefab')
    prefab_path.write_text(prefab_header+render_node(prefab,0),encoding='utf-8')
    meta_path=prefab_path.with_name(prefab_path.name+'.meta')
    if not meta_path.exists():
        meta_path.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding='utf-8')
folder_meta=PREFABS.with_name(PREFABS.name+'.meta')
if not folder_meta.exists():
    folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding='utf-8')

print(f"Created {SCENE} ({len(nodes)} scene objects, {sum(1 for n in nodes if n['sprite'])} renderers, {sum(1 for n in nodes if n['collider'])} BoxCollider2D)")
print(f"Rendered {PREVIEW} and {DETAIL_PREVIEW}; wrote {len(prefab_names)} reusable geology prefabs to {PREFABS}")
