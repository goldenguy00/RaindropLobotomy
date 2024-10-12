using System;
using System.Linq;
using EntityStates.AI;
using RoR2.CharacterAI;

namespace RaindropLobotomy.Enemies.SingingMachine
{
    public class BewitchedState : BaseState
    {
        public SingingMachineMain mainState;
        public BaseAI enemyAI;
        public CharacterBody enemyBody;
        private bool didWeExitEarly = false;
        private GameObject indicatorPrefab => Load<GameObject>("BewitchedIndicator.prefab");
        private GameObject indicator;
        private bool begunJump = false;
        private bool crushing = false;
        private AISkillDriver targetDriver;
        private BaseAI ai => enemyAI;

        public override void OnEnter()
        {
            base.OnEnter();

            mainState = EntityStateMachine.FindByCustomName(gameObject, "Body").state as SingingMachineMain;

            SphereSearch search = new()
            {
                radius = 50f,
                origin = base.transform.position,
                mask = LayerIndex.entityPrecise.mask
            };
            search.RefreshCandidates();
            search.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(TeamIndex.Player));
            search.FilterCandidatesByDistinctHurtBoxEntities();
            enemyBody = search.GetHurtBoxes()
                .OrderBy(hb => hb.healthComponent.health)
                .Select(x => x.healthComponent.body)
                .FirstOrDefault(body => !body.isBoss && body.moveSpeed > 0 && !body.isFlying && !body.isPlayerControlled);

            if (!enemyBody)
            {
                didWeExitEarly = true;
                outer.SetNextStateToMain();
                return;
            }

            enemyAI = enemyBody.master?.GetComponent<BaseAI>() ?? null;
            if (!enemyAI || !enemyBody.GetComponent<CharacterMotor>())
            {
                didWeExitEarly = true;
                outer.SetNextStateToMain();
                return;
            }

            targetDriver = enemyAI.gameObject.AddComponent<AISkillDriver>();
            targetDriver.customName = "ChaseInteractable";
            targetDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            targetDriver.moveTargetType = AISkillDriver.TargetType.Custom;
            targetDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            targetDriver.maxDistance = Mathf.Infinity;
            targetDriver.minDistance = 0f;
            targetDriver.skillSlot = SkillSlot.None;
            targetDriver.resetCurrentEnemyOnNextDriverSelection = true;
            targetDriver.shouldSprint = true;
            targetDriver.requireSkillReady = false;
            targetDriver.selectionRequiresAimTarget = false;
            targetDriver.selectionRequiresOnGround = false;
            targetDriver.activationRequiresTargetLoS = false;
            targetDriver.activationRequiresAimTargetLoS = false;
            targetDriver.activationRequiresAimConfirmation = false;
            AISkillDriver[] drivers = [.. enemyAI.skillDrivers, targetDriver];
            enemyAI.skillDrivers = drivers;

            // Debug.Log("Bewitching enemy: " + enemy);

            indicator = GameObject.Instantiate(indicatorPrefab, enemyBody.transform);
            indicator.transform.position = new(enemyBody.corePosition.x, enemyBody.corePosition.y + (enemyBody.radius) + 3f, enemyBody.corePosition.z);

            EffectManager.SimpleEffect(Paths.GameObject.OmniImpactExecute, indicator.transform.position, Quaternion.identity, false);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if ((base.fixedAge >= 30f || !enemyBody || !ai) && !crushing)
            {
                if (indicator) Destroy(indicator);
                outer.SetNextStateToMain();
                return;
            }

            if (!ai)
            {
                return;
            }

            if (!NetworkServer.active)
                return;

            ai.customTarget.gameObject = base.gameObject;

            if (ai.customTarget.gameObject)
            {
                ai.customTarget.Update();
                ai.SetGoalPosition(ai.customTarget.gameObject.transform.position);
                ai.localNavigator.Update(Time.fixedDeltaTime);

                ai.skillDriverEvaluation = new BaseAI.SkillDriverEvaluation
                {
                    dominantSkillDriver = targetDriver,
                    aimTarget = ai.customTarget,
                    target = ai.customTarget
                };
            }
            var bodyPosition = enemyBody.transform.position;
            var myPosition = base.transform.position;
            if (Vector3.Distance(bodyPosition, myPosition) < 20f)
            {
                mainState.UpdateLidState(SingingMachineMain.SingingMachineLidState.Open);
                mainState.disallowLidStateChange = true;
            }

            if (Vector3.Distance(bodyPosition, myPosition) < 5f && !begunJump)
            {
                begunJump = true;

                // Debug.Log("jumping");

                float ySpeed = Trajectory.CalculateInitialYSpeed(0.7f, myPosition.y - enemyBody.corePosition.y + 3f);
                float xOff = (myPosition.x - enemyBody.corePosition.x);
                float zOff = (myPosition.z - enemyBody.corePosition.z);

                Vector3 velocity = new(xOff / 0.7f, ySpeed, zOff / 0.7f);

                enemyBody.characterMotor.velocity = velocity;
                enemyBody.characterMotor.disableAirControlUntilCollision = true;
                enemyBody.characterMotor.Motor.ForceUnground();

                // Debug.Log(enemyBody.GetComponent<CharacterMotor>().velocity);
            }

            if (!crushing && Vector3.Distance(new(bodyPosition.x, 0, bodyPosition.z), new(myPosition.x, 0, myPosition.z)) < 0.6f)
            {
                crushing = true;
                // Debug.Log("CRUSHING!");
                enemyBody.characterMotor.enabled = false;
                enemyBody.baseMoveSpeed = 0f;
                enemyBody.moveSpeed = 0f;
                mainState.disallowLidStateChange = false;
                mainState.UpdateLidState(SingingMachineMain.SingingMachineLidState.Closed);
                mainState.disallowLidStateChange = true;
                enemyBody.healthComponent.Suicide();
            }

            if (crushing && (!enemyBody || !enemyBody.healthComponent.alive))
            {
                outer.SetNextState(new MusicState() { type = MusicState.SM_MusicType.Low });
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            if (indicator)
            {
                GameObject.Destroy(indicator);
            }
            skillLocator.secondary.rechargeStopwatch = didWeExitEarly ? 2f : 40f;
        }
    }
}