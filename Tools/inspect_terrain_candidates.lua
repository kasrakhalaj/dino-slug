local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
for i,r in ipairs({{75,1058,256,58},{200,1058,256,58},{0,876,820,182}}) do local piece=Image(im,Rectangle(r[1],r[2],r[3],r[4]));piece:saveAs(root..'candidate-'..i..'.png') end
