-- Re-export user edits without regenerating or repainting the authored animation.
local root = app.params.root or 'E:/MyGames/HW1/DinoSlug'
local source = app.params.source or 'E:/Aseprite/MyDino-Running.aseprite'
local sprite = app.open(source)
assert(sprite and sprite.width == 64 and sprite.height == 64, 'Expected the 64x64 MyDino animation document')
local tags = {}
for _, tag in ipairs(sprite.tags) do tags[tag.name] = tag end
local usedFrames = {}
for _, state in ipairs({ 'Run', 'Jump', 'Hit', 'Death', 'Laugh', 'ArmedRun', 'ArmedJump' }) do
  local tag = tags[state]
  assert(tag, 'Missing animation tag: ' .. state)
  for number = tag.fromFrame.frameNumber, tag.toFrame.frameNumber do
    assert(not usedFrames[number], 'Animation tags overlap at frame ' .. number)
    assert(sprite.frames[number].duration > 0, 'Frame duration must be positive')
    usedFrames[number] = true
  end
end
local entries = {}
for _, state in ipairs({ 'Run', 'Jump', 'Hit', 'Death', 'Laugh', 'ArmedRun', 'ArmedJump' }) do
  local tag = tags[state]
  assert(tag, 'Missing animation tag: ' .. state)
  local first = tag.fromFrame.frameNumber
  for number = first, tag.toFrame.frameNumber do
    local image = Image(64, 64, ColorMode.RGB)
    image:drawSprite(sprite, number)
    local index = number - first
    local name = string.format('mydino_%s_%02d.png', string.lower(state), index)
    image:saveAs(root .. '/Assets/Game/Art/MyDino/' .. name)
    local duration = math.floor(sprite.frames[number].duration * 1000 + .5)
    local running=state=='Run' or state=='ArmedRun' or state=='Laugh'
    local bob=({0,1,0,-1,-2,-1,0,1,0,-1,-2,-1})[index%12+1]
    entries[#entries + 1] = string.format('{"state":"%s","frame":%d,"durationMs":%d,"file":"%s","gripX":%.4f,"gripY":%.4f}', state, index, duration, name, running and 1/16 or 0, running and bob/16 or 0)
  end
end
sprite:saveAs(root .. '/Tools/SourceArt/MyDinoAnimation/MyDino-Animated.aseprite')
local file = io.open(root .. '/Tools/SourceArt/MyDinoAnimation/animation.json', 'w')
file:write('{"width":64,"height":64,"pixelsPerUnit":16,"pivotX":32,"pivotY":16,"frames":[' .. table.concat(entries, ',') .. ']}')
file:close()
print('Exported ' .. #entries .. ' authored animation frames. In Unity choose Dino Slug > Use MyDino Artwork.')
