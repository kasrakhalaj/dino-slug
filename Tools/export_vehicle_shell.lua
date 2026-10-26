-- Re-export only the tank projectile, using the source bank's actual background key.
local root='E:/MyGames/HW1/DinoSlug/'
local source=app.open(root..'Tools/SourceArt/MetalSlug/Vehicles/di-cokka-source.png')
app.command.ChangePixelFormat{format='rgb'}
local image=source.cels[1].image
local shell=Image(image,Rectangle(2,809,13,23))
local key=image:getPixel(2,809)
local removed,preserved=0,0
for y=0,shell.height-1 do for x=0,shell.width-1 do
 if shell:getPixel(x,y)==key then shell:putPixel(x,y,0);removed=removed+1 else preserved=preserved+1 end
end end
local path=root..'Assets/Game/Art/MetalSlug/Vehicles/tank_shell.png'
shell:saveAs(path)
local check=Image{fromFile=path}
for y=0,check.height-1 do for x=0,check.width-1 do
 local original=image:getPixel(x+2,y+809);local current=check:getPixel(x,y)
 if original==key then assert(app.pixelColor.rgbaA(current)==0,'Opaque background remains')
 else assert(original==current,'Changed original projectile pixel') end
end end
assert(removed>0 and preserved>0)
print('PASS: removed '..removed..' source-key pixels; preserved all '..preserved..' original projectile pixels; transparent PNG verified.')
