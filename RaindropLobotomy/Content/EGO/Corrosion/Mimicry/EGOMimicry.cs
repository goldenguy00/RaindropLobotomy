using System;
using System.Linq;
using RaindropLobotomy.Buffs;
using UnityEngine.SceneManagement;
using R2API.Networking.Interfaces;
using UnityEngine.Networking.Types;
using R2API.Networking;
using System.Collections;

namespace RaindropLobotomy.EGO.Viend
{
    public class EGOMimicry : CorrosionBase<EGOMimicry>
    {
        public override string EGODisplayName => "Mimicry";

        public override string Description => "And the many shells cried out one word...";

        public override SurvivorDef TargetSurvivorDef => Paths.SurvivorDef.VoidSurvivor;

        public override UnlockableDef RequiredUnlock => null;

        public override Color Color => new Color32(212, 0, 4, 255);

        public override SurvivorDef Survivor => Load<SurvivorDef>("sdMimicry.asset");

        public override GameObject BodyPrefab => Load<GameObject>("MimicryBody.prefab");

        public override GameObject MasterPrefab => null;

        private static SkillDef MimicSkillDef;

        //

        public static Material matMimicrySlash;
        public static GameObject TracerHello;
        public static GameObject SlashEffect;
        //
        public static DamageAPI.ModdedDamageType WearShellType = DamageAPI.ReserveDamageType();
        public static DamageAPI.ModdedDamageType ClawLifestealType = DamageAPI.ReserveDamageType();
        public static DamageAPI.ModdedDamageType MistType = DamageAPI.ReserveDamageType();
        public static LazyIndex MimicryViendIndex = new LazyIndex("MimicryBody");
        public static SkillDef Goodbye;

        public static HashSet<Type> SkillStates = [
            typeof(Hello),
            typeof(Claw),
            typeof(WearShell),
            typeof(GoodbyeSlash),
            typeof(GenericCharacterMain)
        ];

        public static GameObject MistEffect;

        public static int[] ShellCounts = [
            3, 5, 6, 6, 6, 3
        ];

        public static List<SkillDef> HighPriority = [
            Paths.SkillDef.ImpBodyBlink,
            Paths.SkillDef.BisonBodyCharge,
            Paths.SkillDef.ImpBossBodyFireVoidspikes,
            Paths.SkillDef.VoidJailerChargeCapture,
            Paths.SkillDef.FireConstructBeam,
            Paths.SkillDef.RaidCrabMultiBeam,
            Paths.SkillDef.GrandParentChannelSun,
            Paths.SkillDef.HuntressBodyBlink,
            Paths.SkillDef.HuntressBodyMiniBlink,
            Paths.SkillDef.MageBodyWall,
            Paths.SkillDef.BanditBodyCloak,
            Paths.SkillDef.CaptainTazer,
            Paths.SkillDef.EngiBodyPlaceTurret,
            Paths.SkillDef.EngiBodyPlaceWalkerTurret,
            Paths.SkillDef.ThrowPylon,
            Paths.SkillDef.ThrowGrenade,
            Paths.SkillDef.MercBodyEvis,
            Paths.SkillDef.MercBodyEvisProjectile,
            Paths.SkillDef.RailgunnerBodyFireMineBlinding,
            Paths.SkillDef.RailgunnerBodyFireMineConcussive,
            Paths.SkillDef.VoidBlinkDown,
            Paths.SkillDef.VoidBlinkUp,
            Load<SkillDef>("SilentAdvance.asset"),
            Load<SkillDef>("Scream.asset"),
            Load<SkillDef>("SweeperUtility.asset"),
        ];

        public static List<GameObject> DisallowedBodies = [
            Paths.GameObject.BeetleBody,
            Paths.GameObject.VoidInfestorBody,
            Paths.GameObject.GupBody,
            Paths.GameObject.GipBody,
            Paths.GameObject.GeepBody,
            Paths.GameObject.JellyfishBody,
            Paths.GameObject.BisonBody,
            Paths.GameObject.MagmaWormBody,
            Paths.GameObject.ElectricWormBody,
            Paths.GameObject.VerminBody,
            Paths.GameObject.BeetleGuardBody
        ];

