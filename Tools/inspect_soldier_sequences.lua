local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local out=root..'/Tools/SourceArt/RunAndSoldiers/'
local function zoom(image,n)
  local result=Image(image.width*n,image.height*n,ColorMode.RGB)
  for y=0,result.height-1 do for x=0,result.width-1 do result:putPixel(x,y,image:getPixel(math.floor(x/n),math.floor(y/n))) end end
  return result
end
local rifle=Image{fromFile=root..'/Tools/SourceArt/MetalSlug/Enemies/rebel-rifle-source.png'}
local white=app.pixelColor.rgba(255,255,255,255)
local active,start=false,0
for x=0,505 do
 local opaque=false
 for y=298,337 do local p=rifle:getPixel(x,y);if app.pixelColor.rgbaA(p)>0 and p~=white then opaque=true;break end end
 if opaque and not active then start=x;active=true end
 if not opaque and active then print('rifleAim '..start..' '..x);active=false end
end
for i,range in ipairs({{0,0,506,128},{0,475,506,180},{0,650,506,180},{0,290,506,150}}) do
  zoom(Image(rifle,Rectangle(table.unpack(range))),3):saveAs(out..'rifle-band-'..i..'.png')
end
local rebel=Image{fromFile=root..'/Tools/SourceArt/MetalSlug/Enemies/rebel-sprites.png'}
local row=369
local inRun=false;local start=0
for x=0,814 do
 local opaque=false
 for y=365,413 do if app.pixelColor.rgbaA(rebel:getPixel(x,y))>0 then opaque=true;break end end
 if opaque and not inRun then start=x;inRun=true end
 if not opaque and inRun then print('throw '..start..' '..x);inRun=false end
end
for i,range in ipairs({{0,225,815,55},{0,355,815,65}}) do
  zoom(Image(rebel,Rectangle(table.unpack(range))),3):saveAs(out..'rebel-band-'..i..'.png')
end
zoom(Image(rebel,Rectangle(13,92,24,45)),10):saveAs(out..'rebel-facing.png')
zoom(Image(rebel,Rectangle(308,365,43,49)),10):saveAs(out..'throw-release.png')
local full=Image{fromFile=root..'/Tools/SourceArt/MetalSlug/Enemies/rebel-full-source.png'}
Image(full,Rectangle(0,0,960,1800)):saveAs(out..'infantry-top-left.png')
Image(full,Rectangle(980,0,960,1800)):saveAs(out..'infantry-top-right.png')
Image(full,Rectangle(0,1800,960,1800)):saveAs(out..'infantry-mid-left.png')
zoom(Image(full,Rectangle(980,820,960,110)),2):saveAs(out..'actual-throw-row.png')
for _,i in ipairs({8,9}) do
 local im=Image{fromFile=root..'/Assets/Game/Art/MetalSlug/Combat/grenadier_throw_'..string.format('%02d',i)..'.png'}
 zoom(im,8):saveAs(out..'release-'..i..'.png')
end
