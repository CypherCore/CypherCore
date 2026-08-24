// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using System;

namespace Game.Entities
{
    public enum EntityFragment
    {
        FEntityPosition = 1,
        CGObject = 2, //  UPDATEABLE, INDIRECT,
        FTransportLink = 5,
        FPlayerOwnershipLink = 13,
        CActor = 15, //  INDIRECT,
        FVendor_C = 17, //  UPDATEABLE,
        FMirroredObject_C = 18,
        FMeshObjectData_C = 19, //  UPDATEABLE,
        FHousingDecor_C = 20, //  UPDATEABLE,
        FHousingRoom_C = 21, //  UPDATEABLE,
        FHousingRoomComponentMesh_C = 22, //  UPDATEABLE,
        FHousingPlayerHouse_C = 23, //  UPDATEABLE,
        FJamHousingCornerstone_C = 27, //  UPDATEABLE,
        FHousingDecorActor_C = 28,
        FNeighborhoodMirrorData_C = 30, //  UPDATEABLE,
        FMirroredPositionData_C = 31, //  UPDATEABLE,
        PlayerHouseInfoComponent_C = 32, //  UPDATEABLE, INDIRECT,
        FHousingStorage_C = 33, //  UPDATEABLE,
        FHousingFixture_C = 34, //  UPDATEABLE,
        PlayerInitiativeComponent_C = 37, //  UPDATEABLE, INDIRECT,
        FWorldStateListenerData_C = 42,
        Tag_Item = 200, //  TAG,
        Tag_Container = 201, //  TAG,
        Tag_AzeriteEmpoweredItem = 202, //  TAG,
        Tag_AzeriteItem = 203, //  TAG,
        Tag_Unit = 204, //  TAG,
        Tag_Player = 205, //  TAG,
        Tag_GameObject = 206, //  TAG,
        Tag_DynamicObject = 207, //  TAG,
        Tag_Corpse = 208, //  TAG,
        Tag_AreaTrigger = 209, //  TAG,
        Tag_SceneObject = 210, //  TAG,
        Tag_Conversation = 211, //  TAG,
        Tag_AIGroup = 212, //  TAG,
        Tag_Scenario = 213, //  TAG,
        Tag_LootObject = 214, //  TAG,
        Tag_ActivePlayer = 215, //  TAG,
        Tag_ActiveClient_S = 216, //  TAG,
        Tag_ActiveObject_C = 217, //  TAG,
        Tag_VisibleObject_C = 218, //  TAG,
        Tag_UnitVehicle = 219, //  TAG,
        Tag_HousingRoom = 220, //  TAG,
        Tag_MeshObject = 221, //  TAG,
        Tag_HouseExteriorPiece = 224, //  TAG,
        Tag_HouseExteriorRoot = 225, //  TAG,
        Tag_HousingDecorProxyGameObject = 226, //  TAG,
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

        public UpdateableFragments Updateable = new();

        byte Count;
        public bool IdsChanged;
        public byte UpdateableCount;
        public byte ContentsChangedMask;

        public void Add(EntityFragment fragment, bool update, dynamic data = null)
        {
            Cypher.Assert(Count < Ids.Length);

            (int, bool) insertSorted(ref EntityFragment[] arr, ref byte count, EntityFragment f)
            {
                var whereIndex = Array.IndexOf(arr, f);
                if (whereIndex != -1)
                    return (whereIndex, false);

                whereIndex = Array.IndexOf(arr, EntityFragment.End);

                arr.SetValue(f, whereIndex);
                Array.Sort(arr);
                ++count;
                return (whereIndex, true);
            }

            if (!insertSorted(ref Ids, ref Count, fragment).Item2)
                return;

            if (IsUpdateableFragment(fragment))
            {
                Cypher.Assert(UpdateableCount < Updateable.Ids.Length);

                var index = insertSorted(ref Updateable.Ids, ref UpdateableCount, fragment).Item1;
                byte maskLowPart = (byte)(ContentsChangedMask & ((1 << index) - 1));
                byte maskHighPart = (byte)((ContentsChangedMask & ~((1 << index) - 1)) << (1 + (IsIndirectFragment(fragment) ? 1 : 0)));
                ContentsChangedMask = (byte)(maskLowPart | maskHighPart);
                for (byte i = 0, maskIndex = 0; i < UpdateableCount; ++i)
                {
                    Updateable.Masks[i] = (byte)(1 << maskIndex++);
                    if (IsIndirectFragment(Updateable.Ids[i]))
                    {
                        ContentsChangedMask |= Updateable.Masks[i]; // set the first bit to true to activate fragment
                        ++maskIndex;
                        Updateable.Masks[i] <<= 1;
                    }
                }

                Updateable.Data[index] = data;
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
                    Array.Sort(arr);
                    --count;
                    return (where, true);
                }
                return (where, false);
            }

