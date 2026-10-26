using System;
using System.IO;
using System.Linq;
using DinoSlug;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DinoSlugEditor
{
    /// <summary>Imports the editable MyDino animation tags and preserves gameplay wiring.</summary>
    public static class MyDinoImporter
    {
        private const string ArtFolder = "Assets/Game/Art/MyDino/";
        private const string MenuControllerPath = "Assets/Game/Animation/MyDinoMenu.controller";

        [Serializable] private sealed class AnimationDocument
        {
            public int width, height, pixelsPerUnit, pivotX, pivotY;
            public Frame[] frames;
        }
        [Serializable] private sealed class Frame
        {
            public string state, file;
            public int frame, durationMs;
            public float gripX, gripY;
        }

        [MenuItem("Dino Slug/Use MyDino Artwork")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var document = JsonUtility.FromJson<AnimationDocument>(File.ReadAllText("Tools/SourceArt/MyDinoAnimation/animation.json"));
            if (document == null || document.width != 64 || document.height != 64 || document.pixelsPerUnit != 16 || document.frames == null)
                throw new InvalidOperationException("Export the 64x64 animation with Tools/import_mydino_animation.ps1 first.");
            foreach (var frame in document.frames) ImportFrame(frame, document);
            foreach (string state in new[] { "Run", "Jump", "Hit", "Death", "Laugh", "ArmedRun", "ArmedJump" })
                Animate(state, document.frames.Where(frame => frame.state == state).OrderBy(frame => frame.frame).ToArray());
            ConfigureLaughController();
            ConfigureArmedExpressions();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + document.frames.First(frame => frame.state == "Run" && frame.frame == 0).file);
            var scene = EditorSceneManager.OpenScene(DinoSlugBuilder.ScenePath);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var menu = GameObject.Find("Menu Dinosaur");
            if (player == null || menu == null) throw new InvalidOperationException("Dinosaur scene objects are missing.");
            UseWholeDinosaur(player.transform.Find("Visual"), sprite);
            UseMenuDinosaur(menu, sprite);
            // SetVisible() must not reactivate the old independent head during a restart.
            player.ConfigureHead(null);
            player.Weapon.ConfigureGripMotion(document.frames.Select(f=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+f.file)).ToArray(),
                document.frames.Select(f=>new Vector2(f.gripX,f.gripY)).ToArray());
            EditorUtility.SetDirty(player.Weapon);
            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MyDino] Applied " + document.frames.Length + " refined animation frames, pickup laugh, and the menu run loop.");
        }

        private static void ImportFrame(Frame frame, AnimationDocument document)
        {
            if (Path.GetFileName(frame.file) != frame.file || frame.durationMs <= 0)
                throw new InvalidOperationException("Invalid animation frame: " + frame.file);
            string path = ArtFolder + frame.file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing animation frame: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = new Vector2((float)document.pivotX / document.width, (float)document.pivotY / document.height);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || sprite.rect.width != document.width || sprite.rect.height != document.height)
                throw new InvalidOperationException("Unexpected animation frame size: " + path);
        }

        private static void UseWholeDinosaur(Transform visual, Sprite sprite)
        {
            if (visual == null) throw new InvalidOperationException("Missing dinosaur visual.");
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) throw new InvalidOperationException("Missing dinosaur renderer.");
            renderer.sprite = sprite;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            var oldHead = visual.Find("Head");
            if (oldHead != null) oldHead.gameObject.SetActive(false);
            EditorUtility.SetDirty(renderer);
        }

        private static void UseMenuDinosaur(GameObject menu, Sprite sprite)
        {
            // Animate a child so the shared position curves cannot move the menu's scene anchor.
            var originalRenderer = menu.GetComponent<SpriteRenderer>();
            var originalAnimator = menu.GetComponent<Animator>();
            var visual = menu.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual", typeof(SpriteRenderer), typeof(Animator)).transform;
                visual.SetParent(menu.transform, false);
            }
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sharedMaterial = originalRenderer.sharedMaterial;
            renderer.sortingLayerID = originalRenderer.sortingLayerID;
            renderer.sortingOrder = originalRenderer.sortingOrder;
            var animator = visual.GetComponent<Animator>();
            var menuController = AssetDatabase.LoadAssetAtPath<AnimatorController>(MenuControllerPath);
            if (menuController == null)
            {
                menuController = AnimatorController.CreateAnimatorControllerAtPath(MenuControllerPath);
                var state = menuController.layers[0].stateMachine.AddState("Run");
                menuController.layers[0].stateMachine.defaultState = state;
            }
            menuController.layers[0].stateMachine.defaultState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animation/Run.anim");
            EditorUtility.SetDirty(menuController);
            animator.runtimeAnimatorController = menuController;
            animator.applyRootMotion = false;
            originalRenderer.enabled = false;
            originalAnimator.enabled = false;
            var oldHead = menu.transform.Find("Head");
            if (oldHead != null) oldHead.gameObject.SetActive(false);
            UseWholeDinosaur(visual, sprite);
            EditorUtility.SetDirty(originalRenderer);
            EditorUtility.SetDirty(originalAnimator);
        }

        private static void Animate(string state, Frame[] frames)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animation/" + state + ".anim");
            if (clip == null && (state == "Laugh" || state == "ArmedRun" || state == "ArmedJump"))
            {
                clip = new AnimationClip { name = state };
                AssetDatabase.CreateAsset(clip,"Assets/Game/Animation/" + state + ".anim");
            }
            if (clip == null) throw new InvalidOperationException("Missing " + state + " animation.");
            if (frames.Length == 0) throw new InvalidOperationException("Missing animation tag: " + state);
            // Visual-only curves never move the player's physics root or gun anchor.
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            float time = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].frame != i) throw new InvalidOperationException("Non-contiguous frames in " + state);
                keys[i] = new ObjectReferenceKeyframe { time = time, value = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + frames[i].file) };
                time += frames[i].durationMs / 1000f;
            }
            keys[frames.Length] = new ObjectReferenceKeyframe { time = time, value = keys[frames.Length - 1].value };
            clip.frameRate = 1000f / frames[0].durationMs;
            foreach (string property in new[] { "m_LocalPosition.x", "m_LocalPosition.y", "localEulerAnglesRaw.z" })
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), property), AnimationCurve.Constant(0, time, 0));
            AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding
            { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0;
            settings.stopTime = time;
            settings.loopTime = state == "Run" || state == "ArmedRun";
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void ConfigureLaughController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Animation/Dinosaur.controller");
            if (!controller.parameters.Any(parameter=>parameter.name=="PickupLaugh"))
                controller.AddParameter("PickupLaugh",AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var run = machine.states.First(item=>item.state.name=="Run").state;
            var jump = machine.states.First(item=>item.state.name=="Jump").state;
            var laugh = machine.states.Select(item=>item.state).FirstOrDefault(state=>state.name=="Laugh");
            if (laugh == null) laugh = machine.AddState("Laugh",new Vector3(500,240,0));
            laugh.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animation/Laugh.anim");
            if (!run.transitions.Any(transition=>transition.destinationState==laugh))
            {
                var enter = run.AddTransition(laugh);
                enter.hasExitTime = false; enter.duration = 0;
                enter.AddCondition(AnimatorConditionMode.If,0,"PickupLaugh");
                enter.AddCondition(AnimatorConditionMode.If,0,"Grounded");
                enter.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            }
            if (!laugh.transitions.Any(transition=>transition.destinationState==jump))
            {
                var airborne = laugh.AddTransition(jump);
                airborne.hasExitTime = false; airborne.duration = 0;
                airborne.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
                airborne.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            }
            if (!laugh.transitions.Any(transition=>transition.destinationState==run))
            {
                var finish = laugh.AddTransition(run);
                finish.hasExitTime = true; finish.exitTime = 1; finish.duration = 0;
                finish.AddCondition(AnimatorConditionMode.If,0,"Grounded");
                finish.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            }
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureArmedExpressions()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Animation/Dinosaur.controller");
            if (!controller.parameters.Any(parameter=>parameter.name=="Armed"))
                controller.AddParameter("Armed",AnimatorControllerParameterType.Float);
            foreach (string name in new[] { "Run", "Jump" })
            {
                var state = controller.layers[0].stateMachine.states.First(item=>item.state.name==name).state;
                var tree = state.motion as BlendTree;
                if (tree == null)
                {
                    tree = new BlendTree { name = name + " Expression", hideFlags = HideFlags.HideInHierarchy };
                    AssetDatabase.AddObjectToAsset(tree,controller);
                }
                tree.blendType = BlendTreeType.Simple1D;
                tree.blendParameter = "Armed";
                tree.useAutomaticThresholds = false;
                tree.children = new[]
                {
                    new ChildMotion { motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animation/" + name + ".anim"), threshold = 0, timeScale = 1 },
                    new ChildMotion { motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animation/Armed" + name + ".anim"), threshold = 1, timeScale = 1 }
                };
                state.motion = tree;
                EditorUtility.SetDirty(tree);
                EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);
        }
    }
}
