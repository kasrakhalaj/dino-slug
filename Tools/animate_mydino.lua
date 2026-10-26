-- Native Aseprite authoring: remove the attached ground and animate the user's pixels.
local root = app.params.root or 'E:/MyGames/HW1/DinoSlug'
local output = root .. '/Tools/SourceArt/MyDinoAnimation/'
local game = root .. '/Assets/Game/Art/MyDino/'
local source = Image { fromFile = root .. '/Tools/SourceArt/MyDino/MyDino.png' }
assert(source.width == 32 and source.height == 32)
local rgba, alpha = app.pixelColor.rgba, app.pixelColor.rgbaA
local ground = rgba(80, 67, 87, 255)
local originalDocument = app.open(root .. '/Tools/SourceArt/MyDino/MyDino.aseprite')
local transparent = rgba(0, 0, 0, 0)
local removed = 0
for y = 28, 31 do
  for x = 0, 31 do
    if source:getPixel(x, y) == ground then
      source:putPixel(x, y, transparent)
      removed = removed + 1
    end
  end
end
assert(removed > 0, 'The attached ground pixels were not found')
source:saveAs(output .. 'MyDino-Clean.png')
source:saveAs(root .. '/Assets/Game/Art/my_dino.png')
local cleanDocument = Sprite(32, 32, ColorMode.RGB)
cleanDocument.layers[1].name = 'Dinosaur - ground removed'
cleanDocument:newCel(cleanDocument.layers[1], 1, source)
cleanDocument:setPalette(originalDocument.palettes[1])
cleanDocument:saveAs(output .. 'MyDino-Clean.aseprite')

local W, H, ox, oy, floorY = 64, 64, 16, 18, 48
local names = { 'Tail', 'Far leg', 'Body', 'Near leg', 'Head', 'Eyes' }
local base = {}
for i = 1, #names do base[i] = Image(W, H, ColorMode.RGB) end
local green, outline = rgba(138, 199, 62, 255), rgba(25, 49, 109, 255)
for y = 0, 31 do
  for x = 0, 31 do
    local pixel = source:getPixel(x, y)
    if alpha(pixel) > 0 then
      local layer = 3
      local near = (y == 24 and x >= 12 and x <= 16)
        or (y == 25 and x >= 12 and x <= 15)
        or (y == 26 and x >= 10 and x <= 14)
        or (y == 27 and x >= 10 and x <= 16)
        or (y == 28 and x >= 10 and x <= 18)
        or (y == 29 and x >= 12 and x <= 17)
      local far = (y == 24 and x >= 19)
        or (y >= 25 and y <= 27 and x >= 17)
        or (y == 28 and x >= 20)
      if y <= 11 then layer = 5
      elseif near then layer = 4
      elseif far then layer = 2
      elseif y >= 18 and x < 12 then layer = 1 end
      base[layer]:putPixel(x + ox, y + oy, pixel)
      -- Retain the hip attachment behind the swinging legs.
      if (layer == 2 or layer == 4) and y == 24 then
        base[3]:putPixel(x + ox, y + oy, pixel)
      end
      if y >= 3 and y <= 5 and x >= 16 and x <= 19 then
        base[5]:putPixel(x + ox, y + oy, green)
        base[6]:putPixel(x + ox, y + oy, pixel)
      end
    end
  end
end

local function blank() return Image(W, H, ColorMode.RGB) end
local function shifted(image, dx, dy)
  local result = blank()
  result:drawImage(image, Point(dx, dy))
  return result
end
local function rotated(image, degrees, px, py)
  local result = blank()
  local a = math.rad(degrees)
  local c, s = math.cos(a), math.sin(a)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local dx, dy = x + .5 - px, y + .5 - py
      local sx = math.floor(px + c * dx + s * dy)
      local sy = math.floor(py - s * dx + c * dy)
      if sx >= 0 and sy >= 0 and sx < W and sy < H then
        result:putPixel(x, y, image:getPixel(sx, sy))
      end
    end
  end
  return result
end
local function bottom(image)
  for y = H - 1, 0, -1 do
    for x = 0, W - 1 do
      if alpha(image:getPixel(x, y)) > 0 then return y + 1 end
    end
  end
  return 0
end
local function composite(parts)
  local result = blank()
  for _, part in ipairs(parts) do result:drawImage(part) end
  return result
end
local function leg(index, degrees, lift)
  local hipX = index == 4 and 30.5 or 35.5
  local result = rotated(base[index], degrees, hipX, 42.5)
  return shifted(result, 0, floorY - lift - bottom(result))
end
local function tail(sway)
  local result = blank()
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local pixel = base[1]:getPixel(x, y)
      if alpha(pixel) > 0 then
        local dy = math.floor(sway * math.max(0, 28 - x) / 10 + .5)
        result:putPixel(x, y + dy, pixel)
      end
    end
  end
  return result
