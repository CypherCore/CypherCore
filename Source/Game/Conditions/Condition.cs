// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Miscellaneous;
using Game.Scenarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Conditions
{
    public class Condition
    {
        public Condition()
        {
            SourceType = ConditionSourceType.None;
            ConditionType = ConditionTypes.None;
        }

        public bool Meets(ConditionSourceInfo sourceInfo)
        {
            Cypher.Assert(ConditionTarget < SharedConst.MaxConditionTargets);

            Map map = sourceInfo.mConditionMap;
            bool condMeets = false;
            bool needsObject = false;
            switch (ConditionType)
            {
                case ConditionTypes.None:
                    condMeets = true;                                    // empty condition, always met
                    break;
                case ConditionTypes.ActiveEvent:
                    condMeets = Global.GameEventMgr.IsActiveEvent((ushort)ConditionValue1);
                    break;
                case ConditionTypes.InstanceInfo:
                {
                    InstanceMap instanceMap = map.ToInstanceMap();
                    if (instanceMap != null)
                    {
                        InstanceScript instance = instanceMap.GetInstanceScript();
                        if (instance != null)
                        {
                            switch ((InstanceInfo)ConditionValue3)
                            {
                                case InstanceInfo.Data:
                                    condMeets = instance.GetData(ConditionValue1) == ConditionValue2;
                                    break;
                                case InstanceInfo.BossState:
                                    condMeets = instance.GetBossState(ConditionValue1) == (EncounterState)ConditionValue2;
                                    break;
                                case InstanceInfo.Data64:
                                    condMeets = instance.GetData64(ConditionValue1) == ConditionValue2;
                                    break;
                                default:
                                    condMeets = false;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        BattlegroundMap bgMap = map.ToBattlegroundMap();
                        if (bgMap != null)
                        {
                            ZoneScript zoneScript = bgMap.GetBattlegroundScript();
                            switch ((InstanceInfo)ConditionValue3)
                            {
                                case InstanceInfo.Data:
                                    condMeets = zoneScript.GetData(ConditionValue1) == ConditionValue2;
                                    break;
                                case InstanceInfo.Data64:
                                    condMeets = zoneScript.GetData64(ConditionValue1) == ConditionValue2;
                                    break;
                                default:
                                    condMeets = false;
                                    break;
                            }
                        }
                    }
                    break;
                }
                case ConditionTypes.Mapid:
                    condMeets = map.GetId() == ConditionValue1;
                    break;
                case ConditionTypes.WorldState:
                {
                    condMeets = Global.WorldStateMgr.GetValue((int)ConditionValue1, map) == ConditionValue2;
                    break;
                }
                case ConditionTypes.RealmAchievement:
                {
                    var achievement = CliDB.AchievementStorage.LookupByKey(ConditionValue1);
                    if (achievement != null && Global.AchievementMgr.IsRealmCompleted(achievement))
                        condMeets = true;
                    break;
                }
                case ConditionTypes.DifficultyId:
                {
                    condMeets = (uint)map.GetDifficultyID() == ConditionValue1;
                    break;
                }
                case ConditionTypes.ScenarioStep:
                {
                    InstanceMap instanceMap = map.ToInstanceMap();
                    if (instanceMap != null)
                    {
                        Scenario scenario = instanceMap.GetInstanceScenario();
                        if (scenario != null)
                        {
                            ScenarioStepRecord step = scenario.GetStep();
                            if (step != null)
                                condMeets = step.Id == ConditionValue1;
                        }
                    }
                    break;
                }
                default:
                    needsObject = true;
                    break;
            }

            WorldObject obj = sourceInfo.mConditionTargets[ConditionTarget];
            // object not present, return false
            if (needsObject && obj == null)
            {
                Log.outDebug(LogFilter.Condition, "Condition object not found for condition (Entry: {0} Type: {1} Group: {2})", SourceEntry, SourceType, SourceGroup);
                return false;
            }

            Player player = obj?.ToPlayer();
            Unit unit = obj?.ToUnit();

            switch (ConditionType)
            {
                case ConditionTypes.Aura:
                    if (unit != null)
                        condMeets = unit.HasAuraEffect(ConditionValue1, (byte)ConditionValue2);
                    break;
                case ConditionTypes.Item:
                    if (player != null)
                    {
                        // don't allow 0 items (it's checked during table load)
                        Cypher.Assert(ConditionValue2 != 0);
                        bool checkBank = ConditionValue3 != 0;
                        condMeets = player.HasItemCount(ConditionValue1, ConditionValue2, checkBank);
                    }
                    break;
                case ConditionTypes.ItemEquipped:
                    if (player != null)
                        condMeets = player.HasItemOrGemWithIdEquipped(ConditionValue1, 1);
                    break;
                case ConditionTypes.Zoneid:
                    condMeets = obj.GetZoneId() == ConditionValue1;
                    break;
                case ConditionTypes.ReputationRank:
                    if (player != null)
                    {
                        var faction = CliDB.FactionStorage.LookupByKey(ConditionValue1);
                        if (faction != null)
                            condMeets = Convert.ToBoolean(ConditionValue2 & (1 << (int)player.GetReputationMgr().GetRank(faction)));
                    }
                    break;
                case ConditionTypes.Achievement:
                    if (player != null)
                        condMeets = player.HasAchieved(ConditionValue1);
                    break;
                case ConditionTypes.Team:
                    if (player != null)
                        condMeets = player.GetTeam() == (Team)ConditionValue1;
                    break;
                case ConditionTypes.Class:
                    if (unit != null)
                        condMeets = Convert.ToBoolean(unit.GetClassMask() & ConditionValue1);
                    break;
                case ConditionTypes.Race:
                    if (unit != null)
                        condMeets = new RaceMask<uint>(ConditionValue1).HasRace(unit.GetRace());
                    break;
                case ConditionTypes.Gender:
                    if (player != null)
                        condMeets = player.GetNativeGender() == (Gender)ConditionValue1;
                    break;
                case ConditionTypes.Skill:
                    if (player != null)
                        condMeets = player.HasSkill((SkillType)ConditionValue1) && player.GetBaseSkillValue((SkillType)ConditionValue1) >= ConditionValue2;
                    break;
                case ConditionTypes.QuestRewarded:
                    if (player != null)
                        condMeets = player.GetQuestRewardStatus(ConditionValue1);
                    break;
                case ConditionTypes.QuestTaken:
                    if (player != null)
                    {
                        QuestStatus status = player.GetQuestStatus(ConditionValue1);
                        condMeets = (status == QuestStatus.Incomplete);
                    }
                    break;
                case ConditionTypes.QuestComplete:
                    if (player != null)
                    {
                        QuestStatus status = player.GetQuestStatus(ConditionValue1);
                        condMeets = (status == QuestStatus.Complete && !player.GetQuestRewardStatus(ConditionValue1));
                    }
                    break;
                case ConditionTypes.QuestNone:
                    if (player != null)
                    {
                        QuestStatus status = player.GetQuestStatus(ConditionValue1);
                        condMeets = (status == QuestStatus.None);
                    }
                    break;
                case ConditionTypes.Areaid:
                    condMeets = Global.DB2Mgr.IsInArea(obj.GetAreaId(), ConditionValue1);
                    break;
                case ConditionTypes.Spell:
                    if (player != null)
                        condMeets = player.HasSpell(ConditionValue1);
                    break;
                case ConditionTypes.Level:
                    if (unit != null)
                        condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.GetLevel(), ConditionValue1);
                    break;
                case ConditionTypes.DrunkenState:
                    if (player != null)
                        condMeets = (uint)Player.GetDrunkenstateByValue(player.GetDrunkValue()) >= ConditionValue1;
                    break;
                case ConditionTypes.NearCreature:
                    condMeets = obj.FindNearestCreature(ConditionValue1, ConditionValue2, ConditionValue3 == 0) != null;
                    break;
                case ConditionTypes.NearGameobject:
                    condMeets = obj.FindNearestGameObject(ConditionValue1, ConditionValue2) != null;
                    break;
                case ConditionTypes.ObjectEntryGuid:
                    if ((uint)obj.GetTypeId() == ConditionValue1)
                    {
                        condMeets = ConditionValue2 == 0 || (obj.GetEntry() == ConditionValue2);

                        if (ConditionValue3 != 0)
                        {
                            switch (obj.GetTypeId())
                            {
                                case TypeId.Unit:
                                    condMeets &= obj.ToCreature().GetSpawnId() == ConditionValue3;
                                    break;
                                case TypeId.GameObject:
                                    condMeets &= obj.ToGameObject().GetSpawnId() == ConditionValue3;
                                    break;
                            }
                        }
                    }
                    break;
                case ConditionTypes.TypeMask:
                    condMeets = Convert.ToBoolean((TypeMask)ConditionValue1 & obj.ObjectTypeMask);
                    break;
                case ConditionTypes.RelationTo:
                {
                    WorldObject toObject = sourceInfo.mConditionTargets[ConditionValue1];
                    if (toObject != null)
                    {
                        Unit toUnit = toObject.ToUnit();
                        if (toUnit != null && unit != null)
                        {
                            switch ((RelationType)ConditionValue2)
                            {
                                case RelationType.Self:
                                    condMeets = unit == toUnit;
                                    break;
                                case RelationType.InParty:
                                    condMeets = unit.IsInPartyWith(toUnit);
                                    break;
                                case RelationType.InRaidOrParty:
                                    condMeets = unit.IsInRaidWith(toUnit);
                                    break;
                                case RelationType.OwnedBy:
                                    condMeets = unit.GetOwnerGUID() == toUnit.GetGUID();
                                    break;
                                case RelationType.PassengerOf:
                                    condMeets = unit.IsOnVehicle(toUnit);
                                    break;
                                case RelationType.CreatedBy:
                                    condMeets = unit.GetCreatorGUID() == toUnit.GetGUID();
                                    break;
                            }
                        }
                    }
                    break;
                }
                case ConditionTypes.ReactionTo:
                {
                    WorldObject toObject = sourceInfo.mConditionTargets[ConditionValue1];
                    if (toObject != null)
                    {
                        Unit toUnit = toObject.ToUnit();
                        if (toUnit != null && unit != null)
                            condMeets = Convert.ToBoolean((1 << (int)unit.GetReactionTo(toUnit)) & ConditionValue2);
                    }
                    break;
                }
                case ConditionTypes.DistanceTo:
                {
                    WorldObject toObject = sourceInfo.mConditionTargets[ConditionValue1];
                    if (toObject != null)
                        condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue3, obj.GetDistance(toObject), ConditionValue2);
                    break;
                }
                case ConditionTypes.Alive:
                    if (unit != null)
                        condMeets = unit.IsAlive();
                    break;
                case ConditionTypes.HpVal:
                    if (unit != null)
                        condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.GetHealth(), ConditionValue1);
                    break;
                case ConditionTypes.HpPct:
                    if (unit != null)
                        condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue2, unit.GetHealthPct(), ConditionValue1);
                    break;
                case ConditionTypes.PhaseId:
                    condMeets = obj.GetPhaseShift().HasPhase(ConditionValue1);
                    break;
                case ConditionTypes.Title:
                    if (player != null)
                        condMeets = player.HasTitle(ConditionValue1);
                    break;
                case ConditionTypes.UnitState:
                    if (unit != null)
                        condMeets = unit.HasUnitState((UnitState)ConditionValue1);
                    break;
                case ConditionTypes.CreatureType:
                {
                    Creature creature = obj.ToCreature();
                    if (creature != null)
                        condMeets = (uint)creature.GetCreatureTemplate().CreatureType == ConditionValue1;
                    break;
                }
                case ConditionTypes.InWater:
                    if (unit != null)
                        condMeets = unit.IsInWater();
                    break;
                case ConditionTypes.TerrainSwap:
                    condMeets = obj.GetPhaseShift().HasVisibleMapId(ConditionValue1);
                    break;
                case ConditionTypes.StandState:
                {
                    if (unit != null)
                    {
                        if (ConditionValue1 == 0)
                            condMeets = (unit.GetStandState() == (UnitStandStateType)ConditionValue2);
                        else if (ConditionValue2 == 0)
                            condMeets = unit.IsStandState();
                        else if (ConditionValue2 == 1)
                            condMeets = unit.IsSitState();
                    }
                    break;
                }
                case ConditionTypes.DailyQuestDone:
                {
                    if (player != null)
                        condMeets = player.IsDailyQuestDone(ConditionValue1);
                    break;
                }
                case ConditionTypes.Charmed:
                {
                    if (unit != null)
                        condMeets = unit.IsCharmed();
                    break;
                }
                case ConditionTypes.PetType:
                {
                    if (player != null)
                    {
                        Pet pet = player.GetPet();
                        if (pet != null)
                            condMeets = (((1 << (int)pet.GetPetType()) & ConditionValue1) != 0);
                    }
                    break;
                }
                case ConditionTypes.Taxi:
                {
                    if (player != null)
                        condMeets = player.IsInFlight();
                    break;
                }
                case ConditionTypes.Queststate:
                {
                    if (player != null)
                    {
                        if (
                            (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.None)) && (player.GetQuestStatus(ConditionValue1) == QuestStatus.None)) ||
                            (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Complete)) && (player.GetQuestStatus(ConditionValue1) == QuestStatus.Complete)) ||
                            (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Incomplete)) && (player.GetQuestStatus(ConditionValue1) == QuestStatus.Incomplete)) ||
                            (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Failed)) && (player.GetQuestStatus(ConditionValue1) == QuestStatus.Failed)) ||
                            (Convert.ToBoolean(ConditionValue2 & (1 << (int)QuestStatus.Rewarded)) && player.GetQuestRewardStatus(ConditionValue1))
                        )
                            condMeets = true;
                    }
                    break;
                }
                case ConditionTypes.ObjectiveProgress:
                {
                    if (player != null)
                    {
                        QuestObjective questObj = Global.ObjectMgr.GetQuestObjective(ConditionValue1);
                        if (questObj == null)
                            break;

                        Quest quest = Global.ObjectMgr.GetQuestTemplate(questObj.QuestID);
                        if (quest == null)
                            break;

                        ushort slot = player.FindQuestSlot(questObj.QuestID);
                        if (slot >= SharedConst.MaxQuestLogSize)
                            break;

                        condMeets = player.GetQuestSlotObjectiveData(slot, questObj) == ConditionValue3;
                    }
                    break;
                }
                case ConditionTypes.Gamemaster:
                {
                    if (player != null)
                    {
                        if (ConditionValue1 == 1)
                            condMeets = player.CanBeGameMaster();
                        else
                            condMeets = player.IsGameMaster();
                    }
                    break;
                }
                case ConditionTypes.BattlePetCount:
                {
                    if (player != null)
                        condMeets = MathFunctions.CompareValues((ComparisionType)ConditionValue3, player.GetSession().GetBattlePetMgr().GetPetCount(CliDB.BattlePetSpeciesStorage.LookupByKey(ConditionValue1), player.GetGUID()), ConditionValue2);
                    break;
                }
                case ConditionTypes.SceneInProgress:
                {
                    if (player != null)
                        condMeets = player.GetSceneMgr().GetActiveSceneCount(ConditionValue1) > 0;
                    break;
                }
                case ConditionTypes.PlayerCondition:
                {
                    if (player != null)
                        condMeets = ConditionManager.IsPlayerMeetingCondition(player, ConditionValue1);
                    break;
                }
                case ConditionTypes.PrivateObject:
                {
                    condMeets = !obj.GetPrivateObjectOwner().IsEmpty();
                    break;
                }
                case ConditionTypes.StringId:
                {
                    Creature creature = obj.ToCreature();
                    if (creature != null)
                        condMeets = creature.HasStringId(ConditionStringValue1);
                    else
                    {
                        GameObject go = obj.ToGameObject();
                        if (go != null)
                            condMeets = go.HasStringId(ConditionStringValue1);
                    }
                    break;
                }
                case ConditionTypes.Label:
                {
                    Creature creature = obj.ToCreature();
                    if (creature != null)
                        condMeets = creature.HasLabel((int)ConditionValue1);
                    else
                    {
                        GameObject go = obj.ToGameObject();
                        if (go != null)
                            condMeets = go.HasLabel((int)ConditionValue1);
                    }
                    break;
                }
                default:
                    break;
            }

            if (NegativeCondition)
                condMeets = !condMeets;

            if (!condMeets)
                sourceInfo.mLastFailedCondition = this;

            return condMeets && Global.ScriptMgr.OnConditionCheck(this, sourceInfo); // Returns true by default.;
        }

        public GridMapTypeMask GetSearcherTypeMaskForCondition()
        {
            // build mask of types for which condition can return true
            // this is used for speeding up gridsearches
            if (NegativeCondition)
                return GridMapTypeMask.All;

            GridMapTypeMask mask = 0;
            switch (ConditionType)
            {
                case ConditionTypes.ActiveEvent:
                case ConditionTypes.Areaid:
                case ConditionTypes.DifficultyId:
                case ConditionTypes.DistanceTo:
                case ConditionTypes.InstanceInfo:
                case ConditionTypes.Mapid:
                case ConditionTypes.NearCreature:
                case ConditionTypes.NearGameobject:
                case ConditionTypes.None:
                case ConditionTypes.PhaseId:
                case ConditionTypes.RealmAchievement:
                case ConditionTypes.TerrainSwap:
                case ConditionTypes.WorldState:
                case ConditionTypes.Zoneid:
                    mask |= GridMapTypeMask.All;
                    break;
                case ConditionTypes.Gender:
                case ConditionTypes.Title:
                case ConditionTypes.DrunkenState:
                case ConditionTypes.Spell:
                case ConditionTypes.QuestTaken:
                case ConditionTypes.QuestComplete:
                case ConditionTypes.QuestNone:
                case ConditionTypes.Skill:
                case ConditionTypes.QuestRewarded:
                case ConditionTypes.ReputationRank:
                case ConditionTypes.Achievement:
                case ConditionTypes.Team:
                case ConditionTypes.Item:
                case ConditionTypes.ItemEquipped:
                case ConditionTypes.PetType:
                case ConditionTypes.Taxi:
                case ConditionTypes.Queststate:
                case ConditionTypes.Gamemaster:
                    mask |= GridMapTypeMask.Player;
                    break;
                case ConditionTypes.UnitState:
                case ConditionTypes.Alive:
                case ConditionTypes.HpVal:
                case ConditionTypes.HpPct:
                case ConditionTypes.RelationTo:
                case ConditionTypes.ReactionTo:
                case ConditionTypes.Level:
                case ConditionTypes.Class:
                case ConditionTypes.Race:
                case ConditionTypes.Aura:
                case ConditionTypes.InWater:
                case ConditionTypes.StandState:
                    mask |= GridMapTypeMask.Creature | GridMapTypeMask.Player;
                    break;
                case ConditionTypes.ObjectEntryGuid:
                    switch ((TypeId)ConditionValue1)
                    {
                        case TypeId.Unit:
                            mask |= GridMapTypeMask.Creature;
                            break;
                        case TypeId.Player:
                            mask |= GridMapTypeMask.Player;
                            break;
                        case TypeId.GameObject:
                            mask |= GridMapTypeMask.GameObject;
                            break;
                        case TypeId.Corpse:
                            mask |= GridMapTypeMask.Corpse;
                            break;
                        case TypeId.AreaTrigger:
                            mask |= GridMapTypeMask.AreaTrigger;
                            break;
                        default:
                            break;
                    }
                    break;
                case ConditionTypes.TypeMask:
                    if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Unit))
                        mask |= GridMapTypeMask.Creature | GridMapTypeMask.Player;
                    if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Player))
                        mask |= GridMapTypeMask.Player;
                    if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.GameObject))
                        mask |= GridMapTypeMask.GameObject;
                    if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.Corpse))
                        mask |= GridMapTypeMask.Corpse;
                    if (Convert.ToBoolean((TypeMask)ConditionValue1 & TypeMask.AreaTrigger))
                        mask |= GridMapTypeMask.AreaTrigger;
                    break;
                case ConditionTypes.DailyQuestDone:
                case ConditionTypes.ObjectiveProgress:
                case ConditionTypes.BattlePetCount:
                    mask |= GridMapTypeMask.Player;
                    break;
                case ConditionTypes.ScenarioStep:
                    mask |= GridMapTypeMask.All;
                    break;
                case ConditionTypes.SceneInProgress:
                    mask |= GridMapTypeMask.Player;
                    break;
                case ConditionTypes.PlayerCondition:
                    mask |= GridMapTypeMask.Player;
                    break;
                case ConditionTypes.PrivateObject:
                    mask |= GridMapTypeMask.All & ~GridMapTypeMask.Player;
                    break;
                case ConditionTypes.StringId:
                    mask |= GridMapTypeMask.Creature | GridMapTypeMask.GameObject;
                    break;
                case ConditionTypes.Label:
                    mask |= GridMapTypeMask.Creature | GridMapTypeMask.GameObject;
                    break;
                default:
                    Cypher.Assert(false, "Condition.GetSearcherTypeMaskForCondition - missing condition handling!");
                    break;
            }
            return mask;
        }

        public bool IsLoaded()
        {
            return ConditionType > ConditionTypes.None || ReferenceId != 0 || ScriptId != 0;
        }

        public uint GetMaxAvailableConditionTargets()
        {
            // returns number of targets which are available for given source type
            switch (SourceType)
            {
                case ConditionSourceType.Spell:
                case ConditionSourceType.SpellImplicitTarget:
                case ConditionSourceType.CreatureTemplateVehicle:
                case ConditionSourceType.VehicleSpell:
                case ConditionSourceType.SpellClickEvent:
                case ConditionSourceType.GossipMenu:
                case ConditionSourceType.GossipMenuOption:
                case ConditionSourceType.SmartEvent:
                case ConditionSourceType.NpcVendor:
                case ConditionSourceType.SpellProc:
                case ConditionSourceType.ObjectIdVisibility:
                    return 2;
                default:
                    return 1;
            }
        }

        public string ToString(bool ext = false)
        {
            StringBuilder ss = new();
            ss.AppendFormat("[Condition SourceType: {0}", SourceType);
            if (SourceType < ConditionSourceType.MaxDbAllowed)
                ss.AppendFormat(" ({0})", Global.ConditionMgr.StaticSourceTypeData[(int)SourceType]);
            else if (SourceType == ConditionSourceType.ReferenceCondition)
                ss.Append(" (Reference)");
            else
                ss.Append(" (Unknown)");
            if (Global.ConditionMgr.CanHaveSourceGroupSet(SourceType))
                ss.AppendFormat(", SourceGroup: {0}", SourceGroup);
            ss.AppendFormat(", SourceEntry: {0}", SourceEntry);
            if (Global.ConditionMgr.CanHaveSourceIdSet(SourceType))
                ss.AppendFormat(", SourceId: {0}", SourceId);

            if (ext)
            {
                ss.AppendFormat(", ConditionType: {0}", ConditionType);
                if (ConditionType < ConditionTypes.Max)
                    ss.AppendFormat(" ({0})", Global.ConditionMgr.StaticConditionTypeData[(int)ConditionType].Name);
                else
                    ss.Append(" (Unknown)");
            }

            ss.Append(']');
            return ss.ToString();
        }

        public ConditionSourceType SourceType;        //SourceTypeOrReferenceId
        public uint SourceGroup;
        public int SourceEntry;
        public uint SourceId;          // So far, only used in CONDITION_SOURCE_TYPE_SMART_EVENT
        public uint ElseGroup;
        public ConditionTypes ConditionType;     //ConditionTypeOrReference
        public uint ConditionValue1;
        public uint ConditionValue2;
        public uint ConditionValue3;
        public string ConditionStringValue1;
        public uint ErrorType;
        public uint ErrorTextId;
        public uint ReferenceId;
        public uint ScriptId;
        public byte ConditionTarget;
        public bool NegativeCondition;
    }

    public struct ConditionId
    {
        public uint SourceGroup;
        public int SourceEntry;
        public uint SourceId;

        public ConditionId(uint sourceGroup, int sourceEntry, uint sourceId)
        {
            SourceGroup = sourceGroup;
            SourceEntry = sourceEntry;
            SourceId = sourceId;
        }

        public override int GetHashCode()
        {
            return SourceGroup.GetHashCode() ^ SourceEntry.GetHashCode() ^ SourceId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConditionId)
                return (ConditionId)obj == this;

            return false;
        }

        public static bool operator ==(ConditionId left, ConditionId right)
        {
            if (left.SourceGroup != right.SourceGroup)
                return false;

            if (left.SourceEntry != right.SourceEntry)
                return false;

            if (left.SourceId != right.SourceId)
                return false;

            return true;
        }

        public static bool operator !=(ConditionId left, ConditionId right)
        {
            return !(left == right);
        }
    }

    public class ConditionSourceInfo
    {
        public WorldObject[] mConditionTargets = new WorldObject[SharedConst.MaxConditionTargets]; // an array of targets available for conditions
        public Map mConditionMap;
        public Condition mLastFailedCondition;

        public ConditionSourceInfo(WorldObject target0, WorldObject target1 = null, WorldObject target2 = null)
        {
            mConditionTargets = [target0, target1, target2];

            var target = target0 ?? target1 ?? target2;
            if (target != null)
                mConditionMap = target.GetMap();
        }

        public ConditionSourceInfo(Map map) 
        {
            mConditionMap = map;
        }
    }
}