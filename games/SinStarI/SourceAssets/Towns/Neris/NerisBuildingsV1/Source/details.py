"""Reusable carved openings, heraldry, roof details, and shop dressing."""
import math

from geometry import face_point, on_face


def arch_outline(w, h):
    r = w/2
    shoulder = h*0.64
    left = [(-r+r*(i/16)**2, shoulder+(h-shoulder)*(i/16)) for i in range(17)]
    return [(-r,0)] + left + [(-x,z) for x,z in reversed(left[:-1])] + [(r,0)]


def opening(g, side, plane, x, bottom, width, height, door=False, glass='glass'):
    shape = arch_outline(width, height)
    on_face(g.prism('Arched Recess', shape, 0.13, 'dark'), side, plane, x, bottom, 0.15)
    outline = [(px,0,pz) for px,pz in shape]
    on_face(g.path('Carved Arch Surround', outline, 0.14, 'trim',True),
            side, plane, x, bottom, 0.20)
    on_face(g.path('Gold Arch Molding', outline, 0.038, 'gold',True),
            side, plane, x, bottom, 0.34)
    small = [(px*0.87,pz*0.93+0.045) for px,pz in shape]
    on_face(g.prism('Closed Door' if door else 'Amber Glazing',small,0.05,
                    'iron' if door else glass), side, plane,x,bottom,0.24)
    p = lambda u,z,out=0.32: face_point(side,plane,x+u,bottom+z,out)
    for u in [-width*0.27,0,width*0.27]:
        g.beam('Gold Door Stile' if door else 'Window Mullion',
               p(u,0.10),p(u,height*(0.72 if u else 0.93)),0.028,'gold')
    g.beam('Window Transom',p(-width*.43,height*.58),p(width*.43,height*.58),.03,'gold')
    sill=g.box('Carved Stone Sill',(0,0,0),(width+.35,.48,.14),'trim')
    on_face(sill,side,plane,x,bottom,0.18)
    if door:
        for u in [-width*.23,width*.23]:
            for z in [height*.24,height*.5]:
                g.path('Door Diamond Inlay',[p(u-width*.16,z),p(u,z+height*.12),
                        p(u+width*.16,z),p(u,z-height*.12)],.026,'gold',True)
        for u in [-.12,.12]:
            g.beam('Door Handle',p(u,height*.33,.39),p(u,height*.42,.39),.035,'brightgold')


def crystal(g, loc, height, radius):
    x,y,z=loc
    obj=g.lathe('Faceted Cyan Crystal',[(radius*.45,0),(radius,height*.16),
                    (radius,height*.70),(0,height)],loc,'crystal',6,False)
    obj.data.materials.append(g.materials['crystaledge'])
    for poly in obj.data.polygons:
        poly.material_index=1 if poly.index%3==0 else 0
    g.cylinder('Crystal Gold Socket',(x,y,z+.02),radius*1.22,.12,'gold')
    return obj


def column(g,x,y,h,width=0.9,base=0.0,has_crystal=True):
    g.box('Pillar Shaft',(x,y,base+h/2),(width,width,h),'trim',.065)
    for z,w,d in [(0,.36,.22),(.24,.19,.20),(.7,.08,.11),(h*.50,.08,.13),
                  (h-.28,.19,.23),(h,.27,.18)]:
        g.box('Pillar Molding',(x,y,base+z+d/2),(width+w,width+w,d),'trim')
    for z in [h-.08,h-.4]:
        g.box('Pillar Gold Collar',(x,y,base+z),(width+.22,width+.22,.055),'gold',.01)
    if has_crystal:
        crystal(g,(x,y,base+h+.15),width*1.25,width*.22)
    for z in [.95,1.65,2.35,3.05,3.75,4.45,5.15,5.85,6.55,7.25,7.95,8.65,9.35]:
        if z<h-.4:
            g.box('Pillar Stone Joint',(x,y-.003,base+z),(width+.004,width+.004,.012),'stone',0)


def crystal_fixture(g,side,plane,x,z,height=1.6):
    pos=face_point(side,plane,x,z,.21)
    shape=[(-.17,0),(0,-.35),(.17,0),(.25,height),(-.25,height)]
    on_face(g.prism('Gold Crystal Wall Mount',shape,.19,'gold'),side,plane,x,z,.18)
    crystal(g,face_point(side,plane,x,z+.09,.37),height*.88,.14)
    for h in [0,height]:
        on_face(g.box('Crystal Mount Collar',(0,0,0),(.6,.46,.13),'trim'),
                side,plane,x,z+h,.26)


