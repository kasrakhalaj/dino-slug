local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
Image(im,Rectangle(0,218,1220,180)):saveAs(root..'isolated-huts-inspect.png')
