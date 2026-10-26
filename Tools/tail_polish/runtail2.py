# Parametric hand-tuned tail in frame-0 coordinates (run body cel origin (28,30)).
# u = 28 - x is the distance from the hip; TOP/BOT are the authored contours.
TOP={27:36,26:36,25:37,24:37,23:37,22:38,21:38,20:39,19:39,18:39,17:40,16:40,15:41}
BOT={29:44,28:44,27:44,26:44,25:44,24:44,23:43,22:43,21:43,20:42,19:42,18:42,17:42,16:41,15:41}
SPIKE_U=(2,5,8)          # spikes at x=26,23,20 on the authored tail
def _interp(tab,u):
    xs=28-u; a=int(xs//1); b=a+1
    ya=tab.get(a,tab[min(tab)]) if a>=min(tab) else tab[min(tab)]
    yb=tab.get(b,ya)
    f=xs-a
    return ya*(1-f)+yb*f
def build(d=0,s=1.0):
    """d = tip bend in pixels (+ down), s = length scale. Returns {(x,y):char}."""
    L=13*s
    xmin=int(round(28-L))
    w=lambda x: max(0.0,min(1.0,(23-x)/6*(1/s) if s<1 else (23-x)/6))
    eps=lambda: (0.001 if d>0 else -0.001)
    top={};bot={}
    for x in range(xmin+1,30):
        u=(28-x)/s
        if x<28: top[x]=int(round(_interp(TOP,u)+d*w(x)+eps()))
        bot[x]=int(round(_interp(BOT,u)+d*w(x)+eps())) if x<28 else BOT[x]
    spikes={int(round(28-u*s)) for u in SPIKE_U if u*s<L-3}
    px={}
    for x in range(28,33):
        for y in range(34,44): px[(x,y)]='D'
    for x in range(xmin+1,30):
        t=top.get(x,35); b=bot[x]
        if b<t: b=t
        for y in range(t,b+1):
            k=y-t
            if y==b: c='A'
            elif y==t and x<28: c='E' if x in spikes else 'A'
            elif k<=2: c='F'
            elif y>=b-1 and x<=26: c='H'
            else: c='D'
            px[(x,y)]=c
        if x in spikes: px[(x,t-1)]='G'
    for x in (25,22,19):
        if x in top and px.get((x,top[x]+1))=='F': px[(x,top[x]+1)]='I'
    for (x,k,c) in ((24,3,'G'),(21,2,'E')):
        if x in top and px.get((x,top[x]+k)) in ('F','D','H'): px[(x,top[x]+k)]=c
    tipy=int(round(_interp(BOT,L/s)+d+eps()))-0  # tip sits on the lower contour line
    px[(xmin,min(tipy,bot[xmin+1]))]='A'
    px[(29,43)]='B'; px[(28,43)]='B'; px[(30,43)]='A'
    return px

def contour(d=0,s=1.0,k=1.0):
    """Continuous top/bottom contour functions of template column x (frame-0 coords).
    d bends the tip, s scales length, k scales thickness about the tail's mid-line."""
    L=13*s; xmin=28-L
    def w(x): return max(0.0,min(1.0,(23-x)/6))
    def raw(x):
        t=_interp(TOP,(28-x)/s); b=_interp(BOT,(28-x)/s); m=(t+b)/2
        return m+(t-m)*k+d*w(x), m+(b-m)*k+d*w(x)
    def top(x):
        if x>=28: return 34.0
        return raw(x)[0]
    def bot(x):
        if x>=28: return 44.0
        return raw(x)[1]
    return top,bot,xmin
