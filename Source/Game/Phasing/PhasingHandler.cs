using System;
using System.Collections.Generic;
using System.Text;
using Game.DataStorage;
using Game.Entities;
using Framework.Constants;
using Game.Conditions;
using Game.Spells;
using System.Linq;
using Game.Network.Packets;
using Game.Chat;
using Game.Maps;

namespace Game
{
    public class PhasingHandler
    {
        public static PhaseShift EmptyPhaseShift = new PhaseShift();

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
            bool changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null);

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    AddPhase(controlled, phaseId, updateVisibility);
                });
                unit.RemoveNotOwnSingleTargetAuras(true);
            }

            UpdateVisibilityIfNeeded(obj, updateVisibility, changed);
        }

        public static void RemovePhase(WorldObject obj, uint phaseId, bool updateVisibility)
        {
            bool changed = obj.GetPhaseShift().RemovePhase(phaseId);

            Unit unit = obj.ToUnit();
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
            var phasesInGroup = Global.DB2Mgr.GetPhasesForGroup(phaseGroupId);
            if (phasesInGroup.Empty())
                return;

            bool changed = false;
            foreach (uint phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().AddPhase(phaseId, GetPhaseFlags(phaseId), null) || changed;

            Unit unit = obj.ToUnit();
            if (unit)
            {
                unit.OnPhaseChange();
                ForAllControlled(unit, controlled =>
                {
                    AddPhaseGroup(controlled, phaseGroupId, updateVisibility);
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

            bool changed = false;
            foreach (uint phaseId in phasesInGroup)
                changed = obj.GetPhaseShift().RemovePhase(phaseId) || changed;

            Unit unit = obj.ToUnit();
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
            bool changed = obj.GetPhaseShift().AddVisibleMapId(visibleMapId, terrainSwapInfo);

            foreach (uint uiMapPhaseId  in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().AddUiMapPhaseId(uiMapPhaseId ) || changed;

            Unit unit = obj.ToUnit();
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
            bool changed = obj.GetPhaseShift().RemoveVisibleMapId(visibleMapId);

            foreach (uint uiWorldMapAreaIDSwap in terrainSwapInfo.UiMapPhaseIDs)
                changed = obj.GetPhaseShift().RemoveUiMapPhaseId(uiWorldMapAreaIDSwap) || changed;

            Unit unit = obj.ToUnit();
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
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            ConditionSourceInfo srcInfo = new ConditionSourceInfo(obj);

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
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            var oldPhases = phaseShift.GetPhases(); // for comparison
            ConditionSourceInfo srcInfo = new ConditionSourceInfo(obj);

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

        public static void OnConditionChange(WorldObject obj)
        {
            PhaseShift phaseShift = obj.GetPhaseShift();
            PhaseShift suppressedPhaseShift = obj.GetSuppressedPhaseShift();
            PhaseShift newSuppressions = new PhaseShift();
            ConditionSourceInfo srcInfo = new ConditionSourceInfo(obj);
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
            PhaseShiftChange phaseShiftChange = new PhaseShiftChange();
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

        public static void InitDbVisibleMapId(PhaseShift phaseShift, int visibleMapId)
        {
            phaseShift.VisibleMapIds.Clear();
            if (visibleMapId != -1)
                phaseShift.AddVisibleMapId((uint)visibleMapId, Global.ObjectMgr.GetTerrainSwapInfo((uint)visibleMapId));
        }

        public static bool InDbPhaseShift(WorldObject obj, PhaseUseFlagsValues phaseUseFlags, ushort phaseId, uint phaseGroupId)
        {
            PhaseShift phaseShift = new PhaseShift();
            InitDbPhaseShift(phaseShift, phaseUseFlags, phaseId, phaseGroupId);
            return obj.GetPhaseShift().CanSee(phaseShift);
        }

        public static uint GetTerrainMapId(PhaseShift phaseShift, Map map, float x, float y)
        {
            if (phaseShift.VisibleMapIds.Empty())
                return map.GetId();

            if (phaseShift.VisibleMapIds.Count == 1)
                return phaseShift.VisibleMapIds.First().Key;

            GridCoord gridCoord = GridDefines.ComputeGridCoord(x, y);
            uint gx = (uint)((MapConst.MaxGrids - 1) - gridCoord.x_coord);
            uint gy = (uint)((MapConst.MaxGrids - 1) - gridCoord.y_coord);

            uint gxbegin = Math.Max(gx - 1, 0);
            uint gxend = Math.Min(gx + 1, MapConst.MaxGrids);
            uint gybegin = Math.Max(gy - 1, 0);
            uint gyend = Math.Min(gy + 1, MapConst.MaxGrids);

            foreach (var itr in phaseShift.VisibleMapIds)
                for (uint gxi = gxbegin; gxi < gxend; ++gxi)
                    for (uint gyi = gybegin; gyi < gyend; ++gyi)
                        if (map.HasGridMap(itr.Key, gxi, gyi))
                            return itr.Key;

            return map.GetId();
        }

        public static void SetAlwaysVisible(PhaseShift phaseShift, bool apply)
        {
            if (apply)
                phaseShift.Flags |= PhaseShiftFlags.AlwaysVisible;
            else
                phaseShift.Flags &= ~PhaseShiftFlags.AlwaysVisible;
        }

        public static void SetInversed(PhaseShift phaseShift, bool apply)
        {
            if (apply)
                phaseShift.Flags |= PhaseShiftFlags.Inverse;
            else
                phaseShift.Flags &= ~PhaseShiftFlags.Inverse;

            phaseShift.UpdateUnphasedFlag();
        }

        public static void PrintToChat(CommandHandler chat, PhaseShift phaseShift)
        {
            chat.SendSysMessage(CypherStrings.PhaseshiftStatus, phaseShift.Flags, phaseShift.PersonalGuid.ToString());
            if (!phaseShift.Phases.Empty())
            {
                StringBuilder phases = new StringBuilder();
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
                StringBuilder visibleMapIds = new StringBuilder();
                foreach (var visibleMapId in phaseShift.VisibleMapIds)
                    visibleMapIds.Append(visibleMapId.Key + ',' + ' ');

                chat.SendSysMessage(CypherStrings.PhaseshiftVisibleMapIds, visibleMapIds.ToString());
            }

            if (!phaseShift.UiMapPhaseIds.Empty())
            {
                StringBuilder uiWorldMapAreaIdSwaps = new StringBuilder();
                foreach (var uiWorldMapAreaIdSwap in phaseShift.UiMapPhaseIds)
                    uiWorldMapAreaIdSwaps.Append(uiWorldMapAreaIdSwap.Key + ',' + ' ');

                chat.SendSysMessage(CypherStrings.PhaseshiftUiWorldMapAreaSwaps, uiWorldMapAreaIdSwaps.ToString());
            }
        }

        public static string FormatPhases(PhaseShift phaseShift)
        {
            StringBuilder phases = new StringBuilder();
            foreach (var phaseId in phaseShift.Phases.Keys)
                phases.Append(phaseId + ',');

            return phases.ToString();
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
}
