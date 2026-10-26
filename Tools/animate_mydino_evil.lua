-- Native editable Aseprite revision: deliberate foot plants and a wicked armed face.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local output=root .. '/Tools/SourceArt/MyDinoEvil/'
local source=app.open(app.params.source or 'E:/Aseprite/MyDino-Refined-Animated.aseprite')
assert(source and #source.frames==37,'Expected the previous 37-frame refined character')
source:saveAs(output .. 'MyDino-Before-Evil.aseprite')
local rgba,alpha=app.pixelColor.rgba,app.pixelColor.rgbaA
local colours={}
for line in io.lines(root .. '/Tools/SourceArt/MyDinoRefined/palette.txt') do
  local r,g,b=line:match('#(%x%x)(%x%x)(%x%x)')
  if r then colours[#colours+1]=rgba(tonumber(r,16),tonumber(g,16),tonumber(b,16),255) end
end
local function blank() return Image(64,64,ColorMode.RGB) end
local function part(index,frame)
  local image=blank(); local cel=source.layers[index]:cel(frame)
  if cel then image:drawImage(cel.image,cel.position) end
  return image
end
local base={}; for i=1,6 do base[i]=part(i,1) end
-- Old detached lower-leg pixels belonged to the original separation mask.
for y=42,63 do for x=27,63 do base[3]:putPixel(x,y,0) end end
local function pixel(image,x,y,c)
  x,y=math.floor(x+.5),math.floor(y+.5)
  if x>=0 and y>=0 and x<64 and y<64 then image:putPixel(x,y,colours[c]) end
end
local function stroke(image,x1,y1,x2,y2,width,c)
  local steps=math.max(math.abs(x2-x1),math.abs(y2-y1))*3+1
  local radius=(width-1)/2
  for n=0,steps do
    local x,y=x1+(x2-x1)*n/steps,y1+(y2-y1)*n/steps
    for yy=-radius,radius do for xx=-radius,radius do pixel(image,x+xx,y+yy,c) end end
  end
end
local function polygon(image,points,c)
  for y=0,63 do for x=0,63 do
    local inside=false; local j=#points
    for i=1,#points do
      local a,b=points[i],points[j]
      if (a[2]>y+.5)~=(b[2]>y+.5) and x+.5<(b[1]-a[1])*(y+.5-a[2])/(b[2]-a[2])+a[1] then inside=not inside end
      j=i
    end
    if inside then pixel(image,x,y,c) end
  end end
end
local function shifted(image,dx,dy)
  local out=blank(); out:drawImage(image,Point(dx,dy)); return out
end
local function tilted(image,degrees,dx,dy,px,py)
  px,py=px or 31,py or 30
  local out=blank(); local a=math.rad(degrees); local c,s=math.cos(a),math.sin(a)
  for y=0,63 do for x=0,63 do
    local xx,yy=x+.5-px-dx,y+.5-py-dy
    local sx,sy=math.floor(px+c*xx+s*yy),math.floor(py-s*xx+c*yy)
    if sx>=0 and sy>=0 and sx<64 and sy<64 then out:putPixel(x,y,image:getPixel(sx,sy)) end
  end end
  return out
end
local nearX={35,33,31,29,27,25,25,26,29,32,34,35}
local lift={0,0,0,0,0,0,1,3,5,4,2,1}
local bob={0,0,1,1,0,-1,0,0,1,1,0,-1}
local function leg(phase,far)
  local image=blank(); local hip=far and 35 or 30
  local foot=nearX[phase]+(far and 3 or 0); local y=47-lift[phase]
  local swing=phase>=7
  local knee=math.floor(hip+(foot-hip)*.45+.5)+(swing and 1 or 0)
  local ky=swing and 41 or 43
  stroke(image,hip,40,knee,ky,5,1)
  stroke(image,hip,40,knee,ky,3,far and 2 or 3)
  stroke(image,hip,40,knee,ky,1,far and 3 or 4)
  stroke(image,knee,ky,foot,y-1,3,1)
  stroke(image,knee,ky,foot,y-1,1,far and 3 or 5)
  for x=foot-1,foot+4 do pixel(image,x,y,1) end
  for x=foot,foot+3 do pixel(image,x,y-1,far and 3 or 4) end
  pixel(image,foot+2,y,far and 7 or 11); pixel(image,foot+4,y,far and 7 or 11)
  return image
end
local function evilHead(stage)
  local image=Image(base[5])
  -- Retain the user's wide grin and snout silhouette, with a lifted mouth corner.
  polygon(image,{{32,26},{37,27},{43,26},{47,25},{47,30},{43,32},{36,31},{32,28}},1)
  polygon(image,{{33,26},{37,28},{43,27},{46,26},{46,29},{42,31},{36,30},{33,28}},12)
  for x=34,44 do pixel(image,x,27,11) end
  pixel(image,45,26,11); pixel(image,35,28,11); pixel(image,40,28,11); pixel(image,44,28,11)
  for x=37,43 do pixel(image,x,30,7) end
  if stage=='open' or stage=='cackle' then
    local bottom=stage=='cackle' and 34 or 32
    polygon(image,{{33,28},{46,27},{46,bottom-1},{43,bottom+1},{36,bottom},{33,30}},1)
    polygon(image,{{34,28},{45,28},{45,bottom-1},{42,bottom},{36,bottom-1},{34,30}},12)
    for x=35,44 do pixel(image,x,28,11) end
    pixel(image,35,29,11); pixel(image,40,29,11); pixel(image,44,29,11)
    for x=38,43 do pixel(image,x,bottom-1,10) end
    pixel(image,37,bottom-1,9)
  end
  return image
end
local function evilEyes(cackle)
  local image=blank()
  -- Brow descends toward the snout; the narrow eye stays under it.
  stroke(image,31,20,35,22,1,1)
  stroke(image,31,21,34,22,1,1)
  if cackle then
    stroke(image,32,23,35,23,1,1); pixel(image,31,22,1)
  else
    pixel(image,32,22,11); pixel(image,33,23,11); pixel(image,34,23,1); pixel(image,35,23,1)
    pixel(image,32,24,3)
  end
  return image
end
local function run(phase,armed)
  local parts={}
  local sway=({0,0,1,1,1,0,0,0,-1,-1,-1,0})[phase]
  parts[1]=blank()
  for y=0,63 do for x=0,63 do
    local p=base[1]:getPixel(x,y)
    if alpha(p)>0 then parts[1]:putPixel(x,y+bob[phase]+math.floor(sway*math.max(0,28-x)/10+.5),p) end
  end end
  parts[2]=leg((phase+5)%12+1,true)
  parts[3]=shifted(base[3],0,bob[phase])
  parts[4]=leg(phase,false)
  parts[5]=shifted(armed and evilHead('grin') or base[5],0,bob[phase])
  parts[6]=shifted(armed and evilEyes(false) or base[6],0,bob[phase])
  return parts
end
local sprite=Sprite(64,64,ColorMode.RGB); sprite:setPalette(source.palettes[1])
local layers={sprite.layers[1]}; layers[1].name=source.layers[1].name
for i=2,6 do layers[i]=sprite:newLayer(); layers[i].name=source.layers[i].name end
local count=0; local ranges={}
local function add(parts,ms)
  count=count+1; local frame=count==1 and sprite.frames[1] or sprite:newEmptyFrame(); frame.duration=ms/1000
  for i,image in ipairs(parts) do sprite:newCel(layers[i],frame,image,Point(0,0)) end
end
local function tag(name,first) ranges[#ranges+1]={name,first,count} end
for phase=1,12 do add(run(phase,false),75) end; tag('Run',1)
local first=count+1
for number=9,11 do local p={}; for i=1,6 do p[i]=part(i,number) end; add(p,100) end; tag('Jump',first)
first=count+1
for number=12,14 do local p={}; for i=1,6 do p[i]=part(i,number) end; add(p,83) end; tag('Hit',first)
first=count+1
for number=15,22 do local p={}; for i=1,6 do p[i]=part(i,number) end; add(p,125) end; tag('Death',first)
first=count+1
local sequence={{'grin',100,0,0},{'grin',80,-4,0},{'open',80,-8,0}}
for i=1,5 do sequence[#sequence+1]={'cackle',70,-16,-1}; sequence[#sequence+1]={'open',90,-8,1} end
sequence[#sequence+1]={'open',80,-5,0}; sequence[#sequence+1]={'grin',100,-2,0}; sequence[#sequence+1]={'grin',100,0,0}
for index,pose in ipairs(sequence) do
  local phase=(index-1)%12+1; local p=run(phase,true)
  p[5]=tilted(evilHead(pose[1]),pose[3],0,bob[phase]+pose[4])
  p[6]=tilted(evilEyes(pose[1]=='cackle'),pose[3],0,bob[phase]+pose[4])
  add(p,pose[2])
end; tag('Laugh',first)
first=count+1; for phase=1,12 do add(run(phase,true),75) end; tag('ArmedRun',first)
first=count+1
for i=1,3 do
  local p={}; for layer=1,6 do p[layer]=part(layer,8+i) end
  p[5]=tilted(evilHead('grin'),-4-i*3,0,0,32,48)
  p[6]=tilted(evilEyes(false),-4-i*3,0,0,32,48)
  add(p,100)
end; tag('ArmedJump',first)
for _,range in ipairs(ranges) do sprite:newTag(range[2],range[3]).name=range[1] end
sprite:saveAs(output .. 'MyDino-Evil-Animated.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Evil-Animated.aseprite')
local image=blank(); image:drawSprite(sprite,43); image:saveAs(output .. 'MyDino-Evil-Grin.png')
print('Created '..count..' frames: planted 12-frame gait, evil cackle, armed run/jump grin; six editable layers.')