def dome(g,loc,radius,rise):
    x,y,z=loc
    profile=[(radius*math.cos(i*math.pi/48),rise*math.sin(i*math.pi/48)) for i in range(25)]
    obj=g.lathe('Teal Ribbed Dome',profile,loc,'roof',64)
    obj.data.materials.append(g.materials['roofalt'])
    for face in obj.data.polygons:
        face.material_index=(face.index%64)//4%2
    for j in range(16):
        a=j*math.tau/16
        g.path('Dome Gold Meridian',[(x+(r+.025)*math.cos(a),y+(r+.025)*math.sin(a),z+h+.025)
                                   for r,h in profile],.037,'gold')
    for level in [0.0,.12]:
        g.lathe('Dome Base Cornice',[(radius+.12,level),(radius+.12,level+.09)],loc,'gold',64)
    g.cylinder('Dome Crown',(x,y,z+rise),.36,.18,'gold')


def star(g,side,plane,x,z,size=0.5,outward=.08):
    pts=[]
    for i in range(8):
        a=i*math.pi/4
        radius=size if i%2==0 else size*.24
        pts.append((math.sin(a)*radius,math.cos(a)*radius*1.32))
    obj=g.prism('Neris Four Point Star',list(reversed(pts)),.06,'brightgold')
    on_face(obj,side,plane,x,z,outward)


def banner(g,side,plane,x,top,width=1.45,height=4.0,words='NERIS'):
    outline=[(-width/2,0),(-width/2,-height+.5),(0,-height),(width/2,-height+.5),(width/2,0)]
    on_face(g.prism('Teal Heraldic Banner',outline,.075,'cloth'),side,plane,x,top,.35)
    on_face(g.path('Banner Gold Border',[(u,-.06,v) for u,v in outline],.038,'gold',True),
            side,plane,x,top,.36)
    g.beam('Banner Hanging Rail',face_point(side,plane,x-width*.64,top+.1,.35),
           face_point(side,plane,x+width*.64,top+.1,.35),.07,'gold')
    star(g,side,plane,x,top-height*.28,width*.27,.44)
    for i,word in enumerate(words.split('|')):
        g.text('Banner Motto',word,face_point(side,plane,x,top-height*.59-i*.29,.44),
               min(.18,width*.14),rotation=side*math.pi/2)


def lantern(g,side,plane,x,z,scale=1):
    loc=face_point(side,plane,x,z,.50)
    X,Y,Z=loc
    g.cylinder('Lantern Glass',(X,Y,Z),.15*scale,.56*scale,'lamp',8)
    for dz in [-.34,.34]:
        g.lathe('Lantern Cap',[(.21*scale,dz*scale),(.13*scale,(dz+.12)*scale)],loc,'gold',8)
    for a in [0,math.pi/2,math.pi,3*math.pi/2]:
        xx,yy=X+.145*scale*math.cos(a),Y+.145*scale*math.sin(a)
        g.beam('Lantern Cage',(xx,yy,Z-.31*scale),(xx,yy,Z+.34*scale),.022*scale,'iron')
    g.beam('Lantern Wall Bracket',face_point(side,plane,x,z+.45*scale,.08),
           (X,Y,Z+.45*scale),.04,'iron')


def canopy(g,side,plane,x,z,width=5,reach=1.25):
    verts=[(-width/2,0,0),(width/2,0,0),(width/2,-reach,-.58),(-width/2,-reach,-.58)]
    obj=g.mesh('Teal Canopy',verts,[(0,1,2,3)],'roof')
    on_face(obj,side,plane,x,z,.30)
    solid=obj.modifiers.new('Canopy Thickness','SOLIDIFY'); solid.thickness=.07
    for u in [-width/2,-width/4,0,width/4,width/2]:
        g.beam('Canopy Gold Rib',face_point(side,plane,x+u,z,.30),
               face_point(side,plane,x+u,z-.58,reach+.30),.035,'gold')
    g.beam('Canopy Leading Edge',face_point(side,plane,x-width/2,z-.58,reach+.30),
           face_point(side,plane,x+width/2,z-.58,reach+.30),.055,'gold')
    for u in [-width*.46,width*.46]:
        g.beam('Canopy Brace',face_point(side,plane,x+u,z-1.4,.15),
               face_point(side,plane,x+u,z-.60,reach+.20),.07,'iron')


