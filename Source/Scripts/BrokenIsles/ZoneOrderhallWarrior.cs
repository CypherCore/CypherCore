// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Scripts.BrokenIsles.ZoneOrderhallWarrior
{
    struct SpellIds
    {
        public const uint EmoteBelch = 65937;
        public const uint WarriorOrderFormationScene = 193709;
        public const uint CancelCompleteSceneWarriorOrderFormation = 193711;
    }

    struct PhaseIds
    {
        public const uint Odyn = 5107;
        public const uint Danica = 5090;
    }

    struct QuestIds
    {
        public const uint OdynAndTheValarjar = 39654;
    }

    struct CreatureIds
    {
        public const uint KillCreditFollowedDanica = 103036;
        public const uint DanicaTheReclaimer = 93823;
        public const uint KillCreditArrivedAtOdyn = 96532;
    }

    struct ItemIds
    {
        public const uint MonsterItemMuttonWithBite = 2202;
        public const uint MonsterItemTankardWooden = 2703;
        public const uint Hov2HAxe = 137176;
        public const uint Hov1HSword = 137263;
        public const uint HovShield2 = 137265;
    }

    struct EmoteIds
    {
        public static Emote[] PayingRespectToOdynRandomEmotes = { Emote.OneshotPoint, Emote.OneshotTalk, Emote.OneshotNo };
    }

    [Script]
    class npc_danica_the_reclaimer : ScriptedAI
    {
        ObjectGuid _summonerGuid;

        Vector3[] DanicaPath01 =
        {
            new Vector3(1050.219f, 7232.470f, 100.5846f),
            new Vector3(1046.207f, 7240.372f, 100.5846f),
            new Vector3(1040.963f, 7245.498f, 100.6819f),
            new Vector3(1034.726f, 7250.083f, 100.5846f),
            new Vector3(1027.422f, 7257.835f, 100.5846f),
            new Vector3(1027.542f, 7259.735f, 100.5846f)
        };

        Vector3[] DanicaPath02 =
        {
            new Vector3(1018.493f, 7247.438f, 100.5846f),
            new Vector3(1013.535f, 7243.327f, 100.5846f),
            new Vector3(1007.063f, 7235.723f, 100.5846f),
            new Vector3(1003.337f, 7229.650f, 100.5846f),
            new Vector3(995.4549f, 7227.286f, 100.5846f),
            new Vector3(984.4410f, 7224.357f, 100.5846f)
        };

        Vector3[] DanicaPath03 =
        {
            new Vector3(962.5208f, 7223.089f, 100.5846f),
            new Vector3(934.2795f, 7223.116f, 100.5846f),
            new Vector3(911.8507f, 7223.776f, 100.5846f),
            new Vector3(879.0139f, 7224.100f, 100.9079f),
            new Vector3(851.691f, 7224.5490f, 109.5846f)
        };

        public npc_danica_the_reclaimer(Creature creature) : base(creature) { }

        // Should be the player
        // Personal spawn ? Demon Creator is the player who accepts the quest, no phasing involved but the quest giver dissapears and gets replaced with a new one
        public override void IsSummonedBy(WorldObject summoner)
        {
            if (!summoner.IsPlayer())
                return;

            me.RemoveNpcFlag(NPCFlags.Gossip);
            _summonerGuid = summoner.GetGUID();
            _scheduler.Schedule(TimeSpan.FromSeconds(2), context =>
            {
                me.GetMotionMaster().MoveSmoothPath(0, DanicaPath01, DanicaPath01.Length, false, true);
                Talk(1, summoner);
            });
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    _scheduler.Schedule(TimeSpan.FromSeconds(10), context =>
                    {
                        Player player = Global.ObjAccessor.FindConnectedPlayer(_summonerGuid);
                        me.GetMotionMaster().MoveSmoothPath(1, DanicaPath02, DanicaPath02.Length, false, true);
                        Talk(2, player);
                    });
                    break;
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(10), context =>
                    {
                        Player player = Global.ObjAccessor.FindConnectedPlayer(_summonerGuid);
                        me.GetMotionMaster().MoveSmoothPath(2, DanicaPath03, DanicaPath03.Length, false, true);
                        Talk(3, player);

                        if (player)
                            player.KilledMonsterCredit(CreatureIds.KillCreditFollowedDanica);
                    });
                    break;
                case 2:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        // Should be some other way to do this...
        public override void OnQuestAccept(Player player, Quest quest)
        {
            TempSummon summon = player.SummonCreature(CreatureIds.DanicaTheReclaimer, 1059.613f, 7224.605f, 100.4608f, 0.03462749f, TempSummonType.ManualDespawn, TimeSpan.FromSeconds(0), player.GetGUID());
            if (summon == null)
                return;

            summon.SetDemonCreatorGUID(player.GetGUID());
        }
    }

    [Script]
    class npc_feasting_valarjar : ScriptedAI
    {
        List<Emote> _randomEmotes = new();

        public npc_feasting_valarjar(Creature creature) : base(creature)
        {
            _randomEmotes.AddRange(new[] { Emote.OneshotEatNoSheathe, Emote.OneshotLaughNoSheathe, Emote.OneshotRoar, Emote.OneshotLaugh, Emote.OneshotPoint, Emote.OneshotTalk, Emote.OneshotCheerNoSheathe });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), context =>
            {
                Emote emoteID = _randomEmotes.SelectRandom();
                if (emoteID == Emote.OneshotEatNoSheathe)
                {
                    me.SetVirtualItem(0, RandomHelper.URand(0, 1) == 1 ? ItemIds.MonsterItemMuttonWithBite : ItemIds.MonsterItemTankardWooden);
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), context =>
                    {
                        me.SetVirtualItem(0, 0);
                        if (RandomHelper.randChance(85))
                            DoCastSelf(SpellIds.EmoteBelch);
                    });
                }

                me.HandleEmoteCommand(emoteID);
                context.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20), context =>
            {
                float direction = me.GetOrientation() + MathF.PI;
                me.GetMotionMaster().MovePoint(0, me.GetFirstCollisionPosition(5.0f, direction));
            });
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_incoming_valarjar_aspirant_1 : ScriptedAI
    {
        Vector3[] IncommingValarjarAspirantPath01 =
        {
            new Vector3(876.7396f, 7220.805f, 98.91662f),
            new Vector3(870.6129f, 7220.945f, 101.8951f),
            new Vector3(865.0677f, 7220.975f, 103.7846f),
            new Vector3(854.6389f, 7221.030f, 106.7846f),
            new Vector3(851.1597f, 7220.292f, 106.7846f),
            new Vector3(848.0573f, 7216.386f, 106.7846f),
            new Vector3(844.7570f, 7210.920f, 106.7846f),
            new Vector3(841.9844f, 7207.228f, 106.7846f),
            new Vector3(839.2396f, 7203.619f, 107.5846f),
            new Vector3(836.4844f, 7200.202f, 107.5846f),
            new Vector3(834.2430f, 7196.000f, 107.5846f)
        };

        Vector3[] IncommingValarjarAspirantPath02 =
        {
            new Vector3(828.5851f, 7204.096f, 106.7846f),
            new Vector3(819.4636f, 7212.124f, 106.7846f),
            new Vector3(814.2853f, 7215.074f, 106.7846f),
            new Vector3(809.4948f, 7217.543f, 106.7846f),
            new Vector3(806.0313f, 7219.614f, 106.7846f)
        };

        Vector3[] IncommingValarjarAspirantPath03 =
        {
            new Vector3(824.1597f, 7221.822f, 106.7846f),
            new Vector3(831.7500f, 7221.092f, 106.7846f),
            new Vector3(842.4236f, 7222.208f, 106.7846f),
            new Vector3(853.5781f, 7222.473f, 106.7846f),
            new Vector3(863.9618f, 7223.012f, 103.7846f),
            new Vector3(867.9358f, 7223.165f, 103.3735f),
            new Vector3(880.6215f, 7222.569f, 97.78457f),
            new Vector3(887.8438f, 7221.310f, 97.78457f),
            new Vector3(903.7118f, 7215.743f, 97.78458f)
        };

        public npc_incoming_valarjar_aspirant_1(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.GetMotionMaster().MoveSmoothPath(0, IncommingValarjarAspirantPath01, IncommingValarjarAspirantPath01.Length, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), context =>
                    {
                        me.HandleEmoteCommand(EmoteIds.PayingRespectToOdynRandomEmotes.SelectRandom());
                    });

                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15), context =>
                    {
                        me.GetMotionMaster().MoveSmoothPath(1, IncommingValarjarAspirantPath02, IncommingValarjarAspirantPath02.Length, true);
                    });
                    break;
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), context =>
                    {
                        me.PlayOneShotAnimKitId(1431);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10), context =>
                        {
                            me.GetMotionMaster().MoveSmoothPath(2, IncommingValarjarAspirantPath03, IncommingValarjarAspirantPath03.Length);
                        });
                    });
                    break;
                case 2:
                    me.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_incoming_valarjar_aspirant_2 : ScriptedAI
    {
        Vector3[] IncommingValarjarAspirantPath01 =
        {
            new Vector3(890.5521f, 7235.710f, 97.78457f),
            new Vector3(883.8073f, 7233.402f, 97.78457f),
            new Vector3(872.1979f, 7234.018f, 101.2886f),
            new Vector3(863.5941f, 7234.594f, 103.7846f),
            new Vector3(855.2899f, 7235.626f, 106.7593f),
            new Vector3(849.8177f, 7236.571f, 106.7846f),
            new Vector3(845.7830f, 7241.082f, 106.7846f),
            new Vector3(841.8489f, 7246.654f, 106.7846f),
            new Vector3(839.7205f, 7250.986f, 106.7846f),
            new Vector3(840.8889f, 7254.773f, 107.5846f),
            new Vector3(841.9254f, 7259.517f, 107.5846f),
            new Vector3(840.6077f, 7266.662f, 107.5846f)
        };

        Vector3[] IncommingValarjarAspirantPath02 =
        {
            new Vector3(838.1493f, 7260.027f, 107.5846f),
            new Vector3(832.2691f, 7253.756f, 106.7846f),
            new Vector3(823.1996f, 7246.677f, 106.7846f),
            new Vector3(821.2500f, 7244.573f, 106.7846f),
            new Vector3(815.8906f, 7241.437f, 106.7846f),
            new Vector3(809.8281f, 7239.580f, 106.7846f)
        };

        Vector3[] IncommingValarjarAspirantPath03 =
        {
            new Vector3(827.4757f, 7236.593f, 106.7846f),
            new Vector3(837.1840f, 7236.047f, 106.7846f),
            new Vector3(847.1684f, 7235.377f, 106.7846f),
            new Vector3(854.7185f, 7235.294f, 106.7846f),
            new Vector3(862.3524f, 7234.287f, 104.4290f),
            new Vector3(882.3489f, 7233.743f, 97.78457f),
            new Vector3(894.3768f, 7233.098f, 97.78457f),
            new Vector3(906.0660f, 7232.520f, 97.78458f),
            new Vector3(915.0070f, 7233.368f, 97.78458f),
            new Vector3(924.6910f, 7233.694f, 97.78458f)
        };

        public npc_incoming_valarjar_aspirant_2(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.GetMotionMaster().MoveSmoothPath(0, IncommingValarjarAspirantPath01, IncommingValarjarAspirantPath01.Length, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), context =>
                    {
                        me.HandleEmoteCommand(EmoteIds.PayingRespectToOdynRandomEmotes.SelectRandom());
                    });

                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15), context =>
                    {
                        me.GetMotionMaster().MoveSmoothPath(1, IncommingValarjarAspirantPath02, IncommingValarjarAspirantPath02.Length, true);
                    });
                    break;
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), context =>
                    {
                        me.PlayOneShotAnimKitId(1431);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10), context =>
                        {
                            me.GetMotionMaster().MoveSmoothPath(2, IncommingValarjarAspirantPath03, IncommingValarjarAspirantPath03.Length);
                        });
                    });
                    break;
                case 2:
                    me.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_leaving_valarjar_1 : ScriptedAI
    {
        Vector3[] PathLeavingValarjar01 =
        {
            new Vector3(802.5903f, 7304.605f, 106.7846f),
            new Vector3(809.3333f, 7296.529f, 106.7846f),
            new Vector3(811.8004f, 7293.676f, 106.7846f),
            new Vector3(817.4219f, 7287.498f, 106.7846f),
            new Vector3(821.0313f, 7283.637f, 106.7846f),
            new Vector3(822.1111f, 7275.672f, 106.7846f),
            new Vector3(826.4662f, 7270.601f, 107.5846f),
            new Vector3(830.8212f, 7268.729f, 107.5846f)
        };

        Vector3[] PathLeavingValarjar02 =
        {
            new Vector3(824.9757f, 7261.047f, 107.5846f),
            new Vector3(822.0989f, 7256.705f, 106.7846f),
            new Vector3(819.0261f, 7253.674f, 106.7846f),
            new Vector3(813.1910f, 7249.034f, 106.7846f),
            new Vector3(809.1493f, 7245.616f, 106.7846f),
            new Vector3(806.3559f, 7243.057f, 106.7846f)
        };

        Vector3[] PathLeavingValarjar03 =
        {
            new Vector3(825.3177f, 7244.253f, 106.7846f),
            new Vector3(837.5816f, 7243.241f, 106.7846f),
            new Vector3(845.0243f, 7240.063f, 106.7846f),
            new Vector3(853.7274f, 7238.423f, 106.7953f),
            new Vector3(862.9948f, 7238.000f, 103.9737f),
            new Vector3(872.7899f, 7236.939f, 100.8285f),
            new Vector3(882.8333f, 7235.922f, 97.78457f),
            new Vector3(897.2813f, 7235.469f, 97.78457f),
            new Vector3(908.8090f, 7234.836f, 97.78458f),
            new Vector3(919.8750f, 7238.241f, 97.78458f)
        };

        public npc_leaving_valarjar_1(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.GetMotionMaster().MoveSmoothPath(0, PathLeavingValarjar01, PathLeavingValarjar01.Length, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), context =>
                    {
                        me.HandleEmoteCommand(EmoteIds.PayingRespectToOdynRandomEmotes.SelectRandom());
                    });

                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15), context =>
                    {
                        me.GetMotionMaster().MoveSmoothPath(1, PathLeavingValarjar02, PathLeavingValarjar02.Length, true);
                    });
                    break;
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), context =>
                    {
                        me.PlayOneShotAnimKitId(1431);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10), context =>
                        {
                            me.GetMotionMaster().MoveSmoothPath(2, PathLeavingValarjar03, PathLeavingValarjar03.Length);
                        });
                    });
                    break;
                case 2:
                    me.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_leaving_valarjar_2 : ScriptedAI
    {
        Vector3[] PathLeavingValarjar01 =
        {
            new Vector3(787.2361f, 7155.902f, 107.5846f),
            new Vector3(792.4844f, 7154.038f, 106.7846f),
            new Vector3(798.7830f, 7154.968f, 106.7846f),
            new Vector3(807.8160f, 7162.251f, 106.7846f),
            new Vector3(813.2882f, 7167.856f, 106.7846f),
            new Vector3(816.4913f, 7170.818f, 106.7846f),
            new Vector3(819.8299f, 7166.373f, 107.6281f)
        };

        Vector3[] PathLeavingValarjar02 =
        {
            new Vector3(818.2708f, 7175.469f, 106.7846f),
            new Vector3(819.5643f, 7185.691f, 106.7846f),
            new Vector3(818.4184f, 7193.082f, 106.7846f),
            new Vector3(818.8750f, 7199.256f, 106.7846f),
            new Vector3(815.2361f, 7203.648f, 106.7846f),
            new Vector3(809.6198f, 7208.319f, 106.7846f),
            new Vector3(804.2743f, 7215.379f, 106.7846f)
        };

        Vector3[] PathLeavingValarjar03 =
        {
            new Vector3(810.8403f, 7231.531f, 106.7846f),
            new Vector3(807.5087f, 7248.719f, 106.7846f),
            new Vector3(801.2587f, 7254.592f, 106.7846f),
            new Vector3(794.6649f, 7265.814f, 107.5846f),
            new Vector3(792.0191f, 7274.151f, 107.5846f),
            new Vector3(790.1823f, 7282.182f, 107.5846f)
        };

        public npc_leaving_valarjar_2(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.GetMotionMaster().MoveSmoothPath(0, PathLeavingValarjar01, PathLeavingValarjar01.Length, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), context =>
                    {
                        me.HandleEmoteCommand(EmoteIds.PayingRespectToOdynRandomEmotes.SelectRandom());
                    });

                    _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(15), context =>
                    {
                        me.GetMotionMaster().MoveSmoothPath(1, PathLeavingValarjar02, PathLeavingValarjar02.Length, true);
                    });
                    break;
                case 1:
                    _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), context =>
                    {
                        me.PlayOneShotAnimKitId(1431);
                        _scheduler.Schedule(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10), context =>
                        {
                            me.GetMotionMaster().MoveSmoothPath(2, PathLeavingValarjar03, PathLeavingValarjar03.Length);
                        });
                    });
                    break;
                case 2:
                    me.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_odyn : ScriptedAI
    {
        public npc_odyn(Creature creature) : base(creature) { }

        // Should be an better way of doing this...
        // What about a QuestScript with a hook "OnPlayerChangeArea" ? But The Great Mead Hall does not have a specific area...
        public override void MoveInLineOfSight(Unit who)
        {
            Player player = who.ToPlayer();
            if (player)
            {
                if (player.GetQuestStatus(QuestIds.OdynAndTheValarjar) == QuestStatus.Incomplete)
                {
                    if (player.IsInDist(me, 60.0f))
                    {
                        player.KilledMonsterCredit(CreatureIds.KillCreditArrivedAtOdyn); // SpellWarriorOrderFormationScene does not has this credit.
                        player.CastSpell(player, SpellIds.WarriorOrderFormationScene);
                    }
                }
            }
        }
    }

    [Script]
    class npc_spectating_valarjar : ScriptedAI
    {
        Emote[] _randomEmotes = { Emote.OneshotCheerNoSheathe, Emote.OneshotSalute, Emote.OneshotRoar, Emote.OneshotPoint, Emote.OneshotShout };

        public npc_spectating_valarjar(Creature creature) : base(creature) { }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), context =>
            {
                me.HandleEmoteCommand(_randomEmotes.SelectRandom());
                context.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20), context =>
            {
                float direction = me.GetOrientation() + MathF.PI;
                me.GetMotionMaster().MovePoint(0, me.GetFirstCollisionPosition(5.0f, direction));
            });
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 0:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }


    }

    [Script]
    class npc_valkyr_of_odyn_1 : ScriptedAI
    {
        public npc_valkyr_of_odyn_1(Creature creature) : base(creature) { }

        Vector3[] Path =
        {
            new Vector3(996.5347f, 7321.393f, 124.0931f),
            new Vector3(1009.880f, 7311.655f, 118.0898f),
            new Vector3(1024.688f, 7293.689f, 120.4009f),
            new Vector3(1038.288f, 7266.321f, 122.2708f),
            new Vector3(1049.439f, 7235.418f, 120.1065f),
            new Vector3(1067.825f, 7229.589f, 114.6320f),
            new Vector3(1082.800f, 7223.660f, 98.63562f)
        };

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_2 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(1113.635f, 7214.023f, 7.808200f),
            new Vector3(1110.443f, 7213.999f, 17.28479f),
            new Vector3(1108.583f, 7213.984f, 22.80371f),
            new Vector3(1103.488f, 7221.702f, 70.68047f),
            new Vector3(1101.911f, 7222.535f, 82.51234f),
            new Vector3(1098.861f, 7222.271f, 90.03111f),
            new Vector3(1095.129f, 7223.033f, 94.15130f),
            new Vector3(1089.240f, 7223.335f, 97.94925f),
            new Vector3(1077.932f, 7222.822f, 110.2143f),
            new Vector3(1068.802f, 7223.216f, 110.2143f),
            new Vector3(1045.356f, 7224.674f, 114.5371f),
            new Vector3(1023.946f, 7224.304f, 120.0150f),
            new Vector3(1002.535f, 7224.943f, 121.1011f),
            new Vector3(911.7552f, 7227.165f, 121.7384f),
            new Vector3(879.1285f, 7227.272f, 121.7384f),
            new Vector3(830.8785f, 7233.613f, 121.7384f),
            new Vector3(809.5052f, 7267.270f, 121.7384f),
            new Vector3(795.2899f, 7311.849f, 121.7384f)
        };

        public npc_valkyr_of_odyn_2(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_3 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(1133.929f, 7223.167f, 38.90330f),
            new Vector3(1124.510f, 7222.310f, 42.15336f),
            new Vector3(1119.903f, 7221.891f, 43.74335f),
            new Vector3(1103.934f, 7227.212f, 69.99904f),
            new Vector3(1097.554f, 7226.132f, 89.09371f),
            new Vector3(1092.602f, 7224.059f, 101.1545f),
            new Vector3(1078.701f, 7228.348f, 109.5599f),
            new Vector3(1068.967f, 7232.247f, 116.7876f),
            new Vector3(1053.540f, 7229.623f, 117.8927f),
            new Vector3(1044.104f, 7242.757f, 118.7891f),
            new Vector3(1031.111f, 7256.717f, 118.7891f),
            new Vector3(1029.684f, 7288.019f, 126.3048f),
            new Vector3(1029.889f, 7325.333f, 126.3061f),
            new Vector3(1039.043f, 7365.176f, 133.2310f)
        };

        public npc_valkyr_of_odyn_3(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_4 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(914.8663f, 7204.922f, 128.1687f),
            new Vector3(945.4445f, 7216.170f, 128.1687f),
            new Vector3(987.2483f, 7220.554f, 125.4318f),
            new Vector3(1015.882f, 7222.849f, 126.0546f),
            new Vector3(1053.023f, 7224.076f, 119.6729f),
            new Vector3(1071.891f, 7222.934f, 108.9545f),
            new Vector3(1081.530f, 7224.331f, 98.63076f)
        };
        
        public npc_valkyr_of_odyn_4(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_5 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(1038.141f, 7134.033f, 105.8965f),
            new Vector3(1033.373f, 7134.492f, 105.8965f),
            new Vector3(1027.882f, 7136.373f, 105.8965f),
            new Vector3(1026.943f, 7144.288f, 105.8965f),
            new Vector3(1027.608f, 7167.030f, 108.4167f),
            new Vector3(1027.767f, 7180.922f, 108.4167f),
            new Vector3(1028.484f, 7197.977f, 108.4167f),
            new Vector3(1034.113f, 7207.747f, 108.4167f),
            new Vector3(1041.977f, 7216.452f, 108.4167f),
            new Vector3(1054.269f, 7223.207f, 108.4167f),
            new Vector3(1075.891f, 7224.811f, 101.7954f),
            new Vector3(1082.438f, 7224.540f, 99.12900f)
        };
        
        public npc_valkyr_of_odyn_5(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_6 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(1112.011f, 7233.799f, 45.87240f),
            new Vector3(1107.887f, 7234.073f, 54.97818f),
            new Vector3(1106.264f, 7234.181f, 58.56218f),
            new Vector3(1099.969f, 7236.397f, 75.87664f),
            new Vector3(1096.552f, 7233.196f, 85.53920f),
            new Vector3(1095.531f, 7229.387f, 89.86687f),
            new Vector3(1092.981f, 7225.366f, 97.69602f),
            new Vector3(1082.800f, 7221.249f, 109.4660f),
            new Vector3(1070.983f, 7218.749f, 112.6827f),
            new Vector3(1057.455f, 7216.709f, 112.6827f),
            new Vector3(1051.859f, 7210.338f, 112.6827f),
            new Vector3(1042.427f, 7200.762f, 112.6827f),
            new Vector3(1032.616f, 7183.982f, 112.6827f),
            new Vector3(1027.792f, 7157.764f, 112.6827f),
            new Vector3(1026.870f, 7126.981f, 112.6827f),
            new Vector3(1053.083f, 7102.808f, 125.9283f),
            new Vector3(1055.122f, 7059.807f, 130.4395f)
        };
        
        public npc_valkyr_of_odyn_6(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_valkyr_of_odyn_7 : ScriptedAI
    {
        Vector3[] Path =
        {
            new Vector3(1064.076f, 7305.979f, 117.5428f),
            new Vector3(1058.290f, 7305.543f, 117.5428f),
            new Vector3(1046.578f, 7305.583f, 117.5428f),
            new Vector3(1034.373f, 7295.979f, 117.5428f),
            new Vector3(1026.639f, 7275.582f, 114.1900f),
            new Vector3(1030.729f, 7251.381f, 114.1900f),
            new Vector3(1040.950f, 7237.213f, 114.1900f),
            new Vector3(1057.274f, 7229.228f, 114.1900f),
            new Vector3(1070.297f, 7226.421f, 111.7502f),
            new Vector3(1082.146f, 7225.846f, 101.0798f)
        };

        public npc_valkyr_of_odyn_7(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetPositionZ() >= 100.0f)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context =>
                {
                    me.GetMotionMaster().MoveSmoothPath(2, Path, Path.Length, false, true);
                });
            }
            else
                me.GetMotionMaster().MoveSmoothPath(1, Path, Path.Length, false, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            switch (id)
            {
                case 2:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(250), context =>
                    {
                        /*
                         * (MovementMonsterSpline) (MovementSpline) MoveTime: 3111
                         * (MovementMonsterSpline) (MovementSpline) JumpGravity: 19.2911 -> +-Movement::gravity
                         * 1.4125f is guessed value. Which makes the JumpGravity way closer to the intended one. Not sure how to do it 100% blizzlike.
                         * Also the MoveTime is not correct but I don't know how to set it here.
                         */
                        me.GetMotionMaster().MoveJump(new Position(1107.84f, 7222.57f, 38.9725f, me.GetOrientation()), me.GetSpeed(UnitMoveType.Run), (float)(MotionMaster.gravity * 1.4125f), 3);
                    });
                    break;
                case 1:
                    me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
                    break;
                case 3:
                    me.DespawnOrUnsummon();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_weapon_inspector_valarjar : ScriptedAI
    {
        Tuple<uint, uint>[] _randomWeapons = { Tuple.Create(ItemIds.Hov2HAxe, 0u), Tuple.Create(ItemIds.Hov1HSword, ItemIds.HovShield2) };

        public npc_weapon_inspector_valarjar(Creature creature) : base(creature) { }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), context =>
            {
                me.SetAIAnimKitId(0);
                Tuple<uint, uint> weapons = _randomWeapons.SelectRandom();
                me.SetVirtualItem(0, weapons.Item1);
                me.SetVirtualItem(1, weapons.Item2);

                context.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), context =>
                {
                    me.SetVirtualItem(0, 0);
                    me.SetVirtualItem(1, 0);
                    context.Schedule(TimeSpan.FromSeconds(10), context =>
                    {
                        me.SetAIAnimKitId(1583);
                    });
                });

                context.Repeat(TimeSpan.FromSeconds(30));
            });
        }
    }

    [Script]
    class scene_odyn_intro : SceneScript
    {
        public scene_odyn_intro() : base("scene_odyn_intro") { }

        public override void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            PhasingHandler.RemovePhase(player, PhaseIds.Danica, false);
            PhasingHandler.RemovePhase(player, PhaseIds.Odyn, true);
            player.SetControlled(true, UnitState.Root);
        }

        // Called when a scene is canceled
        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Finish(player);
        }

        // Called when a scene is completed
        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            Finish(player);
        }

        void Finish(Player player)
        {
            PhasingHandler.AddPhase(player, PhaseIds.Odyn, true);
            player.SetControlled(false, UnitState.Root);
            player.CastSpell(player, SpellIds.CancelCompleteSceneWarriorOrderFormation);
        }
    }
}

