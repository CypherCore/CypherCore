// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.BrokenIsles.ZoneMardum
{
    [Script]
    class scene_demonhunter_intro : SceneScript
    {
        const uint ConvoDemonhunterIntroStart = 705;

        public scene_demonhunter_intro() : base("scene_demonhunter_intro") { }

        public override void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Conversation.CreateConversation(ConvoDemonhunterIntroStart, player, player, player.GetGUID(), null);
        }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            PhasingHandler.OnConditionChange(player);
        }
    }

    [Script] // 196030 - Start: Quest Invis
    class spell_demon_hunter_intro : AuraScript
    {
        const uint SpellStartDemonHunterPlayScene = 193525;

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().CastSpell(null, SpellStartDemonHunterPlayScene, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    struct TheInvasionBeginsConst
    {
        public const uint QuestTheInvasionBegins = 40077;

        public const int ConvoTheInvasionBegins = 922;

        public const uint NpcKaynSunfuryInvasionBegins = 93011;
        public const uint NpcJayceDarkweaverInvasionBegins = 98228;
        public const uint NpcAllariTheSouleaterInvasionBegins = 98227;
        public const uint NpcCyanaNightglaiveInvasionBegins = 98290;
        public const uint NpcKorvasBloodthornInvasionBegins = 98292;
        public const uint NpcSevisBrightflameInvasionBegins = 99918;
        public const uint NpcWrathWarriorInvasionBegins = 94580;

        public const uint SpellTheInvasionBegins = 187382;
        public const uint SpellTrackTargetInChannel = 175799;
        public const uint SpellDemonHunterGlideState = 199303;

        // Kayn
        public const uint PathKaynAttackDemon = 9301100;
        public const uint PathKaynAfterDemon = 9301101;

        // Path before Jump
        public const uint PathJayceInvasionBegins = 9822800;
        public const uint PathAllariInvasionBegins = 9822700;
        public const uint PathCyanaInvasionBegins = 9829000;
        public const uint PathKorvasinvasionBegins = 9829200;
        public const uint PathSevisinvasionBegins = 9991800;

        // Path after Jump
        public const uint PathJayceJupinvasionBegins = 9822801;
        public const uint PathAllariJupinvasionBegins = 9822701;
        public const uint PathCyanaJupinvasionBegins = 9829001;
        public const uint PathKorvasJupinvasionBegins = 9829201;
        public const uint PathSevisJupinvasionBegins = 9991801;

        public const uint PointIllidariLandPos = 1;
        public const uint PointKaynTriggerDoubleJump = 2;
        public const uint PointKaynMoveToDemon = 3;

        public const ushort AnimDhWings = 58110;
        public const ushort AnimDhRun = 9767;
        public const ushort AnimDhRunAllari = 9180;

        public const uint SpellVisualKitKaynGlide = 59738;
        public const uint SpellVisualKitKaynWings = 59406;
        public const uint SpellVisualKitKaynDoubleJump = 58110;
        public const uint SpellVisualKitKorvasJump = 63071;
        public const uint SpellVisualKitWrathWarriorDie = 58476;

        public static Position WrathWarriorSpawnPosition = new(1081.9166f, 3183.8716f, 26.335993f);
        public static Position KaynJumpPos = new(1172.17f, 3202.55f, 54.3479f);
        public static Position KaynDoubleJumpPosition = new(1094.2384f, 3186.058f, 28.81562f);
        public static Position JayceJumpPos = new(1119.24f, 3203.42f, 38.1061f);
        public static Position AllariJumpPos = new(1120.08f, 3197.2f, 36.8502f);
        public static Position KorvasJumpPos = new(1117.89f, 3196.24f, 36.2158f);
        public static Position SevisJumpPos = new(1120.74f, 3199.47f, 37.5157f);
        public static Position CyanaJumpPos = new(1120.34f, 3194.28f, 36.4321f);
    }

    [Script] // 93011 - Kayn Sunfury
    class npc_kayn_sunfury_invasion_begins : ScriptedAI
    {
        const uint SoundSpellDoubleJump = 53780;

        public npc_kayn_sunfury_invasion_begins(Creature creature) : base(creature) { }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == TheInvasionBeginsConst.QuestTheInvasionBegins)
            {
                PhasingHandler.OnConditionChange(player);
                player.CastSpell(TheInvasionBeginsConst.WrathWarriorSpawnPosition, TheInvasionBeginsConst.SpellTheInvasionBegins, false);
                Conversation.CreateConversation(TheInvasionBeginsConst.ConvoTheInvasionBegins, player, player, player.GetGUID(), null, false);
            }
        }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathKaynAttackDemon)
            {
                Creature wrathWarrior = me.FindNearestCreatureWithOptions(100.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcWrathWarriorInvasionBegins, IgnorePhases = true, OwnerGuid = me.GetOwnerGUID() });
                if (wrathWarrior == null)
                    return;

                me.SetFacingToObject(wrathWarrior);

                wrathWarrior.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitWrathWarriorDie, 0, 0);
                wrathWarrior.KillSelf();

                _scheduler.Schedule(TimeSpan.FromMilliseconds(600), _ => me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathKaynAfterDemon, false));
            }
            else if (pathId == TheInvasionBeginsConst.PathKaynAfterDemon)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointKaynTriggerDoubleJump)
            {
                TempSummon summon = me.ToTempSummon();
                if (summon == null)
                    return;

                WorldObject summoner = summon.GetSummoner();
                if (summoner == null)
                    return;

                Player summonerPlayer = summoner.ToPlayer();
                if (summonerPlayer == null)
                    return;

                me.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitKaynWings, 4, 3000);
                me.PlayObjectSound(SoundSpellDoubleJump, me.GetGUID(), summonerPlayer);
                me.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitKaynDoubleJump, 0, 0);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.KaynDoubleJumpPosition, 24.0f, 0.9874f, TheInvasionBeginsConst.PointKaynMoveToDemon);
            }
            else if (pointId == TheInvasionBeginsConst.PointKaynMoveToDemon)
            {
                me.SetAIAnimKitId(TheInvasionBeginsConst.AnimDhRun);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathKaynAttackDemon, false, null, 4.0f);
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script] // 98228 - Jayce Darkweaver
    class npc_jayce_darkweaver_invasion_begins : ScriptedAI
    {
        public npc_jayce_darkweaver_invasion_begins(Creature creature) : base(creature) { }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathJayceInvasionBegins)
            {
                me.CastSpell(null, TheInvasionBeginsConst.SpellDemonHunterGlideState, true);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.JayceJumpPos, 12.0f, 15.2792f, TheInvasionBeginsConst.PointIllidariLandPos);
            }
            else if (pathId == TheInvasionBeginsConst.PathJayceJupinvasionBegins)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointIllidariLandPos)
            {
                me.RemoveAurasDueToSpell(TheInvasionBeginsConst.SpellDemonHunterGlideState);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathJayceJupinvasionBegins, false);
            }
        }
    }

    [Script] // 98227 - Allari the Souleater
    class npc_allari_the_souleater_invasion_begins : ScriptedAI
    {
        public npc_allari_the_souleater_invasion_begins(Creature creature) : base(creature) { }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathAllariInvasionBegins)
            {
                me.CastSpell(null, TheInvasionBeginsConst.SpellDemonHunterGlideState, true);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.AllariJumpPos, 12.0f, 9.2722f, TheInvasionBeginsConst.PointIllidariLandPos);
            }
            else if (pathId == TheInvasionBeginsConst.PathAllariJupinvasionBegins)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointIllidariLandPos)
            {
                me.RemoveAurasDueToSpell(TheInvasionBeginsConst.SpellDemonHunterGlideState);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathAllariJupinvasionBegins, false);
            }
        }
    }

    [Script] // 98292 - Korvas Bloodthorn
    class npc_korvas_bloodthorn_invasion_begins : ScriptedAI
    {
        public npc_korvas_bloodthorn_invasion_begins(Creature creature) : base(creature) { }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathKorvasinvasionBegins)
            {
                me.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitKorvasJump, 4, 2000);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.KorvasJumpPos, 24.0f, 19.2911f, TheInvasionBeginsConst.PointIllidariLandPos);
            }
            else if (pathId == TheInvasionBeginsConst.PathKorvasJupinvasionBegins)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointIllidariLandPos)
            {
                me.RemoveAurasDueToSpell(TheInvasionBeginsConst.SpellDemonHunterGlideState);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathKorvasJupinvasionBegins, false);
            }
        }
    }

    [Script] // 99918 - Sevis Brightflame
    class npc_sevis_brightflame_invasion_begins : ScriptedAI
    {
        public npc_sevis_brightflame_invasion_begins(Creature creature) : base(creature) { }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathSevisinvasionBegins)
            {
                me.CastSpell(null, TheInvasionBeginsConst.SpellDemonHunterGlideState, true);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.SevisJumpPos, 12.0f, 13.3033f, TheInvasionBeginsConst.PointIllidariLandPos);
            }
            else if (pathId == TheInvasionBeginsConst.PathSevisJupinvasionBegins)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointIllidariLandPos)
            {
                me.RemoveAurasDueToSpell(TheInvasionBeginsConst.SpellDemonHunterGlideState);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathSevisJupinvasionBegins, false);
            }
        }
    }

    [Script] // 98290 - Cyana Nightglaive
    class npc_cyana_nightglaive_invasion_begins : ScriptedAI
    {
        public npc_cyana_nightglaive_invasion_begins(Creature creature) : base(creature) { }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (pathId == TheInvasionBeginsConst.PathCyanaInvasionBegins)
            {
                me.CastSpell(null, TheInvasionBeginsConst.SpellDemonHunterGlideState, true);
                me.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.CyanaJumpPos, 12.0f, 8.4555f, TheInvasionBeginsConst.PointIllidariLandPos);
            }
            else if (pathId == TheInvasionBeginsConst.PathCyanaJupinvasionBegins)
                me.DespawnOrUnsummon();
        }

        public override void MovementInform(MovementGeneratorType type, uint pointId)
        {
            if (type != MovementGeneratorType.Effect)
                return;

            if (pointId == TheInvasionBeginsConst.PointIllidariLandPos)
            {
                me.RemoveAurasDueToSpell(TheInvasionBeginsConst.SpellDemonHunterGlideState);
                me.GetMotionMaster().MovePath(TheInvasionBeginsConst.PathCyanaJupinvasionBegins, false);
            }
        }
    }

    [Script] // 922 - The Invasion Begins
    class conversation_the_invasion_begins : ConversationScript
    {
        const int ConvoLineTriggerFacing = 2529;
        const int ConvoLineStartPath = 2288;

        const int ConvoActorIdxKayn = 1;
        const int ConvoActorIdxKorvas = 2;

        const uint SoundMetalWeaponUnsheath = 700;

        TaskScheduler _scheduler = new TaskScheduler();

        ObjectGuid _jayceGUID;
        ObjectGuid _allariGUID;
        ObjectGuid _cyanaGUID;
        ObjectGuid _sevisGUID;

        public conversation_the_invasion_begins() : base("conversation_the_invasion_begins") { }

        public override void OnConversationCreate(Conversation conversation, Unit creator)
        {
            Creature kaynObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcKaynSunfuryInvasionBegins, IgnorePhases = true });
            Creature jayceObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcJayceDarkweaverInvasionBegins, IgnorePhases = true });
            Creature allariaObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcAllariTheSouleaterInvasionBegins, IgnorePhases = true });
            Creature cyanaObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcCyanaNightglaiveInvasionBegins, IgnorePhases = true });
            Creature korvasObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcKorvasBloodthornInvasionBegins, IgnorePhases = true });
            Creature sevisObject = creator.FindNearestCreatureWithOptions(10.0f, new FindCreatureOptions() { CreatureId = TheInvasionBeginsConst.NpcSevisBrightflameInvasionBegins, IgnorePhases = true });
            if (kaynObject == null || jayceObject == null || allariaObject == null || cyanaObject == null || korvasObject == null || sevisObject == null)
                return;

            TempSummon kaynClone = kaynObject.SummonPersonalClone(kaynObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            TempSummon jayceClone = jayceObject.SummonPersonalClone(jayceObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            TempSummon allariaClone = allariaObject.SummonPersonalClone(allariaObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            TempSummon cyanaClone = cyanaObject.SummonPersonalClone(cyanaObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            TempSummon korvasClone = korvasObject.SummonPersonalClone(korvasObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            TempSummon sevisClone = sevisObject.SummonPersonalClone(sevisObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), 0, 0, creator.ToPlayer());
            if (kaynClone == null || jayceClone == null || allariaClone == null || cyanaClone == null || korvasClone == null || sevisClone == null)
                return;

            _jayceGUID = jayceClone.GetGUID();
            _allariGUID = allariaClone.GetGUID();
            _cyanaGUID = cyanaClone.GetGUID();
            _sevisGUID = sevisClone.GetGUID();
            allariaClone.SetAIAnimKitId(TheInvasionBeginsConst.AnimDhRunAllari);
            kaynClone.RemoveNpcFlag(NPCFlags.Gossip | NPCFlags.QuestGiver);

            conversation.AddActor(TheInvasionBeginsConst.ConvoTheInvasionBegins, ConvoActorIdxKayn, kaynClone.GetGUID());
            conversation.AddActor(TheInvasionBeginsConst.ConvoTheInvasionBegins, ConvoActorIdxKorvas, korvasClone.GetGUID());
            conversation.Start();
        }

        public override void OnConversationStart(Conversation conversation)
        {
            Locale privateOwnerLocale = conversation.GetPrivateObjectOwnerLocale();

            TimeSpan illidariFacingLineStarted = conversation.GetLineStartTime(privateOwnerLocale, ConvoLineTriggerFacing);
            if (illidariFacingLineStarted != TimeSpan.Zero)
            {
                _scheduler.Schedule(illidariFacingLineStarted, _ =>
                {
                    StartCloneChannel(conversation.GetActorUnit(ConvoActorIdxKayn).GetGUID(), conversation);
                    StartCloneChannel(conversation.GetActorUnit(ConvoActorIdxKorvas).GetGUID(), conversation);
                    StartCloneChannel(_jayceGUID, conversation);
                    StartCloneChannel(_allariGUID, conversation);
                    StartCloneChannel(_cyanaGUID, conversation);
                    StartCloneChannel(_sevisGUID, conversation);
                });
            }

            TimeSpan illidariStartPathLineStarted = conversation.GetLineStartTime(privateOwnerLocale, ConvoLineStartPath);
            if (illidariStartPathLineStarted != TimeSpan.Zero)
            {
                _scheduler.Schedule(illidariStartPathLineStarted, _ =>
                {
                    Creature kaynClone = conversation.GetActorCreature(ConvoActorIdxKayn);
                    if (kaynClone == null)
                        return;

                    Unit privateObjectOwner = ObjAccessor.GetUnit(conversation, conversation.GetPrivateObjectOwner());
                    if (privateObjectOwner == null)
                        return;

                    Player player = privateObjectOwner.ToPlayer();
                    if (player == null)
                        return;

                    kaynClone.PlayObjectSound(SoundMetalWeaponUnsheath, kaynClone.GetGUID(), player);
                    kaynClone.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitKaynGlide, 4, 3000);
                    kaynClone.SendPlaySpellVisualKit(TheInvasionBeginsConst.SpellVisualKitKaynWings, 4, 4000);
                    kaynClone.GetMotionMaster().MoveJumpWithGravity(TheInvasionBeginsConst.KaynJumpPos, 20.5f, 396.3535f, TheInvasionBeginsConst.PointKaynTriggerDoubleJump);
                    kaynClone.SetSheath(SheathState.Melee);
                    kaynClone.SetNpcFlag(NPCFlags.QuestGiver);

                    StartCloneMovement(conversation.GetActorUnit(ConvoActorIdxKorvas).GetGUID(), TheInvasionBeginsConst.PathKorvasinvasionBegins, TheInvasionBeginsConst.AnimDhRun, conversation);
                    StartCloneMovement(_jayceGUID, TheInvasionBeginsConst.PathJayceInvasionBegins, 0, conversation);
                    StartCloneMovement(_allariGUID, TheInvasionBeginsConst.PathAllariInvasionBegins, TheInvasionBeginsConst.AnimDhRunAllari, conversation);
                    StartCloneMovement(_cyanaGUID, TheInvasionBeginsConst.PathCyanaInvasionBegins, 0, conversation);
                    StartCloneMovement(_sevisGUID, TheInvasionBeginsConst.PathSevisinvasionBegins, TheInvasionBeginsConst.AnimDhRun, conversation);
                });
            }
        }

        void StartCloneChannel(ObjectGuid guid, Conversation conversation)
        {
            Unit privateObjectOwner = ObjAccessor.GetUnit(conversation, conversation.GetPrivateObjectOwner());
            if (privateObjectOwner == null)
                return;

            Creature clone = ObjectAccessor.GetCreature(conversation, guid);
            if (clone == null)
                return;

            clone.CastSpell(privateObjectOwner, TheInvasionBeginsConst.SpellTrackTargetInChannel, false);
        }

        void StartCloneMovement(ObjectGuid cloneGUID, uint pathId, uint animKit, Conversation conversation)
        {
            Creature clone = ObjectAccessor.GetCreature(conversation, cloneGUID);
            if (clone == null)
                return;

            clone.InterruptNonMeleeSpells(true);
            clone.GetMotionMaster().MovePath(pathId, false);
            if (animKit != 0)
                clone.SetAIAnimKitId((ushort)animKit);
        }

        public override void OnConversationUpdate(Conversation conversation, uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    // 98459 - Kayn Sunfury
    // 98458 - Jayce Darkweaver
    // 98456 - Allari the Souleater
    // 98460 - Korvas Bloodthorn
    // 99919 - Sevis Brightflame
    [Script] // 98457 - Cyana Nightglaive
    class npc_illidari_fighting_invasion_begins : ScriptedAI
    {
        const uint SpellIllidariChaosStrike = 197639;
        const uint SpellIllidariFelRush = 200879;

        public npc_illidari_fighting_invasion_begins(Creature creature) : base(creature) { }

        Unit GetNextTarget()
        {
            List<Unit> targetList = new();
            AnyUnfriendlyUnitInObjectRangeCheck checker = new(me, me, 100.0f);
            UnitListSearcher searcher = new(me, targetList, checker);
            Cell.VisitAllObjects(me, searcher, 100.0f);
            targetList.RemoveAll(possibleTarget => possibleTarget.IsAttackingPlayer());

            return targetList.SelectRandom();
        }

        void ScheduleTargetSelection()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(200), task =>
            {
                Unit target = GetNextTarget();
                if (target == null)
                {
                    task.Repeat(TimeSpan.FromMilliseconds(500));
                    return;
                }
                AttackStart(target);
            });
        }

        public override void JustAppeared()
        {
            ScheduleTargetSelection();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIllidariChaosStrike);
                task.Repeat(TimeSpan.FromSeconds(5));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIllidariFelRush);
                task.Repeat(TimeSpan.FromSeconds(7));
            });
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            // manualling calling it to not move to home position but move to next target instead
            _EnterEvadeMode(why);
            Reset();
            ScheduleTargetSelection();
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    struct AshtongueIntroData
    {
        public const uint QuestEnterTheIllidariAshtongue = 40378;

        public const uint NpcKaynSunfuryAshtongue = 98229;
        public const uint NpcKorvasBloodthornAshtongue = 98354;
        public const uint NpcSevisBrightflameAshtongue = 99916;
        public const uint NpcAllariSouleaterAshtongue = 94410;

        public const uint DisplayIdSevisMount = 64385;

        public const uint SayKaynActivateGateway = 0;
        public const uint SayKaynCutAHole = 1;
        public const uint SayKorvasSlayMoreDemons = 0;
        public const uint SaySevisSayFindAllari = 1;

        public const uint SpellVisualKitSevisMount = 36264;

        public const uint SpellCastMountDhFelsaber = 200175;
        public const uint SpellAshtongueFellsaberKillCredit = 200254;

        public const uint PathKaynSunfuryNearTeleport = 9822900;
        public const uint PathKorvasBloodthornNearTeleport = 9835400;
        public const uint PathSevisBrightflameGateway = 9991600;
    }

    [Script] // 98229 - Kayn Sunfury
    class npc_kayn_sunfury_ashtongue_intro : ScriptedAI
    {
        public npc_kayn_sunfury_ashtongue_intro(Creature creature) : base(creature) { }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == AshtongueIntroData.QuestEnterTheIllidariAshtongue)
            {
                PhasingHandler.OnConditionChange(player);
                Creature kaynObject = GetClosestCreatureWithOptions(player, 10.0f, new FindCreatureOptions() { CreatureId = AshtongueIntroData.NpcKaynSunfuryAshtongue, IgnorePhases = true });
                Creature korvasObject = GetClosestCreatureWithOptions(player, 10.0f, new FindCreatureOptions() { CreatureId = AshtongueIntroData.NpcKorvasBloodthornAshtongue, IgnorePhases = true });
                if (kaynObject == null || korvasObject == null)
                    return;

                TempSummon kaynClone = kaynObject.SummonPersonalClone(kaynObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.Zero, 0, 0, player);
                TempSummon korvasClone = korvasObject.SummonPersonalClone(korvasObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.Zero, 0, 0, player);
                if (kaynClone == null || korvasClone == null)
                    return;

                korvasClone.SetEmoteState(Emote.StateReady1h);
                kaynClone.RemoveNpcFlag(NPCFlags.QuestGiver);
            }
        }

        public override void JustAppeared()
        {
            if (!me.IsPrivateObject())
                return;

            Creature korvasObject = GetClosestCreatureWithOptions(me, 10.0f, new FindCreatureOptions() { CreatureId = AshtongueIntroData.NpcKorvasBloodthornAshtongue, IgnorePhases = true, PrivateObjectOwnerGuid = me.GetPrivateObjectOwner() });
            if (korvasObject == null)
                return;

            ObjectGuid korvasGuid = korvasObject.GetGUID();

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Unit privateObjectOwner = ObjAccessor.GetUnit(me, me.GetPrivateObjectOwner());
                if (privateObjectOwner == null)
                    return;

                Unit korvas = ObjAccessor.GetUnit(me, korvasGuid);
                if (korvas == null)
                    return;

                Talk(AshtongueIntroData.SayKaynActivateGateway, me);
                me.CastSpell(privateObjectOwner, TheInvasionBeginsConst.SpellTrackTargetInChannel, false);
                korvas.CastSpell(privateObjectOwner, TheInvasionBeginsConst.SpellTrackTargetInChannel, false);

                task.Schedule(TimeSpan.FromSeconds(6), task =>
                {
                    Talk(AshtongueIntroData.SayKaynCutAHole, me);

                    task.Schedule(TimeSpan.FromSeconds(6), task =>
                    {
                        Creature korvas = ObjectAccessor.GetCreature(me, korvasGuid);
                        if (korvas == null)
                            return;

                        if (!korvas.IsAIEnabled())
                            return;

                        korvas.GetAI().Talk(AshtongueIntroData.SayKorvasSlayMoreDemons, me);
                        me.InterruptNonMeleeSpells(true);
                        me.GetMotionMaster().MovePath(AshtongueIntroData.PathKaynSunfuryNearTeleport, false);
                        me.SetAIAnimKitId(TheInvasionBeginsConst.AnimDhRun);
                        me.DespawnOrUnsummon(TimeSpan.FromSeconds(10));

                        task.Schedule(TimeSpan.FromSeconds(2), _ =>
                        {
                            Creature korvas = ObjectAccessor.GetCreature(me, korvasGuid);
                            if (korvas == null)
                                return;

                            korvas.InterruptNonMeleeSpells(true);
                            korvas.GetMotionMaster().MovePath(AshtongueIntroData.PathKorvasBloodthornNearTeleport, false);
                            korvas.SetAIAnimKitId(TheInvasionBeginsConst.AnimDhRun);
                            korvas.DespawnOrUnsummon(TimeSpan.FromSeconds(12));
                        });
                    });
                });
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script] // 1053 - Enter the Illidari: Ashtongue
    class scene_enter_the_illidari_ashtongue : SceneScript
    {
        public scene_enter_the_illidari_ashtongue() : base("scene_enter_the_illidari_ashtongue") { }

        public override void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Creature sevisObject = player.FindNearestCreatureWithOptions(30.0f, new() { CreatureId = AshtongueIntroData.NpcSevisBrightflameAshtongue, IgnorePhases = true });
            if (sevisObject == null)
                return;

            TempSummon sevisClone = sevisObject.SummonPersonalClone(sevisObject.GetPosition(), TempSummonType.ManualDespawn, TimeSpan.Zero, 0, 0, player);
            if (sevisClone == null)
                return;

            sevisClone.CastSpell(player, TheInvasionBeginsConst.SpellTrackTargetInChannel, false);
            sevisClone.DespawnOrUnsummon(TimeSpan.FromSeconds(15));
        }

        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            if (triggerName == "SEEFELSABERCREDIT")
                player.CastSpell(player, AshtongueIntroData.SpellAshtongueFellsaberKillCredit, true);
            else if (triggerName == "UPDATEPHASE")
                PhasingHandler.OnConditionChange(player);
        }
    }

    [Script] // 99916 - Sevis Brightflame (Ashtongue Gateway)
    class npc_sevis_brightflame_ashtongue_gateway_private : ScriptedAI
    {
        public npc_sevis_brightflame_ashtongue_gateway_private(Creature creature) : base(creature) { }

        public override void JustAppeared()
        {
            if (!me.IsPrivateObject())
                return;

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Talk(AshtongueIntroData.SaySevisSayFindAllari, me);

                task.Schedule(TimeSpan.FromSeconds(2), task =>
                {
                    me.SendPlaySpellVisualKit(AshtongueIntroData.SpellVisualKitSevisMount, 0, 0);
                    me.SetMountDisplayId(AshtongueIntroData.DisplayIdSevisMount);

                    task.Schedule(TimeSpan.FromSeconds(3), _ =>
                    {
                        me.InterruptNonMeleeSpells(true);
                        me.GetMotionMaster().MovePath(AshtongueIntroData.PathSevisBrightflameGateway, false);
                    });
                });
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script] // 200255 - Accepting Felsaber Gift
    class spell_accepting_felsaber_gift : SpellScript
    {
        void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(null, AshtongueIntroData.SpellCastMountDhFelsaber, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHitTarget, 3, SpellEffectName.Dummy));
        }
    }

    [Script] // 32 - Mardum - Trigger KillCredit for Quest "Enter the Illidari: Ashtongue"
    class at_enter_the_illidari_ashtongue_allari_killcredit : AreaTriggerAI
    {
        public at_enter_the_illidari_ashtongue_allari_killcredit(AreaTrigger areatrigger) : base(areatrigger) { }

        public override void OnUnitEnter(Unit unit)
        {
            Player player = unit.ToPlayer();
            if (player == null || player.GetQuestStatus(AshtongueIntroData.QuestEnterTheIllidariAshtongue) != QuestStatus.Incomplete)
                return;

            player.KilledMonsterCredit(AshtongueIntroData.NpcAllariSouleaterAshtongue);
        }
    }
}
