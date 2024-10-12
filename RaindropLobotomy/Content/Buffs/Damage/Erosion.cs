using System;

namespace RaindropLobotomy.Buffs
{
    public class Erosion : BuffBase<Erosion>
    {
        public override BuffDef Buff { get; set; } = Load<BuffDef>("bdErosion.asset");
        public BurnEffectController.EffectParams ErosionEffect;
        public static DamageAPI.ModdedDamageType InflictErosion = DamageAPI.ReserveDamageType();
        public static DamageAPI.ModdedDamageType InflictTwoErosion = DamageAPI.ReserveDamageType();
        public static DamageAPI.ModdedDamageType InflictTenErosion = DamageAPI.ReserveDamageType();

        public override void PostCreation()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += OnTakeDamage;
            On.RoR2.CharacterBody.AddBuff_BuffIndex += OnAddErosion;

            ErosionEffect = new();
            ErosionEffect.fireEffectPrefab = Paths.GameObject.BlightEffect;
            ErosionEffect.overlayMaterial = Load<Material>("matErosion.mat");
        }

        private void OnAddErosion(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            orig(self, buffType);

            if (buffType == Buff.buffIndex)
            {
                CharacterModel model = self.modelLocator?.modelTransform?.GetComponent<CharacterModel>() ?? null;

                if (model)
                {
                    ErosionController controller = model.GetComponent<ErosionController>();

                    if (!controller)
                    {
                        controller = model.AddComponent<ErosionController>();
                    }

                    controller.effectType = BurnEffectController.blightEffect;
                    controller.body = self;
                    controller.target = model.gameObject;
                }
            }
        }

        private void OnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            if (damageInfo.procCoefficient > 0 && damageInfo.attacker && NetworkServer.active)
            {
                int count = self.body.GetBuffCount(Buff.buffIndex);

                if (damageInfo.HasModdedDamageType(InflictErosion) && damageInfo.procCoefficient < 0.2f)
                {
                    //no.
                    //goto next;
                }
                else if (count > 0)
                {
                    float damage = (0.2f * count) * damageInfo.attacker.GetComponent<CharacterBody>().damage;
                    self.body.SetBuffCount(Buff.buffIndex, count - 1);

                    DamageInfo info = new();
                    info.attacker = damageInfo.attacker;
                    info.position = damageInfo.position;
                    info.crit = false;
                    info.damageColorIndex = DamageColorIndex.Poison;
                    info.procCoefficient = 0;
                    info.damage = damage;

                    EffectManager.SimpleEffect(Paths.GameObject.CrocoDiseaseImpactEffect, info.position, Quaternion.identity, true);

                    self.TakeDamage(info);
                }
            }

            if (damageInfo.HasModdedDamageType(InflictErosion))
            {
                self.body.AddTimedBuff(Buff.buffIndex, 10f);
            }

            if (damageInfo.HasModdedDamageType(InflictTwoErosion))
            {
                self.body.AddTimedBuff(Buff.buffIndex, 10f);
                self.body.AddTimedBuff(Buff.buffIndex, 10f);
            }

            if (damageInfo.HasModdedDamageType(InflictTenErosion))
            {
                for (int i = 0; i < 10; i++)
                {
                    self.body.AddTimedBuff(Buff.buffIndex, 10f);
                }
            }
        }

        private class ErosionController : BurnEffectController
        {
            internal CharacterBody body;

            public void FixedUpdate()
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive || !body.HasBuff(Erosion.Instance.Buff))
                {
                    Destroy(this);
                }
            }
        }
    }
}