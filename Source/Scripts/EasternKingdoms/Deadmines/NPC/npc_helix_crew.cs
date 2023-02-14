// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_helix_gearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(new uint[] { 49136, 49137, 49138, 49139 })]
    public class npc_helix_crew : PassiveAI
    {
        public npc_helix_crew(Creature pCreature) : base(pCreature)
        {
        }

        public uint ThrowBombTimer;

        public override void Reset()
        {
            ThrowBombTimer = 3000;
            DoCast(me, 18373);

            Player victim = me.FindNearestPlayer(80.0f);
            if (victim != null)
            {
                me.Attack(victim, false);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (ThrowBombTimer <= diff)
            {
                Unit player = SelectTarget(SelectTargetMethod.Random, 0, 200, true);
                if (player != null)
                {
                    DoCast(player, eSpels.SPELL_THROW_BOMB);
                    ThrowBombTimer = 5000;
                }
            }
            else
            {
                ThrowBombTimer -= diff;
            }
        }
    }
}
