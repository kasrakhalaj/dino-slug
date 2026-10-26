"""Mock up a slice of the running lane at true in-game scale (32 screen px per unit), before vs after."""
import os,sys
sys.path.insert(0,os.path.dirname(__file__))
from PIL import Image, ImageDraw
import dusk_grade as dg
A='Assets/Game/Art/'
def load(p): return Image.open(p).convert('RGBA')
def scaled(im,ppu,scale):
    f=32/ppu*scale
    return im.resize((max(1,round(im.width*f)),max(1,round(im.height*f))),Image.NEAREST)
def strip(after):
    W,H=900,190
    img=Image.new('RGBA',(W,H),(0,0,0,255))
    sky=load(A+'sky.png').resize((W,H),Image.NEAREST); img.alpha_composite(sky)
    m=load(A+'mountains_mid.png'); img.alpha_composite(m.crop((0,0,min(W,m.width),m.height)),(0,40))
    d=load(A+'dunes.png'); img.alpha_composite(d,(0,62)); img.alpha_composite(d,(512,62))
    g=load(A+'ground.png'); img.alpha_composite(g,(0,150)); img.alpha_composite(g,(512,150))
    ground=152
    def put(im,x): img.alpha_composite(im,(x,ground-im.height+2))
    def ms(rel,frame=True):
        p=os.path.join(dg.ART,rel)
        return dg.process(p,write=False) if after else load(os.path.join(dg.ORIG,rel) if os.path.exists(os.path.join(dg.ORIG,rel)) else p)
    s=0.75 if after else 1.0
    put(scaled(load(A+'MyDino/mydino_run_00.png'),16,1),40)
    put(scaled(load(A+'cactus.png'),32,1.0 if after else .85),180)
    put(scaled(ms('Rebel/rebel_walk_00.png'),24,s),260)
    put(scaled(ms('Combat/rifle_fire_01.png'),24,s),360)
    put(scaled(ms('Reinforcements/shield_run_00.png'),24,s),470)
    put(scaled(ms('Reinforcements/barrel.png'),24,s),580)
    put(scaled(ms('Combat/grenadier_walk_00.png'),24,s),650)
    blast=scaled(ms('Shotgun/shotgun_blast_02.png'),24,1.0),
    img.alpha_composite(blast[0],(740,ground-blast[0].height-10))
    return img
if __name__=='__main__':
    out=sys.argv[1]; S=2
    a=strip(False); b=strip(True)
    sheet=Image.new('RGB',(a.width*S,(a.height*2)*S+40),(250,248,242))
    sheet.paste(a.resize((a.width*S,a.height*S),Image.NEAREST),(0,20))
    sheet.paste(b.resize((b.width*S,b.height*S),Image.NEAREST),(0,a.height*S+40))
    dr=ImageDraw.Draw(sheet); dr.text((6,4),'NOW',fill=(60,60,60)); dr.text((6,a.height*S+24),'PROPOSED',fill=(60,60,60))
    sheet.save(out)
