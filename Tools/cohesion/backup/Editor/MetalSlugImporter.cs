using System;
using System.IO;
using System.Linq;
using DinoSlug;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoSlugEditor
{
    public static class MetalSlugImporter
    {
        private const string Art = "Assets/Game/Art/MetalSlug/";
        private const string InputPath = "Assets/Game/Input/DinoSlugControls.inputactions";

        [MenuItem("Dino Slug/Install Metal Slug H Pickup")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = path.EndsWith("hmg_bullet.png") ? 64 : 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = path.EndsWith("hmg_gun.png") ? new Vector2(0,.5f)
                    : path.EndsWith("hmg_bullet.png") ? new Vector2(1f/36f,7.5f/13f) : new Vector2(.5f,.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            foreach (string path in Directory.GetFiles(Art+"Rebel/", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 24;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(.5f,4f/64f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            foreach (string path in Directory.GetFiles("Assets/Game/Audio/MetalSlug", "*.wav"))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var gameplay = actions.FindActionMap("Gameplay", true);
            if (gameplay.FindAction("Fire") == null)
            {
                var fire = gameplay.AddAction("Fire", InputActionType.Button);
                fire.AddBinding("<Keyboard>/j");
                fire.AddBinding("<Mouse>/leftButton");
                File.WriteAllText(InputPath, actions.ToJson());
                AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var scene = EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            var player = session.Player;
            var visual = player.transform.Find("Visual");
            var body = visual.GetComponent<SpriteRenderer>();
            var gun = Renderer("Heavy Machine Gun", visual, body, Sprite("hmg_gun"));
            gun.transform.localPosition = new Vector3(.15f, .9f, 0);
            gun.transform.localScale = Vector3.one * .75f;
            gun.enabled = false;
            // The barrel opening is the right edge of source pixel (21,3) in the 22x13 gun.
            var barrel = gun.transform.Find("Barrel Tip");
            if (barrel == null) { barrel = new GameObject("Barrel Tip").transform; barrel.SetParent(gun.transform,false); }
            barrel.localPosition = new Vector3(22f/16f,3f/16f,0);
            var existingMuzzle = gun.transform.Find("Muzzle Flash");
            if (existingMuzzle != null) existingMuzzle.SetParent(barrel,false);
            var muzzle = Renderer("Muzzle Flash", barrel, body, Sprite("hmg_bullet"));
            muzzle.transform.localPosition = Vector3.zero;
            muzzle.transform.localScale = new Vector3(1.4f,1.7f,1);
            muzzle.sortingOrder = gun.sortingOrder + 1;
            muzzle.enabled = false;

            var hudRoot = GameObject.Find("Game Canvas").transform.Find("In Game HUD");
            var template = hudRoot.Find("Jump Hint").GetComponent<TMP_Text>();
            var ammo = Label("Weapon Status", hudRoot, template, new Vector2(425,-215),new Vector2(630,35),18);
            ammo.text = "FIND H TO ARM UP";
            var announcement = Label("Weapon Announcement", hudRoot, template, new Vector2(800,-245),new Vector2(800,65),32);
            announcement.alignment = TextAlignmentOptions.Center;
            announcement.enabled = false;
            var weapon = player.GetComponent<PlayerWeapon>();
            if (weapon == null) weapon = player.gameObject.AddComponent<PlayerWeapon>();
            weapon.Configure(session, gun, muzzle, Sprite("hmg_bullet"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/hmg_pickup.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/MetalSlug/hmg_shot.wav"),ammo,announcement);
            player.ConfigureWeapon(weapon);

            var root = new GameObject("Heavy Machine Gun Pickup");
            var icon = Renderer("Visual", root.transform, body, Sprite("h_pickup_0"));
            icon.gameObject.AddComponent<FrameAnimation>().Configure(Enumerable.Range(0,4).Select(i=>Sprite("h_pickup_"+i)).ToArray());
            var pickup = root.AddComponent<HeavyMachineGunPickup>();
            var rb = root.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.1f,1.2f);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Prefabs/HeavyMachineGunPickup.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            var spawner = UnityEngine.Object.FindAnyObjectByType<ObstacleSpawner>();
            spawner.ConfigureWeaponPickup(prefab.GetComponent<HeavyMachineGunPickup>());
            var enemyRoot = new GameObject("Rebel Soldier");
            var enemyVisual = Renderer("Visual",enemyRoot.transform,body,Sprite("Rebel/rebel_walk_00"));
            var walking = enemyVisual.gameObject.AddComponent<FrameAnimation>();
            walking.Configure(Enumerable.Range(0,12).Select(i=>Sprite("Rebel/rebel_walk_"+i.ToString("D2"))).ToArray());
            var enemy = enemyRoot.AddComponent<RebelSoldier>();
            var enemyBody = enemyRoot.GetComponent<Rigidbody2D>();
            enemyBody.bodyType = RigidbodyType2D.Kinematic;
            enemyBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var enemyCollider = enemyRoot.GetComponent<BoxCollider2D>();
            enemyCollider.isTrigger = true;
            enemyCollider.size = new Vector2(.7f,1.45f);
            enemyCollider.offset = new Vector2(0,.75f);
            enemy.Configure(walking,Enumerable.Range(0,11).Select(i=>Sprite("Rebel/rebel_death_"+i.ToString("D2"))).ToArray());
            var enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyRoot,"Assets/Game/Prefabs/RebelSoldier.prefab");
            UnityEngine.Object.DestroyImmediate(enemyRoot);
            spawner.ConfigureRebelSoldier(enemyPrefab.GetComponent<RebelSoldier>());
            GameObject.Find("Game Canvas").transform.Find("Main Menu/Controls").GetComponent<TMP_Text>().text = "[ SPACE ] JUMP    [ SHIFT ] DASH    [ J / MOUSE ] FIRE    [ ESC ] PAUSE";
            foreach (var item in new UnityEngine.Object[] { player, weapon, spawner }) EditorUtility.SetDirty(item);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MetalSlug] Smaller gun, barrel-aligned tracer origin, H pickup and animated Rebel soldier installed.");
        }

        private static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
            if (sprite == null) throw new InvalidOperationException("Missing Metal Slug sprite: " + name);
            return sprite;
        }
        private static SpriteRenderer Renderer(string name, Transform parent, SpriteRenderer template, Sprite sprite)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name,typeof(SpriteRenderer)).transform; child.SetParent(parent,false); }
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = template.sharedMaterial;
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sortingOrder = template.sortingOrder + 2;
            return renderer;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_Text template, Vector2 position, Vector2 size, int fontSize)
        {
            var child = parent.Find(name);
            var label = child != null ? child.GetComponent<TMP_Text>() : UnityEngine.Object.Instantiate(template,parent);
            label.name = name;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0,1);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.fontSize = fontSize;
            label.color = new Color32(245,196,122,255);
            label.raycastTarget = false;
            return label;
        }
    }
}
