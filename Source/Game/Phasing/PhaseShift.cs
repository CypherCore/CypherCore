/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using System;
using System.Collections.Generic;
using System.Text;
using Game.Conditions;
using Game.Entities;
using System.Collections.Concurrent;
using System.Linq;

namespace Game
{
    public class PhaseShift
    {
        public bool AddPhase(uint phaseId, PhaseFlags flags, List<Condition> areaConditions, int references = 1)
        {
            var phase = new PhaseRef(phaseId, flags, null);
            ModifyPhasesReferences(phase, references);
            if (areaConditions != null)
                phase.AreaConditions = areaConditions;

            Phases.Add(phase);
            return true;
        }

        public bool RemovePhase(uint phaseId)
        {
            var phaseRef = new PhaseRef(phaseId, PhaseFlags.None, null);
            if (Phases.Contains(phaseRef))
            {
                ModifyPhasesReferences(phaseRef, -1);
                if (phaseRef.References == 0)
                {
                    Phases.Remove(phaseRef);
                    return true;
                }

                return false;
            }
            return false;
        }

        public bool AddVisibleMapId(uint visibleMapId, TerrainSwapInfo visibleMapInfo, int references = 1)
        {
            VisibleMapIds.Add(visibleMapId, new VisibleMapIdRef(references, visibleMapInfo));
            return true; //Tryadd?  maybe Concurrent
        }

        public bool RemoveVisibleMapId(uint visibleMapId)
        {
            if (VisibleMapIds.ContainsKey(visibleMapId))
            {
                var mapIdRef = VisibleMapIds[visibleMapId];
                if ((--mapIdRef.References) == 0)
                {
                    VisibleMapIds.Remove(visibleMapId);
                    return true;
                }

                return false;
            }

            return false;
        }

        public bool AddUiWorldMapAreaIdSwap(uint uiWorldMapAreaId, int references = 1)
        {
            UiWorldMapAreaIdSwaps.Add(uiWorldMapAreaId, new UiWorldMapAreaIdSwapRef(references));
            return true; //Tryadd?  maybe Concurrent
        }

        public bool RemoveUiWorldMapAreaIdSwap(uint uiWorldMapAreaId)
        {           
            if (UiWorldMapAreaIdSwaps.ContainsKey(uiWorldMapAreaId))
            {
                var value = UiWorldMapAreaIdSwaps[uiWorldMapAreaId];
                if ((--value.References) == 0)
                {
                    UiWorldMapAreaIdSwaps.Remove(uiWorldMapAreaId);
                    return true;
                }

                return false;
            }

            return false;
        }

        public void Clear()
        {
            ClearPhases();
            PersonalGuid.Clear();
            VisibleMapIds.Clear();
            UiWorldMapAreaIdSwaps.Clear();
        }

        public void ClearPhases()
        {
            Flags &= PhaseShiftFlags.AlwaysVisible | PhaseShiftFlags.Inverse;
            Phases.Clear();
            NonCosmeticReferences = 0;
            CosmeticReferences = 0;
            DefaultReferences = 0;
            UpdateUnphasedFlag();
        }

        public bool CanSee(PhaseShift other)
        {
            if (Flags.HasFlag(PhaseShiftFlags.Unphased) && other.Flags.HasFlag(PhaseShiftFlags.Unphased))
                return true;
            if (Flags.HasFlag(PhaseShiftFlags.AlwaysVisible) || other.Flags.HasFlag(PhaseShiftFlags.AlwaysVisible))
                return true;
            if (Flags.HasFlag(PhaseShiftFlags.Inverse) && other.Flags.HasFlag(PhaseShiftFlags.Inverse))
                return true;

            PhaseFlags excludePhasesWithFlag = PhaseFlags.None;
            if (Flags.HasFlag(PhaseShiftFlags.NoCosmetic) && other.Flags.HasFlag(PhaseShiftFlags.NoCosmetic))
                excludePhasesWithFlag = PhaseFlags.Cosmetic;

            if (!Flags.HasFlag(PhaseShiftFlags.Inverse) && !other.Flags.HasFlag(PhaseShiftFlags.Inverse))
            {
                ObjectGuid ownerGuid = PersonalGuid;
                ObjectGuid otherPersonalGuid = other.PersonalGuid;
                return Phases.Intersect(other.Phases, (myPhase, otherPhase) => !myPhase.Flags.HasAnyFlag(excludePhasesWithFlag) && (!myPhase.Flags.HasFlag(PhaseFlags.Personal) || ownerGuid == otherPersonalGuid)).Any();
            }

            var checkInversePhaseShift = new Func<PhaseShift, PhaseShift, bool>((phaseShift, excludedPhaseShift) =>
            {
                if (phaseShift.Flags.HasFlag(PhaseShiftFlags.Unphased) && !excludedPhaseShift.Flags.HasFlag(PhaseShiftFlags.InverseUnphased))
                    return true;

                foreach (var itr in phaseShift.Phases)
                {
                    if (itr.Flags.HasAnyFlag(excludePhasesWithFlag))
                        continue;

                    var index = excludedPhaseShift.Phases.IndexOf(itr);
                    if (index == -1 || excludedPhaseShift.Phases[index].Flags.HasAnyFlag(excludePhasesWithFlag))
                        return true;
                }

                return false;
            });

            if (other.Flags.HasFlag(PhaseShiftFlags.Inverse))
                return checkInversePhaseShift(this, other);

            return checkInversePhaseShift(other, this);
        }

