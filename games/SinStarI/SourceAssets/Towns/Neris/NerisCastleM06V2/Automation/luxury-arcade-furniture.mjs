// Shaded arcade benches and royal flower pots.
export const build=String.raw`
assert not s.get('luxury_arcade_furniture')
def garden_material(name,color):
    m=bpy.data.materials.get('NC.MAT.'+name)
    if not m:
        m=bpy.data.materials.new('NC.MAT.'+name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.65
    return m
for name,color in [('GardenWood',(.15,.065,.025)),('FlowerRose',(.56,.045,.16)),('FlowerIvory',(.88,.72,.42)),('FlowerViolet',(.22,.075,.48))]:garden_material(name,color)
furniture=[]
for side,sign in [('West',-1),('East',1)]:
    for index,yy in enumerate([-33,-16,1]):
        prefix='NC.Courtyard.Seating.'+side+'.'+str(index);x=sign*40.5
        # Length runs along the covered walk; the back faces the perimeter wall.
        for end in [-1,1]:
            y=yy+end*1.55
            for dx in [-.52,.52]:lathe(prefix+'.Foot.'+str(end)+'.'+str(dx),'Courtyard',(x+dx,y),[(.18,.03),(.22,.17),(.10,.68),(.19,.82)],'IvoryStone',12)
            pipe(prefix+'.Arm.'+str(end),'Courtyard',[(x-sign*.56,y,.85),(x-sign*.56,y,1.32),(x+sign*.48,y,1.38),(x+sign*.57,y,.9)],.065,'PolishedBrass')
        for i in range(5):
            xx=x-.6+i*.30
            bevel(box(prefix+'.SeatSlat.'+str(i),'Courtyard',(xx-.135,xx+.135),(yy-1.85,yy+1.85),(.78,.94),'GardenWood'),.045)
        for i in range(3):
            z=1.05+i*.22
            bevel(box(prefix+'.BackSlat.'+str(i),'Courtyard',(x+sign*.55-.07,x+sign*.55+.07),(yy-1.85,yy+1.85),(z,z+.17),'GardenWood'),.05)
        pipe(prefix+'.BackGold','Courtyard',[(x+sign*.55,yy-1.89,.83),(x+sign*.55,yy-1.89,1.68),(x+sign*.55,yy+1.89,1.68),(x+sign*.55,yy+1.89,.83)],.045,'PolishedBrass')
        furniture.append({'kind':'bench','x':x,'y':yy,'half_x':.76,'half_y':1.98})
        for end in [-1,1]:
            py=yy+end*3.1;name=prefix+'.FlowerPot.'+str(end)
            lathe(name,'Courtyard',(x,py),[(.39,.03),(.47,.18),(.40,.3),(.62,.86),(.65,1.05),(.57,1.12)],'IvoryStone',24)
            for z,r in [(.2,.46),(.97,.65)]:torus_ring(name+'.Gold.'+str(z),'Courtyard',(x,py),r,z,.045,'PolishedBrass')
            cone(name+'.Soil','Courtyard',(x,py),.55,.55,(1.04,1.055),'DarkIron',24)
            for j in range(9):
                a=j*2.399963;r=.15+.30*(j%3)/2;xx=x+r*math.cos(a);cy=py+r*math.sin(a);top=1.38+.36*(j%4)/3
                pipe(name+'.Stem.'+str(j),'Courtyard',[(xx,cy,1.04),(xx+.045*math.cos(a),cy+.045*math.sin(a),top)],.019,'Cypress1')
                verts=[];faces=[]
                for k in range(5):
                    aa=k*math.tau/5;cx=xx+.045*math.cos(a);cc=cy+.045*math.sin(a);n=len(verts)
                    verts.extend([(cx,cc,top-.035),(cx+.12*math.cos(aa-.5),cc+.12*math.sin(aa-.5),top+.045),(cx+.23*math.cos(aa),cc+.23*math.sin(aa),top+.02),(cx+.12*math.cos(aa+.5),cc+.12*math.sin(aa+.5),top+.045)])
                    faces.append((n,n+1,n+2,n+3))
                mesh(name+'.Flower.'+str(j),'Courtyard',verts,faces,['FlowerRose','FlowerIvory','FlowerViolet'][j%3])
                leaf=[(xx,cy,top-.22),(xx+.28*math.cos(a),cy+.28*math.sin(a),top-.11),(xx+.11*math.cos(a+.7),cy+.11*math.sin(a+.7),top-.27)]
                mesh(name+'.Leaf.'+str(j),'Courtyard',leaf,[(0,1,2)],'Cypress1')
            furniture.append({'kind':'pot','x':x,'y':py,'radius':.73})
s['luxury_arcade_furniture']=json.dumps(furniture)
s.camera=review_camera('NC.CAM.ArcadeFurnishings',(14,-52,12),(40.5,-20,3.1),48)
s.render.resolution_x=1600;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r003/arcade-furnishings.png'
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
result={'image':s.render.filepath,'benches':6,'flower_pots':12,'collision_footprints':furniture}
`;
