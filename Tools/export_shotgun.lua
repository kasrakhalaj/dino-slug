-- Export saved edits only; never recreate the user's gun or hands.
local root=app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source=app.params.source or 'E:/Aseprite/Dino-Held-Shotgun.aseprite'
local sprite=app.open(source)
assert(sprite and sprite.width==40 and sprite.height==24,'Keep the shotgun canvas at 40x24')
local flat=Image(40,24,ColorMode.RGB);flat:drawSprite(sprite,1)
flat:saveAs(root..'/Assets/Game/Art/MetalSlug/Shotgun/shotgun_held.png')
print('Exported saved shotgun edits without changing its native layers.')
