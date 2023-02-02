using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;


using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(DMCreatures.NPC_VANESSA_NIGHTMARE)]
    public class npc_vanessa_nightmare : BossAI
    {
        public static readonly Position[] EnragedWorgen_1 =
        {
            new Position(-97.79166f, -717.8542f, 8.668088f, 4.520403f),
            new Position(-94.40278f, -719.7274f, 8.598646f, 3.560472f),
            new Position(-101.9167f, -718.7552f, 8.726379f, 5.51524f)
        };

        public static readonly Position[] EnragedWorgen_2 =
        {
            new Position(3.137153f, -760.0313f, 9.725998f, 5.393067f),
            new Position(8.798013f, -762.2252f, 9.625132f, 3.379143f),
            new Position(4.232807f, -766.6125f, 9.804724f, 1.292649f)
        };


        public static readonly Position[] ElectricSpark =
        {
            new Position(-101.4959f, -648.5552f, 8.121676f, 0.04567058f),
            new Position(-120.96f, -638.3806f, 13.38522f, 6.237791f),
            new Position(-135.365f, -623.0541f, 15.48179f, 6.237976f),
            new Position(-120.1277f, -617.6179f, 15.28394f, 0.04498905f),
            new Position(-136.7082f, -604.6687f, 16.56965f, 6.2384f),
            new Position(-130.45f, -586.5038f, 19.61726f, 6.238641f),
            new Position(-142.9731f, -574.9221f, 20.18317f, 6.238891f)
        };

        public static readonly Position[] FieryBlaze =
        {
            new Position(-178.2031f, -594.9965f, 40.6501f, 4.415683f),
            new Position(-220.625f, -577.9618f, 21.06016f, 2.513274f),
            new Position(-205.3056f, -563.6285f, 21.06016f, 5.25344f),
            new Position(-214.8958f, -546.7136f, 19.3898f, 4.712389f),
            new Position(-207.8004f, -570.6441f, 21.06016f, 1.762783f),
            new Position(-221.4653f, -549.9358f, 19.3898f, 3.211406f),
            new Position(-229.9635f, -559.2552f, 19.3898f, 0),
            new Position(-216.8438f, -568.9011f, 21.06016f, 3.909538f),
            new Position(-235.9045f, -563.3906f, 19.3898f, 0),
            new Position(-226.6736f, -580.8316f, 20.43056f, 2.775074f),
            new Position(-227.5226f, -595.1979f, 20.42358f, 4.206244f),
            new Position(-215.0399f, -576.3941f, 21.06016f, 3.735005f),
            new Position(-210.592f, -583.4739f, 21.06016f, 0),
            new Position(-216.5399f, -602.6528f, 24.88029f, 2.687807f),
            new Position(-220.4879f, -596.382f, 21.95116f, 0),
            new Position(-190.4774f, -552.2778f, 51.31293f, 5.305801f),
            new Position(-195.6267f, -550.4393f, 51.31293f, 3.752458f),
            new Position(-209.7257f, -557.1042f, 51.31293f, 3.525565f),
            new Position(-187.9531f, -567.0469f, 51.31293f, 5.305801f),
            new Position(-192.2031f, -595.9636f, 36.37407f, 2.80998f),
            new Position(-183.4236f, -577.2674f, 46.87183f, 3.944444f),
            new Position(-184.6528f, -572.3663f, 49.27317f, 3.159046f),
            new Position(-187.3333f, -550.8143f, 19.3898f, 3.385939f),
            new Position(-185.2083f, -562.4844f, 19.3898f, 0.9599311f),
            new Position(-228.592f, -553.1684f, 19.3898f, 5.550147f),
            new Position(-210.7431f, -603.2813f, 27.17259f, 4.904375f),
            new Position(-194.1302f, -548.3055f, 19.3898f, 4.153883f),
            new Position(-181.2379f, -555.3177f, 19.3898f, 0.3141593f),
            new Position(-191.2205f, -581.4965f, 21.06015f, 2.007129f),
            new Position(-198.4653f, -580.757f, 21.06015f, 0.8901179f),
            new Position(-196.5504f, -587.7031f, 21.06015f, 1.27409f),
            new Position(-241.5938f, -578.6858f, 19.3898f, 2.775074f),
            new Position(-226.1615f, -573.8021f, 20.40991f, 5.218534f),
            new Position(-186.9792f, -556.8472f, 19.3898f, 4.153883f),
            new Position(-201.224f, -570.6788f, 21.06016f, 3.577925f),
            new Position(-196.8767f, -574.9688f, 21.06016f, 4.29351f),
            new Position(-225.6962f, -601.3871f, 21.82762f, 4.555309f),
            new Position(-215.7205f, -608.4722f, 25.87703f, 2.530727f),
            new Position(-197.1007f, -609.7257f, 32.38494f, 0),
            new Position(-221.8629f, -607.2205f, 23.7542f, 4.939282f),
            new Position(-201.9757f, -611.8663f, 30.62297f, 2.897247f)
        };

        public static readonly Position[] FamilySpawn =
        {
            new Position(-98.63194f, -721.6268f, 8.547067f, 1.53589f),
            new Position(5.239583f, -763.0868f, 9.800426f, 2.007129f),
            new Position(-83.86406f, -775.2837f, 28.37906f, 1.710423f),
            new Position(-83.16319f, -774.9636f, 26.90351f, 1.710423f)
        };


        public npc_vanessa_nightmare(Creature creature) : base(creature, DMData.DATA_VANESSA_NIGHTMARE)
        {
        }

        public bool Nightmare;
        public bool ShiftToTwo;
        public bool ShiftToThree;
        public bool ShiftToFour;

        public byte Phase;
        public byte NightmareCount;
        public byte WorgenCount;
        public uint NightmareTimer;


        public override void Reset()
        {
            me.Say(boss_vanessa_vancleef.VANESSA_GLUB_1, Language.Universal);
            Nightmare = true;
            ShiftToTwo = false;
            ShiftToThree = false;
            ShiftToFour = false;
            NightmareCount = 0;
            WorgenCount = 0;
            Phase = 0;
            NightmareTimer = 3500;
            summons.DespawnAll();
            me.SetSpeed(UnitMoveType.Run, 5.0f);
        }

        public void NightmarePass()
        {
            NightmareCount++;

            if (NightmareCount == 1)
            {
                summons.DespawnAll();
                ShiftToTwo = true;
                Phase = 4;
                NightmareTimer = 3500;
            }

            if (NightmareCount == 2)
            {
                summons.DespawnAll();
                ShiftToThree = true;
                Phase = 9;
                NightmareTimer = 3500;
            }

            if (NightmareCount == 3)
            {
                summons.DespawnAll();
                ShiftToFour = true;
                Phase = 13;
                NightmareTimer = 3500;
            }
        }

        public void WorgenKilled()
        {
            WorgenCount++;

            if (WorgenCount == 3)
            {
                Phase = 18;
            }

            if (WorgenCount == 6)
            {
                Phase = 20;
            }

            if (WorgenCount == 7)
            {
                Phase = 23;
            }
        }

        public override void JustSummoned(Creature summoned)
        {
            summons.Summon(summoned);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            summons.Despawn(summon);
        }

        public void SummonAllFires()
        {
            for (byte i = 0; i < 4; ++i)
            {
                Creature saFires = me.SummonCreature(DMCreatures.NPC_FIRE_BUNNY, FieryBlaze[i], TempSummonType.ManualDespawn);
                if (saFires != null)
                {
                    saFires.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Pacified | UnitFlags.Uninteractible);
                }
            }
        }

        public void SummonSparks()
        {
            for (byte i = 0; i < 7; ++i)
            {
                Creature sSp = me.SummonCreature(DMCreatures.NPC_SPARK, ElectricSpark[i], TempSummonType.ManualDespawn);
                if (sSp != null)
                {
                    sSp.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Pacified | UnitFlags.Uninteractible);
                }
            }
        }

        public void SummonWorgen_1()
        {
            for (byte i = 0; i < 3; ++i)
            {
                me.SummonCreature(DMCreatures.NPC_ENRAGED_WORGEN, EnragedWorgen_1[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
            }
        }

        public void SummonWorgen_2()
        {
            for (byte i = 0; i < 3; ++i)
            {
                me.SummonCreature(DMCreatures.NPC_ENRAGED_WORGEN, EnragedWorgen_2[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (Nightmare)
            {
                if (NightmareTimer <= diff)
                {
                    switch (Phase)
                    {
                        case 0:
                            SummonAllFires();
                            me.Say(boss_vanessa_vancleef.VANESSA_GLUB_2, Language.Universal);
                            NightmareTimer = 3000;
                            Phase++;
                            break;
                        case 1:
                            {
                                me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_3, null, true);
                                NightmareTimer = 4000;
                                Phase++;
                            }
                            break;
                        case 2:
                            {
                                Creature Glubtok = me.FindNearestCreature(DMCreatures.NPC_GLUBTOK_NIGHTMARE, 200.0f, true);
                                if (Glubtok != null)
                                {
                                    Glubtok.SetVisible(false);
                                    Glubtok.GetMotionMaster().MoveCharge(-174.85f, -579.76f, 19.31f, 10.0f);
                                }

                                me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_4, null, true);
                                NightmareTimer = 2000;
                                Phase++;
                            }
                            break;
                        case 3:
                            Nightmare = false;
                            me.SetVisible(false);
                            me.GetMotionMaster().MovePoint(0, -178.85f, -585.76f, 19.31f);
                            break;
                    }
                }
                else
                {
                    NightmareTimer -= diff;
                }
            }

            if (ShiftToTwo)
            {
                if (NightmareTimer <= diff)
                {
                    switch (Phase)
                    {
                        case 4:
                            me.SetVisible(true);
                            me.Say(boss_vanessa_vancleef.VANESSA_HELIX_1, Language.Universal);
                            me.SummonCreature(DMCreatures.NPC_HELIX_NIGHTMARE, -174.85f, -579.76f, 19.31f, 3.14f, TempSummonType.ManualDespawn);
                            NightmareTimer = 3000;
                            Phase++;
                            break;
                        case 5:
                            me.Say(boss_vanessa_vancleef.VANESSA_HELIX_2, Language.Universal);
                            NightmareTimer = 10000;
                            Phase++;
                            break;
                        case 6:
                            me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_5, null, true);
                            NightmareTimer = 1000;
                            Phase++;
                            break;
                        case 7:
                            me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_7, null, true);
                            me.SetVisible(false);
                            NightmareTimer = 2000;
                            Phase++;
                            break;
                        case 8:
                            ShiftToTwo = false;
                            me.GetMotionMaster().MovePoint(1, -150.96f, -579.99f, 19.31f);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    NightmareTimer -= diff;
                }
            }

            if (ShiftToThree)
            {
                if (NightmareTimer <= diff)
                {
                    switch (Phase)
                    {
                        case 9:
                            {
                                SummonSparks();
                                me.SetVisible(true);
                                instance.SetData(DMData.DATA_NIGHTMARE_HELIX, (uint)EncounterState.Done);
                                me.Say(boss_vanessa_vancleef.VANESSA_MECHANICAL_1, Language.Universal);
                                me.SummonCreature(DMCreatures.NPC_MECHANICAL_NIGHTMARE, -101.4549f, -663.6493f, 7.505813f, 1.85f, TempSummonType.ManualDespawn);
                                NightmareTimer = 4000;
                                Phase++;
                            }
                            break;
                        case 10:
                            me.Say(boss_vanessa_vancleef.VANESSA_MECHANICAL_2, Language.Universal);
                            NightmareTimer = 3000;
                            Phase++;
                            break;
                        case 11:
                            me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_8, null, true);
                            NightmareTimer = 3000;
                            Phase++;
                            break;
                        case 12:
                            ShiftToThree = false;
                            me.SetVisible(false);
                            me.GetMotionMaster().MovePoint(2, -96.46f, -660.42f, 7.41f);
                            break;
                    }
                }
                else
                {
                    NightmareTimer -= diff;
                }
            }

            if (ShiftToFour)
            {
                if (NightmareTimer <= diff)
                {
                    switch (Phase)
                    {
                        case 13:
                            me.SetVisible(true);
                            me.Say(boss_vanessa_vancleef.VANESSA_RIPSNARL_1, Language.Universal);
                            NightmareTimer = 4000;
                            Phase++;
                            break;
                        case 14:
                            me.Say(boss_vanessa_vancleef.VANESSA_RIPSNARL_2, Language.Universal);
                            NightmareTimer = 6000;
                            Phase++;
                            break;
                        case 15:
                            me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_9, null, true);
                            instance.SetData(DMData.DATA_NIGHTMARE_MECHANICAL, (uint)EncounterState.Done);
                            NightmareTimer = 2000;
                            Phase++;
                            break;
                        case 16:
                            {
                                List<Unit> players = new List<Unit>();

                                AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                                PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                                Cell.VisitWorldObjects(me, searcher, 150f);

                                foreach (var item in players)
                                {
                                    me.CastSpell(item, boss_vanessa_vancleef.Spells.SPELL_SPRINT, true);
                                }

                                me.SummonCreature(DMCreatures.NPC_EMME_HARRINGTON, FamilySpawn[0], TempSummonType.ManualDespawn);
                                me.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_10, null, true);
                                SummonWorgen_1();

                                me.SetVisible(false);
                                me.GetMotionMaster().MovePoint(3, -103.72f, -724.06f, 8.47f);
                                Phase++;
                                NightmareTimer = 1000;

                            }
                            break;
                        case 17:
                            break;
                        case 18:
                            {
                                List<Unit> players = new List<Unit>();

                                AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                                PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                                Cell.VisitWorldObjects(me, searcher, 150f);

                                foreach (var item in players)
                                {
                                    item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_11, null, true);
                                    me.CastSpell(item, boss_vanessa_vancleef.Spells.SPELL_SPRINT, true);
                                }

                                me.SummonCreature(DMCreatures.NPC_ERIK_HARRINGTON, FamilySpawn[1], TempSummonType.ManualDespawn);
                                SummonWorgen_2();

                                me.GetMotionMaster().MovePoint(4, 2.56f, -776.13f, 9.52f);
                                Phase++;
                                NightmareTimer = 3000;

                            }
                            break;
                        case 19:
                            break;
                        case 20:
                            {
                                List<Unit> players = new List<Unit>();

                                AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                                PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                                Cell.VisitWorldObjects(me, searcher, 150f);

                                foreach (var item in players)
                                {
                                    me.CastSpell(item, boss_vanessa_vancleef.Spells.SPELL_SPRINT, true);
                                    item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_12, null, true);
                                }

                                me.GetMotionMaster().MovePoint(5, -83.16319f, -774.9636f, 26.90351f);
                                me.SummonCreature(DMCreatures.NPC_JAMES_HARRINGTON, FamilySpawn[3], TempSummonType.ManualDespawn);
                                NightmareTimer = 5000;
                                Phase++;

                            }
                            break;
                        case 21:
                            NightmareTimer = 1000;
                            Phase++;
                            break;
                        case 22:
                            break;
                        case 23:
                            {
                                List<Unit> players = new List<Unit>();
                                AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
                                PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
                                Cell.VisitWorldObjects(me, searcher, 150f);

                                foreach (var item in players)
                                {
                                    item.RemoveAurasDueToSpell(DMSharedSpells.SPELL_NIGHTMARE_ELIXIR);
                                    item.RemoveAurasDueToSpell(boss_vanessa_vancleef.Spells.SPELL_EFFECT_1);
                                    item.TextEmote(boss_vanessa_vancleef.VANESSA_NIGHTMARE_13, null, true);
                                }
                                me.SummonCreature(DMCreatures.NPC_VANESSA_BOSS, -79.44965f, -819.8351f, 39.89838f, 0.01745329f, TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(120000));
                                Creature note = me.FindNearestCreature(DMCreatures.NPC_VANESSA_NOTE, 300.0f);
                                if (note != null)
                                {
                                    note.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));
                                }
                                NightmareTimer = 1000;
                                Phase++;
                            }
                            break;
                        case 24:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    NightmareTimer -= diff;
                }
            }
        }
    }
}
