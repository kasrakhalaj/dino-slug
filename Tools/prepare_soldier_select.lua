local root='E:/MyGames/HW1/DinoSlug/'
local source=root..'Tools/SourceArt/SelectionReference/'
local out=root..'Assets/Game/UI/SoldierSelect/'
local reference=app.open(source..'metal-slug-soldier-select-reference.png');app.command.ChangePixelFormat{format='rgb'}
local im=reference.cels[1].image
local function cut(x,y,w,h) return Image(im,Rectangle(x,y,w,h)) end
local function save(name,image)
 image:saveAs(out..name..'.png')
 local s=Sprite(image.width,image.height,ColorMode.RGB);s.layers[1].name='Metal Slug select UI / adapted for Dino';s:newCel(s.layers[1],1,image);s:saveAs(source..name..'.aseprite')
end
save('soldier_select_heading',cut(8,8,304,34))
save('player_one_marker',cut(39,43,37,26))
local shell=Image(320,224,ColorMode.RGB);shell:clear(app.pixelColor.rgba(0,0,0,255))
-- Preserve the original riveted outer metal, with all four portraits/names removed.
shell:drawImage(cut(8,42,12,161),Point(8,42));shell:drawImage(cut(296,42,16,161),Point(296,42))
for x=8,311 do for y=0,7 do shell:putPixel(x,203+y,im:getPixel(8+(x-8)%12,190+y)) end end
save('single_dino_shell',shell)
local frame=Image(112,132,ColorMode.RGB);frame:clear(app.pixelColor.rgba(3,5,2,255))
local dark=app.pixelColor.rgba(32,24,16,255);local light=app.pixelColor.rgba(120,104,64,255)
for y=0,131 do for x=0,111 do
 if x<5 or x>106 or y<5 or y>126 then frame:putPixel(x,y,im:getPixel(8+x%10,74+y%20)) end
 if x==4 or y==4 then frame:putPixel(x,y,light) end
 if x==107 or y==127 then frame:putPixel(x,y,dark) end
end end
save('portrait_frame',frame)
local plate=Image(112,22,ColorMode.RGB)
for y=0,21 do for x=0,111 do plate:putPixel(x,y,im:getPixel(33+x%3,198+y)) end end
plate:drawImage(cut(29,198,5,22),Point(0,0));plate:drawImage(cut(76,198,5,22),Point(107,0))
save('dino_nameplate',plate)
-- Portrait copies only; the user's canonical artwork and animation stay untouched.
for _,state in ipairs({'run','laugh'}) do
 local count=state=='run' and 12 or 16
 for i=0,count-1 do
  local name=string.format('mydino_%s_%02d',state,i)
  local s=app.open(root..'Assets/Game/Art/MyDino/'..name..'.png');app.command.ChangePixelFormat{format='rgb'}
  Image(s.cels[1].image,Rectangle(27,12,28,32)):saveAs(out..name..'_portrait.png')
 end
end
print('PASS: single-character native metal frame, original heading/P1 and 28 unchanged-pixel Dino portrait frames exported.')