end
local function eyes(expression)
  if expression == 'open' then return Image(base[6]) end
  local result = blank()
  if expression == 'closed' then
    for x = 32, 35 do result:putPixel(x, 22, outline) end
  else
    for _, point in ipairs({ {32,21}, {34,21}, {33,22}, {32,23}, {34,23} }) do
      result:putPixel(point[1], point[2], outline)
    end
  end
  return result
end
local function pose(nearAngle, farAngle, nearLift, farLift, bob, sway, expression)
  return {
    shifted(tail(sway), 0, bob), leg(2, farAngle, farLift),
    shifted(base[3], 0, bob), leg(4, nearAngle, nearLift),
    shifted(base[5], 0, bob), shifted(eyes(expression), 0, bob)
  }
end
local function wholePose(parts, degrees, dx, grounded)
  local result = {}
  for i, part in ipairs(parts) do result[i] = shifted(rotated(part, degrees, 32, floorY), dx, 0) end
  if grounded then
    local dy = floorY - bottom(composite(result))
    for i, part in ipairs(result) do result[i] = shifted(part, 0, dy) end
  end
  return result
end

local sprite = Sprite(W, H, ColorMode.RGB)
sprite:setPalette(originalDocument.palettes[1])
local layers = { sprite.layers[1] }
layers[1].name = names[1]
for i = 2, #names do layers[i] = sprite:newLayer(); layers[i].name = names[i] end
local entries, frameNumber = {}, 0
local function add(state, index, duration, parts)
  frameNumber = frameNumber + 1
  local frame = frameNumber == 1 and sprite.frames[1] or sprite:newEmptyFrame()
  frame.duration = duration / 1000
  for i, part in ipairs(parts) do sprite:newCel(layers[i], frame, part, Point(0, 0)) end
  local file = string.format('mydino_%s_%02d.png', string.lower(state), index)
  local full = composite(parts)
  -- Keep transparent padding for each pose: no cropped feet, head, or tail.
  for x = 0, W - 1 do
    assert(alpha(full:getPixel(x, 0)) == 0 and alpha(full:getPixel(x, H - 1)) == 0)
  end
  for y = 0, H - 1 do
    assert(alpha(full:getPixel(0, y)) == 0 and alpha(full:getPixel(W - 1, y)) == 0)
  end
  full:saveAs(game .. file)
  entries[#entries + 1] = string.format('{"state":"%s","frame":%d,"durationMs":%d,"file":"%s"}', state, index, duration, file)
end
local tagRanges = {}
local function tag(name, first)
  tagRanges[#tagRanges + 1] = { name = name, first = first, last = frameNumber }
end

local angles = { 25, 15, 0, -20, -25, -15, 0, 20 }
local lifts = { 0, 0, 0, 1, 3, 3, 1, 0 }
local bob = { 0, 0, -1, -1, 0, 0, -1, -1 }
local sway = { 0, 1, 2, 1, 0, -1, -2, -1 }
local first = 1
for i = 1, 8 do
  local other = (i + 3) % 8 + 1
  add('Run', i - 1, 83, pose(angles[i], angles[other], lifts[i], lifts[other], bob[i], sway[i], 'open'))
end
tag('Run', first)
first = frameNumber + 1
for i = 1, 3 do
  local p = pose(-15 - i * 12, 15 + i * 10, i + 1, i + 2, 0, -i, 'open')
  add('Jump', i - 1, 100, wholePose(p, -4 - i * 3, 0, false))
end
tag('Jump', first)
first = frameNumber + 1
for i = 1, 3 do
  local p = pose(-10, 10, 0, 1, 0, i - 2, i < 3 and 'closed' or 'open')
  add('Hit', i - 1, 83, wholePose(p, ({-10, 5, 0})[i], i == 1 and -1 or 0, true))
end
tag('Hit', first)
first = frameNumber + 1
for i, degrees in ipairs({ 0, 12, 28, 46, 65, 82, 90, 90 }) do
  local tuck = math.min(3, math.floor(i / 2))
  local p = pose(-tuck * 8, tuck * 8, tuck, tuck, 0, i == 7 and -1 or 0, i < 3 and 'closed' or 'dead')
  add('Death', i - 1, 125, wholePose(p, degrees, 0, true))
end
tag('Death', first)

-- Adding frames extends tags that end at the timeline boundary, so tag only now.
for _, range in ipairs(tagRanges) do
  local t = sprite:newTag(range.first, range.last)
  t.name = range.name
end
sprite:saveAs(output .. 'MyDino-Animated.aseprite')
sprite:saveAs('E:/Aseprite/MyDino-Animated.aseprite')
local metadata = io.open(output .. 'animation.json', 'w')
metadata:write('{"width":64,"height":64,"pixelsPerUnit":16,"pivotX":32,"pivotY":16,"removedGroundPixels":' .. removed .. ',"frames":[' .. table.concat(entries, ',') .. ']}')
metadata:close()
print('Created ' .. frameNumber .. ' animation frames on ' .. #layers .. ' editable layers; removed ' .. removed .. ' ground pixels.')
