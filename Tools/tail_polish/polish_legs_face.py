"""Second polish pass on MyDino: lift the solid-dark far leg and thin out face speckles.
Usage (project root): python Tools/tail_polish/polish_legs_face.py [in.aseprite] [out.aseprite]"""
import sys,os,json
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from ase import Ase, Image
PAL={k:tuple(int(v[i:i+2],16) for i in (1,3,5))+(255,) for k,v in dict(A='#1d302a',B='#2a4a34',C='#532b2e',D='#436b3e',E='#82493b',F='#66994a',G='#be6f43',H='#8b9753',I='#97bf64').items()}
INV={v:k for k,v in PAL.items()}
def ch(im,x,y):
    if not(0<=x<64 and 0<=y<64): return None
    p=im.getpixel((x,y)); return INV.get(p,'?') if p[3] else None
def far_leg(im):
    """B runs >=3 wide get a mid-tone core so the far leg reads as a rounded limb, not a slab."""
    out=im.copy(); n=0
    for y in range(64):
        for x in range(64):
            if ch(im,x,y)=='B' and ch(im,x-1,y)=='B' and ch(im,x+1,y) in ('B','D'):
                out.putpixel((x,y),PAL['D']); n+=1
    return out,n
def face(im):
    """Remove rust speckles on the snout/cheek (right of the mane), keep the mane and mouth."""
    bb=im.getbbox()
    if not bb: return im,0
    out=im.copy(); n=0
    for y in range(64):
        for x in range(bb[0]+5,64):
            if ch(im,x,y) not in ('G','E'): continue
            nb=[ch(im,x+a,y+b) for a,b in((1,0),(-1,0),(0,1),(0,-1))]
            if None in nb or any(c in ('A','B','C','?') for c in nb): continue
            greens=[c for c in nb if c in ('F','D','I','H')]
            if len(greens)<2: continue
            pick=max(set(greens),key=greens.count)
            out.putpixel((x,y),PAL[pick]); n+=1
    return out,n
if __name__=='__main__':
    root=os.path.abspath(os.path.join(HERE,'..','..'))
    src=sys.argv[1] if len(sys.argv)>1 else os.path.join(root,'Tools/SourceArt/ClaudeTailPolish/MyDino-Running.aseprite')
    dst=sys.argv[2] if len(sys.argv)>2 else src
    a=Ase(src); legs=spots=0
    for f in range(a.nframes):
        im,n=far_leg(a.layer_image(f,1));
        if n: a.set_cel(f,1,im); legs+=n
        im,n=face(a.layer_image(f,4))
        if n: a.set_cel(f,4,im); spots+=n
    a.save(dst); print('far-leg pixels lifted',legs,'face speckles removed',spots,'->',dst)
