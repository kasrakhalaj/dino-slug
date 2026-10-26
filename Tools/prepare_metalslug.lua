local root = app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source = root .. '/Tools/SourceArt/MetalSlug/'
local output = root .. '/Assets/Game/Art/MetalSlug/'
local items = Image { fromFile = source .. 'metal-slug-items.png' }
local fx = Image { fromFile = source .. 'metal-slug-weapon-effects.png' }
local marco = Image { fromFile = source .. 'marco-sprites.png' }
local function crop(image, x, y, w, h)
  return Image(image, Rectangle(x,y,w,h))
end
crop(marco, 0, 1670, 350, 150):saveAs(source .. 'gun-inspection.png')
crop(fx, 0, 0, 605, 260):saveAs(source .. 'fx-inspection.png')
-- Clear only the white background connected to a crop's borders.
local function clearBackground(image, key)
  local queue, seen = {}, {}
  local function visit(x,y)
    local id = y * image.width + x
    if x < 0 or y < 0 or x >= image.width or y >= image.height or seen[id] then return end
    seen[id] = true
    if image:getPixel(x,y) ~= key then return end
    image:putPixel(x,y,0)
    queue[#queue+1] = {x,y}
  end
  for x=0,image.width-1 do visit(x,0); visit(x,image.height-1) end
  for y=0,image.height-1 do visit(0,y); visit(image.width-1,y) end
  local n = 1
  while n <= #queue do
    local p=queue[n]; n=n+1
    visit(p[1]-1,p[2]); visit(p[1]+1,p[2]); visit(p[1],p[2]-1); visit(p[1],p[2]+1)
  end
  return image
end
for i=0,3 do
  local icon=clearBackground(crop(items, 12+i*26, 268, 26, 25), app.pixelColor.rgba(255,255,255,255))
  icon:saveAs(output .. 'h_pickup_' .. i .. '.png')
end
for group,name in ipairs({'r','s','f','l'}) do
  for i=0,3 do
    local icon=clearBackground(crop(items, 12+group*104+i*26, 268, 26, 25), app.pixelColor.rgba(255,255,255,255))
    icon:saveAs(source .. 'Prepared/' .. name .. '_pickup_' .. i .. '.png')
  end
end
local gun=crop(marco, 19, 1711, 22, 13)
gun:saveAs(output .. 'hmg_gun.png')
local bullet=clearBackground(crop(fx, 3, 17, 36, 13), fx:getPixel(0,0))
bullet:saveAs(output .. 'hmg_bullet.png')
local doc=Sprite(26,25,ColorMode.RGB)
doc.layers[1].name='H pickup - original Metal Slug pixels'
for i=0,3 do
  local frame=i==0 and doc.frames[1] or doc:newEmptyFrame()
  frame.duration=.12
  doc:newCel(doc.layers[1],frame,Image{fromFile=output .. 'h_pickup_' .. i .. '.png'})
end
doc:newTag(1,4).name='H pickup'
doc:saveAs(source .. 'H-Pickup.aseprite')
print('Prepared H pickup frames and source inspection crops.')
