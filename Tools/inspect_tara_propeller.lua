local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Enemies/'
local s=app.open(root..'flying-tara-source.png');app.command.ChangePixelFormat{format='rgb'}
Image(s.cels[1].image,Rectangle(0,1300,180,120)):saveAs(root..'tara-propeller-detail.png')
