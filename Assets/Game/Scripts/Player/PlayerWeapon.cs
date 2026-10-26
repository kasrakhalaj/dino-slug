using TMPro;
using UnityEngine;

namespace DinoSlug
{
    /// <summary>Weapon pickups, bounded projectile pool, ammunition, and feedback.</summary>
    public sealed class PlayerWeapon : MonoBehaviour
    {
        public enum WeaponKind { None, HeavyMachineGun, Shotgun }
        [SerializeField] private GameSession session;
        [SerializeField] private SpriteRenderer gun;
        [SerializeField] private SpriteRenderer muzzle;
        [SerializeField] private Sprite bulletSprite;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private Sprite shotgunSprite;
        [SerializeField] private Sprite[] shotgunBlast;
        [SerializeField] private AudioClip shotgunPickupClip, shotgunShotClip;
        [SerializeField] private TMP_Text ammoLabel;
        [SerializeField] private TMP_Text announcement;
        [SerializeField] private Sprite[] gripFrames;
        [SerializeField] private Vector2[] gripOffsets;
        private AudioSource voice, shots;
        private Transform projectileRoot;
        private Transform visualRoot;
        private SpriteRenderer dinoVisual;
        private Sprite lastPose;
        private Vector3 gripOffset;
        private Vector3 visualRest, gunRest, bodyKick, gunKick;
        private Sprite machineGunSprite, machineGunFlash;
        private Vector3 muzzleRestScale;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private readonly SpriteRenderer[] bullets = new SpriteRenderer[16];
        private readonly float[] lifetimes = new float[16];
        private readonly Vector2[] velocities = new Vector2[16];
        private int nextBullet;
        private bool held, visible = true, paused;
        private float cooldown, flashTime, announcementTime, recoilTime, landingTime;
        private const float LandingDipSeconds = .1f;
        private bool pendingRound;
        private WeaponKind pendingKind, flashKind;
        public WeaponKind ActiveWeapon { get; private set; }
        public int Ammo { get; private set; }
        public int ShotsFired { get; private set; }
        public bool IsEquipped => Ammo > 0;
        public bool PickupVoicePlaying => voice != null && voice.isPlaying;
        public Vector3 MuzzlePosition => muzzle.transform.position;
        public Vector3 LastShotOrigin { get; private set; }

        public void Configure(GameSession owner, SpriteRenderer weapon, SpriteRenderer flash, Sprite tracer,
            AudioClip pickup, AudioClip shot, TMP_Text ammo, TMP_Text message)
        { session = owner; gun = weapon; muzzle = flash; bulletSprite = tracer; pickupClip = pickup; shotClip = shot; ammoLabel = ammo; announcement = message; }
        /// <summary>Briefly drops the dinosaur sprite one pixel so landings feel weighted.</summary>
        public void LandingDip() { landingTime = LandingDipSeconds; }
        public bool IsLandingDip => landingTime > 0;
        public void ConfigureGripMotion(Sprite[] poses, Vector2[] offsets) { gripFrames=poses;gripOffsets=offsets; }
        public void ConfigureShotgun(Sprite weapon, Sprite[] blast, AudioClip pickup, AudioClip shot)
        { shotgunSprite=weapon; shotgunBlast=blast; shotgunPickupClip=pickup; shotgunShotClip=shot; }

        private void Awake()
        {
            visualRoot = gun.transform.parent;
            dinoVisual = visualRoot.GetComponent<SpriteRenderer>();
            visualRest = visualRoot.localPosition;
            gunRest = gun.transform.localPosition;
            machineGunSprite=gun.sprite; machineGunFlash=muzzle.sprite;
            muzzleRestScale=muzzle.transform.localScale;
            voice = gameObject.AddComponent<AudioSource>();
            shots = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = shots.playOnAwake = false;
            voice.spatialBlend = shots.spatialBlend = 0;
            voice.volume = .85f;
            voice.priority = 32;
            projectileRoot = new GameObject("HMG Tracers").transform;
            for (int i = 0; i < bullets.Length; i++)
            {
                var item = new GameObject("HMG Tracer " + i, typeof(SpriteRenderer));
                item.transform.SetParent(projectileRoot, false);
                bullets[i] = item.GetComponent<SpriteRenderer>();
                bullets[i].sprite = bulletSprite;
                bullets[i].sharedMaterial = gun.sharedMaterial;
                bullets[i].sortingOrder = gun.sortingOrder + 1;
                bullets[i].enabled = false;
            }
            ResetWeapon();
        }

