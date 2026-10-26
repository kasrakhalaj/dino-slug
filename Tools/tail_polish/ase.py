import struct, zlib
from PIL import Image
class Ase:
    def __init__(s,path):
        d=open(path,'rb').read(); s.raw=d
        (s.fsize,magic,s.nframes,s.w,s.h,s.depth,s.flags,s.speed)=struct.unpack_from('<IHHHHHIH',d,0)
        assert magic==0xA5E0
        s.header=bytearray(d[:128]); s.frames=[]; s.layers=[]; s.tags=[]
        off=128
        for fi in range(s.nframes):
            fbytes,fm,oldn,dur,_,newn=struct.unpack_from('<IHHH2sI',d,off)
            assert fm==0xF1FA
            n=newn if newn else oldn
            p=off+16; chunks=[]
            for _ in range(n):
                csize,ctype=struct.unpack_from('<IH',d,p)
                chunks.append([ctype,bytearray(d[p+6:p+csize])]); p+=csize
            s.frames.append({'dur':dur,'chunks':chunks})
            for ct,cd in chunks:
                if ct==0x2004:
                    fl,lt,child,_,_,bm,op=struct.unpack_from('<HHHHHHB',cd,0)
                    nl=struct.unpack_from('<H',cd,16)[0]; name=cd[18:18+nl].decode()
                    s.layers.append({'name':name,'type':lt,'flags':fl,'opacity':op})
            off+=fbytes
    def cels(s,fi):
        out={}
        for ct,cd in s.frames[fi]['chunks']:
            if ct==0x2005:
                li,x,y,op,typ,z=struct.unpack_from('<HhhBHh',cd,0)
                out[li]=(x,y,op,typ,cd)
        return out
    def cel_image(s,fi,li):
        c=s.cels(fi).get(li)
        if c is None: return None
        x,y,op,typ,cd=c
        if typ==1:
            src=struct.unpack_from('<H',cd,16)[0]; return s.cel_image(src,li)
        w,h=struct.unpack_from('<HH',cd,16)
        px=cd[20:]
        if typ==2: px=zlib.decompress(bytes(px))
        assert s.depth==32, s.depth
        im=Image.frombytes('RGBA',(w,h),bytes(px))
        full=Image.new('RGBA',(s.w,s.h),(0,0,0,0)); full.paste(im,(x,y)); 
        return full
    def layer_image(s,fi,li):
        im=s.cel_image(fi,li)
        return im if im else Image.new('RGBA',(s.w,s.h),(0,0,0,0))
    def set_cel(s,fi,li,img):
        """replace cel of layer li at frame fi with full-canvas image, compressed, at (0,0) trimmed"""
        bb=img.getbbox()
        chunks=s.frames[fi]['chunks']
        idx=None
        for i,(ct,cd) in enumerate(chunks):
            if ct==0x2005 and struct.unpack_from('<H',cd,0)[0]==li: idx=i
        if bb is None:
            if idx is not None: del chunks[idx]
            return
        crop=img.crop(bb)
        body=struct.pack('<HhhBHh5x',li,bb[0],bb[1],255,2,0)+struct.pack('<HH',crop.width,crop.height)+zlib.compress(crop.tobytes())
        if idx is not None: chunks[idx]=[0x2005,bytearray(body)]
        else:
            # insert after last cel chunk with lower layer index, else after layers/palette
            pos=len(chunks)
            for i,(ct,cd) in enumerate(chunks):
                if ct==0x2005 and struct.unpack_from('<H',cd,0)[0]>li: pos=i;break
            chunks.insert(pos,[0x2005,bytearray(body)])
    def save(s,path):
        out=bytearray()
        for f in s.frames:
            cb=b''.join(struct.pack('<IH',len(cd)+6,ct)+bytes(cd) for ct,cd in f['chunks'])
            n=len(f['chunks'])
            out+=struct.pack('<IHHH2xI',16+len(cb),0xF1FA,min(n,0xFFFF),f['dur'],n)+cb
        hdr=bytearray(s.header); struct.pack_into('<I',hdr,0,128+len(out))
        open(path,'wb').write(bytes(hdr)+bytes(out))
    def composite(s,fi,skip=()):
        im=Image.new('RGBA',(s.w,s.h),(0,0,0,0))
        for li,L in enumerate(s.layers):
            if li in skip or L['type']!=0 or not (L['flags']&1): continue
            im.alpha_composite(s.layer_image(fi,li))
        return im
