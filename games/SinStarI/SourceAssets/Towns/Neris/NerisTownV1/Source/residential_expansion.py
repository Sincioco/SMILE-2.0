"""Three reference-led, separately editable residential templates."""
import bpy
from expansion_architecture import Architecture, arch, block, dome, hip_roof, balcony
from expansion_architecture import planter, buttress, cypress
from details import banner, lantern, crystal


def create_homes(mats):
    result={}
    for style,w,d,h in [('Large',15,12,12),('Medium',10,9,9),('Small',8,7,7.5)]:
        g=Architecture('Neris New '+style+' Home',mats)
        g.root['Design Reference']='Neris - Home - '+style+'.png'
        g.box('House Plinth',(0,0,.17),(w+1,d+1,.34),'trim',.06)
        block(g,'Ivory Residence',0,0,w,d,h,.30)
        for x in [-w/2,w/2]:
            for y in [-d/2,d/2]:
                buttress(g,x,y,h+.8)
        hip_roof(g,0,0,h+.6,w+1,d+1,2.2 if style=='Large' else 1.9)
        if style=='Large':
            block(g,'Central Grand Bay',0,-d/2+.65,5,2.5,h+2,.3)
            arch(g,0,d/2+.65,0,5.9,3.7,7.3,glass='roof')
            dome(g,0,1,h+1,4.7,3.1)
            for x in [-w*.38,w*.38]:
                balcony(g,x,-d/2-.18,5.4,3.6,1.6)
                planter(g,x,-d/2-1.1,.2,1.2)
            for x in [-w/2-1,w/2+1]:
                cypress(g,x,-d/2+.5,.2,4.0)
        else:
            dome(g,0,.2,h+1,w*.38,1.9)
            balcony(g,0,-d/2-.15,h*.52,w*.72,1.0)
        doors=[-2.3,2.3] if style=='Medium' else [0]
        door_plane=d/2+(.65 if style=='Large' else .02)
        for x in doors:
            arch(g,0,door_plane,x,.38,1.55 if style!='Large' else 2.5,2.9,True)
            for dx in [-1.1,1.1]:
                lantern(g,0,door_plane,x+dx,2.5,.9)
        for side,plane,positions in [(0,d/2,[-w*.32,w*.32]),(2,d/2,[-w*.28,w*.28]),
                                      (1,w/2,[-d*.25,d*.25]),(3,w/2,[-d*.25,d*.25])]:
            for x in positions:
                for z in [1.0,h*.56]:
                    arch(g,side,plane,x,z,1.12,2.25)
        for side in [0,2]:
            banner(g,side,d/2+.1,0,h+.2,1.1,3.2,'')
        for side in [1,3]:
            banner(g,side,w/2+.1,0,h-.2,1.0,3.0,'')
        for x in [-w*.37,w*.37]:
            planter(g,x,-d/2-.9,.18,.9)
        for i in range(3):
            g.box('Front Door Step',(0,-d/2-1.4+i*.3,.05+i*.07),
                  (4.0,1.4-i*.3,.10+i*.14),'trim',.02)
        if style=='Small':
            g.cylinder('Stone Chimney',(-2.2,1.8,h+1.9),.36,4,'trim',12)
            g.cylinder('Chimney Cap',(-2.2,1.8,h+4),.48,.20,'iron',12)
        bpy.context.scene.collection.children.unlink(g.collection)
        result[style]=g.collection
    return result
