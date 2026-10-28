import bpy, math, os
from mathutils import Vector

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(name, color, metallic=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metallic
    return m
white=mat('Ivory fabric',(.78,.8,.78)); trim=mat('Silver trim',(.48,.52,.53),.25); hair=mat('Silver white hair',(.72,.78,.79)); dark=mat('Black leather',(.025,.034,.045)); navy=mat('Navy trousers',(.012,.023,.095)); skin=mat('Skin',(.68,.32,.19)); teal=mat('Jade',(.015,.32,.28),.3)
def ball(name, loc, scale, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, location=loc); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(material); bpy.ops.object.shade_smooth(); return o
def tapered(name, a, b, r1, r2, material, vertices=24):
    d=Vector(b)-Vector(a); mid=(Vector(a)+Vector(b))/2
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=d.length, location=mid); o=bpy.context.object; o.name=name; o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); o.data.materials.append(material); bpy.ops.object.shade_smooth(); return o
def panel(name, coords, material):
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(coords, [], [list(range(len(coords)))]); mesh.update(); o=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(o); o.data.materials.append(material); s=o.modifiers.new('Fabric thickness','SOLIDIFY'); s.thickness=.025; b=o.modifiers.new('Soft edges','BEVEL'); b.width=.025; b.segments=2; return o
# Character faces toward negative Y; proportions are a stylized blockout.
ball('Torso',(0,0,2.40),(.48,.26,.57),white)
tapered('High collar',(0,0,2.72),(0,0,2.88),.28,.23,white)
ball('Head',(0,-.015,3.06),(.24,.20,.30),skin)
ball('Hair cap',(0,.015,3.25),(.245,.21,.20),hair)
ball('Hair bun',(0,.07,3.48),(.14,.14,.13),hair)
tapered('Bun tie',(0,.07,3.39),(0,.07,3.43),.12,.12,dark)
ball('Nose',(0,-.22,3.04),(.065,.09,.075),skin)
for side in [-1,1]:
    ball('Eyebrow',(side*.115,-.193,3.16),(.12,.035,.035),hair)
    tapered('Swept eyebrow',(side*.17,-.19,3.16),(side*.27,-.15,3.25),.045,.004,hair)
    tapered('Side hair',(side*.20,-.08,3.11),(side*.20,-.16,2.79),.07,.008,hair)
    ball('Shoulder',(side*.43,0,2.62),(.19,.22,.19),white)
    tapered('Sleeve',(side*.45,0,2.61),(side*.69,-.015,2.02),.18,.23,white)
    tapered('Sleeve cuff',(side*.69,-.015,2.02),(side*.71,-.02,1.94),.23,.23,trim)
    tapered('Dark wrist',(side*.71,-.02,1.94),(side*.72,-.025,1.88),.16,.14,dark)
    ball('Hand',(side*.73,-.03,1.75),(.115,.085,.17),skin)
    for i in range(4):
        x=side*(.66+i*.045); tapered('Finger',(x,-.07,1.72),(x+side*.012,-.07,1.59+(i%2)*.025),.022,.015,skin,12)
    ball('Trouser leg',(side*.25,.015,1.18),(.265,.24,.60),navy)
    tapered('Wrapped calf',(side*.26,.015,.69),(side*.26,.01,.30),.13,.10,dark)
    tapered('Sock',(side*.26,.01,.30),(side*.26,-.015,.16),.10,.105,white)
    ball('Shoe',(side*.26,-.12,.105),(.15,.27,.09),dark)
# Layered tapered beard and moustache.
ball('Beard volume',(0,-.22,2.76),(.20,.12,.25),hair)
for x in [-.15,-.10,-.05,0,.05,.10,.15]:
    tapered('Beard lock',(x,-.22,2.84),(x*.35,-.26,2.30+abs(x)*.35),.065,.004,hair)
for side in [-1,1]:
    tapered('Moustache',(side*.025,-.26,2.98),(side*.19,-.23,2.89),.045,.006,hair)
    tapered('Long hair strand',(side*.20,-.13,3.02),(side*.30,-.17,2.12),.035,.003,hair)
tapered('Belt',(0,0,2.05),(0,0,2.25),.41,.42,dark)
ball('Jade clasp',(-.31,-.30,2.16),(.055,.03,.055),teal)
panel('Front robe panel',[(-.31,-.255,2.03),(.31,-.255,2.03),(.32,-.30,1.02),(0,-.32,.90),(-.32,-.30,1.02)],white)
panel('Back robe panel',[(.34,.22,2.05),(-.34,.22,2.05),(-.42,.29,.95),(0,.31,.80),(.42,.29,.95)],white)
# Narrow dark eyes and layered garment seams provide facial and costume definition.
eye=mat('Eyes',(.025,.035,.04)); seam=mat('Embroidery',(.58,.61,.60))
for side in [-1,1]:
    ball('Eye',(side*.10,-.204,3.095),(.045,.014,.018),eye)
    tapered('Shoulder piping',(side*.28,-.20,2.75),(side*.51,-.19,2.58),.018,.018,trim,12)
    tapered('Robe border',(side*.28,-.318,1.98),(side*.29,-.327,1.07),.013,.013,trim,12)
    tapered('Decorative seam',(side*.20,-.331,1.67),(side*.08,-.339,1.19),.012,.006,seam,12)
for z in [2.11,2.16,2.21]:
    tapered('Belt braid',( -.32,-.285,z),(.32,-.285,z),.012,.012,trim,12)
# Neutral studio presentation.
floor=mat('Floor',(.11,.19,.27)); bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,0)); bpy.context.object.data.materials.append(floor)
world=bpy.context.scene.world; world.color=(.18,.18,.18)
def light(loc,energy,size):
    bpy.ops.object.light_add(type='AREA',location=loc); o=bpy.context.object; o.data.energy=energy; o.data.shape='DISK'; o.data.size=size; o.rotation_euler=(Vector((0,0,1.8))-o.location).to_track_quat('-Z','Y').to_euler()
light((4,-6,7),700,5); light((-4,-2,4),450,4); light((0,4,5),650,3)
bpy.ops.object.camera_add(location=(5,-9,4)); camera=bpy.context.object; camera.rotation_euler=(Vector((0,0,1.8))-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.type='ORTHO'; camera.data.ortho_scale=4.2
scene=bpy.context.scene; scene.camera=camera; scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.render.resolution_x=720; scene.render.resolution_y=900; scene.render.resolution_percentage=100
output_dir=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output_dir,'sage_character_refined.blend'))
scene.render.filepath=os.path.join(output_dir,'sage_character_refined.png'); bpy.ops.render.render(write_still=True)
