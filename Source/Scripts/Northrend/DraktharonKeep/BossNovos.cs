/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Spells;
using System;

namespace Scripts.Northrend.DraktharonKeep.Novos
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayKill = 1;
        public const uint SayDeath = 2;
        public const uint SaySummoningAdds = 3; // Unused
        public const uint SayArcaneField = 4;
        public const uint EmoteSummoningAdds = 5;  // Unused
    }

    struct SpellIds
    {
        public const uint BeamChannel = 52106;
        public const uint ArcaneField = 47346;

        public const uint SummonRisenShadowcaster = 49105;
        public const uint SummonFetidTrollCorpse = 49103;
        public const uint SummonHulkingCorpse = 49104;
        public const uint SummonCrystalHandler = 49179; //not used
        public const uint SummonCopyOfMinions = 59933; //not used

        public const uint ArcaneBlast = 49198;
        public const uint Blizzard = 49034;
        public const uint Frostbolt = 49037;
        public const uint WrathOfMisery = 50089;
        public const uint SummonMinions = 59910;
    }

    struct Misc
    {
        public const int ActionResetCrystals = 0;
        public const int ActionActivateCrystal = 1;
        public const int ActionDeactivate = 2;
        public const uint EventAttack = 3;
        public const uint EventSummonMinions = 4;
        public const uint EventSummonRisenShadowcaster = 5;
        public const uint EventSummonFetidTrollCorpse = 6;
        public const uint EventSummonHulkingCorpse = 7;
        public const uint EventSummonCrystalHandler = 8;
        public const uint DataNovosAchiev = 9;

        public static SummonerInfo[] summoners =
        {
            new SummonerInfo(EventSummonRisenShadowcaster, 7),
            new SummonerInfo(EventSummonFetidTrollCorpse, 3),
            new SummonerInfo(EventSummonHulkingCorpse, 30),
            new SummonerInfo(EventSummonCrystalHandler, 15)
        };

        public struct SummonerInfo
        {
            public SummonerInfo(uint _eventid, uint _timer)
            {
                eventId = _eventid;
                timer = _timer;
            }

            public uint eventId;
            public uint timer;
        }

        public static Position[] SummonPositions =
        {
            new Position(-306.8209f, -703.7687f, 27.2919f, 3.401838f),
            new Position(-421.395f, -705.7863f, 28.57594f, 4.830696f),
            new Position(-308.1955f, -704.8419f, 27.2919f, 3.010279f),
            new Position(-424.1306f, -705.7354f, 28.57594f, 5.325676f)
        };

        public const float MaxYCoordOhNovosMAX = -771.95f;
    }

    [Script]
    class boss_novos : BossAI
    {
        public boss_novos(Creature creature) : base(creature, DTKDataTypes.Novos)
        {
            Initialize();
            _bubbled = false;
        }

        void Initialize()
        {
            _ohNovos = true;
        }

        public override void Reset()
        {
            _Reset();

            Initialize();
            SetCrystalsStatus(false);
            SetSummonerStatus(false);
            SetBubbled(false);
        }

        public override void EnterCombat(Unit victim)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);

            SetCrystalsStatus(true);
            SetSummonerStatus(true);
            SetBubbled(true);
        }

        public override void AttackStart(Unit target)
        {
            if (!target)
                return;

            if (me.Attack(target, true))
                DoStartNoMovement(target);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || _bubbled)
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Misc.EventSummonMinions:
                        DoCast(SpellIds.SummonMinions);
                        _events.ScheduleEvent(Misc.EventSummonMinions, 15000);
                        break;
                    case Misc.EventAttack:
                        Unit victim = SelectTarget(SelectAggroTarget.Random);
                        if (victim)
                            DoCast(victim, RandomHelper.RAND(SpellIds.ArcaneBlast, SpellIds.Blizzard, SpellIds.Frostbolt, SpellIds.WrathOfMisery));
                        _events.ScheduleEvent(Misc.EventAttack, 3000);
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });
        }

        public override void DoAction(int action)
        {
            if (action == DTKDataTypes.ActionCrystalHandlerDied)
            {
                Talk(TextIds.SayArcaneField);
                SetSummonerStatus(false);
                SetBubbled(false);
                _events.ScheduleEvent(Misc.EventAttack, 3000);
                if (IsHeroic())
                    _events.ScheduleEvent(Misc.EventSummonMinions, 15000);
            }
        }

        public override void MoveInLineOfSight(Unit who)
        {
            base.MoveInLineOfSight(who);

            if (!_ohNovos || !who || !who.IsTypeId(TypeId.Player) || who.GetPositionY() > Misc.MaxYCoordOhNovosMAX)
                return;

            uint entry = who.GetEntry();
            if (entry == DTKCreatureIds.HulkingCorpse || entry == DTKCreatureIds.RisenShadowcaster || entry == DTKCreatureIds.FetidTrollCorpse)
                _ohNovos = false;
        }

        public override uint GetData(uint type)
        {
            return type == Misc.DataNovosAchiev && _ohNovos ? 1 : 0u;
        }

        public override void JustSummoned(Creature summon)
        {
            me.Yell(TextIds.SaySummoningAdds, summon);
            me.TextEmote(TextIds.EmoteSummoningAdds, summon);

            summon.SelectNearestTargetInAttackDistance(50f);
            summons.Summon(summon);
        }

        void SetBubbled(bool state)
        {
            _bubbled = state;
            if (!state)
            {
                if (me.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable))
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                if (me.HasUnitState(UnitState.Casting))
                    me.CastStop();
            }
            else
            {
                if (!me.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable))
                    me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                DoCast(SpellIds.ArcaneField);
            }
        }

        void SetSummonerStatus(bool active)
        {
            for (byte i = 0; i < 4; i++)
            {
                ObjectGuid guid = instance.GetGuidData(DTKDataTypes.NovosSummoner1 + i);
                if (!guid.IsEmpty())
                {
                    Creature crystalChannelTarget = ObjectAccessor.GetCreature(me, guid);
                    if (crystalChannelTarget)
                    {
                        if (active)
                            crystalChannelTarget.GetAI().SetData(Misc.summoners[i].eventId, Misc.summoners[i].timer);
                        else
                            crystalChannelTarget.GetAI().Reset();
                    }
                }
            }
        }

        void SetCrystalsStatus(bool active)
        {
            for (byte i = 0; i < 4; i++)
            {
                ObjectGuid guid = instance.GetGuidData(DTKDataTypes.NovosCrystal1 + i);
                if (!guid.IsEmpty())
                {
                    GameObject crystal = ObjectAccessor.GetGameObject(me, guid);
                    if (crystal)
                        SetCrystalStatus(crystal, active);
                }
            }
        }

        void SetCrystalStatus(GameObject crystal, bool active)
        {
            crystal.SetGoState(active ? GameObjectState.Active : GameObjectState.Ready);

            Creature crystalChannelTarget = crystal.FindNearestCreature(DTKCreatureIds.CrystalChannelTarget, 5.0f);
            if (crystalChannelTarget)
            {
                if (active)
                    crystalChannelTarget.CastSpell(null, SpellIds.BeamChannel);
                else if (crystalChannelTarget.HasUnitState(UnitState.Casting))
                    crystalChannelTarget.CastStop();
            }
        }

        bool _ohNovos;
        bool _bubbled;
    }

    [Script]
    class npc_crystal_channel_target : ScriptedAI
    {
        public npc_crystal_channel_target(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _crystalHandlerCount = 0;
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Misc.EventSummonCrystalHandler:
                        me.SummonCreature(DTKCreatureIds.CrystalHandler, Misc.SummonPositions[_crystalHandlerCount++]);
                        if (_crystalHandlerCount < 4)
                            _events.Repeat(TimeSpan.FromSeconds(15));
                        break;
                    case Misc.EventSummonRisenShadowcaster:
                        DoCast(SpellIds.SummonRisenShadowcaster);
                        _events.Repeat(TimeSpan.FromSeconds(7));
                        break;
                    case Misc.EventSummonFetidTrollCorpse:
                        DoCast(SpellIds.SummonFetidTrollCorpse);
                        _events.Repeat(TimeSpan.FromSeconds(3));
                        break;
                    case Misc.EventSummonHulkingCorpse:
                        DoCast(SpellIds.SummonHulkingCorpse);
                        _events.Repeat(TimeSpan.FromSeconds(30));
                        break;
                }
            });
        }

        public override void SetData(uint id, uint value)
        {
            _events.ScheduleEvent(id, TimeSpan.FromSeconds(value));
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            if (_crystalHandlerCount < 4)
                return;

            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
            {
                ObjectGuid guid = instance.GetGuidData(DTKDataTypes.Novos);
                if (!guid.IsEmpty())
                {
                    Creature novos = ObjectAccessor.GetCreature(me, guid);
                    if (novos)
                        novos.GetAI().DoAction(DTKDataTypes.ActionCrystalHandlerDied);
                }
            }
        }

        public override void JustSummoned(Creature summon)
        {
            InstanceScript instance = me.GetInstanceScript();
            if (instance != null)
            {
                ObjectGuid guid = instance.GetGuidData(DTKDataTypes.Novos);
                if (!guid.IsEmpty())
                {
                    Creature novos = ObjectAccessor.GetCreature(me, guid);
                    if (novos)
                        novos.GetAI().JustSummoned(summon);
                }
            }

            if (summon)
                summon.GetMotionMaster().MovePath(summon.GetEntry() * 100, false);
        }

        uint _crystalHandlerCount;
    }

    [Script]
    class achievement_oh_novos : AchievementCriteriaScript
    {
        public achievement_oh_novos() : base("achievement_oh_novos") { }

        public override bool OnCheck(Player player, Unit target)
        {
            return target && target.IsTypeId(TypeId.Unit) && target.ToCreature().GetAI().GetData(Misc.DataNovosAchiev) != 0;
        }
    }

    [Script]
    class spell_novos_summon_minions : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SummonCopyOfMinions);
        }

        void HandleScript(uint effIndex)
        {
            for (byte i = 0; i < 2; ++i)
                GetCaster().CastSpell((Unit)null, SpellIds.SummonCopyOfMinions, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }
}
