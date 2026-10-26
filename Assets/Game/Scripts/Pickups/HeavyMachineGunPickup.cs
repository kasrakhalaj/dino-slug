using UnityEngine;

namespace DinoSlug
{
    public sealed class HeavyMachineGunPickup : Interactable
    {
        protected override void Interact()
        {
            Session.Player.Weapon.CollectHeavyMachineGun();
            Session.Player.EmitCollect(transform.position);
            Destroy(gameObject);
        }
    }
}
