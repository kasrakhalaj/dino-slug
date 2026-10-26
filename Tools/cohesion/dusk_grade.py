"""Put the Metal Slug sprites under Dino Slug's dusk light.
- characters/props/projectiles: desaturate a little, warm sunset key light, purple shadows, dusk-ink outlines
- fire effects (blasts, rifle warning flash): remapped onto a 6-colour sunset fire ramp
- pickups: lighter touch so they still pop, same outline ink
The player's held guns (hmg_held, shotgun_held, hmg_gun) are left as drawn.
Originals are copied to Tools/cohesion/original/ the first time this runs.
Usage (project root): python Tools/cohesion/dusk_grade.py [--preview out.png]"""
import os,sys,glob,shutil
from PIL import Image
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
ART=os.path.join(ROOT,'Assets/Game/Art/MetalSlug')
ORIG=os.path.join(ROOT,'Tools/cohesion/original')
SKIP=('hmg_held','shotgun_held','hmg_gun')
INK=(0x2b,0x25,0x40)                      # outline ink, from the scenery's darkest purples
KEY=(1.0,0.90,0.85)                       # low sunset key light
SHADE=(0.30,0.23,0.40)                    # purple ambient in the shadows
FIRE=[(0x3b,0x1f,0x2b),(0x8a,0x2f,0x2f),(0xc9,0x47,0x2f),(0xe8,0x82,0x3f),(0xf6,0xc2,0x66),(0xfd,0xf1,0xcd)]  # dusk-ink to cream, shares the coin gold and dino cream

def kind(name):
    if any(k in name for k in SKIP): return None
    if 'blast' in name or 'rifle_warning' in name: return 'fire'
    if 'pickup' in name or 'medicine' in name: return 'pickup'
    return 'world'
def luma(c): return (0.299*c[0]+0.587*c[1]+0.114*c[2])/255
def grade(p,strength):
    r,g,b,a=p
    if a==0: return p
    l=luma(p); mx,mn=max(r,g,b),min(r,g,b)
    if l<0.13 and mx-mn<60: return INK+(a,)
    sat=0.78 if strength>.9 else 0.9
    c=[(l*255+(v-l*255)*sat) for v in (r,g,b)]
    c=[v*k for v,k in zip(c,KEY)]
    t=((1-l)**2)*0.32*strength
    c=[v*(1-t)+s*255*t for v,s in zip(c,SHADE)]
    return tuple(int(max(0,min(255,round(v)))) for v in c)+(a,)
def fire_map(im):
    """Snap every fire colour to the nearest colour of a 6-step sunset fire ramp (hue is kept: reds stay red)."""
    def f(p):
        if p[3]==0: return p
        best=min(FIRE,key=lambda c:2*(c[0]-p[0])**2+4*(c[1]-p[1])**2+3*(c[2]-p[2])**2)
        return best+(p[3],)
    return f
def process(path,write=True):
    name=os.path.basename(path)[:-4]; k=kind(name)
    if k is None: return None
    src=os.path.join(ORIG,os.path.relpath(path,ART))
    if not os.path.exists(src):
        os.makedirs(os.path.dirname(src),exist_ok=True); shutil.copy2(path,src)
    im=Image.open(src).convert('RGBA'); px=im.load()
    cache={}
    fire=fire_map(im) if k=='fire' else None
    for y in range(im.height):
        for x in range(im.width):
            p=px[x,y]
            if p not in cache: cache[p]=fire(p) if k=='fire' else grade(p,1.0 if k=='world' else 0.5)
            px[x,y]=cache[p]
    if write: im.save(path)
    return im
if __name__=='__main__':
    files=sorted(glob.glob(os.path.join(ART,'*.png'))+glob.glob(os.path.join(ART,'*','*.png')))
    n=0
    for f in files:
        if process(f) is not None: n+=1
    print('graded',n,'sprites; originals in',ORIG)
