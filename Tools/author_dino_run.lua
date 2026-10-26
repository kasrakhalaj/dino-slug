-- True run construction at the existing 65 ms cadence; no gameplay speed edits.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/RunAndSoldiers/'
local sprite=app.open('E:/Aseprite/MyDino-Polished.aseprite')
sprite:saveAs(out..'MyDino-Before-Run.aseprite')
local rgba,a=app.pixelColor.rgba,app.pixelColor.rgbaA
local function col(h) return rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local ink,deep,shade,green,light,claw=col('1D302A'),col('2A4A34'),col('436B3E'),col('66994A'),col('97BF64'),col('C6CB80')
local function blank() return Image(64,64,ColorMode.RGB) end
local function put(im,x,y,c) x,y=math.floor(x+.5),math.floor(y+.5);if x>=0 and y>=0 and x<64 and y<64 then im:putPixel(x,y,c) end end
local function part(i,f) local im=blank();local c=sprite.layers[i]:cel(f);if c then im:drawImage(c.image,c.position) end;return im end
local function shift(im,dx,dy) local out=blank();out:drawImage(im,Point(dx,dy));return out end
local function replace(i,f,im) local c=sprite.layers[i]:cel(f);if c then c.image=im;c.position=Point(0,0) else sprite:newCel(sprite.layers[i],f,im,Point(0,0)) end end
local x={38,35,31,27,25,26,29,32,35,38,40,40}
local y={47,47,47,46,43,41,41,41,42,43,44,46}
local bob={0,1,0,-1,-2,-1,0,1,0,-1,-2,-1}
local function leg(phase,far,bodyBob)
 local mask=blank();local hx=far and 34 or 31;local hy=40+bodyBob
 local fx=x[phase]+(far and 1 or 0);local fy=y[phase]
 local kx,ky
 if phase<=3 then kx=math.floor((hx+fx)/2);ky=44+math.max(bodyBob,0)
 elseif phase<=6 then kx=hx-2;ky=43+bodyBob
 elseif phase<=9 then kx=hx+3;ky=39+bodyBob
 else kx=hx+5;ky=41+bodyBob end
 local function disc(cx,cy,r)
  for yy=math.floor(cy-r),math.ceil(cy+r) do for xx=math.floor(cx-r),math.ceil(cx+r) do
   if (xx-cx)^2+(yy-cy)^2<=r*r then put(mask,xx,yy,green) end
  end end
 end
 local function limb(x1,y1,x2,y2,r1,r2)
  for n=0,24 do local t=n/24;disc(x1+(x2-x1)*t,y1+(y2-y1)*t,r1+(r2-r1)*t) end
 end
 limb(hx,hy,kx,ky,2.1,1.8);limb(kx,ky,fx,fy-1,1.7,1.3)
 for xx=fx-1,fx+3 do put(mask,xx,fy,green);put(mask,xx,fy-1,green) end
 local im=blank()
 for yy=0,63 do for xx=0,63 do if a(mask:getPixel(xx,yy))>0 then
  local edge=false
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do if a(mask:getPixel(math.max(0,math.min(63,xx+d[1])),math.max(0,math.min(63,yy+d[2]))))==0 then edge=true end end
  local c=far and shade or green
  if edge and yy>hy+1 then c=ink elseif xx<hx-1 or yy>=fy-1 then c=far and deep or shade end
  put(im,xx,yy,c)
 end end end
 for xx=hx-1,hx+1 do put(im,xx,hy,far and shade or green) end
 if not far then put(im,hx+1,hy+1,light);put(im,kx,ky-1,light) end
 put(im,fx+3,fy,far and shade or claw)
 return im
end
local offsets={}
for _,tag in ipairs(sprite.tags) do
 if tag.name=='Run' or tag.name=='ArmedRun' or tag.name=='Laugh' then
  for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
   local phase=(f-tag.fromFrame.frameNumber)%12+1
   local body=part(3,f);local bottom=0
   for yy=0,63 do for xx=27,39 do if a(body:getPixel(xx,yy))>0 then bottom=math.max(bottom,yy) end end end
   local oldBob=bottom-41
   body=shift(body,0,-oldBob)
   local leaned=blank()
   for yy=0,63 do for xx=0,63 do local p=body:getPixel(xx,yy)
    if a(p)>0 then put(leaned,xx+math.floor(math.max(0,40-yy)*.3+.5),yy+bob[phase],p) end
   end end
   replace(3,f,leaned)
   replace(1,f,shift(part(1,f),-1,bob[phase]-oldBob))
   replace(5,f,shift(part(5,f),4,bob[phase]-oldBob))
   replace(6,f,shift(part(6,f),4,bob[phase]-oldBob))
   replace(2,f,leg((phase+5)%12+1,true,bob[phase]))
   replace(4,f,leg(phase,false,bob[phase]))
   offsets[f]=bob[phase]/16
  end
 end
end
-- Only the gun's presentation follows the authored rise/fall; root physics stays fixed.
local file=io.open(out..'grip-offsets.json','w');local entries={}
for f=1,#sprite.frames do entries[#entries+1]=string.format('"%d":%.4f',f,offsets[f] or 0) end
file:write('{'..table.concat(entries,',')..'}');file:close()
sprite:saveAs(out..'MyDino-Running.aseprite');sprite:saveAs('E:/Aseprite/MyDino-Running.aseprite')
local sheet=Image(64*6,64*2,ColorMode.RGB)
for f=1,12 do local im=blank();im:drawSprite(sprite,f);sheet:drawImage(im,Point((f-1)%6*64,math.floor((f-1)/6)*64)) end
local zoom=Image(sheet.width*4,sheet.height*4,ColorMode.RGB)
for yy=0,zoom.height-1 do for xx=0,zoom.width-1 do zoom:putPixel(xx,yy,sheet:getPixel(math.floor(xx/4),math.floor(yy/4))) end end
zoom:saveAs(out..'Run-poses.png')
print('Authored forward lean, contact/compression/drive/flight and folded recovery at unchanged frame durations.')