        public static SkillDef Fallback => Paths.SkillDef.CommandoSlide;

        public static HashSet<BodyIndex> BlacklistedBodyIndexes;

        public class EGOMimicryConfig : ConfigClass
        {
            public override string Section => "EGO Corrosions :: Mimicry";
            public bool PlayGoodbyeAudio => base.Option<bool>("Goodbye Audio", "Play the Goodbye sound effect when using the Goodbye skill.", true);

            public override void Initialize()
            {
                _ = PlayGoodbyeAudio;
            }
        }

        public static EGOMimicryConfig config = new();

        public override void Modify()
        {
            base.Modify();

            BodyPrefab.GetComponent<CameraTargetParams>().cameraParams = Paths.CharacterCameraParams.ccpStandard;

            var animController = Paths.GameObject.VoidSurvivorDisplay.GetComponentInChildren<Animator>().runtimeAnimatorController;

            BodyPrefab.GetComponent<ModelLocator>()._modelTransform.GetComponent<Animator>().runtimeAnimatorController = Paths.RuntimeAnimatorController.animVoidSurvivor;
            BodyPrefab.GetComponent<ModelLocator>()._modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet = Paths.ItemDisplayRuleSet.idrsVoidSurvivor;
            BodyPrefab.GetComponent<ModelLocator>()._modelTransform.GetComponent<ChildLocator>().FindChild("ScytheScaleBone").AddComponent<GoodbyeArmStretcher>();
            Load<GameObject>("MimicryDisplay.prefab").GetComponentInChildren<Animator>().runtimeAnimatorController = animController;
            BodyPrefab.GetComponent<CharacterBody>()._defaultCrosshairPrefab = Paths.GameObject.VoidSurvivorBody.GetComponent<CharacterBody>().defaultCrosshairPrefab;
            BodyPrefab.AddComponent<MimicryShellController>();

            matMimicrySlash = Load<Material>("matMimicrySlash.mat");
            matMimicrySlash.SetTexture("_RemapTex", Paths.Texture2D.texRampInfusion);
            matMimicrySlash.SetTexture("_Cloud1Tex", Paths.Texture2D.texCloudCaustic3);
            matMimicrySlash.SetTexture("_Cloud2Tex", Paths.Texture2D.texCloudWaterFoam1);
            matMimicrySlash.SetTexture("_MainTex", Paths.Texture2D.texOmniHitspark2Mask);
            matMimicrySlash.SetShaderKeywords(new string[] { "USE_CLOUDS" });

            TracerHello = PrefabAPI.InstantiateClone(Paths.GameObject.TracerCommandoBoost, "TracerHello");
            TracerHello.GetComponent<LineRenderer>().material = Paths.Material.matLunarGolemChargeGlow;
            TracerHello.GetComponent<Tracer>().speed = 370;
            ContentAddition.AddEffect(TracerHello);

            SlashEffect = PrefabAPI.InstantiateClone(Paths.GameObject.VoidSurvivorMeleeSlash3, "MimicrySlash");
            SlashEffect.transform.Find("Rotator").Find("SwingTrail").GetComponent<ParticleSystemRenderer>().material = matMimicrySlash;
            var main = SlashEffect.transform.Find("Rotator").Find("SwingTrail").GetComponent<ParticleSystem>().main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ContentAddition.AddEffect(SlashEffect);

            MistEffect = Load<GameObject>("MistCloud.prefab");
            ContentAddition.AddEffect(MistEffect);

            On.RoR2.Skills.SkillDef.IsReady += DisallowMimicWhenNoShell;
            On.RoR2.GlobalEventManager.OnCharacterDeath += WearShellOnKill;
            On.RoR2.GlobalEventManager.OnHitEnemy += Heal;
            RoR2.BodyCatalog.availability.CallWhenAvailable(FillDisallowedIndexes);

            MimicSkillDef = Load<SkillDef>("Mimic.asset");

            Goodbye = Load<SkillDef>("Goodbye.asset");

            TransformHooks();

            NetworkingAPI.RegisterMessageType<SyncShell>();
        }

