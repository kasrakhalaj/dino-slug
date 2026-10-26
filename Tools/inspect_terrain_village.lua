local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Terrain/'
local s=app.open(root..'mission-1-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
local cut=Image(im,Rectangle(1970,872,1450,245));cut:saveAs(root..'village-detail.png')
local white=app.pixelColor.rgba(255,255,255,255)
local first=nil
for x=0,330 do
 local on=im:getPixel(x,100)~=white
 if on and not first then first=x end
 if not on and first then print('Background row100 x='..first..'..'..(x-1));first=nil end
end
for y=0,230 do
 local on=im:getPixel(20,y)~=white
 if on and not first then first=y end
 if not on and first then print('Background col20 y='..first..'..'..(y-1));first=nil end
end
