// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using static Global;

namespace Scripts.World.GameObjects
{
    class go_gilded_brazier : GameObjectAI
    {
        const uint NpcStillblade = 17716;
        const uint QuestTheFirstTrial = 9678;

        public go_gilded_brazier(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
            {
                if (player.GetQuestStatus(QuestTheFirstTrial) == QuestStatus.Incomplete)
                {
                    Creature stillblade = player.SummonCreature(NpcStillblade, 8106.11f, -7542.06f, 151.775f, 3.02598f, TempSummonType.DeadDespawn, TimeSpan.FromMinutes(1));
                    if (stillblade != null)
                        stillblade.GetAI().AttackStart(player);
                }
            }
            return true;
        }
    }

    class go_tablet_of_the_seven : GameObjectAI
    {
        public go_tablet_of_the_seven(GameObject go) : base(go) { }

        /// @todo use gossip option ("Transcript the Tablet") instead, if Trinity adds support.
        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoType() != GameObjectTypes.QuestGiver)
                return true;

            if (player.GetQuestStatus(4296) == QuestStatus.Incomplete)
                player.CastSpell(player, 15065, false);

            return true;
        }
    }

    class go_ethereum_prison : GameObjectAI
    {
        const uint SpellRepLc = 39456;
        const uint SpellRepShat = 39457;
        const uint SpellRepCe = 39460;
        const uint SpellRepCon = 39474;
        const uint SpellRepKt = 39475;
        const uint SpellRepSpor = 39476;

        uint[] NpcPrisonEntry =
        {
            22810, 22811, 22812, 22813, 22814, 22815,               //good guys
            20783, 20784, 20785, 20786, 20788, 20789, 20790         //bad guys
        };

        public go_ethereum_prison(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            me.UseDoorOrButton();

            Creature creature = player.SummonCreature(NpcPrisonEntry.SelectRandom(), me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAbsoluteAngle(player), TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));
            if (creature != null)
            {
                if (!creature.IsHostileTo(player))
                {
                    var pFaction = creature.GetFactionTemplateEntry();
                    if (pFaction != null)
                    {
                        uint spellId = 0;

                        switch (pFaction.Faction)
                        {
                            case 1011: spellId = SpellRepLc; break;
                            case 935: spellId = SpellRepShat; break;
                            case 942: spellId = SpellRepCe; break;
                            case 933: spellId = SpellRepCon; break;
                            case 989: spellId = SpellRepKt; break;
                            case 970: spellId = SpellRepSpor; break;
                        }

                        if (spellId != 0)
                            creature.CastSpell(player, spellId, false);
                        else
                            Log.outError(LogFilter.Scripts, $"go_ethereum_prison summoned Creature (entry {creature.GetEntry()})but faction ({creature.GetFaction()})are not expected by script.");
                    }
                }
            }

            return false;
        }
    }

    class go_ethereum_stasis : GameObjectAI
    {
        uint[] NpcStasisEntry = { 22825, 20888, 22827, 22826, 22828 };

        public go_ethereum_stasis(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            me.UseDoorOrButton();

            player.SummonCreature(NpcStasisEntry.SelectRandom(), me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAbsoluteAngle(player),
                TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

            return false;
        }
    }

    class go_resonite_cask : GameObjectAI
    {
        const uint NpcGoggeroc = 11920;

        public go_resonite_cask(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
                me.SummonCreature(NpcGoggeroc, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5));

            return false;
        }
    }

    class go_southfury_moonstone : GameObjectAI
    {
        const uint NpcRizzle = 23002;
        const uint SpellBlackjack = 39865; //stuns player
        const uint SpellSummonRizzle = 39866;

        public go_southfury_moonstone(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            //implicitTarget=48 not implemented as of writing this code, and manual summon may be just ok for our purpose
            //player.CastSpell(player, SpellSummonRizzle, false);

            Creature creature = player.SummonCreature(NpcRizzle, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.DeadDespawn);
            if (creature != null)
                creature.CastSpell(player, SpellBlackjack, false);

            return false;
        }
    }

    class go_tele_to_dalaran_crystal : GameObjectAI
    {
        const uint QuestTeleCrystalFlag = 12845;

        public go_tele_to_dalaran_crystal(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(QuestTeleCrystalFlag))
                return false;

            player.GetSession().SendNotification("This teleport crystal cannot be used until the teleport crystal in Dalaran has been used at least once.");
            return true;
        }
    }

    class go_tele_to_violet_stand : GameObjectAI
    {
        const uint QuestLearnLeaveReturn = 12790;

        public go_tele_to_violet_stand(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(QuestLearnLeaveReturn) || player.GetQuestStatus(QuestLearnLeaveReturn) == QuestStatus.Incomplete)
                return false;

            return true;
        }
    }

    class go_blood_filled_orb : GameObjectAI
    {
        const uint NpcZelemar = 17830;

        public go_blood_filled_orb(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
                player.SummonCreature(NpcZelemar, -369.746f, 166.759f, -21.50f, 5.235f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

            return true;
        }
    }

    class go_soulwell : GameObjectAI
    {
        public go_soulwell(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            Unit owner = me.GetOwner();
            if (owner == null || !owner.IsPlayer() || !player.IsInSameRaidWith(owner.ToPlayer()))
                return true;
            return false;
        }
    }

    class go_amberpine_outhouse : GameObjectAI
    {
        const uint ItemAnderholsSliderCider = 37247;
        const uint NpcOuthouseBunny = 27326;
        const uint QuestDoingYourDuty = 12227;
        const uint SpellIndisposed = 53017;
        const uint SpellIndisposedIii = 48341;
        const uint SpellCreateAmberseeds = 48330;
        const uint GossipOuthouseInuse = 12775;
        const uint GossipOuthouseVacant = 12779;

        public go_amberpine_outhouse(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            QuestStatus status = player.GetQuestStatus(QuestDoingYourDuty);
            if (status == QuestStatus.Incomplete || status == QuestStatus.Complete || status == QuestStatus.Rewarded)
            {
                player.AddGossipItem(GossipOptionNpc.None, "Use the outhouse.", eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                player.SendGossipMenu(GossipOuthouseVacant, me.GetGUID());
            }
            else
                player.SendGossipMenu(GossipOuthouseInuse, me.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            if (action == eTradeskill.GossipActionInfoDef + 1)
            {
                player.CloseGossipMenu();
                Creature target = ScriptedAI.GetClosestCreatureWithEntry(player, NpcOuthouseBunny, 3.0f);
                if (target != null)
                {
                    target.GetAI().SetData(1, (uint)player.GetNativeGender());
                    me.CastSpell(target, SpellIndisposedIii);
                }
                me.CastSpell(player, SpellIndisposed);
                if (player.HasItemCount(ItemAnderholsSliderCider))
                    me.CastSpell(player, SpellCreateAmberseeds);
                return true;
            }
            else
            {
                player.CloseGossipMenu();
                player.GetSession().SendNotification("Quest item Anderhol's Slider Cider not found.");
                return false;
            }
        }
    }

    class go_massive_seaforium_charge : GameObjectAI
    {
        public go_massive_seaforium_charge(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            me.SetLootState(LootState.JustDeactivated);
            return true;
        }
    }

    class go_veil_skith_cage : GameObjectAI
    {
        const uint QuestMissingFriends = 10852;
        const uint NpcCaptiveChild = 22314;
        const uint SayFree0 = 0;

        public go_veil_skith_cage(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            me.UseDoorOrButton();
            if (player.GetQuestStatus(QuestMissingFriends) == QuestStatus.Incomplete)
            {
                List<Creature> childrenList = me.GetCreatureListWithEntryInGrid(NpcCaptiveChild, SharedConst.InteractionDistance);
                foreach (Creature creature in childrenList)
                {
                    player.KilledMonsterCredit(NpcCaptiveChild, creature.GetGUID());
                    creature.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                    creature.GetMotionMaster().MovePoint(1, me.GetPositionX() + 5, me.GetPositionY(), me.GetPositionZ());
                    creature.GetAI().Talk(SayFree0);
                    creature.GetMotionMaster().Clear();
                }
            }
            return false;
        }
    }

    class go_midsummer_bonfire : GameObjectAI
    {
        const uint StampOutBonfireQuestComplete = 45458;

        public go_midsummer_bonfire(GameObject go) : base(go) { }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            player.CastSpell(player, StampOutBonfireQuestComplete, true);
            player.CloseGossipMenu();
            return false;
        }
    }

    class go_midsummer_ribbon_pole : GameObjectAI
    {
        const uint SpellTestRibbonPole1 = 29705;
        const uint SpellTestRibbonPole2 = 29726;
        const uint SpellTestRibbonPole3 = 29727;
        const uint NpcPoleRibbonBunny = 17066;
        const int ActionCosmeticFires = 0;

        uint[] RibbonPoleSpells =
        {
            SpellTestRibbonPole1,
            SpellTestRibbonPole2,
            SpellTestRibbonPole3
        };

        public go_midsummer_ribbon_pole(GameObject go) : base(go) { }

        public override bool OnGossipHello(Player player)
        {
            Creature creature = me.FindNearestCreature(NpcPoleRibbonBunny, 10.0f);
            if (creature != null)
            {
                creature.GetAI().DoAction(ActionCosmeticFires);
                player.CastSpell(player, RibbonPoleSpells[RandomHelper.URand(0, 2)], true);
            }
            return true;
        }
    }

    struct BrewfestMusicConst
    {
        public const uint Dwarf01 = 11810; // 1.35 min
        public const uint Dwarf02 = 11812; // 1.55 min
        public const uint Dwarf03 = 11813; // 0.23 min
        public const uint Goblin01 = 11811; // 1.08 min
        public const uint Goblin02 = 11814; // 1.33 min
        public const uint Goblin03 = 11815; // 0.28 min

        public static TimeSpan Dwarf01Time = TimeSpan.FromSeconds(95);
        public static TimeSpan Dwarf02Time = TimeSpan.FromSeconds(155);
        public static TimeSpan Dwarf03Time = TimeSpan.FromSeconds(23);
        public static TimeSpan Goblin01Time = TimeSpan.FromSeconds(68);
        public static TimeSpan Goblin02Time = TimeSpan.FromSeconds(93);
        public static TimeSpan Goblin03Time = TimeSpan.FromSeconds(28);
    }

    enum BrewfestMusicAreasIds
    {
        Silvermoon = 3430, // Horde
        Undercity = 1497,
        Orgrimmar1 = 1296,
        Orgrimmar2 = 14,
        Thunderbluff = 1638,
        Ironforge1 = 809, // Alliance
        Ironforge2 = 1,
        Stormwind = 12,
        Exodar = 3557,
        Darnassus = 1657,
        Shattrath = 3703 // General
    }

    [Script]
    class go_brewfest_music : GameObjectAI
    {
        uint rnd;
        TimeSpan musicTime = TimeSpan.FromSeconds(1);

        public go_brewfest_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (!GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                    return;
                rnd = RandomHelper.URand(0, 2); // Select random music sample
                task.Repeat(musicTime); // Select new song music after play time is over
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                if (!GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                    return;

                switch ((BrewfestMusicAreasIds)me.GetAreaId())
                {
                    // Horde
                    case BrewfestMusicAreasIds.Silvermoon:
                    case BrewfestMusicAreasIds.Undercity:
                    case BrewfestMusicAreasIds.Orgrimmar1:
                    case BrewfestMusicAreasIds.Orgrimmar2:
                    case BrewfestMusicAreasIds.Thunderbluff:
                        if (rnd == 0)
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Goblin01);
                            musicTime = BrewfestMusicConst.Goblin01Time;
                        }
                        else if (rnd == 1)
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Goblin02);
                            musicTime = BrewfestMusicConst.Goblin02Time;
                        }
                        else
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Goblin03);
                            musicTime = BrewfestMusicConst.Goblin03Time;
                        }
                        break;
                    // Alliance
                    case BrewfestMusicAreasIds.Ironforge1:
                    case BrewfestMusicAreasIds.Ironforge2:
                    case BrewfestMusicAreasIds.Stormwind:
                    case BrewfestMusicAreasIds.Exodar:
                    case BrewfestMusicAreasIds.Darnassus:
                        if (rnd == 0)
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Dwarf01);
                            musicTime = BrewfestMusicConst.Dwarf01Time;
                        }
                        else if (rnd == 1)
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Dwarf02);
                            musicTime = BrewfestMusicConst.Dwarf02Time;
                        }
                        else
                        {
                            me.PlayDirectMusic(BrewfestMusicConst.Dwarf03);
                            musicTime = BrewfestMusicConst.Dwarf03Time;
                        }
                        break;
                    // Neurtal
                    case BrewfestMusicAreasIds.Shattrath:
                        List<Unit> playersNearby = me.GetPlayerListInGrid(me.GetVisibilityRange());
                        foreach (Player player in playersNearby)
                        {
                            if (player.GetTeam() == Team.Horde)
                            {
                                if (rnd == 0)
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Goblin01);
                                    musicTime = BrewfestMusicConst.Goblin01Time;
                                }
                                else if (rnd == 1)
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Goblin02);
                                    musicTime = BrewfestMusicConst.Goblin02Time;
                                }
                                else
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Goblin03);
                                    musicTime = BrewfestMusicConst.Goblin03Time;
                                }
                            }
                            else
                            {
                                if (rnd == 0)
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Dwarf01);
                                    musicTime = BrewfestMusicConst.Dwarf01Time;
                                }
                                else if (rnd == 1)
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Dwarf02);
                                    musicTime = BrewfestMusicConst.Dwarf02Time;
                                }
                                else
                                {
                                    me.PlayDirectMusic(BrewfestMusicConst.Dwarf03);
                                    musicTime = BrewfestMusicConst.Dwarf03Time;
                                }
                            }
                        }
                        break;
                }

                task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SmsgPlayMusic packet (PlayDirectMusic) is pushed to the client
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class go_midsummer_music : GameObjectAI
    {
        const uint EventmidsummerfirefestivalA = 12319; // 1.08 min
        const uint EventmidsummerfirefestivalH = 12325; // 1.12 min

        public go_midsummer_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (!GameEventMgr.IsHolidayActive(HolidayIds.MidsummerFireFestival))
                    return;

                List<Unit> playersNearby = me.GetPlayerListInGrid(me.GetVisibilityRange());
                foreach (Player player in playersNearby)
                {
                    if (player.GetTeam() == Team.Horde)
                        me.PlayDirectMusic(EventmidsummerfirefestivalH, player);
                    else
                        me.PlayDirectMusic(EventmidsummerfirefestivalA, player);
                }
                task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SmsgPlayMusic packet (PlayDirectMusic) is pushed to the client (sniffed value)
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class go_darkmoon_faire_music : GameObjectAI
    {
        const uint MusicDarkmoonFaireMusic = 8440;

        public go_darkmoon_faire_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (!GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaire))
                    return;

                me.PlayDirectMusic(MusicDarkmoonFaireMusic);
                task.Repeat(TimeSpan.FromSeconds(5));  // Every 5 second's SmsgPlayMusic packet (PlayDirectMusic) is pushed to the client (sniffed value)
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class go_pirate_day_music : GameObjectAI
    {
        const uint MusicPirateDayMusic = 12845;

        public go_pirate_day_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (!GameEventMgr.IsHolidayActive(HolidayIds.PiratesDay))
                    return;

                me.PlayDirectMusic(MusicPirateDayMusic);
                task.Repeat(TimeSpan.FromSeconds(5));  // Every 5 second's SmsgPlayMusic packet (PlayDirectMusic) is pushed to the client (sniffed value)
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }


    struct BellHourlyConst
    {
        public const uint Belltollhorde = 6595; // Undercity
        public const uint Belltolltribal = 6675; // Orgrimma/Thunderbluff
        public const uint Belltollalliance = 6594; // Stormwind
        public const uint Belltollnightelf = 6674; // Darnassus
        public const uint Belltolldwarfgnome = 7234; // Ironforge
        public const uint Belltollkharazhan = 9154;  // Kharazhan

        public const uint GoHordeBell = 175885;
        public const uint GoAllianceBell = 176573;
        public const uint GoKharazhanBell = 182064;

        public const uint GameEventHourlyBells = 73;
        public const uint EventRingBell = 1;
    }

    enum BellHourlySoundZoneIds
    {
        TirisfalZone = 85,
        UndercityZone = 1497,
        DunMoroghZone = 1,
        IronforgeZone = 1537,
        TeldrassilZone = 141,
        DarnassusZone = 1657,
        AshenvaleZone = 331,
        HillsbradFoothillsZone = 267,
        DuskwoodZone = 10
    }

    [Script]
    class go_bells : GameObjectAI
    {
        uint _soundId;

        public go_bells(GameObject go) : base(go) { }

        public override void InitializeAI()
        {
            uint zoneId = me.GetZoneId();

            switch (me.GetEntry())
            {
                case BellHourlyConst.GoHordeBell:
                {
                    switch ((BellHourlySoundZoneIds)zoneId)
                    {
                        case BellHourlySoundZoneIds.TirisfalZone:
                        case BellHourlySoundZoneIds.UndercityZone:
                        case BellHourlySoundZoneIds.HillsbradFoothillsZone:
                        case BellHourlySoundZoneIds.DuskwoodZone:
                            _soundId = BellHourlyConst.Belltollhorde;  // undead bell sound
                            break;
                        default:
                            _soundId = BellHourlyConst.Belltolltribal; // orc drum sound
                            break;
                    }
                    break;
                }
                case BellHourlyConst.GoAllianceBell:
                {
                    switch ((BellHourlySoundZoneIds)zoneId)
                    {
                        case BellHourlySoundZoneIds.IronforgeZone:
                        case BellHourlySoundZoneIds.DunMoroghZone:
                            _soundId = BellHourlyConst.Belltolldwarfgnome; // horn sound
                            break;
                        case BellHourlySoundZoneIds.DarnassusZone:
                        case BellHourlySoundZoneIds.TeldrassilZone:
                        case BellHourlySoundZoneIds.AshenvaleZone:
                            _soundId = BellHourlyConst.Belltollnightelf;   // nightelf bell sound
                            break;
                        default:
                            _soundId = BellHourlyConst.Belltollalliance;   // human bell sound
                            break;
                    }
                    break;
                }
                case BellHourlyConst.GoKharazhanBell:
                {
                    _soundId = BellHourlyConst.Belltollkharazhan;
                    break;
                }
            }
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            if (eventId == BellHourlyConst.GameEventHourlyBells && start)
            {
                var localTm = GameTime.GetDateAndTime();
                int _rings = localTm.Hour % 12;
                if (_rings == 0) // 00:00 and 12:00
                {
                    _rings = 12;
                }

                // Dwarf hourly horn should only play a Single time, each time the next hour begins.
                if (_soundId == BellHourlyConst.Belltolldwarfgnome)
                {
                    _rings = 1;
                }

                for (var i = 0; i < _rings; ++i)
                    _scheduler.Schedule(TimeSpan.FromSeconds(i * 4 + 1), task => me.PlayDirectSound(_soundId));
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}

