-- Preserve the user's contours and editable cels while replacing the rainbow palette.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local output=root .. '/Tools/SourceArt/MyDinoRefined/'
local rgba=app.pixelColor.rgba
local colours={
  {29,48,42}, {42,74,52}, {67,107,62}, {102,153,74}, {151,191,100},
  {139,151,83}, {198,203,128}, {233,229,171}, {130,73,59}, {190,111,67},
  {253,241,205}, {83,43,46}
}
local map={}
local function group(index,values)
  for _,c in ipairs(values) do map[rgba(c[1],c[2],c[3],255)]=rgba(table.unpack(colours[index])) end
end
group(1,{{25,49,109},{16,16,43},{65,2,95},{157,0,3}})
group(2,{{5,65,98},{67,73,143},{80,67,87},{99,52,132},{102,84,54},{129,46,96}})
group(3,{{62,153,65},{8,87,109},{24,131,106},{2,104,115},{11,113,150},{6,133,144},{121,163,61}})
group(4,{{138,199,62},{111,186,59}})
group(5,{{238,229,111},{210,220,85},{225,195,95},{247,151,56}})
group(6,{{45,182,169},{23,161,163},{154,142,208}})
group(7,{{39,214,177},{56,209,197},{78,236,193}})
group(8,{{127,244,202},{210,253,205}})
group(9,{{200,19,88},{199,15,35},{127,43,134},{193,0,122},{145,0,76}})
group(10,{{235,22,100},{221,1,140},{239,98,46},{223,97,88}})
group(11,{{254,255,255}})
local palette=Palette(#colours+1)
palette:setColor(0,Color{r=0,g=0,b=0,a=0})
for i,c in ipairs(colours) do palette:setColor(i,Color{r=c[1],g=c[2],b=c[3],a=255}) end
local function refine(image)
  local result=Image(image)
  for y=0,result.height-1 do
    for x=0,result.width-1 do
      local pixel=result:getPixel(x,y)
      if app.pixelColor.rgbaA(pixel)>0 then
        assert(map[pixel],'Unmapped original colour: '..pixel)
        result:putPixel(x,y,map[pixel])
      end
    end
  end
  return result
end
local sprite=app.open('E:/Aseprite/MyDino-Animated.aseprite')
assert(sprite.width==64 and sprite.height==64 and #sprite.frames==22)
sprite:saveAs(output .. 'MyDino-Before-Palette.aseprite')
for _,cel in ipairs(sprite.cels) do cel.image=refine(cel.image) end
sprite:setPalette(palette)
sprite:saveAs(output .. 'MyDino-Refined-Animated.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Refined-Animated.aseprite')
local original=Image{fromFile=root .. '/Tools/SourceArt/MyDinoAnimation/MyDino-Clean.png'}
local clean=refine(original)
clean:saveAs(output .. 'MyDino-Refined.png')
local pose=Sprite(32,32,ColorMode.RGB)
pose.layers[1].name='Dinosaur - refined palette'
pose:newCel(pose.layers[1],1,clean)
pose:setPalette(palette)
pose:saveAs(output .. 'MyDino-Refined.aseprite')
pose:saveAs('E:/Aseprite/MyDino-Refined.aseprite')
local comparison=Image(76,40,ColorMode.RGB)
comparison:drawImage(original,Point(2,4))
comparison:drawImage(clean,Point(42,4))
comparison:saveAs(output .. 'palette-comparison.png')
local file=io.open(output .. 'palette.txt','w')
for _,c in ipairs(colours) do file:write(string.format('#%02X%02X%02X\n',c[1],c[2],c[3])) end
file:close()
print('Refined 22 frames on six editable layers to a cohesive 12-colour palette; contours preserved.')
