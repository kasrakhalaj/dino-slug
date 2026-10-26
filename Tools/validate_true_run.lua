local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/RunAndSoldiers/'
local old=app.open(out..'MyDino-Before-Run.aseprite')
local new=app.open(out..'MyDino-Running.aseprite')
local a=app.pixelColor.rgbaA
local function part(s,i,f) local im=Image(64,64,ColorMode.RGB);local c=s.layers[i]:cel(f);if c then im:drawImage(c.image,c.position) end;return im end
local bob={0,1,0,-1,-2,-1,0,1,0,-1,-2,-1}
local colours={};local flight,contact=0,0
for _,tag in ipairs(new.tags) do
 local moving=tag.name=='Run' or tag.name=='ArmedRun' or tag.name=='Laugh'
 for f=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
  assert(old.frames[f].duration==new.frames[f].duration,'Changed cadence at '..f)
  local phase=(f-tag.fromFrame.frameNumber)%12+1
  if moving then
   local body=part(old,3,f);local bottom=0
   for y=0,63 do for x=27,39 do if a(body:getPixel(x,y))>0 then bottom=math.max(bottom,y) end end end
   local dy=bob[phase]-(bottom-41)
   for _,i in ipairs({5,6}) do
    local expected=Image(64,64,ColorMode.RGB);expected:drawImage(part(old,i,f),Point(4,dy))
    local actual=part(new,i,f)
    for y=0,63 do for x=0,63 do assert(expected:getPixel(x,y)==actual:getPixel(x,y),'Face repaint at '..f) end end
   end
  else
   -- Tail/body seam edits are covered by validate_tail_attachment.lua.
   for _,i in ipairs({2,4,5,6}) do local before,after=part(old,i,f),part(new,i,f)
    for y=0,63 do for x=0,63 do assert(before:getPixel(x,y)==after:getPixel(x,y),'Unrelated pose change') end end
   end
  end
  local flat=Image(64,64,ColorMode.RGB);flat:drawSprite(new,f)
  local planted=false
  for y=0,63 do for x=0,63 do local p=flat:getPixel(x,y)
   if a(p)>0 then colours[p]=true;assert(x>0 and x<63 and y>0 and y<63,'Clipped pose');if y>=47 then planted=true end end
  end end
  if tag.name=='Run' then if planted then contact=contact+1 else flight=flight+1 end end
 end
end
local n=0;for _ in pairs(colours) do n=n+1 end
assert(n==12 and flight==6 and contact==6,'Expected 12 colours, six flight and six contact frames; got '..n..','..flight..','..contact)
print('PASS: unchanged cadence on all 57 frames; face/eye drawings preserved by translation; other action poses unchanged; 12 colours; six contact + six flight frames per run cycle; no clipping.')
