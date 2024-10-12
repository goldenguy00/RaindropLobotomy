using System;

namespace RaindropLobotomy.Buffs
{
    public class Poise : BuffBase<Poise>
    {
        public override BuffDef Buff { get; set; } = Load<BuffDef>("bdPoise");
        public DamageAPI.ModdedDamageType GivePoise = DamageAPI.ReserveDamageType();

        public override void PostCreation()
        {
            RecalculateStatsAPI.GetStatCoefficients += AddPoiseBuffs;
            On.RoR2.GlobalEventManager.OnHitEnemy += ReducePoise;
        }

        private void ReducePoise(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent<CharacterBody>(out var body))
            {
                int count = body.GetBuffCount(Buff);
                if (damageInfo.crit && count > 0)
                {
                    body.SetBuffCount(Buff.buffIndex, Mathf.Clamp(count - 1, 0, 20));
                }

                if (damageInfo.HasModdedDamageType(GivePoise))
                {
                    count = body.GetBuffCount(Buff);
                    body.SetBuffCount(Buff.buffIndex, Mathf.Clamp(count + 1, 0, 20));
                }
            }
        }

        private void AddPoiseBuffs(CharacterBody sender, StatHookEventArgs args)
        {
            int PoiseCount = sender.GetBuffCount(Buff);

            if (PoiseCount > 0)
            {
                args.critAdd += PoiseCount * 5;
            }
        }
    }
}