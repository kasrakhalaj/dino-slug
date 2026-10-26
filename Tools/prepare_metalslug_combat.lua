local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source=root .. '/Tools/SourceArt/MetalSlug/'
local output=root .. '/Assets/Game/Art/MetalSlug/Combat/'
local rifle=Image{fromFile=source .. 'Enemies/rebel-rifle-source.png'}
local rebel=Image{fromFile=source .. 'Enemies/rebel-sprites.png'}
local fx=Image{fromFile=source .. 'metal-slug-weapon-effects.png'}
local items=Image{fromFile=source .. 'metal-slug-items.png'}
local function crop(image,x,y,w,h,key)
  local result=Image(image,Rectangle(x,y,w,h))
  if key then for yy=0,h-1 do for xx=0,w-1 do
    if result:getPixel(xx,yy)==key then result:putPixel(xx,yy,0) end
  end end end
  return result
end
local white=app.pixelColor.rgba(255,255,255,255)
local function prepare(name,image,ranges,key)
  local sprite=Sprite(80,64,ColorMode.RGB);sprite.layers[1].name='Original Metal Slug pixels'
  local count,tags=0,{}
  for _,range in ipairs(ranges) do
    local first=count+1
    for index,col in ipairs(range.columns) do
      local part=crop(image,col[1],range.y,col[2]-col[1],range.h,key)
      local bottom=0
      for y=0,part.height-1 do for x=0,part.width-1 do
        if app.pixelColor.rgbaA(part:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end
      end end
      local full=Image(80,64,ColorMode.RGB)
      full:drawImage(part,Point(51-part.width,60-bottom))
      full:saveAs(output .. string.format('%s_%s_%02d.png',name,string.lower(range.name),index-1))
      count=count+1; local frame=count==1 and sprite.frames[1] or sprite:newEmptyFrame()
      frame.duration=range.ms/1000; sprite:newCel(sprite.layers[1],frame,full)
    end
    tags[#tags+1]={range.name,first,count}
  end
  for _,tag in ipairs(tags) do sprite:newTag(tag[2],tag[3]).name=tag[1] end
  sprite:saveAs(source .. 'Enemies/' .. name .. '-Combat.aseprite')
end
prepare('rifle',rifle,{
 {name='Walk',y=44,h=37,ms=90,columns={{3,37},{40,77},{80,122},{125,167},{170,207},{210,245},{248,282},{285,323},{326,366},{369,407},{410,447},{450,485}}},
 {name='Fire',y=3,h=38,ms=100,columns={{3,43},{46,86},{89,128},{131,171}}},
 {name='Death',y=652,h=45,ms=83,columns={{3,46},{49,88},{91,130},{133,171},{174,213},{218,250},{253,280},{283,314},{317,365}}}
},white)
prepare('grenadier',rebel,{
 {name='Throw',y=234,h=37,ms=100,columns={{42,67},{113,135},{182,203},{249,271},{315,339},{380,407},{446,475},{514,543}}}
},nil)
local grenade=crop(items,344,76,12,13,white)
local full=Image(24,24,ColorMode.RGB);full:drawImage(grenade,Point(6,5));full:saveAs(output .. 'grenade.png')
local bullet=crop(fx,10,76,20,5,fx:getPixel(0,0))
bullet:saveAs(output .. 'enemy_bullet.png')
local cells={{4,680,33,48},{39,680,49,48},{91,680,57,48},{151,680,57,48},
 {211,680,59,48},{273,680,66,48},{70,728,60,59},{197,728,62,59}}
local burst=Sprite(72,72,ColorMode.RGB);burst.layers[1].name='Original weapon explosion pixels'
for i,cell in ipairs(cells) do
  local part=crop(fx,cell[1],cell[2],cell[3],cell[4],fx:getPixel(0,0))
  local image=Image(72,72,ColorMode.RGB);image:drawImage(part,Point(math.floor((72-part.width)/2),math.floor((72-part.height)/2)))
  image:saveAs(output .. string.format('grenade_blast_%02d.png',i-1))
  local frame=i==1 and burst.frames[1] or burst:newEmptyFrame();frame.duration=.05
  burst:newCel(burst.layers[1],frame,image)
end
burst:newTag(1,#cells).name='Blast';burst:saveAs(source .. 'Enemies/Grenade-Blast.aseprite')
print('Prepared rifle Walk/Fire/Death, grenadier Throw, grenade, enemy bullet and eight blast frames.')
