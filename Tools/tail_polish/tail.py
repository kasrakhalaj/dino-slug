import sys,math,json; sys.path.insert(0, __file__.rsplit('/',1)[0]); from ase import *
P={k:tuple(int(v[i:i+2],16) for i in (1,3,5))+(255,) for k,v in dict(A='#1d302a',B='#2a4a34',C='#532b2e',D='#436b3e',E='#82493b',F='#66994a',G='#be6f43',H='#8b9753',I='#97bf64',J='#c6cb80').items()}
def mask(im): return {(x,y) for y in range(64) for x in range(64) if im.getpixel((x,y))[3]}
def cent(S): return (sum(p[0] for p in S)/len(S), sum(p[1] for p in S)/len(S))
def bez(p0,p1,p2,t):
    u=1-t; return (u*u*p0[0]+2*u*t*p1[0]+t*t*p2[0], u*u*p0[1]+2*u*t*p1[1]+t*t*p2[1])
def draw_tail(R,T,up_hint,body,hw0=3.4,arch=1.2,tipw=0.55,spines=True):
    """R root centre (inside body), T tip; returns RGBA 64x64 tail image."""
    dx,dy=T[0]-R[0],T[1]-R[1]; L=math.hypot(dx,dy); tx,ty=dx/L,dy/L
    nx,ny=-ty,tx
    if nx*up_hint[0]+ny*up_hint[1]<0: nx,ny=-nx,-ny     # n points to the back/top side
    C=((R[0]+T[0])/2+nx*arch,(R[1]+T[1])/2+ny*arch)
    N=400; pts=[bez(R,C,T,i/N) for i in range(N+1)]
    def hw(t): return tipw+(hw0-tipw)*(1-t)**0.85
    def sample(x,y):
        best=None
        for i,(px,py) in enumerate(pts):
            d=(x-px)**2+(y-py)**2
            if best is None or d<best[0]: best=(d,i)
        d,i=best; t=i/N; px,py=pts[i]
        j=min(i+1,N); k=max(i-1,0); gx,gy=pts[j][0]-pts[k][0],pts[j][1]-pts[k][1]; gl=math.hypot(gx,gy) or 1
        mx,my=-gy/gl,gx/gl
        if mx*nx+my*ny<0: mx,my=-mx,-my
        s=((x-px)*mx+(y-py)*my)          # + = top side
        return t,math.sqrt(d),s
    inside={}
    for y in range(64):
        for x in range(64):
            t,d,s=sample(x+.5,y+.5)
            if d<=hw(t)+0.05 and (t<1 or d<=tipw+0.3): inside[(x,y)]=(t,s/max(hw(t),.01))
    # remove single-pixel stubs
    for _ in range(2):
        for p in list(inside):
            n=sum((p[0]+a,p[1]+b) in inside for a,b in((1,0),(-1,0),(0,1),(0,-1)))
            if n<=1 and inside[p][0]>0.15: del inside[p]
    out=Image.new('RGBA',(64,64),(0,0,0,0))
    def nb(p): return [(p[0]+a,p[1]+b) for a,b in((1,0),(-1,0),(0,1),(0,-1))]
    outline=set()
    for p in inside:
        if any(q not in inside and q not in body for q in nb(p)): outline.add(p)
    for p,(t,s) in inside.items():
        if p in outline: c='A'
        elif s>0.35: c='F' if not (s>0.6 and t<0.55 and (p[0]+p[1])%2==0) else 'I'
        elif s>-0.25: c='D'
        else: c='H' if t<0.6 else 'D'
        out.putpixel(p,P[c])
    # rust spots that travel with the tail (same family as body speckles)
    for (ts,ss,c) in ((0.22,0.15,'G'),(0.42,-0.05,'E'),(0.6,0.25,'G')):
        best=min((p for p in inside if p not in outline),key=lambda p:(inside[p][0]-ts)**2*40+(inside[p][1]-ss)**2,default=None)
        if best: out.putpixel(best,P[c])
    if spines:
        # dorsal spines continue the back ridge: E on the contour, G tip outside
        tops=sorted([p for p in outline if inside[p][1]>0.3 and 0.05<inside[p][0]<0.72],key=lambda p:inside[p][0])
        last=-9
        for p in tops:
            t=inside[p][0]
            if t*L-last>=3.0:
                last=t*L
                out.putpixel(p,P['G'])
                q=(round(p[0]-nx),round(p[1]-ny))
                if q in inside and q not in outline: out.putpixel(q,P['E'])
    return out,(nx,ny)