        private static void FillDisallowedIndexes()
        {
            BlacklistedBodyIndexes = [];

            foreach (var body in DisallowedBodies)
            {
                BlacklistedBodyIndexes.Add(body.GetComponent<CharacterBody>().bodyIndex);
            }

            BlacklistedBodyIndexes.Add(new LazyIndex("BobombBody"));
            BlacklistedBodyIndexes.Add(new LazyIndex("BodyBrassMonolith"));
            BlacklistedBodyIndexes.Add(new LazyIndex("CoilGolemBody"));
            BlacklistedBodyIndexes.Add(new LazyIndex("FrostWispBody"));
            BlacklistedBodyIndexes.Add(new LazyIndex("BobombBody"));
            BlacklistedBodyIndexes.Add(new LazyIndex("RunshroomBody"));
            BlacklistedBodyIndexes.Add(new LazyIndex("SteamMachineBody"));
        }

        private static void Heal(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (damageInfo.HasModdedDamageType(ClawLifestealType) && damageInfo.attacker && damageInfo.attacker.TryGetComponent<HealthComponent>(out var attackerHealth))
            {
                attackerHealth.Heal(damageInfo.damage * 0.4f, new(), true);
            }
        }

        private static void WearShellOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport report)
        {
            if (report.damageInfo.HasModdedDamageType(WearShellType) && report.attackerBody && report.attackerBody.TryGetComponent<MimicryShellController>(out var mimic))
            {
                mimic.UpdateShell(report.victimBody);
                new SyncShell(report.attacker, report.victimBody).Send(NetworkDestination.Clients);
            }

            orig(self, report);

            if (report.damageInfo.HasModdedDamageType(MistType))
            {
                EffectManager.SpawnEffect(MistEffect, new EffectData
                {
                    origin = report.victimBody.corePosition,
                    scale = report.victimBody.bestFitRadius
                }, true);

                if (report.victimBody.modelLocator?.modelTransform) GameObject.Destroy(report.victimBody.modelLocator.modelTransform.gameObject);
            }
        }

        private bool DisallowMimicWhenNoShell(On.RoR2.Skills.SkillDef.orig_IsReady orig, SkillDef self, GenericSkill skillSlot)
        {
            if (self == MimicSkillDef)
            {
                if (!skillSlot.GetComponent<CharacterBody>().HasBuff(WornShell.Instance.Buff))
                {
                    return false;
                }
            }

            return orig(self, skillSlot);
        }

        public override void SetupLanguage()
        {
            base.SetupLanguage();

            "RL_EGO_MIMICRY_NAME".Add("Void Fiend :: Mimicry");


            "RL_EGO_MIMICRY_PASSIVE_NAME".Add("IMi?taTio??N");
            "RL_EGO_MIMICRY_PASSIVE_DESC".Add("Gain stacks of <style=cIsDamage>Imitation</style> as you acquire unique shells. After reaching enough <style=cIsDamage>Imitation</style>, replace your special with <style=cDeath>G?oOd??ByE?</style>");

            "RL_EGO_MIMICRY_PRIMARY_NAME".Add("H??eLlO?");
            "RL_EGO_MIMICRY_PRIMARY_DESC".Add("Fire a medium-range blast for <style=cIsDamage>325% damage</style>.");

            "RL_EGO_MIMICRY_SECONDARY_NAME".Add("C?lA?w");
            "RL_EGO_MIMICRY_SECONDARY_DESC".Add("Lunge and swipe forward, dealing <style=cIsDamage>500% damage</style> and <style=cIsHealing>healing 40% of damage dealt</style>. Hold up to 2 charges.");

            "RL_EGO_MIMICRY_UTILITY_NAME".Add("M?iMiC??");
            "RL_EGO_MIMICRY_UTILITY_DESC".Add("Activate the effect of your <style=cIsUtility>current shell</style>, if you have one.");

            "RL_EGO_MIMICRY_SPECIAL_NAME".Add("W?eAr S??hElL");
            "RL_EGO_MIMICRY_SPECIAL_DESC".Add("Perform a devastating slash for <style=cIsDamage>1400% damage</style>. <style=cDeath>If this kills, wear the target's shell.</style>");

            "RL_EGO_MIMICRY_GOODBYE_NAME".Add("G?oOd??ByE?");
            "RL_EGO_MIMICRY_GOODBYE_DESC".Add("Leap forward and perform a devastating slash, dealing <style=cIsDamage>2200% damage</style>. <style=cDeath>If this kills, wear the target's shell.</style>");
        }

