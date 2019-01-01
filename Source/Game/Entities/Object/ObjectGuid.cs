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
using System;

namespace Game.Entities
{
    public struct ObjectGuid : IEquatable<ObjectGuid>
    {
        public static ObjectGuid Empty = new ObjectGuid();
        public static ObjectGuid TradeItem = Create(HighGuid.Uniq, 10ul);

        public ObjectGuid(ulong high, ulong low)
        {
            _low = low;
            _high = high;
        }

        public static ObjectGuid Create(HighGuid type, ulong counter)
        {
            switch (type)
            {
                case HighGuid.Uniq:
                case HighGuid.Party:
                case HighGuid.WowAccount:
                case HighGuid.BNetAccount:
                case HighGuid.GMTask:
                case HighGuid.RaidGroup:
                case HighGuid.Spell:
                case HighGuid.Mail:
                case HighGuid.UserRouter:
                case HighGuid.PVPQueueGroup:
                case HighGuid.UserClient:
                case HighGuid.UniqUserClient:
                case HighGuid.BattlePet:
                case HighGuid.CommerceObj:
                case HighGuid.ClientSession:
                    return GlobalCreate(type, counter);
                case HighGuid.Player:
                case HighGuid.Item:   // This is not exactly correct, there are 2 more unknown parts in highguid: (high >> 10 & 0xFF), (high >> 18 & 0xFFFFFF)
                case HighGuid.Guild:
                case HighGuid.Transport:
                    return RealmSpecificCreate(type, counter);
                default:
                    Log.outError(LogFilter.Server, "This guid type cannot be constructed using Create(HighGuid: {0} ulong counter).", type);
                    break;
            }
            return ObjectGuid.Empty;
        }
        public static ObjectGuid Create(HighGuid type, uint mapId, uint entry, ulong counter)
        {
            if (type == HighGuid.Transport)
                return ObjectGuid.Empty;

            return MapSpecificCreate(type, 0, (ushort)mapId, 0, entry, counter);
        }

        public static ObjectGuid Create(HighGuid type, SpellCastSource subType, uint mapId, uint entry, ulong counter) { return MapSpecificCreate(type, (byte)subType, (ushort)mapId, 0, entry, counter); }

        static ObjectGuid GlobalCreate(HighGuid type, ulong counter)
        {
            return new ObjectGuid(((ulong)type << 58), counter);
        }
        static ObjectGuid RealmSpecificCreate(HighGuid type, ulong counter)
        {
            return new ObjectGuid(((ulong)type << 58 | (ulong)Global.WorldMgr.GetRealm().Id.Realm << 42), counter);
        }
        static ObjectGuid MapSpecificCreate(HighGuid type, byte subType, ushort mapId, uint serverId, uint entry, ulong counter)
        {
            return new ObjectGuid((((ulong)type << 58) | ((ulong)(Global.WorldMgr.GetRealm().Id.Realm & 0x1FFF) << 42) | ((ulong)(mapId & 0x1FFF) << 29) | ((ulong)(entry & 0x7FFFFF) << 6) | ((ulong)subType & 0x3F)),
                (((ulong)(serverId & 0xFFFFFF) << 40) | (counter & 0xFFFFFFFFFF)));
        }

        public byte[] GetRawValue()
        {
            byte[] temp = new byte[16];
            var hiBytes = BitConverter.GetBytes(_high);
            var lowBytes = BitConverter.GetBytes(_low);
            for (var i = 0; i < temp.Length / 2; ++i)
            {
                temp[i] = hiBytes[i];
                temp[8 + i] = lowBytes[i];
            }

            return temp;
        }
        public void SetRawValue(byte[] bytes)
        {
            _high = BitConverter.ToUInt64(bytes, 0);
            _low = BitConverter.ToUInt64(bytes, 8);
        }

        public void SetRawValue(ulong high, ulong low) { _high = high; _low = low; }
        public void Clear() { _high = 0; _low = 0; }
        public ulong GetHighValue()
        {
            return _high;
        }
        public ulong GetLowValue()
        {
            return _low;
        }

        public HighGuid GetHigh() { return (HighGuid)(_high >> 58); }
        byte GetSubType() { return (byte)(_high & 0x3F); }
        uint GetRealmId() { return (uint)((_high >> 42) & 0x1FFF); }
        uint GetServerId() { return (uint)((_low >> 40) & 0x1FFF); }
        uint GetMapId() { return (uint)((_high >> 29) & 0x1FFF); }
        public uint GetEntry() { return (uint)((_high >> 6) & 0x7FFFFF); }
        public ulong GetCounter() { return _low & 0xFFFFFFFFFF; }

        public bool IsEmpty() { return _low == 0 && _high == 0; }
        public bool IsCreature() { return GetHigh() == HighGuid.Creature; }
        public bool IsPet() { return GetHigh() == HighGuid.Pet; }
        public bool IsVehicle() { return GetHigh() == HighGuid.Vehicle; }
        public bool IsCreatureOrPet() { return IsCreature() || IsPet(); }
        public bool IsCreatureOrVehicle() { return IsCreature() || IsVehicle(); }
        public bool IsAnyTypeCreature() { return IsCreature() || IsPet() || IsVehicle(); }
        public bool IsPlayer() { return !IsEmpty() && GetHigh() == HighGuid.Player; }
        public bool IsUnit() { return IsAnyTypeCreature() || IsPlayer(); }
        public bool IsItem() { return GetHigh() == HighGuid.Item; }
        public bool IsGameObject() { return GetHigh() == HighGuid.GameObject; }
        public bool IsDynamicObject() { return GetHigh() == HighGuid.DynamicObject; }
        public bool IsCorpse() { return GetHigh() == HighGuid.Corpse; }
        public bool IsAreaTrigger() { return GetHigh() == HighGuid.AreaTrigger; }
        public bool IsMOTransport() { return GetHigh() == HighGuid.Transport; }
        public bool IsAnyTypeGameObject() { return IsGameObject() || IsMOTransport(); }
        public bool IsParty() { return GetHigh() == HighGuid.Party; }
        public bool IsGuild() { return GetHigh() == HighGuid.Guild; }
        bool IsSceneObject() { return GetHigh() == HighGuid.SceneObject; }
        bool IsConversation() { return GetHigh() == HighGuid.Conversation; }
        bool IsCast() { return GetHigh() == HighGuid.Cast; }

