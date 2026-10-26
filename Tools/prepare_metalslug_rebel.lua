-- Extract original walking and defeat poses into aligned, editable native frames.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source=root .. '/Tools/SourceArt/MetalSlug/Enemies/'
local output=root .. '/Assets/Game/Art/MetalSlug/Rebel/'
local image=Image{fromFile=source .. 'rebel-sprites.png'}
local sprite=Sprite(64,64,ColorMode.RGB)
sprite.layers[1].name='Rebel soldier - original pixels'
local ranges={
  {name='Walk',y0=92,y1=137,columns={{13,33},{47,67},{78,101},{109,135},{145,169},{181,203},{216,237},{250,271},{283,305},{314,339},{351,373},{388,407}},duration=125},
  {name='Death',y0=298,y1=343,columns={{7,33},{42,67},{77,101},{111,135},{142,169},{176,203},{210,237},{244,271},{279,305},{311,339},{344,373}},duration=83}
}
local number,tags=0,{}
for _,range in ipairs(ranges) do
  local first=number+1
  for index,column in ipairs(range.columns) do
    local bottom=range.y0
    for y=range.y0,range.y1-1 do
      for x=column[1],column[2]-1 do
        if app.pixelColor.rgbaA(image:getPixel(x,y))>0 then bottom=math.max(bottom,y+1) end
      end
    end
    local part=Image(image,Rectangle(column[1],range.y0,column[2]-column[1],range.y1-range.y0))
    local frameImage=Image(64,64,ColorMode.RGB)
    frameImage:drawImage(part,Point(32-math.floor((column[2]-column[1])/2),60-bottom+range.y0))
    frameImage:saveAs(output .. string.format('rebel_%s_%02d.png',string.lower(range.name),index-1))
    number=number+1
    local frame=number==1 and sprite.frames[1] or sprite:newEmptyFrame()
    frame.duration=range.duration/1000
    sprite:newCel(sprite.layers[1],frame,frameImage)
  end
  tags[#tags+1]={range.name,first,number}
end
for _,range in ipairs(tags) do sprite:newTag(range[2],range[3]).name=range[1] end
sprite:saveAs(source .. 'Rebel-Soldier.aseprite')
print('Prepared 12 walking and 11 defeat frames for the Rebel soldier.')
