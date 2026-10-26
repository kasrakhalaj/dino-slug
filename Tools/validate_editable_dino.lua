local spr = app.activeSprite
assert(spr and #spr.layers == 5 and #spr.frames == 15)
assert(spr.colorMode == ColorMode.RGB)
assert(#spr.tags == 4)
for _, layer in ipairs(spr.layers) do
  assert(layer.isEditable and layer.isVisible, layer.name .. ' is not editable')
  assert(#layer.cels == 15)
end
for layerIndex = 2, 5 do
  local layer = spr.layers[layerIndex]
  local first = layer:cel(1).image
  for frame = 2, 10 do
    assert(layer:cel(frame).image.id == first.id, 'Repeated heads are not linked')
  end
  local old = first:getPixel(0, 0)
  first:putPixel(0, 0, app.pixelColor.rgba(255, 0, 255, 255))
  assert(layer:cel(7).image:getPixel(0, 0) == first:getPixel(0, 0), 'Edit did not reach Jump')
  assert(layer:cel(9).image:getPixel(0, 0) == first:getPixel(0, 0), 'Edit did not reach Hit')
  first:putPixel(0, 0, old)
end
local head = app.open('E:/Aseprite/DinoSlug-Head-Editable.aseprite')
assert(head.width == 32 and head.height == 32 and #head.layers == 4)
assert(head.colorMode == ColorMode.RGB)
local colors = {}
for _, layer in ipairs(head.layers) do
  assert(layer.isEditable and layer.isVisible)
  local count = 0
  for pixel in layer:cel(1).image:pixels() do
    local value = pixel()
    local alpha = app.pixelColor.rgbaA(value)
    assert(alpha == 0 or alpha == 255, 'Partial transparency remains')
    if alpha > 0 then
      colors[value] = true
      count = count + 1
    end
  end
  assert(count > 0, layer.name .. ' is empty')
  print(layer.name .. ': ' .. count .. ' visible pixels; editable')
end
local colorCount = 0
for _ in pairs(colors) do colorCount = colorCount + 1 end
assert(colorCount <= 28)
print('PASS: ' .. colorCount .. ' opaque colors; 15 full-dino frames; four tags; linked head edits verified without saving test changes.')
