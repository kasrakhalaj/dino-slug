local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/'
local function cut(file,name,r)
 local s=app.open(root..file);app.command.ChangePixelFormat{format='rgb'}
 Image(s.cels[1].image,Rectangle(r[1],r[2],r[3],r[4])):saveAs(root..name..'.png')
 print(file..' '..s.width..'x'..s.height)
end
cut('Enemies/biker-source.png','Enemies/biker-detail',{0,0,545,590})
cut('Enemies/flying-tara-source.png','Enemies/tara-detail',{0,20,334,430})
cut('Terrain/mission-1-source.png','Terrain/coast-detail',{780,876,1050,240})
cut('Terrain/mission-1-source.png','Terrain/waterfall-detail',{25,430,360,240})
