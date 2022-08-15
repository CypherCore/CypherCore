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
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.Karazhan.TerestianIllhoof
{
    struct SpellIds
    {
        public const uint ShadowBolt = 30055;
        public const uint SummonImp = 30066;
        public const uint FiendishPortal1 = 30171;
        public const uint FiendishPortal2 = 30179;
        public const uint Berserk = 32965;
        public const uint SummonFiendishImp = 30184;
        public const uint BrokenPact = 30065;
        public const uint AmplifyFlames = 30053;
        public const uint Firebolt = 30050;
        public const uint SummonDemonchains = 30120;
        public const uint DemonChains = 30206;
        public const uint Sacrifice = 30115;
    }

    struct TextIds
    {
        public const uint SaySlay = 0;
        public const uint SayDeath = 1;
        public const uint SayAggro = 2;
        public const uint SaySacrifice = 3;
        public const uint SaySummonPortal = 4;
    }

    struct MiscConst
    {
        public const uint NpcFiendishPortal = 17265;
        public const int ActionDespawnImps = 1;
    }

    [Script]
    class boss_terestian : BossAI
    {
        public boss_terestian(Creature creature) : base(creature, DataTypes.Terestian) { }

        public override void Reset()
        {
            EntryCheckPredicate pred = new(MiscConst.NpcFiendishPortal);
            summons.DoAction(MiscConst.ActionDespawnImps, pred);
            _Reset();

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.MaxThreat, 0);
                if (target)
                    DoCast(target, SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                me.RemoveAurasDueToSpell(SpellIds.BrokenPact);
                DoCastAOE(SpellIds.SummonImp, new CastSpellExtraArgs(true));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);
                if (target)
                {
                    DoCast(target, SpellIds.Sacrifice, new CastSpellExtraArgs(true));
                    target.CastSpell(target, SpellIds.SummonDemonchains, true);
                    Talk(TextIds.SaySacrifice);
                }
                task.Repeat(TimeSpan.FromSeconds(42));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Talk(TextIds.SaySummonPortal);
                DoCastAOE(SpellIds.FiendishPortal1);
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(11), task =>
            {
                DoCastAOE(SpellIds.FiendishPortal2, new CastSpellExtraArgs(true));
            });
            _scheduler.Schedule(TimeSpan.FromMinutes(10), task =>
            {
                DoCastSelf(SpellIds.Berserk, new CastSpellExtraArgs(true));
            });
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.BrokenPact)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(32), task =>
                {
                    me.RemoveAurasDueToSpell(SpellIds.BrokenPact);
                    DoCastAOE(SpellIds.SummonImp, new CastSpellExtraArgs(true));
                });
            }
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            EntryCheckPredicate pred = new(MiscConst.NpcFiendishPortal);
            summons.DoAction(MiscConst.ActionDespawnImps, pred);
            _JustDied();
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script]
    class npc_kilrek : ScriptedAI
    {
        public npc_kilrek(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
                {
                    DoCastVictim(SpellIds.AmplifyFlames);
                    task.Repeat(TimeSpan.FromSeconds(9));
                });
        }

        public override void JustDied(Unit killer)
        {
            DoCastAOE(SpellIds.BrokenPact, new CastSpellExtraArgs(true));
            me.DespawnOrUnsummon(TimeSpan.FromSeconds(15));
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () =>
            {
                DoMeleeAttackIfReady();
            });
        }
    }

    [Script]
    class npc_demon_chain : PassiveAI
    {
        ObjectGuid _sacrificeGUID;

        public npc_demon_chain(Creature creature) : base(creature) { }

        public override void IsSummonedBy(WorldObject summoner)
        {
            _sacrificeGUID = summoner.GetGUID();
            DoCastSelf(SpellIds.DemonChains, new CastSpellExtraArgs(true));
        }

        public override void JustDied(Unit killer)
        {
            Unit sacrifice = Global.ObjAccessor.GetUnit(me, _sacrificeGUID);
            if (sacrifice)
                sacrifice.RemoveAurasDueToSpell(SpellIds.Sacrifice);
        }
    }

    [Script]
    class npc_fiendish_portal : PassiveAI
    {
        SummonList _summons;

        public npc_fiendish_portal(Creature creature) : base(creature)
        {
            _summons = new(me);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(2400), TimeSpan.FromSeconds(8), task =>
                {
                    DoCastAOE(SpellIds.SummonFiendishImp, new CastSpellExtraArgs(true));
                    task.Repeat();
                });
        }

        public override void DoAction(int action)
        {
            if (action == MiscConst.ActionDespawnImps)
                _summons.DespawnAll();
        }

        public override void JustSummoned(Creature summon)
        {
            _summons.Summon(summon);
            DoZoneInCombat(summon);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_fiendish_imp : ScriptedAI
    {
        public npc_fiendish_imp(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastVictim(SpellIds.Firebolt);
                task.Repeat(TimeSpan.FromMilliseconds(2400));
            });

            me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Fire, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }
}