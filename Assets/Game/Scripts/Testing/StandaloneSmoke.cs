#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DinoSlug
{
    /// <summary>Opt-in executable check. Inactive during normal play.</summary>
    public sealed class StandaloneSmoke : MonoBehaviour
    {
        [Serializable] private sealed class Evidence
        {
            public bool hadRecord;
            public int originalRecord;
            public int expectedRecord;
            public bool firstLaunchPassed;
            public bool secondLaunchPassed;
            public string error;
        }
        private string folder;
        private bool verify;
        private Evidence evidence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-dino-smoke");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Executable Verification").AddComponent<StandaloneSmoke>();
            runner.folder = args[index + 1];
            runner.verify = Array.IndexOf(args, "-verify-record") >= 0;
            // Only this explicit automated check accepts input with no focused window.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            runner.StartCoroutine(runner.Run());
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory(folder);
            var steps = Steps();
            while (true)
            {
                object next = null;
                bool more = false;
                bool failed = false;
                try { more = steps.MoveNext(); if (more) next = steps.Current; }
                catch (Exception error)
                {
                    if (evidence == null) evidence = new Evidence();
                    evidence.error = error.ToString();
                    File.WriteAllText(Path.Combine(folder, "standalone-results.json"), JsonUtility.ToJson(evidence, true));
                    Debug.LogError(error.ToString());
                    Application.Quit(1);
                    failed = true;
                }
                if (failed) yield break;
                if (!more) break;
                yield return next;
            }
            File.WriteAllText(Path.Combine(folder, "standalone-results.json"), JsonUtility.ToJson(evidence, true));
            Debug.Log("[DinoSlug] Executable verification passed, secondLaunch=" + verify);
            Application.Quit(0);
        }

        private IEnumerator Steps()
        {
            yield return new WaitForSecondsRealtime(.5f);
            var session = FindAnyObjectByType<GameSession>();
            Require(session != null && session.Model.State == RunState.MainMenu, "Executable did not open at the main menu.");
            string path = Path.Combine(folder, "standalone-results.json");
            if (verify)
            {
                evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(path));
                Require(session.Model.HighScore == evidence.expectedRecord, "High score did not survive closing and reopening the executable.");
                // Let the session save before restoring the user's original preference.
                Destroy(session.gameObject); yield return null;
                if (evidence.hadRecord) PlayerPrefs.SetInt(GameSession.HighScoreKey, evidence.originalRecord);
                else PlayerPrefs.DeleteKey(GameSession.HighScoreKey);
                PlayerPrefs.Save();
                evidence.secondLaunchPassed = true;
                yield break;
            }
            evidence = new Evidence { hadRecord = PlayerPrefs.HasKey(GameSession.HighScoreKey), originalRecord = session.Model.HighScore };
            Capture("standalone-main-menu.png");
            yield return new WaitForSecondsRealtime(.4f);
            var start = GameObject.Find("Game Canvas/Main Menu/Start Run").GetComponent<UnityEngine.UI.Button>();
            start.onClick.Invoke();
            Require(session.Model.State==RunState.SelectingCharacter,"The menu did not open Dino character selection.");
            yield return new WaitForSeconds(.2f);Capture("standalone-dino-selection.png");
            var deploy=GameObject.Find("Game Canvas/Character Select/Arcade Layout/Deploy Dino").GetComponent<UnityEngine.UI.Button>();
            deploy.onClick.Invoke();yield return new WaitForSeconds(.3f);Capture("standalone-dino-selected-laugh.png");
            Require(session.CharacterSelection.IsConfirming,"Selecting Dino did not trigger the selection animation.");
            Require(session.CharacterSelection.HasDinoVoice && session.Audio.SelectionVoicePlaying,"The supplied Dino announcement did not play in the executable.");
            Require(session.Audio.MusicSource.volume<.08f,"Selection announcement did not duck the music.");
            yield return new WaitForSeconds(1.3f);
            Require(!session.IsPlaying && session.Audio.SelectionVoicePlaying,"Deployment cut off the Dino announcement.");
            float deployment=Time.time+3;while(!session.IsPlaying && Time.time<deployment) yield return null;
            Require(session.IsPlaying,"Dino did not deploy after the confirmation animation.");
            Require(session.Audio.MusicSource!=null && session.Audio.MusicSource.loop && session.Audio.MusicSource.isPlaying,"Original background music did not start.");
            Require(session.Presentation!=null && session.Presentation.GetComponentsInChildren<Collider2D>().Length==0,"Cosmetic scenery is missing or has gameplay colliders.");
            var stage=GameObject.Find("Metal Slug Terrain");
            Require(stage!=null && GameObject.Find("Dusk Sky")==null,"The executable is still using the placeholder landscape.");
            var terrain=stage.transform.Find("Stone Ground/Tile 1").GetComponent<SpriteRenderer>();
            Require(terrain.sprite.name=="ms1_stone_ground" && Mathf.Abs(terrain.bounds.max.y-GameObject.Find("Ground Collider").GetComponent<BoxCollider2D>().bounds.max.y)<.001f,"Original terrain is not aligned with the collision floor.");
            FindAnyObjectByType<ObstacleSpawner>().enabled = false;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); yield return null;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Require(session.Player.GetComponent<Rigidbody2D>().linearVelocity.y > 0, "Space did not jump in the executable.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(.9f);
            Require(session.Player.IsGrounded, "Dinosaur did not land.");
            var pickup = new GameObject("Executable H Pickup", typeof(HeavyMachineGunPickup));
            pickup.transform.position = session.Player.transform.position + Vector3.up * .6f;
            pickup.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            pickup.GetComponent<BoxCollider2D>().isTrigger = true;
            pickup.GetComponent<HeavyMachineGunPickup>().Initialize(session);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            var weapon = session.Player.Weapon;
            Require(weapon != null && weapon.Ammo == 50 && weapon.PickupVoicePlaying, "H pickup did not equip 50 rounds and play its announcement in the executable.");
            Require(weapon.ShotsFired == 0, "Weapon fired without input.");
            yield return new WaitForSeconds(.3f);
            Require(session.Player.IsLaughing, "H pickup did not play the dinosaur's laugh in the executable.");
            Capture("standalone-laugh.png");
            RebelSoldier rebelPrefab = null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<RebelSoldier>())
                if (!candidate.gameObject.scene.IsValid() && candidate.GetComponent<SoldierAttack>() == null) { rebelPrefab = candidate; break; }
            Require(rebelPrefab != null, "The Rebel soldier prefab is missing from the executable.");
            var rebel = Instantiate(rebelPrefab,new Vector3(0,-3.2f,0),Quaternion.identity,FindAnyObjectByType<ObstacleSpawner>().transform);
            rebel.Initialize(session);
            Require(rebel.HitPoints == 3, "The Rebel soldier did not start with three hit points.");
            Capture("standalone-rebel.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null;
            yield return new WaitForSeconds(.6f);
            Require(weapon.ShotsFired > 1 && weapon.Ammo == 50 - weapon.ShotsFired, "J did not fire the 50-round magazine correctly.");
            Require(rebel == null || rebel.HitPoints == 0, "HMG bullets did not defeat the Rebel soldier.");
            Require(session.Presentation.CasingsEmitted>0 && session.Presentation.ImpactCount>0,"Real weapon fire did not emit casings and impacts.");
            Require(session.Audio.SoldierDeathCount>0 && session.Audio.LastSoldierDeath!=null,"The defeated infantry did not play a soldier death voice.");
            Capture("standalone-hmg.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSeconds(.7f);
            Require(session.Player.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite.name.StartsWith("mydino_armedrun_"), "The armed evil grin did not remain after the laugh.");
            Capture("standalone-armed-grin.png");
            foreach (var role in new[] { SoldierAttack.Weapon.Rifle, SoldierAttack.Weapon.Grenade })
            {
                RebelSoldier combatPrefab=null;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<RebelSoldier>())
                {
                    var attack=candidate.GetComponent<SoldierAttack>();
                    if (!candidate.gameObject.scene.IsValid() && attack != null && attack.Kind==role) { combatPrefab=candidate; break; }
                }
                Require(combatPrefab != null,"An attacking soldier prefab is missing from the executable.");
                var attacker=Instantiate(combatPrefab,new Vector3(session.Player.transform.position.x+12,-3.2f,0),Quaternion.identity,FindAnyObjectByType<ObstacleSpawner>().transform);
                attacker.Initialize(session);
                float attackDeadline=Time.time+3f;
                while (attacker.GetComponent<SoldierAttack>().ShotsFired==0 && Time.time<attackDeadline) yield return null;
                Require(attacker.GetComponent<SoldierAttack>().ShotsFired>0,"The soldier did not attack in the executable.");
                if (role==SoldierAttack.Weapon.Rifle) Require(session.Audio.RifleShotCount>0,"The rifle discharge sound is missing.");
                Capture(role==SoldierAttack.Weapon.Rifle ? "standalone-rifle-attack.png" : "standalone-grenade-throw.png");
                Destroy(attacker.gameObject);
                if (role==SoldierAttack.Weapon.Grenade)
                {
                    var grenade=FindAnyObjectByType<EnemyOrdnance>();float deadline=Time.time+3f;
                    while (grenade != null && !grenade.IsExploding && Time.time<deadline) yield return null;
                    Require(grenade != null && grenade.IsExploding,"The grenade did not detonate in the executable.");
                    Require(session.Audio.ExplosionCount==1,"The grenade must play its blast once at detonation.");
                    Require(session.Presentation.ExplosionCount==1,"The real grenade did not trigger explosion debris and camera feedback.");
                    yield return new WaitForSeconds(.12f);
                    Capture("standalone-grenade-blast.png");
                }
                foreach (var shot in FindObjectsByType<EnemyOrdnance>()) Destroy(shot.gameObject);
                yield return null;
            }
            var sPickup=new GameObject("Executable S Pickup",typeof(ShotgunPickup));
            sPickup.transform.position=session.Player.transform.position+Vector3.up*.6f;
            sPickup.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;sPickup.GetComponent<BoxCollider2D>().isTrigger=true;
            sPickup.GetComponent<ShotgunPickup>().Initialize(session);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Require(weapon.ActiveWeapon==PlayerWeapon.WeaponKind.Shotgun && weapon.Ammo==30 && weapon.PickupVoicePlaying,"S did not equip and announce 30 shells in the executable.");
            yield return new WaitForSeconds(.28f);
            var closeTarget=Instantiate(rebelPrefab,new Vector3(session.Player.transform.position.x+4.5f,-3.2f,0),Quaternion.identity,FindAnyObjectByType<ObstacleSpawner>().transform);
            closeTarget.Initialize(session);
            int beforeS=weapon.ShotsFired;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));yield return null;
            float sDeadline=Time.time+1;
            while (weapon.ShotsFired==beforeS && Time.time<sDeadline) yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            yield return new WaitForSeconds(.08f);Capture("standalone-shotgun-blast.png");
            Require(session.Presentation.CameraOffset==Vector3.zero,"The camera moved during combat.");
            yield return new WaitForSeconds(.2f);
            Require(weapon.Ammo==29 && closeTarget.HitPoints==0,"One S shell did not defeat close three-HP infantry.");
            yield return new WaitForSeconds(.4f);
            int beforeMouse = weapon.Ammo;
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); yield return null;
            yield return new WaitForSeconds(.2f);
            Require(weapon.Ammo < beforeMouse, "Left mouse did not fire the equipped gun.");
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null;
            InputSystem.RemoveDevice(mouse);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift)); yield return null;
            Require(session.Player.IsDashing, "Shift did not dash in the executable.");
            session.TogglePause();
            float distance = session.Model.Distance; float cooldown = session.Player.DashReadyFraction; int pausedAmmo = weapon.Ammo;
            Capture("standalone-pause.png");
            var frozenCamera=Camera.main.transform.position;
            var foreground=session.Presentation.transform.Find("Metal Slug Scenery/Prop 0");var frozenForeground=foreground.position;
            yield return new WaitForSecondsRealtime(.35f);
            Require(session.Model.Distance == distance && session.Player.DashReadyFraction == cooldown, "Pause did not freeze the run.");
            Require(weapon.Ammo == pausedAmmo, "Pause did not freeze the weapon.");
            Require(Camera.main.transform.position==frozenCamera && foreground.position==frozenForeground,"Pause did not freeze camera and scenery.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            session.StartRun();
            Require(session.Presentation.ActiveEffectCount==0,"Restart did not clear existing debris before the first new footstep.");
            yield return null;
            Require(!session.Player.IsDashing && Time.timeScale == 1, "Restart from pause did not clear dash/time state.");
            Require(!weapon.IsEquipped && weapon.Ammo == 0, "Restart did not clear the weapon.");
            Require(!session.Player.IsLaughing, "Restart did not clear the pickup laugh.");
            Require(session.Presentation.CasingsEmitted==0,"Restart did not clear cosmetic combat feedback.");
            RebelSoldier riflePrefab=null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<RebelSoldier>())
                if (!candidate.gameObject.scene.IsValid() && candidate.GetComponent<SoldierAttack>()!=null && candidate.GetComponent<SoldierAttack>().Kind==SoldierAttack.Weapon.Rifle) { riflePrefab=candidate;break; }
            Require(riflePrefab!=null,"Rifle prefab missing for the unarmed dodge check.");
            foreach (bool fast in new[]{false,true})
            {
                session.StartRun();yield return null;yield return new WaitForSeconds(.15f);
                if (fast) session.Model.Advance(1000);
                var attacker=Instantiate(riflePrefab,new Vector3(session.Player.transform.position.x+14,-3.2f,0),Quaternion.identity,FindAnyObjectByType<ObstacleSpawner>().transform);attacker.Initialize(session);
                var attack=attacker.GetComponent<SoldierAttack>();float deadline=Time.time+3;
                while (!attack.IsWindingUp && Time.time<deadline) yield return null;
                Require(attack.IsWindingUp,"Rifleman never warned.");
                yield return new WaitForSeconds(.49f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                yield return new WaitForSeconds(.27f);Capture(fast ? "standalone-unarmed-dodge-fast.png" : "standalone-unarmed-dodge.png");
                yield return new WaitForSeconds(1.2f);
                Require(session.Model.Lives==3 && !weapon.IsEquipped && attack.ShotsFired==2 && session.Player.IsGrounded,"One unarmed jump did not clear the real rifle doublet and body.");
            }
            session.StartRun();yield return null;
            session.Spawner.enabled=false;
            HealthPickup medicinePrefab=null;
            RebelSoldier shieldPrefab=null,bazookaPrefab=null;
            foreach(var candidate in Resources.FindObjectsOfTypeAll<HealthPickup>())
                if(!candidate.gameObject.scene.IsValid()) { medicinePrefab=candidate;break; }
            foreach(var candidate in Resources.FindObjectsOfTypeAll<RebelSoldier>())
                if(!candidate.gameObject.scene.IsValid())
                {
                    if(candidate.name=="ShieldSoldier") shieldPrefab=candidate;
                    if(candidate.name=="BazookaSoldier") bazookaPrefab=candidate;
                }
            Require(medicinePrefab!=null && shieldPrefab!=null && bazookaPrefab!=null,"Medicine or reinforcement prefab missing from the player build.");
            var shield=Instantiate(shieldPrefab,new Vector3(0,-3.2f,0),Quaternion.identity,session.Spawner.transform);shield.Initialize(session);
            var launcher=Instantiate(bazookaPrefab,new Vector3(6,-3.2f,0),Quaternion.identity,session.Spawner.transform);launcher.Initialize(session);
            session.Hit();
            var medicine=Instantiate(medicinePrefab,new Vector3(-3,-2.65f,0),Quaternion.identity,session.Spawner.transform);medicine.Initialize(session);
            yield return new WaitForSeconds(.15f);Capture("standalone-reinforcements-and-medicine.png");
            medicine.GetComponent<Rigidbody2D>().position=new Vector2(session.Player.transform.position.x,-2.65f);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            Require(session.Model.Lives==3 && medicine==null,"Medicine did not restore exactly one life through physical contact.");
            shield.TakeProjectileHit();shield.TakeProjectileHit();shield.TakeProjectileHit();
            launcher.TakeProjectileHit();launcher.TakeProjectileHit();launcher.TakeProjectileHit();
            Require(shield.HitPoints==0 && launcher.HitPoints==0,"New infantry did not obey three-hit health.");
            session.StartRun();yield return null;
            Require(FindObjectsByType<HealthPickup>().Length==0,"Restart left medicine behind.");
            session.Spawner.enabled=false;
            VehicleEnemy tankPrefab=null,heliPrefab=null;
            foreach(var candidate in Resources.FindObjectsOfTypeAll<VehicleEnemy>())
                if(!candidate.gameObject.scene.IsValid())
                { if(candidate.Kind==VehicleEnemy.VehicleKind.Tank) tankPrefab=candidate;else heliPrefab=candidate; }
            Require(tankPrefab!=null && heliPrefab!=null,"Original tank/helicopter prefabs missing from the build.");
            var tank=Instantiate(tankPrefab,new Vector3(session.Player.transform.position.x+5.8f,-3.2f,0),Quaternion.identity,session.Spawner.transform);tank.Initialize(session);
            Require(tank.HitPoints==16,"Tank armour was not initialized.");
            weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);yield return new WaitForSeconds(1.03f);Capture("standalone-di-cokka.png");
            float armourDeadline=Time.time+3;while(tank.HitPoints>0 && Time.time<armourDeadline) yield return null;
            weapon.SetFireHeld(false);Require(tank.HitPoints==0 && weapon.ShotsFired>=16,"HMG fire failed to destroy the tank.");
            yield return new WaitForSeconds(.1f);Capture("standalone-tank-explosion.png");
            session.StartRun();session.Spawner.enabled=false;yield return null;
            session.Model.Advance(1000);
            var helicopter=Instantiate(heliPrefab,new Vector3(session.Player.transform.position.x+5.8f,-.25f,0),Quaternion.identity,session.Spawner.transform);helicopter.Initialize(session);
            yield return new WaitForSeconds(1.02f);Require(helicopter.ShotsFired==1,"Helicopter did not drop its warned bomb.");
            var bomb=FindAnyObjectByType<EnemyOrdnance>();Require(bomb!=null && bomb.IsGrenade,"Helicopter bomb missing.");
            var flyPose=helicopter.transform.Find("Visual").GetComponent<SpriteRenderer>();var frozenPose=flyPose.sprite;var frozenBomb=bomb.transform.position;
            Capture("standalone-r-shobu.png");session.TogglePause();yield return new WaitForSecondsRealtime(.2f);
            Require(flyPose.sprite==frozenPose && bomb.transform.position==frozenBomb,"Pause did not freeze the rotor/bomb.");
            session.TogglePause();float bombBlastDeadline=Time.time+2;
            while(!bomb.IsExploding && Time.time<bombBlastDeadline) yield return null;
            Require(bomb.IsExploding && session.Model.Lives==2,"An undodged helicopter bomb did not damage the fast-running dino.");
            yield return new WaitForSeconds(.12f);Capture("standalone-helicopter-bomb-damage.png");
            Require(session.Model.Lives==2,"The same bomb blast charged damage more than once.");
            int beforeBlast=session.Audio.ExplosionCount;
            for(int hit=0;hit<12;hit++) helicopter.TakeProjectileHit();
            Require(helicopter.HitPoints==0 && session.Audio.ExplosionCount==beforeBlast+1 && session.Audio.SoldierDeathCount==0,"Machinery destruction did not play one explosion.");
            yield return new WaitForSeconds(.12f);Capture("standalone-helicopter-explosion.png");
            Require(session.Presentation.CameraOffset==Vector3.zero,"Vehicle encounters moved the fixed camera.");
            session.StartRun();yield return null;
            Require(FindObjectsByType<VehicleEnemy>().Length==0 && FindObjectsByType<EnemyOrdnance>().Length==0,"Restart retained a vehicle or its ordnance.");
            session.Spawner.enabled=false;
            GroundObstacle nativeBagPrefab=null;
            foreach(var candidate in Resources.FindObjectsOfTypeAll<GroundObstacle>())
                if(!candidate.gameObject.scene.IsValid() && candidate.name=="SandbagBarricade") {nativeBagPrefab=candidate;break;}
            Require(nativeBagPrefab!=null,"The active course is missing its native sandbag obstacle.");
            var nativeBag=Instantiate(nativeBagPrefab,new Vector3(session.Player.transform.position.x+7,-3.2f,0),Quaternion.identity,session.Spawner.transform);nativeBag.Initialize(session);
            Require(nativeBag.GetComponentInChildren<SpriteRenderer>().sprite.name=="ms1_sandbag_barricade" && nativeBag.HitPoints==3,"Native sandbag art/health is incorrect.");
            Capture("standalone-original-terrain.png");
            weapon.CollectHeavyMachineGun();weapon.SetFireHeld(true);float bagDeadline=Time.time+1.4f;
            while(nativeBag!=null && nativeBag.HitPoints>0 && Time.time<bagDeadline) yield return null;
            weapon.SetFireHeld(false);Require(nativeBag==null || nativeBag.HitPoints==0,"Real gunfire could not destroy the native terrain obstacle.");
            foreach(var kind in new[]{MobileEnemy.EnemyKind.RebelBiker,MobileEnemy.EnemyKind.FlyingTara})
            {
                session.StartRun();session.Spawner.enabled=false;yield return new WaitForSeconds(.15f);
                MobileEnemy mobilePrefab=null;
                foreach(var candidate in Resources.FindObjectsOfTypeAll<MobileEnemy>())
                    if(!candidate.gameObject.scene.IsValid() && candidate.Kind==kind) { mobilePrefab=candidate;break; }
                Require(mobilePrefab!=null,"A new mobile enemy prefab was omitted from the executable.");
                var mobile=Instantiate(mobilePrefab,new Vector3(session.Player.transform.position.x+6.4f,kind==MobileEnemy.EnemyKind.RebelBiker ? -3.2f : .35f,0),Quaternion.identity,session.Spawner.transform);mobile.Initialize(session);
                weapon.CollectHeavyMachineGun();Capture(kind==MobileEnemy.EnemyKind.RebelBiker ? "standalone-rebel-biker.png" : "standalone-flying-tara.png");
                if(kind==MobileEnemy.EnemyKind.FlyingTara) session.Player.RequestJump();
                weapon.SetFireHeld(true);float mobileDeadline=Time.time+1.5f;
                while(mobile!=null && mobile.HitPoints>0 && Time.time<mobileDeadline) yield return null;
                weapon.SetFireHeld(false);Require(mobile==null || mobile.HitPoints==0,"Real HMG shots could not defeat a new mobile enemy in the executable.");
            }
            session.StartRun();session.Spawner.enabled=false;yield return new WaitForSeconds(.15f);
            MobileEnemy passingPrefab=null;
            foreach(var candidate in Resources.FindObjectsOfTypeAll<MobileEnemy>())
                if(!candidate.gameObject.scene.IsValid() && candidate.Kind==MobileEnemy.EnemyKind.FlyingTara) { passingPrefab=candidate;break; }
            var passing=Instantiate(passingPrefab,new Vector3(session.Player.transform.position.x+6.4f,.35f,0),Quaternion.identity,session.Spawner.transform);passing.Initialize(session);
            yield return new WaitForSeconds(.95f);session.Player.RequestJump();
            float passDeadline=Time.time+5;bool passedPlayer=false;
            while(passing!=null && Time.time<passDeadline)
            {
                Require(Mathf.Abs(passing.transform.position.y-.35f)<.001f,"Flying Tara climbed instead of passing by.");
                if(!passedPlayer && passing.transform.position.x<session.Player.transform.position.x-2)
                { passedPlayer=true;Capture("standalone-flying-tara-pass.png"); }
                yield return null;
            }
            Require(passedPlayer && passing==null && session.Model.Lives==3,"The aircraft did not pass safely and clear offscreen.");
            session.StartRun();session.Spawner.enabled=false;yield return null;
            Require(session.Maps!=null && session.Maps.MapCount==4,"The four score maps were not included in the build.");
            Vector3 mapCamera=Camera.main.transform.position;float mapRunnerX=session.Player.transform.position.x;
            weapon.CollectHeavyMachineGun();
            foreach(int milestone in new[]{3000,6000,9000,12000})
            {
                session.Model.Advance((milestone-session.Model.Score)/2f);yield return null;yield return null;
                Require(session.Maps.ActiveSector==milestone/3000,"The score milestone did not change maps in the executable.");
                Require(session.Maps.IsTransitioning,"The landscape changed instantly instead of entering from the right.");
                if(milestone==3000)
                {
                    yield return new WaitForSeconds(.9f);Capture("standalone-flowing-map-transition.png");
                    session.TogglePause();yield return null;float frozenBoundary=session.Maps.BoundaryX;
                    yield return new WaitForSecondsRealtime(.15f);Require(session.Maps.BoundaryX==frozenBoundary,"The transition moved while paused.");session.TogglePause();
                }
                float transition=Time.time+4;while(session.Maps.IsTransitioning && Time.time<transition) yield return null;
                Require(!session.Maps.IsTransitioning,"The map transition did not finish.");
                var mapGround=stage.transform.Find("Stone Ground/Tile 1").GetComponent<SpriteRenderer>();
                string expectedGround=milestone==3000 ? "ms1_beach_ground" : milestone==6000 ? "ms2_rail_ground" : milestone==9000 ? "ms2_factory_ground" : "ms1_stone_ground";
                Require(mapGround.sprite.name==expectedGround && Mathf.Abs(mapGround.bounds.max.y+3.2f)<.001f,"The map platform does not match its stage or the physical floor.");
                Require(weapon.Ammo==50 && session.Model.Lives==3,"A map transition reset ammunition or health.");
                Require(Mathf.Abs(session.Player.transform.position.x-mapRunnerX)<.001f && Camera.main.transform.position==mapCamera,"Changing maps moved the player or camera.");
                Capture(milestone==3000 ? "standalone-map-coast.png" : milestone==6000 ? "standalone-map-harbor.png" : milestone==9000 ? "standalone-map-factory.png" : "standalone-map-temple.png");
            }
            session.TogglePause();var mapPause=stage.transform.Find("Stone Ground/Tile 1").position;
            yield return new WaitForSecondsRealtime(.15f);Require(stage.transform.Find("Stone Ground/Tile 1").position==mapPause,"Map terrain moved while paused.");
            session.StartRun();Require(session.Maps.ActiveSector==0 && session.Model.Score==0,"Restart did not reset map progression.");
            session.StartRun();yield return null;
            session.Model.Advance(evidence.originalRecord + 200);
            evidence.expectedRecord = session.Model.HighScore;
            session.Hit(); session.Hit(); session.Hit();
            yield return new WaitForSeconds(1.3f);
            Require(session.Model.State == RunState.GameOver && session.Model.Lives == 0, "Three hits did not reach Game Over.");
            Capture("standalone-results.png");
            yield return new WaitForSecondsRealtime(.4f);
            InputSystem.RemoveDevice(keyboard);
            evidence.firstLaunchPassed = true;
        }

        private void Capture(string name)
        {
            // Hidden windows have no presented backbuffer. Render the real player pipeline explicitly.
            var camera = Camera.main;
            var canvas = FindAnyObjectByType<Canvas>();
            var target = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            int oldOrder = canvas.sortingOrder;
            var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
            canvas.planeDistance = 1; canvas.sortingOrder = 100;
            Canvas.ForceUpdateCanvases();
            var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            int nonBlack = 0;
            foreach (var pixel in image.GetPixels32()) if (pixel.r + pixel.g + pixel.b > 30) nonBlack++;
            File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
            camera.targetTexture = oldTarget; canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera; canvas.sortingOrder = oldOrder;
            RenderTexture.active = oldActive;
            target.Release(); Destroy(target); Destroy(image);
            Require(nonBlack > 10000, "The player's render target was blank.");
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
