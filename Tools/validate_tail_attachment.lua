local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local before=app.open(root..'/Tools/SourceArt/TailAndAudio/MyDino-Before-Tail.aseprite')
local after=app.open('E:/Aseprite/MyDino-Running.aseprite')
local a=app.pixelColor.rgbaA
local function part(s,i,f) local im=Image(64,64,ColorMode.RGB);local c=s.layers[i]:cel(f);if c then im:drawImage(c.image,c.position) end;return im end
local function opaque(im,x,y) return x>=0 and y>=0 and x<64 and y<64 and a(im:getPixel(x,y))>0 end
local colours={};local minimum=1e9
assert(#after.frames==57 and #after.layers==6 and #after.tags==7)
for f=1,57 do
 assert(before.frames[f].duration==after.frames[f].duration,'Changed timing')
 for _,i in ipairs({2,4,5,6}) do
  local old,new=part(before,i,f),part(after,i,f)
  for y=0,63 do for x=0,63 do assert(old:getPixel(x,y)==new:getPixel(x,y),'Changed legs/face/eyes') end end
 end
 local tail,body=part(after,1,f),part(after,3,f);local overlap=0
 local oldBody=part(before,3,f)
 for y=0,63 do for x=0,63 do
  if oldBody:getPixel(x,y)~=body:getPixel(x,y) then assert(opaque(tail,x,y),'Body changed outside the tail join') end
 end end
 for y=0,63 do for x=0,63 do if opaque(tail,x,y) and opaque(body,x,y) then overlap=overlap+1 end end end
 assert(overlap>=3,'Detached tail '..f);minimum=math.min(minimum,overlap)
 local flat=Image(64,64,ColorMode.RGB);flat:drawSprite(after,f)
 for y=0,63 do for x=0,63 do local p=flat:getPixel(x,y);if a(p)>0 then colours[p]=true;assert(x>0 and x<63 and y>0 and y<63,'Clipping') end end end
end
local n=0;for _ in pairs(colours) do n=n+1 end
assert(n==12,'Changed palette')
print('PASS: all 57 tail roots overlap the body by at least '..minimum..' pixels; legs/faces/eyes and all durations unchanged; six layers, seven tags, 12 colors, no clipping.')
