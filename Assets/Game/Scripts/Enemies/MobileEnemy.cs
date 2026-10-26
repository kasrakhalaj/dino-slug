using UnityEngine;

namespace DinoSlug
{
    /// <summary>Fast ground rush and a short, warned aircraft engagement.</summary>
    public sealed class MobileEnemy : Interactable
    {
        public enum EnemyKind { RebelBiker, FlyingTara }
        [SerializeField] private EnemyKind kind;
        [SerializeField] private SpriteRenderer visual,warning;
        [SerializeField] private Sprite[] movement,explosion;
        [SerializeField] private Sprite round;
        [SerializeField] private AudioClip discharge;
        private float clock,attackClock,deathClock;
        private bool holding,departing,defeated;
        public EnemyKind Kind => kind;
        public int ShotsFired { get; private set; }
        public bool IsWindingUp => holding && ShotsFired==0 && !defeated;
        public bool IsPassing => departing && !defeated;
        public bool EncounterActive => kind==EnemyKind.FlyingTara && !departing && !defeated;
        public override bool IsProjectileTarget => true;
        protected override int InitialHitPoints => kind==EnemyKind.RebelBiker ? 4 : 3;
        protected override float ApproachSpeed => defeated ? -Session.WorldSpeed : kind==EnemyKind.RebelBiker ? 2.5f : holding ? -Session.WorldSpeed : departing ? 3 : 1.5f;
        public void Configure(EnemyKind type,SpriteRenderer image,SpriteRenderer alert,Sprite[] frames,Sprite[] blast,Sprite projectile,AudioClip sound)
        { kind=type;visual=image;warning=alert;movement=frames;explosion=blast;round=projectile;discharge=sound; }
        private void Update()
        {
            if(Session==null || !Session.IsPlaying) return;
            float dt=Time.deltaTime;clock+=dt;
            if(defeated)
            {
                deathClock+=dt;visual.sprite=explosion[Mathf.Min((int)(deathClock*20),explosion.Length-1)];
                if(deathClock>.45f) Destroy(gameObject);return;
            }
            visual.sprite=movement[(int)(clock*14)%movement.Length];
            float distance=transform.position.x-Session.Player.transform.position.x;
            if(kind==EnemyKind.RebelBiker)
            {
                warning.enabled=distance>0 && distance<Session.Model.BaseSpeed*1.3f;
                warning.color=new Color(1,.38f,.12f);return;
            }
            if(!holding && !departing && distance<=6.5f) holding=true;
            if(holding)
            {
                attackClock+=dt;warning.enabled=ShotsFired==0;warning.color=attackClock<.35f ? new Color(1,.85f,.2f) : new Color(1,.3f,.12f);
                if(attackClock>=.75f && ShotsFired==0)
                {
                    var shot=new GameObject("Flying Tara Aimed Round",typeof(SpriteRenderer),typeof(EnemyOrdnance));shot.transform.SetParent(transform.parent,false);
                    shot.transform.position=transform.position+new Vector3(-1.2f,-.0625f,0);
                    var ordnance=shot.GetComponent<EnemyOrdnance>();ordnance.Launch(Session,false,round,explosion,visual.sharedMaterial);
                    ordnance.ConfigureAimedRound(new Vector2(Session.Player.transform.position.x,-2.35f),.6f);
                    Session.Audio.PlayRifleShot(discharge,shot.transform.position);ShotsFired++;
                }
                if(attackClock>=1.35f) { holding=false;departing=true;warning.enabled=false; }
            }
            // After the strafe, continue left at the same altitude. Interactable
            // removes the aircraft once it has passed beyond the left edge.
        }
        protected override void Interact()
        {
            Session.Hit();warning.enabled=false;
            if(kind==EnemyKind.FlyingTara) { holding=false;departing=true; }
        }
        protected override void OnProjectileDestroyed()
        {
            defeated=true;warning.enabled=false;
            Session.Audio.PlayExplosion(transform.position);Session.Presentation.Exploded(transform.position);
            if(kind==EnemyKind.RebelBiker) { Session.Audio.PlaySoldierDeath(transform.position);Session.Spawner.TryDropMedicine(transform.position); }
            visual.flipX=false;
        }
    }
}
