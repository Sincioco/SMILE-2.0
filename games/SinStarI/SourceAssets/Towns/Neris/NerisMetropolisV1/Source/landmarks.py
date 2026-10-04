"""Authored landmark silhouettes in metres; stylized architectural recreations."""
import math
from geometry import Mesh, square_tower


def burj():
    m = Mesh('Burj Khalifa')
    for i in range(18):
        z, h = i*38, 40
        r = max(7,48-i*2.4)
        m.cylinder(0,0,z,r,h,'Glass',segments=24)
        for wing in range(3):
            a = wing*math.tau/3
            extension = max(0,65-i*5.2+(wing-i%3)*7)
            if extension > 3:
                x,y=math.cos(a)*extension,math.sin(a)*extension
                m.cylinder(x,y,z,max(5,r*.6),h,'Glass',segments=16)
                m.cylinder(x,y,z+h-1,max(5,r*.6)+.5,1.2,'Silver',segments=16)
        m.cylinder(0,0,z+h-1,r+.7,1.2,'Silver',segments=24)
    m.cylinder(0,0,684,7,144,'Silver',top=.4,segments=12)
    return m


def shanghai():
    m = Mesh('Shanghai Tower')
    rings=[]
    for i in range(85):
        t=i/84
        rings.append((0,0,t*610,44-17*t,36-13*t,t*2.1))
    m.loft(rings,'Glass',32)
    for i,r in enumerate(rings[:-1]):
        if i%2==0:
            x,y,z,rx,ry,a=r
            m.loft([(x,y,z,rx+.2,ry+.2,a),(x,y,z+.7,rx+.2,ry+.2,a)],'Silver',32)
    for j in range(8):
        for a,b in zip(rings,rings[1:]):
            def point(r):
                x,y,z,rx,ry,t=r
                k=j*math.tau/8
                return (rx*math.cos(k)*math.cos(t)-ry*math.sin(k)*math.sin(t),
                        rx*math.cos(k)*math.sin(t)+ry*math.sin(k)*math.cos(t),z)
            m.beam(point(a),point(b),.6,'Silver',4)
    m.loft([(0,0,610,27,23,2.1),(0,0,632,25,21,2.18)],'Silver',32)
    return m


def petronas():
    m=Mesh('Petronas Twin Towers')
    for x in (-44,44):
        for tier,(z,h,r) in enumerate(((0,200,25),(200,84,22),(284,54,19),(338,36,15),(374,26,11))):
            m.cylinder(x,0,z,r,h,'Glass',segments=16)
            for level in range(int(z)+3,int(z+h),5):
                m.cylinder(x,0,level,r+1,.8,'Silver',segments=16)
        m.cylinder(x,0,400,9,32,'Silver',top=3,segments=16)
        m.cylinder(x,0,432,3,20,'Silver',top=.15,segments=12)
    m.box((0,0,172),(89,12,9),'Glass')
    m.box((0,0,179),(90,13,1.5),'Silver')
    for side in (-1,1):
        m.beam((side*43,0,112),(0,0,169),1.8,'Silver')
    return m


def marina():
    m=Mesh('Marina Bay Sands')
    for x in (-82,0,82):
        square_tower(m,x,0,0,47,37,190)
        for k in range(10):
            z=k*19
            m.box((x,-28+12*(k/9)**2,z+9),(47,13,20),'Glass')
    m.loft([(0,0,191,174,28,0),(0,0,198,179,31,0),(0,0,202,177,30,0)],'Silver',64)
    m.box((0,2,203),(270,24,1),'Green')
    m.box((0,-16,203.5),(146,9,.6),'Water')
    from studio_trees import append
    for i,x in enumerate(range(-120,141,20)):
        append(m,x,5,204,1.8,i%2,i*.7)
    return m


def jeddah():
    m=Mesh('Jeddah Tower')
    # Three tapering wings and observation terrace, total architectural height 1000m.
    for j in range(3):
        a=j*math.tau/3
        for i in range(20):
            z=i*44; r=41*(1-i/24); d=70*(1-i/23)
            x,y=math.cos(a)*d*.45,math.sin(a)*d*.45
            m.box((x,y,z+22),(r,d,46),'Glass',a+math.pi/2)
            m.box((x,y,z+44),(r+.6,d+.6,1),'Silver',a+math.pi/2)
    m.cylinder(0,0,880,7,120,'Silver',top=.2,segments=12)
    m.cylinder(-54,0,610,19,3,'Silver',segments=32)
    return m


def financial_center():
    m=Mesh('Shanghai World Financial Center')
    square_tower(m,0,0,0,63,41,420)
    for sign in (-1,1):
        m.box((sign*22,0,454),(16,35,68),'Glass')
    m.box((0,0,488),(59,35,8),'Silver')
    for side in (-1,1):
        m.beam((-31,side*21,0),(28,side*18,418),1,'Silver')
        m.beam((31,side*21,0),(-28,side*18,418),1,'Silver')
    return m


