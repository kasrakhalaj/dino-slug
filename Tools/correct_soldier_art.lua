local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Assets/Game/Art/MetalSlug/Combat/'
local src=root..'/Tools/SourceArt/MetalSlug/Enemies/'
local full=Image{fromFile=src..'rebel-full-source.png'}
local rifle=Image{fromFile=src..'rebel-rifle-source.png'}
local a=app.pixelColor.rgbaA
local function cut(image,x,y,w,h,key)
 local im=Image(image,Rectangle(x,y,w,h))
 if key then for yy=0,h-1 do for xx=0,w-1 do if im:getPixel(xx,yy)==key then im:putPixel(xx,yy,0) end end end end
 return im
end
local function pad(im,feet)
 local bottom=0
 for y=0,im.height-1 do for x=0,im.width-1 do if a(im:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end end end
 local anchor=im.width/2
 if feet then local l,r=im.width,0
  for y=math.max(0,bottom-4),bottom-1 do for x=0,im.width-1 do if a(im:getPixel(x,y))>0 then l=math.min(l,x);r=math.max(r,x) end end end
  anchor=(l+r)/2
 end
 local canvas=Image(80,64,ColorMode.RGB);canvas:drawImage(im,Point(math.floor(40-anchor+.5),60-bottom));return canvas
end
local function save(name,frames,tagName)
 local spr=Sprite(80,64,ColorMode.RGB);spr.layers[1].name='Original Metal Slug pixels - aligned body'
 for i,im in ipairs(frames) do
  im:saveAs(out..string.format('%s_%02d.png',name,i-1))
  local f=i==1 and spr.frames[1] or spr:newEmptyFrame();f.duration=.07;spr:newCel(spr.layers[1],f,im)
 end
 spr:newTag(1,#frames).name=tagName
 spr:saveAs(src..name..'.aseprite')
 return spr
end
local white=app.pixelColor.rgba(255,255,255,255)
local function columns(image,y,h,key)
 local ranges={};local start,active=0,false
 for x=0,image.width-1 do
  local opaque=false
  for yy=y,y+h-1 do local p=image:getPixel(x,yy);if a(p)>0 and p~=key then opaque=true;break end end
  if opaque and not active then start=x;active=true end
  if not opaque and active then ranges[#ranges+1]={start,x};active=false end
 end
 return ranges
end
local stand=columns(rifle,298,40,white)
local aim=pad(cut(rifle,stand[2][1],298,stand[2][2]-stand[2][1],40,white),true)
local fire2=pad(cut(rifle,stand[3][1],298,stand[3][2]-stand[3][1],40,white),true)
save('rifle_fire',{aim,fire2,aim,aim},'Fire')
local running=columns(rifle,44,37,white);assert(#running==12,'Expected twelve complete rifle approach frames')
local rifleWalk={}
for _,c in ipairs(running) do rifleWalk[#rifleWalk+1]=pad(cut(rifle,c[1],44,c[2]-c[1],37,white),true) end
save('rifle_walk',rifleWalk,'Run')
local minX,top,bottom=80,64,0
for y=0,63 do for x=0,79 do if a(aim:getPixel(x,y))>0 then minX=math.min(minX,x) end end end
for y=0,63 do if a(aim:getPixel(minX,y))>0 then top=math.min(top,y);bottom=math.max(bottom,y) end end
local mx,my=(minX-40)/24,(60-(top+bottom+1)/2)/24
local anchor=io.open(root..'/Tools/SourceArt/RunAndSoldiers/rifle-origin.json','w')
anchor:write(string.format('{"x":%.6f,"y":%.6f}',mx,my));anchor:close()
print('Measured complete rifle muzzle '..mx..','..my)
local death,walk={},{ }
for i=0,10 do
 local im=Image{fromFile=root..'/Assets/Game/Art/MetalSlug/Rebel/rebel_death_'..string.format('%02d',i)..'.png'}
 local canvas=Image(80,64,ColorMode.RGB);canvas:drawImage(im,Point(8,0));death[#death+1]=canvas
end
save('rifle_death',death,'Death');save('grenadier_death',death,'Death')
for i=0,11 do
 local im=Image{fromFile=root..'/Assets/Game/Art/MetalSlug/Rebel/rebel_walk_'..string.format('%02d',i)..'.png'}
 local canvas=Image(80,64,ColorMode.RGB);canvas:drawImage(im,Point(8,0));walk[#walk+1]=canvas
end
save('grenadier_walk',walk,'Walk')
-- Use the labelled THROWING GRENADE row, not the knife attack or stick-grenade row.
local key=full:getPixel(950,1950)
local row=cut(full,0,1930,610,42,key)
local columns={};local active=false;local first
for x=0,row.width-1 do
 local opaque=false;for y=0,row.height-1 do if a(row:getPixel(x,y))>0 then opaque=true;break end end
 if opaque and not active then first=x;active=true end
 if not opaque and active then columns[#columns+1]={first,x};active=false end
end
assert(#columns>=15,'Expected labelled grenade throw sequence, got '..#columns)
local throws={}
for i=1,15 do local c=columns[i];throws[i]=pad(cut(row,c[1],0,c[2]-c[1],row.height),true);print('Throw '..i..' x='..c[1]..'..'..c[2]) end
save('grenadier_throw',throws,'Throw')
-- The source's thrown grenade, rather than the oversized collectible grenade icon.
local ordnance=Image{fromFile=src..'grenadier-source.png'}
local grenade=cut(ordnance,8,15,16,19)
local canvas=Image(24,24,ColorMode.RGB);canvas:drawImage(grenade,Point(4,2));canvas:saveAs(out..'grenade.png')
local contact=Image(80*8,64*2,ColorMode.RGB)
for i,im in ipairs(throws) do contact:drawImage(im,Point((i-1)%8*80,math.floor((i-1)/8)*64)) end
local detail=Image(640,512,ColorMode.RGB)
for yy=0,511 do for xx=0,639 do detail:putPixel(xx,yy,throws[11]:getPixel(math.floor(xx/8),math.floor(yy/8))) end end
detail:saveAs(root..'/Tools/SourceArt/RunAndSoldiers/release-pose.png')
local zoom=Image(contact.width*3,contact.height*3,ColorMode.RGB)
for yy=0,zoom.height-1 do for xx=0,zoom.width-1 do zoom:putPixel(xx,yy,contact:getPixel(math.floor(xx/3),math.floor(yy/3))) end end
zoom:saveAs(root..'/Tools/SourceArt/RunAndSoldiers/Correct-throw.png')
print('Corrected shoulder-aim/fire, actual grenade throw and defeat sprites; all faces point left.')
dofile(root..'/Tools/package_correct_soldiers.lua')
