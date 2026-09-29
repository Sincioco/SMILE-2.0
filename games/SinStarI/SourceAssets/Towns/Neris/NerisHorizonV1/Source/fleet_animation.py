"""Editable Blender preview of the same 120/180-second native ship cycles."""
import bpy

TRANSPORT = [(0,0,0),(60,0,0),(70,0,900),(110,0,900),(120,0,0)]
ROYAL = [(0,-194,0),(30,-194,0),(45,0,0),(55,0,0),(65,0,900),
         (115,0,900),(125,0,0),(135,0,0),(150,-194,0),(180,-194,0)]


def add(scene):
    scene.render.fps=30
    scene.frame_start=0
    scene.frame_end=5400
    for family,keys in [('Transport',TRANSPORT),('Royal',ROYAL)]:
        root=bpy.data.objects.new(family+' Flight Root',None)
        scene.collection.objects.link(root)
        for obj in list(scene.objects):
            if obj.get('fleet')==family:
                original=obj.matrix_world.copy()
                obj.parent=root
                obj.matrix_world=original
        for seconds,forward,height in keys:
            root.location=(0,forward,height)
            root.keyframe_insert(data_path='location',frame=seconds*30)
        action=root.animation_data.action
        # Blender 5 layered actions store fcurves in the bound channel bag.
        for layer in action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(root.animation_data.action_slot)
                for curve in bag.fcurves:
                    points=curve.keyframe_points
                    for index,key in enumerate(points):
                        key.interpolation='BEZIER'
                        key.handle_left_type='FREE'
                        key.handle_right_type='FREE'
                        previous=points[max(0,index-1)].co.x
                        following=points[min(len(points)-1,index+1)].co.x
                        key.handle_left=(key.co.x-(key.co.x-previous)/3,key.co.y)
                        key.handle_right=(key.co.x+(following-key.co.x)/3,key.co.y)
                    curve.modifiers.new('CYCLES')
    scene.frame_set(1500)