def merdeka():
    m=Mesh('Merdeka 118')
    # Diamond plan, shifting sharp corners and asymmetric crystalline crown.
    # Every diagonal seam is an actual facet edge, never a bar through the skin.
    rings=[[(0,-43,0),(41,0,0),(0,36,0),(-39,0,0)],
           [(-8,-42,125),(35,4,140),(5,34,120),(-36,-4,150)],
           [(6,-38,255),(33,-5,275),(-7,32,260),(-31,4,240)],
           [(-6,-35,390),(30,5,365),(3,29,380),(-29,-3,405)],
           [(3,-31,477),(27,2,520),(-2,26,499),(-24,0,468)]]
    m.polygon(list(reversed(rings[0])),'Glass')
    for level,(lo,hi) in enumerate(zip(rings,rings[1:])):
        for i in range(4):
            j=(i+1)%4
            if (i+level)%2:
                triangles=[(lo[i],lo[j],hi[j]),(lo[i],hi[j],hi[i])]
                seam=(lo[i],hi[j])
            else:
                triangles=[(lo[i],lo[j],hi[i]),(lo[j],hi[j],hi[i])]
                seam=(lo[j],hi[i])
            for points in triangles:
                m.polygon(points,'Glass')
            m.beam(*seam,.23,'Silver',4)
            m.beam(lo[i],hi[i],.42,'Silver',4)
    crown=(17,1,523)
    for i in range(4):
        a,b=rings[-1][i],rings[-1][(i+1)%4]
        m.polygon([a,b,crown],'Glass')
        m.beam(a,crown,.35,'Silver',4)
    m.cylinder(17,1,519,2.7,110,'Silver',top=1.2,segments=8)
    m.cylinder(17,1,629,1.2,50,'Silver',top=.12,segments=8)
    return m


def makkah():
    m=Mesh('Makkah Royal Clock Tower')
    square_tower(m,0,0,0,115,100,90,'Glass')
    square_tower(m,0,0,90,77,66,310,'Glass')
    m.box((0,0,430),(80,71,64),'Glass')
    for side in (-1,1):
        y=side*36
        # Clock face is a vertical, gold-rimmed disc with visible hour hands.
        for i in range(48):
            a,b=i*math.tau/48,(i+1)*math.tau/48
            m.polygon([(23*math.cos(a),y,430+23*math.sin(a)),
                       (23*math.cos(b),y,430+23*math.sin(b)),
                       (20*math.cos(b),y,430+20*math.sin(b)),
                       (20*math.cos(a),y,430+20*math.sin(a))],'Gold')
        m.beam((0,y*1.01,430),(0,y*1.01,446),1.2,'Gold')
        m.beam((0,y*1.01,430),(12,y*1.01,435),1.2,'Gold')
    m.cylinder(0,0,462,40,42,'Gold',top=18,segments=8)
    m.cylinder(0,0,504,11,70,'Silver',top=4,segments=12)
    m.cylinder(0,0,574,4,27,'Gold',top=.25,segments=12)
    return m


def taper_tower(name, height, profile, segments=4, twist=math.pi/4):
    m=Mesh(name)
    rings=[(0,0,z,rx,ry,twist) for z,rx,ry in profile]
    m.loft(rings,'Glass',segments)
    for lo,hi in zip(profile,profile[1:]):
        za,ra,da=lo; zb,rb,db=hi
        for z in range(int(za)+5,int(zb),7):
            t=(z-za)/(zb-za); r=ra+(rb-ra)*t; d=da+(db-da)*t
            m.loft([(0,0,z,r+.25,d+.25,twist),(0,0,z+.5,r+.25,d+.25,twist)],'Silver',segments)
    return m


def ping_an():
    m=taper_tower('Ping An Finance Center',599,[(0,58,58),(480,47,47),(555,28,28),(599,3,3)],8)
    for j in range(8):
        a=j*math.tau/8+math.pi/4
        m.beam((58*math.cos(a),58*math.sin(a),0),(47*math.cos(a),47*math.sin(a),480),1.5,'Silver')
    return m


def lotte():
    m=taper_tower('Lotte World Tower',555,[(0,46,36),(330,37,31),(510,24,22),(555,17,18)],24,0)
    for x in (-8,8):
        m.beam((x,-20,495),(x,-18,555),1.5,'Silver')
    return m


