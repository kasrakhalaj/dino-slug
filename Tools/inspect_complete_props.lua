local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
Image(im,Rectangle(810,216,440,126)):saveAs(root..'small-hut-detail.png')
Image(im,Rectangle(2750,0,1400,210)):saveAs(root..'upper-props-detail.png')
