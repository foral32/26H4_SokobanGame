"""Encode actual Unity camera recordings; invoke after RunShowcaseCapture.
Raw sequences stay in Library. Editor-only clips and posters are the deliverable.
"""
import argparse,json,subprocess,shutil
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--ffmpeg',required=True);parser.add_argument('--clips',nargs='+');args=parser.parse_args()
root=Path(__file__).resolve().parents[3]
source=root/'Library/DraculasBoxShowcase/Frames';target=root/'Assets/DraculasBox/Showcase/Editor/Clips';target.mkdir(parents=True,exist_ok=True)
manifest_path=target/'CaptureManifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8'))['clips'] if args.clips and manifest_path.exists() else []
for folder in sorted(source.iterdir()):
 if args.clips and folder.name not in args.clips:continue
 if not (folder/'frames.txt').exists():continue
 count=int((folder/'frames.txt').read_text());
 if count<20:continue
 output=target/(folder.name+'.webm')
 # Unity's built-in VP8 decoder also plays when Windows Media Foundation stalls.
 subprocess.run([args.ffmpeg,'-y','-hide_banner','-loglevel','error','-framerate','20','-i',str(folder/'%05d.jpg'),'-frames:v',str(count),'-c:v','libvpx','-deadline','good','-cpu-used','2','-crf','10','-b:v','1600k','-pix_fmt','yuv420p','-an',str(output)],check=True)
 shutil.copyfile(folder/'00000.jpg',target/(folder.name+'.jpg'))
 manifest=[clip for clip in manifest if clip['id']!=folder.name]
 manifest.append({'id':folder.name,'frames':count,'fps':20,'seconds':count/20,'bytes':output.stat().st_size})
manifest_path.write_text(json.dumps({'source':'Unity Play Mode camera and native UI; isolated save and campaign fixture','clips':sorted(manifest,key=lambda clip:clip['id'])},indent=2),encoding='utf-8')
print(json.dumps(manifest))
