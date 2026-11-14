# Blender (background) script: reduce a generated head to a paper-craft polygon count and write it back as GLB.
#   blender -b -P tools/decimate_head.py -- <in.glb> <out.glb> [target_tris]
# The mesh is joined, welded, reduced with the collapse decimator (UVs kept, so the painted texture stays), shaded flat so
# that the facets read as folded card, recentred with its base at the origin and scaled to 1 m high.
import bpy, sys, mathutils

argv = sys.argv[sys.argv.index('--') + 1:]; src, dst = argv[0], argv[1]; target = int(argv[2]) if len(argv) > 2 else 6000
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1: bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tris0 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.remove_doubles(threshold=1e-5); bpy.ops.object.mode_set(mode='OBJECT')
m = ob.modifiers.new('dec', 'DECIMATE'); m.ratio = min(1.0, target / max(1, tris0)); m.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier='dec')
for p in ob.data.polygons: p.use_smooth = False
flt = argv[3] if len(argv) > 3 else ''
if flt:
    # repaint (after the independent review): the vulture read as a skull with red-rimmed eyes, the crocodile as a bone-yellow skull
    import numpy as np
    for img in bpy.data.images:
        if img.size[0] == 0 or img.size[1] == 0 or not img.name.lower().startswith('color'): continue      # the colour map only: never the normal or the roughness map
        a = np.array(img.pixels[:], dtype=np.float32).reshape(-1, 4); rgb = a[:, :3]
        lum = rgb.mean(1, keepdims=True); mean = rgb.mean(0, keepdims=True)
        if flt == 'vulture':
            red = (rgb[:, 0] > 1.35 * rgb[:, 1]) & (rgb[:, 0] > 1.35 * rgb[:, 2]) & (rgb[:, 0] > 0.22)
            rgb[:] = mean + (rgb - mean) * 0.60                                  # cracks and shading flattened
            rgb[red] = lum[red] * 0.55 * np.array([1.0, 0.92, 0.82], np.float32)  # the red rims become dark warm grey
            rgb *= np.array([0.50, 0.45, 0.40], np.float32)                      # bone white becomes grey-brown paper, dark enough not to turn bone again under a warm lamp
        elif flt in ('wolf', 'hyena'):
            red = (rgb[:, 0] > 2.2 * rgb[:, 1]) & (rgb[:, 0] > 2.2 * rgb[:, 2]) & (rgb[:, 0] > 0.25)       # the red of a mouth, not the orange of fur
            rgb[red] = lum[red] * 0.45 * np.array([1.0, 0.80, 0.66], np.float32)  # the red of the open mouth becomes dark brown
        elif flt == 'crocodile':
            rgb[:] = lum + (rgb - lum) * 1.3; rgb *= np.array([0.66, 0.98, 0.56], np.float32)   # the green found again
        a[:, :3] = np.clip(rgb, 0, 1); img.pixels.foreach_set(a.ravel()); img.update(); img.pack()
        print('REPAINT', flt, img.name, img.size[:])
bb = [ob.matrix_world @ mathutils.Vector(c) for c in ob.bound_box]
mn = mathutils.Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb))); mx = mathutils.Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
h = mx.z - mn.z; s = 1.0 / h
ob.location = (-(mn.x + mx.x) * 0.5 * s, -(mn.y + mx.y) * 0.5 * s, -mn.z * s); ob.scale = (s, s, s)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tris1 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=90, export_apply=True)
print('HEAD', src, 'tris', tris0, '->', tris1, 'size before', tuple(round(v, 3) for v in (mx - mn)))
