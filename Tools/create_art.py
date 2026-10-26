"""Original pixel artwork and sound effects for Dino Slug (Pillow, stdlib)."""
from pathlib import Path
from PIL import Image, ImageDraw
import math, random, wave, struct

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets' / 'Game' / 'Art'
ART.mkdir(parents=True, exist_ok=True)
S = 1

def canvas(w=256, h=256):
    im = Image.new('RGBA', (w*S, h*S))
    return im, ImageDraw.Draw(im)

def poly(d, pts, fill): d.polygon([(int(x*S), int(y*S)) for x,y in pts], fill=fill)
def ellipse(d, box, fill): d.ellipse(tuple(int(v*S) for v in box), fill=fill)
def line(d, pts, fill, width=2): d.line([(int(x*S),int(y*S)) for x,y in pts], fill=fill, width=width*S, joint='curve')
def save(im,name):
    # Sample the authored shapes onto native pixels, with no antialiasing.
    im=im.resize((im.width//4, im.height//4), Image.Resampling.NEAREST)
    if im.mode=='RGBA':
        im.putalpha(im.getchannel('A').point(lambda a: 255 if a >= 128 else 0))
    im.save(ART/f'{name}.png')

def dino(name, phase=0, jump=False, hit=False, dead=0):
    im,d=canvas()
    teal='#79bf2b'; dark='#276c39'; light='#b9df76'; coral='#b0dd54'
    bob = 0 if jump else math.sin(phase*2)*2
    # broad expressive silhouette, tail, back plates, belly, small arms
    poly(d,[(105,141+bob),(59,152),(17,130),(39,170),(88,187),(122,181)],teal)
    for x,y in [(73,149),(89,139),(106,127),(119,114)]:
        poly(d,[(x,y+bob),(x-8,y-16+bob),(x+13,y-6+bob)],coral)
    ellipse(d,(79,110+bob,172,208+bob),teal)
    ellipse(d,(116,141+bob,163,202+bob),light)
    # The reference-inspired head is a separately animated sprite, attached in Unity.
    poly(d,[(126,147+bob),(124,100+bob),(162,100+bob),(165,156+bob)],teal)
    line(d,[(159,150+bob),(178,160+bob),(185,151+bob)],dark,9)
    line(d,[(158,146+bob),(177,155+bob),(183,148+bob)],teal,7)
    # feet alternate through a genuine six-frame run cycle
    if jump:
        leg1=[(114,190),(101,211),(125,211)]; leg2=[(148,190),(160,206),(182,206)]
    else:
        swing=math.sin(phase)*15
        leg1=[(110,191+bob),(105+swing,225-abs(swing)*.3),(126+swing,225-abs(swing)*.3)]
        leg2=[(145,191+bob),(145-swing,227-abs(swing)*.3),(165-swing,227-abs(swing)*.3)]
    line(d,leg2,dark,13); line(d,leg1,teal,15)
    if dead:
        # Rotate on a larger work area so the falling head is never clipped.
        work=Image.new('RGBA',(512,512));work.paste(im,(128,128))
        work=work.rotate(-min(85,dead*17),Image.Resampling.NEAREST,center=(256,338))
        shift=dead*8
        im=work.crop((128+shift,128,384+shift,384))
    save(im,name)

for i in range(6): dino(f'dino_run_{i}',i*math.tau/6)
dino('dino_jump',jump=True)
for i in range(2): dino(f'dino_hit_{i}',phase=i,hit=True)
for i in range(6): dino(f'dino_death_{i}',hit=True,dead=i)

im,d=canvas(160,224)
line(d,[(80,210),(80,42)],'#386a61',32)
line(d,[(80,129),(40,129),(40,83)],'#386a61',23)
line(d,[(81,103),(119,103),(119,61)],'#386a61',24)
line(d,[(72,198),(72,45)],'#78b79a',8)
for x,y in [(38,94),(117,70),(78,49)]: ellipse(d,(x-5,y-11,x+6,y+3),'#8fcaae')
ellipse(d,(70,21,89,39),'#ffa881')
save(im,'cactus')

for i in range(4):
    im,d=canvas(224,160)
    wing=24+math.sin(i*math.tau/4)*35
    poly(d,[(109,87),(57,wing),(19,wing-8),(63,104),(109,107)],'#c17874')
    poly(d,[(95,91),(137,wing-2),(167,wing-10),(143,102),(101,112)],'#edb297')
    ellipse(d,(70,77,150,119),'#d98f82')
    ellipse(d,(133,67,175,105),'#efb39a')
    poly(d,[(168,82),(203,96),(168,98)],'#f7d399')
    poly(d,[(77,96),(34,103),(66,116)],'#c17874')
    ellipse(d,(156,77,162,84),'#332b44')
    save(im,f'bird_{i}')

im,d=canvas(128,128)
ellipse(d,(7,7,121,121),'#a96b3e'); ellipse(d,(8,3,120,115),'#f2b85a')
ellipse(d,(20,15,108,103),'#ffe4a2'); ellipse(d,(28,23,100,95),'#f6c266')
poly(d,[(64,33),(72,54),(94,57),(77,71),(82,92),(64,80),(45,92),(49,70),(33,56),(56,53)],'#fff0ba')
save(im,'coin')

im,d=canvas(256,128)
ellipse(d,(0,0,256,128),'#ffffff'); save(im,'cloud')
im,d=canvas(128,128)
ellipse(d,(0,0,128,128),'#ffffff'); save(im,'disc')

# Continuous sky gradient. All landscape edges match, so layers tile cleanly.
w,h=1536,864
im=Image.new('RGB',(w,h)); px=im.load()
stops=[(0,(32,37,64)),(.48,(97,74,100)),(.8,(216,142,118)),(1,(245,188,135))]
for y in range(h):
    t=y/(h-1)
    for (a,c1),(b,c2) in zip(stops,stops[1:]):
        if a<=t<=b:
            u=(t-a)/(b-a); col=tuple(round(c1[k]*(1-u)+c2[k]*u) for k in range(3));break
    for x in range(w):px[x,y]=col
im=im.resize((384,216),Image.Resampling.NEAREST).quantize(colors=24,dither=Image.Dither.NONE).convert('RGB')
im.save(ART/'sky.png')

for j,(name,col,base,amp) in enumerate([
    ('mountains_far','#715d7d',215,90),('mountains_mid','#9a7488',260,105),('dunes','#bd8c8c',320,45)]):
    w,h=2048,512; im=Image.new('RGBA',(w,h));d=ImageDraw.Draw(im)
    pts=[]
    for x in range(w+1):
        t=x/w*math.tau
        if j<2:y=base+amp*(.64*math.sin(t*3+j)+.28*math.sin(t*7+1)+.22*math.cos(t*11))
        else:y=base+amp*(math.sin(t*2)+.4*math.cos(t*5))
        pts.append((x,y))
    d.polygon(pts+[(w,h),(0,h)],fill=col)
    im.resize((512,128),Image.Resampling.NEAREST).save(ART/f'{name}.png')

im=Image.new('RGBA',(2048,320),'#54485c');d=ImageDraw.Draw(im)
d.rectangle((0,0,2048,11),fill='#f1bc97');d.rectangle((0,12,2048,27),fill='#af7a7c')
random.seed(21)
for i in range(175):
    x=random.randrange(2048);y=random.randrange(38,310)
    d.rounded_rectangle((x,y,x+random.randrange(4,25),y+3),radius=1,fill='#6b576a')
im.resize((512,80),Image.Resampling.NEAREST).save(ART/'ground.png')

# Original bitmap lettering for the title.
glyphs={
 'D':['11110','10001','10001','10001','10001','10001','11110'],
 'E':['11111','10000','10000','11110','10000','10000','11111'],
 'S':['01111','10000','10000','01110','00001','00001','11110'],
 'R':['11110','10001','10001','11110','10100','10010','10001'],
 'T':['11111','00100','00100','00100','00100','00100','00100'],
 'A':['01110','10001','10001','11111','10001','10001','10001'],
 'H':['10001','10001','10001','11111','10001','10001','10001']}
im=Image.new('RGBA',(144,72)); d=ImageDraw.Draw(im)
for word,oy in [('DESERT',0),('DASH',40)]:
    for i,char in enumerate(word):
        for y,row in enumerate(glyphs[char]):
            for x,bit in enumerate(row):
                if bit=='1':d.rectangle((i*24+x*4,oy+y*4,i*24+x*4+3,oy+y*4+3),fill='#fff1d7')
im.save(ART/'title.png')

generated_head=ROOT/'Tools'/'SourceArt'/'goofy-dino-head.png'
if generated_head.exists():
    Image.open(generated_head).convert('RGBA').resize((32,32),Image.Resampling.NEAREST).save(ART/'dino_head.png')

audio=ROOT/'Assets'/'Game'/'Audio';audio.mkdir(parents=True,exist_ok=True)
for name,freq,end,duration in [('jump',420,780,.13),('coin',880,1400,.15),('hit',160,65,.2),('dash',230,540,.2),('death',220,48,.6),('click',650,850,.06)]:
    rate=22050;count=int(rate*duration)
    with wave.open(str(audio/f'{name}.wav'),'wb') as out:
        out.setparams((1,2,rate,count,'NONE','not compressed'))
        data=[];phase=0
        for i in range(count):
            t=i/count;f=freq+(end-freq)*t;phase+=math.tau*f/rate
            value=math.sin(phase)*(.6+.25*math.sin(phase*2))*math.sin(math.pi*t)**.6*(1-t)*.27
            data.append(struct.pack('<h',int(value*32767)))
        out.writeframes(b''.join(data))
print('Generated native pixel sprites, six-frame character animation, banded dusk sky, seamless landscape, and six sound effects.')
