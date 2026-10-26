using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoSlug
{
    public sealed class RunnerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private GameSession session;
        private InputAction jump;
        private InputAction dash;
        private InputAction pause;
        private InputAction fire;

        public void Configure(InputActionAsset asset, GameSession owner) { actions = asset; session = owner; }

        private void OnEnable()
        {
            jump = actions.FindAction("Gameplay/Jump", true);
            dash = actions.FindAction("Gameplay/Dash", true);
            pause = actions.FindAction("System/Pause", true);
            fire = actions.FindAction("Gameplay/Fire", true);
            jump.performed += OnJump;
            dash.performed += OnDash;
            pause.performed += OnPause;
            fire.performed += OnFire;
            fire.canceled += OnFireCanceled;
            actions.Enable();
        }

        public void SetGameplayEnabled(bool enabled)
        {
            if (!enabled && session.Player.Weapon != null) session.Player.Weapon.SetFireHeld(false);
            var map = actions.FindActionMap("Gameplay", true);
            if (enabled) map.Enable(); else map.Disable();
        }

        private void OnDisable()
        {
            if (jump == null) return;
            jump.performed -= OnJump;
            dash.performed -= OnDash;
            pause.performed -= OnPause;
            fire.performed -= OnFire;
            fire.canceled -= OnFireCanceled;
            actions.Disable();
        }

        private void OnJump(InputAction.CallbackContext context) { session.Player.RequestJump(); }
        private void OnDash(InputAction.CallbackContext context) { session.Player.RequestDash(); }
        private void OnPause(InputAction.CallbackContext context) { session.TogglePause(); }
        private void OnFire(InputAction.CallbackContext context) { session.Player.Weapon?.SetFireHeld(true); }
        private void OnFireCanceled(InputAction.CallbackContext context) { session.Player.Weapon?.SetFireHeld(false); }
    }
}
