import bpy, json, math, os
from mathutils import Vector

source = r'D:\3D printing\Figures\Crucible knight.stl'
out = os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.wm.stl_import(filepath=source)
obj = bpy.context.selected_objects[0]
obj.name = 'Crucible Knight STL'
bpy.context.view_layer.update()
print('INSPECTION ' + json.dumps({'vertices':len(obj.data.vertices), 'faces':len(obj.data.polygons), 'dimensions':list(obj.dimensions), 'bounds_min':[min((obj.matrix_world @ Vector(c))[i] for c in obj.bound_box) for i in range(3)], 'bounds_max':[max((obj.matrix_world @ Vector(c))[i] for c in obj.bound_box) for i in range(3)]}))
center = obj.matrix_world @ Vector(tuple(sum(c[i] for c in obj.bound_box)/8 for i in range(3)))
span = max(obj.dimensions)
mat = bpy.data.materials.new('Neutral clay'); mat.diffuse_color=(.55,.52,.46,1); obj.data.materials.append(mat)
world = bpy.context.scene.world; world.color=(.25,.25,.25)
def area(offset, energy, size):
    bpy.ops.object.light_add(type='AREA', location=center+Vector(offset)*span)
    light=bpy.context.object; light.data.energy=energy*span*span; light.data.size=size*span
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
area((1,-2,2),500,2); area((-2,-1,1),300,2); area((0,2,2),550,2)
bpy.ops.object.camera_add(location=center+Vector((1.6,-2.6,1.2))*span)
cam=bpy.context.object; cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=span*1.5
scene=bpy.context.scene; scene.camera=cam; scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=800; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'crucible_knight_inspection.blend'))
scene.render.filepath=os.path.join(out,'crucible_knight_inspection.png'); bpy.ops.render.render(write_still=True)
