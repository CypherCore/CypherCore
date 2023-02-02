using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(new uint[]{ 48502, 49850 })]
    public class npc_defias_enforcer : ScriptedAI
    {
        public npc_defias_enforcer(Creature creature) : base(creature)
        {
        }

        public uint BloodBathTimer;
        public uint RecklessnessTimer;

        public override void Reset()
        {
            BloodBathTimer = 8000;
            RecklessnessTimer = 13000;
        }

        public override void JustEnteredCombat(Unit who)
        {
            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
            if (target != null)
            {
                DoCast(target, boss_vanessa_vancleef.Spells.SPELL_CHARGE);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (BloodBathTimer <= diff)
            {
                DoCastVictim(boss_vanessa_vancleef.Spells.SPELL_BLOODBATH);
                BloodBathTimer = RandomHelper.URand(8000, 11000);
            }
            else
            {
                BloodBathTimer -= diff;
            }

            if (RecklessnessTimer <= diff)
            {
                DoCast(me, boss_vanessa_vancleef.Spells.SPELL_BLOODBATH);
                RecklessnessTimer = 20000;
            }
            else
            {
                RecklessnessTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }
    }
}
