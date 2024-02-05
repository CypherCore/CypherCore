// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.PyroguardEmberseer
{
    struct SpellIds
    {
        public const uint EncagedEmberseer = 15282; // Self on spawn
        public const uint FireShieldTrigger = 13377; // Self on spawn missing from 335 dbc triggers public const uint FireShield every 3 sec
        public const uint FireShield = 13376; // Triggered by public const uint FireShieldTrigger
        public const uint FreezeAnim = 16245; // Self on event start
        public const uint EmberseerGrowing = 16048; // Self on event start
        public const uint EmberseerGrowingTrigger = 16049; // Triggered by public const uint EmberseerGrowing
        public const uint EmberseerFullStrength = 16047; // Emberseer Full Strength
        public const uint Firenova = 23462; // Combat
        public const uint Flamebuffet = 23341; // Combat
        public const uint Pyroblast = 17274; // Combat
                                             // Blackhand Incarcerator public const uint s
        public const uint EncageEmberseer = 15281; // Emberseer on spawn
        public const uint Strike = 15580; // Combat
        public const uint Encage = 16045; // Combat
                                          // Cast on player by altar
        public const uint EmberseerObjectVisual = 16532;
    }

    struct TextIds
    {
        public const uint EmoteOneStack = 0;
        public const uint EmoteTenStack = 1;
        public const uint EmoteFreeOfBonds = 2;
        public const uint YellFreeOfBonds = 3;
    }

    [Script]
    class boss_pyroguard_emberseer : BossAI
    {
        public boss_pyroguard_emberseer(Creature creature) : base(creature, DataTypes.PyrogaurdEmberseer) { }

        public override void Reset()
        {
            me.SetUninteractible(true);
            me.SetImmuneToPC(true);
            _scheduler.CancelAll();
            // Apply auras on spawn and reset
            // DoCast(me, SpellFireShieldTrigger); // Need to find this in old Dbc if possible
            me.RemoveAura(SpellIds.EmberseerFullStrength);
            me.RemoveAura(SpellIds.EmberseerGrowing);
            me.RemoveAura(SpellIds.EmberseerGrowingTrigger);

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                instance.SetData(DataTypes.BlackhandIncarcerator, 1);
                instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.NotStarted);
            });
            // Hack for missing trigger spell
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                // #### Spell isn't doing any damage ??? ####
                DoCast(me, SpellIds.FireShield);
                task.Repeat(TimeSpan.FromSeconds(3));
            });
        }

        public override void SetData(uint type, uint data)
        {
            switch (data)
            {
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
                    {
                        // As of Patch 3.0.8 only one person needs to channel the altar
                        bool _hasAura = false;
                        var players = me.GetMap().GetPlayers();
                        foreach (var player in players)
                        {
                            if (player != null && player.HasAura(SpellIds.EmberseerObjectVisual))
                            {
                                _hasAura = true;
                                break;
                            }
                        }

                        if (_hasAura)
                        {
                            task.Schedule(TimeSpan.FromSeconds(1), preFlightTask1 =>
                            {
                                // Set data on all Blackhand Incarcerators
                                List<Creature> creatureList = me.GetCreatureListWithEntryInGrid(CreaturesIds.BlackhandIncarcerator, 35.0f);
                                foreach (var creature in creatureList)
                                {
                                    if (creature != null)
                                    {
                                        creature.SetImmuneToAll(false);
                                        creature.InterruptSpell(CurrentSpellTypes.Channeled);
                                        DoZoneInCombat(creature);
                                    }
                                }
                                me.RemoveAura(SpellIds.EncagedEmberseer);
                                preFlightTask1.Schedule(TimeSpan.FromSeconds(32), preFlightTask2 =>
                                {
                                    me.CastSpell(me, SpellIds.FreezeAnim);
                                    me.CastSpell(me, SpellIds.EmberseerGrowing);
                                    Talk(TextIds.EmoteOneStack);
                                });
                            });
                            instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.InProgress);
                        }
                    });
                    break;
                default:
                    break;
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            // ### Todo Check combat timing ###
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCast(me, SpellIds.Firenova);
                task.Repeat(TimeSpan.FromSeconds(6));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCast(me, SpellIds.Flamebuffet);
                task.Repeat(TimeSpan.FromSeconds(14));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                    DoCast(target, SpellIds.Pyroblast);
                task.Repeat(TimeSpan.FromSeconds(15));
            });
        }

        public override void JustDied(Unit killer)
        {
            // Activate all the runes
            UpdateRunes(GameObjectState.Ready);
            // Complete encounter
            instance.SetBossState(DataTypes.PyrogaurdEmberseer, EncounterState.Done);
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.EncageEmberseer)
            {
                if (me.GetAuraCount(SpellIds.EncagedEmberseer) == 0)
                {
                    me.CastSpell(me, SpellIds.EncagedEmberseer);
                    Reset();
                }
            }

            if (spellInfo.Id == SpellIds.EmberseerGrowingTrigger)
            {
                if (me.GetAuraCount(SpellIds.EmberseerGrowingTrigger) == 10)
                    Talk(TextIds.EmoteTenStack);

                if (me.GetAuraCount(SpellIds.EmberseerGrowingTrigger) == 20)
                {
                    me.RemoveAura(SpellIds.FreezeAnim);
                    me.CastSpell(me, SpellIds.EmberseerFullStrength);
                    Talk(TextIds.EmoteFreeOfBonds);
                    Talk(TextIds.YellFreeOfBonds);
                    me.SetUninteractible(false);
                    me.SetImmuneToPC(false);
                    _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
                    {
                        AttackStart(me.SelectNearestPlayer(30.0f));
                    });
                }
            }
        }

        void UpdateRunes(GameObjectState state)
        {
            // update all runes
            GameObject rune1 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune1));
            if (rune1 != null)
                rune1.SetGoState(state);
            GameObject rune2 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune2));
            if (rune2 != null)
                rune2.SetGoState(state);
            GameObject rune3 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune3));
            if (rune3 != null)
                rune3.SetGoState(state);
            GameObject rune4 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune4));
            if (rune4 != null)
                rune4.SetGoState(state);
            GameObject rune5 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune5));
            if (rune5 != null)
                rune5.SetGoState(state);
            GameObject rune6 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune6));
            if (rune6 != null)
                rune6.SetGoState(state);
            GameObject rune7 = ObjectAccessor.GetGameObject(me, instance.GetGuidData(GameObjectsIds.EmberseerRune7));
            if (rune7 != null)
                rune7.SetGoState(state);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            if (!UpdateVictim())
                return;
        }
    }

    [Script]
    class npc_blackhand_incarcerator : ScriptedAI
    {
        public npc_blackhand_incarcerator(Creature creature) : base(creature) { }

        public override void JustAppeared()
        {
            DoCast(SpellIds.EncageEmberseer);
        }

        public override void JustEngagedWith(Unit who)
        {
            // Had to do this because CallForHelp will ignore any npcs without Los
            List<Creature> creatureList = me.GetCreatureListWithEntryInGrid(CreaturesIds.BlackhandIncarcerator, 60.0f);
            foreach (var creature in creatureList)
                if (creature != null)
                    DoZoneInCombat(creature);    // GetAI().AttackStart(me.GetVictim());

            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), task =>
            {
                DoCastVictim(SpellIds.Strike, new CastSpellExtraArgs(true));
                task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(23));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.Encage, new CastSpellExtraArgs(true));
                task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(12));
            });
        }

        public override void JustReachedHome()
        {
            DoCast(SpellIds.EncageEmberseer);

            me.SetImmuneToAll(true);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}

