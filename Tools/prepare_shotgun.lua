-- Native sibling of the user's held HMG; its source pixels are never overwritten.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local src=root..'/Tools/SourceArt/MetalSlug/'
local out=root..'/Assets/Game/Art/MetalSlug/Shotgun/'
app.fs.makeAllDirectories(out)
local rgba=app.pixelColor.rgba
local function c(hex) return rgba(tonumber(hex:sub(1,2),16),tonumber(hex:sub(3,4),16),tonumber(hex:sub(5,6),16),255) end
local function rect(im,x,y,w,h,col) for yy=y,y+h-1 do for xx=x,x+w-1 do im:putPixel(xx,yy,col) end end end
local hmg=app.open(src..'Dino-Held-HMG.aseprite')
local sg=Sprite(40,24,ColorMode.RGB)
sg:deleteLayer(sg.layers[1])
local function layer(name,im,pos) local l=sg:newLayer();l.name=name;sg:newCel(l,1,im,pos or Point(0,0));return l end
for _,l in ipairs(hmg.layers) do
  if l.name=='Far arm' then local cel=l:cel(1);layer('Far tiny hand',Image(cel.image),cel.position) end
end
local stock=Image(40,24,ColorMode.RGB)
rect(stock,6,7,11,6,c('241E1B'));rect(stock,7,8,9,4,c('775033'));rect(stock,7,8,8,1,c('B8874A'))
rect(stock,6,11,4,4,c('241E1B'));rect(stock,7,11,3,3,c('775033'))
layer('Wood stock',stock)
local metal=Image(40,24,ColorMode.RGB)
rect(metal,15,6,12,6,c('1D211F'));rect(metal,16,7,10,4,c('65685D'));rect(metal,16,7,10,1,c('B4B5A1'))
rect(metal,25,6,9,3,c('1D211F'));rect(metal,26,7,7,1,c('C3C4AE'))
rect(metal,24,9,9,2,c('1D211F'));rect(metal,25,9,7,1,c('70796D'))
rect(metal,23,10,8,4,c('241E1B'));rect(metal,24,11,6,2,c('976537'))
for x=25,29,2 do rect(metal,x,11,1,2,c('4C3829')) end
rect(metal,17,12,4,4,c('1D211F'));rect(metal,18,13,2,2,c('8A917E'))
layer('Receiver barrel and pump',metal)
for _,l in ipairs(hmg.layers) do
  if l.name=='Near arm and trigger hand' then local cel=l:cel(1);layer('Near tiny hand',Image(cel.image),cel.position) end
end
sg:saveAs(src..'Dino-Held-Shotgun.aseprite')
local flat=Image(40,24,ColorMode.RGB);flat:drawSprite(sg,1);flat:saveAs(out..'shotgun_held.png')
local zoom=Image(240,144,ColorMode.RGB)
for y=0,143 do for x=0,239 do zoom:putPixel(x,y,flat:getPixel(x//6,y//6)) end end
zoom:saveAs(src..'shotgun-held-preview.png')
local pickup=Sprite(26,25,ColorMode.RGB);pickup.layers[1].name='Original S pickup'
for i=0,3 do
  local f=i==0 and pickup.frames[1] or pickup:newEmptyFrame();f.duration=.12
  local im=Image{fromFile=src..'Prepared/s_pickup_'..i..'.png'}
  pickup:newCel(pickup.layers[1],f,im);im:saveAs(out..'s_pickup_'..i..'.png')
end
pickup:newTag(1,4).name='S pickup';pickup:saveAs(src..'S-Pickup.aseprite')
local fx=Image{fromFile=src..'metal-slug-weapon-effects.png'}
Image(fx,Rectangle(0,4260,605,230)):saveAs(src..'shotgun-normal-inspection.png')
local blast=Sprite(128,96,ColorMode.RGB);blast.layers[1].name='Original normal Shotgun SFX'
local cells={{0,4286,75,69},{75,4286,78,69},{153,4286,87,69},{240,4286,89,69},{329,4286,96,69},{425,4286,99,69},
  {0,4355,106,78},{106,4355,79,78},{185,4355,75,78},{260,4355,74,78},{334,4355,67,78},{401,4355,58,78}}
for i,b in ipairs(cells) do
  local f=i==1 and blast.frames[1] or blast:newEmptyFrame();f.duration=.02
  local cell=Image(fx,Rectangle(b[1],b[2],b[3],b[4]))
  local key=fx:getPixel(604,4320)
  for y=0,cell.height-1 do for x=0,cell.width-1 do if cell:getPixel(x,y)==key then cell:putPixel(x,y,0) end end end
  local frame=Image(128,96,ColorMode.RGB)
  frame:drawImage(cell,Point(0,i<=6 and 14 or 10))
  blast:newCel(blast.layers[1],f,frame)
  frame:saveAs(out..'shotgun_blast_'..string.format('%02d',i-1)..'.png')
end
blast:newTag(1,12).name='Blast';blast:saveAs(src..'Shotgun-Blast.aseprite')
-- Warning is a simple native pixel glyph, with a dark silhouette at game scale.
local warning=Sprite(9,14,ColorMode.RGB);warning.layers[1].name='Aim warning'
local im=Image(9,14,ColorMode.RGB)
rect(im,2,0,5,9,c('251E22'));rect(im,3,1,3,6,c('FFFFFF'))
rect(im,2,10,5,4,c('251E22'));rect(im,3,11,3,2,c('FFFFFF'))
warning:newCel(warning.layers[1],1,im);warning:saveAs(src..'Rifle-Warning.aseprite');im:saveAs(out..'rifle_warning.png')
print('Prepared editable S pickup, held shotgun with tiny hands, and rifle warning.')
