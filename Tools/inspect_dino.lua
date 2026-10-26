local spr = app.activeSprite
assert(spr, 'Dino document did not load')
print('Frames: ' .. #spr.frames)
for _, layer in ipairs(spr.layers) do
  print(layer.name .. ' editable=' .. tostring(layer.isEditable) .. ' visible=' .. tostring(layer.isVisible))
  for _, cel in ipairs(layer.cels) do
    print('  frame=' .. cel.frameNumber .. ' image=' .. cel.image.id)
  end
end
