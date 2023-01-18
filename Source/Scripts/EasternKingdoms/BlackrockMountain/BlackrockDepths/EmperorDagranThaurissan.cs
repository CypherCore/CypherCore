// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

