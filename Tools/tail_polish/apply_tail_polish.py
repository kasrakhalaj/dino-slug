"""Redraw the MyDino Tail layer so it grows out of the hip, and re-export the 57 frames.
Usage (from the project root):  python Tools/tail_polish/apply_tail_polish.py [source.aseprite]
Writes the polished native document next to the source copy and the PNG frames Unity uses."""
import sys,os,json,shutil
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from polish import *
root=os.path.abspath(os.path.join(HERE,'..','..'))
src=sys.argv[1] if len(sys.argv)>1 else os.path.join(root,'Tools/SourceArt/MyDinoAnimation/MyDino-Animated.aseprite')
js=json.load(open(os.path.join(root,'Tools/SourceArt/MyDinoAnimation/animation.json')))
a=Ase(src); assert [l['name'] for l in a.layers]==['Tail','Far leg','Body','Near leg','Head','Eyes']
pl=plan(a,js)
new={f:new_tail(a,f,pl[f]) for f in range(a.nframes)}
for f,t in new.items(): a.set_cel(f,0,t)
# stray cream pixel floating beside the mouth on the first laugh frame
eyes=a.layer_image(26,5)
if eyes.getpixel((51,35))[3]:
    eyes.putpixel((51,35),(0,0,0,0)); a.set_cel(26,5,eyes)
out=os.path.join(root,'Tools/SourceArt/ClaudeTailPolish/MyDino-Running.aseprite')
a.save(out)
b=Ase(out)   # re-read to prove the written document is valid
for f,fr in enumerate(js['frames']):
    img=b.composite(f)
    img.save(os.path.join(root,'Assets/Game/Art/MyDino',fr['file']))
shutil.copyfile(out,os.path.join(root,'Tools/SourceArt/MyDinoAnimation/MyDino-Animated.aseprite'))
print('Polished',a.nframes,'frames ->',out)
