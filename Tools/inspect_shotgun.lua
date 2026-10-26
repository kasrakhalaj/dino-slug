local root='E:/MyGames/HW1/DinoSlug/'
local fx=Image{fromFile=root..'Tools/SourceArt/MetalSlug/metal-slug-weapon-effects.png'}
Image(fx,Rectangle(0,3820,605,630)):saveAs(root..'Tools/SourceArt/MetalSlug/shotgun-inspection.png')
local doc=app.open(root..'Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite')
for _,layer in ipairs(doc.layers) do
  local cel=layer:cel(1)
  print(layer.name..' '..(cel and (cel.position.x..','..cel.position.y..' '..cel.image.width..'x'..cel.image.height) or 'none'))
end
