local base='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Reinforcements/'
for _,n in ipairs({'bazooka','shield'}) do
 local s=Sprite{fromFile=base..n..'-source.png'}; local im=s.cels[1].image; local white=app.pixelColor.rgba(255,255,255,255)
 local first=nil
 for y=0,760 do local on=false; for x=0,im.width-1 do local p=im:getPixel(x,y);if p~=white and app.pixelColor.rgbaA(p)>0 then on=true;break end end
 if on and not first then first=y end
 if not on and first then print(n..' band '..first..' '..(y-1));first=nil end end
end
