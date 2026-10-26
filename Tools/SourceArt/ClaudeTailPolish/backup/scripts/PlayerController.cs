using UnityEngine;

namespace DinoSlug
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer head;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private float jumpSpeed = 13f;
        [SerializeField] private float dashDuration = .24f;
        [SerializeField] private float dashCooldown = 2.4f;
        private Rigidbody2D body;
        private Vector3 origin;
        private float dashRemaining;
        private float cooldownRemaining;
        private float hitRemaining;
        private float jumpBuffer;
        private float coyoteRemaining;
        private float laughRequestRemaining;
        private bool dead;
        private bool grounded;
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private static readonly int PickupLaugh = Animator.StringToHash("PickupLaugh");
        private static readonly int Armed = Animator.StringToHash("Armed");
        public bool IsDashing => dashRemaining > 0 && !dead;
        public bool IsGrounded => grounded;
        public bool IsLaughing => animator.GetCurrentAnimatorStateInfo(0).IsName("Laugh");
        public PlayerWeapon Weapon => weapon;
        public float DashReadyFraction => 1f - Mathf.Clamp01(cooldownRemaining / dashCooldown);

        public void Configure(GameSession owner, Animator animation, SpriteRenderer sprite, ParticleSystem trail, ParticleSystem burst)
        { session = owner; animator = animation; visual = sprite; dust = trail; sparks = burst; }

        public void ConfigureHead(SpriteRenderer face) { head = face; }
        public void ConfigureWeapon(PlayerWeapon value) { weapon = value; }

        private void Awake() { body = GetComponent<Rigidbody2D>(); origin = transform.position; }

        public void ResetRunner()
        {
            if (body == null) { body = GetComponent<Rigidbody2D>(); origin = transform.position; }
            body.position = origin;
            body.linearVelocity = Vector2.zero;
            body.simulated = true;
            dead = false;
            if (weapon != null) weapon.ResetWeapon();
            grounded = true;
            dashRemaining = cooldownRemaining = hitRemaining = jumpBuffer = laughRequestRemaining = 0;
            coyoteRemaining = .09f;
            visual.color = Color.white;
            if (head != null) head.color = Color.white;
            animator.Rebind();
            animator.Update(0);
            animator.SetBool(Dead, false);
            animator.SetBool(Grounded, true);
            animator.SetFloat(Armed, 0);
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!session.IsPlaying)
            {
                if (dust.isEmitting) dust.Stop();
                return;
            }
            dashRemaining = Mathf.Max(0, dashRemaining - Time.deltaTime);
            cooldownRemaining = Mathf.Max(0, cooldownRemaining - Time.deltaTime);
            hitRemaining = Mathf.Max(0, hitRemaining - Time.deltaTime);
            animator.SetFloat(Armed, weapon != null && weapon.IsEquipped ? 1f : 0f);
            if (laughRequestRemaining > 0)
            {
                laughRequestRemaining -= Time.deltaTime;
                // Queue an airborne pickup until landing; damage and death retain priority.
                if (grounded && hitRemaining <= 0 && animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
                {
                    animator.SetTrigger(PickupLaugh);
                    laughRequestRemaining = 0;
                }
            }
            jumpBuffer = Mathf.Max(0, jumpBuffer - Time.deltaTime);
            coyoteRemaining = grounded ? .09f : Mathf.Max(0, coyoteRemaining - Time.deltaTime);
            visual.color = hitRemaining > 0 ? new Color(1f, .48f, .42f) : (IsDashing ? new Color(.75f, 1f, 1f) : Color.white);
            if (head != null) head.color = visual.color;
            animator.speed = IsDashing ? 1.8f : 1f;
            if (grounded && !dust.isEmitting) dust.Play();
            if (!grounded && dust.isEmitting) dust.Stop();
        }

        private void FixedUpdate()
        {
            if (!session.IsPlaying || dead) return;
            if (jumpBuffer > 0 && coyoteRemaining > 0)
            {
                body.linearVelocity = new Vector2(0, jumpSpeed);
                grounded = false;
                coyoteRemaining = jumpBuffer = 0;
                animator.SetBool(Grounded, false);
                session.Audio.Play("jump");
            }
        }

        public void RequestJump() { if (session.IsPlaying && !dead) jumpBuffer = .12f; }
        public void RequestPickupLaugh() { if (session.IsPlaying && !dead) laughRequestRemaining = 1.8f; }

        public void SetVisible(bool visible) { visual.enabled = visible; if (head != null) head.enabled = visible; if (weapon != null) weapon.SetVisible(visible); }

        public void RequestDash()
        {
            if (!session.IsPlaying || dead || cooldownRemaining > 0) return;
            dashRemaining = dashDuration;
            cooldownRemaining = dashCooldown;
            session.Audio.Play("dash");
        }

        public void ReceiveHit(bool fatal)
        {
            laughRequestRemaining = 0;
            animator.ResetTrigger(PickupLaugh);
            hitRemaining = .28f;
            sparks.transform.position = transform.position + Vector3.up * .8f;
            sparks.Emit(15);
            if (fatal)
            {
                if (weapon != null) weapon.ResetWeapon();
                dead = true;
                dashRemaining = jumpBuffer = 0;
                visual.color = Color.white;
                if (head != null) head.color = Color.white;
                animator.speed = 1f;
                animator.SetBool(Dead, true);
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
                dust.Stop();
            }
            else animator.SetTrigger(Hit);
        }

        public void EmitCollect(Vector3 position)
        {
            sparks.transform.position = position;
            sparks.Emit(8);
        }

        private void OnCollisionEnter2D(Collision2D collision) { CheckGround(collision); }
        private void OnCollisionStay2D(Collision2D collision) { CheckGround(collision); }
        private void CheckGround(Collision2D collision)
        {
            if (dead || body.linearVelocity.y > .1f) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (collision.GetContact(i).normal.y > .6f)
                {
                    if (!grounded && session.Presentation!=null) session.Presentation.Landed(transform.position);
                    grounded = true; animator.SetBool(Grounded, true); return;
                }
        }
        private void OnCollisionExit2D(Collision2D collision) { grounded = false; }
    }
}
