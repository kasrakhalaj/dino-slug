local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
Image(im,Rectangle(3900,0,140,180)):saveAs(root..'clean-watchtower-inspect.png')
Image(im,Rectangle(819,218,118,112)):saveAs(root..'clean-hut-inspect.png')
