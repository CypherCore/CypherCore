// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;

namespace Scripts.EasternKingdoms.AlteracValley
{
    struct SpellIds
    {
        public const uint Charge = 22911;
        public const uint Cleave = 40504;
        public const uint DemoralizingShout = 23511;
        public const uint Enrage = 8599;
        public const uint Whirlwind = 13736;

        public const uint NorthMarshal = 45828;
        public const uint SouthMarshal = 45829;
        public const uint StonehearthMarshal = 45830;
        public const uint IcewingMarshal = 45831;
        public const uint IcebloodWarmaster = 45822;
        public const uint TowerPointWarmaster = 45823;
        public const uint WestFrostwolfWarmaster = 45824;
        public const uint EastFrostwolfWarmaster = 45826;
    }

    struct CreatureIds
    {
        public const uint NorthMarshal = 14762;
        public const uint SouthMarshal = 14763;
        public const uint IcewingMarshal = 14764;
        public const uint StonehearthMarshal = 14765;
        public const uint EastFrostwolfWarmaster = 14772;
        public const uint IcebloodWarmaster = 14773;
        public const uint TowerPointWarmaster = 14776;
        public const uint WestFrostwolfWarmaster = 14777;
    }

    [Script]
    class npc_av_marshal_or_warmaster : ScriptedAI
    {
        (uint npcEntry, uint spellId)[] _auraPairs =
        {
            new (CreatureIds.NorthMarshal, SpellIds.NorthMarshal),
            new (CreatureIds.SouthMarshal, SpellIds.SouthMarshal),
            new (CreatureIds.StonehearthMarshal, SpellIds.StonehearthMarshal),
            new (CreatureIds.IcewingMarshal, SpellIds.IcewingMarshal),
            new (CreatureIds.EastFrostwolfWarmaster, SpellIds.EastFrostwolfWarmaster),
            new (CreatureIds.WestFrostwolfWarmaster, SpellIds.WestFrostwolfWarmaster),
            new (CreatureIds.TowerPointWarmaster, SpellIds.TowerPointWarmaster),
            new (CreatureIds.IcebloodWarmaster, SpellIds.IcebloodWarmaster)
        };

        bool _hasAura;

        public npc_av_marshal_or_warmaster(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _hasAura = false;
        }

        public override void Reset()
        {
            Initialize();

            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Charge);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(11), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(16));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCast(me, SpellIds.DemoralizingShout);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(me, SpellIds.Whirlwind);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(me, SpellIds.Enrage);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Position _homePosition = me.GetHomePosition();
                if (me.GetDistance2d(_homePosition.GetPositionX(), _homePosition.GetPositionY()) > 50.0f)
                {
                    EnterEvadeMode();
                    return;
                }
                task.Repeat(TimeSpan.FromSeconds(5));
            });
        }

        public override void JustAppeared()
        {
            Reset();
        }

        public override void UpdateAI(uint diff)
        {
            // I have a feeling this isn't blizzlike, but owell, I'm only passing by and cleaning up.
            if (!_hasAura)
            {
                for (byte i = 0; i < _auraPairs.Length; ++i)
                    if (_auraPairs[i].npcEntry == me.GetEntry())
                        DoCast(me, _auraPairs[i].spellId);

                _hasAura = true;
            }

            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;    

            DoMeleeAttackIfReady();
        }
    }
}

