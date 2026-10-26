local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
for _,r in ipairs({{'top-background',0,0,1200,235},{'jungle-stage',0,850,2000,280},{'stage-ground',0,1010,1050,110}}) do
 local cut=Image(im,Rectangle(r[2],r[3],r[4],r[5]));cut:saveAs(root..r[1]..'-inspect.png')
end
print('Native mission 1 detail crops saved; sheet '..im.width..'x'..im.height)
