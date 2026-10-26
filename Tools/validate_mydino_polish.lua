local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/MyDinoPolish/'
local original=app.open(out..'MyDino-User-Before-Polish.aseprite')
local polished=app.open(out..'MyDino-Polished.aseprite')
local a=app.pixelColor.rgbaA
local function part(s,index,f)
  local im=Image(s.width,s.height,ColorMode.RGB);local cel=s.layers[index]:cel(f)
  if cel then im:drawImage(cel.image,cel.position) end
  return im
end
local function equal(im,other)
  for y=0,im.height-1 do for x=0,im.width-1 do assert(im:getPixel(x,y)==other:getPixel(x,y),'Changed preserved pixel at '..x..','..y) end end
end
assert(#polished.frames==57 and #polished.tags==7 and #polished.layers==6)
local colours={}
for f=1,57 do
  equal(part(original,5,f),part(polished,5,f));equal(part(original,6,f),part(polished,6,f))
  equal(part(original,1,f),part(polished,1,f))
  local flat=Image(64,64,ColorMode.RGB);flat:drawSprite(polished,f)
  for y=0,63 do for x=0,63 do local p=flat:getPixel(x,y);if a(p)>0 then colours[p]=true;assert(x>0 and y>0 and x<63 and y<63,'Clipped pose') end end end
end
local total=0;for _ in pairs(colours) do total=total+1 end
assert(total==12,'Unexpected new colours: '..total)
for _,tag in ipairs(polished.tags) do
  local before
  for _,t in ipairs(original.tags) do if t.name==tag.name then before=t end end
  assert(before and tag.fromFrame.frameNumber==before.fromFrame.frameNumber and tag.toFrame.frameNumber==before.toFrame.frameNumber)
  for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
    if tag.name=='Run' or tag.name=='ArmedRun' then
      local flat=Image(64,64,ColorMode.RGB);flat:drawSprite(polished,f)
      local baseline=false
      for x=20,43 do if a(flat:getPixel(x,47))>0 then baseline=true end end
      assert(baseline,'No planted foot at '..f)
    else assert(polished.frames[f].duration==original.frames[f].duration,'Changed expression/action timing') end
  end
end
local gunBefore=app.open(out..'Dino-Held-HMG-User-Before.aseprite')
local gun=app.open(root..'/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite')
equal(part(gunBefore,2,1),part(gun,2,1));equal(part(gunBefore,3,1),part(gun,3,1))
local armPixels=0
for _,i in ipairs({1,4}) do local im=part(gun,i,1)
  for y=0,gun.height-1 do for x=0,gun.width-1 do if a(im:getPixel(x,y))>0 then armPixels=armPixels+1 end end end
end
assert(armPixels<=20,'Arms too large')
-- Preview the same 16 PPU and .75 gun scale with its new scene anchor.
local dino=Image(64,64,ColorMode.RGB);dino:drawSprite(polished,43)
local gunImage=Image(40,24,ColorMode.RGB);gunImage:drawSprite(gun,1)
for y=20,47 do for x=20,55 do
  local sx=math.floor((x+.5-32+.12*16)/.75+8)
  local sy=math.floor((y+.5-48+.65*16)/.75+12)
  if sx>=0 and sy>=0 and sx<40 and sy<24 then local p=gunImage:getPixel(sx,sy);if a(p)>0 then dino:putPixel(x,y,p) end end
end end
local preview=Image(512,512,ColorMode.RGB)
for y=0,511 do for x=0,511 do preview:putPixel(x,y,dino:getPixel(math.floor(x/8),math.floor(y/8))) end end
preview:saveAs(out..'Dino-held-preview.png')
print('PASS: 57 frames, seven tags, six layers, 12 colours, all faces/eyes/tail pixels unchanged; action/laugh durations unchanged; planted run baseline; complete gun pixels unchanged; '..armPixels..' hand/arm pixels total.')
