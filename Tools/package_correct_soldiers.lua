local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local folder=root..'/Tools/SourceArt/MetalSlug/Enemies/'
local function combine(name,ranges)
 local sprite=Sprite(80,64,ColorMode.RGB);sprite.layers[1].name='Original Metal Slug pixels - corrected poses'
 local count=0;local tags={}
 for _,range in ipairs(ranges) do
  local first=count+1
  for i=0,range[3]-1 do
   local im=Image{fromFile=root..'/Assets/Game/Art/MetalSlug/Combat/'..range[2]..string.format('%02d',i)..'.png'}
   count=count+1;local frame=count==1 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=range[4]/1000
   sprite:newCel(sprite.layers[1],frame,im)
  end
  tags[#tags+1]={range[1],first,count}
 end
 for _,tag in ipairs(tags) do sprite:newTag(tag[2],tag[3]).name=tag[1] end
 sprite:saveAs(folder..name..'-Combat.aseprite')
 print('Saved corrected '..name..' native document with '..count..' frames.')
end
combine('rifle',{{'Run','rifle_walk_',12,100},{'Fire','rifle_fire_',4,62.5},{'Death','rifle_death_',11,83}})
combine('grenadier',{{'Walk','grenadier_walk_',12,100},{'Throw','grenadier_throw_',15,70},{'Death','grenadier_death_',11,83}})
