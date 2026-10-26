-- Repair the tail/pelvis seam in the current saved document, not regenerated art.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/TailAndAudio/'
local sprite=app.open('E:/Aseprite/MyDino-Running.aseprite')
sprite:saveAs(out..'MyDino-Before-Tail.aseprite')
local rgba,a=app.pixelColor.rgba,app.pixelColor.rgbaA
local ink,shade,green=rgba(29,48,42,255),rgba(67,107,62,255),rgba(102,153,74,255)
local function part(i,f)
 local im=Image(64,64,ColorMode.RGB);local c=sprite.layers[i]:cel(f)
 if c then im:drawImage(c.image,c.position) end;return im
end
local function replace(i,f,im)
 local c=sprite.layers[i]:cel(f)
 if c then c.image=im;c.position=Point(0,0) else sprite:newCel(sprite.layers[i],f,im,Point(0,0)) end
end
local function opaque(im,x,y) return x>=0 and y>=0 and x<64 and y<64 and a(im:getPixel(x,y))>0 end
local function connection(tail,body)
 local overlap,neighbors=0,0
 for y=0,63 do for x=0,63 do if opaque(tail,x,y) then
  if opaque(body,x,y) then overlap=overlap+1 end
  if opaque(body,x-1,y) or opaque(body,x+1,y) or opaque(body,x,y-1) or opaque(body,x,y+1) then neighbors=neighbors+1 end
 end end end;return overlap,neighbors
end
local repaired=0
for f=1,#sprite.frames do
 local tail,body=part(1,f),part(3,f)
 local oldOverlap,oldNeighbors=connection(tail,body)
 local best=1e9;local ax,ay,bx,by
 for y=0,63 do for x=0,63 do if opaque(tail,x,y) then
  for yy=0,63 do for xx=0,63 do if opaque(body,xx,yy) then
   local d=(x-xx)^2+(y-yy)^2
   if d<best then best=d;ax,ay,bx,by=x,y,xx,yy end
  end end end
 end end end
 assert(ax,'Missing tail/body at '..f)
 local mask=Image(64,64,ColorMode.RGB)
 -- A thick root overlaps both parts, rather than a fragile diagonal pixel.
 for i=0,20 do local t=i/20;local cx,cy=ax+(bx-ax)*t,ay+(by-ay)*t
  for yy=math.floor(cy-2),math.ceil(cy+2) do for xx=math.floor(cx-2),math.ceil(cx+2) do
   if xx>=0 and yy>=0 and xx<64 and yy<64 and (xx-cx)^2+(yy-cy)^2<=4 then mask:putPixel(xx,yy,green) end
  end end
 end
 for y=0,63 do for x=0,63 do if opaque(mask,x,y) then
  local edge=false
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
   local xx,yy=x+d[1],y+d[2]
   if not opaque(mask,xx,yy) and not opaque(tail,xx,yy) and not opaque(body,xx,yy) then edge=true end
  end
  local c=edge and ink or (y>(ay+by)/2+1 and shade or green)
  tail:putPixel(x,y,c)
  -- Remove an internal black cut at the join while retaining the outside contour.
  if opaque(body,x,y) and body:getPixel(x,y)==ink and not edge then body:putPixel(x,y,green) end
 end end end
 local overlap,neighbors=connection(tail,body)
 assert(overlap>=3 and neighbors>=5,'Weak tail root at '..f)
 replace(1,f,tail);replace(3,f,body);repaired=repaired+1
 print(string.format('Frame %d: overlap %d -> %d, connected pixels %d -> %d',f,oldOverlap,overlap,oldNeighbors,neighbors))
end
sprite:saveAs(out..'MyDino-Tail-Fixed.aseprite')
sprite:saveAs(root..'/Tools/SourceArt/RunAndSoldiers/MyDino-Running.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Running.aseprite')
local sheet=Image(64*6,64*2,ColorMode.RGB)
for f=1,12 do local im=Image(64,64,ColorMode.RGB);im:drawSprite(sprite,f);sheet:drawImage(im,Point((f-1)%6*64,math.floor((f-1)/6)*64)) end
local zoom=Image(sheet.width*4,sheet.height*4,ColorMode.RGB)
for y=0,zoom.height-1 do for x=0,zoom.width-1 do zoom:putPixel(x,y,sheet:getPixel(math.floor(x/4),math.floor(y/4))) end end
zoom:saveAs(out..'Tail-join-poses.png')
print('Repaired '..repaired..' tail roots; native layers, faces and timings preserved.')
