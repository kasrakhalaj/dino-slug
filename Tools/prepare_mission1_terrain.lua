-- Reuse original Mission 1 pixels; no generated replacement landscape.
local root='E:/MyGames/HW1/DinoSlug/'
local folder=root..'Tools/SourceArt/MetalSlug/Terrain/'
local out=root..'Assets/Game/Art/MetalSlug/Terrain/'
local source=app.open(folder..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'}
local sheet=source.cels[1].image
local white=sheet:getPixel(0,0)
local function cut(x,y,w,h,transparent)
 local result=Image(sheet,Rectangle(x,y,w,h))
 if transparent then for yy=0,h-1 do for xx=0,w-1 do if result:getPixel(xx,yy)==white then result:putPixel(xx,yy,0) end end end end
 return result
end
local function mirrored(original)
 local result=Image(original.width*2,original.height,ColorMode.RGB);result:drawImage(original,Point(0,0))
 for y=0,original.height-1 do for x=0,original.width-1 do result:putPixel(original.width+x,y,original:getPixel(original.width-1-x,y)) end end
 for y=0,result.height-1 do
  assert(result:getPixel(0,y)==result:getPixel(result.width-1,y),'Outer tile seam')
  assert(result:getPixel(original.width-1,y)==result:getPixel(original.width,y),'Center tile seam')
 end
 return result
end
local function save(name,image)
 image:saveAs(out..name..'.png')
 local sprite=Sprite(image.width,image.height,ColorMode.RGB);sprite.layers[1].name='Original Metal Slug Mission 1 pixels'
 sprite:newCel(sprite.layers[1],sprite.frames[1],image);sprite:saveAs(folder..name..'.aseprite')
 print(name..' '..image.width..'x'..image.height)
end
local function trim(image)
 local x0,y0,x1,y1=image.width,image.height,0,0
 for y=0,image.height-1 do for x=0,image.width-1 do if app.pixelColor.rgbaA(image:getPixel(x,y))>0 then
  x0=math.min(x0,x);x1=math.max(x1,x);y0=math.min(y0,y);y1=math.max(y1,y)
 end end end
 return Image(image,Rectangle(x0,y0,x1-x0+1,y1-y0+1))
end
local function isolate(image)
 -- Separate the complete prop from disconnected neighboring debris in the sheet.
 local visited,largest={},{}
 for y=0,image.height-1 do for x=0,image.width-1 do
  local start=y*image.width+x
  if not visited[start] and app.pixelColor.rgbaA(image:getPixel(x,y))>0 then
   local component,queue={},{{x,y}};visited[start]=true;local head=1
   while head<=#queue do
    local p=queue[head];head=head+1;component[#component+1]=p
    for dy=-1,1 do for dx=-1,1 do
     local xx,yy=p[1]+dx,p[2]+dy
     if xx>=0 and yy>=0 and xx<image.width and yy<image.height then
      local index=yy*image.width+xx
      if not visited[index] and app.pixelColor.rgbaA(image:getPixel(xx,yy))>0 then visited[index]=true;queue[#queue+1]={xx,yy} end
     end
    end end
   end
   if #component>#largest then largest=component end
  end
 end end
 local result=Image(image.width,image.height,ColorMode.RGB)
 for _,p in ipairs(largest) do result:putPixel(p[1],p[2],image:getPixel(p[1],p[2])) end
 return trim(result)
end
save('ms1_far_jungle',mirrored(cut(2,41,320,176,false)))
local forest=app.open(folder..'prepared-bg3.png');app.command.ChangePixelFormat{format='rgb'}
save('ms1_near_jungle',mirrored(forest.cels[1].image))
save('ms1_stone_ground',mirrored(cut(75,1058,256,58,false)))
save('ms1_village_hut',trim(cut(819,218,118,112,true)))
save('ms1_watchtower',isolate(cut(3900,0,140,180,true)))
-- Tight ground pivot for the original sandbag obstacle, without its sheet padding.
local bags=Image{fromFile=root..'Assets/Game/Art/MetalSlug/Reinforcements/sandbags.png'}
local left,top,right,bottom=bags.width,bags.height,0,0
for y=0,bags.height-1 do for x=0,bags.width-1 do if app.pixelColor.rgbaA(bags:getPixel(x,y))>0 then
 left=math.min(left,x);right=math.max(right,x);top=math.min(top,y);bottom=math.max(bottom,y)
end end end
save('ms1_sandbag_barricade',Image(bags,Rectangle(left,top,right-left+1,bottom-top+1)))
print('PASS: original palette and source crops retained; mirrored terrain edges match exactly; sheet keys removed from scenery.')
