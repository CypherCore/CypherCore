using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49550)]
    public class npc_rope_away : ScriptedAI
    {
        public npc_rope_away(Creature creature) : base(creature)
        {
            me.SetNpcFlag(NPCFlags.SpellClick);
        }

        public override void Reset()
        {
            Phase = 0;
            MoveTimer = 500;
            RunAway = false;
            me.SetSpeed(UnitMoveType.Flight, 3.0f);
        }

        public override void MovementInform(MovementGeneratorType UnnamedParameter, uint id)
        {
            if (id == 1)
            {
                Unit passenger = me.GetVehicleKit().GetPassenger(0);
                if (passenger != null)
                {
                    passenger.ExitVehicle();
                }
            }
        }

        public override void PassengerBoarded(Unit who, sbyte UnnamedParameter, bool apply)
        {
            if (who.GetTypeId() == TypeId.Player)
            {
                if (apply)
                {
                    RunAway = true;
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (RunAway)
            {
                if (MoveTimer <= diff)
                {
                    switch (Phase)
                    {
                        case 0:
                            me.GetMotionMaster().MovePoint(0, -77.97f, -877.09f, 49.44f);
                            MoveTimer = 2500;
                            Phase++;
                            break;
                        case 1:
                            me.GetMotionMaster().MovePoint(1, -64.02f, -839.84f, 41.22f);
                            MoveTimer = 3000;
                            Phase++;
                            break;
                        case 2:
                            break;
                    }
                }
                else
                {
                    MoveTimer -= diff;
                }
            }
        }

        private bool RunAway;
        private byte Phase;
        private uint MoveTimer;
    }
}
