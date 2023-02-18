// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;


using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49670)]
    public class npc_glubtok_dm : BossAI
    {
        public npc_glubtok_dm(Creature creature) : base(creature, DMData.DATA_NIGHTMARE_MECHANICAL)
        {
        }

        public uint FlagResetTimer;

        public override void Reset()
        {
            _Reset();
            FlagResetTimer = 10000;
            _events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(11000, 15000)));
        }

        public override void JustEnteredCombat(Unit who)
        {
            base.JustEnteredCombat(who);
            _events.RescheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(6000, 8000)));

            _events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(6000));
        }

        public override void JustDied(Unit killer)
        {
            List<Unit> players = new List<Unit>();

            AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
            PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
            Cell.VisitGrid(me, searcher, 150f);

            foreach (var item in players)
            {
                item.AddAura(boss_vanessa_vancleef.Spells.EFFECT_1, item);
            }

            me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_14, null, true);

            Creature Vanessa = me.FindNearestCreature(DMCreatures.NPC_VANESSA_NIGHTMARE, 500, true);
            if (Vanessa != null)
            {
                npc_vanessa_nightmare pAI = (npc_vanessa_nightmare)Vanessa.GetAI();
                if (pAI != null)
                {
                    pAI.NightmarePass();
                }
            }

            base.JustDied(killer);

        }
        public override void UpdateAI(uint diff)
        {
            if (FlagResetTimer <= diff)
            {
                me.SetVisible(true);
                me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
            }
            else
            {
                FlagResetTimer -= diff;
            }

            _events.Update(diff);

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case boss_vanessa_vancleef.BossEvents.EVENT_ICYCLE_AOE:
                        Player pPlayer = me.FindNearestPlayer(200.0f, true);
                        if (pPlayer != null)
                        {
                            DoCast(pPlayer, boss_vanessa_vancleef.Spells.ICYCLE);
                        }
                        _events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_ICYCLE_AOE, TimeSpan.FromMilliseconds(RandomHelper.URand(6000, 8000)));
                        break;
                    case boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE:
                        DoCastVictim(boss_vanessa_vancleef.Spells.SPIRIT_STRIKE);
                        _events.ScheduleEvent(boss_vanessa_vancleef.BossEvents.EVENT_SPIRIT_STRIKE, TimeSpan.FromMilliseconds(RandomHelper.URand(5000, 7000)));
                        break;
                }
            }
            DoMeleeAttackIfReady();
        }
    }
}
