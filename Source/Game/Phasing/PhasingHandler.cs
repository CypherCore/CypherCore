// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game
{
    public class PhasingHandler
    {
        public static PhaseShift EmptyPhaseShift = new();
        public static PhaseShift AlwaysVisible;

        static PhasingHandler()
        {
            AlwaysVisible = new();
            InitDbPhaseShift(AlwaysVisible, PhaseUseFlagsValues.AlwaysVisible, 0, 0);
        }

        public static PhaseFlags GetPhaseFlags(uint phaseId)
        {
            PhaseRecord phase = CliDB.PhaseStorage.LookupByKey(phaseId);
            if (phase != null)
            {
                if (phase.Flags.HasAnyFlag(PhaseEntryFlags.Cosmetic))
                    return PhaseFlags.Cosmetic;

                if (phase.Flags.HasAnyFlag(PhaseEntryFlags.Personal))
                    return PhaseFlags.Personal;
            }

            return PhaseFlags.None;
        }

        public static void ForAllControlled(Unit unit, Action<Unit> func)
        {
            for (var i = 0; i < unit.m_Controlled.Count; ++i)
            {
                Unit controlled = unit.m_Controlled[i];
                if (controlled.GetTypeId() != TypeId.Player
                    && controlled.GetVehicle() == null)                   // Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
                    func(controlled);
            }

            for (byte i = 0; i < SharedConst.MaxSummonSlot; ++i)
            {
                if (!unit.m_SummonSlot[i].IsEmpty())
                {
                    Creature summon = unit.GetMap().GetCreature(unit.m_SummonSlot[i]);
                    if (summon)
                        func(summon);
                }
            }

            Vehicle vehicle = unit.GetVehicleKit();
            if (vehicle != null)
            {
                foreach (var seat in vehicle.Seats)
                {
                    Unit passenger = Global.ObjAccessor.GetUnit(unit, seat.Value.Passenger.Guid);
                    if (passenger != null)
                        func(passenger);
                }
            }
        }

        public static void AddPhase(WorldObject obj, uint phaseId, bool updateVisibility)
        {
            ControlledUnitVisitor visitor = new(obj);
            AddPhase(obj, phaseId, obj.GetGUID(), updateVisibility, visitor);
        }

        static void AddPhase(WorldObject obj, uint phaseId, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
        {
            bool changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null);

            if (obj.GetPhaseShift().PersonalReferences != 0)
                obj.GetPhaseShift().PersonalGuid = personalGuid;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                visitor.VisitControlledOf(unit, controlled =>
                {
                    AddPhase(controlled, phaseId, personalGuid, updateVisibility, visitor);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility)
        {
            ControlledUnitVisitor visitor = new(obj);
            RemovePhase(obj, phaseId, updateVisibility, visitor);
        }

        static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility, ControlledUnitVisitor visitor)
        {
            bool changed = obj.GetPhaseShift().RemovePhase(phaseId);

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                visitor.VisitControlledOf(unit, controlled =>
                {
                    RemovePhase(controlled, phaseId, updateVisibility, visitor);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void AddPhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
        {
            var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
            if (phasesInGroup.Empty())
                return;
            ControlledUnitVisitor visitor = new(obj);
            AddPhaseGroup(obj, phasesInGroup, obj.GetGUID(), updateVisibility, visitor);
        }

        static void AddPhaseGroup(WorldObject obj, List<uint> phasesInGroup, ObjectGuid personalGuid, bool updateVisibility, ControlledUnitVisitor visitor)
        {
            bool changed = false;
            foreach (uint phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;

            if (obj.GetPhaseShift().PersonalReferences != 0)
                obj.GetPhaseShift().PersonalGuid = personalGuid;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                visitor.VisitControlledOf(unit, controlled =>
                {
                    AddPhaseGroup(controlled, phasesInGroup, personalGuid, updateVisibility, visitor);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void RemovePhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
        {
            var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
            if (phasesInGroup.Empty())
                return;

            ControlledUnitVisitor visitor = new(obj);
            RemovePhaseGroup(obj, phasesInGroup, updateVisibility, visitor);
        }

        static void RemovePhaseGroup(WorldObject obj, List<uint> phasesInGroup, bool updateVisibility, ControlledUnitVisitor visitor)
        {
            bool changed = false;
            foreach (uint phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().RemovePhase(phaseId) || changed;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                visitor.VisitControlledOf(unit, controlled =>
                {
                    RemovePhaseGroup(controlled, phasesInGroup, updateVisibility, visitor);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void AddVisibleMapId(WorldObject obj, uint visibleMapId)
        {
            ControlledUnitVisitor visitor = new(obj);
            AddVisibleMapId(obj, visibleMapId, visitor);
        }

        static void AddVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
        {
            TerrainSwapInfo terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
            bool changed = obj.GetPhaseShift().AddVisibleMapId(visibleMapId, terrainSwapInfo);

            foreach (uint uiMapPhaseId in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().AddUiMapPhaseId(uiMapPhaseId) || changed;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                visitor.VisitControlledOf(unit, controlled =>
                {
                    AddVisibleMapId(controlled, visibleMapId, visitor);
                });
            }

            UpdateVisibilityIfNeeded(obj, false, changed);
        }

        public static void RemoveVisibleMapId(WorldObject obj, uint visibleMapId)
        {
            ControlledUnitVisitor visitor = new(obj);
            RemoveVisibleMapId(obj, visibleMapId, visitor);
        }

        static void RemoveVisibleMapId(WorldObject obj, uint visibleMapId, ControlledUnitVisitor visitor)
        {
            TerrainSwapInfo terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
            bool changed = obj.GetPhaseShift().RemoveVisibleMapId(visibleMapId);

            foreach (uint uiWorldMapAreaIDSwap in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().RemoveUiMapPhaseId(uiWorldMapAreaIDSwap) || changed;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                visitor.VisitControlledOf(unit, controlled =>
                {
                    RemoveVisibleMapId(controlled, visibleMapId, visitor);
                });
            }

            UpdateVisibilityIfNeeded(obj, false, changed);
        }

        public static void ResetPhaseShift(WorldObject obj)
        {
            obj.GetPhaseShift().Clear();
            obj.GetSuppressedPhaseShift().Clear();
        }

        public static void InheritPhaseShift(WorldObject target, WorldObject source)
        {
            target.SetPhaseShift(source.GetPhaseShift());
            target.SetSuppressedPhaseShift(source.GetSuppressedPhaseShift());
        }

        public static void OnMapChange(WorldObject obj)
        {
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            ConditionSourceInfo srcInfo = new(obj);

            obj.GetPhaseShift().VisibleMapIds.Clear();
            obj.GetPhaseShift().UiMapPhaseIds.Clear();
            obj.GetSuppressedPhaseShift().VisibleMapIds.Clear();

            foreach (var (mapId, visibleMapInfo) in Global.ObjectMgr.GetTerrainSwaps())
            {
                if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, visibleMapInfo.Id, srcInfo))
                {
                    if (mapId == obj.GetMapId())
                        phaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);

                    // ui map is visible on all maps
                    foreach (uint uiMapPhaseId in visibleMapInfo.UiMapPhaseIDs)
                        phaseShift.AddUiMapPhaseId(uiMapPhaseId);
                }
                else if(mapId == obj.GetMapId())
                    suppressedPhaseShift.AddVisibleMapId(visibleMapInfo.Id, visibleMapInfo);
            }

            UpdateVisibilityIfNeeded(obj, false, true);
        }

        public static void OnAreaChange(WorldObject obj)
        {
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            var oldPhases = phaseShift.GetPhases(); // for comparison
            ConditionSourceInfo srcInfo = new(obj);

            obj.GetPhaseShift().ClearPhases();
            obj.GetSuppressedPhaseShift().ClearPhases();

            uint areaId = obj.GetAreaId();
            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
            while (areaEntry != null)
            {
                var newAreaPhases = Global.ObjectMgr.GetPhasesForArea(areaEntry.Id);
                if (!newAreaPhases.Empty())
                {
                    foreach (PhaseAreaInfo phaseArea in newAreaPhases)
                    {
                        if (phaseArea.SubAreaExclusions.Contains(areaId))
                            continue;

                        uint phaseId = phaseArea.PhaseInfo.Id;
                        if (Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, phaseArea.Conditions))
                            phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
                        else
                            suppressedPhaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), phaseArea.Conditions);
                    }
                }

                areaEntry = CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
            }

            bool changed = phaseShift.GetPhases() != oldPhases;
            Unit unit = obj.ToUnit();
            if (unit)
            {
                foreach (AuraEffect aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
                {
                    uint phaseId = (uint)aurEff.GetMiscValueB();
                    changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
                }

                foreach (AuraEffect aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
                {
                    var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.GetMiscValueB());
                    foreach (uint phaseId in phasesInGroup)
                        changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
                }

                if (phaseShift.PersonalReferences != 0)
                    phaseShift.PersonalGuid = unit.GetGUID();

                if (changed)
                    unit.OnPhaseChange();

                ControlledUnitVisitor visitor = new(unit);
                visitor.VisitControlledOf(unit, controlled =>
                {
                    InheritPhaseShift(controlled, unit);
                });

                if (changed)
                    unit.RemoveNotOwnSingleTargetAuras(true);
            }
            else
            {
                if (phaseShift.PersonalReferences != 0)
                    phaseShift.PersonalGuid = obj.GetGUID();
            }

            UpdateVisibilityIfNeeded(obj, true, changed);
        }

        public static void OnConditionChange(WorldObject obj)
        {
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            PhaseShift newSuppressions = new();
            ConditionSourceInfo srcInfo = new(obj);
            bool changed = false;

            foreach (var pair in phaseShift.Phases.ToList())
            {
                if (pair.Value.AreaConditions != null && !Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
                {
                    newSuppressions.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References);
                    phaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
                    phaseShift.Phases.Remove(pair.Key);
                }
            }

            foreach (var pair in suppressedPhaseShift.Phases.ToList())
            {
                if (Global.ConditionMgr.IsObjectMeetToConditions(srcInfo, pair.Value.AreaConditions))
                {
                    changed = phaseShift.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References) || changed;
                    suppressedPhaseShift.ModifyPhasesReferences(pair.Key, pair.Value, -pair.Value.References);
                    suppressedPhaseShift.Phases.Remove(pair.Key);
                }
            }

            foreach (var pair in phaseShift.VisibleMapIds.ToList())
            {
                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
                {
                    newSuppressions.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References);
                    foreach (uint uiMapPhaseId in pair.Value.VisibleMapInfo.UiMapPhaseIDs)
                        changed = phaseShift.RemoveUiMapPhaseId(uiMapPhaseId) || changed;

                    phaseShift.VisibleMapIds.Remove(pair.Key);
                }
            }

            foreach (var pair in suppressedPhaseShift.VisibleMapIds.ToList())
            {
                if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Key, srcInfo))
                {
                    changed = phaseShift.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References) || changed;
                    foreach (uint uiMapPhaseId in pair.Value.VisibleMapInfo.UiMapPhaseIDs)
                        changed = phaseShift.AddUiMapPhaseId(uiMapPhaseId) || changed;

                    suppressedPhaseShift.VisibleMapIds.Remove(pair.Key);
                }
            }

            Unit unit = obj.ToUnit();
            if (unit)
            {
                foreach (AuraEffect aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
                {
                    uint phaseId = (uint)aurEff.GetMiscValueB();
                    // if condition was met previously there is nothing to erase
                    if (newSuppressions.RemovePhase(phaseId))
                        phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);//todo needs checked
                }

                foreach (AuraEffect aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
                {
                    var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.GetMiscValueB());
                    if (!phasesInGroup.Empty())
                    {
                        foreach (uint phaseId in phasesInGroup)
                        {
                            var eraseResult = newSuppressions.RemovePhase(phaseId);
                            // if condition was met previously there is nothing to erase
                            if (eraseResult)
                                phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
                        }
                    }
                }
            }

            if (phaseShift.PersonalReferences != 0)
                phaseShift.PersonalGuid = obj.GetGUID();

            changed = changed || !newSuppressions.Phases.Empty() || !newSuppressions.VisibleMapIds.Empty();
            foreach (var pair in newSuppressions.Phases)
                suppressedPhaseShift.AddPhase(pair.Key, pair.Value.Flags, pair.Value.AreaConditions, pair.Value.References);

            foreach (var pair in newSuppressions.VisibleMapIds)
                suppressedPhaseShift.AddVisibleMapId(pair.Key, pair.Value.VisibleMapInfo, pair.Value.References);

            if (unit)
            {
                if (changed)
                    unit.OnPhaseChange();

                ControlledUnitVisitor visitor = new(unit);
                visitor.VisitControlledOf(unit, controlled =>
                {
                    InheritPhaseShift(controlled, unit);
                });

                if (changed)
                    unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, true, changed);
        }

        public static void SendToPlayer(Player player, PhaseShift phaseShift)
        {
            PhaseShiftChange phaseShiftChange = new();
            phaseShiftChange.Client = player.GetGUID();
            phaseShiftChange.Phaseshift.PhaseShiftFlags = (uint)phaseShift.Flags;
            phaseShiftChange.Phaseshift.PersonalGUID = phaseShift.PersonalGuid;

            foreach (var pair in phaseShift.Phases)
                phaseShiftChange.Phaseshift.Phases.Add(new PhaseShiftDataPhase((uint)pair.Value.Flags, pair.Key));

            foreach (var visibleMapId in phaseShift.VisibleMapIds)
                phaseShiftChange.VisibleMapIDs.Add((ushort)visibleMapId.Key);

            foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
                phaseShiftChange.UiMapPhaseIDs.Add((ushort)uiWorldMapAreaIdSwap.Key);

            player.SendPacket(phaseShiftChange);
        }

        public static void SendToPlayer(Player player)
        {
            SendToPlayer(player, player.GetPhaseShift());
        }

        public static void FillPartyMemberPhase(PartyMemberPhaseStates partyMemberPhases, PhaseShift phaseShift)
        {
            partyMemberPhases.PhaseShiftFlags = (int)phaseShift.Flags;
            partyMemberPhases.PersonalGUID = phaseShift.PersonalGuid;

            foreach (var pair in phaseShift.Phases)
                partyMemberPhases.List.Add(new PartyMemberPhase((uint)pair.Value.Flags, pair.Key));
        }

        public static PhaseShift GetAlwaysVisiblePhaseShift()
        {
            return AlwaysVisible;
        }
        
        public static void InitDbPhaseShift(PhaseShift phaseShift, PhaseUseFlagsValues phaseUseFlags, uint phaseId, uint phaseGroupId)
        {
            phaseShift.ClearPhases();
            phaseShift.IsDbPhaseShift = true;

            PhaseShiftFlags flags = PhaseShiftFlags.None;
            if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible))
                flags = flags | PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Unphased;
            if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
                flags |= PhaseShiftFlags.Inverse;

            if (phaseId != 0)
                phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
            else
            {
                var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
                foreach (uint phaseInGroup in phasesInGroup)
                    phaseShift.AddPhase(phaseInGroup, GetPhaseFlags(phaseInGroup), null);
            }

            if (phaseShift.Phases.Empty() || phaseShift.HasPhase(169))
            {
                if (flags.HasFlag(PhaseShiftFlags.Inverse))
                    flags |= PhaseShiftFlags.InverseUnphased;
                else
                    flags |= PhaseShiftFlags.Unphased;
            }

            phaseShift.Flags = flags;
        }

        public static void InitDbPersonalOwnership(PhaseShift phaseShift, ObjectGuid personalGuid)
        {
            Cypher.Assert(phaseShift.IsDbPhaseShift);
            Cypher.Assert(phaseShift.HasPersonalPhase());
            phaseShift.PersonalGuid = personalGuid;
        }

        public static void InitDbVisibleMapId(PhaseShift phaseShift, int visibleMapId)
        {
            phaseShift.VisibleMapIds.Clear();
            if (visibleMapId != -1)
                phaseShift.AddVisibleMapId((uint)visibleMapId, Global.ObjectMgr.GetTerrainSwapInfo((uint)visibleMapId));
        }

        public static bool InDbPhaseShift(WorldObject obj, PhaseUseFlagsValues phaseUseFlags, ushort phaseId, uint phaseGroupId)
        {
            PhaseShift phaseShift = new();
            InitDbPhaseShift(phaseShift, phaseUseFlags, phaseId, phaseGroupId);
            return obj.GetPhaseShift().CanSee(phaseShift);
        }

        public static uint GetTerrainMapId(PhaseShift phaseShift, uint mapId, TerrainInfo terrain, float x, float y)
        {
            if (phaseShift.VisibleMapIds.Empty())
                return mapId;

            if (phaseShift.VisibleMapIds.Count == 1)
                return phaseShift.VisibleMapIds.First().Key;

            GridCoord gridCoord = GridDefines.ComputeGridCoord(x, y);
            int gx = (int)((MapConst.MaxGrids - 1) - gridCoord.X_coord);
            int gy = (int)((MapConst.MaxGrids - 1) - gridCoord.Y_coord);

            foreach (var visibleMap in phaseShift.VisibleMapIds)
                if (terrain.HasChildTerrainGridFile(visibleMap.Key, gx, gy))
                    return visibleMap.Key;

            return mapId;
        }

        public static void SetAlwaysVisible(WorldObject obj, bool apply, bool updateVisibility)
        {
            if (apply)
                obj.GetPhaseShift().Flags |= PhaseShiftFlags.AlwaysVisible;
            else
                obj.GetPhaseShift().Flags &= ~PhaseShiftFlags.AlwaysVisible;

            UpdateVisibilityIfNeeded(obj, updateVisibility, true);
        }

        public static void SetInversed(WorldObject obj, bool apply, bool updateVisibility)
        {
            if (apply)
                obj.GetPhaseShift().Flags |= PhaseShiftFlags.Inverse;
            else
                obj.GetPhaseShift().Flags &= ~PhaseShiftFlags.Inverse;

            obj.GetPhaseShift().UpdateUnphasedFlag();

            UpdateVisibilityIfNeeded(obj, updateVisibility, true);
        }

        public static void PrintToChat(CommandHandler chat, WorldObject target)
        {
            PhaseShift phaseShift = target.GetPhaseShift();

            string phaseOwnerName = "N/A";
            if (phaseShift.HasPersonalPhase())
            {
                WorldObject personalGuid = Global.ObjAccessor.GetWorldObject(target, phaseShift.PersonalGuid);
                if (personalGuid != null)
                    phaseOwnerName = personalGuid.GetName();
            }

            chat.SendSysMessage(CypherStrings.PhaseshiftStatus, phaseShift.Flags, phaseShift.PersonalGuid.ToString(), phaseOwnerName);

            if (!phaseShift.Phases.Empty())
            {
                StringBuilder phases = new();
                string cosmetic = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagCosmetic, chat.GetSessionDbcLocale());
                string personal = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagPersonal, chat.GetSessionDbcLocale());
                foreach (var pair in phaseShift.Phases)
                {
                    phases.Append("\r\n");
                    phases.Append("   ");
                    phases.Append($"{pair.Key} ({Global.ObjectMgr.GetPhaseName(pair.Key)})'");
                    if (pair.Value.Flags.HasFlag(PhaseFlags.Cosmetic))
                        phases.Append($" ({cosmetic})");
                    if (pair.Value.Flags.HasFlag(PhaseFlags.Personal))
                        phases.Append($" ({personal})");
                }

                chat.SendSysMessage(CypherStrings.PhaseshiftPhases, phases.ToString());
            }

            if (!phaseShift.VisibleMapIds.Empty())
            {
                StringBuilder visibleMapIds = new();
                foreach (var visibleMapId in phaseShift.VisibleMapIds)
                    visibleMapIds.Append(visibleMapId.Key + ',' + ' ');

                chat.SendSysMessage(CypherStrings.PhaseshiftVisibleMapIds, visibleMapIds.ToString());
            }

            if (!phaseShift.UiMapPhaseIds.Empty())
            {
                StringBuilder uiWorldMapAreaIdSwaps = new();
                foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
                    uiWorldMapAreaIdSwaps.AppendFormat($"{uiWorldMapAreaIdSwap.Key}, ");

                chat.SendSysMessage(CypherStrings.PhaseshiftUiWorldMapAreaSwaps, uiWorldMapAreaIdSwaps.ToString());
            }
        }

        public static string FormatPhases(PhaseShift phaseShift)
        {
            StringBuilder phases = new();
            foreach (var phaseId in phaseShift.Phases.Keys)
                phases.Append(phaseId + ',');

            return phases.ToString();
        }

        public static bool IsPersonalPhase(uint phaseId)
        {
            var phase = CliDB.PhaseStorage.LookupByKey(phaseId);
            if (phase != null)
                return phase.Flags.HasFlag(PhaseEntryFlags.Personal);

            return false;
        }
        
        static void UpdateVisibilityIfNeeded(WorldObject obj, bool updateVisibility, bool changed)
        {
            if (changed && obj.IsInWorld)
            {
                Player player = obj.ToPlayer();
                if (player)
                    SendToPlayer(player);

                if (updateVisibility)
                {
                    if (player)
                        player.GetMap().SendUpdateTransportVisibility(player);

                    obj.UpdateObjectVisibility();
                }
            }
        }
    }

    class ControlledUnitVisitor
    {
        HashSet<WorldObject> _visited = new();

        public ControlledUnitVisitor(WorldObject owner)
        {
            _visited.Add(owner);
        }

        public void VisitControlledOf(Unit unit, Action<Unit> func)
        {
            foreach (Unit controlled in unit.m_Controlled)
            {
                // Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
                if (!controlled.IsPlayer() && controlled.GetVehicle() == null)
                    if (_visited.Add(controlled))
                        func(controlled);
            }

            foreach (ObjectGuid summonGuid in unit.m_SummonSlot)
            {
                if (!summonGuid.IsEmpty())
                {
                    Creature summon = ObjectAccessor.GetCreature(unit, summonGuid);
                    if (summon != null)
                        if (_visited.Add(summon))
                            func(summon);
                }
            }

            Vehicle vehicle = unit.GetVehicleKit();
            if (vehicle != null)
            {
                foreach (var seatPair in vehicle.Seats)
                {
                    Unit passenger = Global.ObjAccessor.GetUnit(unit, seatPair.Value.Passenger.Guid);
                    if (passenger != null && passenger != unit)
                        if (_visited.Add(passenger))
                            func(passenger);
                }
            }
        }
    }
}
