// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Game.Entities
{
    public enum EntityFragment
    {
        CGObject = 0, //  UPDATEABLE, INDIRECT,
        Tag_Item = 1, //  TAG,
        Tag_Container = 2, //  TAG,
        Tag_AzeriteEmpoweredItem = 3, //  TAG,
        Tag_AzeriteItem = 4, //  TAG,
        Tag_Unit = 5, //  TAG,
        Tag_Player = 6, //  TAG,
        Tag_GameObject = 7, //  TAG,
        Tag_DynamicObject = 8, //  TAG,
        Tag_Corpse = 9, //  TAG,
        Tag_AreaTrigger = 10, //  TAG,
        Tag_SceneObject = 11, //  TAG,
        Tag_Conversation = 12, //  TAG,
        Tag_AIGroup = 13, //  TAG,
        Tag_Scenario = 14, //  TAG,
        Tag_LootObject = 15, //  TAG,
        Tag_ActivePlayer = 16, //  TAG,
        Tag_ActiveClient_S = 17, //  TAG,
        Tag_ActiveObject_C = 18, //  TAG,
        Tag_VisibleObject_C = 19, //  TAG,
        Tag_UnitVehicle = 20, //  TAG,
        FEntityPosition = 112,
        FEntityLocalMatrix = 113,
        FEntityWorldMatrix = 114,
        CActor = 115, //  INDIRECT,
        FVendor_C = 117, //  UPDATEABLE,
        FMirroredObject_C = 119,
        End = 255,
    }

    public enum EntityFragmentSerializationType
    {
        Full = 0,
        Partial = 1
    }

    class EntityDefinitionsConst
    {
        // common case optimization, make use of the fact that fragment arrays are sorted
        public const byte CGObjectActiveMask = 0x1;
        public const byte CGObjectChangedMask = 0x2;
        public const byte CGObjectUpdateMask = CGObjectActiveMask | CGObjectChangedMask;

    }

    public class EntityFragmentsHolder
    {
        EntityFragment[] Ids =
        [
            EntityFragment.End, EntityFragment.End, EntityFragment.End, EntityFragment.End,
            EntityFragment.End, EntityFragment.End, EntityFragment.End, EntityFragment.End
        ];

        public byte Count;
        public bool IdsChanged;

        public EntityFragment[] UpdateableIds = [EntityFragment.End, EntityFragment.End];
        public byte[] UpdateableMasks = new byte[2];
        public byte UpdateableCount;
        public byte ContentsChangedMask;

        public void Add(EntityFragment fragment, bool update)
        {
            Cypher.Assert(Count < Ids.Length);

            (int, bool) insertSorted(ref EntityFragment[] arr, ref byte count, EntityFragment f)
            {
                //auto where = std::ranges::lower_bound(arr.begin(), arr.begin() + count, f);
                var where = Array.IndexOf(arr, f);
                if (where != -1)
                    return (where, false);

                arr.SetValue(f, where);
                arr = arr[^1..^0].Concat(arr[..^1]).ToArray();
                ++count;
                return (where, true);
            };

            if (!insertSorted(ref Ids, ref Count, fragment).Item2)
                return;

            if (IsUpdateableFragment(fragment))
            {
                Cypher.Assert(UpdateableCount < UpdateableIds.Length);

                var index = insertSorted(ref UpdateableIds, ref UpdateableCount, fragment).Item1;
                byte maskLowPart = (byte)(ContentsChangedMask & ((1 << index) - 1));
                byte maskHighPart = (byte)((ContentsChangedMask & ~((1 << index) - 1)) << (1 + (IsIndirectFragment(fragment) ? 1 : 0)));
                ContentsChangedMask = (byte)(maskLowPart | maskHighPart);
                for (byte i = 0, maskIndex = 0; i < UpdateableCount; ++i)
                {
                    UpdateableMasks[i] = (byte)(1 << maskIndex++);
                    if (IsIndirectFragment(UpdateableIds[i]))
                    {
                        ContentsChangedMask |= UpdateableMasks[i]; // set the first bit to true to activate fragment
                        UpdateableMasks[i] |= (byte)(1 << maskIndex++);
                    }
                }
            }

            if (update)
                IdsChanged = true;
        }

        public void Remove(EntityFragment fragment)
        {
            (int, bool) removeSorted(ref EntityFragment[] arr, ref byte count, EntityFragment f)
            {
                var where = Array.IndexOf(arr, f);
                if (where != -1)
                {
                    arr.SetValue(EntityFragment.End, where);
                    arr = arr[^1..^0].Concat(arr[..^1]).ToArray();
                    --count;
                    return (where, true);
                }
                return (where, false);
            };

            if (!removeSorted(ref Ids, ref Count, fragment).Item2)
                return;

            if (IsUpdateableFragment(fragment))
            {
                var (index, removed) = removeSorted(ref UpdateableIds, ref UpdateableCount, fragment);
                if (removed)
                {
                    byte maskLowPart = (byte)(ContentsChangedMask & ((1 << index) - 1));
                    byte maskHighPart = (byte)((ContentsChangedMask & ~((1 << index) - 1)) >> (1 + (IsIndirectFragment(fragment) ? 1 : 0)));
                    ContentsChangedMask = (byte)(maskLowPart | maskHighPart);
                    for (byte i = 0, maskIndex = 0; i < UpdateableCount; ++i)
                    {
                        UpdateableMasks[i] = (byte)(1 << maskIndex++);
                        if (IsIndirectFragment(UpdateableIds[i]))
                            UpdateableMasks[i] |= (byte)(1 << maskIndex++);
                    }
                }
            }

            IdsChanged = true;
        }

        public static bool IsUpdateableFragment(EntityFragment frag)
        {
            return frag == EntityFragment.CGObject || frag == EntityFragment.FVendor_C;
        }

        public static bool IsIndirectFragment(EntityFragment frag)
        {
            return frag == EntityFragment.CGObject || frag == EntityFragment.CActor;
        }

        public EntityFragment[] GetIds() { return Ids[..Count]; }
        public EntityFragment[] GetUpdateableIds() { return UpdateableIds[..UpdateableCount]; }

        public byte GetUpdateMaskFor(EntityFragment fragment)
        {
            if (fragment == EntityFragment.CGObject)   // common case optimization, make use of the fact that fragment arrays are sorted
                return EntityDefinitionsConst.CGObjectChangedMask;

            for (byte i = 1; i < UpdateableCount; ++i)
                if (UpdateableIds[i] == fragment)
                    return UpdateableMasks[i];

            return 0;
        }
    }
}
