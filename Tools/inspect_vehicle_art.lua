local root='E:/MyGames/HW1/DinoSlug/Tools/SourceArt/MetalSlug/Vehicles/'
for _,n in ipairs({'di-cokka','r-shobu'}) do
 local s=app.open(root..n..'-source.png');app.command.ChangePixelFormat{format='rgb'};local im=s.cels[1].image
 local key=im:getPixel(0,0);local start=nil
 local limit=n=='di-cokka' and 789 or 78
 for y=0,limit do
  local on=false
  for x=0,math.min(im.width-1,n=='di-cokka' and 445 or 680) do local p=im:getPixel(x,y);if p~=key and app.pixelColor.rgbaA(p)>0 then on=true;break end end
  if on and not start then start=y end
  if not on and start then print(n..' band '..start..' '..(y-1));start=nil end
 end
end
