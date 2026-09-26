"""Place the existing detailed trees and street furniture around the new districts."""
import math
import random
import bpy


def place(obj,x,y,z=None,yaw=None):
    world=obj.matrix_world.copy()
    obj.parent=None;obj.matrix_world=world
    obj.location.x=x;obj.location.y=y
    if z is not None:obj.location.z=z
    if yaw is not None:obj.rotation_euler.z=yaw


def arrange(layout):
    bpy.context.view_layer.update()
    sites=[]
    # Spacious estates: four shade trees per garden; middle homes one each.
    for home in layout['homes']:
        x,y=home['x'],home['y']
        if home['style']=='Large':
            sites += [(x+dx,y+dy) for dx in (-16,16) for dy in (-22,22)]
        elif home['style']=='Medium':sites.append((x-12,y+15))
    sites += [(x,y) for x in (-32,32) for y in (-132,-198,-236,-300)]
    sites += [(-220,112),(-170,112),(-68,112),(40,110),(210,112)]
    sites += [(x,316) for x in (-190,-145,-90,-40)]
    sites += [(222,y) for y in (196,240,292)]
    sites += [(100,184),(200,184),(115,318),(180,318)]
    sites += [(95,y) for y in (-148,-200,-252,-312)]
    sites += [(225,y) for y in (-152,-208,-260)]
    sites += [(101,75),(222,73),(101,-95),(222,-95)]
    sites += [(-460,325),(-271,325),(-460,125),(-274,125)]
    sites += [(-65,75),(48,75),(-65,5),(50,5)]
    trees=sorted([o for o in bpy.data.objects if o.name.startswith('Garden Tree ')],key=lambda o:o.name)
    # Keep the existing bounded native allocation and reusable mesh templates.
    sites += [(x,-334) for x in (-208,-152,-98,98,152,208)]
    sites += [(x,124) for x in (-210,-165,-68,38,75,225)]
    assert len(sites)>=len(trees)
    original_count=len(trees)
    sites=sites[:original_count]
    for x in (-13,13):
        for y in (-310,-292,-260,-242,-224,-206,-188,-152,-134):
            tree=trees[0].copy()
            tree.name='Garden Tree Park '+str(len(trees)+1)
            trees[0].users_collection[0].objects.link(tree)
            trees.append(tree)
            sites.append((x,y))
    for x in (-220,-186,-158,-103):
        for y in (-68,-98):
            tree=trees[0].copy();tree.name='Garden Tree Estate Park '+str(len(trees)+1)
            trees[0].users_collection[0].objects.link(tree)
            trees.append(tree);sites.append((x,y))
    park_centers=[(-150,-167),(-150,-243),(-150,-287),(150,50),(150,-54),
                  (214,-180),(214,-282),(128,-301),(150,-181)]
    for x,y in park_centers:
        for dx,dy in ((-9,-12),(9,12),(-9,12),(9,-12)):
            tree=trees[0].copy();tree.name='Garden Tree Neighborhood '+str(len(trees)+1)
            trees[0].users_collection[0].objects.link(tree)
            trees.append(tree);sites.append((x+dx,y+dy))
    street_sites=[(x,y) for x in (-219,238) for y in (-308,-220,-130,0,95,124)]
    for x,y in street_sites:
        tree=trees[0].copy();tree.name='Garden Tree Avenue '+str(len(trees)+1)
        trees[0].users_collection[0].objects.link(tree)
        trees.append(tree);sites.append((x,y))
    rng=random.Random(260926)
    occupied=[]
    buildings=[(h['x']-h['depth']/2-1,h['y']-h['width']/2-1,
                h['x']+h['depth']/2+1,h['y']+h['width']/2+1) for h in layout['homes']]
    buildings += [(x-6,y-6,x+6,y+6) for x,y,_ in layout['legacyHomes']]
    buildings += [(112,y-13,138,y+13) for y in (-156,-208,-260)]
    regions=layout['waterRectangles']+layout['roadRectangles']+buildings
    regions += [(-193,165,-39,303),(68,173,222,311),(-443,139,-289,313)]
    def clear(x,y,radius):
        if not any(a+radius<x<c-radius and b+radius<y<d-radius for a,b,c,d in layout['land']):return False
        if any(a-radius-.6<x<c+radius+.6 and b-radius-.6<y<d+radius+.6 for a,b,c,d in regions):return False
        return not any(math.hypot(x-px,y-py)<radius+pr+.7 for px,py,pr in occupied)
    layout['decorations']=[]
    def safe(x,y,radius,label):
        candidates=[(x+dx,y+dy) for r in (0,3,6,9,12,18,26,36,48,64,96,128,160,192,224)
                    for dx,dy in ((r,0),(-r,0),(0,r),(0,-r),(r,r),(-r,r),(r,-r),(-r,-r))]
        result=next((p for p in candidates if clear(*p,radius)),None)
        assert result is not None, f'No clear garden position for {label} at {x},{y}'
        occupied.append((*result,radius))
        layout['decorations'].append(dict(name=label,x=result[0],y=result[1],radius=radius))
        return result
    for index,(obj,(x,y)) in enumerate(zip(trees,sites)):
        x,y=safe(x,y,1.25,obj.name)
        place(obj,x,y,.03,rng.uniform(0,math.tau))
        height=rng.uniform(1.25,1.55) if index>=original_count else rng.uniform(.8,1.32)
        width=height*rng.uniform(.9,1.06)
        obj.scale=(width,width,height)
    pots=sorted([o for o in bpy.data.objects if o.instance_collection and
                 ('Flower' in o.instance_collection.name)],key=lambda o:o.name)
    sites=[(x,y) for y in (-112,-148,-212,-250,-310) for x in (-20,20)]
    sites += [(-184,-6),(-118,-6),(-184,60),(-118,60),(131,-144),(131,-196),(131,-248)]
    for obj,(x,y) in zip(pots,sites):
        x,y=safe(x,y,1.7,obj.name);place(obj,x,y,.18)
    for x in (-20,20):
        for y in (-304,-248,-212,-144):
            obj=pots[0].copy();obj.name='Park Flower Planter'
            pots[0].users_collection[0].objects.link(obj)
            px,py=safe(x,y,1.7,obj.name);place(obj,px,py,.18)
    lamps=sorted([o for o in bpy.data.objects if o.instance_collection and
                  o.instance_collection.name=='Crystal Lamp'],key=lambda o:o.name)
    sites=[(x,y) for y in (-112,-150,-210,-250,-306) for x in (-10,10)]
    sites += [(x,y) for x in (-80,80) for y in (-110,-170,-280,108)]
    sites += [(-125,120),(-107,120),(141,178),(159,178),(-375,101),(-357,101),
              (105,-135),(105,-187),(105,-239),(207,-135),(207,-187),(207,-239)]
    for obj,(x,y) in zip(lamps,sites):
        x,y=safe(x,y,.5,obj.name);place(obj,x,y,.18)
    benches=sorted([o for o in bpy.data.objects if o.instance_collection and
                    o.instance_collection.name=='Bench'],key=lambda o:o.name)
    sites=[(x,y) for y in (-150,-185,-235,-295) for x in (-26,26)]
    sites += [(-178,17),(-112,81),(199,-169),(199,-221)]
    for obj,(x,y) in zip(benches,sites):
        x,y=safe(x,y,1.5,obj.name);place(obj,x,y,.21,math.pi/2 if x<0 else -math.pi/2)
    for x in (-21,21):
        for y in (-298,-252,-216,-140):
            obj=benches[0].copy();obj.name='Park Bench'
            benches[0].users_collection[0].objects.link(obj)
            px,py=safe(x,y,1.5,obj.name);place(obj,px,py,.21,math.pi/2 if x<0 else -math.pi/2)
    # Two quiet estate parks occupy the formerly empty lawns, away from streets.
    from geometry import Geometry
    from props import fountain
    mats={key:bpy.data.materials[name] for key,name in {
        'stone':'Ivory Limestone','trim':'Pale Carved Stone','gold':'Aged Gold',
        'water':'Royal Deep Blue Water','crystal':'Cyan Relay Crystal',
        'crystaledge':'Crystal Bright Facets','roof':'Teal Enamel Roof'}.items()}
    garden=Geometry('Waterfront Estate Garden Features',mats)
    for center in (-202,-130):
        fx,fy=safe(center,-82,5.1,'Estate Fountain');fountain(garden,fx,fy)
        for x,y in [(center-10,-72),(center+10,-72),(center-10,-92),(center+10,-92)]:
            obj=benches[0].copy();obj.name='Estate Park Bench'
            benches[0].users_collection[0].objects.link(obj)
            px,py=safe(x,y,1.5,obj.name);place(obj,px,py,.21,0 if y<-82 else math.pi)
            obj=pots[0].copy();obj.name='Estate Park Flower Planter'
            pots[0].users_collection[0].objects.link(obj)
            px,py=safe(x,y+3,1.7,obj.name);place(obj,px,py,.18)
        for x in (center-16,center+16):
            obj=lamps[0].copy();obj.name='Estate Park Crystal Lamp'
            lamps[0].users_collection[0].objects.link(obj)
            px,py=safe(x,-82,.5,obj.name);place(obj,px,py,.18)
    from details import crystal
    for index,(x,y) in enumerate(park_centers):
        x,y=safe(x,y,5.1,'Neighborhood Fountain' if index in (1,6,8) else 'Wayfarer Circle')
        if index in (1,6,8):
            fountain(garden,x,y)
        else:
            garden.cylinder('Wayfarer Plaza',(x,y,.22),4.7,.3,'trim',48)
            garden.cylinder('Wayfarer Teal Inlay',(x,y,.4),3.9,.055,'roof',48)
            for i in range(8):
                a=i*math.pi/4
                garden.path('Arrival Gold Rays',[(x+math.cos(a)*r,y+math.sin(a)*r,.44)
                            for r in (1,3.6)],.055,'gold')
            for i in range(3):
                a=math.pi/2+i*math.tau/3
                px,py=x+3.35*math.cos(a),y+3.35*math.sin(a)
                garden.lathe('Wayfarer Pylon',[(.45,0),(.3,1.2),(.22,2)],(px,py,.4),'trim',8)
                crystal(garden,(px,py,2.4),1.05,.21)
        for dx,dy in ((-8,0),(8,0)):
            for template,label,radius,z in ((benches[0],'Neighborhood Bench',1.5,.21),
                                           (pots[0],'Neighborhood Flower Planter',1.7,.18)):
                obj=template.copy();obj.name=label
                template.users_collection[0].objects.link(obj)
                px,py=safe(x+dx,y+dy,radius,label);place(obj,px,py,z,math.pi/2)
        obj=lamps[0].copy();obj.name='Neighborhood Crystal Lamp'
        lamps[0].users_collection[0].objects.link(obj)
        px,py=safe(x,y-8,.5,obj.name);place(obj,px,py,.18)
    for index,(x,y) in enumerate(street_sites):
        for template,label,radius,z in ((lamps[0],'Avenue Crystal Lamp',.5,.18),
                                       (benches[0],'Avenue Bench',1.5,.21)):
            obj=template.copy();obj.name=label
            template.users_collection[0].objects.link(obj)
            px,py=safe(x,y+6,radius,label);place(obj,px,py,z,math.pi/2)
    print('LANDSCAPE retained'  ,len(trees),'varied trees,',len(pots),'flower pots,',len(lamps),'lamps',flush=True)
