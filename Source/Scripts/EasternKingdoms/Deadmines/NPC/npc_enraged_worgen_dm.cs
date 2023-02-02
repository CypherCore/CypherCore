using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49532)]
    public class npc_enraged_worgen_dm : ScriptedAI
    {
        public npc_enraged_worgen_dm(Creature creature) : base(creature)
        {
        }

        public override void JustEnteredCombat(Unit who)
        {
            DoZoneInCombat();
        }

        public override void JustDied(Unit killer)
        {
            Creature Vanessa = me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);
            if (Vanessa != null)
            {
                npc_vanessa_nightmare pAI = (npc_vanessa_nightmare)Vanessa.GetAI();
                if (pAI != null)
                {
                    pAI.WorgenKilled();
                }
            }

        }
        public override void UpdateAI(uint diff)
        {
            DoMeleeAttackIfReady();
        }
    }
}
