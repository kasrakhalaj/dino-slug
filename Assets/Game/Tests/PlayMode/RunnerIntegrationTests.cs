using System.Collections;
using System.IO;
using DinoSlug;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RunnerIntegrationTests
{
    private GameSession session;
    private Keyboard keyboard;
    private bool hadRecord;
    private int originalRecord;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

    [UnitySetUp] public IEnumerator LoadGame()
    {
        hadRecord = PlayerPrefs.HasKey(GameSession.HighScoreKey);
        originalRecord = PlayerPrefs.GetInt(GameSession.HighScoreKey, 0);
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        yield return SceneManager.LoadSceneAsync("DinoSlug");
        yield return null;
        session = Object.FindAnyObjectByType<GameSession>();
        Assert.That(session, Is.Not.Null);
        keyboard = InputSystem.AddDevice<Keyboard>();
        session.StartRun();
        Object.FindAnyObjectByType<ObstacleSpawner>().enabled = false;
        yield return new WaitForSeconds(.15f);
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.captureFramerate=0;
        Time.timeScale = 1;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        // Destroy the session before restoring preferences: its cleanup saves the latest record.
        if (session != null) Object.Destroy(session.gameObject);
        yield return null;
        if (hadRecord) PlayerPrefs.SetInt(GameSession.HighScoreKey, originalRecord);
        else PlayerPrefs.DeleteKey(GameSession.HighScoreKey);
        PlayerPrefs.Save();
    }

    private VehicleEnemy SpawnVehicle(string name,float ahead)
    {
#if UNITY_EDITOR
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<VehicleEnemy>("Assets/Game/Prefabs/"+name+".prefab");
        var vehicle=Object.Instantiate(prefab,new Vector3(session.Player.transform.position.x+ahead,name=="DiCokkaTank" ? -3.2f : -.25f,0),Quaternion.identity,session.Spawner.transform);
        vehicle.Initialize(session);return vehicle;
#else
        return null;
#endif
    }

    private MobileEnemy SpawnMobile(string name,float ahead)
    {
#if UNITY_EDITOR
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<MobileEnemy>("Assets/Game/Prefabs/"+name+".prefab");
        var enemy=Object.Instantiate(prefab,new Vector3(session.Player.transform.position.x+ahead,name=="RebelBiker" ? -3.2f : .35f,0),Quaternion.identity,session.Spawner.transform);
        enemy.Initialize(session);return enemy;
#else
        return null;
#endif
    }

    [UnityTest] public IEnumerator NewMobileEnemiesAppearInTheRealCourseAndRealGunfireCanDefeatThem()
    {
#if UNITY_EDITOR
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        foreach(int index in new[]{4,10})
        {
            session.StartRun();session.Spawner.enabled=false;yield return new WaitForSeconds(.15f);
            typeof(ObstacleSpawner).GetField("sequence",flags).SetValue(session.Spawner,index);
            typeof(ObstacleSpawner).GetField("remainingDistance",flags).SetValue(session.Spawner,0f);
            typeof(ObstacleSpawner).GetField("weaponDistance",flags).SetValue(session.Spawner,100f);
            session.Spawner.enabled=true;yield return null;yield return null;session.Spawner.enabled=false;
            var mobile=session.Spawner.GetComponentInChildren<MobileEnemy>();Assert.That(mobile,Is.Not.Null);
            Assert.That(mobile.Kind,Is.EqualTo(index==4 ? MobileEnemy.EnemyKind.RebelBiker : MobileEnemy.EnemyKind.FlyingTara));
            Capture(index==4 ? "40-original-rebel-biker" : "41-original-flying-tara");
            var weapon=session.Player.Weapon;weapon.CollectHeavyMachineGun();
            if(index==10)
            {
                float approach=Time.time+4;while(!mobile.IsWindingUp && Time.time<approach) yield return null;
                session.Player.RequestJump();
            }
            weapon.SetFireHeld(true);float deadline=Time.time+1.5f;
            while(mobile!=null && mobile.HitPoints>0 && Time.time<deadline) yield return null;
            weapon.SetFireHeld(false);Assert.That(mobile==null || mobile.HitPoints==0,Is.True,"Real HMG rounds must reach and defeat the native hitbox.");
            session.StartRun();yield return null;Assert.That(Object.FindObjectsByType<MobileEnemy>(),Is.Empty);
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator DinoSelectionAnimatesConfirmsOnceAndCanBeCancelled()
    {
        session.MainMenu();yield return null;int frozenScore=session.Model.Score;
        GameObject.Find("Game Canvas/Main Menu/Start Run").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
        Assert.That(session.Model.State,Is.EqualTo(RunState.SelectingCharacter));Assert.That(session.IsPlaying,Is.False);
        var layout=GameObject.Find("Game Canvas/Character Select/Arcade Layout");Assert.That(layout,Is.Not.Null);
        Assert.That(layout.transform.Find("Soldier Select Heading").GetComponent<UnityEngine.UI.Image>().sprite.name,Is.EqualTo("soldier_select_heading"));
        Assert.That(layout.transform.Find("P1 Marker"),Is.Not.Null);
        Assert.That(layout.GetComponentsInChildren<UnityEngine.UI.Button>().Length,Is.EqualTo(2),"One Dino portrait and Back are the only choices.");
        Assert.That(layout.transform.Find("Tagline"),Is.Null);Assert.That(layout.transform.Find("Abilities"),Is.Null);
        var picker=session.CharacterSelection;var initial=picker.PreviewSprite;yield return new WaitForSeconds(.18f);
        Assert.That(picker.PreviewSprite,Is.Not.SameAs(initial));Assert.That(session.Model.Score,Is.EqualTo(frozenScore));Capture("37-dino-character-select");
        var panel=(RectTransform)picker.transform;var originalSize=panel.sizeDelta;
        foreach(float inset in new[]{0f,400f,800f})
        {
            panel.sizeDelta=new Vector2(-inset,originalSize.y);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            var outer=new Vector3[4];var inner=new Vector3[4];panel.GetWorldCorners(outer);((RectTransform)layout.transform).GetWorldCorners(inner);
            Assert.That(inner[0].x,Is.GreaterThanOrEqualTo(outer[0].x-.01f));Assert.That(inner[2].x,Is.LessThanOrEqualTo(outer[2].x+.01f));
            Assert.That(inner[0].y,Is.GreaterThanOrEqualTo(outer[0].y-.01f));Assert.That(inner[2].y,Is.LessThanOrEqualTo(outer[2].y+.01f));
        }
        panel.sizeDelta=originalSize;yield return null;
        Assert.That(picker.HasDinoVoice,Is.True,"The supplied Dino take must be bound in the scene.");
        var voice=session.Audio.transform.Find("Dino Selection Voice").GetComponent<AudioSource>();
        Assert.That(voice,Is.Not.Null);Assert.That(voice.playOnAwake,Is.False);Assert.That(voice.loop,Is.False);
        picker.ConfirmDino();yield return new WaitForSeconds(.3f);
        Assert.That(session.Audio.SelectionVoicePlaying,Is.True);Assert.That(voice.clip.name,Is.EqualTo("Dino-Selection"));
        Assert.That(voice.clip.length,Is.InRange(1.9f,2.1f));
        Assert.That(session.Audio.MusicSource.volume,Is.LessThan(.08f),"Music must leave room for Dino's announcement.");
        int voicePosition=voice.timeSamples;picker.ConfirmDino();
        Assert.That(voice.timeSamples,Is.GreaterThanOrEqualTo(voicePosition),"Repeated confirmation must not restart the announcement.");
        Assert.That(picker.IsConfirming,Is.True);Assert.That(picker.PreviewSprite.name,Does.StartWith("mydino_laugh_"));Capture("38-dino-selection-laugh");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;Assert.That(session.Model.State,Is.EqualTo(RunState.MainMenu));
        Assert.That(session.Audio.SelectionVoicePlaying,Is.False,"Back must stop the voice immediately.");
        yield return new WaitForSeconds(voice.clip.length+.2f);Assert.That(session.Model.State,Is.EqualTo(RunState.MainMenu),"Cancelled selection must never deploy later.");
        session.OpenCharacterSelect();yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));yield return null;yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(1.6f);
        Assert.That(session.Model.State,Is.EqualTo(RunState.SelectingCharacter),"Deployment must wait for the entire announcement.");
        Assert.That(session.Audio.SelectionVoicePlaying,Is.True);
        float deploymentDeadline=Time.unscaledTime+3f;
        while(!session.IsPlaying && Time.unscaledTime<deploymentDeadline) yield return null;
        Assert.That(session.IsPlaying,Is.True);Assert.That(session.Model.Lives,Is.EqualTo(3));Assert.That(session.Maps.ActiveSector,Is.Zero);
        Assert.That(picker.gameObject.activeSelf,Is.False);
    }

    [UnityTest] public IEnumerator PixelHudKeepsLivesOnOneLineAndHitStopResetsAcrossMenus()
    {
        var lives=GameObject.Find("Game Canvas/In Game HUD/Lives").GetComponent<TMPro.TMP_Text>();
        foreach(int expected in new[]{3,2,1})
        {
            yield return null;Canvas.ForceUpdateCanvases();lives.ForceMeshUpdate();
            Assert.That(session.Model.Lives,Is.EqualTo(expected));
            Assert.That(lives.textInfo.lineCount,Is.EqualTo(1),"The pixel font must keep the complete life counter on one line.");
            Assert.That(lives.preferredWidth,Is.LessThanOrEqualTo(lives.rectTransform.rect.width));
            if(expected>1) { session.Hit();Assert.That(Time.timeScale,Is.LessThan(.1f));yield return new WaitForSecondsRealtime(.14f); }
        }
        Assert.That(Time.timeScale,Is.EqualTo(1));
        session.HitStop(.1f);session.TogglePause();Assert.That(Time.timeScale,Is.Zero);
        yield return new WaitForSecondsRealtime(.15f);Assert.That(Time.timeScale,Is.Zero);
        session.TogglePause();Assert.That(Time.timeScale,Is.EqualTo(1));
        session.HitStop(.1f);session.StartRun();Assert.That(Time.timeScale,Is.EqualTo(1));
        session.HitStop(.1f);session.MainMenu();Assert.That(Time.timeScale,Is.EqualTo(1));
        yield return new WaitForSecondsRealtime(.15f);Assert.That(Time.timeScale,Is.EqualTo(1));
        session.StartRun();yield return null;Capture("43-pixel-hud-and-life-counter");
    }

    [UnityTest] public IEnumerator OriginalMusicLoopsDucksForAnnouncementsPausesAndContinuesAcrossMaps()
    {
        var audio=session.Audio;var music=audio.MusicSource;yield return new WaitForSeconds(.4f);
        Assert.That(music,Is.Not.Null);Assert.That(music.clip.name,Is.EqualTo("main_theme"));Assert.That(music.loop,Is.True);Assert.That(music.isPlaying,Is.True);
        Assert.That(music.clip.length,Is.InRange(109,111));
        session.Player.Weapon.CollectHeavyMachineGun();yield return new WaitForSeconds(.3f);Assert.That(music.volume,Is.LessThan(.13f));
        session.TogglePause();yield return null;yield return new WaitForSecondsRealtime(.06f);int paused=music.timeSamples;yield return new WaitForSecondsRealtime(.15f);Assert.That(music.timeSamples,Is.EqualTo(paused));
        session.TogglePause();yield return new WaitForSeconds(.15f);Assert.That(music.isPlaying,Is.True);Assert.That(music.timeSamples,Is.GreaterThan(paused));
        int before=music.timeSamples;session.Model.Advance(1500);yield return new WaitForSeconds(.1f);Assert.That(music.timeSamples,Is.GreaterThan(before));
        audio.ToggleMusic();Assert.That(music.mute,Is.True);audio.ToggleMusic();Assert.That(music.mute,Is.False);
        before=music.timeSamples;session.StartRun();yield return new WaitForSeconds(.1f);Assert.That(music.timeSamples,Is.GreaterThan(before),"Restart must not replay the music intro.");
    }

    [UnityTest] public IEnumerator BikerUsesOriginalAnimationTakesFourHitsAndIsJumpableAtBothSpeeds()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();if(fast) session.Model.Advance(1000);session.Spawner.enabled=false;yield return new WaitForSeconds(.15f);
            var bike=SpawnMobile("RebelBiker",9);Assert.That(bike.HitPoints,Is.EqualTo(4));
            var image=bike.GetComponentInChildren<SpriteRenderer>();Assert.That(image.sprite.name,Does.StartWith("biker_drive_"));
            var pose=image.sprite;yield return new WaitForSeconds(.1f);Assert.That(image.sprite,Is.Not.SameAs(pose));
            float deadline=Time.time+2;
            while(bike.transform.position.x-session.Player.transform.position.x>(session.Model.BaseSpeed+2.5f)*.42f && Time.time<deadline) yield return null;
            session.Player.RequestJump();yield return new WaitForSeconds(.9f);Assert.That(session.Model.Lives,Is.EqualTo(3));
            bike.TakeProjectileHit();bike.TakeProjectileHit();bike.TakeProjectileHit();Assert.That(bike.HitPoints,Is.EqualTo(1));bike.TakeProjectileHit();
            Assert.That(bike.GetComponent<Collider2D>().enabled,Is.False);Assert.That(bike.TakeProjectileHit(),Is.False);yield return new WaitForSeconds(.5f);Assert.That(bike==null,Is.True);
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator FlyingTaraWarnsDamagesStandingRunnerOnceAndCanBeDodgedAtBothSpeeds()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true}) foreach(bool jump in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);yield return new WaitForSeconds(.15f);
            var plane=SpawnMobile("FlyingTara",6.4f);yield return null;Assert.That(plane.IsWindingUp,Is.True);
            session.TogglePause();yield return null;Vector3 frozen=plane.transform.position;yield return new WaitForSecondsRealtime(.1f);Assert.That(plane.transform.position,Is.EqualTo(frozen));session.TogglePause();
            yield return new WaitForSeconds(.95f);if(jump) session.Player.RequestJump();yield return new WaitForSeconds(.6f);
            Assert.That(plane.ShotsFired,Is.EqualTo(1));Assert.That(session.Model.Lives,Is.EqualTo(jump ? 3 : 2),"The warned strafing round must hit the lane and remain dodgeable.");
            Assert.That(plane.EncounterActive,Is.False);if(!fast && !jump) Capture("39-flying-tara-attack");
            yield return new WaitForSeconds(.5f);Assert.That(session.Model.Lives,Is.EqualTo(jump ? 3 : 2));
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator FlyingTaraPassesLeftAtConstantHeightAndClearsAfterTheStrafe()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);
            yield return new WaitForSeconds(.15f);var plane=SpawnMobile("FlyingTara",6.4f);float height=plane.transform.position.y;
            yield return new WaitForSeconds(.95f);session.Player.RequestJump();
            float deadline=Time.time+2;while(!plane.IsPassing && Time.time<deadline) yield return null;
            Assert.That(plane.IsPassing,Is.True);float firstX=plane.transform.position.x;
            session.TogglePause();yield return null;var frozen=plane.transform.position;
            yield return new WaitForSecondsRealtime(.1f);Assert.That(plane.transform.position,Is.EqualTo(frozen));session.TogglePause();
            bool passed=false;deadline=Time.time+4;
            while(plane!=null && Time.time<deadline)
            {
                Assert.That(plane.transform.position.y,Is.EqualTo(height).Within(.001f),"The new aircraft must never climb away.");
                Assert.That(plane.transform.position.x,Is.LessThanOrEqualTo(firstX+.001f),"It must continue past, without reversing to the right.");
                if(plane.transform.position.x<session.Player.transform.position.x-2 && !passed)
                { passed=true;if(!fast) Capture("42-flying-tara-passes-dino"); }
                yield return null;
            }
            Assert.That(passed,Is.True);Assert.That(plane==null,Is.True,"The pass must end in offscreen cleanup.");
            Assert.That(session.Model.Lives,Is.EqualTo(3));
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator ScoreMapsFlowAcrossTheViewAndResetWithoutMovingPlayerOrCamera()
    {
        session.StartRun();session.enabled=false;session.Spawner.enabled=false;
        var camera=Camera.main.transform.position;float runnerX=session.Player.transform.position.x;
        var stage=GameObject.Find("Metal Slug Terrain");var floor=GameObject.Find("Ground Collider").GetComponent<BoxCollider2D>();
        session.Model.Advance(1499.5f);yield return null;yield return null;
        Assert.That(session.Maps.ActiveSector,Is.Zero);Assert.That(session.Maps.IsTransitioning,Is.False);
        Object.FindAnyObjectByType<GameHud>().RefreshValues(session.Model,1);Capture("32-temple-map");
        session.Model.Advance(.5f);yield return null;yield return null;
        Assert.That(session.Maps.ActiveSector,Is.EqualTo(1));Assert.That(session.Maps.ActiveMapName,Is.EqualTo("CRASH COAST"));
        Assert.That(session.Maps.IsTransitioning,Is.True);Assert.That(session.Maps.BoundaryX,Is.GreaterThan(10));
        Assert.That(stage.GetComponentsInChildren<ParallaxLayer>().Length,Is.EqualTo(6),"Both landscapes stay alive while the boundary moves.");
        yield return new WaitForSeconds(.9f);Capture("35-flowing-map-boundary");
        session.TogglePause();yield return null;float boundary=session.Maps.BoundaryX;
        yield return new WaitForSecondsRealtime(.15f);Assert.That(session.Maps.BoundaryX,Is.EqualTo(boundary));
        session.TogglePause();
        float deadline=Time.time+4;while(session.Maps.IsTransitioning && Time.time<deadline) yield return null;
        Assert.That(session.Maps.IsTransitioning,Is.False);yield return null;
        Assert.That(stage.GetComponentsInChildren<ParallaxLayer>().Length,Is.EqualTo(3));
        Object.FindAnyObjectByType<GameHud>().RefreshValues(session.Model,1);Capture("36-crash-coast-map");
        string[] grounds={"ms1_stone_ground","ms1_beach_ground","ms2_rail_ground","ms2_factory_ground"};
        foreach(int score in new[]{6000,9000,12000,15000,18000,21000})
        {
            session.Model.Advance((score-session.Model.Score)/2f);yield return null;yield return null;
            Time.captureFramerate=10;for(int frame=0;frame<100;frame++) yield return null;Time.captureFramerate=0;
            Assert.That(session.Maps.IsTransitioning,Is.False);Assert.That(session.Maps.ActiveSector,Is.EqualTo(score/3000));
            var ground=stage.transform.Find("Stone Ground").GetComponentsInChildren<SpriteRenderer>();
            Assert.That(ground[1].sprite.name,Is.EqualTo(grounds[(score/3000)%4]));
            Assert.That(ground[1].bounds.max.y,Is.EqualTo(floor.bounds.max.y).Within(.001f));
            Assert.That(ground[1].bounds.min.y,Is.LessThanOrEqualTo(-6.75f));
            System.Array.Sort(ground,(a,b)=>a.bounds.min.x.CompareTo(b.bounds.min.x));
            for(int i=0;i<2;i++) Assert.That(ground[i].bounds.max.x,Is.EqualTo(ground[i+1].bounds.min.x).Within(.001f));
            if(score==6000 || score==9000)
            { Object.FindAnyObjectByType<GameHud>().RefreshValues(session.Model,1);Capture(score==6000 ? "33-harbor-map" : "34-iron-works-map"); }
        }
        Assert.That(session.Player.transform.position.x,Is.EqualTo(runnerX).Within(.001f));Assert.That(Camera.main.transform.position,Is.EqualTo(camera));Assert.That(session.Model.Lives,Is.EqualTo(3));
        session.Model.Advance(1500);yield return null;yield return null;Assert.That(session.Maps.IsTransitioning,Is.True);
        session.MainMenu();Assert.That(session.Maps.ActiveSector,Is.Zero);Assert.That(session.Maps.IsTransitioning,Is.False);
        session.StartRun();yield return null;Assert.That(stage.GetComponentsInChildren<ParallaxLayer>().Length,Is.EqualTo(3));
        Assert.That(stage.transform.Find("Stone Ground/Tile 1").GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("ms1_stone_ground"));session.enabled=true;
    }

    [UnityTest] public IEnumerator LaterMapsBringArmedReinforcementsWithJumpableSpacingAndEarlierVehicles()
    {
        session.StartRun();session.enabled=false;
        var spawner=session.Spawner;Assert.That(spawner.EncounterSpacing,Is.EqualTo(new Vector2(14,18)));Assert.That(spawner.VehicleInterval,Is.EqualTo(9));
        foreach(int sector in new[]{1,2,3})
        {
            session.StartRun();session.enabled=false;session.Model.Advance(sector*1500);spawner.enabled=true;
            Assert.That(spawner.EncounterSpacing.x,Is.GreaterThanOrEqualTo(12));
            Assert.That(spawner.EncounterSpacing.x/session.Model.BaseSpeed,Is.GreaterThan(1),"Keep more than a second between ordinary threats at maximum running speed.");
            Assert.That(spawner.EncounterSpacing.y,Is.LessThan(18));Assert.That(spawner.VehicleInterval,Is.LessThan(9));
            yield return new WaitForSeconds(.35f);
            var rifle=spawner.GetComponentInChildren<SoldierAttack>();Assert.That(rifle,Is.Not.Null,"Later maps open with armed infantry instead of an extra sandbag barrier.");
            spawner.enabled=false;
        }
        session.enabled=true;
    }

    private void QueueEncounter(int index)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(ObstacleSpawner).GetField("sequence",flags).SetValue(session.Spawner,index);
        typeof(ObstacleSpawner).GetField("remainingDistance",flags).SetValue(session.Spawner,0f);
        typeof(ObstacleSpawner).GetField("weaponDistance",flags).SetValue(session.Spawner,100f);
    }

    [UnityTest] public IEnumerator LaterMapsSpawnPairsAndTriosWithOneAttackerAndJumpableBodies()
    {
        foreach(var wave in new[]{new[]{0,1,1},new[]{1,1,2},new[]{2,1,2},new[]{2,3,3},new[]{2,6,3}})
        {
            session.StartRun();session.enabled=false;session.Spawner.enabled=false;yield return null;
            session.Model.Advance(wave[0]*1500);yield return null;
            Time.captureFramerate=10;for(int frame=0;frame<40;frame++) yield return null;Time.captureFramerate=0;
            QueueEncounter(wave[1]);session.Spawner.enabled=true;yield return null;yield return null;session.Spawner.enabled=false;
            var troops=session.Spawner.GetComponentsInChildren<RebelSoldier>();Assert.That(troops.Length,Is.EqualTo(wave[2]));
            Assert.That(session.Spawner.GetComponentsInChildren<SoldierAttack>().Length,Is.LessThanOrEqualTo(1),"Supports must not stack independent volleys on the lead warning.");
            System.Array.Sort(troops,(a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));
            for(int i=1;i<troops.Length;i++)
                Assert.That(troops[i].GetComponent<Collider2D>().bounds.min.x,Is.GreaterThan(troops[i-1].GetComponent<Collider2D>().bounds.max.x),"Bodies must have separate space in the formation.");
            if(wave[0]==2 && wave[1]==3)
            {
                yield return new WaitForSeconds(.8f);Object.FindAnyObjectByType<GameHud>().RefreshValues(session.Model,1);Capture("44-three-soldier-wave");
                yield return new WaitForSeconds(.65f);
                for(int i=1;i<troops.Length;i++) Assert.That(troops[i].GetComponent<Collider2D>().bounds.min.x,Is.GreaterThan(troops[i-1].GetComponent<Collider2D>().bounds.max.x),"Supports must retain their formation during the leader's attack.");
            }
            if(wave[0]==1)
            {
                var attack=troops[0].GetComponent<SoldierAttack>();float deadline=Time.time+3;
                while(!attack.IsWindingUp && Time.time<deadline) yield return null;
                Assert.That(attack.IsWindingUp,Is.True);yield return new WaitForSeconds(.56f);
                session.Player.RequestJump();yield return new WaitForSeconds(1.2f);
                Assert.That(session.Model.Lives,Is.EqualTo(3),"A late-red jump must clear the rifle volley and both soldiers without a gun.");
            }
            if(wave[0]==2 && wave[1]==6)
            {
                float deadline=Time.time+3;
                while(troops[0].transform.position.x-session.Player.transform.position.x>(session.WorldSpeed+1.25f)*.32f && Time.time<deadline) yield return null;
                session.Player.RequestJump();yield return new WaitForSeconds(1.1f);
                Assert.That(session.Model.Lives,Is.EqualTo(3),"One well-timed unarmed jump must clear the three bodies.");
                Assert.That(session.Player.IsGrounded,Is.True);
            }
        }
        session.enabled=true;
    }

    [UnityTest] public IEnumerator TankWaitsForTroopsAndOrdnanceAndBlocksWavesUntilItLeaves()
    {
#if UNITY_EDITOR
        session.StartRun();session.enabled=false;session.Model.Advance(3000);QueueEncounter(1);
        session.Spawner.enabled=true;yield return null;yield return null;session.Spawner.enabled=false;
        var troops=session.Spawner.GetComponentsInChildren<RebelSoldier>();Assert.That(troops.Length,Is.EqualTo(2));
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(ObstacleSpawner).GetField("encountersSinceVehicle",flags).SetValue(session.Spawner,session.Spawner.VehicleInterval-1);
        QueueEncounter(1);session.Spawner.enabled=true;yield return null;yield return null;
        Assert.That(session.Spawner.GetComponentInChildren<VehicleEnemy>(),Is.Null,"Tank must wait for the preceding group.");
        session.Spawner.enabled=false;foreach(var troop in troops) Object.Destroy(troop.gameObject);yield return null;
        var projectile=new GameObject("Remaining enemy round",typeof(SpriteRenderer),typeof(EnemyOrdnance));projectile.transform.SetParent(session.Spawner.transform,false);projectile.transform.position=new Vector3(12,0,0);
        projectile.GetComponent<EnemyOrdnance>().Launch(session,false,UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/enemy_bullet.png"),null,session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial);
        session.Spawner.enabled=true;yield return null;yield return null;Assert.That(session.Spawner.GetComponentInChildren<VehicleEnemy>(),Is.Null,"Tank must also wait for the group's remaining rounds.");
        Object.Destroy(projectile);yield return null;yield return null;
        var tank=session.Spawner.GetComponentInChildren<VehicleEnemy>();Assert.That(tank,Is.Not.Null);Assert.That(tank.Kind,Is.EqualTo(VehicleEnemy.VehicleKind.Tank));
        QueueEncounter(1);yield return new WaitForSeconds(.2f);Assert.That(session.Spawner.GetComponentsInChildren<RebelSoldier>(),Is.Empty);
        typeof(VehicleEnemy).GetField("retreating",flags).SetValue(tank,true);
        Assert.That(tank.EncounterActive,Is.False);yield return new WaitForSeconds(.2f);
        Assert.That(session.Spawner.GetComponentsInChildren<RebelSoldier>(),Is.Empty,"Retreating armour still owns its clear lane until it leaves.");
        Object.Destroy(tank.gameObject);yield return null;yield return null;
        Assert.That(session.Spawner.GetComponentsInChildren<RebelSoldier>().Length,Is.EqualTo(2),"Infantry must resume after the tank clears.");
        session.StartRun();session.Spawner.enabled=false;session.enabled=true;yield return null;
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator EveryMapKeepsGroundAndBackgroundTilesJoinedWhileScrolling()
    {
        session.StartRun();session.enabled=false;session.Spawner.enabled=false;
        var terrain=GameObject.Find("Metal Slug Terrain").transform;
        float floor=GameObject.Find("Ground Collider").GetComponent<Collider2D>().bounds.max.y;
        for(int sector=0;sector<4;sector++)
        {
            session.Model.Advance((sector*3000-session.Model.Score)/2f);yield return null;
            Time.captureFramerate=5;
            for(int frame=0;frame<85;frame++)
            {
                yield return null;
                if(session.Maps.IsTransitioning) continue;
                foreach(var layerName in new[]{"Far Jungle","Near Jungle","Stone Ground"})
                {
                    var tiles=terrain.Find(layerName).GetComponentsInChildren<SpriteRenderer>();
                    if(!tiles[0].enabled) { Assert.That(sector==3 && layerName=="Near Jungle",Is.True);continue; }
                    System.Array.Sort(tiles,(a,b)=>a.bounds.min.x.CompareTo(b.bounds.min.x));
                    for(int i=1;i<tiles.Length;i++) Assert.That(tiles[i].bounds.min.x,Is.EqualTo(tiles[i-1].bounds.max.x).Within(.002f),layerName+" gap on map "+sector);
                    Assert.That(tiles[0].bounds.min.x,Is.LessThan(-14));Assert.That(tiles[2].bounds.max.x,Is.GreaterThan(14));
                    if(layerName=="Stone Ground") foreach(var tile in tiles) Assert.That(tile.bounds.max.y,Is.EqualTo(floor).Within(.001f));
                }
            }
            Time.captureFramerate=0;Assert.That(session.Maps.ActiveSector,Is.EqualTo(sector));Assert.That(session.Maps.IsTransitioning,Is.False);
        }
        session.enabled=true;
    }

    [UnityTest] public IEnumerator OriginalMissionTerrainCoversTheViewAlignsWithPhysicsAndScrollsWithoutGaps()
    {
        Assert.That(GameObject.Find("Dusk Sky"),Is.Null);Assert.That(GameObject.Find("Setting Sun"),Is.Null);
        Assert.That(GameObject.Find("Far Mountains"),Is.Null);Assert.That(GameObject.Find("Dunes"),Is.Null);
        var root=GameObject.Find("Metal Slug Terrain");Assert.That(root,Is.Not.Null);
        var layers=root.GetComponentsInChildren<ParallaxLayer>();Assert.That(layers.Length,Is.EqualTo(3));
        var floor=GameObject.Find("Ground Collider").GetComponent<BoxCollider2D>();float floorY=floor.bounds.max.y;
        foreach(var layer in layers)
        {
            Assert.That(layer.transform.childCount,Is.EqualTo(3));
            foreach(var image in layer.GetComponentsInChildren<SpriteRenderer>())
            { Assert.That(image.sprite.name,Does.StartWith("ms1_"));Assert.That(image.sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(image.color,Is.EqualTo(Color.white)); }
        }
        var ground=root.transform.Find("Stone Ground").GetComponentsInChildren<SpriteRenderer>();
        Assert.That(ground[1].bounds.max.y,Is.EqualTo(floorY).Within(.001f));Assert.That(ground[1].bounds.min.y,Is.LessThanOrEqualTo(-6.75f));
        foreach(var prop in session.Presentation.transform.Find("Metal Slug Scenery").GetComponentsInChildren<SpriteRenderer>())
        { Assert.That(prop.enabled,Is.False,"Village props do not belong in the temple environment.");Assert.That(prop.sortingOrder,Is.LessThan(ground[1].sortingOrder)); }
        Assert.That(session.Presentation.transform.Find("Drifting Cloud 0").GetComponent<SpriteRenderer>().enabled,Is.False);
        session.Model.Advance(1000);Time.captureFramerate=10;
        for(int frame=0;frame<100;frame++) yield return null;
        Time.captureFramerate=0;
        System.Array.Sort(ground,(a,b)=>a.bounds.min.x.CompareTo(b.bounds.min.x));
        for(int i=0;i<2;i++) Assert.That(ground[i].bounds.max.x,Is.EqualTo(ground[i+1].bounds.min.x).Within(.001f),"Ground tiles must stay joined after wrapping.");
        Assert.That(ground[0].bounds.min.x,Is.LessThan(-12));Assert.That(ground[2].bounds.max.x,Is.GreaterThan(12));
        session.StartRun();
        Assert.That(root.transform.Find("Stone Ground/Tile 1").position.x,Is.Zero);
        yield return null;Capture("30-original-metal-slug-terrain");
    }

    [UnityTest] public IEnumerator NativeSandbagBarricadeMatchesTheGroundTakesThreeHitsAndCanBeJumped()
    {
#if UNITY_EDITOR
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GroundObstacle>("Assets/Game/Prefabs/SandbagBarricade.prefab");
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);
            var bag=Object.Instantiate(prefab,new Vector3(session.Player.transform.position.x+7,-3.2f,0),Quaternion.identity,session.Spawner.transform);bag.Initialize(session);
            var art=bag.GetComponentInChildren<SpriteRenderer>();Assert.That(art.sprite.name,Is.EqualTo("ms1_sandbag_barricade"));
            Assert.That(art.bounds.min.y,Is.EqualTo(-3.2f).Within(.04f));Assert.That(bag.HitPoints,Is.EqualTo(3));
            if(!fast) Capture("31-native-sandbag-barricade");
            float limit=Time.time+3;while(bag.transform.position.x-session.Player.transform.position.x>session.Model.BaseSpeed*.42f && Time.time<limit) yield return null;
            session.Player.RequestJump();yield return new WaitForSeconds(1.05f);Assert.That(session.Model.Lives,Is.EqualTo(3),"The native barrier must remain jumpable at both run speeds.");
            bag.GetComponent<Rigidbody2D>().position=new Vector2(session.Player.transform.position.x+6,-3.2f);
            bag.TakeProjectileHit();bag.TakeProjectileHit();Assert.That(bag.HitPoints,Is.EqualTo(1));Assert.That(bag.GetComponent<Collider2D>().enabled,Is.True);
            bag.TakeProjectileHit();yield return null;Assert.That(bag==null,Is.True);
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator HelicopterBombHitsStandingRunnerAtBothSpeedsAndChargesOnlyOnce()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);
            var helicopter=SpawnVehicle("RShobuHelicopter",5.8f);
            EnemyOrdnance bomb=null;float deadline=Time.time+3;
            while(Time.time<deadline)
            {
                bomb=Object.FindAnyObjectByType<EnemyOrdnance>();
                if(bomb!=null && bomb.IsExploding) break;
                yield return null;
            }
            Assert.That(bomb!=null && bomb.IsExploding,Is.True);
            Assert.That(session.Model.Lives,Is.EqualTo(2),"Standing in the bomb blast must cost one life: fast="+fast+", blastX="+bomb.transform.position.x+", playerX="+session.Player.transform.position.x);
            yield return new WaitForSeconds(.2f);
            Assert.That(session.Model.Lives,Is.EqualTo(2),"One blast cannot repeatedly charge damage.");
            Capture(fast ? "29-helicopter-bomb-damage-fast" : "28-helicopter-bomb-damage");
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator LargeVehicleEncountersAlternateSupplyAmmoAndClearOnRestart()
    {
#if UNITY_EDITOR
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        foreach(int sector in new[]{0,1,2}) foreach(var index in new[]{0,1})
        {
            session.StartRun();session.Spawner.enabled=false;
            session.Model.Advance(sector*1500);
            typeof(ObstacleSpawner).GetField("encountersSinceVehicle",flags).SetValue(session.Spawner,session.Spawner.VehicleInterval-1);
            typeof(ObstacleSpawner).GetField("vehicleSequence",flags).SetValue(session.Spawner,index);
            typeof(ObstacleSpawner).GetField("remainingDistance",flags).SetValue(session.Spawner,0f);
            typeof(ObstacleSpawner).GetField("weaponDistance",flags).SetValue(session.Spawner,100f);
            session.Spawner.enabled=true;yield return null;yield return null;
            var vehicle=Object.FindAnyObjectByType<VehicleEnemy>();Assert.That(vehicle,Is.Not.Null);
            Assert.That(vehicle.Kind,Is.EqualTo(index==0 ? VehicleEnemy.VehicleKind.Tank : VehicleEnemy.VehicleKind.Helicopter));
            Assert.That(Object.FindObjectsByType<HeavyMachineGunPickup>().Length,Is.EqualTo(1));
            int sequence=(int)typeof(ObstacleSpawner).GetField("sequence",flags).GetValue(session.Spawner);
            yield return new WaitForSeconds(.35f);
            Assert.That((int)typeof(ObstacleSpawner).GetField("sequence",flags).GetValue(session.Spawner),Is.EqualTo(sequence),"Normal hazards must leave this encounter clear.");
            session.StartRun();session.Spawner.enabled=false;yield return null;
            Assert.That(Object.FindObjectsByType<VehicleEnemy>(),Is.Empty);
            Assert.That(Object.FindObjectsByType<EnemyOrdnance>(),Is.Empty);
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator TankHasArmourAnimatedTracksPauseAndOneDestructionExplosion()
    {
#if UNITY_EDITOR
        var vehicle=SpawnVehicle("DiCokkaTank",12);var visual=vehicle.transform.Find("Visual").GetComponent<SpriteRenderer>();
        Assert.That(vehicle.HitPoints,Is.EqualTo(16));Assert.That(visual.bounds.size.x,Is.GreaterThan(3));
        var first=visual.sprite;yield return new WaitForSeconds(.15f);Assert.That(visual.sprite,Is.Not.EqualTo(first));
        vehicle.GetComponent<Rigidbody2D>().position=new Vector2(session.Player.transform.position.x+5.8f,-3.2f);
        yield return new WaitForSeconds(.15f);Capture("24-di-cokka-tank");
        var camera=Camera.main.transform.position;var position=vehicle.transform.position;var pose=visual.sprite;
        session.TogglePause();yield return new WaitForSecondsRealtime(.2f);
        Assert.That(vehicle.transform.position,Is.EqualTo(position));Assert.That(visual.sprite,Is.EqualTo(pose));Assert.That(vehicle.ShotsFired,Is.Zero);
        session.TogglePause();int explosions=session.Audio.ExplosionCount;
        for(int i=0;i<15;i++) vehicle.TakeProjectileHit();Assert.That(vehicle.HitPoints,Is.EqualTo(1));
        Assert.That(session.Audio.ExplosionCount,Is.EqualTo(explosions));
        vehicle.TakeProjectileHit();Assert.That(vehicle.TakeProjectileHit(),Is.False);
        Assert.That(vehicle.GetComponent<Collider2D>().enabled,Is.False);Assert.That(session.Audio.ExplosionCount,Is.EqualTo(explosions+1));
        yield return new WaitForSeconds(.15f);Capture("25-tank-destruction");
        Assert.That(visual.sprite.name,Does.StartWith("tank_death"));Assert.That(Camera.main.transform.position,Is.EqualTo(camera));
        yield return new WaitForSeconds(1.2f);Assert.That(vehicle==null,Is.True);
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator RealGunfireDestroysTankAndAirborneFireCanReachTheHelicopter()
    {
#if UNITY_EDITOR
        var weapon=session.Player.Weapon;var tank=SpawnVehicle("DiCokkaTank",5.8f);
        weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);
        float deadline=Time.time+3;while(tank.HitPoints>0 && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);Assert.That(tank.HitPoints,Is.Zero);Assert.That(weapon.ShotsFired,Is.GreaterThanOrEqualTo(16));
        session.StartRun();session.Spawner.enabled=false;yield return null;
        var closeTank=SpawnVehicle("DiCokkaTank",5.8f);weapon.CollectShotgun();weapon.SetFireHeld(true);
        deadline=Time.time+4;while(closeTank.HitPoints>0 && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);Assert.That(closeTank.HitPoints,Is.Zero,"Close shotgun volleys must damage the same armour through real pellets.");
        Assert.That(weapon.Ammo,Is.GreaterThanOrEqualTo(25));
        session.StartRun();session.Spawner.enabled=false;yield return null;
        var heli=SpawnVehicle("RShobuHelicopter",5.8f);var renderer=heli.transform.Find("Visual").GetComponent<SpriteRenderer>();
        weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);yield return new WaitForSeconds(.45f);
        Assert.That(heli.HitPoints,Is.EqualTo(12),"Ground-level HMG rounds pass below the airframe.");
        for(int jump=0;jump<3 && heli.HitPoints>0;jump++)
        {
            session.Player.RequestJump();yield return new WaitForSeconds(.38f);
            if(jump==0) Capture("26-r-shobu-airborne-fight");
            yield return new WaitForSeconds(.55f);
        }
        weapon.SetFireHeld(false);Assert.That(heli.HitPoints,Is.Zero,"Jumping and firing must defeat the helicopter within one magazine.");
        Assert.That(weapon.Ammo,Is.GreaterThan(0));Assert.That(heli.GetComponent<Collider2D>().enabled,Is.False);
        Capture("27-helicopter-destruction");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator UnarmedRunnerCanJumpCannonShellsAndVehiclesEventuallyRetreat()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);
            var tank=SpawnVehicle("DiCokkaTank",5.8f);
            for(int volley=0;volley<3;volley++)
            {
                float deadline=Time.time+3;while(!tank.IsWindingUp && Time.time<deadline) yield return null;
                yield return new WaitForSeconds(.72f);session.Player.RequestJump();
                yield return new WaitForSeconds(.9f);
                Assert.That(session.Model.Lives,Is.EqualTo(3),"A warned cannon shell must be dodgeable without a weapon, fast="+fast);
            }
            float retreatDeadline=Time.time+3;while(!tank.IsRetreating && Time.time<retreatDeadline) yield return null;
            Assert.That(tank.IsRetreating,Is.True);Assert.That(tank.ShotsFired,Is.EqualTo(3));
        }
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;yield return null;if(fast) session.Model.Advance(1000);
            var heli=SpawnVehicle("RShobuHelicopter",5.8f);
            float limit=Time.time+7;
            while(!heli.IsRetreating && Time.time<limit)
            {
                foreach(var bomb in Object.FindObjectsByType<EnemyOrdnance>())
                    if(bomb.IsGrenade && bomb.FuseRemaining<.38f && !bomb.IsExploding && session.Player.IsGrounded) session.Player.RequestJump();
                yield return null;
            }
            Assert.That(heli.IsRetreating,Is.True);Assert.That(session.Model.Lives,Is.EqualTo(3),"The flashing bombs must be jumpable without a weapon, fast="+fast);
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator AuthoredFootstepsAndLandingDipRespectAirbornePauseAndRestart()
    {
        var feel=session.Presentation;var weapon=session.Player.Weapon;
        var visual=session.Player.transform.Find("Visual");Vector3 camera=Camera.main.transform.position;
        yield return new WaitForSeconds(.5f);Assert.That(feel.FootstepCount,Is.GreaterThan(0));
        session.Player.RequestJump();yield return new WaitForSeconds(.2f);
        int steps=feel.FootstepCount;Assert.That(session.Player.IsGrounded,Is.False);
        yield return new WaitForSeconds(.2f);Assert.That(feel.FootstepCount,Is.EqualTo(steps));
        float deadline=Time.time+2;while(!weapon.IsLandingDip && Time.time<deadline) yield return null;
        Assert.That(weapon.IsLandingDip,Is.True);yield return null;
        Vector3 dip=visual.localPosition;Assert.That(dip.y,Is.EqualTo(-1f/16f).Within(.001f));
        session.TogglePause();yield return null;yield return new WaitForSecondsRealtime(.15f);
        Assert.That(visual.localPosition,Is.EqualTo(dip));Assert.That(weapon.IsLandingDip,Is.True);
        Assert.That(Camera.main.transform.position,Is.EqualTo(camera));
        session.TogglePause();yield return new WaitForSeconds(.15f);Assert.That(weapon.IsLandingDip,Is.False);
        session.StartRun();Assert.That(feel.FootstepCount,Is.Zero);Assert.That(feel.ActiveEffectCount,Is.Zero);
        Assert.That(weapon.IsLandingDip,Is.False);
    }

    [UnityTest] public IEnumerator CameraStaysFixedAndMapSpritesStayOpaqueDuringJumpAndCombat()
    {
        var feel=session.Presentation;Vector3 camera=Camera.main.transform.position;
        Assert.That(feel.transform.Find("Foreground Rocks And Scrub"),Is.Null);
        var props=GameObject.Find("Metal Slug Terrain").GetComponentsInChildren<SpriteRenderer>();
        Assert.That(props.Length,Is.EqualTo(9));
        session.Player.RequestJump();session.Player.RequestDash();
        session.Player.Weapon.CollectShotgun();session.Player.Weapon.SetFireHeld(true);
        feel.Exploded(session.Player.transform.position+Vector3.right*3);feel.PlayerHit();
        for(int i=0;i<50;i++)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(Camera.main.transform.position,Is.EqualTo(camera));
            foreach(var prop in props) { Assert.That(prop.enabled,Is.True);Assert.That(prop.color.a,Is.EqualTo(1));Assert.That(prop.sprite,Is.Not.Null); }
        }
        session.Player.Weapon.SetFireHeld(false);
        Capture("20-original-scenery-fixed-camera");
    }

    [UnityTest] public IEnumerator SoldierMedicineDropIsOnceOnlyHealsOneLifeAndResets()
    {
#if UNITY_EDITOR
        session.Hit();session.Hit();
        var config=new UnityEditor.SerializedObject(session.Spawner);
        config.FindProperty("medicineDropChance").floatValue=1;config.ApplyModifiedPropertiesWithoutUndo();
        var enemy=SpawnCombatSoldier("ShieldSoldier",6);
        enemy.TakeProjectileHit();enemy.TakeProjectileHit();Assert.That(Object.FindObjectsByType<HealthPickup>(),Is.Empty);
        enemy.TakeProjectileHit();Assert.That(enemy.TakeProjectileHit(),Is.False);
        var drops=Object.FindObjectsByType<HealthPickup>();Assert.That(drops.Length,Is.EqualTo(1));
        var drop=drops[0];var position=drop.transform.position;
        session.TogglePause();yield return new WaitForSecondsRealtime(.2f);Assert.That(drop.transform.position,Is.EqualTo(position));
        session.TogglePause();
        Capture("21-medicine-drop");
        drop.GetComponent<Rigidbody2D>().position=new Vector2(session.Player.transform.position.x,-2.65f);
        yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
        Assert.That(session.Model.Lives,Is.EqualTo(2));Assert.That(drop==null,Is.True);
        yield return new WaitForFixedUpdate();Assert.That(session.Model.Lives,Is.EqualTo(2));
        var again=session.Spawner.TryDropMedicine(session.Player.transform.position+Vector3.right*8);Assert.That(again,Is.Not.Null);
        session.StartRun();yield return null;Assert.That(Object.FindObjectsByType<HealthPickup>(),Is.Empty);
        Assert.That(session.Spawner.TryDropMedicine(Vector3.zero),Is.Null,"Full-health players do not generate useless drops.");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator ReinforcementsAppearInCourseAndShieldDiesAfterThreeHits()
    {
#if UNITY_EDITOR
        var config=new UnityEditor.SerializedObject(session.Spawner);
        typeof(ObstacleSpawner).GetField("sequence",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session.Spawner,6);
        typeof(ObstacleSpawner).GetField("remainingDistance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session.Spawner,0f);
        session.Spawner.enabled=true;yield return null;yield return null;session.Spawner.enabled=false;
        var shield=GameObject.Find("ShieldSoldier(Clone)").GetComponent<RebelSoldier>();Assert.That(shield,Is.Not.Null);
        typeof(ObstacleSpawner).GetField("remainingDistance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(session.Spawner,0f);
        session.Spawner.enabled=true;yield return null;yield return null;session.Spawner.enabled=false;
        var bazooka=GameObject.Find("BazookaSoldier(Clone)").GetComponent<SoldierAttack>();Assert.That(bazooka.Kind,Is.EqualTo(SoldierAttack.Weapon.Bazooka));
        shield.GetComponent<Rigidbody2D>().position=new Vector2(0,-3.2f);bazooka.GetComponent<Rigidbody2D>().position=new Vector2(6,-3.2f);
        var sprite=shield.GetComponentInChildren<SpriteRenderer>();var first=sprite.sprite;
        yield return new WaitForSeconds(.15f);Assert.That(sprite.sprite,Is.Not.EqualTo(first));Capture("22-shield-and-bazooka");
        shield.TakeProjectileHit();shield.TakeProjectileHit();Assert.That(shield.HitPoints,Is.EqualTo(1));
        shield.TakeProjectileHit();Assert.That(shield.GetComponent<Collider2D>().enabled,Is.False);
        yield return new WaitForSeconds(.2f);Assert.That(sprite.sprite.name,Does.StartWith("shield_death"));
        var late=SpawnCombatSoldier("BazookaSoldier",6);late.TakeProjectileHit();late.TakeProjectileHit();late.TakeProjectileHit();
        yield return new WaitForSeconds(.8f);Assert.That(Object.FindObjectsByType<EnemyOrdnance>(),Is.Empty,"Defeat must cancel an unfinished launch.");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator UnarmedJumpClearsBazookaRocketAndBodyAtBothRunSpeeds()
    {
#if UNITY_EDITOR
        foreach(bool fast in new[]{false,true})
        {
            session.StartRun();session.Spawner.enabled=false;if(fast) session.Model.Advance(1000);
            var enemy=SpawnCombatSoldier("BazookaSoldier",14);var attack=enemy.GetComponent<SoldierAttack>();
            float deadline=Time.time+4;while(!attack.IsWindingUp && Time.time<deadline) yield return null;
            Assert.That(attack.IsWindingUp,Is.True);yield return new WaitForSeconds(.49f);session.Player.RequestJump();
            yield return new WaitForSeconds(.25f);
            var rocket=Object.FindAnyObjectByType<EnemyOrdnance>();Assert.That(rocket,Is.Not.Null);Assert.That(rocket.IsRocket,Is.True);
            if(!fast) Capture("23-bazooka-dodge");
            yield return new WaitForSeconds(1);
            Assert.That(attack.ShotsFired,Is.EqualTo(1));Assert.That(session.Model.Lives,Is.EqualTo(3),"One warned jump must clear rocket and body.");
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator SceneryHasDepthFreezesOnPauseAndResetsWithoutChangingRunPhysics()
    {
        var feel=session.Presentation;Assert.That(feel,Is.Not.Null);
        var sky=GameObject.Find("Metal Slug Terrain/Far Jungle/Tile 1").GetComponent<SpriteRenderer>().bounds;
        Assert.That(sky.min.x,Is.LessThanOrEqualTo(-12f-.1875f),"Sky must cover the full camera kick at the left edge.");
        Assert.That(sky.max.x,Is.GreaterThanOrEqualTo(12f+.1875f));
        Assert.That(sky.max.y,Is.GreaterThanOrEqualTo(6.75f));
        var ground=GameObject.Find("Metal Slug Terrain/Stone Ground/Tile 1").transform;
        Assert.That(ground.GetComponent<SpriteRenderer>().bounds.min.y,Is.LessThanOrEqualTo(-6.75f));
        var near=feel.transform.Find("Metal Slug Scenery/Prop 0");
        var cloud=feel.transform.Find("Drifting Cloud 0");
        var sand=feel.transform.Find("Wind And Sand/Wind Sand 0");
        Assert.That(feel.GetComponentsInChildren<Collider2D>(),Is.Empty,"Decoration must never become an invisible gameplay obstacle.");
        Vector3 first=ground.position;float farX=cloud.position.x;float rootX=session.Player.transform.position.x;
        float speed=session.WorldSpeed;yield return new WaitForSeconds(.25f);
        Assert.That(first.x-ground.position.x,Is.GreaterThan(speed*.22f),"Terrain must scroll at the world's running speed.");
        Assert.That(Mathf.Abs(farX-cloud.position.x),Is.LessThan(.3f),"Clouds remain distant.");
        Assert.That(session.Player.transform.position.x,Is.EqualTo(rootX).Within(.001f));
        Assert.That(session.Player.GetComponent<Rigidbody2D>().gravityScale,Is.EqualTo(3.2f));
        session.Player.Weapon.CollectShotgun();session.Player.Weapon.SetFireHeld(true);
        float deadline=Time.time+1;while (feel.CasingsEmitted==0 && Time.time<deadline) yield return null;
        session.Player.Weapon.SetFireHeld(false);yield return null;
        session.TogglePause();yield return null;
        Vector3 camera=Camera.main.transform.position,prop=near.position,wind=sand.position;
        var casing=feel.transform.Find("Pooled Combat Debris/Debris 0");Vector3 shell=casing.position;
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(Camera.main.transform.position,Is.EqualTo(camera));Assert.That(near.position,Is.EqualTo(prop));
        Assert.That(sand.position,Is.EqualTo(wind));Assert.That(casing.position,Is.EqualTo(shell));
        session.StartRun();Assert.That(feel.ActiveEffectCount,Is.Zero);yield return null;
        Assert.That(feel.CasingsEmitted,Is.Zero);
        Assert.That(feel.CameraOffset.sqrMagnitude,Is.LessThan(.002f));
        Assert.That(session.Player.transform.position.x,Is.EqualTo(rootX).Within(.001f));
    }

    [UnityTest] public IEnumerator ActualWeaponShotsEmitCasingsAndHitSparksWithBoundedCameraFeedback()
    {
#if UNITY_EDITOR
        var feel=session.Presentation;var weapon=session.Player.Weapon;
        float rootX=session.Player.transform.position.x;
        weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);
        float deadline=Time.time+1;
        while (weapon.ShotsFired<3 && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);yield return null;
        Assert.That(feel.CasingsEmitted,Is.EqualTo(weapon.ShotsFired));
        int hits=feel.ImpactCount;
        weapon.CollectShotgun();yield return new WaitForSeconds(.27f);
        var enemy=SpawnCombatSoldier("RifleSoldier",4.5f);
        int fired=weapon.ShotsFired;weapon.SetFireHeld(true);
        deadline=Time.time+1;while (weapon.ShotsFired==fired && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);yield return null;
        Assert.That(feel.CameraOffset,Is.EqualTo(Vector3.zero),"Shots must never move the screen.");
        Assert.That(feel.CameraOffset.magnitude,Is.LessThan(.32f));
        yield return new WaitForSeconds(.28f);
        Assert.That(enemy.HitPoints,Is.Zero);Assert.That(feel.ImpactCount,Is.GreaterThan(hits));
        Assert.That(feel.ActiveEffectCount,Is.InRange(1,feel.EffectCapacity));
        Assert.That(session.Player.transform.position.x,Is.EqualTo(rootX).Within(.001f));
        // Stop fresh footstep emissions while testing that existing debris expires.
        session.Player.enabled=false;yield return new WaitForSeconds(1.4f);
        Assert.That(feel.ActiveEffectCount,Is.Zero);Assert.That(feel.CameraOffset.sqrMagnitude,Is.LessThan(.002f));
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator CaptureSceneryAndCombatMotionForVisualReview()
    {
#if UNITY_EDITOR
        string folder=Path.Combine(Application.dataPath,"..","Docs","Screenshots","Presentation-Motion");Directory.CreateDirectory(folder);
        var weapon=session.Player.Weapon;
        Time.captureFramerate=24;
        weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);
        for (int frame=0;frame<144;frame++)
        {
            if (frame==8 || frame==30) SpawnCombatSoldier("RebelSoldier",6);
            if (frame==40) weapon.SetFireHeld(false);
            if (frame==60) SpawnCombatSoldier("Grenadier",14);
            if (frame==65) { weapon.CollectShotgun();weapon.SetFireHeld(true); }
            if (frame==96) SpawnCombatSoldier("RifleSoldier",5);
            if (frame==108) session.Player.RequestJump();
            yield return null;
            Capture("Presentation-Motion/frame_"+frame.ToString("D4"));
        }
        weapon.SetFireHeld(false);Time.captureFramerate=0;
        Assert.That(session.Presentation.CasingsEmitted,Is.GreaterThan(12));
        Assert.That(session.Presentation.ImpactCount,Is.GreaterThan(0));
        Assert.That(session.Presentation.ExplosionCount,Is.GreaterThan(0));
        Assert.That(session.Presentation.ActiveEffectCount,Is.LessThanOrEqualTo(session.Presentation.EffectCapacity));
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator SpaceJumpsLandsAndDoesNotDoubleJump()
    {
        float initial = session.Player.transform.position.y;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); yield return null;
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(session.Player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        yield return new WaitForSeconds(.2f);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
        float before = session.Player.GetComponent<Rigidbody2D>().linearVelocity.y;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); yield return null;
        yield return new WaitForFixedUpdate();
        Assert.That(session.Player.GetComponent<Rigidbody2D>().linearVelocity.y, Is.LessThan(before));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSeconds(1f);
        Assert.That(session.Player.IsGrounded, Is.True);
        Assert.That(session.Player.transform.position.y, Is.EqualTo(initial).Within(.06f));
    }

    [UnityTest] public IEnumerator ShiftDashesAndPauseFreezesCooldownAndPhysics()
    {
        float normal = session.WorldSpeed;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift)); yield return null;
        Assert.That(session.Player.IsDashing, Is.True); Assert.That(session.WorldSpeed, Is.GreaterThan(normal));
        session.TogglePause();
        float cooldown = session.Player.DashReadyFraction; float distance = session.Model.Distance;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(session.Player.DashReadyFraction, Is.EqualTo(cooldown)); Assert.That(session.Model.Distance, Is.EqualTo(distance));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        session.StartRun(); yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(session.Player.IsDashing, Is.False);
        Assert.That(session.Player.DashReadyFraction, Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator PhysicalObstacleOnlyCostsOneLifeAndThirdHitPlaysDeath()
    {
        var obstacle = new GameObject("Test Ground Obstacle");
        obstacle.transform.position = session.Player.transform.position + Vector3.up * .6f;
        var body = obstacle.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var trigger = obstacle.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = Vector2.one;
        var hazard = obstacle.AddComponent<GroundObstacle>(); hazard.Initialize(session);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(session.Model.Lives, Is.EqualTo(2));
        yield return new WaitForSeconds(.05f);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Hit"), Is.True);
        yield return new WaitForSeconds(.15f); Assert.That(session.Model.Lives, Is.EqualTo(2));
        session.Hit(); session.Hit(); yield return null;
        Assert.That(session.Model.State, Is.EqualTo(RunState.Dying));
        yield return new WaitForSeconds(.15f);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death"), Is.True);
        yield return new WaitForSeconds(1.1f);
        Assert.That(session.Model.State, Is.EqualTo(RunState.GameOver));
        Object.Destroy(obstacle);
        session.StartRun(); yield return null;
        Assert.That(session.Model.Lives, Is.EqualTo(3)); Assert.That(session.Model.State, Is.EqualTo(RunState.Playing));
    }

    [UnityTest] public IEnumerator RecordSurvivesSceneReloadAndMenusHaveNewInputModule()
    {
        int target = originalRecord + 123;
        session.Model.Advance(target);
        int expected = session.Model.HighScore;
        session.TogglePause();
        yield return SceneManager.LoadSceneAsync("DinoSlug"); yield return null;
        session = Object.FindAnyObjectByType<GameSession>();
        Assert.That(session.Model.HighScore, Is.EqualTo(expected)); Assert.That(session.Model.State, Is.EqualTo(RunState.MainMenu));
        Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length, Is.EqualTo(1));
        Assert.That(Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<UnityEngine.EventSystems.StandaloneInputModule>(), Is.Null);
    }

    [UnityTest] public IEnumerator HPhysicalPickupEquipsOnceAndAnnounces()
    {
        var weapon = session.Player.Weapon;
        Assert.That(weapon, Is.Not.Null);
        var item = new GameObject("Test H Pickup", typeof(HeavyMachineGunPickup));
        item.transform.position = session.Player.transform.position + Vector3.up * .6f;
        item.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        item.GetComponent<BoxCollider2D>().isTrigger = true;
        item.GetComponent<HeavyMachineGunPickup>().Initialize(session);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
        Assert.That(weapon.Ammo, Is.EqualTo(50));
        Assert.That(weapon.ShotsFired, Is.Zero, "Picking up H must not fire automatically.");
        Assert.That(weapon.PickupVoicePlaying, Is.True);
        yield return new WaitForSeconds(.15f);
        Assert.That(item == null, Is.True, "The consumed pickup must disappear.");
        Capture("05-hmg-pickup");
        weapon.CollectHeavyMachineGun();
        Assert.That(weapon.Ammo, Is.EqualTo(50), "A refill provides a 50-round magazine.");
        weapon.CollectHeavyMachineGun();
        Assert.That(weapon.Ammo, Is.EqualTo(50), "Extra pickups must not stack beyond 50 rounds.");
    }

    [UnityTest] public IEnumerator HKeyboardMousePauseAndRestartRespectWeaponState()
    {
        var weapon = session.Player.Weapon;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null;
        yield return new WaitForSeconds(.15f);
        Assert.That(weapon.ShotsFired, Is.Zero);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        weapon.CollectHeavyMachineGun();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null;
        float fireDeadline=Time.time+1.5f;
        while (weapon.ShotsFired<2 && Time.time<fireDeadline) yield return null;
        Assert.That(weapon.ShotsFired, Is.GreaterThan(1));
        Assert.That(weapon.Ammo, Is.EqualTo(50 - weapon.ShotsFired));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        int released = weapon.Ammo;
        yield return new WaitForSeconds(.2f);
        Assert.That(weapon.Ammo, Is.EqualTo(released));
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); yield return null;
            yield return new WaitForSeconds(.2f);
            Assert.That(weapon.Ammo, Is.LessThan(released));
            session.TogglePause(); yield return null;
            int pausedAmmo = weapon.Ammo;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(weapon.Ammo, Is.EqualTo(pausedAmmo));
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null;
            session.StartRun(); yield return null;
            Assert.That(weapon.Ammo, Is.Zero);
            Assert.That(weapon.ShotsFired, Is.Zero);
            weapon.CollectHeavyMachineGun();
            session.Hit(); session.Hit(); session.Hit(); yield return null;
            Assert.That(weapon.IsEquipped, Is.False, "Death clears the equipped gun.");
        }
        finally { InputSystem.RemoveDevice(mouse); }
    }

    [UnityTest] public IEnumerator HBulletsDestroyHazardsAndLeaveCoinsIntact()
    {
        var weapon = session.Player.Weapon;
        weapon.CollectHeavyMachineGun();
        var cactus = new GameObject("Shootable Cactus", typeof(GroundObstacle));
        cactus.transform.position = session.Player.transform.position + new Vector3(13f,.75f,0);
        cactus.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        cactus.GetComponent<BoxCollider2D>().isTrigger = true;
        cactus.GetComponent<BoxCollider2D>().size = new Vector2(.6f,1.4f);
        cactus.GetComponent<GroundObstacle>().Initialize(session);
        var coin = new GameObject("Bullet-safe Coin", typeof(SpriteRenderer), typeof(Coin));
        coin.transform.position = session.Player.transform.position + new Vector3(11f,1.04f,0);
        coin.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        coin.GetComponent<BoxCollider2D>().isTrigger = true;
        coin.GetComponent<Coin>().Initialize(session);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null;
        float deadline = Time.time + 1f;
        while (weapon.ShotsFired < 2 && Time.time < deadline) yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        Assert.That(weapon.ShotsFired, Is.EqualTo(2));
        yield return new WaitForSeconds(.4f);
        Assert.That(cactus != null, Is.True, "Two HMG rounds must not destroy a cactus.");
        Assert.That(cactus.GetComponent<GroundObstacle>().HitPoints, Is.EqualTo(1));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null;
        deadline = Time.time + 1f;
        while (weapon.ShotsFired < 3 && Time.time < deadline) yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
        Assert.That(weapon.ShotsFired, Is.EqualTo(3));
        yield return new WaitForSeconds(.4f);
        Assert.That(cactus == null, Is.True);
        Assert.That(coin != null, Is.True, "Bullets must pass through collectibles.");
        Assert.That(session.Model.Lives, Is.EqualTo(3));
        Capture("06-hmg-firing");
        Object.Destroy(coin);
    }

    [UnityTest] public IEnumerator CactusAndBirdNeedThreeHitsAndIgnoreDamageWhilePaused()
    {
        foreach (bool airborne in new[] {false,true})
        {
            var target = new GameObject(airborne ? "Three-hit Bird" : "Three-hit Cactus");
            target.transform.position = new Vector3(10,0,0);
            Interactable hazard = airborne ? (Interactable)target.AddComponent<AirObstacle>() : target.AddComponent<GroundObstacle>();
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<BoxCollider2D>().isTrigger = true;
            hazard.Initialize(session);
            Assert.That(hazard.HitPoints, Is.EqualTo(3));
            Assert.That(hazard.TakeProjectileHit(), Is.True);
            Assert.That(hazard.HitPoints, Is.EqualTo(2));
            Assert.That(target.activeSelf, Is.True);
            Assert.That(hazard.TakeProjectileHit(), Is.True);
            Assert.That(hazard.HitPoints, Is.EqualTo(1));
            Assert.That(target.GetComponent<BoxCollider2D>().enabled, Is.True, "A damaged hazard must remain dangerous.");
            session.TogglePause();
            Assert.That(hazard.TakeProjectileHit(), Is.False);
            Assert.That(hazard.HitPoints, Is.EqualTo(1));
            session.TogglePause();
            Assert.That(hazard.TakeProjectileHit(), Is.True);
            Assert.That(hazard.HitPoints, Is.Zero);
            Assert.That(target.activeSelf, Is.False);
            Assert.That(hazard.TakeProjectileHit(), Is.False, "A defeated target must not consume another bullet.");
            yield return null;
            Assert.That(target == null, Is.True);
        }
    }

    [UnityTest] public IEnumerator GunPickupPlaysLaughTransitionsAndReturnsToRunning()
    {
        session.Player.Weapon.CollectHeavyMachineGun();
        yield return new WaitForSeconds(.35f);
        Assert.That(session.Player.IsLaughing, Is.True);
        var renderer = session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>();
        var first = renderer.sprite;
        Assert.That(first.name, Does.StartWith("mydino_laugh_"));
        Capture("09-refined-pickup-laugh");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J)); yield return null;
        yield return new WaitForSeconds(.25f);
        Assert.That(renderer.sprite, Is.Not.EqualTo(first), "The reaction must advance through different poses.");
        Assert.That(session.Player.Weapon.ShotsFired, Is.GreaterThan(0), "Laughing must not block firing.");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        yield return new WaitForSeconds(1f);
        Assert.That(session.Player.IsLaughing, Is.False);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Run"), Is.True);
        Assert.That(renderer.sprite.name, Does.StartWith("mydino_armedrun_"), "The wicked grin must remain after the laugh.");
        Capture("10-evil-armed-grin");
    }

    [UnityTest] public IEnumerator ArmedGrinSurvivesJumpAndEndsWhenAllFiftyRoundsAreSpent()
    {
        var weapon = session.Player.Weapon;
        var renderer = session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>();
        weapon.CollectHeavyMachineGun();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return null;
        yield return new WaitForSeconds(.15f);
        Assert.That(renderer.sprite.name, Does.StartWith("mydino_armedjump_"));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        yield return new WaitForSeconds(2.4f);
        Assert.That(renderer.sprite.name, Does.StartWith("mydino_armedrun_"));
        var first = renderer.sprite;
        yield return new WaitForSeconds(.15f);
        Assert.That(renderer.sprite, Is.Not.EqualTo(first), "Armed walking must keep advancing.");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J)); yield return null;
        float deadline = Time.time + 7f;
        while (weapon.IsEquipped && Time.time < deadline) yield return null;
        yield return new WaitForSeconds(.12f);
        Assert.That(weapon.Ammo, Is.Zero);
        Assert.That(weapon.ShotsFired, Is.EqualTo(50), "Exactly 50 bullets may be fired per pickup.");
        Assert.That(renderer.sprite.name, Does.StartWith("mydino_run_"), "The normal face returns when the magazine is empty.");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        weapon.CollectHeavyMachineGun();
        Assert.That(weapon.Ammo, Is.EqualTo(50));
        session.StartRun(); yield return new WaitForSeconds(.1f);
        Assert.That(renderer.sprite.name, Does.StartWith("mydino_run_"), "Restart clears the armed expression.");
    }

    [UnityTest] public IEnumerator PickupLaughYieldsToJumpHitDeathAndRestart()
    {
        session.Player.Weapon.CollectHeavyMachineGun();
        yield return new WaitForSeconds(.2f);
        Assert.That(session.Player.IsLaughing, Is.True);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return null;
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(.1f);
        Assert.That(session.Player.IsLaughing, Is.False);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        yield return new WaitForSeconds(1f);
        session.Player.Weapon.CollectHeavyMachineGun();
        yield return new WaitForSeconds(.2f);
        Assert.That(session.Player.IsLaughing, Is.True);
        session.Hit(); yield return new WaitForSeconds(.06f);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Hit"), Is.True);
        session.Hit(); session.Hit(); yield return new WaitForSeconds(.1f);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death"), Is.True);
        session.StartRun(); yield return new WaitForSeconds(.2f);
        Assert.That(session.Player.IsLaughing, Is.False);
        Assert.That(session.Player.Weapon.IsEquipped, Is.False);
    }

    [UnityTest] public IEnumerator AirbornePickupWaitsForLandingAndLaughFreezesDuringPause()
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return null;
        yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Assert.That(session.Player.IsGrounded, Is.False);
        session.Player.Weapon.CollectHeavyMachineGun();
        yield return new WaitForSeconds(.1f);
        Assert.That(session.Player.IsLaughing, Is.False, "Jump presentation has priority until landing.");
        float deadline = Time.time + 1.8f;
        while (!session.Player.IsLaughing && Time.time < deadline) yield return null;
        Assert.That(session.Player.IsLaughing, Is.True);
        session.TogglePause(); yield return null;
        var animator = session.Player.GetComponentInChildren<Animator>();
        float frozen = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(frozen).Within(.001f));
        session.TogglePause();
        yield return new WaitForSeconds(1.5f);
        Assert.That(session.Player.IsLaughing, Is.False);
    }

    [UnityTest] public IEnumerator TracerStartsAtTheVisibleBarrelHeight()
    {
        var weapon = session.Player.Weapon;
        var gun = session.Player.transform.Find("Visual/Heavy Machine Gun").GetComponent<SpriteRenderer>();
        // Held-gun canvas: opening at x=34, y=7.5 measured from its top.
        var expected = gun.transform.TransformPoint(new Vector3((34f-gun.sprite.pivot.x)/gun.sprite.pixelsPerUnit,(16.5f-gun.sprite.pivot.y)/gun.sprite.pixelsPerUnit,0));
        Assert.That(Vector3.Distance(weapon.MuzzlePosition,expected), Is.LessThan(.005f));
        weapon.CollectHeavyMachineGun();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J)); yield return null;
        float deadline=Time.time+1f;
        while (weapon.ShotsFired==0 && Time.time<deadline) yield return null;
        yield return null; // Allow the authored-pose/recoil LateUpdate to emit the requested round.
        var tracers = GameObject.Find("HMG Tracers").GetComponentsInChildren<SpriteRenderer>();
        int visible = 0;
        foreach (var tracer in tracers)
            if (tracer.enabled)
            {
                visible++;
                Assert.That(tracer.transform.position.y, Is.EqualTo(expected.y).Within(.19f), "Each bullet retains the barrel height of its authored run pose and recoil.");
                Assert.That(tracer.transform.position.x, Is.GreaterThanOrEqualTo(expected.x-.13f));
            }
        Assert.That(visible, Is.GreaterThan(0));
    }

    [UnityTest] public IEnumerator FiringRecoilMovesTheVisualAndGunButFreezesWithPause()
    {
        var weapon=session.Player.Weapon;
        var visual=session.Player.transform.Find("Visual");
        var gun=visual.Find("Heavy Machine Gun");
        var originalGun=gun.localPosition;
        float rootX=session.Player.transform.position.x;
        weapon.CollectHeavyMachineGun();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;
        float deadline=Time.time+1f;
        while (weapon.ShotsFired==0 && Time.time<deadline) yield return null;
        yield return null;
        Assert.That(weapon.ShotsFired,Is.GreaterThan(0));
        Assert.That(visual.localPosition.x,Is.LessThan(0));
        Assert.That(gun.localPosition.x,Is.LessThan(originalGun.x));
        Assert.That(session.Player.transform.position.x,Is.EqualTo(rootX).Within(.001f));
        Assert.That(Vector3.Distance(weapon.LastShotOrigin,weapon.MuzzlePosition),Is.LessThan(.001f));
        session.TogglePause();yield return null;
        var frozen=visual.localPosition;var gunFrozen=gun.localPosition;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(visual.localPosition,Is.EqualTo(frozen));Assert.That(gun.localPosition,Is.EqualTo(gunFrozen));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        session.TogglePause();yield return new WaitForSeconds(.2f);yield return null;
        Assert.That(visual.localPosition,Is.EqualTo(Vector3.zero));
        Assert.That(gun.localPosition.x,Is.EqualTo(originalGun.x).Within(.001f));
        Assert.That(Mathf.Abs(gun.localPosition.y-originalGun.y),Is.LessThanOrEqualTo(.19f),"The held gun continues following the running body's rise/fall after recoil settles.");
    }

    [UnityTest] public IEnumerator RifleSoldierWarnsPausesAndDefeatCancelsTheRemainingBurst()
    {
#if UNITY_EDITOR
        var enemy=SpawnCombatSoldier("RifleSoldier",12);
        var attack=enemy.GetComponent<SoldierAttack>();
        float aimDeadline=Time.time+3;
        while (!attack.IsWindingUp && Time.time<aimDeadline) yield return null;
        yield return new WaitForSeconds(.2f);
        Assert.That(attack.IsWindingUp,Is.True);Assert.That(attack.ShotsFired,Is.Zero);
        Assert.That(attack.IsAdvancing,Is.False,"Aiming must stop the soldier's own advance while the world scrolls.");
        Assert.That(enemy.GetComponentInChildren<SpriteRenderer>().sprite.name,Is.EqualTo("rifle_fire_00"),"Wind-up holds a shoulder-aim pose rather than completing the firing animation early.");
        Capture("11-rifle-soldier-aim");
        session.TogglePause();yield return new WaitForSecondsRealtime(.25f);
        Assert.That(attack.ShotsFired,Is.Zero);
        session.TogglePause();
        float fireDeadline=Time.time+1;
        while (attack.ShotsFired==0 && Time.time<fireDeadline) yield return null;
        Assert.That(attack.ShotsFired,Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<EnemyOrdnance>().Length,Is.EqualTo(1));
        var barrel=enemy.transform.TransformPoint(new Vector3(-34f/24f,30.5f/24f,0));
        Assert.That(attack.LastShotOrigin.y,Is.EqualTo(barrel.y).Within(.005f),"Round height matches the shoulder-aim barrel at the authored soldier scale.");
        Assert.That(attack.AttackOrigin.x,Is.EqualTo(barrel.x).Within(.005f),"The origin is at the muzzle, rather than inside the rifle.");
        enemy.TakeProjectileHit();enemy.TakeProjectileHit();enemy.TakeProjectileHit();
        yield return new WaitForSeconds(.4f);
        Assert.That(attack.ShotsFired,Is.EqualTo(1),"Defeat cancels the second shot.");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator UnarmedJumpClearsBothRifleRoundsAndTheSoldierAtBothSpeeds()
    {
#if UNITY_EDITOR
        foreach (bool fast in new[]{false,true})
        foreach (float delay in new[]{.42f,.49f,.56f})
        {
            session.StartRun();yield return null;yield return new WaitForSeconds(.15f);
            if (fast) session.Model.Advance(1000);
            var enemy=SpawnCombatSoldier("RifleSoldier",14);
            var attack=enemy.GetComponent<SoldierAttack>();
            float deadline=Time.time+3;
            while (!attack.IsWindingUp && Time.time<deadline) yield return null;
            Assert.That(attack.IsWindingUp,Is.True);
            Assert.That(enemy.transform.Find("Aim Warning").GetComponent<SpriteRenderer>().enabled,Is.True);
            yield return new WaitForSeconds(delay);
            Assert.That(enemy.transform.Find("Aim Warning").GetComponent<SpriteRenderer>().color.g,Is.LessThan(.5f),"Red warning cues the jump.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            yield return new WaitForSeconds(.3f);
            if (!fast && delay==.49f) Capture("17-unarmed-rifle-dodge");
            yield return new WaitForSeconds(1.2f);
            Assert.That(session.Model.Lives,Is.EqualTo(3),"One unarmed jump must clear both bullets AND the body: fast="+fast+", delay="+delay);
            Assert.That(attack.ShotsFired,Is.EqualTo(2));
            Assert.That(session.Player.Weapon.Ammo,Is.Zero);
            Assert.That(session.Player.IsGrounded,Is.True);
            Assert.That(enemy==null || enemy.transform.position.x<session.Player.transform.position.x-1,Is.True,"The soldier must have passed the player or scrolled offscreen.");
        }
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator ShotgunPhysicalPickupUsesThirtyShellsAndSwitchesCleanlyWithH()
    {
#if UNITY_EDITOR
        var weapon=session.Player.Weapon;
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<ShotgunPickup>("Assets/Game/Prefabs/ShotgunPickup.prefab");
        var pickup=Object.Instantiate(prefab,session.Player.transform.position+Vector3.up*.6f,Quaternion.identity);
        pickup.Initialize(session);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Assert.That(weapon.ActiveWeapon,Is.EqualTo(PlayerWeapon.WeaponKind.Shotgun));
        Assert.That(weapon.Ammo,Is.EqualTo(30));Assert.That(weapon.ShotsFired,Is.Zero);Assert.That(weapon.PickupVoicePlaying,Is.True);
        Assert.That(session.Player.transform.Find("Visual/Heavy Machine Gun").GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("shotgun_held"));
        yield return new WaitForSeconds(.3f);Capture("18-shotgun-pickup");
        Assert.That(session.Player.IsLaughing,Is.True);
        weapon.CollectShotgun();Assert.That(weapon.Ammo,Is.EqualTo(30),"S refills rather than stacking.");
        weapon.CollectHeavyMachineGun();Assert.That(weapon.Ammo,Is.EqualTo(50));
        Assert.That(session.Player.transform.Find("Visual/Heavy Machine Gun").GetComponent<SpriteRenderer>().sprite.name,Is.EqualTo("hmg_held"));
        weapon.CollectShotgun();weapon.SetFireHeld(true);
        float deadline=Time.time+1;
        while (weapon.ShotsFired==0 && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);yield return null;
        Assert.That(weapon.Ammo,Is.EqualTo(29));
        var flash=session.Player.transform.Find("Visual/Heavy Machine Gun/Barrel Tip/Muzzle Flash").GetComponent<SpriteRenderer>();
        Assert.That(flash.sprite.name,Does.StartWith("shotgun_blast_"));
        Assert.That(Vector3.Distance(weapon.LastShotOrigin,weapon.MuzzlePosition),Is.LessThan(.2f));
        session.TogglePause();int ammo=weapon.Ammo;Vector3 position=GameObject.Find("HMG Tracer 0").transform.position;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(weapon.Ammo,Is.EqualTo(ammo));Assert.That(GameObject.Find("HMG Tracer 0").transform.position,Is.EqualTo(position));
        session.StartRun();yield return null;
        Assert.That(weapon.ActiveWeapon,Is.EqualTo(PlayerWeapon.WeaponKind.None));Assert.That(weapon.Ammo,Is.Zero);Assert.That(flash.enabled,Is.False);
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator ShotgunBlastDefeatsCloseTargetsButHasLimitedRangeAndPumpCadence()
    {
#if UNITY_EDITOR
        var weapon=session.Player.Weapon;weapon.CollectShotgun();yield return new WaitForSeconds(.27f);
        var enemy=SpawnCombatSoldier("RifleSoldier",4.5f);
        // Keep a distant target fixed to test the weapon's range independently of scrolling.
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GroundObstacle>("Assets/Game/Prefabs/Cactus.prefab");
        Assert.That(prefab,Is.Not.Null);
        var far=Object.Instantiate(prefab,new Vector3(weapon.MuzzlePosition.x+8,-3.2f,0),Quaternion.identity);
        far.Initialize(session);far.GetComponent<Rigidbody2D>().simulated=false;
        weapon.SetFireHeld(true);float deadline=Time.time+1;
        while (weapon.ShotsFired==0 && Time.time<deadline) yield return null;
        yield return null;yield return new WaitForSeconds(.08f);Capture("19-shotgun-blast");
        yield return new WaitForSeconds(.22f);
        Assert.That(enemy.HitPoints,Is.Zero,"One close S blast should defeat the three-HP soldier.");
        Assert.That(weapon.ShotsFired,Is.EqualTo(1),"Holding fire must respect the pump cycle.");
        Assert.That(far.HitPoints,Is.EqualTo(3),"S pellets expire after 4.76 units, unlike HMG rounds.");
        deadline=Time.time+1;
        while (weapon.ShotsFired<2 && Time.time<deadline) yield return null;
        weapon.SetFireHeld(false);Assert.That(weapon.ShotsFired,Is.EqualTo(2));Assert.That(weapon.Ammo,Is.EqualTo(28));
        Object.Destroy(far.gameObject);
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator ShotgunStopsAfterExactlyThirtyShellsAndReturnsToUnarmedRun()
    {
        var weapon=session.Player.Weapon;weapon.CollectShotgun();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;
        float deadline=Time.time+23;
        while (weapon.Ammo>0 && Time.time<deadline) yield return null;
        yield return new WaitForSeconds(.8f);
        Assert.That(weapon.ShotsFired,Is.EqualTo(30));Assert.That(weapon.Ammo,Is.Zero);Assert.That(weapon.IsEquipped,Is.False);
        Assert.That(session.Player.GetComponentInChildren<Animator>().GetFloat("Armed"),Is.Zero);
        Assert.That(session.Player.transform.Find("Visual/Heavy Machine Gun").GetComponent<SpriteRenderer>().enabled,Is.False);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
    }

    [UnityTest] public IEnumerator CourseActuallySpawnsShotgunAfterTheOpeningMachineGun()
    {
        var course=Object.FindAnyObjectByType<ObstacleSpawner>();course.enabled=true;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;
        float deadline=Time.time+6;
        while (Object.FindAnyObjectByType<ShotgunPickup>()==null && Time.time<deadline) yield return null;
        Assert.That(Object.FindAnyObjectByType<ShotgunPickup>(),Is.Not.Null,"S must appear in the actual course, rather than existing only as an unused prefab.");
        Assert.That(session.Player.Weapon.ActiveWeapon,Is.EqualTo(PlayerWeapon.WeaponKind.HeavyMachineGun),"The opening H should still be reachable.");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
    }

    [UnityTest] public IEnumerator GrenadierThrowsAnArcPausesAndExplosionDamagesOnlyOnce()
    {
#if UNITY_EDITOR
        var enemy=SpawnCombatSoldier("Grenadier",12);
        yield return new WaitForSeconds(.35f);
        var throwPose=enemy.GetComponentInChildren<SpriteRenderer>().sprite.name;
        Assert.That(throwPose,Does.StartWith("grenadier_throw_"));
        Assert.That(enemy.transform.Find("Held Grenade"),Is.Null,"The grenade belongs in the original hand cels; a floating overlay duplicates it.");
        Assert.That(enemy.GetComponent<SoldierAttack>().ShotsFired,Is.Zero);
        yield return new WaitForSeconds(.43f);
        Assert.That(enemy.GetComponent<SoldierAttack>().ShotsFired,Is.EqualTo(1));
        var grenade=Object.FindAnyObjectByType<EnemyOrdnance>();
        Assert.That(grenade,Is.Not.Null);Assert.That(grenade.IsGrenade,Is.True);
        Assert.That(grenade.transform.localScale.x,Is.EqualTo(.5f),"Thrown grenades use the enlarged ordnance scale, not collectible size.");
        Object.Destroy(enemy.gameObject);
        float height=grenade.transform.position.y;
        yield return new WaitForSeconds(.12f);
        Assert.That(grenade.transform.position.y,Is.GreaterThan(height),"The grenade must arc upward.");
        Capture("12-grenade-arc");
        session.TogglePause();var frozen=grenade.transform.position;float fuse=grenade.FuseRemaining;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(grenade.transform.position,Is.EqualTo(frozen));Assert.That(grenade.FuseRemaining,Is.EqualTo(fuse));
        session.TogglePause();float deadline=Time.time+2f;
        while (grenade != null && !grenade.IsExploding && Time.time<deadline) yield return null;
        Assert.That(grenade != null && grenade.IsExploding,Is.True);
        Assert.That(session.Model.Lives,Is.EqualTo(2));yield return new WaitForSeconds(.12f);Capture("13-grenade-blast");
        yield return new WaitForSeconds(.2f);Assert.That(session.Model.Lives,Is.EqualTo(2));
        session.StartRun();yield return null;
        Assert.That(Object.FindObjectsByType<EnemyOrdnance>(),Is.Empty);
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator SoldierDeathVoicesVarySurviveTheBodyAndRespectPauseAndRestart()
    {
#if UNITY_EDITOR
        var seen=new System.Collections.Generic.HashSet<AudioClip>();
        AudioClip previous=null;
        for (int i=0;i<6;i++)
        {
            var enemy=SpawnCombatSoldier(i%2==0 ? "RifleSoldier" : "Grenadier",20);
            enemy.TakeProjectileHit();enemy.TakeProjectileHit();
            Assert.That(session.Audio.SoldierDeathCount,Is.EqualTo(i),"Surviving hits must not play a death voice.");
            enemy.TakeProjectileHit();
            var clip=session.Audio.LastSoldierDeath;
            Assert.That(clip,Is.Not.Null);Assert.That(clip.name,Does.StartWith("soldier_death_"));
            Assert.That(clip,Is.Not.SameAs(previous),"Consecutive deaths should use different original voices.");
            seen.Add(clip);previous=clip;
            Assert.That(enemy.TakeProjectileHit(),Is.False);
            Assert.That(session.Audio.SoldierDeathCount,Is.EqualTo(i+1),"Each defeat charges one voice.");
            Object.Destroy(enemy.gameObject);
        }
        yield return null;
        Assert.That(seen.Count,Is.GreaterThan(1));
        AudioSource voice=null;
        foreach (var candidate in session.Audio.GetComponentsInChildren<AudioSource>())
            if (candidate.clip==previous && candidate.isPlaying) voice=candidate;
        Assert.That(voice,Is.Not.Null,"Removing the sprite must not cut off the voice.");
        session.TogglePause();yield return null;
        int frozen=voice.timeSamples;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(voice.timeSamples,Is.EqualTo(frozen),"Paused combat audio must not advance.");
        session.TogglePause();yield return new WaitForSeconds(.04f);
        Assert.That(voice.isPlaying,Is.True);
        session.StartRun();yield return null;
        foreach (var candidate in session.Audio.GetComponentsInChildren<AudioSource>())
            if (candidate.name.StartsWith("Combat Voice")) Assert.That(candidate.isPlaying,Is.False);
        Assert.That(session.Audio.SoldierDeathCount,Is.Zero);
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator RifleAndExplosionSoundsPlayAtDischargeAndDetonation()
    {
#if UNITY_EDITOR
        var rifle=SpawnCombatSoldier("RifleSoldier",12);
        yield return new WaitForSeconds(.2f);
        Assert.That(session.Audio.RifleShotCount,Is.Zero,"Aiming is silent until discharge.");
        float deadline=Time.time+1.2f;
        while (rifle.GetComponent<SoldierAttack>().ShotsFired==0 && Time.time<deadline) yield return null;
        Assert.That(session.Audio.RifleShotCount,Is.EqualTo(1));
        Assert.That(session.Audio.LastCombatClip.name,Is.EqualTo("enemy_rifle_shot"));
        session.StartRun();yield return null;
        Object.FindAnyObjectByType<ObstacleSpawner>().enabled=false;
        var thrower=SpawnCombatSoldier("Grenadier",12);
        deadline=Time.time+1.2f;
        while (thrower.GetComponent<SoldierAttack>().ShotsFired==0 && Time.time<deadline) yield return null;
        Assert.That(session.Audio.ExplosionCount,Is.Zero,"The throw must not play the blast early.");
        var grenade=Object.FindAnyObjectByType<EnemyOrdnance>();Assert.That(grenade,Is.Not.Null);
        Object.Destroy(thrower.gameObject);deadline=Time.time+3f;
        while (!grenade.IsExploding && Time.time<deadline) yield return null;
        Assert.That(session.Audio.ExplosionCount,Is.EqualTo(1));
        Assert.That(session.Audio.LastCombatClip.name,Is.EqualTo("enemy_grenade_explosion"));
        yield return new WaitForSeconds(.15f);
        Assert.That(session.Audio.ExplosionCount,Is.EqualTo(1));
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator SoldierApproachesThenPlantsItsFeetForTheAttack()
    {
#if UNITY_EDITOR
        var enemy=SpawnCombatSoldier("RifleSoldier",18);
        var attack=enemy.GetComponent<SoldierAttack>();
        yield return new WaitForSeconds(.1f);
        Assert.That(attack.IsAdvancing,Is.True);
        float before=enemy.GetComponent<Rigidbody2D>().position.x;
        yield return new WaitForSeconds(.1f);
        float advancing=before-enemy.GetComponent<Rigidbody2D>().position.x;
        Assert.That(advancing,Is.GreaterThan(session.WorldSpeed*.1f),"Running soldiers must advance relative to the ground.");
        enemy.GetComponent<Rigidbody2D>().position=new Vector2(session.Player.transform.position.x+session.Model.BaseSpeed-.1f,-3.2f);
        yield return new WaitForSeconds(.08f);
        Assert.That(attack.IsAdvancing,Is.False);
        before=enemy.GetComponent<Rigidbody2D>().position.x;
        yield return new WaitForSeconds(.1f);
        float planted=before-enemy.GetComponent<Rigidbody2D>().position.x;
        Assert.That(planted,Is.EqualTo(session.WorldSpeed*.1f).Within(.16f),"An aiming soldier stays on the scrolling ground without continuing its approach.");
        enemy.TakeProjectileHit();enemy.TakeProjectileHit();enemy.TakeProjectileHit();
        yield return new WaitForSeconds(.5f);
        Assert.That(enemy.GetComponentInChildren<SpriteRenderer>().sprite.name,Does.StartWith("rifle_death_"));
        Assert.That(attack.ShotsFired,Is.Zero,"A defeated rifleman must not fire its unfinished attack.");
        Capture("14-rifle-real-defeat");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator EnemyRifleRoundCanBeJumpedAndChargesOnceWhenItHits()
    {
#if UNITY_EDITOR
        var projectile=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MetalSlug/Combat/enemy_bullet.png");
        var material=session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial;
        var spawner=Object.FindAnyObjectByType<ObstacleSpawner>().transform;
        EnemyOrdnance Shoot()
        {
            var root=new GameObject("Test Enemy Round",typeof(SpriteRenderer),typeof(EnemyOrdnance));root.transform.SetParent(spawner);
            root.transform.position=new Vector3(session.Player.transform.position.x+3,-2.15f,0);
            var round=root.GetComponent<EnemyOrdnance>();round.Launch(session,false,projectile,null,material);return round;
        }
        Shoot();yield return new WaitForSeconds(.3f);Assert.That(session.Model.Lives,Is.EqualTo(2));
        yield return new WaitForSeconds(.2f);Assert.That(session.Model.Lives,Is.EqualTo(2));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;
        yield return new WaitForSeconds(.25f);Shoot();yield return new WaitForSeconds(.3f);
        Assert.That(session.Model.Lives,Is.EqualTo(2),"A jump clears the waist-high rifle round.");
        session.StartRun();yield return null;
        Assert.That(Object.FindObjectsByType<EnemyOrdnance>(),Is.Empty);
#else
        yield return null;
#endif
    }

#if UNITY_EDITOR
    private RebelSoldier SpawnCombatSoldier(string name,float ahead)
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<RebelSoldier>("Assets/Game/Prefabs/"+name+".prefab");
        Assert.That(prefab,Is.Not.Null);
        var enemy=Object.Instantiate(prefab,new Vector3(session.Player.transform.position.x+ahead,-3.2f,0),Quaternion.identity,Object.FindAnyObjectByType<ObstacleSpawner>().transform);
        enemy.Initialize(session);return enemy;
    }
#endif

    [UnityTest] public IEnumerator RebelSoldierAnimatesTakesThreeHitsAndDefeatRespectsPause()
    {
#if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<RebelSoldier>("Assets/Game/Prefabs/RebelSoldier.prefab");
        var enemy = Object.Instantiate(prefab,new Vector3(0,-3.2f,0),Quaternion.identity);
        enemy.Initialize(session);
        var renderer = enemy.GetComponentInChildren<SpriteRenderer>();
        var first = renderer.sprite;
        session.Player.Weapon.CollectHeavyMachineGun();
        yield return new WaitForSeconds(.2f);
        Assert.That(renderer.sprite, Is.Not.EqualTo(first), "Soldier walking frames must advance.");
        Capture("07-metal-slug-rebel");
        enemy.TakeProjectileHit(); enemy.TakeProjectileHit();
        Assert.That(enemy.HitPoints, Is.EqualTo(1));
        Assert.That(enemy.GetComponent<BoxCollider2D>().enabled, Is.True);
        enemy.TakeProjectileHit();
        Assert.That(enemy.HitPoints, Is.Zero);
        Assert.That(enemy.GetComponent<BoxCollider2D>().enabled, Is.False);
        session.TogglePause();
        var frozen = renderer.sprite;
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(enemy != null && renderer.sprite == frozen, Is.True);
        session.TogglePause();
        yield return new WaitForSeconds(.65f);
        Capture("08-rebel-defeated");
        yield return new WaitForSeconds(.45f);
        Assert.That(enemy == null, Is.True);
        Assert.That(session.Model.Lives, Is.EqualTo(3), "A defeated soldier cannot damage the dino.");
#else
        yield return null;
#endif
    }

    [UnityTest] public IEnumerator CoinAirObstacleAndMenusRenderCorrectly()
    {
        session.MainMenu(); yield return null;
        Capture("01-main-menu");
        session.StartRun(); yield return new WaitForSeconds(.2f);
        // Exercise the real prefab interaction route for both remaining object types.
        var coinObject = new GameObject("Test Coin", typeof(SpriteRenderer));
        coinObject.transform.position = session.Player.transform.position + Vector3.up * .6f;
        var coinBody = coinObject.AddComponent<Rigidbody2D>(); coinBody.bodyType = RigidbodyType2D.Kinematic;
        var coinCollider = coinObject.AddComponent<BoxCollider2D>(); coinCollider.isTrigger = true; coinCollider.size = Vector2.one;
        coinObject.AddComponent<Coin>().Initialize(session);
        yield return new WaitForSeconds(.1f);
        Assert.That(session.Model.Coins, Is.EqualTo(1), "Coin interaction was not observed; state=" + session.Model.State);
        var bird = new GameObject("Test Air Obstacle");
        bird.transform.position = session.Player.transform.position + Vector3.up * .6f;
        var birdBody = bird.AddComponent<Rigidbody2D>(); birdBody.bodyType = RigidbodyType2D.Kinematic;
        var birdCollider = bird.AddComponent<BoxCollider2D>(); birdCollider.isTrigger = true;
        bird.AddComponent<AirObstacle>().Initialize(session);
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Assert.That(session.Model.Lives, Is.EqualTo(2));
        Object.Destroy(bird);
        yield return new WaitForSeconds(.35f);
#if UNITY_EDITOR
        var cactusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GroundObstacle>("Assets/Game/Prefabs/Cactus.prefab");
        var birdPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<AirObstacle>("Assets/Game/Prefabs/Pterosaur.prefab");
        var coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Coin>("Assets/Game/Prefabs/Coin.prefab");
        Object.Instantiate(cactusPrefab, new Vector3(0, -3.2f, 0), Quaternion.identity).Initialize(session);
        Object.Instantiate(birdPrefab, new Vector3(5, -.85f, 0), Quaternion.identity).Initialize(session);
        for (int i = 0; i < 3; i++) Object.Instantiate(coinPrefab, new Vector3(2 + i, -1.1f, 0), Quaternion.identity).Initialize(session);
#endif
        Capture("02-gameplay");
        session.TogglePause(); yield return null; Capture("03-pause-menu");
        session.TogglePause(); session.Hit(); session.Hit();
        yield return new WaitForSeconds(1.3f); Capture("04-game-over");
        Assert.That(session.Model.State, Is.EqualTo(RunState.GameOver));
    }

    private static void Capture(string name)
    {
        string folder = Path.Combine(Application.dataPath, "..", "Docs", "Screenshots");
        Directory.CreateDirectory(folder);
        var camera = Camera.main;
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var target = new RenderTexture(1600, 900, 24);
        var previousTarget = camera.targetTexture;
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        int previousOrder = canvas.sortingOrder;
        var previousActive = RenderTexture.active;
        camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera; canvas.planeDistance = 1;
        canvas.sortingOrder = 100;
        Canvas.ForceUpdateCanvases();
        var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target };
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
        RenderTexture.active = target;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
        camera.targetTexture = previousTarget;
        canvas.renderMode = previousMode; canvas.worldCamera = previousCamera;
        canvas.sortingOrder = previousOrder;
        RenderTexture.active = previousActive;
        target.Release(); Object.Destroy(target); Object.Destroy(image);
    }
}
