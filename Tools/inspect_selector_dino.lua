local root='E:/MyGames/HW1/DinoSlug/'
for _,name in ipairs({'mydino_run_00','mydino_laugh_04'}) do
 local s=app.open(root..'Assets/Game/Art/MyDino/'..name..'.png');app.command.ChangePixelFormat{format='rgb'}
 local im=s.cels[1].image;local big=Image(512,512,ColorMode.RGB)
 for y=0,511 do for x=0,511 do big:putPixel(x,y,im:getPixel(math.floor(x/8),math.floor(y/8))) end end
 big:saveAs(root..'Tools/SourceArt/SelectionReference/'..name..'-inspect.png')
end
