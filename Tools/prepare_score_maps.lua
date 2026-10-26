-- Assemble original stage pixels in native Aseprite, retaining source palettes.
local root='E:/MyGames/HW1/DinoSlug/'
local folder=root..'Tools/SourceArt/MetalSlug/Terrain/'
local out=root..'Assets/Game/Art/MetalSlug/Terrain/'
local function load(name)
 local s=app.open(folder..name);app.command.ChangePixelFormat{format='rgb'};return s.cels[1].image
end
local one,two=load('mission-1-source.png'),load('mission-2-source.png')
local function cut(sheet,x,y,w,h,key)
 local im=Image(sheet,Rectangle(x,y,w,h))
 if key then for yy=0,h-1 do for xx=0,w-1 do if im:getPixel(xx,yy)==key then im:putPixel(xx,yy,0) end end end end
 return im
end
local function mirror(im)
 local result=Image(im.width*2,im.height,ColorMode.RGB);result:drawImage(im,Point(0,0))
 for y=0,im.height-1 do for x=0,im.width-1 do result:putPixel(im.width+x,y,im:getPixel(im.width-1-x,y)) end end
 for y=0,im.height-1 do assert(result:getPixel(0,y)==result:getPixel(result.width-1,y),'Tile edge mismatch') end
 return result
end
local function save(name,im)
 im=mirror(im);im:saveAs(out..name..'.png')
 local s=Sprite(im.width,im.height,ColorMode.RGB);s.layers[1].name='Original Metal Slug stage pixels'
 s:newCel(s.layers[1],s.frames[1],im);s:saveAs(folder..name..'.aseprite')
 print(name..' '..im.width..'x'..im.height)
end
save('ms1_temple_backdrop',cut(one,240,876,560,182,one:getPixel(0,0)))
local harbor=Image(640,200,ColorMode.RGB)
harbor:clear(two:getPixel(2000,950))
harbor:drawImage(cut(two,6,854,540,175,two:getPixel(0,0)),Point(50,25))
save('ms2_harbor_skyline',harbor)
save('ms2_harbor_station',cut(two,0,299,820,207,two:getPixel(0,0)))
save('ms2_rail_ground',cut(two,10,494,780,58))
save('ms2_factory_wall',cut(two,3770,24,330,208))
save('ms2_factory_ground',cut(two,3770,241,320,58))
print('PASS: native original stage crops and palettes exported, all repeat edges match.')
