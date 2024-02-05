// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.AlteracValley.Balinda
{
    struct SpellIds
    {
        public const uint ArcaneExplosion = 46608;
        public const uint ConeOfCold = 38384;
        public const uint Fireball = 46988;
        public const uint Frostbolt = 46987;
        public const uint SummonWaterElemental = 45067;
        public const uint Iceblock = 46604;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayEvade = 1;
        public const uint SaySalvation = 2;
    }

    struct ActionIds
    {
        public const int BuffYell = -30001; // shared from Battleground
    }

    [Script]
    class boss_balinda : ScriptedAI
    {
        SummonList summons;
        ObjectGuid WaterElementalGUID;
        bool HasCastIceblock;

        public boss_balinda(Creature creature) : base(creature)
        {
            summons = new(me);
            Initialize();
        }

        void Initialize()
        {
            WaterElementalGUID.Clear();
            HasCastIceblock = false;
        }

        public override void Reset()
        {
            Initialize();
            _scheduler.CancelAll();
            summons.DespawnAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.ArcaneExplosion);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.ConeOfCold);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                DoCastVictim(SpellIds.Fireball);
                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(9));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.Frostbolt);
                task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                if (summons.Empty())
                    DoCast(SpellIds.SummonWaterElemental);
                task.Repeat(TimeSpan.FromSeconds(50));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                if (me.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
                {
                    EnterEvadeMode();
                    Talk(TextIds.SayEvade);
                }
                Creature elemental = ObjectAccessor.GetCreature(me, WaterElementalGUID);
                if (elemental != null)
                    if (elemental.GetDistance2d(me.GetHomePosition().GetPositionX(), me.GetHomePosition().GetPositionY()) > 50)
                        elemental.GetAI().EnterEvadeMode();
                task.Repeat();
            });
        }

        public override void JustSummoned(Creature summoned)
        {
            summoned.GetAI().AttackStart(SelectTarget(SelectTargetMethod.Random, 0, 50, true));
            summoned.SetFaction(me.GetFaction());
            WaterElementalGUID = summoned.GetGUID();
            summons.Summon(summoned);
        }

        public override void SummonedCreatureDespawn(Creature summoned)
        {
            summons.Despawn(summoned);
        }

        public override void JustDied(Unit killer)
        {
            summons.DespawnAll();
        }

        public override void DoAction(int actionId)
        {
            if (actionId == ActionIds.BuffYell)
                Talk(TextIds.SayAggro);
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(40, damage) && !HasCastIceblock)
            {
                DoCast(SpellIds.Iceblock);
                HasCastIceblock = true;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}