        public void CollectHeavyMachineGun()
        {
            Collect(WeaponKind.HeavyMachineGun,50,pickupClip,"HEAVY MACHINE GUN!");
        }

        public void CollectShotgun()
        {
            Collect(WeaponKind.Shotgun,30,shotgunPickupClip,"SHOTGUN!");
        }

        private void Collect(WeaponKind kind,int ammunition,AudioClip cue,string title)
        {
            if (!session.IsPlaying) return;
            ActiveWeapon=kind; Ammo=ammunition;
            gun.sprite=kind==WeaponKind.Shotgun ? shotgunSprite : machineGunSprite;
            pendingRound=false; flashTime=recoilTime=0; muzzle.enabled=false;
            cooldown = .25f;
            announcementTime = 2.8f;
            announcement.text = title;
            announcement.enabled = true;
            voice.Stop();
            voice.clip = cue;
            voice.Play();
            RefreshAmmo();
            gun.enabled = visible;
            session.Player.RequestPickupLaugh();
        }

        public void SetFireHeld(bool value) { held = value && session.IsPlaying; }
        public void SetVisible(bool value)
        {
            visible = value;
            gun.enabled = value && IsEquipped;
            if (!value) muzzle.enabled = false;
        }

        public void ResetWeapon()
        {
            held = false; Ammo = ShotsFired = nextBullet = 0;
            ActiveWeapon=WeaponKind.None;
            cooldown = flashTime = announcementTime = recoilTime = landingTime = 0;
            pendingRound = false;
            bodyKick = gunKick = Vector3.zero;
            gripOffset=Vector3.zero;lastPose=null;
            if (visualRoot != null) { visualRoot.localPosition = visualRest; gun.transform.localPosition = gunRest; }
            gun.enabled = muzzle.enabled = false;
            announcement.enabled = false;
            if (voice != null) voice.Stop();
            if (shots != null) shots.Stop();
            for (int i = 0; i < bullets.Length; i++)
            {
                lifetimes[i] = 0;
                if (bullets[i] != null) bullets[i].enabled = false;
            }
            RefreshAmmo();
        }

        private void Update()
        {
            bool shouldPause = session.Model.State == RunState.Paused;
            if (paused != shouldPause)
            {
                paused = shouldPause;
                if (paused) { voice.Pause(); shots.Pause(); }
                else { voice.UnPause(); shots.UnPause(); }
            }
            if (!session.IsPlaying) return;
            cooldown -= Time.deltaTime;
            recoilTime = Mathf.Max(0,recoilTime-Time.deltaTime);
            landingTime = Mathf.Max(0,landingTime-Time.deltaTime);
            flashTime -= Time.deltaTime;
            announcementTime -= Time.deltaTime;
            announcement.enabled = announcementTime > 0;
            muzzle.enabled = visible && flashTime > 0;
            if (flashKind==WeaponKind.Shotgun && flashTime>0 && shotgunBlast!=null && shotgunBlast.Length>0)
            {
                int frame=Mathf.Clamp((int)((.24f-flashTime)*shotgunBlast.Length/.24f),0,shotgunBlast.Length-1);
                muzzle.sprite=shotgunBlast[frame];
            }
            if (held && IsEquipped && cooldown <= 0) Fire();
        }

