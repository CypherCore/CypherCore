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
using System.Collections.Generic;
using System.Linq;
using Game;

namespace Scripts.Northrend.AzjolNerub.Ahnkahet.HeraldVolazj
{
    struct SpellIds
    {
        public const uint Insanity = 57496; //Dummy
        public const uint InsanityVisual = 57561;
        public const uint InsanityTarget = 57508;
        public const uint MindFlay = 57941;
        public const uint ShadowBoltVolley = 57942;
        public const uint Shiver = 57949;
        public const uint ClonePlayer = 57507; //Cast On Player During Insanity
        public const uint InsanityPhasing1 = 57508;
        public const uint InsanityPhasing2 = 57509;
        public const uint InsanityPhasing3 = 57510;
        public const uint InsanityPhasing4 = 57511;
        public const uint InsanityPhasing5 = 57512;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySlay = 1;
        public const uint SayDeath = 2;
        public const uint SayPhase = 3;
    }

    struct Misc
    {
        public const uint AchievQuickDemiseStartEvent = 20382;
    }

    [Script]
    class boss_volazj : ScriptedAI
    {
        public boss_volazj(Creature creature) : base(creature)
        {
            Summons = new SummonList(me);

            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            uiMindFlayTimer = 8 * Time.InMilliseconds;
            uiShadowBoltVolleyTimer = 5 * Time.InMilliseconds;
            uiShiverTimer = 15 * Time.InMilliseconds;
            // Used for Insanity handling
            insanityHandled = 0;
        }

        // returns the percentage of health after taking the given damage.
        uint GetHealthPct(uint damage)
        {
            if (damage > me.GetHealth())
                return 0;
            return (uint)(100 * (me.GetHealth() - damage) / me.GetMaxHealth());
        }

        public override void DamageTaken(Unit pAttacker, ref uint damage)
        {
            if (me.HasFlag(UnitFields.Flags, UnitFlags.NotSelectable))
                damage = 0;

            if ((GetHealthPct(0) >= 66 && GetHealthPct(damage) < 66) ||
                (GetHealthPct(0) >= 33 && GetHealthPct(damage) < 33))
            {
                me.InterruptNonMeleeSpells(false);
                DoCast(me, SpellIds.Insanity, false);
            }
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.Insanity)
            {
                // Not good target or too many players
                if (target.GetTypeId() != TypeId.Player || insanityHandled > 4)
                    return;
                // First target - start channel visual and set self as unnattackable
                if (insanityHandled == 0)
                {
                    // Channel visual
                    DoCast(me, SpellIds.InsanityVisual, true);
                    // Unattackable
                    me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                    me.SetControlled(true, UnitState.Stunned);
                }

                // phase the player
                target.CastSpell(target, SpellIds.InsanityTarget + insanityHandled, true);

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.InsanityTarget + insanityHandled);
                if (spellInfo == null)
                    return;

                // summon twisted party members for this target
                var players = me.GetMap().GetPlayers();
                foreach (var player in players)
                {
                    if (!player || !player.IsAlive())
                        continue;
                    // Summon clone
                    Unit summon = me.SummonCreature(AKCreatureIds.TwistedVisage, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation(), TempSummonType.CorpseDespawn, 0);
                    if (summon)
                    {
                        // clone
                        player.CastSpell(summon, SpellIds.ClonePlayer, true);
                        // phase the summon
                        PhasingHandler.AddPhase(summon, (uint)spellInfo.GetEffect(0).MiscValueB, true);
                    }
                }
                ++insanityHandled;
            }
        }

        void ResetPlayersPhase()
        {
            var players = me.GetMap().GetPlayers();
            foreach (var player in players)
            {
                for (uint index = 0; index <= 4; ++index)
                    player.RemoveAurasDueToSpell(SpellIds.InsanityTarget + index);
            }
        }

        public override void Reset()
        {
            Initialize();

            instance.SetBossState(DataTypes.HeraldVolazj, EncounterState.NotStarted);
            instance.DoStopCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievQuickDemiseStartEvent);

            // Visible for all players in insanity
            for (uint i = 173; i <= 177; ++i)
                PhasingHandler.AddPhase(me, i, false);
            PhasingHandler.AddPhase(me, 169, true);

            ResetPlayersPhase();

            // Cleanup
            Summons.DespawnAll();
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.SetControlled(false, UnitState.Stunned);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayAggro);

            instance.SetBossState(DataTypes.HeraldVolazj, EncounterState.InProgress);
            instance.DoStartCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievQuickDemiseStartEvent);
        }

        public override void JustSummoned(Creature summon)
        {
            Summons.Summon(summon);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            uint nextPhase = 0;
            Summons.Despawn(summon);

            // Check if all summons in this phase killed
            foreach (var guid in Summons)
            {
                Creature visage = ObjectAccessor.GetCreature(me, guid);
                if (visage)
                {
                    // Not all are dead
                    if (visage.IsInPhase(summon))
                        return;
                    else
                    {
                        nextPhase = visage.GetPhaseShift().GetPhases().First().Key;
                        break;
                    }
                }
            }

            // Roll Insanity
            var players = me.GetMap().GetPlayers();
            foreach (var player in players)
            {
                if (player)
                {
                    for (uint index = 0; index <= 4; ++index)
                        player.RemoveAurasDueToSpell(SpellIds.InsanityTarget + index);
                    player.CastSpell(player, SpellIds.InsanityTarget + nextPhase - 173, true);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            //Return since we have no target
            if (!UpdateVictim())
                return;

            if (insanityHandled != 0)
            {
                if (!Summons.Empty())
                    return;

                insanityHandled = 0;
                me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                me.SetControlled(false, UnitState.Stunned);
                me.RemoveAurasDueToSpell(SpellIds.InsanityVisual);
            }

            if (uiMindFlayTimer <= diff)
            {
                DoCastVictim(SpellIds.MindFlay);
                uiMindFlayTimer = 20 * Time.InMilliseconds;
            }
            else uiMindFlayTimer -= diff;

            if (uiShadowBoltVolleyTimer <= diff)
            {
                DoCastVictim(SpellIds.ShadowBoltVolley);
                uiShadowBoltVolleyTimer = 5 * Time.InMilliseconds;
            }
            else uiShadowBoltVolleyTimer -= diff;

            if (uiShiverTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.Shiver);
                uiShiverTimer = 15 * Time.InMilliseconds;
            }
            else uiShiverTimer -= diff;

            DoMeleeAttackIfReady();
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            instance.SetBossState(DataTypes.HeraldVolazj, EncounterState.Done);

            Summons.DespawnAll();
            ResetPlayersPhase();
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(TextIds.SaySlay);
        }

        InstanceScript instance;

        uint uiMindFlayTimer;
        uint uiShadowBoltVolleyTimer;
        uint uiShiverTimer;
        uint insanityHandled;
        SummonList Summons;
    }
}
