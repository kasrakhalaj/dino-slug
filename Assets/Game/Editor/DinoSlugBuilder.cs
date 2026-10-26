using System;
using System.IO;
using System.Linq;
using DinoSlug;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DinoSlugEditor
{
    /// <summary>Creates real, inspectable scenes, prefabs, animation clips, and input assets.</summary>
    public static class DinoSlugBuilder
    {
        private const string Root = "Assets/Game";
        public const string ScenePath = Root + "/Scenes/DinoSlug.unity";
        private static readonly Color Cream = Hex("FFF1D7");
        private static readonly Color Muted = Hex("D9BEBD");
        private static readonly Color Gold = Hex("F5C47A");
        private static readonly Color Ink = Hex("28283F");
        private static TMP_FontAsset font;
        private static Material spriteMaterial;

        [MenuItem("Dino Slug/Create Game Scene")]
        public static void CreateGame()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Game scene already exists. Open it instead of replacing authored work.");
            foreach (string dir in new[] { "Scenes", "Prefabs", "Animation", "Input", "Materials", "Settings" })
                Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("TMP essential font is missing.");
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP Sprite Unlit shader is unavailable.");
            spriteMaterial = new Material(shader);
            AssetDatabase.CreateAsset(spriteMaterial, Root + "/Materials/WorldSprite.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sessionObject = new GameObject("GameSession");
            var session = sessionObject.AddComponent<GameSession>();
            var input = sessionObject.AddComponent<RunnerInput>();
            var audio = sessionObject.AddComponent<GameAudio>();
            audio.Configure(AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Audio" }).Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray());
            var actions = CreateInput();
            input.Configure(actions, session);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.75f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("25283F");
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.volumeLayerMask = 1;
            ConfigurePixelCamera(camera);
            var light = new GameObject("Global 2D Light").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1;

            CreateLandscape(session);
            var floor = new GameObject("Ground Collider").AddComponent<BoxCollider2D>();
            floor.size = new Vector2(200, 1);
            floor.transform.position = new Vector3(0, -3.7f, 0);

            var controller = CreateAnimator();
            var playerRoot = new GameObject("Dinosaur");
            playerRoot.transform.position = new Vector3(-6.4f, -3.18f, 0);
            var body = playerRoot.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collision = playerRoot.AddComponent<BoxCollider2D>();
            collision.size = new Vector2(.78f, 1.4f);
            collision.offset = new Vector2(.1f, .72f);
            var visual = Sprite("Visual", "dino_run_0", Vector3.zero, Vector3.one, 20, playerRoot.transform);
            var face = AddHead(visual.transform);
            var animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var player = playerRoot.AddComponent<PlayerController>();
            var dust = Particles("Footstep Dust", Hex("E9B993"), .13f, 11, true);
            dust.transform.SetParent(playerRoot.transform, false);
            dust.transform.localPosition = new Vector3(-.25f, .08f, 0);
            var sparks = Particles("Interaction Sparks", Gold, .12f, 0, false);
            player.Configure(session, animator, visual, dust, sparks);
            player.ConfigureHead(face);

            var spawner = new GameObject("Obstacle Spawner").AddComponent<ObstacleSpawner>();
            var cactus = CreatePrefab<GroundObstacle>("Cactus", "cactus", new Vector2(.62f, 1.42f), new Vector2(0, .75f), new Vector3(.85f, .85f, 1));
            var bird = CreatePrefab<AirObstacle>("Pterosaur", "bird_0", new Vector2(1.05f, .4f), Vector2.zero, Vector3.one);
            var coin = CreatePrefab<Coin>("Coin", "coin", new Vector2(.5f, .5f), Vector2.zero, Vector3.one * .65f);
            spawner.Configure(session, cactus, bird, coin);

            var hero = Sprite("Menu Dinosaur", "dino_run_0", new Vector3(5.2f, -3.2f, 0), Vector3.one * 3f, 19);
            hero.gameObject.AddComponent<Animator>().runtimeAnimatorController = controller;
            AddHead(hero.transform);
            var hud = CreateUI(session, actions, hero.gameObject);
            session.Configure(player, spawner, input, hud, audio);

            PlayerSettings.companyName = "HW1";
            PlayerSettings.productName = "Dino Slug";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            // New Input System ONLY. No legacy fallback is shipped.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handling = settings.FindProperty("activeInputHandler");
            if (handling != null) { handling.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
            var names = QualitySettings.names;
            QualitySettings.SetQualityLevel(names.Length - 1, true);
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            Time.fixedDeltaTime = 1f / 60f;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[DinoSlug] Created fully wired game scene, input actions, four-state Animator, prefabs, and menus.");
        }

        private static void ImportSprites()
        {
            foreach (string file in Directory.GetFiles(Root + "/Art", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                bool isMyDino = Path.GetFileName(file) == "my_dino.png";
                importer.spritePixelsPerUnit = isMyDino ? 16 : 32;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                var texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);
                texSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                texSettings.spritePivot = isMyDino ? new Vector2(.5f, 2f / 32f) : Path.GetFileName(file).StartsWith("dino_") && Path.GetFileName(file) != "dino_head.png" ? new Vector2(.5f, 7f / 64f) : Path.GetFileName(file) == "cactus.png" ? new Vector2(.5f, 3f / 56f) : new Vector2(.5f, .5f);
                texSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(texSettings);
                importer.SaveAndReimport();
            }
        }

        private static InputActionAsset CreateInput()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "DinoSlugControls";
            var gameplay = asset.AddActionMap("Gameplay");
            gameplay.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            var dash = gameplay.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            dash.AddBinding("<Keyboard>/rightShift");
            var system = asset.AddActionMap("System");
            system.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            var ui = asset.AddActionMap("UI");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position").expectedControlType = "Vector2";
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton").expectedControlType = "Button";
            ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll").expectedControlType = "Vector2";
            ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough);
            navigate.expectedControlType = "Vector2";
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            string path = Root + "/Input/DinoSlugControls.inputactions";
            File.WriteAllText(path, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static AnimatorController CreateAnimator()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animation/Dinosaur.controller");
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var run = machine.AddState("Run", new Vector3(220, 70));
            run.motion = Clip("Run", "dino_run_", 6, 12, true);
            var jump = machine.AddState("Jump", new Vector3(480, 70));
            jump.motion = Clip("Jump", "dino_jump", 1, 1, true);
            var hit = machine.AddState("Hit", new Vector3(220, 230));
            hit.motion = Clip("Hit", "dino_hit_", 2, 8, false);
            var death = machine.AddState("Death", new Vector3(480, 230));
            death.motion = Clip("Death", "dino_death_", 6, 6, false);
            machine.defaultState = run;
            Transition(run, jump, "Grounded", false);
            Transition(jump, run, "Grounded", true);
            var damage = machine.AddAnyStateTransition(hit);
            damage.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            damage.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            damage.duration = 0; damage.canTransitionToSelf = false;
            var dead = machine.AddAnyStateTransition(death);
            dead.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            dead.duration = 0; dead.canTransitionToSelf = false;
            Transition(hit, run, "Grounded", true, true);
            Transition(hit, jump, "Grounded", false, true);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip Clip(string name, string prefix, int count, float fps, bool loop)
        {
            var clip = new AnimationClip { name = name, frameRate = fps };
            var frames = new ObjectReferenceKeyframe[count + 1];
            for (int i = 0; i <= count; i++)
                frames[i] = new ObjectReferenceKeyframe { time = i / fps, value = LoadSprite(count == 1 ? prefix : prefix + Math.Min(i, count - 1)) };
            AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, frames);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimateHead(clip, name, count, fps);
            AssetDatabase.CreateAsset(clip, Root + "/Animation/" + name + ".anim");
            return clip;
        }

        private static void Transition(AnimatorState from, AnimatorState to, string condition, bool value, bool exit = false)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = exit; transition.exitTime = .95f; transition.duration = 0;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        }

        private static SpriteRenderer AddHead(Transform parent)
        {
            var existing = parent.Find("Head");
            if (existing != null) return existing.GetComponent<SpriteRenderer>();
            return Sprite("Head", "dino_head", new Vector3(.375f, 1.125f, 0), Vector3.one, 21, parent);
        }

        private static void AnimateHead(AnimationClip clip, string state, int count, float fps)
        {
            var x = new AnimationCurve(); var y = new AnimationCurve(); var angleCurve = new AnimationCurve();
            for (int i = 0; i <= count; i++)
            {
                int frame = Math.Min(i, count - 1);
                var position = new Vector3(.375f, 1.125f, 0);
                float angle = 0;
                if (state == "Run") position.y += Mathf.Sin(frame * Mathf.PI * 4 / 6) * .015625f;
                else if (state == "Hit") position.y += Mathf.Sin(frame * 2f) * .015625f;
                else if (state == "Death")
                {
                    angle = -Mathf.Min(85, frame * 17);
                    var pivot = new Vector3(0, .140625f, 0);
                    position = pivot + Quaternion.Euler(0, 0, angle) * (position - pivot) + Vector3.left * frame * .0625f;
                    position.y = Mathf.Max(.35f, position.y);
                }
                x.AddKey(i / fps, position.x); y.AddKey(i / fps, position.y); angleCurve.AddKey(i / fps, angle);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Head", typeof(Transform), "m_LocalPosition.x"), x);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Head", typeof(Transform), "m_LocalPosition.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Head", typeof(Transform), "localEulerAnglesRaw.z"), angleCurve);
        }

        private static T CreatePrefab<T>(string name, string sprite, Vector2 size, Vector2 offset, Vector3 scale) where T : Interactable
        {
            var root = new GameObject(name);
            var renderer = Sprite("Visual", sprite, Vector3.zero, scale, 18, root.transform);
            var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = size; collider.offset = offset;
            var item = root.AddComponent<T>();
            if (typeof(T) == typeof(AirObstacle)) renderer.gameObject.AddComponent<FrameAnimation>().Configure(Enumerable.Range(0, 4).Select(i => LoadSprite("bird_" + i)).ToArray());
            string path = Root + "/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        private static void CreateLandscape(GameSession session)
        {
            Sprite("Dusk Sky", "sky", Vector3.zero, new Vector3(2, 2, 1), -20);
            var sun = Sprite("Setting Sun", "disc", new Vector3(5.4f, .8f, 0), Vector3.one * 3.3f, -19);
            sun.color = Hex("F4C497");
            UnityEngine.Random.InitState(40);
            for (int i = 0; i < 33; i++)
            {
                var star = Sprite("Star " + i, "disc", new Vector3(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(1.5f, 6.3f), 0), Vector3.one * UnityEngine.Random.Range(.025f, .055f), -18);
                star.color = new Color(1, .9f, .8f, UnityEngine.Random.Range(.25f, .7f));
            }
            Layer(session, "Far Mountains", "mountains_far", .08f, -1.6f, -15, 2f);
            Layer(session, "Near Mountains", "mountains_mid", .17f, -1.6f, -12, 2f);
            Layer(session, "Dunes", "dunes", .32f, -1.9f, -8, 2f);
            Layer(session, "Ground", "ground", 1f, -5.7f, 3, 2f);
        }

        private static void Layer(GameSession session, string name, string sprite, float factor, float y, int order, float scale)
        {
            var layer = new GameObject(name).AddComponent<ParallaxLayer>();
            float width = LoadSprite(sprite).bounds.size.x * scale;
            layer.Configure(session, factor, width);
            for (int i = -1; i <= 1; i++) Sprite("Tile " + (i + 1), sprite, new Vector3(i * width, y, 0), new Vector3(scale, scale, 1), order, layer.transform);
        }

        private static void SetupVolume()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("URP is not configured by the template.");
            pipeline.supportsHDR = true;
            EditorUtility.SetDirty(pipeline);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.25f); bloom.threshold.Override(.95f);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.18f); vignette.smoothness.Override(.6f);
            AssetDatabase.CreateAsset(profile, Root + "/Settings/DuskVolume.asset");
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            var volume = new GameObject("Dusk Atmosphere").AddComponent<Volume>();
            volume.isGlobal = true; volume.sharedProfile = profile;
        }

        private static void ConfigurePixelCamera(Camera camera)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || !(pipeline.GetType().FullName ?? "").Contains("Universal"))
                throw new InvalidOperationException("Pixel art configuration expects the project's URP 2D renderer.");
            var pixel = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            pixel.assetsPPU = 32;
            pixel.refResolutionX = 768; pixel.refResolutionY = 432;
            pixel.gridSnapping = UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixel.cropFrame = UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.Windowbox;
        }

        [MenuItem("Dino Slug/Apply Pixel Art Settings")]
        public static void ApplyPixelArtSettings()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var camera = Camera.main;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
            if (camera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>() == null) ConfigurePixelCamera(camera);
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            Time.fixedDeltaTime = 1f / 60f;
            var hero = GameObject.Find("Menu Dinosaur");
            if (hero != null) hero.transform.localScale = Vector3.one * 3;
            var mainTitle = GameObject.Find("Game Canvas/Main Menu/Title");
            if (mainTitle != null)
            {
                var oldText = mainTitle.GetComponent<TMP_Text>();
                if (oldText != null) oldText.enabled = false;
                var image = mainTitle.GetComponent<UnityEngine.UI.Image>() ?? mainTitle.AddComponent<UnityEngine.UI.Image>();
                image.sprite = LoadSprite("title"); image.preserveAspect = true; image.raycastTarget = false;
                ((RectTransform)mainTitle.transform).sizeDelta = new Vector2(600, 300);
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[DinoSlug] Native pixel art applied: point filtering, 32 PPU, pixel snapping, no antialiasing or blur.");
        }

        [MenuItem("Dino Slug/Apply Goofy Dinosaur Face")]
        public static void ApplyGoofyFace()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ImportSprites();
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/WorldSprite.mat");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            player.ConfigureHead(AddHead(player.transform.Find("Visual")));
            var hero = GameObject.Find("Menu Dinosaur");
            if (hero != null) AddHead(hero.transform);
            foreach (string state in new[] { "Run", "Jump", "Hit", "Death" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animation/" + state + ".anim");
                int count = state == "Jump" ? 1 : state == "Hit" ? 2 : 6;
                float fps = state == "Jump" ? 1 : state == "Hit" ? 8 : state == "Run" ? 12 : 6;
                AnimateHead(clip, state, count, fps); EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(player);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[DinoSlug] Reference-inspired goofy face attached and animated in all four states.");
        }

        private static ParticleSystem Particles(string name, Color color, float size, float rate, bool loop)
        {
            var particles = new GameObject(name).AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.playOnAwake = false; main.loop = loop; main.duration = 1;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size);
            main.startColor = color; main.gravityModifier = .2f; main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.rateOverTime = rate;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .05f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.sortingOrder = 30;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) throw new InvalidOperationException("URP particle shader missing.");
            string path = Root + "/Materials/Sparks.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                material.SetTexture("_BaseMap", LoadSprite("disc").texture);
                material.renderQueue = 3000;
                AssetDatabase.CreateAsset(material, path);
            }
            renderer.sharedMaterial = material;
            return particles;
        }

        private static GameHud CreateUI(GameSession session, InputActionAsset actions, GameObject hero)
        {
            var root = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = events.GetComponent<InputSystemUIInputModule>(); module.actionsAsset = actions;
            module.point = InputActionReference.Create(actions.FindAction("UI/Point"));
            module.leftClick = InputActionReference.Create(actions.FindAction("UI/Click"));
            module.scrollWheel = InputActionReference.Create(actions.FindAction("UI/ScrollWheel"));
            module.move = InputActionReference.Create(actions.FindAction("UI/Navigate"));
            module.submit = InputActionReference.Create(actions.FindAction("UI/Submit"));
            module.cancel = InputActionReference.Create(actions.FindAction("UI/Cancel"));
            // Persist action references as subassets rather than temporary editor objects.
            AssetDatabase.CreateAsset(module.point, Root + "/Input/UIReferences.asset");
            foreach (var reference in new[] { module.leftClick, module.scrollWheel, module.move, module.submit, module.cancel })
                AssetDatabase.AddObjectToAsset(reference, Root + "/Input/UIReferences.asset");
            var hud = root.AddComponent<GameHud>();

            var ingame = Panel("In Game HUD", root.transform, Color.clear);
            Label("Brand", ingame.transform, "DINO / SLUG", 22, new Vector2(210, 90), new Vector2(220, 35), Gold);
            Label("Lives Label", ingame.transform, "VITALITY", 13, new Vector2(160, 138), new Vector2(120, 20), Muted);
            var health = Label("Lives", ingame.transform, "3 / 3", 18, new Vector2(300, 138), new Vector2(60, 25), Cream);
            var healthBar = Bar("Health Bar", ingame.transform, new Vector2(210, 170), new Vector2(220, 8), Hex("79D3B2"));
            Label("Current Label", ingame.transform, "SCORE", 13, new Vector2(650, 70), new Vector2(180, 25), Muted, TextAlignmentOptions.Center);
            var score = Label("Current Score", ingame.transform, "00000", 42, new Vector2(650, 111), new Vector2(220, 65), Cream, TextAlignmentOptions.Center);
            Label("Best Label", ingame.transform, "PERSONAL BEST", 13, new Vector2(880, 70), new Vector2(180, 25), Muted, TextAlignmentOptions.Center);
            var best = Label("High Score", ingame.transform, "00000", 30, new Vector2(880, 111), new Vector2(220, 55), Gold, TextAlignmentOptions.Center);
            var coins = Label("Coin Count", ingame.transform, "00", 26, new Vector2(1180, 95), new Vector2(90, 45), Gold);
            Label("Coin Icon", ingame.transform, "+", 30, new Vector2(1125, 95), new Vector2(35, 45), Gold, TextAlignmentOptions.Center);
            Button("Pause Button", ingame.transform, "II", new Vector2(1450, 95), new Vector2(90, 55), false, session.TogglePause);
            var meters = Label("Distance", ingame.transform, "0 m", 17, new Vector2(1390, 820), new Vector2(160, 30), Muted, TextAlignmentOptions.Right);
            Label("Jump Hint", ingame.transform, "SPACE   JUMP", 15, new Vector2(180, 805), new Vector2(200, 35), Cream);
            var dashLabel = Label("Dash Hint", ingame.transform, "SHIFT   DASH READY", 15, new Vector2(445, 805), new Vector2(250, 35), Cream);
            var dashBar = Bar("Dash Cooldown", ingame.transform, new Vector2(445, 832), new Vector2(250, 4), Gold);

            var main = Panel("Main Menu", root.transform, new Color(.07f, .07f, .12f, .09f));
            Label("Eyebrow", main.transform, "A LITTLE DINOSAUR. AN ENDLESS HORIZON.", 15, new Vector2(430, 184), new Vector2(640, 30), Gold);
            var title = Rect("Title", main.transform, new Vector2(400, 341), new Vector2(600, 300));
            var titleImage = title.gameObject.AddComponent<UnityEngine.UI.Image>();
            titleImage.sprite = LoadSprite("title"); titleImage.preserveAspect = true; titleImage.raycastTarget = false;
            Label("Description", main.transform, "Chase the sunset. Leap the cacti.\nCollect a little gold along the way.", 23, new Vector2(420, 513), new Vector2(620, 85), Cream);
            Button("Start Run", main.transform, "LET'S RUN     >", new Vector2(270, 626), new Vector2(340, 68), true, session.StartRun);
            var mainBest = Label("Menu Record", main.transform, "PERSONAL BEST   0", 14, new Vector2(390, 698), new Vector2(580, 28), Gold);
            Label("Controls", main.transform, "[ SPACE ]  JUMP       [ SHIFT ]  DASH       [ ESC ]  PAUSE", 15, new Vector2(570, 806), new Vector2(940, 35), Muted);
            Label("Edition", main.transform, "01 / THE DUSK RUN", 13, new Vector2(1370, 806), new Vector2(260, 35), Cream, TextAlignmentOptions.Right);
            Button("Quit", main.transform, "QUIT", new Vector2(1450, 90), new Vector2(100, 45), false, session.Quit);

            var pause = Panel("Pause Menu", root.transform, new Color(.09f, .09f, .15f, .88f));
            Label("Pause Eyebrow", pause.transform, "TAKE A BREATHER", 15, new Vector2(800, 226), new Vector2(600, 35), Gold, TextAlignmentOptions.Center);
            Label("Pause Title", pause.transform, "The horizon can wait.", 49, new Vector2(800, 300), new Vector2(900, 90), Cream, TextAlignmentOptions.Center);
            Button("Resume", pause.transform, "KEEP RUNNING", new Vector2(800, 420), new Vector2(380, 65), true, session.TogglePause);
            Button("Restart", pause.transform, "START AGAIN", new Vector2(800, 509), new Vector2(380, 60), false, session.StartRun);
            Button("Back To Menu", pause.transform, "BACK TO MENU", new Vector2(800, 592), new Vector2(380, 60), false, session.MainMenu);
            Label("Pause Hint", pause.transform, "ESC TO RESUME", 14, new Vector2(800, 703), new Vector2(500, 30), Muted, TextAlignmentOptions.Center);

            var over = Panel("Game Over", root.transform, new Color(.09f, .09f, .15f, .9f));
            Label("Over Eyebrow", over.transform, "EVERY RUN IS A NEW STORY", 15, new Vector2(800, 183), new Vector2(800, 35), Gold, TextAlignmentOptions.Center);
            Label("Over Title", over.transform, "One more horizon?", 52, new Vector2(800, 262), new Vector2(900, 80), Cream, TextAlignmentOptions.Center);
            var result = Label("Final Score", over.transform, "0", 96, new Vector2(800, 389), new Vector2(900, 130), Gold, TextAlignmentOptions.Center);
            var detail = Label("Result Detail", over.transform, "", 17, new Vector2(800, 480), new Vector2(1000, 50), Muted, TextAlignmentOptions.Center);
            Button("Try Again", over.transform, "RUN IT BACK     >", new Vector2(800, 588), new Vector2(380, 66), true, session.StartRun);
            Button("Result Menu", over.transform, "BACK TO MENU", new Vector2(800, 679), new Vector2(380, 60), false, session.MainMenu);

            hud.Configure(main, pause, over, ingame, hero, score, best, coins, health, meters, result, detail, mainBest, dashLabel, healthBar, dashBar);
            main.SetActive(true); pause.SetActive(false); over.SetActive(false); ingame.SetActive(false);
            return hud;
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = color.a > .5f;
            return go;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, string text, float size, Vector2 position, Vector2 bounds, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rect = Rect(name, parent, position, bounds);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size;
            label.color = color; label.alignment = align; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static void Button(string name, Transform parent, string text, Vector2 position, Vector2 size, bool primary, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = primary ? Gold : new Color(.17f, .16f, .23f, .8f); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = Hex("FFE4BA"); colors.pressedColor = Hex("D3A467"); colors.selectedColor = Hex("F5D3A5"); button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            var label = Label("Label", rect, text, 18, new Vector2(size.x / 2, size.y / 2), size, primary ? Ink : Cream, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
        }

        private static RectTransform Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var root = Rect(name, parent, position, size);
            var background = root.gameObject.AddComponent<UnityEngine.UI.Image>(); background.color = new Color(1, 1, 1, .15f); background.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)); fill.transform.SetParent(root, false);
            var rect = (RectTransform)fill.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = fill.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + name + ".png");
        private static SpriteRenderer Sprite(string name, string asset, Vector3 position, Vector3 scale, int order, Transform parent = null)
        {
            var go = new GameObject(name); if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = LoadSprite(asset); renderer.sortingOrder = order; renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }

        [MenuItem("Dino Slug/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/DinoSlug.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("[DinoSlug] Windows build succeeded: " + report.summary.totalSize + " bytes.");
        }
    }
}
