-- Export saved user gun cels without regenerating stock, barrel or hands.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source=app.params.source or root..'/Tools/SourceArt/MetalSlug/Dino-Held-HMG.aseprite'
local sprite=app.open(source)
assert(sprite and sprite.width==40 and sprite.height==24,'Keep the held HMG canvas at 40x24')
local flat=Image(40,24,ColorMode.RGB);flat:drawSprite(sprite,1)
flat:saveAs(root..'/Assets/Game/Art/MetalSlug/Combat/hmg_held.png')
print('Exported the saved held HMG without changing the editable source.')
