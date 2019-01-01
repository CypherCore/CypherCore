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
using System.Collections.Generic;

namespace Scripts.Northrend.Nexus.Nexus
{
    struct KeristraszaConst
    {
        //Spells
        public const uint SpellFrozenPrison = 47854;
        public const uint SpellTailSweep = 50155;
        public const uint SpellCrystalChains = 50997;
        public const uint SpellEnrage = 8599;
        public const uint SpellCrystalFireBreath = 48096;
        public const uint SpellCrystalize = 48179;
        public const uint SpellIntenseCold = 48094;
        public const uint SpellIntenseColdTriggered = 48095;

        //Yells
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayEnrage = 2;
        public const uint SayDeath = 3;
        public const uint SayCrystalNova = 4;
        public const uint SayFrenzy = 5;

        //Misc
        public const uint DataIntenseCold = 1;
        public const uint DataContainmentSpheres = 3;
    }

    [Script]
    public class boss_keristrasza : BossAI
    {
        public boss_keristrasza(Creature creature) : base(creature, DataTypes.Keristrasza)
        {
            Initialize();
        }

        void Initialize()
        {
            _enrage = false;

            //Crystal FireBreath
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                DoCastVictim(KeristraszaConst.SpellCrystalFireBreath);
                task.Repeat(TimeSpan.FromSeconds(14));
            });

            //CrystalChainsCrystalize
            _scheduler.Schedule(TimeSpan.FromSeconds(DungeonMode<uint>(30, 11)), task =>
            {
                Talk(KeristraszaConst.SayCrystalNova);
                if (IsHeroic())
                    DoCast(me, KeristraszaConst.SpellCrystalize);
                else
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f, true);
                    if (target)
                        DoCast(target, KeristraszaConst.SpellCrystalChains);
                }

                task.Repeat(TimeSpan.FromSeconds(DungeonMode<uint>(30, 11)));
            });

            //TailSweep
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCast(me, KeristraszaConst.SpellTailSweep);
                task.Repeat(TimeSpan.FromSeconds(5));
            });
        }

        public override void Reset()
        {
            Initialize();
            _intenseColdList.Clear();

            me.RemoveFlag(UnitFields.Flags, UnitFlags.Stunned);

            RemovePrison(CheckContainmentSpheres());
            _Reset();
        }

        public override void EnterCombat(Unit who)
        {
            Talk(KeristraszaConst.SayAggro);
            DoCastAOE(KeristraszaConst.SpellIntenseCold);
            _EnterCombat();
        }

        public override void JustDied(Unit killer)
        {
            Talk(KeristraszaConst.SayDeath);
            _JustDied();
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(KeristraszaConst.SaySlay);
        }

        public bool CheckContainmentSpheres(bool removePrison = false)
        {
            for (uint i = DataTypes.AnomalusContainmetSphere; i < (DataTypes.AnomalusContainmetSphere + KeristraszaConst.DataContainmentSpheres); ++i)
            {
                GameObject containmentSpheres = ObjectAccessor.GetGameObject(me, instance.GetGuidData(i));
                if (!containmentSpheres || containmentSpheres.GetGoState() != GameObjectState.Active)
                    return false;
            }
            if (removePrison)
                RemovePrison(true);
            return true;
        }

        void RemovePrison(bool remove)
        {
            if (remove)
            {
                me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                if (me.HasAura(KeristraszaConst.SpellFrozenPrison))
                    me.RemoveAurasDueToSpell(KeristraszaConst.SpellFrozenPrison);
            }
            else
            {
                me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                DoCast(me, KeristraszaConst.SpellFrozenPrison, false);
            }
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            if (id == KeristraszaConst.DataIntenseCold)
                _intenseColdList.Add(guid);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (!_enrage && me.HealthBelowPctDamaged(25, damage))
            {
                Talk(KeristraszaConst.SayEnrage);
                Talk(KeristraszaConst.SayFrenzy);
                DoCast(me, KeristraszaConst.SpellEnrage);
                _enrage = true;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }

        bool _enrage;

        public List<ObjectGuid> _intenseColdList = new List<ObjectGuid>();
    }

    [Script]
    class containment_sphere : GameObjectScript
    {
        public containment_sphere() : base("containment_sphere") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            InstanceScript instance = go.GetInstanceScript();

            Creature pKeristrasza = ObjectAccessor.GetCreature(go, instance.GetGuidData(DataTypes.Keristrasza));
            if (pKeristrasza && pKeristrasza.IsAlive())
            {
                // maybe these are hacks :(
                go.SetFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                go.SetGoState(GameObjectState.Active);

                ((boss_keristrasza)pKeristrasza.GetAI()).CheckContainmentSpheres(true);
            }
            return true;
        }

    }

    [Script]
    class spell_intense_cold : AuraScript
    {
        void HandlePeriodicTick(AuraEffect aurEff)
        {
            if (aurEff.GetBase().GetStackAmount() < 2)
                return;
            Unit caster = GetCaster();
            // @todo the caster should be boss but not the player
            if (!caster || caster.GetAI() == null)
                return;

            caster.GetAI().SetGUID(GetTarget().GetGUID(), (int)KeristraszaConst.DataIntenseCold);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 1, AuraType.PeriodicDamage));
        }
    }

    [Script]
    class achievement_intense_cold : AchievementCriteriaScript
    {
        public achievement_intense_cold() : base("achievement_intense_cold") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            var _intenseColdList = ((boss_keristrasza)target.ToCreature().GetAI())._intenseColdList;
            if (!_intenseColdList.Empty())
            {
                foreach (var guid in _intenseColdList)
                    if (player.GetGUID() == guid)
                        return false;
            }

            return true;
        }
    }
}
