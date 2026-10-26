import sys,math,json; sys.path.insert(0,'/'.join(__file__.split('/')[:-1]) or '.')
from tail import P, mask, cent
from ase import Ase, Image
import runtail2 as rt
CHARS={v:k for k,v in P.items()}
ROOT=(28.0,40.0)
TILT=float(__import__('os').environ.get('TILT','0.8'))   # template hip pivot (frame-0 coordinates)

def clip_ranges(js):
    r={}
    for i,fr in enumerate(js['frames']): r.setdefault(fr['state'],[]).append(i)
    return r

def render(tpl,body,dx=0,dy=0,theta=0.0,pivot=None):
    """Place template; theta (deg, clockwise on screen) rotates about ROOT, then ROOT moves to pivot."""
    im=Image.new('RGBA',(64,64),(0,0,0,0))
    if theta==0 and pivot is None:
        for (x,y),c in tpl.items():
            X,Y=x+dx,y+dy
            if 0<=X<64 and 0<=Y<64: im.putpixel((X,Y),P[c])
        return im
    th=math.radians(theta); ct,st=math.cos(th),math.sin(th)
    px,py=pivot
    m={}
    for Y in range(64):
        for X in range(64):
            u,v=X+.5-px,Y+.5-py
            x= ct*u+st*v+ROOT[0]; y=-st*u+ct*v+ROOT[1]   # inverse rotation
            c=tpl.get((math.floor(x),math.floor(y)))
            if c: m[(X,Y)]=c
    for (X,Y),c in m.items():
        edge=any((X+a,Y+b) not in m and (X+a,Y+b) not in body for a,b in((1,0),(-1,0),(0,1),(0,-1)))
        if edge: c=c if c in 'AGE' else 'A'
        elif c=='A': c='D'
        im.putpixel((X,Y),P[c])
    return im

def plan(a,js):
    R=clip_ranges(js); out={}
    by=lambda f: a.layer_image(f,2).getbbox()[1]
    for st in ('Run','ArmedRun','Laugh'):
        fr=R[st]; n=len(fr); ys=[by(f) for f in fr]
        for i,f in enumerate(fr):
            # tail lags the body bob by ~1.5 frames (secondary motion)
            lag=(ys[(i-1)%n]+ys[(i-2)%n])/2.0
            d=int(round(0.7*(lag-ys[i])))
            out[f]=dict(dx=0,dy=ys[i]-30,d=max(-1,min(1,d)))
    # (dx,dy,bend,length) per authored action pose
    pose={'Jump':[(-4,0,2,.72),(-5,-1,1,.68),(-5,0,0,.72)],'ArmedJump':[(-4,0,2,.72),(-5,-1,1,.68),(-5,0,0,.72)],
          'Hit':[(-6,-1,0,.7),(-1,-2,1,.85),(-2,-2,2,.85)]}
    Br=mask(a.layer_image(0,2)); Hr=mask(a.layer_image(0,4)); cr=cent(Br); hr=cent(Hr)
    angr=math.atan2(hr[1]-cr[1],hr[0]-cr[0])
    for st,v in pose.items():
        for f,(dx,dy,d,s) in zip(R[st],v):
            B=mask(a.layer_image(f,2)); H=mask(a.layer_image(f,4)); c=cent(B); h=cent(H)
            th=math.degrees(math.atan2(h[1]-c[1],h[0]-c[0])-angr)*TILT
            out[f]=dict(dx=dx,dy=dy,d=d,s=s,k=.85,theta=th,pivot=(ROOT[0]+dx,ROOT[1]+dy))
    D=R['Death']; f0=D[0]
    out[f0]=dict(dx=-2,dy=-2,d=2,s=.9)
    B0=mask(a.layer_image(f0,2)); H0=mask(a.layer_image(f0,4)); c0=cent(B0); h0=cent(H0)
    ang0=math.atan2(h0[1]-c0[1],h0[0]-c0[0])
    p18=(ROOT[0]-2,ROOT[1]-2)
    for f in D[1:]:
        B=mask(a.layer_image(f,2)); H=mask(a.layer_image(f,4)); c=cent(B); h=cent(H)
        th=math.atan2(h[1]-c[1],h[0]-c[0])-ang0
        u,v=p18[0]-c0[0],p18[1]-c0[1]
        piv=(c[0]+math.cos(th)*u-math.sin(th)*v, c[1]+math.sin(th)*u+math.cos(th)*v)
        out[f]=dict(theta=math.degrees(th)*0.9,pivot=piv,d=1,s=.7,k=.85)
    return out

def new_tail(a,f,p):
    body=mask(a.layer_image(f,2))
    if 'theta' in p: return render_param(body,p['pivot'],p['theta'],p.get('d',0),p.get('s',1.0),p.get('k',1.0))
    tpl=rt.build(p.get('d',0),p.get('s',1.0))
    return render(tpl,body,p['dx'],p['dy'])

def render_param(body,pivot,theta,d=0,s=1.0,k=1.0):
    """Rasterise the authored tail shape rotated by theta about ROOT and placed at pivot."""
    top,bot,xmin=rt.contour(d,s,k)
    th=math.radians(theta); ct,st=math.cos(th),math.sin(th)
    def inv(X,Y):
        u,v=X+.5-pivot[0],Y+.5-pivot[1]
        return ct*u+st*v+ROOT[0], -st*u+ct*v+ROOT[1]
    def fwd(x,y):
        u,v=x-ROOT[0],y-ROOT[1]
        return int(math.floor(ct*u-st*v+pivot[0])), int(math.floor(st*u+ct*v+pivot[1]))
    info={}
    for Y in range(64):
        for X in range(64):
            x,y=inv(X,Y)
            if x<xmin-0.3 or x>33: continue
            t,b=top(x),bot(x)
            if b-t<0.6:
                t-=0.35;b+=0.35
            if t-0.5<=y<=b+0.5 and not (x>=28 and y<34): info[(X,Y)]=(x,y,y-(t-0.5),(b+0.5)-y)
    im=Image.new('RGBA',(64,64),(0,0,0,0))
    m=set(info)
    for p,(x,y,kt,kb) in info.items():
        X,Y=p
        edge=any((X+a,Y+b) not in m and (X+a,Y+b) not in body for a,b in((1,0),(-1,0),(0,1),(0,-1)))
        if edge: c='A'
        elif kt<3: c='F'
        elif kb<2.2 and x<=26.5: c='H'
        else: c='D'
        im.putpixel(p,P[c])
    # spikes continue the back ridge; speckles match the body pattern
    for u in rt.SPIKE_U:
        if u*s>=13*s-3: continue
        x=28-u*s+0.5
        X,Y=fwd(x,top(x)-0.5)       # just outside the top contour
        if (X,Y) not in m and (X,Y) not in body and 0<=X<64 and 0<=Y<64: im.putpixel((X,Y),P['G'])
        X2,Y2=fwd(x,top(x)+0.5)
        if (X2,Y2) in m: im.putpixel((X2,Y2),P['E'])
    for (x,k,c) in ((24.5,3.5,'G'),(21.5,2.5,'E'),(25.5,1.5,'I'),(22.5,1.5,'I')):
        xx=28-(28-x)*s; X,Y=fwd(xx,top(xx)+k-0.5)
        if (X,Y) in m and im.getpixel((X,Y))[:3] in (P['F'][:3],P['D'][:3],P['H'][:3]): im.putpixel((X,Y),P[c])
    return im
