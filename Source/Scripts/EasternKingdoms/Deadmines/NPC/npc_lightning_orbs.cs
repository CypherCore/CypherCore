using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49520)]
    public class npc_lightning_orbs : NullCreatureAI
    {
        public npc_lightning_orbs(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            TurnTimer = 100;
            Vehicle vehicle = me.GetVehicleKit();
            if (vehicle != null)
            {
                for (sbyte i = 0; i < 8; i++)
                {
                    if (vehicle.HasEmptySeat(i))
                    {
                        Creature pas = me.SummonCreature(49521, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ());
                        if (pas != null)
                        {
                            pas.EnterVehicle(me, i);
                        }
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (TurnTimer <= diff)
            {
                me.SetFacingTo(me.GetOrientation() + 0.05233f);
                TurnTimer = 100;
            }
            else
            {
                TurnTimer -= diff;
            }
        }

        private uint TurnTimer;
    }
}
