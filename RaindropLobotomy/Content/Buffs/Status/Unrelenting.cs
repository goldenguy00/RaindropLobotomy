using System;

namespace RaindropLobotomy.Buffs
{
    public class Unrelenting : BuffBase<Unrelenting>
    {
        public override BuffDef Buff { get; set; } = Load<BuffDef>("bdUnrelenting.asset");

        public override void PostCreation()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += TheOscarKeypageIsBalanced;
        }

        private void TheOscarKeypageIsBalanced(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self.body.HasBuff(Buff))
            {
                damageInfo.damageType |= DamageType.NonLethal;
            }

            orig(self, damageInfo);
        }
    }
}