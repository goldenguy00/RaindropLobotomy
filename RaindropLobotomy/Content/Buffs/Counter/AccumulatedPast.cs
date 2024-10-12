using System;

namespace RaindropLobotomy.Buffs
{
    public class AccumulatedPast : BuffBase<AccumulatedPast>
    {
        public override BuffDef Buff { get; set; } = Load<BuffDef>("bdAccPast.asset");

        public override void PostCreation()
        {

        }
    }
}