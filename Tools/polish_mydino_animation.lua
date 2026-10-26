-- Refine the saved user art in Aseprite; preserve faces, tags and expression timings.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local output=root..'/Tools/SourceArt/MyDinoPolish/'
local function exists(path) local f=io.open(path,'rb');if f then f:close();return true end;return false end
local characterBackup=output..'MyDino-User-Before-Polish.aseprite'
local source=app.open(app.params.source or (exists(characterBackup) and characterBackup or 'E:/Aseprite/MyDino-Evil-Animated.aseprite'))
assert(source and #source.frames==57 and #source.layers==6)
if not exists(characterBackup) then source:saveAs(characterBackup) end
local rgba,a=app.pixelColor.rgba,app.pixelColor.rgbaA
local function colour(hex) return rgba(tonumber(hex:sub(1,2),16),tonumber(hex:sub(3,4),16),tonumber(hex:sub(5,6),16),255) end
local ink,deep,shade,green,light,claw=colour('1D302A'),colour('2A4A34'),colour('436B3E'),colour('66994A'),colour('97BF64'),colour('C6CB80')
local function blank() return Image(64,64,ColorMode.RGB) end
local function part(sprite,index,f)
  local im=blank();local cel=sprite.layers[index]:cel(f)
  if cel then im:drawImage(cel.image,cel.position) end
  return im
end
local function put(im,x,y,c) x,y=math.floor(x+.5),math.floor(y+.5);if x>=0 and y>=0 and x<64 and y<64 then im:putPixel(x,y,c) end end
local function transform(im,deg,dx,dy)
  local out=blank();local r=math.rad(deg);local c,s=math.cos(r),math.sin(r)
  for y=0,63 do for x=0,63 do
    local xx,yy=x+.5-32-dx,y+.5-48-dy
    local sx,sy=math.floor(32+c*xx+s*yy),math.floor(48-s*xx+c*yy)
    if sx>=0 and sy>=0 and sx<64 and sy<64 then out:putPixel(x,y,im:getPixel(sx,sy)) end
  end end;return out
end
local function shift(im,dx,dy) local out=blank();out:drawImage(im,Point(dx,dy));return out end
local footX={35,33,31,29,27,25,25,27,30,33,35,36}
local footY={47,47,47,47,47,47,46,44,43,43,45,46}
local function leg(hipX,hipY,kneeX,kneeY,heelX,heelY,far)
  local mask=blank()
  local function disc(x,y,r)
    for yy=math.floor(y-r),math.ceil(y+r) do for xx=math.floor(x-r),math.ceil(x+r) do
      if (xx-x)^2+(yy-y)^2<=r*r then put(mask,xx,yy,green) end
    end end
  end
  local function limb(x1,y1,x2,y2,r1,r2)
    for i=0,24 do local t=i/24;disc(x1+(x2-x1)*t,y1+(y2-y1)*t,r1+(r2-r1)*t) end
  end
  limb(hipX,hipY,kneeX,kneeY,2.3,1.9)
  limb(kneeX,kneeY,heelX,heelY-1,1.8,1.4)
  -- A compact forward-facing toe with a single muted claw; no bright shoe band.
  for x=heelX-1,heelX+3 do put(mask,x,heelY,green);put(mask,x,heelY-1,green) end
  local out=blank()
  for y=0,63 do for x=0,63 do if a(mask:getPixel(x,y))>0 then
    local edge=false
    for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
      local xx,yy=x+d[1],y+d[2]
      if xx<0 or yy<0 or xx>63 or yy>63 or a(mask:getPixel(xx,yy))==0 then edge=true end
    end
    local c=far and shade or green
    if edge and y>=hipY+2 then c=ink
    elseif x<math.min(hipX,kneeX) or y>=heelY-1 then c=far and deep or shade end
    put(out,x,y,c)
  end end end
  -- Open the hip into the body instead of drawing a black seam across it.
  for x=hipX-1,hipX+1 do put(out,x,hipY,far and shade or green) end
  if not far then
    put(out,hipX+1,hipY+1,light);put(out,kneeX,kneeY-1,light)
  end
  put(out,heelX+3,heelY,far and shade or claw)
  return out
end
local function runLeg(phase,far,bob)
  local hx=far and 34 or 31
  local fx=footX[phase]+(far and 2 or 0);local fy=footY[phase]
  local ky=phase>=8 and 42 or 43
  local kx=math.floor(hx+(fx-hx)*.45+.5)
  return leg(hx,40+bob,kx,ky,fx,fy,far)
end
local beforeHeads={}
for f=1,#source.frames do beforeHeads[f]={part(source,5,f),part(source,6,f)} end
-- The older action poses rotate around the same foot pivot. Find their saved
-- body translation rather than regenerating any user-painted upper body.
local reference=app.open('E:/Aseprite/MyDino-Refined-Animated.aseprite')
local baseBody=part(reference,3,1)
for y=42,63 do for x=27,63 do baseBody:putPixel(x,y,0) end end
local function fittedTranslation(f,deg,dx)
  local body=part(source,3,f);local best,bestDy=-1,0
  for dy=-16,12 do
    local test=transform(baseBody,deg,dx,dy);local score=0
    for y=0,63 do for x=0,63 do local p=test:getPixel(x,y)
      if a(p)>0 and p==body:getPixel(x,y) then score=score+1 end
    end end
    if score>best then best,bestDy=score,dy end
  end
  assert(best>65,'Body alignment failed at frame '..f..': '..best)
  return bestDy
end
local function replace(f,index,im)
  local cel=source.layers[index]:cel(f)
  if cel then cel.image=im;cel.position=Point(0,0) else source:newCel(source.layers[index],f,im,Point(0,0)) end
end
for _,tag in ipairs(source.tags) do
  for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
    local i=f-tag.fromFrame.frameNumber+1
    local near,far
    if tag.name=='Run' or tag.name=='ArmedRun' or tag.name=='Laugh' then
      local phase=(i-1)%12+1
      local body=part(source,3,f);local bottom=0
      for y=0,63 do for x=27,39 do if a(body:getPixel(x,y))>0 then bottom=math.max(bottom,y) end end end
      local bob=bottom-41
      near=runLeg(phase,false,bob)
      far=runLeg((phase+5)%12+1,true,bob)
      if tag.name~='Laugh' then source.frames[f].duration=.065 end
    else
      local deg,dx=0,0
      if tag.name=='Jump' or tag.name=='ArmedJump' then deg=-4-i*3
      elseif tag.name=='Hit' then deg=({-10,5,0})[i];dx=i==1 and -1 or 0
      elseif tag.name=='Death' then deg=({0,12,28,46,65,82,90,90})[i] end
      local dy=fittedTranslation(f,deg,dx)
      if tag.name=='Jump' or tag.name=='ArmedJump' then
        -- Gather the legs on ascent, tuck at the apex, extend to land.
        near=leg(31,40,({30,28,32})[i],({43,41,43})[i],({27,29,34})[i],({46,44,46})[i],false)
        far=leg(34,40,({35,35,34})[i],({42,42,44})[i],({37,36,31})[i],({44,44,47})[i],true)
      elseif tag.name=='Hit' then
        near=leg(31,40,30,43,29,47,false);far=leg(34,40,35,43,36,46,true)
      else
        local tuck=math.min(3,math.floor(i/2))
        near=leg(31,40,30,43-tuck,28,47-tuck,false)
        far=leg(34,40,35,42,36,47-tuck,true)
      end
      near=transform(near,deg,dx,dy);far=transform(far,deg,dx,dy)
      -- Clear legacy detached lower-leg pixels embedded in the body layer.
      local mask=blank();for y=42,63 do for x=27,63 do put(mask,x,y,green) end end
      mask=transform(mask,deg,dx,dy)
      local body=part(source,3,f)
      for y=0,63 do for x=0,63 do if a(mask:getPixel(x,y))>0 then body:putPixel(x,y,0) end end end
      replace(f,3,body)
      print(tag.name..' frame '..i..' body offset '..dx..','..dy)
    end
    replace(f,2,far);replace(f,4,near)
  end
end
-- Exact cel checks protect the saved laughter, face and eye edits.
for f=1,#source.frames do for j,index in ipairs({5,6}) do
  local im=part(source,index,f)
  for y=0,63 do for x=0,63 do assert(im:getPixel(x,y)==beforeHeads[f][j]:getPixel(x,y),'Face changed') end end
end end
source:saveAs(output..'MyDino-Polished.aseprite')
source:saveAs('E:/Aseprite/MyDino-Polished.aseprite')
local function enlarged(im,scale)
  local out=Image(im.width*scale,im.height*scale,ColorMode.RGB)
  for y=0,out.height-1 do for x=0,out.width-1 do out:putPixel(x,y,im:getPixel(math.floor(x/scale),math.floor(y/scale))) end end;return out
end
for _,tag in ipairs(source.tags) do
  local n=tag.toFrame.frameNumber-tag.fromFrame.frameNumber+1
  local sheet=Image(math.min(n,8)*64,math.ceil(n/8)*64,ColorMode.RGB)
  for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do local im=blank();im:drawSprite(source,f);local k=f-tag.fromFrame.frameNumber
    sheet:drawImage(im,Point(k%8*64,math.floor(k/8)*64))
  end
  enlarged(sheet,4):saveAs(output..tag.name..'-after.png')
end
-- Keep the user's complete stock/receiver/barrel cels exactly as saved.
local gunBackup=output..'Dino-Held-HMG-User-Before.aseprite'
local gun=app.open(app.params.gun or (exists(gunBackup) and gunBackup or root..'/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite'))
if not exists(gunBackup) then gun:saveAs(gunBackup) end
local function gunPart(name,points)
  local layer
  for _,l in ipairs(gun.layers) do if l.name==name then layer=l end end
  assert(layer,'Missing arm layer '..name)
  local im=Image(gun.width,gun.height,ColorMode.RGB)
  for _,p in ipairs(points) do im:putPixel(p[1],p[2],p[3]) end
  local cel=layer:cel(1)
  if cel then cel.image=im;cel.position=Point(0,0) else gun:newCel(layer,1,im) end
end
gunPart('Far arm',{{20,12,shade},{21,12,shade},{21,13,shade},{22,13,shade},{23,13,green},{23,12,green}})
gunPart('Near arm and trigger hand',{{15,15,green},{16,15,green},{16,16,green},{17,16,green},{18,16,shade},{18,15,green},{17,15,light},{17,17,shade},{18,17,ink}})
gun:saveAs(root..'/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite')
local flat=Image(gun.width,gun.height,ColorMode.RGB);flat:drawSprite(gun,1)
flat:saveAs(root..'/Assets/Game/Art/MetalSlug/Combat/hmg_held.png')
enlarged(flat,12):saveAs(output..'gun-after.png')
print('Polished 57 frames; face and eye pixels preserved, tiny hands added over the user gun.')
