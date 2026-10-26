# Dino Slug — project overview

Dino Slug is a Unity 6.6 endless runner with three lives, jumping, dashing, coins and a persistent best score. It uses the New Input System, uGUI and URP's 2D renderer. Normal travel speed increases from 7 to 11 units per second. Space jumps, Shift gives a short speed boost, and J or the left mouse button fires an equipped weapon.

## Game rules and components

`RunModel` owns score, health, difficulty and run state without depending on Unity objects. `GameSession` connects it to input, player movement, the HUD, audio, map progression and PlayerPrefs. `PlayerController` handles movement, grounded checks, dash timing and animation reactions.

`Interactable` provides shared movement, collision consumption and projectile-hit handling. Ground and air obstacles, coins, health and weapon pickups specialize that behavior. A consumed contact cannot remove health twice. `PlayerWeapon` manages ammunition, weapon switching, firing, recoil and pooled projectiles. H gives 50 heavy-machine-gun rounds; S gives 30 shotgun shells with five pellets per shot.

Infantry use `RebelSoldier` for health and defeat animation, with `SoldierAttack` supplying rifle, grenade or bazooka behavior. `VehicleEnemy` handles tanks and helicopters; `MobileEnemy` handles the biker and passing aircraft. Warnings give an unarmed player a chance to dodge. Later maps add groups of two or three soldiers, while vehicle encounters wait for a clear lane and block infantry waves until they finish.

`MapProgression` changes from Temple Outskirts to Crash Coast at 3,000 points, Rebel Harbor at 6,000 and Iron Works at 9,000. Terrain enters from the right rather than switching in one frame. The four-map sequence repeats after 12,000 points. `ParallaxLayer` wraps scenery; `RunPresentation` manages cosmetic feedback while the camera stays fixed.

The Animator contains Run, Jump, Hit, Death, Laugh and armed running/jumping variants. Dino laughs after obtaining a gun and keeps the armed expression while ammunition remains. The selection screen plays the equip cue followed by the author's Dino announcement. Pause freezes gameplay and combat audio; restart clears run-specific state.

## Source distribution

The Windows download is the complete game. The Unity source deliberately omits Metal Slug media, including the mixed selection announcement. All runtime/editor/test code is present, alongside original/non-Metal-Slug artwork and the current editable Dino animation. Asset GUIDs and references are retained so the omitted files can be restored without rebuilding the scene. Read `MissingMedia.md` before running the full scene from the source checkout.