            if (!removeSorted(ref Ids, ref Count, fragment).Item2)
                return;

            if (IsUpdateableFragment(fragment))
            {
                var (index, removed) = removeSorted(ref Updateable.Ids, ref UpdateableCount, fragment);
                if (removed)
                {
                    byte maskLowPart = (byte)(ContentsChangedMask & ((1 << index) - 1));
                    byte maskHighPart = (byte)((ContentsChangedMask & ~((1 << index) - 1)) >> (1 + (IsIndirectFragment(fragment) ? 1 : 0)));
                    ContentsChangedMask = (byte)(maskLowPart | maskHighPart);
                    for (byte i = 0, maskIndex = 0; i < UpdateableCount; ++i)
                    {
                        Updateable.Masks[i] = (byte)(1 << maskIndex++);
                        if (IsIndirectFragment(Updateable.Ids[i]))
                        {
                            ++maskIndex;
                            Updateable.Masks[i] <<= 1;
                        }
                    }

                    Updateable.Data[index] = null;
                }
            }

            IdsChanged = true;
        }

        public static bool IsUpdateableFragment(EntityFragment frag)
        {
            return frag == EntityFragment.CGObject
                || frag == EntityFragment.FVendor_C
                || frag == EntityFragment.FMeshObjectData_C
                || frag == EntityFragment.FHousingDecor_C
                || frag == EntityFragment.FHousingRoom_C
                || frag == EntityFragment.FHousingRoomComponentMesh_C
                || frag == EntityFragment.FHousingPlayerHouse_C
                || frag == EntityFragment.FJamHousingCornerstone_C
                || frag == EntityFragment.FNeighborhoodMirrorData_C
                || frag == EntityFragment.FMirroredPositionData_C
                || frag == EntityFragment.PlayerHouseInfoComponent_C
                || frag == EntityFragment.FHousingStorage_C
                || frag == EntityFragment.FHousingFixture_C
                || frag == EntityFragment.PlayerInitiativeComponent_C;
        }

        public static bool IsIndirectFragment(EntityFragment frag)
        {
            return frag == EntityFragment.CGObject
                || frag == EntityFragment.CActor
                || frag == EntityFragment.PlayerHouseInfoComponent_C
                || frag == EntityFragment.PlayerInitiativeComponent_C;
        }

        public Span<EntityFragment> GetIds() { return Ids[..Count]; }

        public byte GetUpdateMaskFor(EntityFragment fragment)
        {
            if (fragment == EntityFragment.CGObject)   // common case optimization, make use of the fact that fragment arrays are sorted
                return EntityDefinitionsConst.CGObjectChangedMask;

            for (byte i = 1; i < UpdateableCount; ++i)
                if (Updateable.Ids[i] == fragment)
                    return Updateable.Masks[i];

            return 0;
        }

        public struct UpdateableFragments()
        {
            static int N = 4;

