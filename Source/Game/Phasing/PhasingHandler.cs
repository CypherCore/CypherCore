﻿using System;
using System.Collections.Generic;
using System.Text;
using Game.DataStorage;
using Game.Entities;
using Framework.Constants;
using Game.Conditions;
using Game.Spells;
using System.Linq;
using Game.Networking.Packets;
using Game.Chat;
using Game.Maps;

namespace Game
{
    public class PhasingHandler
    {
        public static PhaseShift EmptyPhaseShift = new PhaseShift();

        public static PhaseFlags GetPhaseFlags(uint phaseId)
        {
            var phase = CliDB.PhaseStorage.LookupByKey(phaseId);
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
                var controlled = unit.m_Controlled[i];
                if (controlled.GetTypeId() != TypeId.Player)
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
        }

        public static void AddPhase(WorldObject obj, uint phaseId, bool updateVisibility)
        {
            AddPhase(obj, phaseId, obj.GetGUID(), updateVisibility);
        }

        private static void AddPhase(WorldObject obj, uint phaseId, ObjectGuid personalGuid, bool updateVisibility)
        {
            var changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null);

            if (obj.GetPhaseShift().PersonalReferences != 0)
                obj.GetPhaseShift().PersonalGuid = personalGuid;

            var unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    AddPhase(controlled, phaseId, personalGuid, updateVisibility);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility)
        {
            var changed = obj.GetPhaseShift().RemovePhase(phaseId);

            var unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    RemovePhase(controlled, phaseId, updateVisibility);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void AddPhaseGroup(WorldObject obj, uint phaseGroupId, bool updateVisibility)
        {
            AddPhaseGroup(obj, phaseGroupId, obj.GetGUID(), updateVisibility);
        }

        private static void AddPhaseGroup(WorldObject obj, uint phaseGroupId, ObjectGuid personalGuid, bool updateVisibility)
        {
            var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
            if (phasesInGroup.Empty())
                return;

            var changed = false;
            foreach (var phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;

            if (obj.GetPhaseShift().PersonalReferences != 0)
                obj.GetPhaseShift().PersonalGuid = personalGuid;

            var unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    AddPhaseGroup(controlled, phaseGroupId, personalGuid, updateVisibility);
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

            var changed = false;
            foreach (var phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().RemovePhase(phaseId) || changed;

            var unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    RemovePhaseGroup(controlled, phaseGroupId, updateVisibility);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void AddVisibleMapId(WorldObject obj, uint visibleMapId)
        {
            TerrainSwapInfo terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
            var changed = obj.GetPhaseShift().AddVisibleMapId(visibleMapId, terrainSwapInfo);

            foreach (uint uiMapPhaseId  in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().AddUiMapPhaseId(uiMapPhaseId ) || changed;

            var unit = obj.ToUnit();
            if (unit)
            {
                ForAllControlled(unit, controlled =>
                {
                    AddVisibleMapId(controlled, visibleMapId);
                });
            }

            UpdateVisibilityIfNeeded(obj, false, changed);
        }

        public static void RemoveVisibleMapId(WorldObject obj, uint visibleMapId)
        {
            TerrainSwapInfo terrainSwapInfo = Global.ObjectMgr.GetTerrainSwapInfo(visibleMapId);
            var changed = obj.GetPhaseShift().RemoveVisibleMapId(visibleMapId);

            foreach (uint uiWorldMapAreaIDSwap in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().RemoveUiMapPhaseId(uiWorldMapAreaIDSwap) || changed;

            var unit = obj.ToUnit();
            if (unit)
            {
                ForAllControlled(unit, controlled =>
                {
                    RemoveVisibleMapId(controlled, visibleMapId);
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
            var phaseShift = obj.GetPhaseShift();
            var suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            var srcInfo = new ConditionSourceInfo(obj);

            obj.GetPhaseShift().VisibleMapIds.Clear();
            obj.GetPhaseShift().UiMapPhaseIds.Clear();
            obj.GetSuppressedPhaseShift().VisibleMapIds.Clear();

            foreach (var pair in Global.ObjectMgr.GetTerrainSwaps())
            {
                if (Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.TerrainSwap, pair.Value.Id, srcInfo))
                {
                    if (pair.Key == obj.GetMapId())
                        phaseShift.AddVisibleMapId(pair.Value.Id, pair.Value);

                    // ui map is visible on all maps
                    foreach (uint uiMapPhaseId in pair.Value.UiMapPhaseIDs)
                        phaseShift.AddUiMapPhaseId(uiMapPhaseId);
                }
                else
                    suppressedPhaseShift.AddVisibleMapId(pair.Value.Id, pair.Value);
            }

            UpdateVisibilityIfNeeded(obj, false, true);
        }

        public static void OnAreaChange(WorldObject obj)
        {
            var phaseShift = obj.GetPhaseShift();
            var suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            var oldPhases = phaseShift.GetPhases(); // for comparison
            var srcInfo = new ConditionSourceInfo(obj);

            obj.GetPhaseShift().ClearPhases();
            obj.GetSuppressedPhaseShift().ClearPhases();

            var areaId = obj.GetAreaId();
            var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
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

            var changed = phaseShift.GetPhases() != oldPhases;
            var unit = obj.ToUnit();
            if (unit)
            {
                foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
                {
                    var phaseId = (uint)aurEff.GetMiscValueB();
                    changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
                }

                foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
                {
                    var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.GetMiscValueB());
                    foreach (var phaseId in phasesInGroup)
                        changed = phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;
                }

                if (phaseShift.PersonalReferences != 0)
                    phaseShift.PersonalGuid = unit.GetGUID();

                if (changed)
                    unit.OnPhaseChange();

                ForAllControlled(unit, controlled =>
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
            var phaseShift = obj.GetPhaseShift();
            var suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            var newSuppressions = new PhaseShift();
            var srcInfo = new ConditionSourceInfo(obj);
            var changed = false;

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

            var unit = obj.ToUnit();
            if (unit)
            {
                foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.Phase))
                {
                    var phaseId = (uint)aurEff.GetMiscValueB();
                    // if condition was met previously there is nothing to erase
                    if (newSuppressions.RemovePhase(phaseId))
                        phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);//todo needs checked
                }

                foreach (var aurEff in unit.GetAuraEffectsByType(AuraType.PhaseGroup))
                {
                    var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup((uint)aurEff.GetMiscValueB());
                    if (!phasesInGroup.Empty())
                    {
                        foreach (var phaseId in phasesInGroup)
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

                ForAllControlled(unit, controlled =>
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
            var phaseShiftChange = new PhaseShiftChange();
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

        public static void InitDbPhaseShift(PhaseShift phaseShift, PhaseUseFlagsValues phaseUseFlags, uint phaseId, uint phaseGroupId)
        {
            phaseShift.ClearPhases();
            phaseShift.IsDbPhaseShift = true;

            var flags = PhaseShiftFlags.None;
            if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible))
                flags = flags | PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Unphased;
            if (phaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
                flags |= PhaseShiftFlags.Inverse;

            if (phaseId != 0)
                phaseShift.AddPhase(phaseId, GetPhaseFlags(phaseId), null);
            else
            {
                var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
                foreach (var phaseInGroup in phasesInGroup)
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

        public static void InitDbVisibleMapId(PhaseShift phaseShift, int visibleMapId)
        {
            phaseShift.VisibleMapIds.Clear();
            if (visibleMapId != -1)
                phaseShift.AddVisibleMapId((uint)visibleMapId, Global.ObjectMgr.GetTerrainSwapInfo((uint)visibleMapId));
        }

        public static bool InDbPhaseShift(WorldObject obj, PhaseUseFlagsValues phaseUseFlags, ushort phaseId, uint phaseGroupId)
        {
            var phaseShift = new PhaseShift();
            InitDbPhaseShift(phaseShift, phaseUseFlags, phaseId, phaseGroupId);
            return obj.GetPhaseShift().CanSee(phaseShift);
        }

        public static uint GetTerrainMapId(PhaseShift phaseShift, Map map, float x, float y)
        {
            if (phaseShift.VisibleMapIds.Empty())
                return map.GetId();

            if (phaseShift.VisibleMapIds.Count == 1)
                return phaseShift.VisibleMapIds.First().Key;

            var gridCoord = GridDefines.ComputeGridCoord(x, y);
            uint gx = ((MapConst.MaxGrids - 1) - gridCoord.X_coord);
            uint gy = ((MapConst.MaxGrids - 1) - gridCoord.Y_coord);

            foreach (var visibleMap in phaseShift.VisibleMapIds)
                if (map.HasChildMapGridFile(visibleMap.Key, gx, gy))
                    return visibleMap.Key;

            return map.GetId();
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

        public static void PrintToChat(CommandHandler chat, PhaseShift phaseShift)
        {
            chat.SendSysMessage(CypherStrings.PhaseshiftStatus, phaseShift.Flags, phaseShift.PersonalGuid.ToString());
            if (!phaseShift.Phases.Empty())
            {
                var phases = new StringBuilder();
                string cosmetic = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagCosmetic, chat.GetSessionDbcLocale());
                string personal = Global.ObjectMgr.GetCypherString(CypherStrings.PhaseFlagPersonal, chat.GetSessionDbcLocale());
                foreach (var pair in phaseShift.Phases)
                {
                    phases.Append(pair.Key);
                    if (pair.Value.Flags.HasFlag(PhaseFlags.Cosmetic))
                        phases.Append(' ' + '(' + cosmetic + ')');
                    if (pair.Value.Flags.HasFlag(PhaseFlags.Personal))
                        phases.Append(' ' + '(' + personal + ')');
                    phases.Append(", ");
                }

                chat.SendSysMessage(CypherStrings.PhaseshiftPhases, phases.ToString());
            }

            if (!phaseShift.VisibleMapIds.Empty())
            {
                var visibleMapIds = new StringBuilder();
                foreach (var visibleMapId in phaseShift.VisibleMapIds)
                    visibleMapIds.Append(visibleMapId.Key + ',' + ' ');

                chat.SendSysMessage(CypherStrings.PhaseshiftVisibleMapIds, visibleMapIds.ToString());
            }

            if (!phaseShift.UiMapPhaseIds.Empty())
            {
                var uiWorldMapAreaIdSwaps = new StringBuilder();
                foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
                    uiWorldMapAreaIdSwaps.Append(uiWorldMapAreaIdSwap.Key + ',' + ' ');

                chat.SendSysMessage(CypherStrings.PhaseshiftUiWorldMapAreaSwaps, uiWorldMapAreaIdSwaps.ToString());
            }
        }

        public static string FormatPhases(PhaseShift phaseShift)
        {
            var phases = new StringBuilder();
            foreach (var phaseId in phaseShift.Phases.Keys)
                phases.Append(phaseId + ',');

            return phases.ToString();
        }

        private static void UpdateVisibilityIfNeeded(WorldObject obj, bool updateVisibility, bool changed)
        {
            if (changed && obj.IsInWorld)
            {
                var player = obj.ToPlayer();
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
}
