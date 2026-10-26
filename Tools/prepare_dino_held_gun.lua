local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source=Image{fromFile=root .. '/Assets/Game/Art/MetalSlug/hmg_gun.png'}
local rgba=app.pixelColor.rgba
local function colour(hex) return rgba(tonumber(hex:sub(1,2),16),tonumber(hex:sub(3,4),16),tonumber(hex:sub(5,6),16),255) end
local ink,shadow,green,light=colour('1D302A'),colour('436B3E'),colour('66994A'),colour('97BF64')
local sprite=Sprite(40,24,ColorMode.RGB)
local function layer(name) local l=sprite:newLayer();l.name=name;return l end
sprite.layers[1].name='Far arm'
local far=Image(40,24,ColorMode.RGB)
local near=Image(40,24,ColorMode.RGB)
local function rect(im,x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do im:putPixel(xx,yy,c) end end end
rect(far,3,12,5,4,ink);rect(far,6,13,15,4,ink);rect(far,18,11,6,5,ink)
rect(far,4,13,4,2,shadow);rect(far,7,14,13,2,shadow);rect(far,19,12,4,3,green)
sprite:newCel(sprite.layers[1],1,far)
local stock=Image(40,24,ColorMode.RGB)
rect(stock,6,6,8,7,colour('2A2119'));rect(stock,7,7,7,5,colour('70522D'))
rect(stock,7,7,6,1,colour('BD9450'));rect(stock,10,8,6,4,colour('443A22'))
sprite:newCel(layer('Full stock'),1,stock)
local gun=Image(40,24,ColorMode.RGB);gun:drawImage(source,Point(12,4));sprite:newCel(layer('Metal Slug gun'),1,gun)
rect(near,4,15,5,4,ink);rect(near,7,17,11,4,ink);rect(near,15,12,5,8,ink)
rect(near,5,16,4,2,green);rect(near,8,18,9,2,green);rect(near,16,14,3,5,green)
rect(near,6,16,3,1,light);rect(near,8,18,7,1,light);rect(near,17,13,2,3,light)
near:putPixel(18,14,ink);near:putPixel(18,16,ink)
sprite:newCel(layer('Near arm and trigger hand'),1,near)
sprite:saveAs(root .. '/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite')
local image=Image(40,24,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root .. '/Assets/Game/Art/MetalSlug/Combat/hmg_held.png')
print('Prepared full HMG stock and two dinosaur arms in four editable layers.')
