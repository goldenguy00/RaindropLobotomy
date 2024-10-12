using System;

namespace RaindropLobotomy.Enemies.SteamMachine
{
    public class Spray : BaseState
    {
        private GameObject sprayInstance;
        private Transform muzzle;
        private Timer sprayTimer = new(0.2f, false, true, false, true);
        private bool started = false;
        private Animator animator;
        public override void OnEnter()
        {
            base.OnEnter();
            PlayAnimation("Gesture, Override", "Spray", "Spray.playbackRate", 8f);
            muzzle = FindModelChild("Nozzle");
            animator = GetModelAnimator();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.characterBody.SetAimTimer(1f);

            if (!started && animator.GetFloat("sprayBegun") >= 0.5f)
            {
                FindModelChild("Spray").GetComponent<ParticleSystem>().Play();
                started = true;
            }

            if (animator.GetFloat("sprayBegun") <= 0.2f && started)
            {
                outer.SetNextStateToMain();
                return;
            }


            if (sprayTimer.Tick() && started && base.isAuthority)
            {
                new BulletAttack()
                {
                    damage = base.damageStat * 4f * sprayTimer.duration,
                    aimVector = -muzzle.transform.forward,
                    weapon = muzzle.gameObject,
                    owner = base.gameObject,
                    falloffModel = BulletAttack.FalloffModel.None,
                    isCrit = base.RollCrit(),
                    stopperMask = LayerIndex.world.mask,
                    origin = muzzle.transform.position,
                    procCoefficient = 1f,
                    radius = 1f,
                    smartCollision = true,
                    maxDistance = 35f,
                    muzzleName = "Nozzle"
                }.Fire();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            FindModelChild("Spray").GetComponent<ParticleSystem>().Stop();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}