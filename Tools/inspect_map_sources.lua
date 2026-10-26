local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local one=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'}
Image(one.cels[1].image,Rectangle(240,876,560,182)):saveAs(root..'ruins-map-inspect.png')
local two=app.open(root..'mission-2-source.png');app.command.ChangePixelFormat{format='rgb'}
local im=two.cels[1].image
for _,r in ipairs({{'harbor-map',0,295,1200,265},{'town-map',1560,840,580,425},{'works-map',3660,0,480,300},{'bridge-map',1020,565,1550,275},{'night-map',0,840,1570,300}}) do
 Image(im,Rectangle(r[2],r[3],r[4],r[5])):saveAs(root..r[1]..'-inspect.png')
end
print('Native map source detail crops saved.')
