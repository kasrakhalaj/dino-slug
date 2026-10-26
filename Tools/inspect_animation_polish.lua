local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/MyDinoPolish/'
local a=app.pixelColor.rgbaA
local function enlarged(im,scale)
  local result=Image(im.width*scale,im.height*scale,ColorMode.RGB)
  for y=0,result.height-1 do for x=0,result.width-1 do result:putPixel(x,y,im:getPixel(math.floor(x/scale),math.floor(y/scale))) end end
  return result
end
local sprite=app.open('E:/Aseprite/MyDino-Evil-Animated.aseprite')
print('DINO '..sprite.width..'x'..sprite.height..' frames='..#sprite.frames)
for i,l in ipairs(sprite.layers) do
  local count={} local minx,miny,maxx,maxy=64,64,0,0
  local cel=l:cel(1)
  if cel then for y=0,cel.image.height-1 do for x=0,cel.image.width-1 do local p=cel.image:getPixel(x,y);if a(p)>0 then
    local key=string.format('#%02X%02X%02X',app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p))
    count[key]=(count[key] or 0)+1
    minx=math.min(minx,x+cel.position.x);maxx=math.max(maxx,x+cel.position.x);miny=math.min(miny,y+cel.position.y);maxy=math.max(maxy,y+cel.position.y)
  end end end end
  print(i..' '..l.name..' bounds='..minx..','..miny..' '..maxx..','..maxy)
  for c,n in pairs(count) do print(' '..c..' '..n) end
end
for _,tag in ipairs(sprite.tags) do
  local n=tag.toFrame.frameNumber-tag.fromFrame.frameNumber+1
  local sheet=Image(64*math.min(n,8),64*math.ceil(n/8),ColorMode.RGB)
  for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
    local im=Image(64,64,ColorMode.RGB);im:drawSprite(sprite,f)
    local k=f-tag.fromFrame.frameNumber;sheet:drawImage(im,Point(k%8*64,math.floor(k/8)*64))
  end
  enlarged(sheet,3):saveAs(out..tag.name..'-before.png')
  print(tag.name..' '..tag.fromFrame.frameNumber..'-'..tag.toFrame.frameNumber)
end
local gun=app.open(root..'/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite')
print('GUN '..gun.width..'x'..gun.height)
local flat=Image(gun.width,gun.height,ColorMode.RGB);flat:drawSprite(gun,1)
enlarged(flat,12):saveAs(out..'gun-before.png')
for i,l in ipairs(gun.layers) do print(i..' '..l.name) end
