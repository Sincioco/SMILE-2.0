"""Newly modeled royal stonework and tracery; no imported mesh or baked textures."""
import math
from geometry import face_point, on_face
from details import arch_outline, crystal, lantern, star


def framed_window(g, side, plane, u, z, width, height, glass='roof'):
    """Layered dark reveal, glazing, carved jambs and slender bronze tracery."""
    outline = arch_outline(width, height)
    p = lambda x, h, out=.42: face_point(side, plane, u+x, z+h, out)
    on_face(g.prism('Royal Window Shadow Reveal', outline, .12, 'dark'), side, plane, u, z, .08)
    on_face(g.prism('Royal Teal Glazing', [(x*.87, h*.94+.12) for x,h in outline], .08, glass),
            side, plane, u, z, .18)
    for scale, radius, out, mat in [(1.12,.20,.13,'trim'),(1.0,.065,.34,'gold'),(.88,.065,.35,'trim')]:
        g.path('Carved Royal Archivolt', [p(x*scale,h*scale,out) for x,h in outline],radius,mat,True)
    for x in [-width*.48, width*.48]:
        g.beam('Window Engaged Column',p(x,.08,.36),p(x,height*.64,.36),.11,'trim')
        for h in [.13,height*.63]:
            on_face(g.box('Window Capital',(0,0,0),(.37,.45,.23),'trim',.025),side,plane,u+x,z+h,.37)
    for x in [-width*.25,0,width*.25]:
        g.beam('Royal Window Bronze Mullion',p(x,.15),p(x,height*(.90 if x==0 else .69)),.045,'gold')
        points=[p(x-width*.20,height*.60),p(x-width*.12,height*.74),
                p(x,height*.80),p(x+width*.12,height*.74),p(x+width*.20,height*.60)]
        g.path('Twin Lancet Tracery',points,.04,'gold')
    g.beam('Royal Window Transom',p(-width*.4,height*.47),p(width*.4,height*.47),.045,'gold')
    on_face(g.box('Projecting Royal Window Sill',(0,0,0),(width*1.27,.72,.28),'trim',.04),
            side,plane,u,z-.06,.24)


def pilaster(g, side, plane, u, bottom, height, width=.9):
    for name, w, depth, z, h, mat in [
            ('Royal Pier',width,.50,bottom+height/2,height,'trim'),
            ('Royal Pier Foot',width+.55,.88,bottom+.30,.60,'trim'),
            ('Royal Pier Capital',width+.55,.88,bottom+height-.24,.48,'trim'),
            ('Royal Pier Bronze Collar',width+.58,.91,bottom+height-.52,.09,'gold')]:
        on_face(g.box(name,(0,0,0),(w,depth,h),mat,.035),side,plane,u,z,.22)
    for i in range(1,int(height/1.8)):
        on_face(g.box('Pier Block Joint',(0,0,0),(width+.025,.53,.028),'stone',0),
                side,plane,u,bottom+i*1.8,.23)


def arcaded_cornice(g, x, y, z, radius, count=16):
    """A carved crown: repeating corbels below a projecting stone/bronze rim."""
    for h,r,d,mat in [(0,radius+.14,.16,'gold'),(.2,radius+.35,.22,'trim'),
                       (.48,radius+.46,.18,'trim'),(.63,radius+.47,.07,'gold')]:
        g.cylinder('Royal Projecting Cornice',(x,y,z+h),r,d,mat,32)
    for i in range(count):
        a=(i+.5)*math.tau/count
        obj=g.box('Royal Carved Corbel',(x+(radius+.12)*math.cos(a),y+(radius+.12)*math.sin(a),z-.38),
                  (.34,.40,.88),'trim',.03)
        obj.rotation_euler.z=a


def crown_lantern(g,x,y,z,radius,height=2.2):
    for dz,r,h in [(0,radius,.22),(height,radius+.12,.23)]:
        g.cylinder('Open Crown Ring',(x,y,z+dz),r,h,'gold',24)
    for i in range(8):
        a=i*math.tau/8
        g.cylinder('Open Crown Column',(x+radius*.8*math.cos(a),y+radius*.8*math.sin(a),z+height/2),
                   .12,height,'trim',8)
    crystal(g,(x,y,z+height+.12),radius*2.7,radius*.38)


def great_facade(g, side, plane, u=0):
    framed_window(g,side,plane,u,15.4,10.0,26.2)
    star(g,side,plane,u,28.5,3.1,.63)
    # A tall projecting gable caps the lancet instead of leaving a plain flat wall.
    p=lambda x,z,out:face_point(side,plane,u+x,z,out)
    for out,radius,mat in [(.34,.34,'trim'),(.68,.09,'gold')]:
        g.path('Royal Great Gable',[p(-6,34.9,out),p(-4.6,40,out),p(0,44.4,out),
                                  p(4.6,40,out),p(6,34.9,out)],radius,mat)
    for x in [-6.2,6.2]:
        pilaster(g,side,plane,u+x,14.5,23.3,1.0)
        crystal(g,face_point(side,plane,u+x,38.4,.50),1.2,.22)
    for x in [-3.5,3.5]:
        lantern(g,side,plane,u+x,12.7,1.5)


def gate_spandrels(g, front=-31.48, width=10, height=10.4):
    """Solid stone follows the portal arch while preserving the open passage."""
    edge=arch_outline(width,height)
    left=[(x,z) for x,z in edge if x<=0 and z>=height*.64]
    for a,b in zip(left,left[1:]):
        if abs(a[0]-b[0])<.0001:continue
        for sign in [-1,1]:
            outline=[(sign*a[0],a[1]),(sign*b[0],b[1]),(sign*b[0],height+1),(sign*a[0],height+1)]
            obj=g.prism('Cut Stone Gate Vault',outline,2.35,'stone')
            obj.location.y=front+1.1
    for x in [-5.8,5.8]:pilaster(g,0,-front,x,.25,height+.8,1.1)
    star(g,0,-front,0,height+1.05,.65,.40)


def stair_sweep(g, sign, balustrade):
    """Two sweeping garden approaches rise continuously to the first terrace."""
    points=[]
    count=46
    for i in range(count+1):
        t=i/count
        points.append((sign*(12.5+12.0*math.sin(t*math.pi/2)), -19.0+24.5*t, .12+14.0*t))
    for i in range(count):
        a,b=points[i],points[i+1]
        dx,dy=b[0]-a[0],b[1]-a[1]
        length=math.hypot(dx,dy)
        obj=g.box('Sweeping Royal Garden Stair',((a[0]+b[0])/2,(a[1]+b[1])/2,b[2]/2),
                  (3.8,length+.065,b[2]),'trim',0)
        obj.rotation_euler.z=-math.atan2(dx,dy)
    for edge in [-2.08,2.08]:
        rail=[]
        for i,(x,y,z) in enumerate(points):
            a,b=points[max(0,i-1)],points[min(count,i+1)]
            dx,dy=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dy)
            rail.append((x+edge*dy/length,y-edge*dx/length,z+.15))
        for i in range(0,count,3):balustrade(g,rail[i],rail[min(i+3,count)],1.1)


def bridge_inlay(g):
    for x in [-3.95,3.65,3.95]:
        g.box('Bridge Bronze Inlay',(x,-40.5,.17),(.055,25,.025),'gold',0)
    for y in [-52.8,-28.2]:g.box('Bridge Bronze Inlay',(0,y,.17),(7.9,.055,.025),'gold',0)
    for y in [-52,-29]:
        g.path('Bridge Diamond Emblem',[(-1.2,y,.20),(0,y+1,.20),(1.2,y,.20),(0,y-1,.20)],.035,'gold',True)
