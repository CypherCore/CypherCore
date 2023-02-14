// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(new uint[] { 48447, 48450 })]
    public class npc_deadmines_bird : ScriptedAI
    {
        public npc_deadmines_bird(Creature creature) : base(creature)
        {
            Instance = creature.GetInstanceScript();
        }

        public InstanceScript Instance;
        public uint IiTimerEyePeck;
        public uint UiTimerEyeGouge;

        public override void Reset()
        {
            IiTimerEyePeck = RandomHelper.URand(4000, 4900);
            UiTimerEyeGouge = RandomHelper.URand(7000, 9000);
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!me)
            {
                return;
            }

            if (!UpdateVictim())
            {
                return;
            }

            if (UiTimerEyeGouge <= uiDiff)
            {
                Unit victim = me.GetVictim();

                if (victim != null)
                {
                    me.CastSpell(victim, IsHeroic() ? DMSpells.SPELL_EYE_GOUGE_H : DMSpells.SPELL_EYE_GOUGE);
                }
                UiTimerEyeGouge = RandomHelper.URand(9000, 12000);
                return;
            }
            else
            {
                UiTimerEyeGouge -= uiDiff;
            }

            if (IiTimerEyePeck <= uiDiff)
            {
                Unit victim = me.GetVictim();

                if (victim != null)
                {
                    me.CastSpell(victim, IsHeroic() ? DMSpells.SPELL_EYE_PECK_H : DMSpells.SPELL_EYE_PECK);
                }
                IiTimerEyePeck = RandomHelper.URand(16000, 19000);
                return;
            }
            else
            {
                IiTimerEyePeck -= uiDiff;
            }

            DoMeleeAttackIfReady();
        }
    }
}
