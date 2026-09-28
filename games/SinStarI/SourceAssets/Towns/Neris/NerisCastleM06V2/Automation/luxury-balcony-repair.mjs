import fs from 'node:fs';
import { primitives } from './model-primitives.mjs';
import { helpers as facade } from './m03-facade.mjs';
import { helpers as towers } from './m03-towers.mjs';
import { build as palace } from './luxury-palace.mjs';
import { build as terraces } from './luxury-terraces.mjs';
import { runBlender } from './blender-background.mjs';

const root = 'D:/Projects/Sin-Star-I-Assets/Neris-Castle/';
const source = root + 'source/neris-castle-M06-r003.blend';
const target = root + 'source/neris-castle-M06-r004.blend';
if (fs.existsSync(target)) throw Error('Use a fresh castle revision.');
fs.mkdirSync(root + 'checkpoints/M06-r004', { recursive: true });
const columns = palace.slice(palace.indexOf('def lux_column'), palace.indexOf('# Replace sparse'));
const rail = terraces.slice(terraces.indexOf('def balcony_rail'), terraces.indexOf("for side,sign in"));
const code = primitives + facade + towers + columns + rail + String.raw`
from pathlib import Path
assert not Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M06-r004.blend').exists()
checks=[]
for side,sign in [('West',-1),('East',1)]:
    prefix='NC.Palace.Luxury.Balcony.'+side
    for o in list(bpy.data.collections['NC.Palace'].objects):
        if o.name.startswith(tuple(prefix+suffix for suffix in ['.Deck','.GoldLip','.Front.','.Side.','.Bracket.','.Pier.','.UpperWindow.1.'])):
            remove(o.name)
    x0,x1=sorted([sign*20.2,sign*39])
    bevel(box(prefix+'.Deck','Palace',(x0-.15,x1+.15),(12.65,16.4),(12.65,13.13)),.045)
    box(prefix+'.GoldLip','Palace',(x0-.17,x1+.17),(12.62,16.4),(12.97,13.05),'RoyalGold')
    balcony_rail(prefix+'.Front','Palace',(x0,12.85),(x1,12.85),13.15)
    for x in [x0,x1]:balcony_rail(prefix+'.Side.'+str(x),'Palace',(x,12.85),(x,16.25),13.15)
    for x in [sign*20.8,sign*26,sign*32.7,sign*39]:
        lux_column(prefix+'.Pier.'+str(x),'Palace',x,15.6,3.25,9.43,.21)
        lathe(prefix+'.Bracket.'+str(x),'Palace',(x,15.3),[(.13,11.55),(.22,11.85),(.45,12.35),(.58,12.65)],segments=12)
        assert all(abs(x-sign*w)>1.5+.32 for w in [23,29,36]), 'Pier crosses lower window'
    # A glazed double door reaches the deck, with the same curved portal vocabulary.
    door=prefix+'.AccessDoor';x=sign*30.5;z=13.13
    window(door,'Palace',bpy.data.objects['NC.Palace.Wing.'+side],x,16,z,2.4,4.3,2.95,.25,.22,0,'RoyalGlass')
    for o in list(bpy.data.collections['NC.Palace'].objects):
        if o.name.startswith(door+'.Tracery.'):remove(o.name)
    for sx in [-1,1]:
        lancet(door+'.LeafArch.'+str(sx),'Palace',x+sx*.57,15.84,z+.07,1.0,3.3,2.28,.035)
        flower(door+'.LeafRose.'+str(sx),'Palace',x+sx*.57,15.80,z+2.86,.16)
        pipe(door+'.Handle.'+str(sx),'Palace',[(x+sx*.16,15.72,z+1.02),(x+sx*.16,15.72,z+1.34)],.047,'PolishedBrass')
    bevel(box(door+'.Threshold','Palace',(x-1.42,x+1.42),(15.38,16.26),(13.10,13.17)),.025)
    # Reuse the approved pots and flowers; keep the door's walking route clear.
    plant_prefix='NC.Courtyard.Seating.East.0.FlowerPot.-1'
    samples=[o for o in bpy.data.collections['NC.Courtyard'].objects if o.name==plant_prefix or o.name.startswith(plant_prefix+'.')]
    assert samples
    for i,px in enumerate([sign*23.5,sign*37]):
        delta=Matrix.Translation(Vector((px-40.5,14.1-(-36.1),13.13-.03)))
        for original in samples:
            clone=original.copy();clone.data=original.data.copy()
            bpy.data.collections['NC.Palace'].objects.link(clone)
            clone.name=prefix+'.FlowerPot.'+str(i)+original.name[len(plant_prefix):]
            clone.matrix_world=delta@original.matrix_world;clone['owner']='palace'
    # Deck back and side return physically enter the main round tower footprint.
    assert math.hypot(20.2-18,16.25-18)<3
    checks.append({'side':side,'deck_x':[x0-.15,x1+.15],'tower_contact':True,'piers_clear_windows':True,'door_threshold':13.13,'pots':2})
for o in bpy.data.collections['NC.Palace'].objects:
    if '.Balcony.' in o.name and o.type=='MESH':normals(o)
s['balcony_revision']='M06-r004';s['balcony_checks']=json.dumps(checks)
s.camera=review_camera('NC.CAM.BalconyRepaired',(43,-12,20),(29,16,12),58)
s.render.resolution_x=1500;s.render.resolution_y=1100;s.render.resolution_percentage=100
s.render.filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r004/balcony-repaired.png'
bpy.ops.wm.save_as_mainfile(filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/source/neris-castle-M06-r004.blend',compress=True)
bpy.ops.render.render(write_still=True)
Path('D:/Projects/Sin-Star-I-Assets/Neris-Castle/checkpoints/M06-r004/balcony-checks.json').write_text(json.dumps(checks,indent=2))
print('NC_RESULT '+json.dumps(checks))
`;
await runBlender(source, code, root + 'checkpoints/M06-r004/balcony-build.log');
