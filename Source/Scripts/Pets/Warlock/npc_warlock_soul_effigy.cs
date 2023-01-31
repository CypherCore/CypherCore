using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // 103679 - Soul Effigy
        public class npc_warlock_soul_effigy : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public npc_warlock_soul_effigy() : base("npc_warlock_soul_effigy")
            {
            }

            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_soul_effigyAI(creature);
            }

            public class npc_warlock_soul_effigyAI : CreatureAI
            {
                public npc_warlock_soul_effigyAI(Creature creature) : base(creature)
                {
                }

                public override void Reset()
                {
                    me.SetControlled(true, UnitState.Root);
                    me.CastSpell(me, SpellIds.SOUL_EFFIGY_AURA, true);
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                }
            }
        }
    }
}