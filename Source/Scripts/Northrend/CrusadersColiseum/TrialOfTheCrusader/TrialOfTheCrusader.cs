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
using Framework.GameMath;
using Framework.IO;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheCrusader
{
    [Script]
    class instance_trial_of_the_crusader : InstanceMapScript
    {
        public instance_trial_of_the_crusader() : base("instance_trial_of_the_crusader", 649) { }

        class instance_trial_of_the_crusader_InstanceMapScript : InstanceScript
        {
            public instance_trial_of_the_crusader_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("TCR");
                SetBossNumber(DataTypes.MaxEncounters);
                LoadBossBoundaries(MiscData.boundaries);
                TrialCounter = 50;
                EventStage = 0;
                northrendBeasts = EncounterState.NotStarted;
                EventTimer = 1000;
                NotOneButTwoJormungarsTimer = 0;
                ResilienceWillFixItTimer = 0;
                SnoboldCount = 0;
                MistressOfPainCount = 0;
                TributeToImmortalityEligible = true;
                NeedSave = false;
            }

            public override bool IsEncounterInProgress()
            {
                for (byte i = 0; i < DataTypes.MaxEncounters; ++i)
                    if (GetBossState(i) == EncounterState.InProgress)
                        return true;

                // Special state is set at Faction Champions after first champ dead, encounter is still in combat
                if (GetBossState(DataTypes.BossCrusaders) == EncounterState.Special)
                    return true;

                return false;
            }

            public override void OnPlayerEnter(Player player)
            {
                if (instance.IsHeroic())
                {
                    player.SendUpdateWorldState(WorldStateIds.Show, 1);
                    player.SendUpdateWorldState(WorldStateIds.Count, GetData(DataTypes.Counter));
                }
                else
                    player.SendUpdateWorldState(WorldStateIds.Show, 0);

                // make sure Anub'arak isnt missing and floor is destroyed after a crash
                if (GetBossState(DataTypes.BossLichKing) == EncounterState.Done && TrialCounter != 0 && GetBossState(DataTypes.BossAnubarak) != EncounterState.Done)
                {
                    Creature anubArak = ObjectAccessor.GetCreature(player, GetGuidData(CreatureIds.Anubarak));
                    if (!anubArak)
                        anubArak = player.SummonCreature(CreatureIds.Anubarak, MiscData.AnubarakLoc[0].GetPositionX(), MiscData.AnubarakLoc[0].GetPositionY(), MiscData.AnubarakLoc[0].GetPositionZ(), 3, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);

                    GameObject floor = ObjectAccessor.GetGameObject(player, GetGuidData(GameObjectIds.ArgentColiseumFloor));
                    if (floor)
                        floor.SetDestructibleState(GameObjectDestructibleState.Damaged);
                }
            }

            void OpenDoor(ObjectGuid guid)
            {
                if (guid.IsEmpty())
                    return;

                GameObject go = instance.GetGameObject(guid);
                if (go)
                    go.SetGoState(GameObjectState.ActiveAlternative);
            }

            void CloseDoor(ObjectGuid guid)
            {
                if (guid.IsEmpty())
                    return;

                GameObject go = instance.GetGameObject(guid);
                if (go)
                    go.SetGoState(GameObjectState.Ready);
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case CreatureIds.Barrent:
                        BarrentGUID = creature.GetGUID();
                        if (TrialCounter == 0)
                            creature.DespawnOrUnsummon();
                        break;
                    case CreatureIds.Tirion:
                        TirionGUID = creature.GetGUID();
                        break;
                    case CreatureIds.TirionFordring:
                        TirionFordringGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Fizzlebang:
                        FizzlebangGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Garrosh:
                        GarroshGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Varian:
                        VarianGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Gormok:
                        GormokGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Acidmaw:
                        AcidmawGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Dreadscale:
                        DreadscaleGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Icehowl:
                        IcehowlGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Jaraxxus:
                        JaraxxusGUID = creature.GetGUID();
                        break;
                    case CreatureIds.ChampionsController:
                        ChampionsControllerGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Darkbane:
                        DarkbaneGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Lightbane:
                        LightbaneGUID = creature.GetGUID();
                        break;
                    case CreatureIds.Anubarak:
                        AnubarakGUID = creature.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case GameObjectIds.CrusadersCache10:
                        if (instance.GetDifficultyID() == Difficulty.Raid10N)
                            CrusadersCacheGUID = go.GetGUID();
                        break;
                    case GameObjectIds.CrusadersCache25:
                        if (instance.GetDifficultyID() == Difficulty.Raid25N)
                            CrusadersCacheGUID = go.GetGUID();
                        break;
                    case GameObjectIds.CrusadersCache10H:
                        if (instance.GetDifficultyID() == Difficulty.Raid10HC)
                            CrusadersCacheGUID = go.GetGUID();
                        break;
                    case GameObjectIds.CrusadersCache25H:
                        if (instance.GetDifficultyID() == Difficulty.Raid25HC)
                            CrusadersCacheGUID = go.GetGUID();
                        break;
                    case GameObjectIds.ArgentColiseumFloor:
                        FloorGUID = go.GetGUID();
                        break;
                    case GameObjectIds.MainGateDoor:
                        MainGateDoorGUID = go.GetGUID();
                        break;
                    case GameObjectIds.EastPortcullis:
                        EastPortcullisGUID = go.GetGUID();
                        break;
                    case GameObjectIds.WebDoor:
                        WebDoorGUID = go.GetGUID();
                        break;

                    case GameObjectIds.TributeChest10h25:
                    case GameObjectIds.TributeChest10h45:
                    case GameObjectIds.TributeChest10h50:
                    case GameObjectIds.TributeChest10h99:
                    case GameObjectIds.TributeChest25h25:
                    case GameObjectIds.TributeChest25h45:
                    case GameObjectIds.TributeChest25h50:
                    case GameObjectIds.TributeChest25h99:
                        TributeChestGUID = go.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                switch (type)
                {
                    case DataTypes.BossBeasts:
                        break;
                    case DataTypes.BossJaraxxus:
                        // Cleanup Icehowl
                        Creature icehowl = instance.GetCreature(IcehowlGUID);
                        if (icehowl)
                            icehowl.DespawnOrUnsummon();
                        if (state == EncounterState.Done)
                            EventStage = 2000;
                        break;
                    case DataTypes.BossCrusaders:
                        {
                            // Cleanup Jaraxxus
                            Creature jaraxxus = instance.GetCreature(JaraxxusGUID);
                            if (jaraxxus)
                                jaraxxus.DespawnOrUnsummon();

                            Creature fizzlebang = instance.GetCreature(FizzlebangGUID);
                            if (fizzlebang)
                                fizzlebang.DespawnOrUnsummon();

                            switch (state)
                            {
                                case EncounterState.InProgress:
                                    ResilienceWillFixItTimer = 0;
                                    break;
                                case EncounterState.Special: //Means the first blood
                                    ResilienceWillFixItTimer = 60 * Time.InMilliseconds;
                                    state = EncounterState.InProgress;
                                    break;
                                case EncounterState.Done:
                                    DoUpdateCriteria(CriteriaTypes.BeSpellTarget, AchievementData.SpellDefeatFactionChampions);
                                    if (ResilienceWillFixItTimer > 0)
                                        DoUpdateCriteria(CriteriaTypes.BeSpellTarget, AchievementData.SpellChampionsKilledInMinute);
                                    DoRespawnGameObject(CrusadersCacheGUID, 7 * Time.Day);

                                    GameObject cache = instance.GetGameObject(CrusadersCacheGUID);
                                    if (cache)
                                        cache.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.NotSelectable);
                                    EventStage = 3100;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }
                    case DataTypes.BossValkiries:
                        {
                            // Cleanup chest
                            GameObject cache = instance.GetGameObject(CrusadersCacheGUID);
                            if (cache)
                                cache.Delete();
                            switch (state)
                            {
                                case EncounterState.Fail:
                                    if (GetBossState(DataTypes.BossValkiries) == EncounterState.NotStarted)
                                        state = EncounterState.NotStarted;
                                    break;
                                case EncounterState.Special:
                                    if (GetBossState(DataTypes.BossValkiries) == EncounterState.Special)
                                        state = EncounterState.Done;
                                    break;
                                case EncounterState.Done:
                                    if (instance.GetPlayers()[0].GetTeam() == Team.Alliance)
                                        EventStage = 4020;
                                    else
                                        EventStage = 4030;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        }
                    case DataTypes.BossLichKing:
                        break;
                    case DataTypes.BossAnubarak:
                        switch (state)
                        {
                            case EncounterState.Done:
                                {
                                    EventStage = 6000;
                                    uint tributeChest = 0;
                                    if (instance.GetDifficultyID() == Difficulty.Raid10HC)
                                    {
                                        if (TrialCounter >= 50)
                                            tributeChest = GameObjectIds.TributeChest10h99;
                                        else
                                        {
                                            if (TrialCounter >= 45)
                                                tributeChest = GameObjectIds.TributeChest10h50;
                                            else
                                            {
                                                if (TrialCounter >= 25)
                                                    tributeChest = GameObjectIds.TributeChest10h45;
                                                else
                                                    tributeChest = GameObjectIds.TributeChest10h25;
                                            }
                                        }
                                    }
                                    else if (instance.GetDifficultyID() == Difficulty.Raid25HC)
                                    {
                                        if (TrialCounter >= 50)
                                            tributeChest = GameObjectIds.TributeChest25h99;
                                        else
                                        {
                                            if (TrialCounter >= 45)
                                                tributeChest = GameObjectIds.TributeChest25h50;
                                            else
                                            {
                                                if (TrialCounter >= 25)
                                                    tributeChest = GameObjectIds.TributeChest25h45;
                                                else
                                                    tributeChest = GameObjectIds.TributeChest25h25;
                                            }
                                        }
                                    }

                                    if (tributeChest != 0)
                                    {
                                        Creature tirion = instance.GetCreature(TirionGUID);
                                        if (tirion)
                                        {
                                            GameObject chest = tirion.SummonGameObject(tributeChest, 805.62f, 134.87f, 142.16f, 3.27f, Quaternion.fromEulerAnglesZYX(3.27f, 0.0f, 0.0f), Time.Week);
                                            if (chest)
                                                chest.SetRespawnTime((int)chest.GetRespawnDelay());
                                        }
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                if (IsEncounterInProgress())
                {
                    CloseDoor(GetGuidData(GameObjectIds.EastPortcullis));
                    CloseDoor(GetGuidData(GameObjectIds.WebDoor));
                }
                else
                {
                    OpenDoor(GetGuidData(GameObjectIds.EastPortcullis));
                    OpenDoor(GetGuidData(GameObjectIds.WebDoor));
                }

                if (type < DataTypes.MaxEncounters)
                {
                    Log.outInfo(LogFilter.Scripts, "[ToCr] BossState(type {0}) {1} = state {2};", type, GetBossState(type), state);
                    if (state == EncounterState.Fail)
                    {
                        if (instance.IsHeroic())
                        {
                            --TrialCounter;
                            // decrease attempt counter at wipe
                            var PlayerList = instance.GetPlayers();
                            foreach (var player in PlayerList)
                                player.SendUpdateWorldState(WorldStateIds.Count, TrialCounter);

                            // if theres no more attemps allowed
                            if (TrialCounter == 0)
                            {
                                Unit announcer = instance.GetCreature(GetGuidData(CreatureIds.Barrent));
                                if (announcer)
                                    announcer.ToCreature().DespawnOrUnsummon();

                                Creature anubArak = instance.GetCreature(GetGuidData(CreatureIds.Anubarak));
                                if (anubArak)
                                    anubArak.DespawnOrUnsummon();
                            }
                        }
                        NeedSave = true;
                        EventStage = (uint)(type == DataTypes.BossBeasts ? 666 : 0);
                        state = EncounterState.NotStarted;
                    }

                    if (state == EncounterState.Done || NeedSave)
                    {
                        Unit announcer = instance.GetCreature(GetGuidData(CreatureIds.Barrent));
                        if (announcer)
                            announcer.SetFlag64(UnitFields.NpcFlags, NPCFlags.Gossip);
                        Save();
                    }
                }
                return true;
            }

            public override void SetData(uint type, uint data)
            {
                switch (type)
                {
                    case DataTypes.Counter:
                        TrialCounter = data;
                        data = (uint)EncounterState.Done;
                        break;
                    case DataTypes.Event:
                        EventStage = data;
                        data = (uint)EncounterState.NotStarted;
                        break;
                    case DataTypes.EventTimer:
                        EventTimer = data;
                        data = (uint)EncounterState.NotStarted;
                        break;
                    case DataTypes.NorthrendBeasts:
                        northrendBeasts = (EncounterState)data;
                        switch (data)
                        {
                            case NorthrendBeasts.GormokDone:
                                EventStage = 200;
                                SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.InProgress);
                                break;
                            case NorthrendBeasts.SnakesInProgress:
                                NotOneButTwoJormungarsTimer = 0;
                                break;
                            case NorthrendBeasts.SnakesSpecial:
                                NotOneButTwoJormungarsTimer = 10 * Time.InMilliseconds;
                                break;
                            case NorthrendBeasts.SnakesDone:
                                if (NotOneButTwoJormungarsTimer > 0)
                                    DoUpdateCriteria(CriteriaTypes.BeSpellTarget, AchievementData.SpellWormsKilledIn10Seconds);
                                EventStage = 300;
                                SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.InProgress);
                                break;
                            case NorthrendBeasts.IcehowlDone:
                                EventStage = 400;
                                SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.Done);
                                SetBossState(DataTypes.BossBeasts, EncounterState.Done);
                                break;
                            case (uint)EncounterState.Fail:
                                SetBossState(DataTypes.BossBeasts, EncounterState.Fail);
                                break;
                            default:
                                break;
                        }
                        break;
                    //Achievements
                    case DataTypes.SnoboldCount:
                        if (data == DataTypes.Increase)
                            ++SnoboldCount;
                        else if (data == DataTypes.Decrease)
                            --SnoboldCount;
                        break;
                    case DataTypes.MistressOfPainCount:
                        if (data == DataTypes.Increase)
                            ++MistressOfPainCount;
                        else if (data == DataTypes.Decrease)
                            --MistressOfPainCount;
                        break;
                    case DataTypes.TributeToImmortalityEligible:
                        TributeToImmortalityEligible = false;
                        break;
                    default:
                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint type)
            {
                switch (type)
                {
                    case CreatureIds.Barrent:
                        return BarrentGUID;
                    case CreatureIds.Tirion:
                        return TirionGUID;
                    case CreatureIds.TirionFordring:
                        return TirionFordringGUID;
                    case CreatureIds.Fizzlebang:
                        return FizzlebangGUID;
                    case CreatureIds.Garrosh:
                        return GarroshGUID;
                    case CreatureIds.Varian:
                        return VarianGUID;
                    case CreatureIds.Gormok:
                        return GormokGUID;
                    case CreatureIds.Acidmaw:
                        return AcidmawGUID;
                    case CreatureIds.Dreadscale:
                        return DreadscaleGUID;
                    case CreatureIds.Icehowl:
                        return IcehowlGUID;
                    case CreatureIds.Jaraxxus:
                        return JaraxxusGUID;
                    case CreatureIds.ChampionsController:
                        return ChampionsControllerGUID;
                    case CreatureIds.Darkbane:
                        return DarkbaneGUID;
                    case CreatureIds.Lightbane:
                        return LightbaneGUID;
                    case CreatureIds.Anubarak:
                        return AnubarakGUID;
                    case GameObjectIds.ArgentColiseumFloor:
                        return FloorGUID;
                    case GameObjectIds.MainGateDoor:
                        return MainGateDoorGUID;
                    case GameObjectIds.EastPortcullis:
                        return EastPortcullisGUID;
                    case GameObjectIds.WebDoor:
                        return WebDoorGUID;
                    default:
                        break;
                }

                return ObjectGuid.Empty;
            }

            public override uint GetData(uint type)
            {
                switch (type)
                {
                    case DataTypes.Counter:
                        return TrialCounter;
                    case DataTypes.Event:
                        return EventStage;
                    case DataTypes.NorthrendBeasts:
                        return (uint)northrendBeasts;
                    case DataTypes.EventTimer:
                        return EventTimer;
                    case DataTypes.EventNpc:
                        switch (EventStage)
                        {
                            case 110:
                            case 140:
                            case 150:
                            case 155:
                            case 200:
                            case 205:
                            case 210:
                            case 220:
                            case 300:
                            case 305:
                            case 310:
                            case 315:
                            case 400:
                            case 666:
                            case 1010:
                            case 1180:
                            case 2000:
                            case 2030:
                            case 3000:
                            case 3001:
                            case 3060:
                            case 3061:
                            case 3090:
                            case 3091:
                            case 3092:
                            case 3100:
                            case 3110:
                            case 4000:
                            case 4010:
                            case 4015:
                            case 4016:
                            case 4040:
                            case 4050:
                            case 5000:
                            case 5005:
                            case 5020:
                            case 6000:
                            case 6005:
                            case 6010:
                                return CreatureIds.Tirion;
                            case 5010:
                            case 5030:
                            case 5040:
                            case 5050:
                            case 5060:
                            case 5070:
                            case 5080:
                                return CreatureIds.LichKing;
                            case 120:
                            case 122:
                            case 2020:
                            case 3080:
                            case 3051:
                            case 3071:
                            case 4020:
                                return CreatureIds.Varian;
                            case 130:
                            case 132:
                            case 2010:
                            case 3050:
                            case 3070:
                            case 3081:
                            case 4030:
                                return CreatureIds.Garrosh;
                            case 1110:
                            case 1120:
                            case 1130:
                            case 1132:
                            case 1134:
                            case 1135:
                            case 1140:
                            case 1142:
                            case 1144:
                            case 1150:
                                return CreatureIds.Fizzlebang;
                            default:
                                return CreatureIds.Tirion;
                        }
                    default:
                        break;
                }

                return 0;
            }

            public override void Update(uint diff)
            {
                if (GetData(DataTypes.NorthrendBeasts) == NorthrendBeasts.SnakesSpecial && NotOneButTwoJormungarsTimer != 0)
                {
                    if (NotOneButTwoJormungarsTimer <= diff)
                        NotOneButTwoJormungarsTimer = 0;
                    else
                        NotOneButTwoJormungarsTimer -= diff;
                }

                if (GetBossState(DataTypes.BossCrusaders) == EncounterState.Special && ResilienceWillFixItTimer != 0)
                {
                    if (ResilienceWillFixItTimer <= diff)
                        ResilienceWillFixItTimer = 0;
                    else
                        ResilienceWillFixItTimer -= diff;
                }
            }

            void Save()
            {
                OUT_SAVE_INST_DATA();

                string saveStream = "";

                for (byte i = 0; i < DataTypes.MaxEncounters; ++i)
                    saveStream += GetBossState(i) + ' ';

                saveStream += TrialCounter;
                SaveDataBuffer = saveStream;

                SaveToDB();
                OUT_SAVE_INST_DATA_COMPLETE();
                NeedSave = false;
            }

            public override string GetSaveData()
            {
                return SaveDataBuffer;
            }

            public override void Load(string strIn)
            {
                if (string.IsNullOrEmpty(strIn))
                {
                    OUT_LOAD_INST_DATA_FAIL();
                    return;
                }

                OUT_LOAD_INST_DATA(strIn);

                StringArguments loadStream = new StringArguments(strIn);

                for (byte i = 0; i < DataTypes.MaxEncounters; ++i)
                {
                    EncounterState tmpState = (EncounterState)loadStream.NextUInt32();
                    if (tmpState == EncounterState.InProgress || tmpState > EncounterState.Special)
                        tmpState = EncounterState.NotStarted;
                    SetBossState(i, tmpState);
                }

                TrialCounter = loadStream.NextUInt32();
                EventStage = 0;

                OUT_LOAD_INST_DATA_COMPLETE();
            }

            public override bool CheckAchievementCriteriaMeet(uint criteria_id, Player source, Unit target, uint miscvalue1)
            {
                switch (criteria_id)
                {
                    case AchievementData.UpperBackPain10Player:
                    case AchievementData.UpperBackPain10PlayerHeroic:
                        return SnoboldCount >= 2;
                    case AchievementData.UpperBackPain25Player:
                    case AchievementData.UpperBackPain25PlayerHeroic:
                        return SnoboldCount >= 4;
                    case AchievementData.ThreeSixtyPainSpike10Player:
                    case AchievementData.ThreeSixtyPainSpike10PlayerHeroic:
                    case AchievementData.ThreeSixtyPainSpike25Player:
                    case AchievementData.ThreeSixtyPainSpike25PlayerHeroic:
                        return MistressOfPainCount >= 2;
                    case AchievementData.ATributeToSkill10Player:
                    case AchievementData.ATributeToSkill25Player:
                        return TrialCounter >= 25;
                    case AchievementData.ATributeToMadSkill10Player:
                    case AchievementData.ATributeToMadSkill25Player:
                        return TrialCounter >= 45;
                    case AchievementData.ATributeToInsanity10Player:
                    case AchievementData.ATributeToInsanity25Player:
                    case AchievementData.RealmFirstGrandCrusader:
                        return TrialCounter == 50;
                    case AchievementData.ATributeToImmortalityAlliance:
                    case AchievementData.ATributeToImmortalityHorde:
                        return TrialCounter == 50 && TributeToImmortalityEligible;
                    case AchievementData.ATributeToDedicatedInsanity:
                        return false/*uiGrandCrusaderAttemptsLeft == 50 && !bHasAtAnyStagePlayerEquippedTooGoodItem*/;
                    default:
                        break;
                }

                return false;
            }

            uint TrialCounter;
            uint EventStage;
            uint EventTimer;
            EncounterState northrendBeasts;
            bool NeedSave;
            string SaveDataBuffer;

            ObjectGuid BarrentGUID;
            ObjectGuid TirionGUID;
            ObjectGuid TirionFordringGUID;
            ObjectGuid FizzlebangGUID;
            ObjectGuid GarroshGUID;
            ObjectGuid VarianGUID;

            ObjectGuid GormokGUID;
            ObjectGuid AcidmawGUID;
            ObjectGuid DreadscaleGUID;
            ObjectGuid IcehowlGUID;
            ObjectGuid JaraxxusGUID;
            ObjectGuid ChampionsControllerGUID;
            ObjectGuid DarkbaneGUID;
            ObjectGuid LightbaneGUID;
            ObjectGuid AnubarakGUID;

            ObjectGuid CrusadersCacheGUID;
            ObjectGuid FloorGUID;
            ObjectGuid TributeChestGUID;
            ObjectGuid MainGateDoorGUID;
            ObjectGuid EastPortcullisGUID;
            ObjectGuid WebDoorGUID;

            // Achievement stuff
            uint NotOneButTwoJormungarsTimer;
            uint ResilienceWillFixItTimer;
            byte SnoboldCount;
            byte MistressOfPainCount;
            bool TributeToImmortalityEligible;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_trial_of_the_crusader_InstanceMapScript(map);
        }
    }

    [Script]
    class npc_announcer_toc10 : CreatureScript
    {
        public npc_announcer_toc10() : base("npc_announcer_toc10") { }

        class npc_announcer_toc10AI : ScriptedAI
        {
            public npc_announcer_toc10AI(Creature creature) : base(creature) { }

            public override void Reset()
            {
                me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                Creature pAlly = GetClosestCreatureWithEntry(me, CreatureIds.Thrall, 300.0f);
                if (pAlly)
                    pAlly.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);

                pAlly = GetClosestCreatureWithEntry(me, CreatureIds.Proudmoore, 300.0f);
                if (pAlly)
                    pAlly.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
            }

            public override void AttackStart(Unit who) { }
        }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            InstanceScript instance = creature.GetInstanceScript();
            if (instance == null)
                return true;

            string _message = "We are ready!";

            if (player.IsInCombat() || instance.IsEncounterInProgress() || instance.GetData(DataTypes.Event) != 0)
                return true;

            byte i = 0;
            for (; i < MiscData._GossipMessage.Length; ++i)
            {
                if ((!MiscData._GossipMessage[i].state && instance.GetBossState(MiscData._GossipMessage[i].encounter) != EncounterState.Done)
                    || (MiscData._GossipMessage[i].state && instance.GetBossState(MiscData._GossipMessage[i].encounter) == EncounterState.Done))
                {
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, _message, eTradeskill.GossipSenderMain, MiscData._GossipMessage[i].id);
                    break;
                }
            }

            if (i >= MiscData._GossipMessage.Length)
                return false;

            player.SEND_GOSSIP_MENU((uint)MiscData._GossipMessage[i].msgnum, creature.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, Creature creature, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            player.CLOSE_GOSSIP_MENU();
            InstanceScript instance = creature.GetInstanceScript();
            if (instance == null)
                return true;

            if (instance.GetBossState(DataTypes.BossBeasts) != EncounterState.Done)
            {
                instance.SetData(DataTypes.Event, 110);
                instance.SetData(DataTypes.NorthrendBeasts, (uint)EncounterState.NotStarted);
                instance.SetBossState(DataTypes.BossBeasts, EncounterState.NotStarted);
            }
            else if (instance.GetBossState(DataTypes.BossJaraxxus) != EncounterState.Done)
            {
                // if Jaraxxus is spawned, but the raid wiped
                Creature jaraxxus = ObjectAccessor.GetCreature(player, instance.GetGuidData(CreatureIds.Jaraxxus));
                if (jaraxxus)
                {
                    jaraxxus.RemoveAurasDueToSpell(Spells.JaraxxusChains);
                    jaraxxus.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                    jaraxxus.SetReactState(ReactStates.Defensive);
                    jaraxxus.SetInCombatWithZone();
                }
                else
                {
                    instance.SetData(DataTypes.Event, 1010);
                    instance.SetBossState(DataTypes.BossJaraxxus, EncounterState.NotStarted);
                }
            }
            else if (instance.GetBossState(DataTypes.BossCrusaders) != EncounterState.Done)
            {
                if (player.GetTeam() == Team.Alliance)
                    instance.SetData(DataTypes.Event, 3000);
                else
                    instance.SetData(DataTypes.Event, 3001);
                instance.SetBossState(DataTypes.BossCrusaders, EncounterState.NotStarted);
            }
            else if (instance.GetBossState(DataTypes.BossValkiries) != EncounterState.Done)
            {
                instance.SetData(DataTypes.Event, 4000);
                instance.SetBossState(DataTypes.BossValkiries, EncounterState.NotStarted);
            }
            else if (instance.GetBossState(DataTypes.BossLichKing) != EncounterState.Done)
            {
                GameObject floor = ObjectAccessor.GetGameObject(player, instance.GetGuidData(GameObjectIds.ArgentColiseumFloor));
                if (floor)
                    floor.SetDestructibleState(GameObjectDestructibleState.Damaged);

                creature.CastSpell(creature, Spells.CorpseTeleport, false);
                creature.CastSpell(creature, Spells.DestroyFloorKnockup, false);

                Creature anubArak = ObjectAccessor.GetCreature(creature, instance.GetGuidData(CreatureIds.Anubarak));
                if (!anubArak || !anubArak.IsAlive())
                    anubArak = creature.SummonCreature(CreatureIds.Anubarak, MiscData.AnubarakLoc[0].GetPositionX(), MiscData.AnubarakLoc[0].GetPositionY(), MiscData.AnubarakLoc[0].GetPositionZ(), 3, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);

                instance.SetBossState(DataTypes.BossAnubarak, EncounterState.NotStarted);

                if (creature.IsVisible())
                    creature.SetVisible(false);
            }
            creature.RemoveFlag64(UnitFields.NpcFlags, NPCFlags.Gossip);
            return true;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_announcer_toc10AI>(creature);
        }
    }

    [Script]
    class boss_lich_king_toc : ScriptedAI
    {
        public boss_lich_king_toc(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _updateTimer = 0;
            me.SetReactState(ReactStates.Passive);
            Creature summoned = me.SummonCreature(CreatureIds.Trigger, MiscData.ToCCommonLoc[2].GetPositionX(), MiscData.ToCCommonLoc[2].GetPositionY(), MiscData.ToCCommonLoc[2].GetPositionZ(), 5, TempSummonType.TimedDespawn, 1 * Time.Minute * Time.InMilliseconds);
            if (summoned)
            {
                summoned.CastSpell(summoned, 51807, false);
                summoned.SetDisplayFromModel(1);
            }

            _instance.SetBossState(DataTypes.BossLichKing, EncounterState.InProgress);
            me.SetWalk(true);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point || _instance == null)
                return;

            switch (id)
            {
                case 0:
                    _instance.SetData(DataTypes.Event, 5030);
                    break;
                case 1:
                    _instance.SetData(DataTypes.Event, 5050);
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (_instance == null)
                return;

            if (_instance.GetData(DataTypes.EventNpc) != CreatureIds.LichKing)
                return;

            _updateTimer = _instance.GetData(DataTypes.EventTimer);
            if (_updateTimer <= uiDiff)
            {
                switch (_instance.GetData(DataTypes.Event))
                {
                    case 5010:
                        Talk(Texts.Stage_4_02);
                        _updateTimer = 3 * Time.InMilliseconds;
                        me.GetMotionMaster().MovePoint(0, MiscData.LichKingLoc[0]);
                        _instance.SetData(DataTypes.Event, 5020);
                        break;
                    case 5030:
                        Talk(Texts.Stage_4_04);
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.StateTalk);
                        _updateTimer = 10 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 5040);
                        break;
                    case 5040:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);
                        me.GetMotionMaster().MovePoint(1, MiscData.LichKingLoc[1]);
                        _updateTimer = 1 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 5050:
                        me.HandleEmoteCommand(Emote.OneshotExclamation);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 5060);
                        break;
                    case 5060:
                        Talk(Texts.Stage_4_05);
                        me.HandleEmoteCommand(Emote.OneshotKneel);
                        _updateTimer = (uint)(2.5 * Time.InMilliseconds);
                        _instance.SetData(DataTypes.Event, 5070);
                        break;
                    case 5070:
                        me.CastSpell(me, 68198, false);
                        _updateTimer = (uint)(1.5 * Time.InMilliseconds);
                        _instance.SetData(DataTypes.Event, 5080);
                        break;
                    case 5080:
                        {
                            GameObject go = ObjectAccessor.GetGameObject(me, _instance.GetGuidData(GameObjectIds.ArgentColiseumFloor));
                            if (go)
                            {
                                go.SetDisplayId(MiscData.DisplayIdDestroyedFloor);
                                go.SetFlag(GameObjectFields.Flags, GameObjectFlags.Damaged | GameObjectFlags.NoDespawn);
                                go.SetGoState(GameObjectState.Active);
                            }

                            me.CastSpell(me, Spells.CorpseTeleport, false);
                            me.CastSpell(me, Spells.DestroyFloorKnockup, false);

                            _instance.SetBossState(DataTypes.BossLichKing, EncounterState.Done);
                            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Anubarak));
                            if (!temp || !temp.IsAlive())
                                temp = me.SummonCreature(CreatureIds.Anubarak, MiscData.AnubarakLoc[0].GetPositionX(), MiscData.AnubarakLoc[0].GetPositionY(), MiscData.AnubarakLoc[0].GetPositionZ(), 3, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);

                            _instance.SetData(DataTypes.Event, 0);

                            me.DespawnOrUnsummon();
                            _updateTimer = 20 * Time.InMilliseconds;
                            break;
                        }
                    default:
                        break;
                }
            }
            else
                _updateTimer -= uiDiff;

            _instance.SetData(DataTypes.EventTimer, _updateTimer);
        }

        InstanceScript _instance;
        uint _updateTimer;
    }

    [Script]
    class npc_fizzlebang_toc : ScriptedAI
    {
        public npc_fizzlebang_toc(Creature creature) : base(creature)
        {
            _summons = new SummonList(me);
            _instance = me.GetInstanceScript();
        }

        public override void JustDied(Unit killer)
        {
            Talk(Texts.Stage_1_06, killer);
            _instance.SetData(DataTypes.Event, 1180);
            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Jaraxxus));
            if (temp)
            {
                temp.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                temp.SetReactState(ReactStates.Aggressive);
                temp.SetInCombatWithZone();
            }
        }

        public override void Reset()
        {
            me.SetWalk(true);
            _portalGUID.Clear();
            me.GetMotionMaster().MovePoint(1, MiscData.ToCCommonLoc[10].GetPositionX(), MiscData.ToCCommonLoc[10].GetPositionY() - 60, MiscData.ToCCommonLoc[10].GetPositionZ());
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;

            switch (id)
            {
                case 1:
                    me.SetWalk(false);
                    _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                    _instance.SetData(DataTypes.Event, 1120);
                    _instance.SetData(DataTypes.EventTimer, 1 * Time.InMilliseconds);
                    break;
                default:
                    break;
            }
        }

        public override void JustSummoned(Creature summoned)
        {
            _summons.Summon(summoned);
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (_instance == null)
                return;

            if (_instance.GetData(DataTypes.EventNpc) != CreatureIds.Fizzlebang)
                return;

            _updateTimer = _instance.GetData(DataTypes.EventTimer);
            if (_updateTimer <= uiDiff)
            {
                switch (_instance.GetData(DataTypes.Event))
                {
                    case 1110:
                        _instance.SetData(DataTypes.Event, 1120);
                        _updateTimer = 4 * Time.InMilliseconds;
                        break;
                    case 1120:
                        Talk(Texts.Stage_1_02);
                        _instance.SetData(DataTypes.Event, 1130);
                        _updateTimer = 12 * Time.InMilliseconds;
                        break;
                    case 1130:
                        {
                            me.GetMotionMaster().MovementExpired();
                            Talk(Texts.Stage_1_03);
                            me.HandleEmoteCommand(Emote.OneshotSpellCastOmni);
                            Creature pTrigger = me.SummonCreature(CreatureIds.Trigger, MiscData.ToCCommonLoc[1].GetPositionX(), MiscData.ToCCommonLoc[1].GetPositionY(), MiscData.ToCCommonLoc[1].GetPositionZ(), 4.69494f, TempSummonType.ManualDespawn);
                            if (pTrigger)
                            {
                                _triggerGUID = pTrigger.GetGUID();
                                pTrigger.SetObjectScale(2.0f);
                                pTrigger.SetDisplayFromModel(0);
                                pTrigger.CastSpell(pTrigger, Spells.WilfredPortal, false);
                            }
                            _instance.SetData(DataTypes.Event, 1132);
                            _updateTimer = 4 * Time.InMilliseconds;
                            break;
                        }
                    case 1132:
                        me.GetMotionMaster().MovementExpired();
                        _instance.SetData(DataTypes.Event, 1134);
                        _updateTimer = 4 * Time.InMilliseconds;
                        break;
                    case 1134:
                        {
                            me.HandleEmoteCommand(Emote.OneshotSpellCastOmni);
                            Creature pPortal = me.SummonCreature(CreatureIds.WilfredPortal, MiscData.ToCCommonLoc[1].GetPositionX(), MiscData.ToCCommonLoc[1].GetPositionY(), MiscData.ToCCommonLoc[1].GetPositionZ(), 4.71239f, TempSummonType.ManualDespawn);
                            if (pPortal)
                            {
                                pPortal.SetReactState(ReactStates.Passive);
                                pPortal.SetObjectScale(2.0f);
                                pPortal.CastSpell(pPortal, Spells.WilfredPortal, false);
                                _portalGUID = pPortal.GetGUID();
                            }
                            _updateTimer = 4 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 1135);
                            break;
                        }
                    case 1135:
                        _instance.SetData(DataTypes.Event, 1140);
                        _updateTimer = 3 * Time.InMilliseconds;
                        break;
                    case 1140:
                        {
                            Talk(Texts.Stage_1_04);
                            Creature temp = me.SummonCreature(CreatureIds.Jaraxxus, MiscData.ToCCommonLoc[1].GetPositionX(), MiscData.ToCCommonLoc[1].GetPositionY(), MiscData.ToCCommonLoc[1].GetPositionZ(), 5.0f, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);
                            if (temp)
                            {
                                temp.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                                temp.SetReactState(ReactStates.Passive);
                                temp.GetMotionMaster().MovePoint(0, MiscData.ToCCommonLoc[1].GetPositionX(), MiscData.ToCCommonLoc[1].GetPositionY() - 10, MiscData.ToCCommonLoc[1].GetPositionZ());
                            }
                            _instance.SetData(DataTypes.Event, 1142);
                            _updateTimer = 5 * Time.InMilliseconds;
                            break;
                        }
                    case 1142:
                        {
                            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Jaraxxus));
                            if (temp)
                                temp.SetTarget(me.GetGUID());

                            Creature pTrigger = ObjectAccessor.GetCreature(me, _triggerGUID);
                            if (pTrigger)
                                pTrigger.DespawnOrUnsummon();

                            Creature pPortal = ObjectAccessor.GetCreature(me, _portalGUID);
                            if (pPortal)
                                pPortal.DespawnOrUnsummon();
                            _instance.SetData(DataTypes.Event, 1144);
                            _updateTimer = 10 * Time.InMilliseconds;
                            break;
                        }
                    case 1144:
                        {
                            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Jaraxxus));
                            if (temp)
                                temp.GetAI().Talk(Texts.Stage_1_05);
                            _instance.SetData(DataTypes.Event, 1150);
                            _updateTimer = 5 * Time.InMilliseconds;
                            break;
                        }
                    case 1150:
                        {
                            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Jaraxxus));
                            if (temp)
                            {
                                //1-shot Fizzlebang
                                temp.CastSpell(me, 67888, false);
                                me.SetInCombatWith(temp);
                                temp.AddThreat(me, 1000.0f);
                                temp.GetAI().AttackStart(me);
                            }
                            _instance.SetData(DataTypes.Event, 1160);
                            _updateTimer = 3 * Time.InMilliseconds;
                            break;
                        }
                }
            }
            else
                _updateTimer -= uiDiff;
            _instance.SetData(DataTypes.EventTimer, _updateTimer);
        }

        InstanceScript _instance;
        SummonList _summons;
        uint _updateTimer;
        ObjectGuid _portalGUID;
        ObjectGuid _triggerGUID;
    }

    [Script]
    class npc_tirion_toc : ScriptedAI
    {
        public npc_tirion_toc(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
        }

        public override void Reset() { }

        public override void AttackStart(Unit who) { }

        public override void UpdateAI(uint uiDiff)
        {
            if (_instance == null)
                return;

            if (_instance.GetData(DataTypes.EventNpc) != CreatureIds.Tirion)
                return;

            _updateTimer = _instance.GetData(DataTypes.EventTimer);
            if (_updateTimer <= uiDiff)
            {
                switch (_instance.GetData(DataTypes.Event))
                {
                    case 110:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotTalk);
                        Talk(Texts.Stage_0_01);
                        _updateTimer = 22 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 120);
                        break;
                    case 140:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotTalk);
                        Talk(Texts.Stage_0_02);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 150);
                        break;
                    case 150:
                        {
                            me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);
                            if (_instance.GetBossState(DataTypes.BossBeasts) != EncounterState.Done)
                            {
                                _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));

                                Creature temp = me.SummonCreature(CreatureIds.Gormok, MiscData.ToCSpawnLoc[0].GetPositionX(), MiscData.ToCSpawnLoc[0].GetPositionY(), MiscData.ToCSpawnLoc[0].GetPositionZ(), 5, TempSummonType.CorpseTimedDespawn, 30 * Time.InMilliseconds);
                                if (temp)
                                {
                                    temp.GetMotionMaster().MovePoint(0, MiscData.ToCCommonLoc[5].GetPositionX(), MiscData.ToCCommonLoc[5].GetPositionY(), MiscData.ToCCommonLoc[5].GetPositionZ());
                                    temp.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                                    temp.SetReactState(ReactStates.Passive);
                                }
                            }
                            _updateTimer = 3 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 155);
                            break;
                        }
                    case 155:
                        // keep the raid in combat for the whole encounter, pauses included
                        me.SetInCombatWithZone();
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 160);
                        break;
                    case 200:
                        {
                            Talk(Texts.Stage_0_04);
                            if (_instance.GetBossState(DataTypes.BossBeasts) != EncounterState.Done)
                            {
                                _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                                Creature temp = me.SummonCreature(CreatureIds.Dreadscale, MiscData.ToCSpawnLoc[1].GetPositionX(), MiscData.ToCSpawnLoc[1].GetPositionY(), MiscData.ToCSpawnLoc[1].GetPositionZ(), 5, TempSummonType.ManualDespawn);
                                if (temp)
                                {
                                    temp.GetMotionMaster().MovePoint(0, MiscData.ToCCommonLoc[5].GetPositionX(), MiscData.ToCCommonLoc[5].GetPositionY(), MiscData.ToCCommonLoc[5].GetPositionZ());
                                    temp.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                                    temp.SetReactState(ReactStates.Passive);
                                }
                            }
                            _updateTimer = 5 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 220);
                            break;
                        }
                    case 220:
                        _instance.SetData(DataTypes.Event, 230);
                        break;
                    case 300:
                        {
                            Talk(Texts.Stage_0_05);
                            if (_instance.GetBossState(DataTypes.BossBeasts) != EncounterState.Done)
                            {
                                _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                                Creature temp = me.SummonCreature(CreatureIds.Icehowl, MiscData.ToCSpawnLoc[0].GetPositionX(), MiscData.ToCSpawnLoc[0].GetPositionY(), MiscData.ToCSpawnLoc[0].GetPositionZ(), 5, TempSummonType.DeadDespawn);
                                if (temp)
                                {
                                    temp.GetMotionMaster().MovePoint(2, MiscData.ToCCommonLoc[5].GetPositionX(), MiscData.ToCCommonLoc[5].GetPositionY(), MiscData.ToCCommonLoc[5].GetPositionZ());
                                    me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
                                    me.SetReactState(ReactStates.Passive);
                                }
                            }
                            _updateTimer = 5 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 315);
                            break;
                        }
                    case 315:
                        _instance.SetData(DataTypes.Event, 320);
                        break;
                    case 400:
                        Talk(Texts.Stage_0_06);
                        me.GetThreatManager().clearReferences();
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 666:
                        Talk(Texts.Stage_0_Wipe);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 1010:
                        Talk(Texts.Stage_1_01);
                        _updateTimer = 7 * Time.InMilliseconds;
                        _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                        me.SummonCreature(CreatureIds.Fizzlebang, MiscData.ToCSpawnLoc[0].GetPositionX(), MiscData.ToCSpawnLoc[0].GetPositionY(), MiscData.ToCSpawnLoc[0].GetPositionZ(), 2, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 1180:
                        Talk(Texts.Stage_1_07);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 2000:
                        Talk(Texts.Stage_1_08);
                        _updateTimer = 18 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 2010);
                        break;
                    case 2030:
                        Talk(Texts.Stage_1_11);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 3000:
                        Talk(Texts.Stage_2_01);
                        _updateTimer = 12 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3050);
                        break;
                    case 3001:
                        Talk(Texts.Stage_2_01);
                        _updateTimer = 10 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3051);
                        break;
                    case 3060:
                        Talk(Texts.Stage_2_03);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3070);
                        break;
                    case 3061:
                        Talk(Texts.Stage_2_03);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3071);
                        break;
                    //Summoning crusaders
                    case 3091:
                        {
                            Creature pChampionController = me.SummonCreature(CreatureIds.ChampionsController, MiscData.ToCCommonLoc[1]);
                            if (pChampionController)
                                pChampionController.GetAI().SetData(0, (uint)Team.Horde);
                            _updateTimer = 3 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 3092);
                            break;
                        }
                    //Summoning crusaders
                    case 3090:
                        {
                            Creature pChampionController = me.SummonCreature(CreatureIds.ChampionsController, MiscData.ToCCommonLoc[1]);
                            if (pChampionController)
                                pChampionController.GetAI().SetData(0, (uint)Team.Alliance);
                            _updateTimer = 3 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 3092);
                            break;
                        }
                    case 3092:
                        {
                            Creature pChampionController = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.ChampionsController));
                            if (pChampionController)
                                pChampionController.GetAI().SetData(1, (uint)EncounterState.NotStarted);
                            _instance.SetData(DataTypes.Event, 3095);
                            break;
                        }
                    //Crusaders battle end
                    case 3100:
                        Talk(Texts.Stage_2_06);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 4000:
                        Talk(Texts.Stage_3_01);
                        _updateTimer = 13 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 4010);
                        break;
                    case 4010:
                        {
                            Talk(Texts.Stage_3_02);
                            Creature temp = me.SummonCreature(CreatureIds.Lightbane, MiscData.ToCSpawnLoc[1].GetPositionX(), MiscData.ToCSpawnLoc[1].GetPositionY(), MiscData.ToCSpawnLoc[1].GetPositionZ(), 5, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);
                            if (temp)
                            {
                                temp.SetVisible(false);
                                temp.SetReactState(ReactStates.Passive);
                                temp.SummonCreature(CreatureIds.LightEssence, MiscData.TwinValkyrsLoc[0].GetPositionX(), MiscData.TwinValkyrsLoc[0].GetPositionY(), MiscData.TwinValkyrsLoc[0].GetPositionZ());
                                temp.SummonCreature(CreatureIds.LightEssence, MiscData.TwinValkyrsLoc[1].GetPositionX(), MiscData.TwinValkyrsLoc[1].GetPositionY(), MiscData.TwinValkyrsLoc[1].GetPositionZ());
                            }

                            temp = me.SummonCreature(CreatureIds.Darkbane, MiscData.ToCSpawnLoc[2].GetPositionX(), MiscData.ToCSpawnLoc[2].GetPositionY(), MiscData.ToCSpawnLoc[2].GetPositionZ(), 5, TempSummonType.CorpseTimedDespawn, MiscData.DespawnTime);
                            if (temp)
                            {
                                temp.SetVisible(false);
                                temp.SetReactState(ReactStates.Passive);
                                temp.SummonCreature(CreatureIds.DarkEssence, MiscData.TwinValkyrsLoc[2].GetPositionX(), MiscData.TwinValkyrsLoc[2].GetPositionY(), MiscData.TwinValkyrsLoc[2].GetPositionZ());
                                temp.SummonCreature(CreatureIds.DarkEssence, MiscData.TwinValkyrsLoc[3].GetPositionX(), MiscData.TwinValkyrsLoc[3].GetPositionY(), MiscData.TwinValkyrsLoc[3].GetPositionZ());
                            }
                            _updateTimer = 3 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 4015);
                            break;
                        }
                    case 4015:
                        {
                            _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                            Creature temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Lightbane));
                            if (temp)
                            {
                                temp.GetMotionMaster().MovePoint(1, MiscData.ToCCommonLoc[8].GetPositionX(), MiscData.ToCCommonLoc[8].GetPositionY(), MiscData.ToCCommonLoc[8].GetPositionZ());
                                temp.SetVisible(true);
                            }

                            temp = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.Darkbane));
                            if (temp)
                            {
                                temp.GetMotionMaster().MovePoint(1, MiscData.ToCCommonLoc[9].GetPositionX(), MiscData.ToCCommonLoc[9].GetPositionY(), MiscData.ToCCommonLoc[9].GetPositionZ());
                                temp.SetVisible(true);
                            }
                            _updateTimer = 10 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 4016);
                            break;
                        }
                    case 4016:
                        _instance.DoUseDoorOrButton(_instance.GetGuidData(GameObjectIds.MainGateDoor));
                        _instance.SetData(DataTypes.Event, 4017);
                        break;
                    case 4040:
                        _updateTimer = 1 * Time.Minute * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 5000);
                        break;
                    case 5000:
                        Talk(Texts.Stage_4_01);
                        _updateTimer = 10 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 5005);
                        break;
                    case 5005:
                        _updateTimer = 8 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 5010);
                        me.SummonCreature(CreatureIds.LichKing, MiscData.ToCCommonLoc[2].GetPositionX(), MiscData.ToCCommonLoc[2].GetPositionY(), MiscData.ToCCommonLoc[2].GetPositionZ(), 5);
                        break;
                    case 5020:
                        Talk(Texts.Stage_4_03);
                        _updateTimer = 1 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 0);
                        break;
                    case 6000:
                        me.SummonCreature(CreatureIds.TirionFordring, MiscData.EndSpawnLoc[0]);
                        me.SummonCreature(CreatureIds.ArgentMage, MiscData.EndSpawnLoc[1]);
                        me.SummonGameObject(GameObjectIds.PortalToDalaran, MiscData.EndSpawnLoc[2], Quaternion.fromEulerAnglesZYX(MiscData.EndSpawnLoc[2].GetOrientation(), 0.0f, 0.0f), 0);
                        _updateTimer = 20 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 6005);
                        break;
                    case 6005:
                        {
                            Creature tirionFordring = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.TirionFordring));
                            if (tirionFordring)
                                tirionFordring.GetAI().Talk(Texts.Stage_4_06);
                            _updateTimer = 20 * Time.InMilliseconds;
                            _instance.SetData(DataTypes.Event, 6010);
                            break;
                        }
                    case 6010:
                        if (IsHeroic())
                        {
                            Creature tirionFordring = ObjectAccessor.GetCreature(me, _instance.GetGuidData(CreatureIds.TirionFordring));
                            if (tirionFordring)
                                tirionFordring.GetAI().Talk(Texts.Stage_4_07);
                            _updateTimer = 1 * Time.Minute * Time.InMilliseconds;
                            _instance.SetBossState(DataTypes.BossAnubarak, EncounterState.Special);
                            _instance.SetData(DataTypes.Event, 6020);
                        }
                        else
                            _instance.SetData(DataTypes.Event, 6030);
                        break;
                    case 6020:
                        me.DespawnOrUnsummon();
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 6030);
                        break;
                    default:
                        break;
                }
            }
            else
                _updateTimer -= uiDiff;
            _instance.SetData(DataTypes.EventTimer, _updateTimer);
        }

        InstanceScript _instance;
        uint _updateTimer;
    }

    [Script]
    class npc_garrosh_toc : ScriptedAI
    {
        public npc_garrosh_toc(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
        }

        public override void Reset() { }

        public override void AttackStart(Unit who) { }

        public override void UpdateAI(uint uiDiff)
        {
            if (_instance == null)
                return;

            if (_instance.GetData(DataTypes.EventNpc) != CreatureIds.Garrosh)
                return;

            _updateTimer = _instance.GetData(DataTypes.EventTimer);
            if (_updateTimer <= uiDiff)
            {
                switch (_instance.GetData(DataTypes.Event))
                {
                    case 130:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotTalk);
                        Talk(Texts.Stage_0_03h);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 132);
                        break;
                    case 132:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 140);
                        break;
                    case 2010:
                        Talk(Texts.Stage_1_09);
                        _updateTimer = 9 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 2020);
                        break;
                    case 3050:
                        Talk(Texts.Stage_2_02h);
                        _updateTimer = 15 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3060);
                        break;
                    case 3070:
                        Talk(Texts.Stage_2_04h);
                        _updateTimer = 6 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3080);
                        break;
                    case 3081:
                        Talk(Texts.Stage_2_05h);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3091);
                        break;
                    case 4030:
                        Talk(Texts.Stage_3_03h);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 4040);
                        break;
                    default:
                        break;
                }
            }
            else
                _updateTimer -= uiDiff;
            _instance.SetData(DataTypes.EventTimer, _updateTimer);
        }

        InstanceScript _instance;
        uint _updateTimer;
    }

    [Script]
    class npc_varian_toc : ScriptedAI
    {
        public npc_varian_toc(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
        }

        public override void Reset() { }

        public override void AttackStart(Unit who) { }

        public override void UpdateAI(uint uiDiff)
        {
            if (_instance == null)
                return;

            if (_instance.GetData(DataTypes.EventNpc) != CreatureIds.Varian)
                return;

            _updateTimer = _instance.GetData(DataTypes.EventTimer);
            if (_updateTimer <= uiDiff)
            {
                switch (_instance.GetData(DataTypes.Event))
                {
                    case 120:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotTalk);
                        Talk(Texts.Stage_0_03a);
                        _updateTimer = 2 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 122);
                        break;
                    case 122:
                        me.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 130);
                        break;
                    case 2020:
                        Talk(Texts.Stage_1_10);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 2030);
                        break;
                    case 3051:
                        Talk(Texts.Stage_2_02a);
                        _updateTimer = 17 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3061);
                        break;
                    case 3071:
                        Talk(Texts.Stage_2_04a);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3081);
                        break;
                    case 3080:
                        Talk(Texts.Stage_2_05a);
                        _updateTimer = 3 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 3090);
                        break;
                    case 4020:
                        Talk(Texts.Stage_3_03a);
                        _updateTimer = 5 * Time.InMilliseconds;
                        _instance.SetData(DataTypes.Event, 4040);
                        break;
                    default:
                        break;
                }
            }
            else
                _updateTimer -= uiDiff;
            _instance.SetData(DataTypes.EventTimer, _updateTimer);
        }

        InstanceScript _instance;
        uint _updateTimer;
    }
}