        public class SyncShell : INetMessage
        {
            public CharacterBody target;
            public GameObject applyTo;
            private NetworkInstanceId _target;
            private NetworkInstanceId _applyTo;
            public void Deserialize(NetworkReader reader)
            {
                _applyTo = reader.ReadNetworkId();
                _target = reader.ReadNetworkId();

                applyTo = Util.FindNetworkObject(_applyTo);
                target = Util.FindNetworkObject(_target).GetComponent<CharacterBody>();
            }

            public void OnReceived()
            {
                applyTo.GetComponent<MimicryShellController>().UpdateShell(target);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(applyTo.GetComponent<NetworkIdentity>().netId);
                writer.Write(target.GetComponent<NetworkIdentity>().netId);
            }

            public SyncShell()
            {

            }

            public SyncShell(GameObject applyTo, CharacterBody target)
            {
                this.applyTo = applyTo;
                this.target = target;
            }
        }

        public class GoodbyeArmStretcher : MonoBehaviour
        {
            public bool isStretching = false;
            public int windState = 0;
            public Vector3 targetScale = new(4f, 4f, 4f);
            public Vector3 originalScale;
            public float stopwatch = 0f;
            public float totalDuration = 0.2f;
            public float stretchDurationPerc = 0.5f;
            public float shrinkDurationPerc = 0.4f;
            public Vector3 lockedScale = Vector3.zero;

            public float shrinkDur => totalDuration * shrinkDurationPerc;
            public float stretchDur => totalDuration * stretchDurationPerc;

            public void BeginGoodbye(int windstate)
            {
                stopwatch = 0f;
                isStretching = true;
                windState = windstate;
                lockedScale = Vector3.zero;
            }

            public void LateUpdate()
            {
                if (isStretching)
                {
                    stopwatch += Time.fixedDeltaTime;

                    if (windState == 1)
                    {
                        base.transform.localScale = Vector3.Lerp(targetScale, originalScale, (stopwatch / totalDuration));
                    }
                    else
                    {
                        base.transform.localScale = Vector3.Lerp(originalScale, targetScale, (stopwatch / totalDuration));
                    }

                    if (stopwatch >= totalDuration)
                    {
                        base.transform.localScale = windState == 1 ? originalScale : targetScale;
                        isStretching = false;
                        lockedScale = windState == 1 ? originalScale : targetScale;
                    }
                }

                if (lockedScale != Vector3.zero)
                {
                    base.transform.localScale = lockedScale;
                }
            }

            public void Start()
            {
                originalScale = transform.localScale;
            }
        }

        public class MimicryShellController : MonoBehaviour
        {
            private CharacterBody us;
            private SkillLocator loc;

            public List<BodyIndex> shellsWorn = [];

            private bool assignedMimicry = false;
            private float shellCooldown = 0f;
            private float lastEnemyHp = 0f;

            public void Start()
            {
                loc = GetComponent<SkillLocator>();
                us = GetComponent<CharacterBody>();
            }

