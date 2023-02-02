using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49552)]
    public class npc_rope_ship : ScriptedAI
    {
        public npc_rope_ship(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            if (me.IsSummon())
            {
                var summoner = me.ToTempSummon().GetSummoner();
                if (summoner != null)
                {
                    if (summoner)
                    {
                        me.CastSpell(summoner, 43785, true);
                    }
                }
            }
        }
    }
}
