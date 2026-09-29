"""Author three versioned town snapshots; never install or overwrite live saves."""
import sys,json,copy,hashlib,bisect
from pathlib import Path
ROOT=Path(__file__).resolve().parents[7]
sys.path.insert(0,str(ROOT/'tools/Character3DViewer'))
from town_document_codec import decode,unwrap,encode,envelope
PACKAGE=Path(__file__).resolve().parents[1]
CAT=json.loads((PACKAGE.parent/'NerisTownV1/Authoring/catalog.json').read_text())
source=Path(sys.argv[1])
raw=source.read_bytes();original=decode(unwrap(raw),CAT)
assert original['name']=='Neris Town' and len(original['items'])==362
out=PACKAGE/'Town/r009';out.mkdir(parents=True,exist_ok=True)
(out/'Neris-Town-Before-Split.town').write_bytes(raw)
def sample(d,x,z):
 c=bisect.bisect_right(d['xs'],x)-1;r=bisect.bisect_right(d['zs'],z)-1
 return d['cells'][r*d['columns']+c] if 0<=c<d['columns'] and 0<=r<d['rows'] else 1
def surface(d,xs,zs,fn):
 d.update(xs=xs,zs=zs,columns=len(xs)-1,rows=len(zs)-1)
 d['cells']=[fn((a+b)/2,(c+e)/2) for c,e in zip(zs,zs[1:]) for a,b in zip(xs,xs[1:])]
 return d
town=copy.deepcopy(original)
xs=sorted(set([-5200.,2750.]+[x for x in original['xs'] if -5200<x<2750]))
zs=sorted(set([-3450.,4200.,800.,1000.,-1100.,-900.]+[z for z in original['zs'] if -3450<z<4200]))
def central(x,z):
 v=sample(original,x,z)
 # Remove the old airport spur and northern/southern excess road stubs.
 if x < -4900 and z>1000: v=1
 if x < -4900 and z<-3300: v=1
 if -5200<=x<=-2500 and 800<=z<=1000: v=3
 if 2400<=x<=2750 and -1100<=z<=-900: v=3
 return v
surface(town,xs,zs,central)
def blank(name):
 d=copy.deepcopy(original);d.update(name=name,items=[],cell_size=100.,dirty=0);return d
port=blank('Neris Spaceport')
def portcell(x,z):
 if z>=-3700: v=1
 else:v=2
 if (-5600<=x<=-1300 and -3600<=z<=-3400) or (-5600<=x<=-5400 and -3700<=z<=-3400):v=3
 return v
surface(port,list(range(-9300,-1299,100)),list(range(-10100,-3299,100)),portcell)
airport=blank('Horizon Airport')
def airportcell(x,z):
 return 3 if x<5400 and 1150<=z<=1350 else 1
surface(airport,list(range(5200,11601,100)),sorted(set(list(range(-3750,6251,100))+[1150,1350])),airportcell)
report={'source_sha256':hashlib.sha256(raw).hexdigest(),'preserved_items':len(town['items']),'towns':{}}
for d,filename in [(town,'Neris-Town-r009'),(port,'Neris-Spaceport-r002'),(airport,'Horizon-Airport-r009')]:
 assert d['columns']<=512 and d['rows']<=512
 for i in d['items']:assert d['xs'][0]<=i['position'][0]<=d['xs'][-1] and d['zs'][0]<=i['position'][2]<=d['zs'][-1]
 data=envelope(encode(d,CAT));(out/(filename+'.town')).write_bytes(data)
 report['towns'][d['name']]={'file':filename+'.town','bounds':[d['xs'][0],d['xs'][-1],d['zs'][0],d['zs'][-1]],'grid':[d['columns'],d['rows']],'items':len(d['items']),'sha256':hashlib.sha256(data).hexdigest()}
assert town['items']==original['items']
# The optional world editor shows the same west/center/east relationship.
put=lambda n:int(n).to_bytes(3,'little')
name=lambda s:put(len(s))+b''.join(put(ord(c)) for c in s)
world=put(1)+name('Neris Region')+put(3)
for label,x in [('Neris Spaceport',150),('Neris Town',500),('Horizon Airport',850)]:world+=name(label)+put(x)+put(500)
world+=b''.join(put(abs(a-b)==1) for a in range(3) for b in range(3))
(out/'Neris-Region.world').write_bytes(envelope(world))
(out/'migration-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
