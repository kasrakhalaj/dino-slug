local root='E:/MyGames/HW1/DinoSlug/'
local function inspect(name,path,x,y)
 local s=app.open(root..path);app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
 local function c(p)return string.format('(%d,%d,%d,%d)',app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p),app.pixelColor.rgbaA(p))end
 print(name..' '..im.width..'x'..im.height..' sample '..c(im:getPixel(x,y)))
 if name=='shell' then
 local counts={};for yy=0,im.height-1 do for xx=0,im.width-1 do local p=im:getPixel(xx,yy);if app.pixelColor.rgbaA(p)>0 then counts[p]=(counts[p] or 0)+1 end end end
 for p,n in pairs(counts) do if n>4 then print('opaque '..c(p)..' count '..n)end end
 end
end
inspect('tank bank','Tools/SourceArt/MetalSlug/Vehicles/di-cokka-source.png',2,809)
inspect('shell','Assets/Game/Art/MetalSlug/Vehicles/tank_shell.png',0,0)
inspect('heli bomb','Assets/Game/Art/MetalSlug/Vehicles/heli_bomb.png',0,0)
