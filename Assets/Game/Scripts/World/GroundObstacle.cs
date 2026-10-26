namespace DinoSlug
{
    public sealed class GroundObstacle : Interactable
    {
        public override bool IsProjectileTarget => true;
        protected override void Interact() { Session.Hit(); }
    }
}
