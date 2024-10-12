using System;

namespace RaindropLobotomy.Enemies.SteamMachine
{
    public class Slam : BaseState
    {
        public Transform clawMuzzle;
        public bool performedSlam = false;
        private Vector3 forward;
        private OverlapAttack attack;
        private Animator animator;
        public override void OnEnter()
        {
            base.OnEnter();
            clawMuzzle = FindModelChild("Claw");
            PlayAnimation("Gesture, Override", "Slash", "Slam.playbackRate", 1f);
            attack = new()
            {
                damage = base.damageStat * 5f,
                attacker = base.gameObject,
                hitBoxGroup = FindHitBoxGroup("SlashHitbox"),
                teamIndex = GetTeam(),
                isCrit = RollCrit(),
                procCoefficient = 1f,
                pushAwayForce = 2000f,
                forceVector = Vector3.up
            };

            base.characterBody.SetAimTimer(0.3f);

            animator = GetModelAnimator();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (animator.GetFloat("slashBegun") >= 0.5f && base.isAuthority)
            {
                attack.Fire();
            }

            if (base.fixedAge >= 2f)
            {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}