        public void ModifyPhasesReferences(PhaseRef phaseRef, int references)
        {
            phaseRef.References += references;

            if (!IsDbPhaseShift)
            {
                if (phaseRef.Flags.HasAnyFlag(PhaseFlags.Cosmetic))
                    CosmeticReferences += references;
                else if (phaseRef.Id != 169)
                    NonCosmeticReferences += references;
                else
                    DefaultReferences += references;

                if (CosmeticReferences != 0)
                    Flags |= PhaseShiftFlags.NoCosmetic;
                else
                    Flags &= ~PhaseShiftFlags.NoCosmetic;

                UpdateUnphasedFlag();
            }
        }

        public void UpdateUnphasedFlag()
        {
            PhaseShiftFlags unphasedFlag = !Flags.HasAnyFlag(PhaseShiftFlags.Inverse) ? PhaseShiftFlags.Unphased : PhaseShiftFlags.InverseUnphased;
            Flags &= ~(!Flags.HasFlag(PhaseShiftFlags.Inverse) ? PhaseShiftFlags.InverseUnphased : PhaseShiftFlags.Unphased);
            if (NonCosmeticReferences != 0 && DefaultReferences == 0)
                Flags &= ~unphasedFlag;
            else
                Flags |= unphasedFlag;
        }

        public bool HasPhase(uint phaseId) { return Phases.Contains(new PhaseRef(phaseId, PhaseFlags.None, null)); }
        public List<PhaseRef> GetPhases() { return Phases; }

        public bool HasVisibleMapId(uint visibleMapId) { return VisibleMapIds.ContainsKey(visibleMapId); }
        public Dictionary<uint, VisibleMapIdRef> GetVisibleMapIds() { return VisibleMapIds; }

        public bool HasUiWorldMapAreaIdSwap(uint uiWorldMapAreaId) { return UiWorldMapAreaIdSwaps.ContainsKey(uiWorldMapAreaId); }
        public Dictionary<uint, UiWorldMapAreaIdSwapRef> GetUiWorldMapAreaIdSwaps() { return UiWorldMapAreaIdSwaps; }

        public PhaseShiftFlags Flags = PhaseShiftFlags.Unphased;
        public ObjectGuid PersonalGuid;
        public List<PhaseRef> Phases = new List<PhaseRef>();
        public Dictionary<uint, VisibleMapIdRef> VisibleMapIds = new Dictionary<uint, VisibleMapIdRef>();
        public Dictionary<uint, UiWorldMapAreaIdSwapRef> UiWorldMapAreaIdSwaps = new Dictionary<uint, UiWorldMapAreaIdSwapRef>();

        int NonCosmeticReferences;
        int CosmeticReferences;
        int DefaultReferences;
        public bool IsDbPhaseShift;
    }

    public struct PhaseRef
    {
        public PhaseRef(uint id, PhaseFlags flags, List<Condition> conditions)
        {
            Id = id;
            Flags = flags;
            References = 0;
            AreaConditions = conditions;
        }

        public uint Id;
        public PhaseFlags Flags;
        public int References;
        public List<Condition> AreaConditions;

        public static bool operator <(PhaseRef left, PhaseRef right)
        {
            return left.Id < right.Id;
        }

        public static bool operator >(PhaseRef left, PhaseRef right)
        {
            return left.Id > right.Id;
        }

        public static bool operator ==(PhaseRef left, PhaseRef right) { return left.Id == right.Id; }
        public static bool operator !=(PhaseRef left, PhaseRef right) { return !(left == right); }

        public override bool Equals(object obj)
        {
            if (obj is PhaseRef)
                return (PhaseRef)obj == this;

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Flags.GetHashCode() ^ References.GetHashCode() ^ AreaConditions.GetHashCode();
        }
    }

    public struct VisibleMapIdRef
    {
        public VisibleMapIdRef(int references, TerrainSwapInfo visibleMapInfo)
        {
            References = references;
            VisibleMapInfo = visibleMapInfo;
        }

        public int References;
        public TerrainSwapInfo VisibleMapInfo;
    }

    public struct UiWorldMapAreaIdSwapRef
    {
        public UiWorldMapAreaIdSwapRef(int references)
        {
            References = references;
        }

        public int References;
    }

    public enum PhaseShiftFlags
    {
        None = 0x00,
        AlwaysVisible = 0x01, // Ignores all phasing, can see everything and be seen by everything
        Inverse = 0x02, // By default having at least one shared phase for two objects means they can see each other
                        // this flag makes objects see each other if they have at least one non-shared phase
        InverseUnphased = 0x04,
        Unphased = 0x08,
        NoCosmetic = 0x10  // This flag ignores shared cosmetic phases (two players that both have shared cosmetic phase but no other phase cannot see each other)
    }

    public enum PhaseFlags : ushort
    {
        None = 0x0,
        Cosmetic = 0x1,
        Personal = 0x2
    }
}