            public void FixedUpdate()
            {
                if (shellCooldown >= 0f)
                {
                    shellCooldown -= Time.fixedDeltaTime;
                }

                if (shellCooldown <= 0f)
                {
                    lastEnemyHp = 0f;
                }
            }

            public void UpdateShell(CharacterBody body)
            {
                // Debug.Log("Wearing a shell!");

                var useFallbackSkill = false;

                foreach (var index in BlacklistedBodyIndexes)
                {
                    if (body.bodyIndex == index) useFallbackSkill = true;
                }

                var onlyAllowHigherShell = shellCooldown >= 0f;

                if (onlyAllowHigherShell)
                {
                    if (body.maxHealth >= lastEnemyHp)
                    {
                        lastEnemyHp = body.maxHealth;
                    }
                    else
                    {
                        return;
                    }
                }

                if (shellCooldown <= 0f)
                {
                    shellCooldown = 2f;
                }

                if (!shellsWorn.Contains(body.bodyIndex))
                {
                    shellsWorn.Add(body.bodyIndex);

                    if (NetworkServer.active)
                        us.AddBuff(Buffs.Imitation.Instance.Buff);

                    var index = 0;

                    if (SceneCatalog.mostRecentSceneDef == Paths.SceneDef.moon || SceneCatalog.mostRecentSceneDef == Paths.SceneDef.moon2)
                    {
                        index = 5;
                    }
                    else
                    {
                        index = (Run.instance.stageClearCount + 1) % 5;
                        if (index == 0) index = 5;
                        index--;
                    }

                    // Debug.Log($"target shell count is {ShellCounts[index]} at index {index}");

                    if (shellsWorn.Count >= ShellCounts[index] && !assignedMimicry)
                    {
                        if (us.hasAuthority)
                            loc.special.SetSkillOverride(base.gameObject, Goodbye, GenericSkill.SkillOverridePriority.Upgrade);
                        assignedMimicry = true;
                    }
                }

                var copySkill = EGOMimicry.Fallback;

                if (!useFallbackSkill)
                {
                    var skills = body.GetComponents<GenericSkill>().OrderBy(x => x.skillDef.baseRechargeInterval);
                    var skill = skills.FirstOrDefault(x => HighPriority.Contains(x.skillDef));

                    if (!skill)
                    {
                        skill = skills.FirstOrDefault(x => x.skillDef.activationStateMachineName != "Body");

                        if (!skill)
                            skill = skills.First();
                    }

                    copySkill = skill.skillDef;
                }

                var def = loc.utility ? loc.utility.skillDef : null;

                if (def && def == MimicSkillDef)
                {
                    def.activationStateMachineName = "Mimic";
                    def.activationState = copySkill.activationState;
                    def.beginSkillCooldownOnSkillEnd = true;
                    def.baseMaxStock = copySkill.baseMaxStock;
                    def.stockToConsume = copySkill.stockToConsume;
                    def.rechargeStock = copySkill.rechargeStock;
                    def.interruptPriority = InterruptPriority.Any;
                    def.fullRestockOnAssign = true;
                    def.isCombatSkill = copySkill.isCombatSkill;
                    def.resetCooldownTimerOnUse = copySkill.resetCooldownTimerOnUse;
                    def.mustKeyPress = copySkill.mustKeyPress;
                    def.cancelSprintingOnActivation = copySkill.cancelSprintingOnActivation;

                    var targetCD = copySkill.baseRechargeInterval * 0.65f;

                    // Debug.Log("target's modified cd: " + targetCD);

                    var newCD = Mathf.Min(targetCD, 5f);
                    // Debug.Log("clamped cd: " + newCD);

                    def.baseRechargeInterval = newCD;
                    loc.utility.RecalculateFinalRechargeInterval();
                }

                if (NetworkServer.active)
                    us.SetBuffCount(Buffs.WornShell.Instance.Buff.buffIndex, 1);

                Buffs.WornShell.Instance.Buff.buffColor = body.bodyColor;
            }
        }

