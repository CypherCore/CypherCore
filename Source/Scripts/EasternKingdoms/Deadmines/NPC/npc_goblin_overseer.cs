// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48279)]
    public class npc_goblin_overseer : ScriptedAI
    {
        public npc_goblin_overseer(Creature creature) : base(creature)
        {
        }

        public uint MotivateTimer;

        private bool _threat;

        public override void Reset()
        {
            MotivateTimer = 4000;
            _threat = true;
        }

        public override void UpdateAI(uint diff)
        {
            if (MotivateTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                {
                    DoCast(target, boss_vanessa_vancleef.Spells.MOTIVATE);
                }
                MotivateTimer = RandomHelper.URand(8000, 11000);
            }
            else
            {
                MotivateTimer -= diff;
            }

            if (HealthBelowPct(50) && !_threat)
            {
                DoCast(me, boss_vanessa_vancleef.Spells.THREATENING);
                _threat = true;
            }

            DoMeleeAttackIfReady();
        }
    }
}
