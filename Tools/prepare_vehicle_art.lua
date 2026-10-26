-- Extract original SNK vehicle pixels through Aseprite; retain complete source banners.
local root='E:/MyGames/HW1/DinoSlug'
local src=root..'/Tools/SourceArt/MetalSlug/Vehicles/'
local out=root..'/Assets/Game/Art/MetalSlug/Vehicles/'
local function load(name)
 local s=app.open(src..name..'-source.png');app.command.ChangePixelFormat{format='rgb'};return s.cels[1].image
end
local alpha=app.pixelColor.rgbaA
local white=app.pixelColor.rgba(255,255,255,255)
local green=app.pixelColor.rgba(0,255,0,255)
local function cut(im,x,y,w,h,key)
 local r=Image(im,Rectangle(x,y,w,h))
 for yy=0,h-1 do for xx=0,w-1 do if r:getPixel(xx,yy)==key then r:putPixel(xx,yy,0) end end end
 return r
end
local function row(im,y,h,limit,key)
 local result={};local first=nil
 for x=0,limit do
  local opaque=false
  if x<limit then for yy=y,y+h-1 do local p=im:getPixel(x,yy);if alpha(p)>0 and p~=key then opaque=true;break end end end
  if opaque and not first then first=x end
  if not opaque and first then result[#result+1]=cut(im,first,y,x-first,h,key);first=nil end
 end
 return result
end
local function alignTank(im)
 local canvas=Image(128,128,ColorMode.RGB)
 local bottom=0
 for y=0,im.height-1 do for x=0,im.width-1 do if alpha(im:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end end end
 canvas:drawImage(im,Point(math.floor(64-im.width/2),112-bottom));return canvas
end
local previews=Image(128*8,128*4,ColorMode.RGB);local previewRow=0
local function save(name,frames,fps,center)
 local spr=Sprite(128,128,ColorMode.RGB);spr.layers[1].name='Original SNK vehicle pixels'
 for i,im in ipairs(frames) do
  local canvas=center and im or alignTank(im)
  canvas:saveAs(out..string.format('%s_%02d.png',name,i-1))
  local f=i==1 and spr.frames[1] or spr:newEmptyFrame();f.duration=1/fps;spr:newCel(spr.layers[1],f,canvas)
  if i<=8 and previewRow<4 then previews:drawImage(canvas,Point((i-1)*128,previewRow*128)) end
 end
 spr:newTag(1,#frames).name=name;spr:saveAs(src..name..'.aseprite');previewRow=previewRow+1
 print(name..' '..#frames..' frames')
end
local tank=load('di-cokka')
save('tank_drive',row(tank,61,57,446,white),12,false)
local prepare=row(tank,357,61,532,white);local fire=row(tank,421,60,296,white)
save('tank_fire',fire,12,false)
local aimed=alignTank(fire[1]);local left,top,bottom=128,128,0
for y=52,74 do for x=0,127 do if alpha(aimed:getPixel(x,y))>0 then left=math.min(left,x) end end end
for y=52,74 do if alpha(aimed:getPixel(left,y))>0 then top=math.min(top,y);bottom=math.max(bottom,y) end end
local origin=io.open(src..'tank-origin.json','w');origin:write(string.format('{"x":%.6f,"y":%.6f}',(left-64)/20,(112-(top+bottom+1)/2)/20));origin:close()
local death=row(tank,613,68,507,white)
local wreck=row(tank,684,55,501,white)
for _,im in ipairs(wreck) do death[#death+1]=im end
local final=row(tank,742,47,207,white);for _,im in ipairs(final) do death[#death+1]=im end
save('tank_death',death,14,false)
-- A small original shell sprite from the bank of SFX.
cut(tank,2,809,13,23,tank:getPixel(2,809)):saveAs(out..'tank_shell.png')
local heli=load('r-shobu')
local body=row(heli,20,59,668,green)
assert(#body==7,'Expected seven helicopter body poses, got '..#body)
local rotors={}
for i=0,3 do rotors[#rotors+1]=cut(heli,4,226+i*26,91,22,green) end
local flight={}
local native=Sprite(128,128,ColorMode.RGB);native.layers[1].name='Fuselage';local rotorLayer=native:newLayer();rotorLayer.name='Main rotor'
for i=0,27 do
 local image=body[i%7+1];local b=Image(128,128,ColorMode.RGB);b:drawImage(image,Point(math.floor(64-image.width/2),44))
 local r=Image(128,128,ColorMode.RGB);r:drawImage(rotors[i%4+1],Point(19,30))
 local composite=Image(b);composite:drawImage(r,Point(0,0));flight[#flight+1]=composite
 local f=i==0 and native.frames[1] or native:newEmptyFrame();f.duration=1/18
 native:newCel(native.layers[1],f,b);native:newCel(rotorLayer,f,r)
end
save('heli_fly',flight,18,true)
native:newTag(1,28).name='Fly';native:saveAs(src..'R-Shobu-Layered.aseprite')
cut(heli,7,477,15,29,green):saveAs(out..'heli_bomb.png')
previews:saveAs(src..'Vehicle-Animation-Preview.png')
print('Prepared original tank, helicopter, independent rotor poses and ordnance.')
