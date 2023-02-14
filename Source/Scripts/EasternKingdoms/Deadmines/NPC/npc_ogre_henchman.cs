// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48230)]
    public class npc_ogre_henchman : ScriptedAI
    {
        public npc_ogre_henchman(Creature creature) : base(creature)
        {
        }

        public uint UppercutTimer;

        public override void Reset()
        {
            UppercutTimer = 4000;
        }

        public override void UpdateAI(uint diff)
        {
            if (UppercutTimer <= diff)
            {
                DoCastVictim(boss_vanessa_vancleef.Spells.SPELL_UPPERCUT);
                UppercutTimer = RandomHelper.URand(8000, 11000);
            }
            else
            {
                UppercutTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }
    }
}
