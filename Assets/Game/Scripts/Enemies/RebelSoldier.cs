using UnityEngine;

namespace DinoSlug
{
    public sealed class RebelSoldier : Interactable
    {
        [SerializeField] private FrameAnimation animation;
        [SerializeField] private Sprite[] deathFrames;
        public override bool IsProjectileTarget => true;
        public GameSession Owner => Session;
        private RebelSoldier formationLeader;
        public void FollowFormation(RebelSoldier leader) { formationLeader=leader; }
        protected override float ApproachSpeed => HitPoints<=0 ? 0 : formationLeader!=null && formationLeader.HitPoints>0
            ? formationLeader.ApproachSpeed : attack==null || attack.IsAdvancing ? 1.25f : 0;
        private SoldierAttack attack;
        private void Awake() { attack=GetComponent<SoldierAttack>(); }
        public void Configure(FrameAnimation visual, Sprite[] defeated) { animation = visual; deathFrames = defeated; }
        protected override void Interact() { Session.Hit(); }
        protected override void OnProjectileDestroyed()
        {
            Session.Audio.PlaySoldierDeath(transform.position);
            Session.Spawner.TryDropMedicine(transform.position);
            if (animation == null || deathFrames == null || deathFrames.Length == 0)
            { base.OnProjectileDestroyed(); return; }
            animation.PlayOnce(deathFrames,12);
            Destroy(gameObject,deathFrames.Length/12f+.08f);
        }
    }
}