            public EntityFragment[] Ids = [EntityFragment.End, EntityFragment.End, EntityFragment.End, EntityFragment.End];
            public byte[] Masks = new byte[N];
            public dynamic[] Data = new dynamic[N];
        }
    }

    public delegate void EntityFragmentSerializeFn(dynamic rawFragmentData, UpdateFieldFlag flags, WorldPacket data, Player target, BaseEntity baseEntity);
    public delegate bool EntityFragmentIsChangedFn(dynamic rawFragmentData);
    public delegate void EntityFragmentClearChangedFn(dynamic rawFragmentData);

    public struct EntityFragmentInfo
    {
        static EntityFragmentInfo()
        {
            Register<VendorData, Creature>(EntityFragment.FVendor_C);
            Register<MeshObjectData, WorldObject>(EntityFragment.FMeshObjectData_C);
            Register<HousingDecorData, WorldObject>(EntityFragment.FHousingDecor_C);
            Register<HousingRoomData, BaseEntity>(EntityFragment.FHousingRoom_C);
            Register<HousingRoomComponentMeshData, WorldObject>(EntityFragment.FHousingRoomComponentMesh_C);
            Register<HousingPlayerHouseData, BaseEntity>(EntityFragment.FHousingPlayerHouse_C);
            Register<HousingCornerstoneData, GameObject>(EntityFragment.FJamHousingCornerstone_C);
            Register<NeighborhoodMirrorData, BaseEntity>(EntityFragment.FNeighborhoodMirrorData_C);
            Register<MirroredPositionData, BaseEntity>(EntityFragment.FMirroredPositionData_C);
            Register<PlayerHouseInfoComponentData, Player>(EntityFragment.PlayerHouseInfoComponent_C);
            Register<HousingStorageData, BaseEntity>(EntityFragment.FHousingStorage_C);
            Register<HousingFixtureData, WorldObject>(EntityFragment.FHousingFixture_C);
            Register<PlayerInitiativeComponentData, Player>(EntityFragment.PlayerInitiativeComponent_C);
        }

        static int N = (int)EntityFragment.End + 1;

        public static EntityFragmentSerializeFn[] SerializeCreate = new EntityFragmentSerializeFn[N];
        public static EntityFragmentSerializeFn[] SerializeUpdate = new EntityFragmentSerializeFn[N];
        public static EntityFragmentIsChangedFn[] IsChanged = new EntityFragmentIsChangedFn[N];
        public static EntityFragmentClearChangedFn[] ClearChanged = new EntityFragmentClearChangedFn[N];

        public static void Register<FragmentData, OwnerObject>(EntityFragment fragment) where FragmentData : IsUpdateFieldStructure<OwnerObject> where OwnerObject : BaseEntity
        {
            int index = (int)fragment;
            SerializeCreate[index] = BuildCreate<FragmentData, OwnerObject>;
            SerializeUpdate[index] = BuildUpdate<OwnerObject>;
            IsChanged[index] = BuildIsChanged<OwnerObject>;
            ClearChanged[index] = BuildClearChanged<OwnerObject>;
        }

        public static void Register(EntityFragment fragment, EntityFragmentSerializeFn serializeCreate, EntityFragmentSerializeFn serializeUpdate, EntityFragmentIsChangedFn isChanged, EntityFragmentClearChangedFn clearChanged)
        {
            int index = (int)fragment;
            SerializeCreate[index] = serializeCreate;
            SerializeUpdate[index] = serializeUpdate;
            IsChanged[index] = isChanged;
            ClearChanged[index] = clearChanged;
        }

        static void BuildCreate<FragmentData, OwnerObject>(dynamic rawFragmentData, UpdateFieldFlag flags, WorldPacket data, Player target, BaseEntity baseEntity) where FragmentData : IsUpdateFieldStructure<OwnerObject> where OwnerObject : BaseEntity
        {
            ((FragmentData)rawFragmentData).WriteCreate(flags, data, target, (OwnerObject)baseEntity);
        }

        static void BuildUpdate<OwnerObject>(dynamic rawFragmentData, UpdateFieldFlag flags, WorldPacket data, Player target, BaseEntity baseEntity) where OwnerObject : BaseEntity
        {
            ((IsUpdateFieldStructure<OwnerObject>)rawFragmentData).WriteUpdate(flags, data, target, (OwnerObject)baseEntity);
        }

        static bool BuildIsChanged<OwnerObject>(dynamic rawFragmentData) where OwnerObject : BaseEntity
        {
            return ((IHasChangesMask)rawFragmentData).GetChangesMask().IsAnySet();
        }

        static void BuildClearChanged<OwnerObject>(dynamic rawFragmentData) where OwnerObject : BaseEntity
        {
            ((IHasChangesMask)rawFragmentData).ClearChangesMask();
        }
    }
}
