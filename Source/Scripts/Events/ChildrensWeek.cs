// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.Events.ChildrensWeek
{
    struct TextIds
    {
        public const uint OracleOrphan1 = 1;
        public const uint OracleOrphan2 = 2;
        public const uint OracleOrphan3 = 3;
        public const uint OracleOrphan4 = 4;
        public const uint OracleOrphan5 = 5;
        public const uint OracleOrphan6 = 6;
        public const uint OracleOrphan7 = 7;
        public const uint OracleOrphan8 = 8;
        public const uint OracleOrphan9 = 9;
        public const uint OracleOrphan10 = 10;
        public const uint OracleOrphan11 = 11;
        public const uint OracleOrphan12 = 12;
        public const uint OracleOrphan13 = 13;
        public const uint OracleOrphan14 = 14;

        public const uint WolvarOrphan1 = 1;
        public const uint WolvarOrphan2 = 2;
        public const uint WolvarOrphan3 = 3;
        public const uint WolvarOrphan4 = 4;
        public const uint WolvarOrphan5 = 5;
        // 6 - 9 used in Nesingwary script
        public const uint WolvarOrphan10 = 10;
        public const uint WolvarOrphan11 = 11;
        public const uint WolvarOrphan12 = 12;
        public const uint WolvarOrphan13 = 13;

        public const uint WinterfinPlaymate1 = 1;
        public const uint WinterfinPlaymate2 = 2;

        public const uint SnowfallGladePlaymate1 = 1;
        public const uint SnowfallGladePlaymate2 = 2;

        public const uint SooRoo1 = 1;
        public const uint ElderKekek1 = 1;

        public const uint Alexstrasza2 = 2;
        public const uint Krasus8 = 8;
    }

    struct QuestIds
    {
        public const uint PlaymateWolvar = 13951;
        public const uint PlaymateOracle = 13950;
        public const uint TheBiggestTreeEver = 13929;
        public const uint TheBronzeDragonshrineOracle = 13933;
        public const uint TheBronzeDragonshrineWolvar = 13934;
        public const uint MeetingAGreatOne = 13956;
        public const uint TheMightyHemetNesingwary = 13957;
        public const uint DownAtTheDocks = 910;
        public const uint GatewayToTheFrontier = 911;
        public const uint BoughtOfEternals = 1479;
        public const uint SpookyLighthouse = 1687;
        public const uint StonewroughtDam = 1558;
        public const uint DarkPortalH = 10951;
        public const uint DarkPortalA = 10952;
        public const uint LordaeronThroneRoom = 1800;
        public const uint AuchindounAndTheRing = 10950;
        public const uint TimeToVisitTheCavernsH = 10963;
        public const uint TimeToVisitTheCavernsA = 10962;
        public const uint TheSeatOfTheNaruu = 10956;
        public const uint CallOnTheFarseer = 10968;
        public const uint JheelIsAtAerisLanding = 10954;
        public const uint HchuuAndTheMushroomPeople = 10945;
        public const uint VisitTheThroneOfElements = 10953;
        public const uint NowWhenIGrowUp = 11975;
        public const uint HomeOfTheBearMen = 13930;
        public const uint TheDragonQueenOracle = 13954;
        public const uint TheDragonQueenWolvar = 13955;
    }

    struct AreatriggerIds
    {
        public const uint DownAtTheDocks = 3551;
        public const uint GatewayToTheFrontier = 3549;
        public const uint LordaeronThroneRoom = 3547;
        public const uint BoughtOfEternals = 3546;
        public const uint SpookyLighthouse = 3552;
        public const uint StonewroughtDam = 3548;
        public const uint DarkPortal = 4356;
    }

    struct CreatureIds
    {
        public const uint OrphanOracle = 33533;
        public const uint OrphanWolvar = 33532;
        public const uint OrphanBloodElf = 22817;
        public const uint OrphanDraenei = 22818;
        public const uint OrphanHuman = 14305;
        public const uint OrphanOrcish = 14444;

        public const uint CavernsOfTimeCwTrigger = 22872;
        public const uint Exodar01CwTrigger = 22851;
        public const uint Exodar02CwTrigger = 22905;
        public const uint AerisLandingCwTrigger = 22838;
        public const uint AuchindounCwTrigger = 22831;
        public const uint SporeggarCwTrigger = 22829;
        public const uint ThroneOfElementsCwTrigger = 22839;
        public const uint Silvermoon01CwTrigger = 22866;
        public const uint Krasus = 27990;
    }

    struct Misc
    {
        public const uint SpellSnowball = 21343;
        public const uint SpellOrphanOut = 58818;

        public const uint DisplayInvisible = 11686;

        public static ObjectGuid GetOrphanGUID(Player player, uint orphan)
        {
            Aura orphanOut = player.GetAura(SpellOrphanOut);
            if (orphanOut != null)
                if (orphanOut.GetCaster() != null && orphanOut.GetCaster().GetEntry() == orphan)
                    return orphanOut.GetCaster().GetGUID();

            return ObjectGuid.Empty;
        }
    }

    [Script]
    class npc_winterfin_playmate : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_winterfin_playmate(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.PlaymateOracle) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanOracle);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.OracleOrphan1);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(3), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            Talk(TextIds.WinterfinPlaymate1);
                            me.HandleEmoteCommand(Emote.StateDance);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(6), _ => orphan.GetAI().Talk(TextIds.OracleOrphan2));

                        _scheduler.Schedule(TimeSpan.FromSeconds(9), _ => Talk(TextIds.WinterfinPlaymate2));

                        _scheduler.Schedule(TimeSpan.FromSeconds(14), _ =>
                        {
                            orphan.GetAI().Talk(TextIds.OracleOrphan3);
                            me.HandleEmoteCommand(Emote.StateNone);
                            player.GroupEventHappens(QuestIds.PlaymateOracle, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_snowfall_glade_playmate : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_snowfall_glade_playmate(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.PlaymateWolvar) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanWolvar);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.WolvarOrphan1);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(5), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            Talk(TextIds.SnowfallGladePlaymate1);
                            DoCast(orphan, Misc.SpellSnowball);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(10), _ => Talk(TextIds.SnowfallGladePlaymate2));

                        _scheduler.Schedule(TimeSpan.FromSeconds(15), _ =>
                        {
                            orphan.GetAI().Talk(TextIds.WolvarOrphan2);
                            orphan.CastSpell(me, Misc.SpellSnowball);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(20), _ =>
                        {
                            orphan.GetAI().Talk(TextIds.WolvarOrphan3);
                            player.GroupEventHappens(QuestIds.PlaymateWolvar, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_the_biggest_tree : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_the_biggest_tree(Creature creature) : base(creature)
        {
            Initialize();
            me.SetDisplayId(Misc.DisplayInvisible);
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.TheBiggestTreeEver) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanOracle);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ => orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ()));

                        _scheduler.Schedule(TimeSpan.FromSeconds(2), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            orphan.GetAI().Talk(TextIds.OracleOrphan4);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(7), _ =>
                        {
                            player.GroupEventHappens(QuestIds.TheBiggestTreeEver, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_high_oracle_soo_roo : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_high_oracle_soo_roo(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.TheBronzeDragonshrineOracle) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanOracle);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.OracleOrphan5);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(3), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            Talk(TextIds.SooRoo1);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(9), _ =>
                        {
                            orphan.GetAI().Talk(TextIds.OracleOrphan6);
                            player.GroupEventHappens(QuestIds.TheBronzeDragonshrineOracle, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_elder_kekek : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_elder_kekek(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.TheBronzeDragonshrineWolvar) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanWolvar);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.WolvarOrphan4);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(3), _ => Talk(TextIds.ElderKekek1));

                        _scheduler.Schedule(TimeSpan.FromSeconds(9), _ =>
                        {
                            orphan.GetAI().Talk(TextIds.WolvarOrphan5);
                            player.GroupEventHappens(QuestIds.TheBronzeDragonshrineWolvar, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_the_etymidian : ScriptedAI
    {
        const uint SayActivation = 0;
        const uint QuestTheActivationRune = 12547;

        bool working;
        ObjectGuid orphanGUID;

        public npc_the_etymidian(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
        {
            if (quest.Id != QuestTheActivationRune)
                return;

            Talk(SayActivation);
        }

        // doesn't trigger if creature is stunned. Restore aura 25900 when it will be possible or
        // find another way to start event(from orphan script)
        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.MeetingAGreatOne) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanOracle);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.OracleOrphan7);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(5), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            orphan.GetAI().Talk(TextIds.OracleOrphan8);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(10), _ => orphan.GetAI().Talk(TextIds.OracleOrphan9));

                        _scheduler.Schedule(TimeSpan.FromSeconds(15), _ => orphan.GetAI().Talk(TextIds.OracleOrphan10));

                        _scheduler.Schedule(TimeSpan.FromSeconds(20), _ =>
                        {
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            player.GroupEventHappens(QuestIds.MeetingAGreatOne, me);
                            Reset();
                        });
                    }
                }

            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class npc_alexstraza_the_lifebinder : ScriptedAI
    {
        bool working;
        ObjectGuid orphanGUID;

        public npc_alexstraza_the_lifebinder(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            working = false;
            orphanGUID.Clear();
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void SetData(uint type, uint data)
        {
            // Existing SmartAI
            if (type == 0)
            {
                switch (data)
                {
                    case 1:
                        me.SetOrientation(1.6049f);
                        break;
                    case 2:
                        me.SetOrientation(me.GetHomePosition().GetOrientation());
                        break;
                }
            }
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!working && who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player == null)
                {
                    Reset();
                    return;
                }

                if (player.GetQuestStatus(QuestIds.TheDragonQueenOracle) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanOracle);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.OracleOrphan11);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(5), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            orphan.GetAI().Talk(TextIds.OracleOrphan12);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(10), _ => orphan.GetAI().Talk(TextIds.OracleOrphan13));

                        _scheduler.Schedule(TimeSpan.FromSeconds(15), _ =>
                        {
                            Talk(TextIds.Alexstrasza2);
                            me.SetStandState(UnitStandStateType.Kneel);
                            me.SetFacingToObject(orphan);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(20), _ => orphan.GetAI().Talk(TextIds.OracleOrphan14));

                        _scheduler.Schedule(TimeSpan.FromSeconds(25), _ =>
                        {
                            me.SetStandState(UnitStandStateType.Stand);
                            me.SetOrientation(me.GetHomePosition().GetOrientation());
                            player.GroupEventHappens(QuestIds.TheDragonQueenOracle, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                            return;
                        });
                    }
                }
                else if (player.GetQuestStatus(QuestIds.TheDragonQueenWolvar) == QuestStatus.Incomplete)
                {
                    orphanGUID = Misc.GetOrphanGUID(player, CreatureIds.OrphanWolvar);
                    if (!orphanGUID.IsEmpty())
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, orphanGUID);
                        if (orphan == null)
                        {
                            Reset();
                            return;
                        }

                        working = true;

                        _scheduler.Schedule(TimeSpan.FromSeconds(0), _ =>
                        {
                            orphan.GetMotionMaster().MovePoint(0, me.GetPositionX() + MathF.Cos(me.GetOrientation()) * 5, me.GetPositionY() + MathF.Sin(me.GetOrientation()) * 5, me.GetPositionZ());
                            orphan.GetAI().Talk(TextIds.WolvarOrphan11);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(5), _ =>
                        {
                            Creature krasus = me.FindNearestCreature(CreatureIds.Krasus, 10.0f);
                            if (krasus != null)
                            {
                                orphan.SetFacingToObject(krasus);
                                krasus.GetAI().Talk(TextIds.Krasus8);
                            }
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(10), _ => orphan.GetAI().Talk(TextIds.WolvarOrphan12));

                        _scheduler.Schedule(TimeSpan.FromSeconds(15), _ =>
                        {
                            orphan.SetFacingToObject(me);
                            Talk(TextIds.Alexstrasza2);
                        });

                        _scheduler.Schedule(TimeSpan.FromSeconds(20), _ => orphan.GetAI().Talk(TextIds.WolvarOrphan13));

                        _scheduler.Schedule(TimeSpan.FromSeconds(25), _ =>
                        {
                            player.GroupEventHappens(QuestIds.TheDragonQueenWolvar, me);
                            orphan.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            Reset();
                            return;
                        });
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class at_bring_your_orphan_to : AreaTriggerScript
    {
        public at_bring_your_orphan_to() : base("at_bring_your_orphan_to") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger)
        {
            if (player.IsDead() || !player.HasAura(Misc.SpellOrphanOut))
                return false;

            uint questId = 0;
            uint orphanId = 0;

            switch (trigger.Id)
            {
                case AreatriggerIds.DownAtTheDocks:
                    questId = QuestIds.DownAtTheDocks;
                    orphanId = CreatureIds.OrphanOrcish;
                    break;
                case AreatriggerIds.GatewayToTheFrontier:
                    questId = QuestIds.GatewayToTheFrontier;
                    orphanId = CreatureIds.OrphanOrcish;
                    break;
                case AreatriggerIds.LordaeronThroneRoom:
                    questId = QuestIds.LordaeronThroneRoom;
                    orphanId = CreatureIds.OrphanOrcish;
                    break;
                case AreatriggerIds.BoughtOfEternals:
                    questId = QuestIds.BoughtOfEternals;
                    orphanId = CreatureIds.OrphanHuman;
                    break;
                case AreatriggerIds.SpookyLighthouse:
                    questId = QuestIds.SpookyLighthouse;
                    orphanId = CreatureIds.OrphanHuman;
                    break;
                case AreatriggerIds.StonewroughtDam:
                    questId = QuestIds.StonewroughtDam;
                    orphanId = CreatureIds.OrphanHuman;
                    break;
                case AreatriggerIds.DarkPortal:
                    questId = player.GetTeam() == Team.Alliance ? QuestIds.DarkPortalA : QuestIds.DarkPortalH;
                    orphanId = player.GetTeam() == Team.Alliance ? CreatureIds.OrphanDraenei : CreatureIds.OrphanBloodElf;
                    break;
            }

            if (questId != 0 && orphanId != 0 && !Misc.GetOrphanGUID(player, orphanId).IsEmpty() && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                player.AreaExploredOrEventHappens(questId);

            return true;
        }
    }

    class npc_cw_area_trigger : ScriptedAI
    {
        public npc_cw_area_trigger(Creature creature) : base(creature)
        {
            me.SetDisplayId(Misc.DisplayInvisible);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who != null && me.GetDistance2d(who) < 20.0f)
            {
                Player player = who.ToPlayer();
                if (player != null && player.HasAura(Misc.SpellOrphanOut))
                {
                    uint questId = 0;
                    uint orphanId = 0;
                    switch (me.GetEntry())
                    {
                        case CreatureIds.CavernsOfTimeCwTrigger:
                            questId = player.GetTeam() == Team.Alliance ? QuestIds.TimeToVisitTheCavernsA : QuestIds.TimeToVisitTheCavernsH;
                            orphanId = player.GetTeam() == Team.Alliance ? CreatureIds.OrphanDraenei : CreatureIds.OrphanBloodElf;
                            break;
                        case CreatureIds.Exodar01CwTrigger:
                            questId = QuestIds.TheSeatOfTheNaruu;
                            orphanId = CreatureIds.OrphanDraenei;
                            break;
                        case CreatureIds.Exodar02CwTrigger:
                            questId = QuestIds.CallOnTheFarseer;
                            orphanId = CreatureIds.OrphanDraenei;
                            break;
                        case CreatureIds.AerisLandingCwTrigger:
                            questId = QuestIds.JheelIsAtAerisLanding;
                            orphanId = CreatureIds.OrphanDraenei;
                            break;
                        case CreatureIds.AuchindounCwTrigger:
                            questId = QuestIds.AuchindounAndTheRing;
                            orphanId = CreatureIds.OrphanDraenei;
                            break;
                        case CreatureIds.SporeggarCwTrigger:
                            questId = QuestIds.HchuuAndTheMushroomPeople;
                            orphanId = CreatureIds.OrphanBloodElf;
                            break;
                        case CreatureIds.ThroneOfElementsCwTrigger:
                            questId = QuestIds.VisitTheThroneOfElements;
                            orphanId = CreatureIds.OrphanBloodElf;
                            break;
                        case CreatureIds.Silvermoon01CwTrigger:
                            if (player.GetQuestStatus(QuestIds.NowWhenIGrowUp) == QuestStatus.Incomplete && !Misc.GetOrphanGUID(player, CreatureIds.OrphanBloodElf).IsEmpty())
                            {
                                player.AreaExploredOrEventHappens(QuestIds.NowWhenIGrowUp);
                                if (player.GetQuestStatus(QuestIds.NowWhenIGrowUp) == QuestStatus.Complete)
                                {
                                    Creature samuro = me.FindNearestCreature(25151, 20.0f);
                                    if (samuro != null)
                                        samuro.HandleEmoteCommand(RandomHelper.RAND(Emote.OneshotWave, Emote.OneshotRoar, Emote.OneshotFlex, Emote.OneshotSalute, Emote.OneshotDance));
                                }
                            }
                            break;
                    }
                    if (questId != 0 && orphanId != 0 && !Misc.GetOrphanGUID(player, orphanId).IsEmpty() && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                        player.AreaExploredOrEventHappens(questId);
                }

            }
        }
    }

    class npc_grizzlemaw_cw_trigger : ScriptedAI
    {
        public npc_grizzlemaw_cw_trigger(Creature creature) : base(creature)
        {
            me.SetDisplayId(Misc.DisplayInvisible);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who != null && who.GetDistance2d(me) < 10.0f)
            {
                Player player = who.ToPlayer();
                if (player != null)
                {
                    if (player.GetQuestStatus(QuestIds.HomeOfTheBearMen) == QuestStatus.Incomplete)
                    {
                        Creature orphan = ObjectAccessor.GetCreature(me, Misc.GetOrphanGUID(player, CreatureIds.OrphanWolvar));
                        if (orphan != null)
                        {
                            player.AreaExploredOrEventHappens(QuestIds.HomeOfTheBearMen);
                            orphan.GetAI().Talk(TextIds.WolvarOrphan10);
                        }
                    }
                }
            }
        }
    }
}