def one_wtc():
    m=Mesh('One World Trade Center')
    square_tower(m,0,0,0,61,61,57,'WTC Glass',False)
    lower=[(30,30,57),(-30,30,57),(-30,-30,57),(30,-30,57)]
    upper=[(0,30,417),(-30,0,417),(0,-30,417),(30,0,417)]
    # Eight planar triangular faces join the square base to a rotated square crown.
    # This produces the distinctive octagonal middle without twisting the facade.
    for i in range(4):
        j=(i+1)%4
        faces=[(lower[i],lower[j],upper[i]),(lower[j],upper[j],upper[i])]
        for a,b,c in faces:
            m.polygon([a,b,c],'WTC Glass')
            for start,end in ((a,b),(b,c),(c,a)):
                m.beam(start,end,.32,'Silver')
            for z in range(63,417,6):
                intersections=[]
                for p,q in ((a,b),(b,c),(c,a)):
                    if min(p[2],q[2])<z<max(p[2],q[2]):
                        t=(z-p[2])/(q[2]-p[2])
                        intersections.append(tuple(p[k]+(q[k]-p[k])*t for k in range(3)))
                if len(intersections)==2:
                    m.beam(*intersections,.065,'Silver',4)
    m.polygon(upper,'Silver')
    m.cylinder(0,0,417,8,7,'Graphite',segments=16)
    m.cylinder(0,0,417,3.3,124,'Silver',top=.3,segments=12)
    m.solids.append(((-30,-30,0),(30,30,417)))
    return m


def guangzhou():
    m=Mesh('Guangzhou CTF Finance Centre')
    for z,w,d,h in ((0,76,66,70),(70,65,60,210),(280,55,54,160),(440,43,45,90)):
        square_tower(m,0,0,z,w,d,h)
        for x in range(-int(w/2),int(w/2)+1,6):
            m.box((x,-d/2-.4,z+h/2),(1.2,1,h),'Stone')
    return m


def tianjin():
    return taper_tower('Tianjin CTF Finance Centre',530,
        [(0,47,40),(120,51,43),(310,39,33),(440,30,27),(510,19,19),(530,5,5)],24,0)


def citic():
    return taper_tower('CITIC Tower',528,
        [(0,55,55),(95,45,45),(260,37,37),(430,44,44),(510,55,55),(528,51,51)],8)


def pagoda(name,height,levels):
    m=Mesh(name)
    square_tower(m,0,0,0,68,68,height*.18,'Stone')
    bottom=height*.18; group=height*.68/levels
    for i in range(levels):
        r=39-i*.65
        m.loft([(0,0,bottom+i*group,r*.75,r*.75,math.pi/4),
                (0,0,bottom+(i+1)*group,r,r,math.pi/4)],'Glass',4)
        m.loft([(0,0,bottom+(i+1)*group-1,r+1,r+1,math.pi/4),
                (0,0,bottom+(i+1)*group+1,r+1,r+1,math.pi/4)],'Silver',4)
        for z in range(1,int(group),5):
            t=z/group; rr=r*(.75+.25*t)
            m.loft([(0,0,bottom+i*group+z,rr,rr,math.pi/4),
                    (0,0,bottom+i*group+z+.5,rr,rr,math.pi/4)],'Silver',4)
    m.cylinder(0,0,height*.86,14,height*.14,'Silver',top=.2,segments=8)
    return m


def sphere(m,center,radius,key):
    x,y,z=center
    m.loft([(x,y,z+radius*math.cos(i*math.pi/16),
             max(.01,radius*math.sin(i*math.pi/16)),
             max(.01,radius*math.sin(i*math.pi/16)),0) for i in range(16,-1,-1)],key,24)


def pearl():
    m=Mesh('Oriental Pearl Tower')
    for j in range(3):
        a=j*math.tau/3
        m.beam((48*math.cos(a),48*math.sin(a),0),(20*math.cos(a),20*math.sin(a),110),6,'Stone',10)
        m.cylinder(13*math.cos(a),13*math.sin(a),80,5,280,'Silver',segments=12)
    sphere(m,(0,0,110),32,'Magenta')
    sphere(m,(0,0,275),24,'Glass')
    m.cylinder(0,0,293,7,90,'Silver',segments=12)
    sphere(m,(0,0,350),10,'Gold')
    m.cylinder(0,0,375,4,93,'Silver',top=.2,segments=10)
    for z,r in ((110,32),(275,24)):
        m.ring(r+1,2,z,'Gold',segments=64)
    return m


DESIGNS = [('Burj Khalifa',828,burj),('Shanghai Tower',632,shanghai),
           ('Petronas Twin Towers',452,petronas),('Marina Bay Sands',213,marina),
           ('Jeddah Tower',1000,jeddah),('Shanghai World Financial Center',492,financial_center),
           ('Merdeka 118',679,merdeka),('Makkah Royal Clock Tower',601,makkah),
           ('Ping An Finance Center',599,ping_an),('Lotte World Tower',555,lotte),
           ('One World Trade Center',541,one_wtc),('Guangzhou CTF Finance Centre',530,guangzhou),
           ('Tianjin CTF Finance Centre',530,tianjin),('CITIC Tower',528,citic),
           ('Taipei 101',508,lambda: pagoda('Taipei 101',508,8)),
           ('Jin Mao Tower',421,lambda: pagoda('Jin Mao Tower',421,12)),
           ('Oriental Pearl Tower',468,pearl)]