        public void TransformHooks()
        {
            On.ChildLocator.FindChild_string += ChildLocator_FindChild_string;
            On.ChildLocator.FindChildIndex_string += ChildLocator_FindChildIndex_string;
            On.EntityStates.EntityState.PlayAnimation_string_string += EntityState_PlayAnimation_string_string;
            On.EntityStates.EntityState.PlayAnimation_string_string_string_float_float += EntityState_PlayAnimation_string_string_string_float_float;
            On.EntityStates.BaseState.OnEnter += BaseState_OnEnter;
            On.RoR2.EntityStateMachine.ManagedFixedUpdate += EntityStateMachine_ManagedFixedUpdate;
        }

        // this is fucking disgusting
        private static void EntityStateMachine_ManagedFixedUpdate(On.RoR2.EntityStateMachine.orig_ManagedFixedUpdate orig, EntityStateMachine self, float delta)
        {
            if (!self.state?.characterBody || self.state.characterBody.bodyIndex != MimicryViendIndex)
            {
                orig(self, delta);
                return;
            }

            var wasS1Down = false;
            var didWeEvenRun = false;

            if (IsAStolenSkill(self.state))
            {
                wasS1Down = self.state.inputBank.skill1.down;
                self.state.inputBank.skill1.down = self.state.inputBank.skill3.down;

                didWeEvenRun = true;
            }

            orig(self, delta);

            if (didWeEvenRun)
            {
                self.state.inputBank.skill1.down = wasS1Down;
            }
        }

        private static void BaseState_OnEnter(On.EntityStates.BaseState.orig_OnEnter orig, BaseState self)
        {
            orig(self);

            if (self.characterBody && self.characterBody.bodyIndex == MimicryViendIndex)
            {
                if (IsAStolenSkill(self))
                {
                    self.damageStat *= 1.75f;
                    self.attackSpeedStat *= 1.75f;
                }
            }
        }

        private static void EntityState_PlayAnimation_string_string_string_float_float(On.EntityStates.EntityState.orig_PlayAnimation_string_string_string_float_float orig, EntityState self, string str, string str2, string str3, float f, float f2)
        {
            if (self.characterBody && self.characterBody.bodyIndex == MimicryViendIndex)
            {
                var anim = self.GetModelAnimator();
                var state = anim.HasState(anim.GetLayerIndex(str), Animator.StringToHash(str2));

                // Debug.Log("has state: " + state);

                if (!state)
                {
                    str = "LeftArm, Override";
                    str2 = "FireHandBeam";
                    str3 = "HandBeam.playbackRate";
                }
            }

            orig(self, str, str2, str3, f, f2);
        }

        private static void EntityState_PlayAnimation_string_string(On.EntityStates.EntityState.orig_PlayAnimation_string_string orig, EntityState self, string str, string str2)
        {
            if (self.characterBody && self.characterBody.bodyIndex == MimicryViendIndex)
            {
                var anim = self.GetModelAnimator();
                var state = anim.HasState(anim.GetLayerIndex(str), Animator.StringToHash(str2));

                // Debug.Log("has state: " + state);

                if (!state)
                {
                    str = "LeftArm, Override";
                    str2 = "FireHandBeam";
                }
            }

            orig(self, str, str2);
        }

        private static int ChildLocator_FindChildIndex_string(On.ChildLocator.orig_FindChildIndex_string orig, ChildLocator self, string str)
        {
            var c = orig(self, str);
            return (c != -1) ? c : orig(self, "MuzzleHandBeam");
        }

        private static Transform ChildLocator_FindChild_string(On.ChildLocator.orig_FindChild_string orig, ChildLocator self, string str)
        {
            var transform = orig(self, str);
            return (transform || str == "HealthBarOrigin") ? transform : orig(self, "MuzzleHandBeam");
        }

        public static bool IsAStolenSkill(EntityState state)
        {
            foreach (var stateType in SkillStates)
            {
                if (state.GetType() == stateType)
                {
                    return false;
                }
            }

            return true;
        }
    }
}