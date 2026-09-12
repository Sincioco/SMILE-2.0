import os, sys, time, json
from pathlib import Path
os.environ['HF_HUB_DISABLE_IMPLICIT_TOKEN'] = '1'
os.environ['HF_HUB_OFFLINE'] = '1'
os.environ['TRANSFORMERS_OFFLINE'] = '1'
sys.path.insert(0, r'D:\AI\Mira3D\hunyuan-paint-libs')
sys.path.insert(0, r'D:\AI\Mira3D\Hunyuan3D-2.1\hy3dpaint')
import torch
from PIL import Image
from diffusers import DiffusionPipeline, UniPCMultistepScheduler
from hunyuanpaintpbr.unet.modules import Dino_v2

head_mode = '--head' in sys.argv
root = Path(r'D:\AI\Mira3D\Mira3') / ('HeadPaint' if head_mode else '')
model = Path(r'D:\AI\Mira3D\HunyuanPaintModels\paint\hunyuan3d-paintpbr-v2-1')
started = time.time()
print('Loading official Hunyuan3D-2.1 Paint model locally', flush=True)
pipeline = DiffusionPipeline.from_pretrained(
    str(model), custom_pipeline=r'D:\AI\Mira3D\Hunyuan3D-2.1\hy3dpaint\hunyuanpaintpbr',
    torch_dtype=torch.float16, local_files_only=True)
pipeline.scheduler = UniPCMultistepScheduler.from_config(pipeline.scheduler.config, timestep_spacing='trailing')
pipeline.eval()
pipeline.view_size = 512
pipeline.to('cuda')
pipeline.enable_vae_slicing()
print('Loading local DINO reference encoder', flush=True)
dino = Dino_v2(r'D:\AI\Mira3D\HunyuanPaintModels\dino').to(dtype=torch.float16, device='cuda')
reference = Image.open(r'D:\AI\Mira3D\ComfyUI\input\mira-tpose-masked.png')
if head_mode:
    width, height = reference.size
    reference = reference.crop((int(width*.38),int(height*.018),int(width*.62),int(height*.198)))
reference = reference.resize((512, 512))
normal = [[Image.open(root / f'normal-{i}.png').convert('RGB') for i in range(6)]]
position = [[Image.open(root / f'position-{i}.png').convert('RGB') for i in range(6)]]
print('Generating six albedo and six material views', flush=True)
with torch.inference_mode():
    embedding = dino(reference)
    del dino
    torch.cuda.empty_cache()
    result = pipeline(
        [reference], num_inference_steps=15, prompt='high quality',
        sync_condition=None, guidance_scale=3.0, width=512, height=512,
        num_in_batch=6, images_normal=normal, images_position=position,
        dino_hidden_states=embedding,
        generator=torch.Generator(device='cuda').manual_seed(0)).images
for i, view in enumerate(result):
    kind = 'albedo' if i < 6 else 'mr'
    view.save(root / f'{kind}-{i % 6}.png')
(root / 'paint-provenance.json').write_text(json.dumps({
    'model': 'Tencent-Hunyuan/Hunyuan3D-2.1', 'paint': 'hunyuan3d-paintpbr-v2-1',
    'dtype': 'float16', 'steps': 15, 'guidance': 3.0, 'seed': 0, 'size': 512,
    'view_count': 6, 'geometry_conditions': 'Blender Cycles normal and position renders',
    'elapsed_seconds': time.time() - started
}, indent=2), encoding='utf-8')
print(f'PAINT_COMPLETE {len(result)} views in {time.time()-started:.1f}s', flush=True)
