using System;

namespace RaindropLobotomy.Ordeals.Noon.Green
{
    public class Chaingun : BaseSkillState
    {
        public static float duration = 6f;
        public static float startDelay = 1f;

        private int hitRate = 5;
        private float aimTimer = 2f;
        private float delay;
        private bool defensive;
        private float stopwatch;

        private Animator modelAnimator;
        private Transform muzzle;

        public static GameObject tracer, hitEffect, flash;

        public override void OnEnter()
        {
            base.OnEnter();

            muzzle = FindModelChild("MuzzleCannon");

            modelAnimator = GetModelAnimator();
            defensive = modelAnimator.GetBool("isDefensive");

            modelAnimator.SetBool("isFiring", true);
            PlayAnimation("Gesture, Override", defensive ? "Defensive Fire" : "Fire", "Standard.playbackRate", startDelay);

            hitRate = Mathf.CeilToInt(hitRate * base.attackSpeedStat);

            if (defensive)
            {
                hitRate *= 5;
                aimTimer = 10f;
            }

            delay = 1f / hitRate;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= duration)
            {
                outer.SetNextStateToMain();
                return;
            }


            if (base.fixedAge >= startDelay)
            {
                stopwatch += Time.fixedDeltaTime;

                if (stopwatch >= delay)
                {
                    stopwatch = 0f;

                    base.StartAimMode(aimTimer);

                    FireBulletAuthority();
                }
            }
        }

        public void FireBulletAuthority()
        {
            if (!isAuthority) return;

            new BulletAttack()
            {
                owner = base.gameObject,
                weapon = base.gameObject,
                origin = muzzle.position,
                aimVector = -muzzle.forward,
                minSpread = 4f,
                maxSpread = 9f,
                damage = base.damageStat,
                force = 40f,
                tracerEffectPrefab = tracer,
                muzzleName = "MuzzleCannon",
                hitEffectPrefab = hitEffect,
                isCrit = Util.CheckRoll(critStat, base.characterBody.master),
                radius = 0.2f,
                smartCollision = true
            }.Fire();

            EffectManager.SimpleMuzzleFlash(flash, base.gameObject, "MuzzleCannon", true);

            AkSoundEngine.PostEvent(Events.Play_commando_R, base.gameObject);
        }

        public override void OnExit()
        {
            base.OnExit();
            modelAnimator.SetBool("isFiring", false);
            base.StartAimMode(2f);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}