        private void LateUpdate()
        {
            // Apply after Animator evaluation. The physics root stays still and muzzle/arms follow the kick.
            if (session.IsPlaying)
            {
                if (dinoVisual.sprite!=lastPose)
                {
                    lastPose=dinoVisual.sprite;gripOffset=Vector3.zero;
                    if (gripFrames!=null && gripOffsets!=null)
                    {
                        int pose=System.Array.IndexOf(gripFrames,lastPose);
                        if (pose>=0 && pose<gripOffsets.Length) gripOffset=gripOffsets[pose];
                    }
                }
                bool shotgun=flashKind==WeaponKind.Shotgun;
                float strength=recoilTime/(shotgun ? .16f : .08f);
                float kick=shotgun ? 2f/16f : 1f/16f;
                bodyKick = new Vector3(strength>.3f ? -kick : 0, strength>.55f ? (ShotsFired%2==0 ? 1f : -1f)/32f : 0,0);
                gunKick = new Vector3(strength>.5f ? -kick : 0,0,0);
            }
            // One authored pixel (1/16 unit) of squash on touchdown; physics root is untouched.
            Vector3 dip = landingTime > 0 && (session.IsPlaying || session.Model.State==RunState.Paused) ? Vector3.down/16f : Vector3.zero;
            visualRoot.localPosition = visualRest+bodyKick+dip;
            gun.transform.localPosition = gunRest+gripOffset+gunKick;
            if (pendingRound && session.IsPlaying)
            {
                pendingRound=false;
                LastShotOrigin=muzzle.transform.position;
                if (session.Presentation!=null) session.Presentation.Fired(LastShotOrigin,pendingKind==WeaponKind.Shotgun);
                int pellets=pendingKind==WeaponKind.Shotgun ? 5 : 1;
                for (int p=0;p<pellets;p++)
                {
                    int index=nextBullet; nextBullet=(nextBullet+1)%bullets.Length;
                    float angle=pellets==1 ? 0 : (p-2)*6f*Mathf.Deg2Rad;
                    velocities[index]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*28f;
                    bullets[index].transform.position=LastShotOrigin;
                    bullets[index].transform.rotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                    bullets[index].transform.localScale=Vector3.one*(pellets==1 ? 1.25f : .5625f);
                    bullets[index].enabled=true;
                    // Five pellets, each one HP; a close blast defeats a three-HP target.
                    lifetimes[index]=pellets==1 ? .8f : .17f;
                }
            }
        }

        private void Fire()
        {
            Ammo--; ShotsFired++;
            bool shotgun=ActiveWeapon==WeaponKind.Shotgun;
            pendingKind=flashKind=ActiveWeapon;
            cooldown = shotgun ? .65f : .09f;
            flashTime = shotgun ? .24f : .045f;
            recoilTime = shotgun ? .16f : .08f;
            muzzle.sprite=shotgun ? shotgunBlast[0] : machineGunFlash;
            // The S blast's native pivot is at its left edge, on the same barrel anchor.
            muzzle.transform.localScale=shotgun ? Vector3.one/.75f : muzzleRestScale;
            pendingRound = true;
            muzzle.enabled = visible;
            shots.PlayOneShot(shotgun ? shotgunShotClip : shotClip,voice.isPlaying ? .16f : .4f);
            RefreshAmmo();
            if (Ammo == 0) gun.enabled = false;
        }

        private void FixedUpdate()
        {
            if (!session.IsPlaying) return;
            // Keep the existing result buffer, default layers and global trigger-query setting.
            var collisionFilter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
            collisionFilter.SetLayerMask(Physics2D.DefaultRaycastLayers);
            for (int i = 0; i < bullets.Length; i++)
            {
                if (lifetimes[i] <= 0) continue;
                var tracer = bullets[i];
                float travel=Mathf.Min(Time.fixedDeltaTime,lifetimes[i])*28f;
                Vector2 direction=velocities[i]/28f;
                int count = Physics2D.CircleCast(tracer.transform.position, .1f, direction, collisionFilter, hits, travel);
                Interactable target = null;
                Vector2 impact=tracer.transform.position;
                float closest = float.MaxValue;
                for (int j = 0; j < count; j++)
                    if (hits[j].collider.TryGetComponent<Interactable>(out var item) && item.IsProjectileTarget && hits[j].distance < closest)
                    { target = item; closest = hits[j].distance; impact=hits[j].point; }
                if (target != null && target.TakeProjectileHit(impact)) lifetimes[i] = 0;
                else
                {
                    tracer.transform.position += (Vector3)(direction*travel);
                    lifetimes[i] -= Time.fixedDeltaTime;
                }
                tracer.enabled = lifetimes[i] > 0;
            }
        }

        private void RefreshAmmo() { ammoLabel.text = IsEquipped ? (ActiveWeapon==WeaponKind.Shotgun ? "S  " : "H  ") + Ammo.ToString("D3") + "   |   HOLD J / LEFT MOUSE" : "FIND H OR S TO ARM UP"; }
        private void OnDisable() { held = false; if (voice != null) voice.Stop(); if (shots != null) shots.Stop(); }
        private void OnDestroy() { if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
