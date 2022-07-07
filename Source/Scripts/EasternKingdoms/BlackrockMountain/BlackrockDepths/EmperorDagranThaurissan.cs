/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Draganthaurissan
{
    struct SpellIds
    {
        public const uint Handofthaurissan = 17492;
        public const uint Avatarofflame = 15636;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;

        public const uint EmoteShaken = 0;
    }

    [Script]
    class boss_draganthaurissan : ScriptedAI
    {
        InstanceScript _instance;

        public boss_draganthaurissan(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);
            me.CallForHelp(166.0f);
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target)
                    DoCast(target, SpellIds.Handofthaurissan);
                task.Repeat(TimeSpan.FromSeconds(5));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(25), task =>
            {
                DoCastVictim(SpellIds.Avatarofflame);
                task.Repeat(TimeSpan.FromSeconds(18));
            });
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsPlayer())
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            Creature moira = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.DataMoira));
            if (moira)
            {
                moira.GetAI().EnterEvadeMode();
                moira.SetFaction((uint)FactionTemplates.Friendly);
                moira.GetAI().Talk(TextIds.EmoteShaken);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}

