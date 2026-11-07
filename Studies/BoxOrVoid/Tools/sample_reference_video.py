"""Extract timestamped reference frames; never alters the source recording."""
from pathlib import Path
import subprocess
import sys
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
VIDEO = Path(r"F:\GDocument\Eagle\实况资源库.library\images\MNU375V1N2HI1.info\Box or Void 箱即是空_Demo.mkv")
FFMPEG = Path(r"C:\Users\19118\.codex\tmp\sokoban-video-tools\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v9.0.2.exe")
frames = ROOT / "Research/Frames"
frames.mkdir(parents=True, exist_ok=True)
times = [int(x) for x in sys.argv[1:]] or [60,90,120,180,210,270,330,390,450,510,570,630]
sheet = Image.new("RGB", (1600, ((len(times)+2)//3)*322), "#252525")
draw = ImageDraw.Draw(sheet)
for index, stamp in enumerate(times):
    output = frames / f"frame_{stamp:04}.png"
    if not output.exists():
        subprocess.run([str(FFMPEG),"-hide_banner","-loglevel","error","-ss",str(stamp),"-i",str(VIDEO),"-frames:v","1","-y",str(output)],check=True)
    image = Image.open(output).convert("RGB")
    image.thumbnail((528,297))
    x,y = (index%3)*534,(index//3)*322
    sheet.paste(image,(x,y+22))
    draw.text((x+4,y+4),f"{stamp//60:02}:{stamp%60:02}",fill="white")
output = frames / ("contact_"+str(times[0])+"_"+str(times[-1])+".jpg")
sheet.save(output,quality=90)
print(output)
