"""Original Luma architecture: blue glass, luminous crystals and open star forms."""
import math
from geometry import Mesh
from landmarks import sphere


def crown():
    m=Mesh('Luma Crown Spire')
    # Sin's crystal cathedral reference: one dominant needle, silver ribs,
    # pointed glass portals and subordinate pinnacles, rather than a shard bouquet.
    # The 164m base stays within the existing avenue's 246m inner radius.
    profile=[(18,78),(360,72),(730,56),(950,35),(1250,.12)]
    m.cylinder(0,0,0,164,10,'Crystal Frame',segments=64)
    m.cylinder(0,0,10,160,8,'Crystal Ice',segments=64)
    first=len(m.colors)
    m.loft([(0,0,z,r,r,0) for z,r in profile],'Crystal Clear',8)
    for face in range(first+2,len(m.colors)):
        m.colors[face]=('Crystal Clear','Crystal Ice','Crystal Clear','Crystal Blue')[(face-first-2)%4]

    def radius_at(z):
        for (za,ra),(zb,rb) in zip(profile,profile[1:]):
            if za<=z<=zb:
                return ra+(rb-ra)*(z-za)/(zb-za)
        return profile[0][1]

    def radial(angle,r,z,tangent=0):
        return (r*math.cos(angle)-tangent*math.sin(angle),
                r*math.sin(angle)+tangent*math.cos(angle),z)

    for side in range(8):
        angle=side*math.tau/8
        for (za,ra),(zb,rb) in zip(profile,profile[1:]):
            m.beam(radial(angle,ra,za),radial(angle,rb,zb),1.7,'Crystal Frame')
        # Pointed tracery follows the actual face plane, never crosses the tower.
        facing=angle+math.pi/8
        for bottom,shoulder,top in ((30,260,340),(365,620,710),(735,860,935)):
            width=radius_at(shoulder)*math.sin(math.pi/8)*.72
            points=[radial(facing,radius_at(z)*math.cos(math.pi/8)+.7,z,t)
                    for z,t in ((bottom,-width),(shoulder,-width),(top,0),
                                (shoulder,width),(bottom,width))]
            for a,b in zip(points,points[1:]):
                m.beam(a,b,1.15,'Crystal Frame')

        # Swept faceted buttresses tie the broad base to the narrow central shaft.
        outline=[(68,18),(151,18),(145,130),(102,425),(68,640)]
        left=[radial(angle,r,z,-17) for r,z in outline]
        right=[radial(angle,r,z,17) for r,z in outline]
        m.polygon(list(reversed(left)),'Crystal Blue')
        m.polygon(right,'Crystal Clear')
        for j in range(len(outline)):
            k=(j+1)%len(outline)
            m.polygon([left[j],left[k],right[k],right[j]],'Crystal Ice')
            m.beam(left[j],left[k],1.8,'Crystal Frame')
            m.beam(right[j],right[k],1.8,'Crystal Frame')
        height=560 if side%2 else 700
        rings=[(118,115,23),(108,height*.62,17),(99,height*.80,13),(96,height,.1)]
        m.loft([(*radial(angle,r,0)[:2],z,w,w,angle) for r,z,w in rings],'Crystal Ice',6)
        for (ra,za,wa),(rb,zb,wb) in zip(rings,rings[1:]):
            for ridge in range(6):
                turn=angle+ridge*math.tau/6
                a=radial(angle,ra,za); b=radial(angle,rb,zb)
                m.beam((a[0]+wa*math.cos(turn),a[1]+wa*math.sin(turn),za),
                       (b[0]+wb*math.cos(turn),b[1]+wb*math.sin(turn),zb),1,'Crystal Frame')
        # Small perimeter needles echo the reference without widening the base.
        m.cylinder(*radial(angle,153,18),2.5,100,'Crystal Frame',top=.1,segments=6)
    m.solids.append([[-78,-78,0],[78,78,950]])
    return m


def tide():
    m=Mesh('Neris Tide Arcology')
    for side in (-1,1):
        rings=[]
        for i in range(49):
            t=i/48
            x=side*(43+45*math.sin(t*math.pi))
            rings.append((x,0,430*t,30*(1-.55*t),27*(1-.35*t),side*t*.6))
        m.loft(rings,'Glass',24)
        for i in range(0,48,3):
            a=rings[i]
            m.loft([a,(a[0],a[1],a[2]+.8,a[3]+.3,a[4]+.3,a[5])],'Silver',24)
    for z in (140,320):
        m.box((0,0,z),(165,20,8),'Glass')
        m.box((0,0,z+4.5),(163,18,1),'Green')
    return m


def hall():
    m=Mesh('Starweave Civic Hall')
    m.cylinder(0,0,0,87,12,'Stone',segments=64)
    for j in range(8):
        angle=j*math.tau/8
        m.box((48*math.cos(angle),48*math.sin(angle),24),(75,27,26),'Glass',angle)
        m.beam((85*math.cos(angle),85*math.sin(angle),36),(0,0,104),1.7,'Gold')
    m.loft([(0,0,35,63,63,0),(0,0,60,57,57,0),(0,0,84,35,35,0),
            (0,0,101,2,2,0)],'Glass',48)
    for j in range(12):
        a=j*math.tau/12
        m.beam((61*math.cos(a),61*math.sin(a),36),(0,0,102),.8,'Silver')
    m.cylinder(0,0,102,2,32,'Gold',top=.2,segments=8)
    return m


def observatory():
    m=Mesh('Aether Gate Observatory')
    for j in range(4):
        a=math.pi/4+j*math.tau/4
        for i in range(20):
            t=i/20; t2=(i+1)/20
            r=75-45*math.sin(t*math.pi/2); r2=75-45*math.sin(t2*math.pi/2)
            m.beam((r*math.cos(a),r*math.sin(a),t*240),
                   (r2*math.cos(a),r2*math.sin(a),t2*240),5,'Silver',8)
    m.ring(76,25,220,'Glass',segments=128)
    m.ring(76,26,230,'Glass',segments=128)
    for r in (63,89):
        m.loft([(0,0,220,r,r,0),(0,0,230,r,r,0)],'Glass',96)
        m.ring(r,1,231,'Gold',segments=96)
    m.cylinder(0,0,0,13,251,'Glass',segments=24)
    sphere(m,(0,0,262),25,'Glass')
    m.cylinder(0,0,287,2,18,'Gold',top=.15,segments=8)
    return m


ORIGINALS=[('Luma Crown Spire',1250,crown),('Neris Tide Arcology',430,tide),
           ('Starweave Civic Hall',134,hall),('Aether Gate Observatory',305,observatory)]
