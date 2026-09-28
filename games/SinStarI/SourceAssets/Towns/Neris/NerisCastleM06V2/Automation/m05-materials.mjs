// Shared palette and portable masonry texture; owner-local final fittings.
export const build = String.raw`
s['milestone']='M05'
bpy.ops.wm.save_as_mainfile(filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M05-r001.blend')
for item in spec['materials']:
    m=bpy.data.materials[item['id']];m.use_nodes=True
    rgb=[int(item['srgb_hex'][i:i+2],16)/255 for i in [1,3,5]]
    lin=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb]
    m.diffuse_color=(*lin,1);p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*lin,1)
    p.inputs['Metallic'].default_value=item['metallic'];p.inputs['Roughness'].default_value=item['roughness']
    p.inputs['Emission Strength'].default_value=0
# A single portable colour texture supplies restrained joints to the major stone shells.
m=bpy.data.materials['NC.MAT.IvoryStone'].copy();m.name='NC.MAT.Masonry'
im=bpy.data.images.load('D:/Projects/Sin-Star-I-Assets/Neris-Castle/textures/neris-ivory-courses-1024.png',check_existing=True)
im.pack();im.filepath='//../textures/neris-ivory-courses-1024.png'
tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;tex.extension='REPEAT';tex.interpolation='Linear'
m.node_tree.links.new(tex.outputs['Color'],m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
shells=[]
for o in s.objects:
    n=o.name
    if o.type!='MESH' or not n.startswith('NC.'):continue
    use=(n.endswith('.Shaft') or n=='NC.Palace.Drum' or n in ['NC.Gatehouse.Jamb.West','NC.Gatehouse.Jamb.East','NC.Gatehouse.Arch.Vault','NC.Palace.Keep.Main','NC.Palace.Keep.Front.West','NC.Palace.Keep.Front.East','NC.Palace.Keep.EntranceVault','NC.Palace.Wing.West','NC.Palace.Wing.East','NC.Palace.RearGallery'] or (n.startswith('NC.Fortifications.Wall.') and n.count('.')==3))
    if not use:continue
    o.data.materials.clear();o.data.materials.append(m)
    for p in o.data.polygons:p.material_index=0
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='StoneMeters')
    cyl=n.endswith('.Shaft') or n=='NC.Palace.Drum'
    radius=13 if n=='NC.Palace.Drum' else o.dimensions.x/2
    period=math.tau*radius/8
    for p in o.data.polygons:
        normal=o.matrix_world.to_3x3()@p.normal
        axis=max(range(3),key=lambda i:abs(normal[i]))
        first=None
        for li in p.loop_indices:
            co=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            if cyl and axis!=2:
                u=math.atan2(co.y-o.location.y,co.x-o.location.x)*radius/8
                if first is None:first=u
                while u-first>period/2:u-=period
                while u-first<-period/2:u+=period
                v=co.z/4.8
            elif axis==2:u=co.x/8;v=co.y/4.8
            elif axis==0:u=co.y/8;v=co.z/4.8
            else:u=co.x/8;v=co.z/4.8
            uv.data[li].uv=(u,v)
    shells.append(n)
# The upper gate towers use the same established vocabulary, retaining their height category.
for side,x in [('West',-19),('East',19)]:
    prefix='NC.Gatehouse.Tower.'+side
    # Existing M02 mouldings remain; add four small arched recesses above banner height.
    for i in range(4):
        a=i*math.pi/2
        window(prefix+'.Window.'+str(i),'Gatehouse',bpy.data.objects[prefix+'.Shaft'],x+5*math.sin(a),-58-5*math.cos(a),13,1.8,4,2.8,.35,.18,a)
    for i in range(8):
        a=i*math.tau/8
        pipe(prefix+'.Roof.Rib.'+str(i),'Gatehouse',[(x+5.65*math.cos(a),-58+5.65*math.sin(a),20.07),(x+.16*math.cos(a),-58+.16*math.sin(a),27.9)],.05)
# Main crystal becomes a faceted crystal held by a small crown, within the same peak.
remove('NC.Palace.Crystal.Main')
lathe('NC.Palace.Crystal.Main','Palace',(0,34),[(.55,51),(1.8,53.5),(1.2,56),(0,60)],'CrystalBlue',6)
lathe('NC.Palace.Crystal.Crown','Palace',(0,34),[(1.6,50.9),(1.9,51.1),(1.9,51.55),(1.2,51.8)],'RoyalGold',16)
# Under-deck iron bracing travels with the same hinge and never extends beyond the source leaf.
pivot=bpy.data.objects['NC.Bridge.Pivot']
for i,y in enumerate([-17,-10,-3]):
    o=box('NC.Bridge.UnderBand.'+str(i),'Bridge',(-4.8,4.8),(y-.14,y+.14),(-.285,-.249),'DarkIron')
    o.parent=pivot
for i,ends in enumerate([((-4.5,-18,-.287),(4.5,-11,-.287)),((4.5,-9,-.287),(-4.5,-2,-.287))]):
    o=pipe('NC.Bridge.UnderBrace.'+str(i),'Bridge',ends,.055,'DarkIron');o.parent=pivot
s['finish_stone_shell_count']=len(shells)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
result={'masonry_shells':len(shells),'source_texture':[1024,1024],'shared_palette':11,'emission':'disabled on every source material'}
`;
