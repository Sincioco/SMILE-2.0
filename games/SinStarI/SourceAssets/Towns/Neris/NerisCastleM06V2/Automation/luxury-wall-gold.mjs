// Continuous gold plinth and wall-end trim.
export const build=String.raw`
assert not bpy.data.objects.get('NC.Fortifications.Luxury.GoldFrame.West.Base')
runs=[('SouthWest',-44.2,-14.0,0,-64,0),('SouthEast',14.0,44.2,0,-64,0),('West',-56.2,56.2,-52,0,-math.pi/2),('East',-56.2,56.2,52,0,math.pi/2),('North',-44.2,44.2,0,64,math.pi)]
for side,a,b,x,y,angle in runs:
    prefix='NC.Fortifications.Luxury.GoldFrame.'+side
    for suffix,z in [('Base',.72),('Plinth',2.17)]:
        o=box(prefix+'.'+suffix,'Fortifications',(a-.24,b+.24),(-.535,-.46),(z,z+.075),'PolishedBrass');place(o,x,y,angle)
    for i,xx in enumerate([a,b]):
        o=box(prefix+'.End.'+str(i),'Fortifications',(xx-.036,xx+.036),(-.55,-.485),(.72,11.78),'PolishedBrass');place(o,x,y,angle)
n=bpy.data.scenes['NC.Review.Night.M06-r003'];n.compositing_node_group=n.compositing_node_group.copy();n.compositing_node_group.name='NC.Review.NightGlow'
for node in n.compositing_node_group.nodes:
    if node.type=='R_LAYERS':node.scene=n
bpy.ops.render.render(write_still=True,scene=n.name)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter'];bpy.context.window.scene=s
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
result={'image':n.render.filepath,'gold_wall_sections':len(runs)}
`;