        public TypeId GetTypeId() { return GetTypeId(GetHigh()); }
        bool HasEntry() { return HasEntry(GetHigh()); }

        public static bool operator <(ObjectGuid left, ObjectGuid right)
        {
            if (left._high < right._high)
                return true;
            else if (left._high > right._high)
                return false;

            return left._low < right._low;
        }
        public static bool operator >(ObjectGuid left, ObjectGuid right)
        {
            if (left._high > right._high)
                return true;
            else if (left._high < right._high)
                return false;

            return left._low > right._low;
        }

        public override string ToString()
        {
            string str = $"GUID Full: 0x{_high + _low}, Type: {GetHigh()}";
            if (HasEntry())
                str += (IsPet() ? " Pet number: " : " Entry: ") + GetEntry() + " ";

            str += " Low: " + GetCounter();
            return str;
        }

        public static bool operator ==(ObjectGuid first, ObjectGuid other)
        {
            return first.Equals(other);
        }

        public static bool operator !=(ObjectGuid first, ObjectGuid other)
        {
            return !(first == other);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ObjectGuid && Equals((ObjectGuid)obj);
        }

        public bool Equals(ObjectGuid other)
        {
            return other._high == _high && other._low == _low;
        }

        public override int GetHashCode()
        {
            return new { _high, _low }.GetHashCode();
        }

        //Static Methods 
        static TypeId GetTypeId(HighGuid high)
        {
            switch (high)
            {
                case HighGuid.Item:
                    return TypeId.Item;
                case HighGuid.Creature:
                case HighGuid.Pet:
                case HighGuid.Vehicle:
                    return TypeId.Unit;
                case HighGuid.Player:
                    return TypeId.Player;
                case HighGuid.GameObject:
                case HighGuid.Transport:
                    return TypeId.GameObject;
                case HighGuid.DynamicObject:
                    return TypeId.DynamicObject;
                case HighGuid.Corpse:
                    return TypeId.Corpse;
                case HighGuid.AreaTrigger:
                    return TypeId.AreaTrigger;
                case HighGuid.SceneObject:
                    return TypeId.SceneObject;
                case HighGuid.Conversation:
                    return TypeId.Conversation;
                default:
                    return TypeId.Object;
            }
        }
        static bool HasEntry(HighGuid high)
        {
            switch (high)
            {
                case HighGuid.GameObject:
                case HighGuid.Creature:
                case HighGuid.Pet:
                case HighGuid.Vehicle:
                default:
                    return true;
            }
        }
        public static bool IsMapSpecific(HighGuid high)
        {
            switch (high)
            {
                case HighGuid.Conversation:
                case HighGuid.Creature:
                case HighGuid.Vehicle:
                case HighGuid.Pet:
                case HighGuid.GameObject:
                case HighGuid.DynamicObject:
                case HighGuid.AreaTrigger:
                case HighGuid.Corpse:
                case HighGuid.LootObject:
                case HighGuid.SceneObject:
                case HighGuid.Scenario:
                case HighGuid.AIGroup:
                case HighGuid.DynamicDoor:
                case HighGuid.Vignette:
                case HighGuid.CallForHelp:
                case HighGuid.AIResource:
                case HighGuid.AILock:
                case HighGuid.AILockTicket:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsRealmSpecific(HighGuid high)
        {
            switch (high)
            {
                case HighGuid.Player:
                case HighGuid.Item:
                case HighGuid.Transport:
                case HighGuid.Guild:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsGlobal(HighGuid high)
        {
            switch (high)
            {
                case HighGuid.Uniq:
                case HighGuid.Party:
                case HighGuid.WowAccount:
                case HighGuid.BNetAccount:
                case HighGuid.GMTask:
                case HighGuid.RaidGroup:
                case HighGuid.Spell:
                case HighGuid.Mail:
                case HighGuid.UserRouter:
                case HighGuid.PVPQueueGroup:
                case HighGuid.UserClient:
                case HighGuid.UniqUserClient:
                case HighGuid.BattlePet:
                    return true;
                default:
                    return false;
            }
        }

        ulong _low;
        ulong _high;
    }

    public class ObjectGuidGenerator
    {
        public ObjectGuidGenerator(HighGuid highGuid, ulong start = 1)
        {
            _highGuid = highGuid;
            _nextGuid = start;
        }

        public void Set(ulong val) { _nextGuid = val; }

        public ulong Generate()
        {
            if (_nextGuid >= ulong.MaxValue - 1)
                HandleCounterOverflow();
            return _nextGuid++;
        }

        public ulong GetNextAfterMaxUsed() { return _nextGuid; }

        void HandleCounterOverflow()
        {
            Log.outFatal(LogFilter.Server, "{0} guid overflow!! Can't continue, shutting down server. ", _highGuid);
            Global.WorldMgr.StopNow();
        }

        ulong _nextGuid;
        HighGuid _highGuid;
    }
}
