local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'prepared-bg3.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
local p=im:getPixel(0,0);print('Near layer '..im.width..'x'..im.height..' top-left alpha='..app.pixelColor.rgbaA(p))
