local root='E:/MyGames/HW1/DinoSlug/'
local source=root..'Tools/SourceArt/MetalSlug/'
local art=root..'Assets/Game/Art/MetalSlug/'
local function load(path) local s=app.open(source..path);app.command.ChangePixelFormat{format='rgb'};return s.cels[1].image end
local function cut(im,x,y,w,h,key)
 local r=Image(im,Rectangle(x,y,w,h))
 if key then for yy=0,h-1 do for xx=0,w-1 do if r:getPixel(xx,yy)==key then r:putPixel(xx,yy,0) end end end end
 return r
end
local function mirror(im)
 local result=Image(im.width*2,im.height,ColorMode.RGB);result:drawImage(im,Point(0,0))
 for y=0,im.height-1 do for x=0,im.width-1 do result:putPixel(im.width+x,y,im:getPixel(im.width-x-1,y)) end end
 return result
end
local function terrain(name,im)
 im=mirror(im);im:saveAs(art..'Terrain/'..name..'.png')
 local s=Sprite(im.width,im.height,ColorMode.RGB);s.layers[1].name='Original Mission 1 coast pixels';s:newCel(s.layers[1],1,im);s:saveAs(source..'Terrain/'..name..'.aseprite')
 print(name..' '..im.width..'x'..im.height)
end
local sheet=load('Terrain/mission-1-source.png')
terrain('ms1_crash_coast',cut(sheet,820,876,900,220))
local beach=Image(512,58,ColorMode.RGB)
-- Repeat complete native sand bands to give the continuous platform its required depth.
for y=0,57 do for x=0,511 do beach:putPixel(x,y,sheet:getPixel(960+x,1090+y%26)) end end
terrain('ms1_beach_ground',beach)
local function saveFrames(name,frames,grounded)
 local s=Sprite(96,80,ColorMode.RGB);s.layers[1].name='Original SNK pixels'
 for i,im in ipairs(frames) do
  local bottom=0
  for y=0,im.height-1 do for x=0,im.width-1 do if app.pixelColor.rgbaA(im:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end end end
  local canvas=Image(96,80,ColorMode.RGB);canvas:drawImage(im,Point(math.floor(48-im.width/2),grounded and 72-bottom or math.floor(40-im.height/2)))
  canvas:saveAs(art..'Mobile/'..string.format('%s_%02d.png',name,i-1))
  local f=i==1 and s.frames[1] or s:newEmptyFrame();f.duration=.08;s:newCel(s.layers[1],f,canvas)
 end
 s:newTag(1,#frames).name='Move';s:saveAs(source..'Enemies/'..name..'.aseprite');print(name..' '..#frames..' complete frames')
end
local bike=load('Enemies/biker-source.png');local key=bike:getPixel(500,100)
local frames={}
for _,r in ipairs({{3,30,57,43},{63,30,58,43},{124,30,59,43}}) do frames[#frames+1]=cut(bike,r[1],r[2],r[3],r[4],key) end
saveFrames('biker_drive',frames,true)
local tara=load('Enemies/flying-tara-source.png');key=tara:getPixel(320,80)
-- The diving bank includes drawn bomb-release cels. Use the idle fuselage plus
-- independent native propeller poses so flight never depicts phantom bombs.
local body=Image(96,80,ColorMode.RGB);body:drawImage(cut(tara,3,40,80,36,key),Point(8,22))
local propellers,first={},nil
for x=0,150 do
 local active=false
 if x<150 then for y=1332,1353 do if tara:getPixel(x,y)~=key then active=true;break end end end
 if active and not first then first=x end
 if not active and first then propellers[#propellers+1]=cut(tara,first,1332,x-first,22,key);first=nil end
end
assert(#propellers>=6,'Expected independent propeller poses')
local native=Sprite(96,80,ColorMode.RGB);native.layers[1].name='Fuselage';local rotor=native:newLayer();rotor.name='Propeller'
frames={}
for i=1,6 do
 local prop=propellers[i];local overlay=Image(96,80,ColorMode.RGB);overlay:drawImage(prop,Point(87-math.floor(prop.width/2),29))
 local composite=Image(body);composite:drawImage(overlay,Point(0,0));frames[#frames+1]=composite
 local f=i==1 and native.frames[1] or native:newEmptyFrame();f.duration=.08;native:newCel(native.layers[1],f,body);native:newCel(rotor,f,overlay)
end
saveFrames('tara_fly',frames,false)
native:newTag(1,6).name='Fly';native:saveAs(source..'Enemies/tara_fly.aseprite')
local atlas=Image(96*6,80*2,ColorMode.RGB)
for i=0,2 do atlas:drawImage(Image{fromFile=art..'Mobile/'..string.format('biker_drive_%02d.png',i)},Point(i*96,0)) end
for i=0,5 do atlas:drawImage(Image{fromFile=art..'Mobile/'..string.format('tara_fly_%02d.png',i)},Point(i*96,80)) end
atlas:saveAs(source..'Enemies/Mobile-Preview.png')
print('PASS: coast, native beach platform, biker and Flying Tara exported with original palettes.')