def signboard(g,side,plane,z,width,title):
    outline=[(-width/2,-.65),(width/2,-.65),(width/2,.65),
             (width*.28,.78),(0,1.17),(-width*.28,.78),(-width/2,.65)]
    on_face(g.prism('Sculpted Shop Sign',outline,.25,'cloth'),side,plane,0,z,.60)
    on_face(g.path('Shop Sign Gold Frame',[(x,-.16,h) for x,h in outline],.07,'gold',True),
            side,plane,0,z,.62)
    for value,dz,size in [('NERIS',.44,.31),(title,-.03,min(.34,5.8/len(title))),
                           ('A BRIGHTER TOMORROW',-.44,.125)]:
        g.text('Raised Sign Lettering',value,face_point(side,plane,0,z+dz,.81),
               size,rotation=side*math.pi/2)
    for x in [-width/2,width/2]:
        a=face_point(side,plane,x,z-.8,.62); b=face_point(side,plane,x,z+.9,.62)
        g.beam('Sign Frame Pillar',a,b,.10,'gold')


def shield(g,side,plane,x,z,size=1):
    shape=[(-.6,.65),(-.55,-.12),(0,-.85),(.55,-.12),(.6,.65),(0,.85)]
    shape=[(u*size,v*size) for u,v in shape]
    on_face(g.prism('Neris Shield',shape,.14*size,'roof'),side,plane,x,z,.3)
    on_face(g.path('Shield Rim',[(u,-.09*size,v) for u,v in shape],.045*size,'gold',True),
            side,plane,x,z,.31)
    star(g,side,plane,x,z,size*.31,.43)


def sword(g,side,plane,x,z,length=2.5,angle=0,cyan=False):
    # Local sword blade and hilt are transformed together for crossed emblems.
    shape=[(-.11,0),(.11,0),(.15,length*.72),(0,length),(-.15,length*.72)]
    transform=lambda u,v:(u*math.cos(angle)+v*math.sin(angle),-u*math.sin(angle)+v*math.cos(angle))
    shape=[transform(u,v) for u,v in shape]
    on_face(g.prism('Sword Blade',shape,.08,'crystal' if cyan else 'trim'),side,plane,x,z,.35)
    for label,a,b,r,mat in [('Sword Guard',(-.4,.10),(.4,.10),.055,'gold'),
                           ('Sword Grip',(0,-.4),(0,.08),.07,'iron')]:
        a,b=transform(*a),transform(*b)
        g.beam(label,face_point(side,plane,x+a[0],z+a[1],.38),
               face_point(side,plane,x+b[0],z+b[1],.38),r,mat)


def crate(g,loc,size=.85):
    x,y,z=loc
    g.box('Wooden Shipping Crate',(x,y,z+size/2),(size,size,size),'wood',.02)
    for dz in [.08,size-.08]:
        g.box('Crate Iron Band',(x,y-.01,z+dz),(size+.04,size+.04,.075),'iron',.012)
    for dx in [-size*.4,size*.4]:
        g.box('Crate Frame',(x+dx,y-size/2-.025,z+size/2),(.08,.06,size),'woodalt',.01)
    g.beam('Crate Diagonal Brace',(x-size*.4,y-size/2-.07,z+.08),
           (x+size*.4,y-size/2-.07,z+size-.08),.045,'woodalt')


def barrel(g,loc,size=1):
    obj=g.lathe('Coopered Barrel',[(.32*size,0),(.4*size,.12*size),(.46*size,.5*size),
                (.4*size,.88*size),(.32*size,size)],loc,'wood',16,False)
    x,y,z=loc
    for h,r in [(.13,.405),(.50,.465),(.87,.405)]:
        g.cylinder('Barrel Iron Hoop',(x,y,z+h*size),r*size,.065*size,'iron')
    return obj


def planter(g,loc,width=1.4,tall=False):
    x,y,z=loc
    g.box('Stone Planter',(x,y,z+.28),(width,width,.56),'stone')
    g.box('Planter Rim',(x,y,z+.58),(width+.15,width+.15,.15),'trim')
    for i in range(7 if not tall else 5):
        a=i*2.4
        xx=x+math.cos(a)*width*.23; yy=y+math.sin(a)*width*.23
        height=1.1+(i%3)*.25 if tall else .35+(i%3)*.09
        g.lathe('Clipped Cypress' if tall else 'Leaf Cluster',
                [(.03,0),(width*.22,height*.15),(width*.24,height*.50),(.04,height)],
                (xx,yy,z+.61),'leaf' if i%2 else 'leaflight',9,False)


def bottle(g,loc,scale=1,blue=True):
    g.lathe('Potion Bottle',[(.085,0),(.14,.08),(.145,.20),(.07,.3),(.048,.33),(.048,.46)],
            loc,'bottle' if blue else 'glass',12)
    x,y,z=loc
    g.cylinder('Bottle Stopper',(x,y,z+.46),.052,.07,'wood',10)


def stairs(g,width,front,steps=5,tread=.28,rise=.15):
    for i in range(steps):
        g.box('Entrance Step',(0,front-(steps-i)*tread/2,(i+1)*rise/2),
               (width,(steps-i)*tread,(i+1)*rise),'trim',.02)
