using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48418)]
    public class npc_defias_envokerAI : ScriptedAI
    {
        public npc_defias_envokerAI(Creature creature) : base(creature)
        {
        }

        public uint HolyfireTimer;
        public uint ShieldTimer;

        public override void Reset()
        {
            HolyfireTimer = 4000;
            ShieldTimer = 8000;
        }

        public override void UpdateAI(uint diff)
        {
            if (HolyfireTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                {
                    DoCast(target, boss_vanessa_vancleef.Spells.SPELL_HOLY_FIRE);
                }
                HolyfireTimer = RandomHelper.URand(8000, 11000);
            }
            else
            {
                HolyfireTimer -= diff;
            }

            if (ShieldTimer <= diff)
            {
                if (IsHeroic())
                {
                    DoCast(me, boss_vanessa_vancleef.Spells.SPELL_SHIELD);
                    ShieldTimer = RandomHelper.URand(18000, 20000);
                }
            }
            else
            {
                ShieldTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }
    }
}
