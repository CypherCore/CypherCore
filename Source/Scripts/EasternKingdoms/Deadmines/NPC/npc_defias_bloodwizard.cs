// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    public class npc_defias_bloodwizard : ScriptedAI
    {
        public npc_defias_bloodwizard(Creature creature) : base(creature)
        {
        }

        public uint BloodboltTimer;
        public uint BloodWashTimer;
        public uint RagezoneTimer;

        public override void Reset()
        {
            BloodboltTimer = 3500;
            BloodWashTimer = 10000;
            RagezoneTimer = RandomHelper.URand(7000, 9000);
        }

        public override void UpdateAI(uint diff)
        {
            if (BloodboltTimer <= diff)
            {
                DoCastVictim(boss_vanessa_vancleef.Spells.SPELL_BLOODBOLT);
                BloodboltTimer = RandomHelper.URand(6400, 8000);
            }
            else
            {
                BloodboltTimer -= diff;
            }

            if (BloodWashTimer <= diff)
            {
                Creature Enforcer = me.FindNearestCreature(DMCreatures.NPC_DEFIAS_SHADOWGUARD, 100.0f, true);
                if (Enforcer != null)
                {
                    DoCast(Enforcer, boss_vanessa_vancleef.Spells.SPELL_BLOODWASH);
                }
                BloodWashTimer = RandomHelper.URand(15000, 21000);
            }
            else
            {
                BloodWashTimer -= diff;
            }

            if (RagezoneTimer <= diff)
            {
                DoCastVictim(boss_vanessa_vancleef.Spells.SPELL_RAGEZONE);
                RagezoneTimer = RandomHelper.URand(11000, 15000);
            }
            else
            {
                RagezoneTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }
    }
}
