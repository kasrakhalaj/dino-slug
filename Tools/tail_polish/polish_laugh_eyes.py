"""Laugh refinement pass 2: squeezed-shut '>' eyes on the big HA frames and a consistent tongue.
Usage (project root): python Tools/tail_polish/polish_laugh_eyes.py [in.aseprite] [out.aseprite]"""
import sys,os
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from ase import Ase, Image
from polish_laugh import PAL
HA=[29,31,33,35,37]                 # big "HA!" frames (head thrown up)
TONGUE_SRC=31                       # HA frame whose tongue is copied to the others
EYE=[(0,0),(1,0),(1,1),(2,1),(3,2),(1,3),(2,3),(0,4),(1,4)]   # '>' pointing at the snout
EYE_X=35
def head_top(a,f):
    H=a.layer_image(f,4).copy(); H.alpha_composite(a.layer_image(f,5)); return H.getbbox()
def run(a):
    n=0
    src=a.layer_image(TONGUE_SRC,4); sb=head_top(a,TONGUE_SRC)
    tongue=[(x,y,src.getpixel((x,y))) for y in range(64) for x in range(64)
            if src.getpixel((x,y))[3] and src.getpixel((x,y)) in (PAL['E'],PAL['G']) and x>=sb[0]+8 and y>=sb[1]+8]
    for f in HA:
        H=a.layer_image(f,4); E=a.layer_image(f,5); bb=head_top(a,f)
        ay=bb[1]+3
        # replace the scribbled near eye with a clean squeezed-shut eye
        for y in range(ay,ay+5):
            for x in range(32,40):
                if E.getpixel((x,y))[3]: E.putpixel((x,y),(0,0,0,0)); n+=1
                if x>=34 and y<ay+4 and H.getpixel((x,y))==PAL['A']: H.putpixel((x,y),PAL['F']); n+=1
        for dx,dy in EYE: H.putpixel((EYE_X+dx,ay+dy),PAL['A']); n+=1
        # same tongue in every HA frame (only drawn over mouth interior)
        dy=bb[1]-sb[1]
        for x,y,c in tongue:
            q=(x,y+dy)
            if H.getpixel(q) in (PAL['C'],PAL['E'],PAL['G']) and not E.getpixel(q)[3]:
                if H.getpixel(q)!=c: H.putpixel(q,c); n+=1
        a.set_cel(f,4,H); a.set_cel(f,5,E)
    return n
if __name__=='__main__':
    root=os.path.abspath(os.path.join(HERE,'..','..'))
    src=sys.argv[1] if len(sys.argv)>1 else os.path.join(root,'Tools/SourceArt/ClaudeTailPolish/MyDino-Running.aseprite')
    dst=sys.argv[2] if len(sys.argv)>2 else src
    a=Ase(src); n=run(a); a.save(dst); print('laugh eye/tongue pixels',n,'->',dst)
