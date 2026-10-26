using UnityEngine;

namespace DinoSlug
{
    [RequireComponent(typeof(RebelSoldier))]
    public sealed class SoldierAttack : MonoBehaviour
    {
        public enum Weapon { Rifle, Grenade, Bazooka }
        [SerializeField] private Weapon kind;
        [SerializeField] private FrameAnimation animation;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer heldGrenade;
        [SerializeField] private Sprite[] walkingFrames, attackFrames, blastFrames;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private Vector2 attackOrigin;
        [SerializeField] private SpriteRenderer muzzleFlash;
        [SerializeField] private SpriteRenderer warning;
        private RebelSoldier soldier;
        private Collider2D contact;
        private float clock;
        private float flashRemaining;
        private bool attacking, finished;
        public Weapon Kind => kind;
        public int ShotsFired { get; private set; }
        public bool IsWindingUp => attacking && ShotsFired == 0;
        public bool IsAdvancing => !attacking;
        public Vector3 AttackOrigin => transform.TransformPoint(attackOrigin);
        public Vector3 LastShotOrigin { get; private set; }

        public void Configure(Weapon weapon, FrameAnimation motion, SpriteRenderer renderer, Sprite[] walk,
            Sprite[] attack, Sprite projectile, Sprite[] explosion, AudioClip shot)
        { kind=weapon; animation=motion; visual=renderer; walkingFrames=walk; attackFrames=attack; projectileSprite=projectile; blastFrames=explosion; shotClip=shot; }
        public void ConfigureHeldGrenade(SpriteRenderer grenade) { heldGrenade=grenade; }
        public void ConfigureAttackOrigin(Vector2 origin, SpriteRenderer flash) { attackOrigin=origin;muzzleFlash=flash; }
        public void ConfigureWarning(SpriteRenderer renderer) { warning=renderer; }

        private void Awake()
        {
            soldier=GetComponent<RebelSoldier>(); contact=GetComponent<Collider2D>();
        }

        private void Update()
        {
            var session=soldier.Owner;
            if (session == null) return;
            if (!contact.enabled || soldier.HitPoints <= 0)
            { if (heldGrenade != null) heldGrenade.enabled=false; if (muzzleFlash!=null) muzzleFlash.enabled=false; if (warning!=null) warning.enabled=false; attacking=false; finished=true; return; }
            if (!session.IsPlaying) return;
            flashRemaining-=Time.deltaTime;
            if (muzzleFlash!=null) muzzleFlash.enabled=flashRemaining>0;
            if (kind==Weapon.Bazooka) visual.transform.localPosition=Vector3.right*(flashRemaining>0 ? 1f/24f : 0);
            if (finished) return;
            float distance=transform.position.x-session.Player.transform.position.x;
            if (!attacking)
            {
                // One second until the body reaches the player, at either run speed.
                // Keeping this timing (rather than a fixed distance) lets one jump
                // clear both the tight volley and the soldier that follows it.
                float range=kind!=Weapon.Grenade ? session.Model.BaseSpeed : 13f;
                if (distance > range || distance < 1f) return;
                attacking=true;
                animation.Hold(attackFrames[0]);
            }
            clock += Time.deltaTime;
            const float windup=.7f;
            if (warning!=null)
            {
                warning.enabled=clock<windup;
                warning.color=clock<.35f ? new Color(1,.85f,.25f) : new Color(1,.3f,.15f);
            }
            // The grenade is already drawn in the native hand cels. Release as
            // the forward arm opens, instead of playing a knife attack early.
            if (kind==Weapon.Grenade) animation.Hold(attackFrames[Mathf.Min((int)(clock/.07f),attackFrames.Length-1)]);
            int rounds=kind==Weapon.Rifle ? 2 : 1;
            const float burstSpacing=.1f;
            if (ShotsFired < rounds && clock >= windup+ShotsFired*burstSpacing)
            {
                var shot=new GameObject(kind==Weapon.Rifle ? "Enemy Rifle Round" : kind==Weapon.Bazooka ? "Enemy Rocket" : "Enemy Grenade",typeof(SpriteRenderer),typeof(EnemyOrdnance));
                shot.transform.SetParent(transform.parent,false);
                LastShotOrigin=AttackOrigin;
                shot.transform.position=LastShotOrigin;
                shot.GetComponent<EnemyOrdnance>().Launch(session,kind==Weapon.Grenade,projectileSprite,blastFrames,visual.sharedMaterial);
                if (kind==Weapon.Bazooka) shot.GetComponent<EnemyOrdnance>().ConfigureRocket();
                ShotsFired++;
                if (heldGrenade != null) heldGrenade.enabled=false;
                if (kind!=Weapon.Grenade) session.Audio.PlayRifleShot(shotClip,LastShotOrigin);
                if (kind!=Weapon.Grenade)
                {
                    animation.PlayOnce(attackFrames,16);
                    flashRemaining=.055f;
                    if (muzzleFlash!=null) muzzleFlash.enabled=true;
                }
            }
            if (clock >= windup+(rounds-1)*burstSpacing+.3f)
            { attacking=false; finished=true; animation.PlayLoop(walkingFrames,10); }
        }
    }
}
