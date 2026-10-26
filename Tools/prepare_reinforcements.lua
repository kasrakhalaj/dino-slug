-- Native Aseprite extraction: preserve SNK pixels, remove sheet keys, align cels.
local root='E:/MyGames/HW1/DinoSlug'
local src=root..'/Tools/SourceArt/MetalSlug/Reinforcements/'
local out=root..'/Assets/Game/Art/MetalSlug/Reinforcements/'
local white=app.pixelColor.rgba(255,255,255,255)
local alpha=app.pixelColor.rgbaA
local function load(path) local s=Sprite{fromFile=path};app.activeSprite=s;app.command.ChangePixelFormat{format="rgb"};return s.cels[1].image end
local function cut(im,x,y,w,h,key)
 local r=Image(im,Rectangle(x,y,w,h))
 for yy=0,h-1 do for xx=0,w-1 do if r:getPixel(xx,yy)==key then r:putPixel(xx,yy,0) end end end
 return r
end
local function row(im,y,h)
 local list={};local start=nil
 for x=0,im.width do
  local on=false
  if x<im.width then for yy=y,y+h-1 do local p=im:getPixel(x,yy);if p~=white and alpha(p)>0 then on=true;break end end end
  if on and not start then start=x end
  if not on and start then list[#list+1]=cut(im,start,y,x-start,h,white);start=nil end
 end
 return list
end
local function align(im,feet)
 local bottom=0
 for y=0,im.height-1 do for x=0,im.width-1 do if alpha(im:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end end end
 local anchor=im.width/2
 if feet then
  local l,r=im.width,0
  for y=math.max(0,bottom-4),bottom-1 do for x=0,im.width-1 do if alpha(im:getPixel(x,y))>0 then l=math.min(l,x);r=math.max(r,x) end end end
  anchor=(l+r)/2
 end
 local canvas=Image(80,64,ColorMode.RGB);canvas:drawImage(im,Point(math.floor(40-anchor+.5),60-bottom));return canvas
end
local contact=Image(80*12,64*5,ColorMode.RGB);local contactRow=0
local function save(name,frames,feet)
 local s=Sprite(80,64,ColorMode.RGB);s.layers[1].name='Original SNK pixels'
 for i,im in ipairs(frames) do
  local canvas=align(im,feet);canvas:saveAs(out..string.format('%s_%02d.png',name,i-1))
  local f=i==1 and s.frames[1] or s:newEmptyFrame();f.duration=.085;s:newCel(s.layers[1],f,canvas)
  if i<=12 then contact:drawImage(canvas,Point((i-1)*80,contactRow*64)) end
 end
 s:newTag(1,#frames).name=name;s:saveAs(src..name..'.aseprite');contactRow=contactRow+1
 print(name..' '..#frames)
end
local shield=load(src..'shield-source.png');local bazooka=load(src..'bazooka-source.png')
save('shield_run',row(shield,46,41),true)
save('bazooka_run',row(bazooka,51,43),true)
local aim=row(bazooka,288,58)
-- Keep the horizontal aiming pose; other recoil rows point upward and are unsuitable here.
save('bazooka_fire',{aim[1],aim[1],aim[1],aim[1]},true)
local aligned=align(aim[1],true);local left,top,bottom=80,64,0
for y=0,63 do for x=0,79 do if alpha(aligned:getPixel(x,y))>0 then left=math.min(left,x) end end end
for y=0,63 do if alpha(aligned:getPixel(left,y))>0 then top=math.min(top,y);bottom=math.max(bottom,y) end end
local f=io.open(src..'bazooka-origin.json','w');f:write(string.format('{"x":%.6f,"y":%.6f}',(left-40)/24,(60-(top+bottom+1)/2)/24));f:close()
local death=row(bazooka,493,43);local prone=row(bazooka,539,26)
for i=1,3 do death[#death+1]=prone[i] end
save('bazooka_death',death,false)
-- Shield collapse retains the infantry's existing full defeat cels and dropped equipment.
local detached=cut(shield,0,907,32,31,white)
local shieldDeath={}
for i=0,10 do
 local im=load(root..'/Assets/Game/Art/MetalSlug/Combat/rifle_death_'..string.format('%02d',i)..'.png')
 local frame=Image(im);frame:drawImage(detached,Point(4,29));shieldDeath[#shieldDeath+1]=frame
end
save('shield_death',shieldDeath,false)
local sand=load(src..'sandbags-source.png');cut(sand,10,30,49,40,sand:getPixel(0,0)):saveAs(out..'sandbags.png')
local barrel=load(src..'barrel-source.png');cut(barrel,0,0,43,52,barrel:getPixel(0,51)):saveAs(out..'barrel.png')
local items=load(root..'/Tools/SourceArt/MetalSlug/metal-slug-items.png')
cut(items,51,834,40,37,white):saveAs(out..'wooden_barrel.png')
cut(items,13,334,24,23,white):saveAs(out..'medicine.png')
cut(bazooka,0,826,44,25,app.pixelColor.rgba(0,255,0,255)):saveAs(out..'rocket.png')
contact:saveAs(src..'Extracted-Animations.png')
print('Prepared native soldiers, scenery, medicine and rocket.')
