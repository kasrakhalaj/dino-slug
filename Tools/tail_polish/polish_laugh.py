"""Polish the MyDino laugh mouths (Head+Eyes layers, Laugh/ArmedRun/ArmedJump frames).
- teeth: upper row bright, lower row shaded, stray floating tooth pixels removed (attached fangs kept)
- tongue: muted-rust tongue with one highlight at the back of open mouths
- lower jaw: a cream/olive jaw under the lower teeth instead of a bare outline
Usage (project root): python Tools/tail_polish/polish_laugh.py [in.aseprite] [out.aseprite] [frames]"""
import sys,os,json
HERE=os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0,HERE)
from ase import Ase, Image
HEX=dict(A='#1d302a',B='#2a4a34',C='#532b2e',D='#436b3e',E='#82493b',F='#66994a',G='#be6f43',H='#8b9753',I='#97bf64',J='#c6cb80',K='#e9e5ab',L='#fdf1cd')
PAL={k:tuple(int(v[i:i+2],16) for i in (1,3,5))+(255,) for k,v in HEX.items()}
INV={v:k for k,v in PAL.items()}
CREAM=('K','L')

def polish_face(face,body):
    g={}
    for y in range(64):
        for x in range(64):
            p=face.getpixel((x,y))
            if p[3]: g[(x,y)]=INV.get(p,'?')
    C=[p for p,c in g.items() if c=='C']
    if len(C)<4: return {}
    x0=min(p[0] for p in C)-2; x1=max(p[0] for p in C)+2; y0=min(p[1] for p in C)-3; y1=max(p[1] for p in C)+3
    inbox=lambda p: x0<=p[0]<=x1 and y0<=p[1]<=y1
    col=lambda x:[y for y in range(y0,y1+1) if g.get((x,y))=='C']
    new={}
    # 1) teeth classification per pixel
    role={}
    for (x,y),c in g.items():
        if c not in CREAM or not inbox((x,y)): continue
        cs=col(x)
        below=any(yy>y for yy in cs); above=any(yy<y for yy in cs)
        if below and not above: role[(x,y)]='up'
        elif above and not below: role[(x,y)]='low'
        elif above and below: role[(x,y)]='mid'
    for p,r in list(role.items()):
        if r!='mid': continue
        x,y=p
        if role.get((x,y-1))=='up': role[p]='fang_up'      # hangs from the upper row
        elif role.get((x,y+1))=='low': role[p]='fang_low'
    # a second pass lets 2-px fangs survive
    for p,r in list(role.items()):
        if r=='mid' and role.get((p[0],p[1]-1))=='fang_up': role[p]='fang_up2'
    for p,r in role.items():
        c={'up':'L','fang_up':'L','fang_up2':'K','low':'L','fang_low':'L','mid':'C'}[r]
        if g[p]!=c: new[p]=c
    G2=dict(g); G2.update(new)
    # 2) tongue: a small rounded rust tongue resting at the back of tall open mouths
    lows=[p for p,r in role.items() if r=='low']
    if lows:
        xs=[p[0] for p in C]; mx0,mx1=min(xs),max(xs); span=mx1-mx0+1
        w=max(3,min(5,int(round(span*0.38))))
        base=[]
        for x in range(mx0+1,mx0+1+w):
            cs=[y for y in col(x) if G2.get((x,y))=='C']
            if len(cs)>=3:
                yb=max(cs)
                if role.get((x,yb+1)) in ('low','fang_low'): base.append((x,yb))
        if len(base)>=3:
            for p in base: new[p]='E'
            for p in base[1:-1]:
                q=(p[0],p[1]-1)
                if G2.get(q)=='C': new[q]='E'
            hi=base[len(base)//2]; q=(hi[0],hi[1]-1)
            if new.get(q)=='E': new[q]='G'
    G2.update(new)
    # 3) jaw under the lower teeth (cream at the front, olive toward the throat)
    lowxs=sorted(p[0] for p in lows)
    if lowxs:
        mid=(lowxs[0]+lowxs[-1])/2
        bottom={}
        for (x,y) in lows: bottom[x]=max(bottom.get(x,-1),y)
        for x,y in bottom.items():
            jaw='J' if x>=mid else 'H'
            below=G2.get((x,y+1))
            if below=='A' and G2.get((x,y+2)) is None:
                new[(x,y+1)]=jaw; new[(x,y+2)]='A'
            elif below=='B':
                new[(x,y+1)]=jaw
        for (x,y),c in list(G2.items()):
            if c=='B' and inbox((x,y)) and y>max(p[1] for p in C): new[(x,y)]='H'
        G2.update(new)
        # close the outline around the new jaw pixels
        for p,c in list(new.items()):
            if c not in ('J','H'): continue
            for d in ((1,0),(-1,0),(0,1)):
                q=(p[0]+d[0],p[1]+d[1])
                if q not in G2 and q not in body: new[q]='A'; G2[q]='A'
    return new

# Hand touch-ups after the automatic pass (frame index -> {(x,y): colour or None to clear})
def _row(x0,y,cols): return {(x0+i,y):c for i,c in enumerate(cols)}
HAND={
 31:{**_row(34,28,'EDHH'),**_row(40,29,'HHHJJJJJ')},
 37:{**_row(34,28,'EDH')},
 33:{**_row(38,31,'HHHHJJJJJJ'),**_row(40,32,'AAAAAAAA')},
 35:{(34,28):'D',**_row(39,28,'HHHHJJJJJ'),**_row(39,29,'AAAAAAAAA')},
 32:{(40,31):'H',(40,32):'H',(40,33):'A'},
 34:{(40,30):'H',(40,31):'H',(40,32):'A'},
}
def hand(a,frames):
    n=0
    for f in frames:
        if f not in HAND: continue
        H=a.layer_image(f,4); E=a.layer_image(f,5)
        for p,c in HAND[f].items():
            H.putpixel(p,PAL[c] if c else (0,0,0,0)); E.putpixel(p,(0,0,0,0)); n+=1
        a.set_cel(f,4,H); a.set_cel(f,5,E)
    return n

def apply(a,frames):
    total=0
    for f in frames:
        H=a.layer_image(f,4); E=a.layer_image(f,5)
        face=H.copy(); face.alpha_composite(E)
        body=set()
        for li in (1,2,3):
            im=a.layer_image(f,li)
            body|={(x,y) for y in range(64) for x in range(64) if im.getpixel((x,y))[3]}
        new=polish_face(face,body)
        if not new: continue
        for p,c in new.items():
            H.putpixel(p,PAL[c]); E.putpixel(p,(0,0,0,0))
        a.set_cel(f,4,H); a.set_cel(f,5,E); total+=len(new)
    return total

if __name__=='__main__':
    root=os.path.abspath(os.path.join(HERE,'..','..'))
    src=sys.argv[1] if len(sys.argv)>1 else os.path.join(root,'Tools/SourceArt/ClaudeTailPolish/MyDino-Running.aseprite')
    dst=sys.argv[2] if len(sys.argv)>2 else src
    js=json.load(open(os.path.join(root,'Tools/SourceArt/MyDinoAnimation/animation.json')))
    frames=[int(x) for x in sys.argv[3].split(',')] if len(sys.argv)>3 else [i for i,f in enumerate(js['frames']) if f['state']=='Laugh']
    a=Ase(src); n=apply(a,frames); n+=hand(a,frames); a.save(dst); print('laugh pixels polished',n,'->',dst)
