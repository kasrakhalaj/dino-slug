local root='E:/MyGames/HW1/DinoSlug'
local sprite=app.open(root..'/Tools/SourceArt/ClaudeTailPolish/MyDino-Running.aseprite')
assert(sprite.width==64 and sprite.height==64 and #sprite.frames==57 and #sprite.layers==6 and #sprite.tags==7,'Unexpected native structure')
local manifest=json.decode(io.open(root..'/Tools/SourceArt/MyDinoAnimation/animation.json'):read('*a'))
local tags={};for _,tag in ipairs(sprite.tags) do tags[tag.name]=tag end
local checked=0
for _,entry in ipairs(manifest.frames) do
 local tag=assert(tags[entry.state]);local number=tag.fromFrame.frameNumber+entry.frame
 assert(number<=tag.toFrame.frameNumber,'Frame outside tag')
 assert(math.floor(sprite.frames[number].duration*1000+.5)==entry.durationMs,'Changed timing '..number)
 local flat=Image(64,64,ColorMode.RGB);flat:drawSprite(sprite,number)
 local exported=Image{fromFile=root..'/Assets/Game/Art/MyDino/'..entry.file}
 for y=0,63 do for x=0,63 do
  local a,b=flat:getPixel(x,y),exported:getPixel(x,y)
  assert(a==b or (app.pixelColor.rgbaA(a)==0 and app.pixelColor.rgbaA(b)==0),'Native/PNG mismatch '..entry.file..' '..x..','..y)
 end end
 checked=checked+1
end
print('PASS: '..checked..' native frames exactly match game PNGs; 64x64, six layers, seven tags and all authored durations verified. No source edits.')
