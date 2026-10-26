using UnityEngine;

namespace DinoSlug
{
    /// <summary>A larger enemy that matches the runner's pace, then retreats if not destroyed.</summary>
    public sealed class VehicleEnemy : Interactable
    {
        public enum VehicleKind { Tank, Helicopter }
        [SerializeField] private VehicleKind kind;
        [SerializeField] private SpriteRenderer visual,warning,blast;
        [SerializeField] private Sprite[] movementFrames,fireFrames,deathFrames,explosionFrames;
        [SerializeField] private Sprite projectile;
        [SerializeField] private AudioClip discharge;
        [SerializeField] private Vector2 muzzle;
        private float age,engageClock,warningClock,deathClock,flash;
        private bool engaging,retreating,destroyed;
        public GameSession Owner => Session;
        public VehicleKind Kind => kind;
        public override bool IsProjectileTarget => true;
        protected override int InitialHitPoints => kind==VehicleKind.Tank ? 16 : 12;
        protected override float ApproachSpeed => destroyed ? 0 : retreating ? -Session.WorldSpeed-7 : engaging ? -Session.WorldSpeed : 0;
        public int ShotsFired { get; private set; }
        public bool IsWindingUp => engaging && !retreating && !destroyed && warningClock>0 && warningClock<.9f;
        public bool IsRetreating => retreating;
        public bool EncounterActive => !retreating && !destroyed;
        public Vector3 LastShotOrigin { get; private set; }

        public void Configure(VehicleKind type,SpriteRenderer image,SpriteRenderer alert,SpriteRenderer explosion,
            Sprite[] movement,Sprite[] firing,Sprite[] defeated,Sprite[] detonation,Sprite round,AudioClip sound,Vector2 origin)
        {
            kind=type;visual=image;warning=alert;blast=explosion;movementFrames=movement;fireFrames=firing;
            deathFrames=defeated;explosionFrames=detonation;projectile=round;discharge=sound;muzzle=origin;
        }
        private void Update()
        {
            if(Session==null || !Session.IsPlaying) return;
            float dt=Time.deltaTime;age+=dt;
            if(destroyed)
            {
                deathClock+=dt;warning.enabled=false;
                if(kind==VehicleKind.Tank && deathFrames.Length>0) visual.sprite=deathFrames[Mathf.Min((int)(deathClock*14),deathFrames.Length-1)];
                if(kind==VehicleKind.Helicopter) { visual.transform.localPosition+=Vector3.down*(deathClock*3*dt);visual.transform.localRotation=Quaternion.Euler(0,0,-deathClock*35); }
                blast.enabled=deathClock<.45f;
                if(blast.enabled) blast.sprite=explosionFrames[Mathf.Min((int)(deathClock*20),explosionFrames.Length-1)];
                if(deathClock>1.25f) Destroy(gameObject);
                return;
            }
            visual.sprite=movementFrames[(int)(age*(kind==VehicleKind.Tank ? 12 : 18))%movementFrames.Length];
            if(retreating) { warning.enabled=false;if(transform.position.x>17) Destroy(gameObject);return; }
            if(!engaging)
            {
                if(transform.position.x-Session.Player.transform.position.x>6) return;
                engaging=true;
            }
            engageClock+=dt;warningClock+=dt;flash=Mathf.Max(0,flash-dt);
            float limit=kind==VehicleKind.Tank ? 6.9f : 6.2f;
            if(engageClock>=limit) { retreating=true;warning.enabled=false;return; }
            float cycle=kind==VehicleKind.Tank ? 2.3f : 2.7f;
            const float windup=.9f;
            warning.enabled=warningClock<windup;
            warning.color=warningClock<.45f ? new Color(1,.85f,.25f) : new Color(1,.3f,.15f);
            if(kind==VehicleKind.Tank && warningClock<windup) visual.sprite=fireFrames[0];
            if(warningClock>=windup && warningClock-dt<windup) Fire();
            if(flash>0 && kind==VehicleKind.Tank) visual.sprite=fireFrames[Mathf.Min((int)((.25f-flash)*12),fireFrames.Length-1)];
            if(warningClock>=cycle) warningClock-=cycle;
        }
        private void Fire()
        {
            var shot=new GameObject(kind==VehicleKind.Tank ? "Tank Cannon Shell" : "Helicopter Bomb",typeof(SpriteRenderer),typeof(EnemyOrdnance));
            shot.transform.SetParent(transform.parent,false);LastShotOrigin=transform.TransformPoint(muzzle);shot.transform.position=LastShotOrigin;
            var ordnance=shot.GetComponent<EnemyOrdnance>();
            ordnance.Launch(Session,kind==VehicleKind.Helicopter,projectile,explosionFrames,visual.sharedMaterial);
            if(kind==VehicleKind.Helicopter) ordnance.ConfigureDroppedBomb();
            if(kind==VehicleKind.Tank)
            {
                // Arrive low at the runner's fixed lane, regardless of a jump already in progress.
                Vector2 target=new Vector2(Session.Player.transform.position.x,-2.35f);
                const float flight=.5f;
                Vector2 speed=(target-(Vector2)LastShotOrigin)/flight+Vector2.right*Session.WorldSpeed;
                speed.y+=12*flight;
                ordnance.ConfigureVehicleShell(speed);
                Session.Audio.PlayRifleShot(discharge,LastShotOrigin);
            }
            ShotsFired++;flash=.25f;
        }
        protected override void Interact() { Session.Hit(); }
        protected override void OnProjectileDestroyed()
        {
            destroyed=true;warning.enabled=false;deathClock=0;
            Session.Audio.PlayExplosion(transform.position);
            if(Session.Presentation!=null) Session.Presentation.Exploded(transform.position+Vector3.up);
            Session.Spawner.TryDropMedicine(transform.position);
        }
    }
}
