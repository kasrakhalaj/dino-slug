local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local current=app.open('E:/Aseprite/MyDino-Evil-Animated.aseprite')
local previous=app.open('E:/Aseprite/MyDino-Refined-Animated.aseprite')
local sprite=Sprite(64,64,ColorMode.RGB); sprite:setPalette(current.palettes[1])
local layers={sprite.layers[1]}; layers[1].name=current.layers[1].name
for i=2,6 do layers[i]=sprite:newLayer(); layers[i].name=current.layers[i].name end
local count,ranges=0,{}
for _,state in ipairs({'Run','Jump','Hit','Death','Laugh','ArmedRun','ArmedJump'}) do
  local source=state=='Laugh' and previous or current
  local tag
  for _,candidate in ipairs(source.tags) do if candidate.name==state then tag=candidate end end
  assert(tag,'Missing tag '..state)
  local first=count+1
  for number=tag.fromFrame.frameNumber,tag.toFrame.frameNumber do
    count=count+1; local frame=count==1 and sprite.frames[1] or sprite:newEmptyFrame()
    frame.duration=source.frames[number].duration
    for i=1,6 do
      local cel=source.layers[i]:cel(number); local image=Image(64,64,ColorMode.RGB)
      if cel then image:drawImage(cel.image,cel.position) end
      sprite:newCel(layers[i],frame,image,Point(0,0))
    end
  end
  ranges[#ranges+1]={state,first,count}
end
for _,r in ipairs(ranges) do sprite:newTag(r[2],r[3]).name=r[1] end
sprite:saveAs(root .. '/Tools/SourceArt/MyDinoCombat/MyDino-Combat.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Combat.aseprite')
local fx=Image{fromFile=root .. '/Tools/SourceArt/MetalSlug/metal-slug-weapon-effects.png'}
Image(fx,Rectangle(0,2900,605,950)):saveAs(root .. '/Tools/SourceArt/MyDinoCombat/fx-inspection.png')
Image(fx,Rectangle(0,450,605,400)):saveAs(root .. '/Tools/SourceArt/MyDinoCombat/explosion-inspection.png')
print('Restored all 15 prior laugh frames and timing exactly; kept improved Run/ArmedRun and armed grin. '..count..' frames total.')
