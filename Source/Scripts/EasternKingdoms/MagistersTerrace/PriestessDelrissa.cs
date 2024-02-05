// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.MagistersTerrace.PriestessDelrissa
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayDeath = 10;
    }

    struct SpellIds
    {
        public const uint DispelMagic = 27609;
        public const uint FlashHeal = 17843;
        public const uint SwPainNormal = 14032;
        public const uint SwPainHeroic = 15654;
        public const uint Shield = 44291;
        public const uint RenewNormal = 44174;
        public const uint RenewHeroic = 46192;

        // Apoko
        public const uint WindfuryTotem = 27621;
        public const uint WarStomp = 46026;
        public const uint Purge = 27626;
        public const uint LesserHealingWave = 44256;
        public const uint FrostShock = 21401;
        public const uint FireNovaTotem = 44257;
        public const uint EarthbindTotem = 15786;

        public const uint HealingPotion = 15503;

        // RogueSpells
        public const uint KidneyShot = 27615;
        public const uint Gouge = 12540;
        public const uint Kick = 27613;
        public const uint Vanish = 44290;
        public const uint Backstab = 15657;
        public const uint Eviscerate = 27611;

        // WarlockSpells
        public const uint Immolate = 44267;
        public const uint ShadowBolt = 12471;
        public const uint SeedOfCorruption = 44141;
        public const uint CurseOfAgony = 14875;
        public const uint Fear = 38595;
        public const uint ImpFireball = 44164;
        public const uint SummonImp = 44163;

        // KickDown
        public const uint Knockdown = 11428;
        public const uint SnapKick = 46182;

        // MageSpells
        public const uint Polymorph = 13323;
        public const uint IceBlock = 27619;
        public const uint Blizzard = 44178;
        public const uint IceLance = 46194;
        public const uint ConeOfCold = 38384;
        public const uint Frostbolt = 15043;
        public const uint Blink = 14514;

        // WarriorSpells
        public const uint InterceptStun = 27577;
        public const uint Disarm = 27581;
        public const uint PiercingHowl = 23600;
        public const uint FrighteningShout = 19134;
        public const uint Hamstring = 27584;
        public const uint BattleShout = 27578;
        public const uint MortalStrike = 44268;

        // HunterSpells
        public const uint AimedShot = 44271;
        public const uint Shoot = 15620;
        public const uint ConcussiveShot = 27634;
        public const uint MultiShot = 31942;
        public const uint WingClip = 44286;
        public const uint FreezingTrap = 44136;

        // EngineerSpells
        public const uint GoblinDragonGun = 44272;
        public const uint RocketLaunch = 44137;
        public const uint Recombobulate = 44274;
        public const uint HighExplosiveSheep = 44276;
        public const uint FelIronBomb = 46024;
        public const uint SheepExplosion = 44279;
    }

    struct CreatureIds
    {
        public const uint Sliver = 24552;
    }

    struct MiscConst
    {
        public const uint MaxActiveLackey = 4;

        public const float fOrientation = 4.98f;
        public const float fZLocation = -19.921f;

        public static float[][] LackeyLocations =
        {
            new float[] {123.77f, 17.6007f},
            new float[] {131.731f, 15.0827f},
            new float[] {121.563f, 15.6213f},
            new float[] {129.988f, 17.2355f},
        };

        public static uint[] m_auiAddEntries =
        {
            24557,                                                  //Kagani Nightstrike
            24558,                                                  //Elris Duskhallow
            24554,                                                  //Eramas Brightblaze
            24561,                                                  //Yazzaj
            24559,                                                  //Warlord Salaris
            24555,                                                  //Garaxxas
            24553,                                                  //Apoko
            24556,                                                  //Zelfan
        };

        public static uint[] LackeyDeath = { 1, 2, 3, 4, };

        public static uint[] PlayerDeath = { 5, 6, 7, 8, 9, };
    }

    [Script]
    class boss_priestess_delrissa : BossAI
    {
        List<uint> LackeyEntryList = new();
        public ObjectGuid[] m_auiLackeyGUID = new ObjectGuid[MiscConst.MaxActiveLackey];

        byte PlayersKilled;

        public boss_priestess_delrissa(Creature creature) : base(creature, DataTypes.PriestessDelrissa)
        {
            Initialize();
            LackeyEntryList.Clear();
        }

        void Initialize()
        {
            PlayersKilled = 0;

            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                ulong health = me.GetHealth();
                Unit target = me;
                for (byte i = 0; i < m_auiLackeyGUID.Length; ++i)
                {
                    Unit pAdd = Global.ObjAccessor.GetUnit(me, m_auiLackeyGUID[i]);
                    if (pAdd != null && pAdd.IsAlive() && pAdd.GetHealth() < health)
                        target = pAdd;
                }

                DoCast(target, SpellIds.FlashHeal);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Unit target = me;

                if (RandomHelper.URand(0, 1) != 0)
                {
                    Unit pAdd = Global.ObjAccessor.GetUnit(me, m_auiLackeyGUID[RandomHelper.Rand32() % m_auiLackeyGUID.Length]);
                    if (pAdd != null && pAdd.IsAlive())
                        target = pAdd;
                }

                DoCast(target, SpellIds.RenewNormal);
                task.Repeat(TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                Unit target = me;

                if (RandomHelper.URand(0, 1) != 0)
                {
                    Unit pAdd = Global.ObjAccessor.GetUnit(me, m_auiLackeyGUID[RandomHelper.Rand32() % m_auiLackeyGUID.Length]);
                    if (pAdd != null && pAdd.IsAlive() && !pAdd.HasAura(SpellIds.Shield))
                        target = pAdd;
                }

                DoCast(target, SpellIds.Shield);
                task.Repeat(TimeSpan.FromSeconds(7.5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                    DoCast(target, SpellIds.SwPainNormal);

                task.Repeat(TimeSpan.FromSeconds(10));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7.5), task =>
            {
                Unit target = null;

                if (RandomHelper.URand(0, 1) != 0)
                    target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                else
                {
                    if (RandomHelper.URand(0, 1) != 0)
                        target = me;
                    else
                    {
                        Unit pAdd = Global.ObjAccessor.GetUnit(me, m_auiLackeyGUID[RandomHelper.Rand32() % m_auiLackeyGUID.Length]);
                        if (pAdd != null && pAdd.IsAlive())
                            target = pAdd;
                    }
                }

                if (target != null)
                    DoCast(target, SpellIds.DispelMagic);

                task.Repeat(TimeSpan.FromSeconds(12));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                me.GetHomePosition(out _, out _, out float z, out _);
                if (me.GetPositionZ() >= z + 10)
                {
                    EnterEvadeMode();
                    return;
                }
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            InitializeLackeys();
        }

        //this mean she at some point evaded
        public override void JustReachedHome()
        {
            instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Fail);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);

            foreach (var lackeyGuid in m_auiLackeyGUID)
            {
                Unit pAdd = Global.ObjAccessor.GetUnit(me, lackeyGuid);
                if (pAdd != null && !pAdd.IsEngaged())
                    AddThreat(who, 0.0f, pAdd);
            }

            instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.InProgress);
        }

        void InitializeLackeys()
        {
            //can be called if Creature are dead, so avoid
            if (!me.IsAlive())
                return;

            byte j = 0;

            //it's empty, so first time
            if (LackeyEntryList.Empty())
            {
                //fill vector array with entries from Creature array
                for (byte i = 0; i < LackeyEntryList.Count; ++i)
                    LackeyEntryList[i] = MiscConst.m_auiAddEntries[i];

                //remove random entries
                LackeyEntryList.RandomResize(MiscConst.MaxActiveLackey);

                //summon all the remaining in vector
                foreach (var guid in LackeyEntryList)
                {
                    Creature pAdd = me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.fZLocation, MiscConst.fOrientation, TempSummonType.CorpseDespawn);
                    if (pAdd != null)
                        m_auiLackeyGUID[j] = pAdd.GetGUID();

                    ++j;
                }
            }
            else
            {
                foreach (var guid in LackeyEntryList)
                {
                    Unit pAdd = Global.ObjAccessor.GetUnit(me, m_auiLackeyGUID[j]);

                    //object already removed, not exist
                    if (pAdd == null)
                    {
                        pAdd = me.SummonCreature(guid, MiscConst.LackeyLocations[j][0], MiscConst.LackeyLocations[j][1], MiscConst.fZLocation, MiscConst.fOrientation, TempSummonType.CorpseDespawn);
                        if (pAdd != null)
                            m_auiLackeyGUID[j] = pAdd.GetGUID();
                    }
                    ++j;
                }
            }
        }

        public override void KilledUnit(Unit victim)
        {
            if (!victim.IsPlayer())
                return;

            Talk(MiscConst.PlayerDeath[PlayersKilled]);

            if (PlayersKilled < 4)
                ++PlayersKilled;
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            if (instance.GetData(DataTypes.DelrissaDeathCount) == MiscConst.MaxActiveLackey)
                instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Done);
            else
                me.RemoveDynamicFlag(UnitDynFlags.Lootable);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    //all 8 possible lackey use this common
    class boss_priestess_lackey_common : ScriptedAI
    {
        InstanceScript instance;
        bool UsedPotion;

        public ObjectGuid[] m_auiLackeyGUIDs = new ObjectGuid[MiscConst.MaxActiveLackey];

        public boss_priestess_lackey_common(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            UsedPotion = false;

            // These guys does not follow normal threat system rules
            // For later development, some alternative threat system should be made
            // We do not know what this system is based upon, but one theory is class (healers=high threat, dps=medium, etc)
            // We reset their threat frequently as an alternative until such a system exist
            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), task =>
            {
                ResetThreatList();
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();
            AcquireGUIDs();

            // in case she is not alive and Reset was for some reason called, respawn her (most likely party wipe after killing her)
            Creature delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
            if (delrissa != null)
            {
                if (!delrissa.IsAlive())
                    delrissa.Respawn();
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            if (who == null)
                return;

            foreach (var guid in m_auiLackeyGUIDs)
            {
                Unit pAdd = Global.ObjAccessor.GetUnit(me, guid);
                if (pAdd != null && !pAdd.IsEngaged() && pAdd != me)
                    AddThreat(who, 0.0f, pAdd);
            }

            Creature delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
            if (delrissa != null)
                if (delrissa.IsAlive() && !delrissa.IsEngaged())
                    AddThreat(who, 0.0f, delrissa);
        }

        public override void JustDied(Unit killer)
        {
            Creature delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
            uint uiLackeyDeathCount = instance.GetData(DataTypes.DelrissaDeathCount);

            if (delrissa == null)
                return;

            //should delrissa really yell if dead?
            delrissa.GetAI().Talk(MiscConst.LackeyDeath[uiLackeyDeathCount]);

            instance.SetData(DataTypes.DelrissaDeathCount, (uint)EncounterState.Special);

            //increase local var, since we now may have four dead
            ++uiLackeyDeathCount;

            if (uiLackeyDeathCount == MiscConst.MaxActiveLackey)
            {
                //time to make her lootable and complete event if she died before lackeys
                if (!delrissa.IsAlive())
                {
                    delrissa.SetDynamicFlag(UnitDynFlags.Lootable);

                    instance.SetBossState(DataTypes.PriestessDelrissa, EncounterState.Done);
                }
            }
        }

        public override void KilledUnit(Unit victim)
        {
            Creature delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
            if (delrissa != null)
                delrissa.GetAI().KilledUnit(victim);
        }

        void AcquireGUIDs()
        {
            Creature delrissa = instance.GetCreature(DataTypes.PriestessDelrissa);
            if (delrissa != null)
            {
                for (byte i = 0; i < MiscConst.MaxActiveLackey; ++i)
                    m_auiLackeyGUIDs[i] = (delrissa.GetAI() as boss_priestess_delrissa).m_auiLackeyGUID[i];
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UsedPotion && HealthBelowPct(25))
            {
                DoCast(me, SpellIds.HealingPotion);
                UsedPotion = true;
            }

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_kagani_nightstrike : boss_priestess_lackey_common
    {
        bool InVanish;

        //Rogue
        public boss_kagani_nightstrike(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5.5), task =>
            {
                DoCastVictim(SpellIds.Gouge);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.Kick);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCast(me, SpellIds.Vanish);

                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);

                ResetThreatList();

                if (unit != null)
                    AddThreat(unit, 1000.0f);

                InVanish = true;
                me.SetCanMelee(false);
                task.Repeat(TimeSpan.FromSeconds(30));
                task.Schedule(TimeSpan.FromSeconds(10), waitTask =>
                {
                    if (InVanish)
                    {
                        DoCastVictim(SpellIds.Backstab, new CastSpellExtraArgs(true));
                        DoCastVictim(SpellIds.KidneyShot, new CastSpellExtraArgs(true));
                        me.SetVisible(true);       // ...? Hacklike
                        me.SetCanMelee(true);
                        InVanish = false;
                    }
                    waitTask.Repeat();
                });
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Eviscerate);
                task.Repeat(TimeSpan.FromSeconds(4));
            });

            InVanish = false;
        }

        public override void Reset()
        {
            Initialize();
            me.SetVisible(true);
            me.SetCanMelee(true);

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_ellris_duskhallow : boss_priestess_lackey_common
    {
        //Warlock
        public boss_ellris_duskhallow(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Immolate);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastVictim(SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(5));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                if (unit != null)
                    DoCast(unit, SpellIds.SeedOfCorruption);

                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                if (unit != null)
                    DoCast(unit, SpellIds.CurseOfAgony);

                task.Repeat(TimeSpan.FromSeconds(13));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                if (unit != null)
                    DoCast(unit, SpellIds.Fear);

                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            DoCast(me, SpellIds.SummonImp);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_eramas_brightblaze : boss_priestess_lackey_common
    {
        //Monk
        public boss_eramas_brightblaze(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Knockdown);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4.5), task =>
            {
                DoCastVictim(SpellIds.SnapKick);
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_yazzai : boss_priestess_lackey_common
    {
        bool HasIceBlocked;

        //Mage
        public boss_yazzai(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            HasIceBlocked = false;

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0);
                if (target != null)
                {
                    DoCast(target, SpellIds.Polymorph);
                    task.Repeat(TimeSpan.FromSeconds(20));
                }
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                if (unit != null)
                    DoCast(unit, SpellIds.Blizzard);

                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.IceLance);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.ConeOfCold);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
            {
                DoCastVictim(SpellIds.Frostbolt);
                task.Repeat(TimeSpan.FromSeconds(8));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                bool InMeleeRange = false;
                foreach (var pair in me.GetCombatManager().GetPvECombatRefs())
                {
                    if (pair.Value.GetOther(me).IsWithinMeleeRange(me))
                    {
                        InMeleeRange = true;
                        break;
                    }
                }

                //if anybody is in melee range than escape by blink
                if (InMeleeRange)
                    DoCast(me, SpellIds.Blink);

                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (HealthBelowPct(35) && !HasIceBlocked)
            {
                DoCast(me, SpellIds.IceBlock);
                HasIceBlocked = true;
            }

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_warlord_salaris : boss_priestess_lackey_common
    {
        //Warrior
        public boss_warlord_salaris(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(500), task =>
            {
                bool InMeleeRange = false;
                foreach (var pair in me.GetCombatManager().GetPvECombatRefs())
                {
                    if (pair.Value.GetOther(me).IsWithinMeleeRange(me))
                    {
                        InMeleeRange = true;
                        break;
                    }
                }

                //if nobody is in melee range than try to use Intercept
                if (!InMeleeRange)
                {
                    Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                    if (unit != null)
                        DoCast(unit, SpellIds.InterceptStun);
                }

                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.Disarm);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.PiercingHowl);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(18), task =>
            {
                DoCastVictim(SpellIds.FrighteningShout);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4.5), task =>
            {
                DoCastVictim(SpellIds.Hamstring);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.MortalStrike);
                task.Repeat(TimeSpan.FromSeconds(4.5));
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void JustEngagedWith(Unit who)
        {
            DoCast(me, SpellIds.BattleShout);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_garaxxas : boss_priestess_lackey_common
    {
        TaskScheduler _meleeScheduler = new();

        ObjectGuid m_uiPetGUID;

        //Hunter
        public boss_garaxxas(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
            {
                DoCastVictim(SpellIds.AimedShot);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2.5), task =>
            {
                DoCastVictim(SpellIds.Shoot);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.ConcussiveShot);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCastVictim(SpellIds.MultiShot);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.WingClip);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                //attempt find go summoned from spell (cast by me)
                GameObject go = me.GetGameObject(SpellIds.FreezingTrap);

                //if we have a go, we need to wait (only one trap at a time)
                if (go != null)
                    task.Repeat(TimeSpan.FromSeconds(2.5));
                else
                {
                    //if go does not exist, then we can cast
                    DoCastVictim(SpellIds.FreezingTrap);
                    task.Repeat();
                }
            });
        }

        public override void Reset()
        {
            Initialize();

            Unit pPet = Global.ObjAccessor.GetUnit(me, m_uiPetGUID);
            if (pPet == null)
                me.SummonCreature(CreatureIds.Sliver, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.CorpseDespawn);

            base.Reset();
        }

        public override void JustSummoned(Creature summoned)
        {
            m_uiPetGUID = summoned.GetGUID();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (me.IsWithinDistInMap(me.GetVictim(), SharedConst.AttackDistance))
                _meleeScheduler.Update(diff);
            else
                _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_apoko : boss_priestess_lackey_common
    {
        byte Totem_Amount;

        //Shaman
        public boss_apoko(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            Totem_Amount = 1;
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCast(me, RandomHelper.RAND(SpellIds.WindfuryTotem, SpellIds.FireNovaTotem, SpellIds.EarthbindTotem));
                ++Totem_Amount;
                task.Repeat(TimeSpan.FromMilliseconds(Totem_Amount * 2000));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, SpellIds.WarStomp);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit unit = SelectTarget(SelectTargetMethod.Random, 0);
                if (unit != null)
                    DoCast(unit, SpellIds.Purge);

                task.Repeat(TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCast(me, SpellIds.LesserHealingWave);
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.FrostShock);
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }

    [Script]
    class boss_zelfan : boss_priestess_lackey_common
    {
        //Engineer
        public boss_zelfan(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(SpellIds.GoblinDragonGun);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.RocketLaunch);
                task.Repeat(TimeSpan.FromSeconds(9));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                foreach (var guid in m_auiLackeyGUIDs)
                {
                    Unit pAdd = Global.ObjAccessor.GetUnit(me, guid);
                    if (pAdd != null && pAdd.IsPolymorphed())
                    {
                        DoCast(pAdd, SpellIds.Recombobulate);
                        break;
                    }
                }
                task.Repeat(TimeSpan.FromSeconds(2));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, SpellIds.HighExplosiveSheep);
                task.Repeat(TimeSpan.FromSeconds(65));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.FelIronBomb);
                task.Repeat();
            });
        }

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            _scheduler.Update(diff);
        }
    }
}