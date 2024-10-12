using System;

namespace RaindropLobotomy.Buffs
{
    public class Resentment : BuffBase<Resentment>
    {
        public override BuffDef Buff { get; set; } = Load<BuffDef>("bdResentment.asset");

        public override void PostCreation()
        {

        }
    }
}