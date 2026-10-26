-- Build on the refined animation and the user's MyDino2 grin, keeping editable layers.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local output=root .. '/Tools/SourceArt/MyDinoRefined/'
local rgba,alpha=app.pixelColor.rgba,app.pixelColor.rgbaA
local source=app.open(output .. 'MyDino-Refined-Animated.aseprite')
assert(#source.frames==22,'Start with the 22-frame palette refinement, before the laugh is appended')
local user=app.open(output .. 'MyDino2-Original.aseprite')
local firstUser,secondUser=Image(64,64,ColorMode.RGB),Image(64,64,ColorMode.RGB)
firstUser:drawSprite(user,1); secondUser:drawSprite(user,2)
local before=Image{fromFile=root .. '/Tools/SourceArt/MyDinoAnimation/MyDino-Clean.png'}
local after=Image{fromFile=output .. 'MyDino-Refined.png'}
local map,colours={},{}
for y=0,31 do for x=0,31 do
  if alpha(before:getPixel(x,y))>0 then map[before:getPixel(x,y)]=after:getPixel(x,y) end
end end
for line in io.lines(output .. 'palette.txt') do
  local r,g,b=line:match('#(%x%x)(%x%x)(%x%x)')
  colours[#colours+1]={tonumber(r,16),tonumber(g,16),tonumber(b,16)}
end
local function colour(i) local c=colours[i]; return rgba(c[1],c[2],c[3],255) end
local function refine(pixel)
  if alpha(pixel)==0 then return 0 end
  if map[pixel] then return map[pixel] end
  local r,g,b=app.pixelColor.rgbaR(pixel),app.pixelColor.rgbaG(pixel),app.pixelColor.rgbaB(pixel)
  local best,distance=1,math.huge
  for i,c in ipairs(colours) do
    local d=(r-c[1])^2+(g-c[2])^2+(b-c[3])^2
    if d<distance then best,distance=i,d end
  end
  return colour(best)
end
local function blank() return Image(64,64,ColorMode.RGB) end
local function part(sprite,layer,frame)
  local result=blank()
  local cel=sprite.layers[layer]:cel(frame)
  if cel then result:drawImage(cel.image,cel.position) end
  return result
end
local neutral=part(source,5,1)
local grin=Image(neutral)
for y=26,33 do for x=33,47 do
  if firstUser:getPixel(x,y)~=secondUser:getPixel(x,y) then
    grin:putPixel(x,y,refine(secondUser:getPixel(x,y)))
  end
end end
local function head(stage)
  if stage=='neutral' then return Image(neutral) end
  if stage=='smirk' then
    local image=Image(neutral)
    for x=36,42 do image:putPixel(x,27,colour(1)) end
    image:putPixel(35,26,colour(1)); image:putPixel(43,26,colour(1))
    return image
  end
  local image=Image(grin)
  for y=26,29 do for x=33,46 do
    local pixel=secondUser:getPixel(x,y)
    if app.pixelColor.rgbaR(pixel)==255 and app.pixelColor.rgbaG(pixel)==255 and app.pixelColor.rgbaB(pixel)==255 then
      local value=colour(11)
      if stage=='open' and y>=28 or stage=='laugh' and y>=27 then value=colour(12) end
      image:putPixel(x,y,value)
    end
  end end
  if stage=='grin' then
    image:putPixel(38,28,colour(7)); image:putPixel(42,28,colour(7))
  else
    for x=38,43 do image:putPixel(x,29,colour(10)) end
    image:putPixel(37,29,colour(9))
  end
  return image
end
local function eyes(stage)
  if stage=='neutral' or stage=='smirk' then return part(source,6,1) end
  local image=blank()
  image:putPixel(32,22,colour(1)); image:putPixel(33,21,colour(1)); image:putPixel(34,22,colour(1))
  image:putPixel(31,23,colour(3)); image:putPixel(35,23,colour(3))
  return image
end
local function transform(image,degrees,dx,dy)
  local result=blank()
  local angle=math.rad(degrees)
  local c,s=math.cos(angle),math.sin(angle)
  for y=0,63 do for x=0,63 do
    local xx,yy=x+.5-31-dx,y+.5-30-dy
    local sx,sy=math.floor(31+c*xx+s*yy),math.floor(30-s*xx+c*yy)
    if sx>=0 and sy>=0 and sx<64 and sy<64 then result:putPixel(x,y,image:getPixel(sx,sy)) end
  end end
  return result
end
local sprite=Sprite(64,64,ColorMode.RGB)
sprite:setPalette(source.palettes[1])
local layers={sprite.layers[1]}
layers[1].name=source.layers[1].name
for i=2,#source.layers do layers[i]=sprite:newLayer(); layers[i].name=source.layers[i].name end
local count=0
local function add(parts,duration)
  count=count+1
  local frame=count==1 and sprite.frames[1] or sprite:newEmptyFrame()
  frame.duration=duration
  for i,image in ipairs(parts) do sprite:newCel(layers[i],frame,image,Point(0,0)) end
end
for number,frame in ipairs(source.frames) do
  local parts={}
  for i=1,#layers do parts[i]=part(source,i,number) end
  add(parts,frame.duration)
end
local sequence={
  {'smirk',90,0,0}, {'grin',100,-3,0}, {'open',90,-5,0},
  {'laugh',80,-10,-1}, {'open',80,-6,1}, {'laugh',80,-11,-1}, {'open',80,-7,1},
  {'laugh',80,-10,-1}, {'open',80,-6,1}, {'laugh',80,-11,-1}, {'open',80,-7,1},
  {'open',90,-5,0}, {'grin',90,-3,0}, {'smirk',100,0,0}, {'neutral',90,0,0}
}
local bob={0,0,-1,-1,0,0,-1,-1}
for index,pose in ipairs(sequence) do
  local phase=(index-1)%8+1
  local parts={}
  for i=1,#layers do parts[i]=part(source,i,phase) end
  parts[1]=transform(parts[1],0,pose[4],0)
  parts[3]=transform(parts[3],0,pose[4],0)
  parts[5]=transform(head(pose[1]),pose[3],pose[4],bob[phase])
  parts[6]=transform(eyes(pose[1]),pose[3],pose[4],bob[phase])
  add(parts,pose[2]/1000)
end
-- Define tags only after every frame exists so Aseprite cannot extend earlier tags.
for _,tag in ipairs(source.tags) do sprite:newTag(tag.fromFrame.frameNumber,tag.toFrame.frameNumber).name=tag.name end
sprite:newTag(23,count).name='Laugh'
sprite:saveAs(output .. 'MyDino-Refined-Laugh.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Refined-Animated.aseprite')
local preview=Image(64,64,ColorMode.RGB)
preview:drawSprite(sprite,26)
preview:saveAs(output .. 'MyDino-Laugh.png')
print('Created 15 pickup-laugh transition frames from MyDino2; 37 frames total, six editable layers.')
