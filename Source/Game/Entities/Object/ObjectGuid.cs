// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public struct ObjectGuid : IEquatable<ObjectGuid>
    {
        public static ObjectGuid Empty = new();
        public static ObjectGuid FromStringFailed = Create(HighGuid.Uniq, 4);
        public static ObjectGuid TradeItem = Create(HighGuid.Uniq, 10);

        ulong _low;
        ulong _high;

        public ObjectGuid(ulong high, ulong low)
        {
            _low = low;
            _high = high;
        }

        public static ObjectGuid Create(HighGuid type, ulong dbId)
        {
            switch (type)
            {
                case HighGuid.Null:
                    return ObjectGuidFactory.CreateNull();
                case HighGuid.Uniq:
                    return ObjectGuidFactory.CreateUniq(dbId);
                case HighGuid.Player:
                    return ObjectGuidFactory.CreatePlayer(0, dbId);
                case HighGuid.Item:
                    return ObjectGuidFactory.CreateItem(0, dbId);
                case HighGuid.StaticDoor:
                case HighGuid.Transport:
                    return ObjectGuidFactory.CreateTransport(type, dbId);
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
                case HighGuid.BattlePet:
                case HighGuid.CommerceObj:
                    return ObjectGuidFactory.CreateGlobal(type, 0, dbId);
                case HighGuid.Guild:
                    return ObjectGuidFactory.CreateGuild(type, 0, dbId);
                default:
                    return Empty;
            }
        }

        public static ObjectGuid Create(HighGuid type, ushort ownerType, ushort ownerId, uint counter)
        {
            if (type != HighGuid.ClientActor)
                return Empty;

            return ObjectGuidFactory.CreateClientActor(ownerType, ownerId, counter);
        }

        public static ObjectGuid Create(HighGuid type, bool builtIn, bool trade, ushort zoneId, byte factionGroupMask, ulong counter)
        {
            if (type != HighGuid.ChatChannel)
                return Empty;

            return ObjectGuidFactory.CreateChatChannel(0, builtIn, trade, zoneId, factionGroupMask, counter);
        }

        public static ObjectGuid Create(HighGuid type, ushort arg1, ulong counter)
        {
            if (type != HighGuid.MobileSession)
                return Empty;

            return ObjectGuidFactory.CreateMobileSession(0, arg1, counter);
        }

        public static ObjectGuid Create(HighGuid type, byte arg1, byte arg2, ulong counter)
        {
            if (type != HighGuid.WebObj)
                return Empty;

            return ObjectGuidFactory.CreateWebObj(0, arg1, arg2, counter);
        }

        public static ObjectGuid Create(HighGuid type, byte arg1, byte arg2, byte arg3, byte arg4, bool arg5, byte arg6, ulong counter)
        {
            if (type != HighGuid.LFGObject)
                return Empty;

            return ObjectGuidFactory.CreateLFGObject(arg1, arg2, arg3, arg4, arg5, arg6, counter);
        }

        public static ObjectGuid Create(HighGuid type, byte arg1, ulong counter)
        {
            if (type != HighGuid.LFGList)
                return Empty;

            return ObjectGuidFactory.CreateLFGList(arg1, counter);
        }

        public static ObjectGuid Create(HighGuid type, uint arg1, ulong counter)
        {
            switch (type)
            {
                case HighGuid.PetBattle:
                case HighGuid.UniqUserClient:
                case HighGuid.ClientSession:
                case HighGuid.ClientConnection:
                case HighGuid.LMMParty:
                    return ObjectGuidFactory.CreateClient(type, 0, arg1, counter);
                default:
                    return Empty;
            }
        }

        public static ObjectGuid Create(HighGuid type, byte clubType, uint clubFinderId, ulong counter)
        {
            if (type != HighGuid.ClubFinder)
                return Empty;

            return ObjectGuidFactory.CreateClubFinder(0, clubType, clubFinderId, counter);
        }

        public static ObjectGuid Create(HighGuid type, uint mapId, uint entry, ulong counter)
        {
            switch (type)
            {
                case HighGuid.WorldTransaction:
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
                    return ObjectGuidFactory.CreateWorldObject(type, 0, 0, (ushort)mapId, 0, entry, counter);
                case HighGuid.ToolsClient:
                    return ObjectGuidFactory.CreateToolsClient(mapId, entry, counter); 
                default:
                    return Empty;
            }
        }

        public static ObjectGuid Create(HighGuid type, SpellCastSource subType, uint mapId, uint entry, ulong counter)
        {
            switch (type)
            {
                case HighGuid.Cast:
                    return ObjectGuidFactory.CreateWorldObject(type, (byte)subType, 0, (ushort)mapId, 0, entry, counter);
                default:
                    return Empty;
            }
        }

        public static ObjectGuid Create(HighGuid type, uint arg1, ushort arg2, byte arg3, uint arg4)
        {
            if (type != HighGuid.WorldLayer)
                return Empty;
            
            return ObjectGuidFactory.CreateWorldLayer(arg1, arg2, arg3, arg4);
        }

        public static ObjectGuid Create(HighGuid type, uint arg2, byte arg3, byte arg4, ulong counter)
        {
            if (type != HighGuid.LMMLobby)
                return Empty;

            return ObjectGuidFactory.CreateLMMLobby(0, arg2, arg3, arg4, counter);
        }

        public byte[] GetRawValue()
        {
            byte[] temp = new byte[16];
            var hiBytes = BitConverter.GetBytes(_high);
            var lowBytes = BitConverter.GetBytes(_low);
            for (var i = 0; i < temp.Length / 2; ++i)
            {
                temp[i] = lowBytes[i];
                temp[8 + i] = hiBytes[i];
            }

            return temp;
        }
        public void SetRawValue(byte[] bytes)
        {
            _low = BitConverter.ToUInt64(bytes, 0);
            _high = BitConverter.ToUInt64(bytes, 8);
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
        public byte GetSubType() { return (byte)(_high & 0x3F); }
        public uint GetRealmId() { return (uint)((_high >> 42) & 0x1FFF); }
        public uint GetServerId() { return (uint)((_low >> 40) & 0x1FFF); }
        public uint GetMapId() { return (uint)((_high >> 29) & 0x1FFF); }
        public uint GetEntry() { return (uint)((_high >> 6) & 0x7FFFFF); }
        public ulong GetCounter()
        {
            if (GetHigh() == HighGuid.Transport)
                return (_high >> 38) & 0xFFFFF;
            else
                return _low & 0xFFFFFFFFFF;
        }
        public static ulong GetMaxCounter(HighGuid highGuid)
        {
            if (highGuid == HighGuid.Transport)
                return 0xFFFFF;
            else
                return 0xFFFFFFFFFF;
        }

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
        public bool IsSceneObject() { return GetHigh() == HighGuid.SceneObject; }
        public bool IsConversation() { return GetHigh() == HighGuid.Conversation; }
        public bool IsCast() { return GetHigh() == HighGuid.Cast; }

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

        public static ObjectGuid FromString(string guidString)
        {
            return ObjectGuidInfo.Parse(guidString);
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
                case HighGuid.ChatChannel:
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
    }

    public class ObjectGuidGenerator
    {
        ulong _nextGuid;
        HighGuid _highGuid;

        public ObjectGuidGenerator(HighGuid highGuid, ulong start = 1)
        {
            _highGuid = highGuid;
            _nextGuid = start;
        }

        public void Set(ulong val) { _nextGuid = val; }

        public ulong Generate()
        {
            if (_nextGuid >= ObjectGuid.GetMaxCounter(_highGuid) - 1)
                HandleCounterOverflow();

            if (_highGuid == HighGuid.Creature || _highGuid == HighGuid.Vehicle || _highGuid == HighGuid.GameObject || _highGuid == HighGuid.Transport)
                CheckGuidTrigger(_nextGuid);

            return _nextGuid++;
        }

        public ulong GetNextAfterMaxUsed() { return _nextGuid; }

        void HandleCounterOverflow()
        {
            Log.outFatal(LogFilter.Server, "{0} guid overflow!! Can't continue, shutting down server. ", _highGuid);
            Global.WorldMgr.StopNow();
        }

        void CheckGuidTrigger(ulong guidlow)
        {
            if (!Global.WorldMgr.IsGuidAlert() && guidlow > WorldConfig.GetUInt64Value(WorldCfg.RespawnGuidAlertLevel))
                Global.WorldMgr.TriggerGuidAlert();
            else if (!Global.WorldMgr.IsGuidWarning() && guidlow > WorldConfig.GetUInt64Value(WorldCfg.RespawnGuidWarnLevel))
                Global.WorldMgr.TriggerGuidWarning();
        }
    }

    class ObjectGuidFactory
    {
        public static ObjectGuid CreateNull()
        {
            return new ObjectGuid();
        }

        public static ObjectGuid CreateUniq(ulong id)
        {
            return new ObjectGuid((ulong)((ulong)HighGuid.Uniq << 58), id);
        }

        public static ObjectGuid CreatePlayer(uint realmId, ulong dbId)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.Player << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId)) << 42)), dbId);
        }

        public static ObjectGuid CreateItem(uint realmId, ulong dbId)
        {
            return new ObjectGuid((ulong)(((ulong)(HighGuid.Item) << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId)) << 42)), dbId);
        }

        public static ObjectGuid CreateWorldObject(HighGuid type, byte subType, uint realmId, ushort mapId, uint serverId, uint entry, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)type << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(mapId & 0x1FFF) << 29) | ((ulong)(entry & 0x7FFFFF) << 6) | ((ulong)(subType) & 0x3F)), (ulong)(((ulong)(serverId & 0xFFFFFF) << 40) | (counter & (ulong)0xFFFFFFFFFF)));
        }

        public static ObjectGuid CreateTransport(HighGuid type, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)type << 58) | ((ulong)counter << 38)), 0ul);
        }

        public static ObjectGuid CreateClientActor(ushort ownerType, ushort ownerId, uint counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.ClientActor << 58) | ((ulong)(ownerType & 0x1FFF) << 42) | ((ulong)(ownerId & 0xFFFFFF) << 26)), (ulong)counter);
        }

        public static ObjectGuid CreateChatChannel(uint realmId, bool builtIn, bool trade, ushort zoneId, byte factionGroupMask, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.ChatChannel << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(builtIn ? 1 : 0) << 25) | ((ulong)(trade ? 1 : 0) << 24) | ((ulong)(zoneId & 0x3FFF) << 10) | ((ulong)(factionGroupMask & 0x3F) << 4)), counter);
        }

        public static ObjectGuid CreateGlobal(HighGuid type, ulong dbIdHigh, ulong dbId)
        {
            return new ObjectGuid((ulong)(((ulong)type << 58) | ((ulong)(dbIdHigh & (ulong)0x3FFFFFFFFFFFFFF))), dbId);
        }

        public static ObjectGuid CreateGuild(HighGuid type, uint realmId, ulong dbId)
        {
            return new ObjectGuid((ulong)(((ulong)type << 58) | ((ulong)GetRealmIdForObjectGuid(realmId) << 42)), dbId);
        }

        public static ObjectGuid CreateMobileSession(uint realmId, ushort arg1, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.MobileSession << 58) | ((ulong)GetRealmIdForObjectGuid(realmId) << 42) | ((ulong)(arg1 & 0x1FF) << 33)), counter);
        }

        public static ObjectGuid CreateWebObj(uint realmId, byte arg1, byte arg2, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.WebObj << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(arg1 & 0x1F) << 37) | ((ulong)(arg2 & 0x3) << 35)), counter);
        }

        public static ObjectGuid CreateLFGObject(byte arg1, byte arg2, byte arg3, byte arg4, bool arg5, byte arg6, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.LFGObject << 58) | ((ulong)(arg1 & 0xF) << 54) | ((ulong)(arg2 & 0xF) << 50) | ((ulong)(arg3 & 0xF) << 46) | ((ulong)(arg4 & 0xFF) << 38) | ((ulong)(arg5 ? 1 : 0) << 37) | ((ulong)(arg6 & 0x3) << 35)), counter);
        }

        public static ObjectGuid CreateLFGList(byte arg1, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.LFGObject << 58) | ((ulong)(arg1 & 0xF) << 54)), counter);
        }

        public static ObjectGuid CreateClient(HighGuid type, uint realmId, uint arg1, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)type << 58) | ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) | ((ulong)(arg1 & 0xFFFFFFFF) << 10)), counter);
        }

        public static ObjectGuid CreateClubFinder(uint realmId, byte type, uint clubFinderId, ulong dbId)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.ClubFinder << 58) | (type == 1 ? ((ulong)(GetRealmIdForObjectGuid(realmId) & 0x1FFF) << 42) : 0ul) | ((ulong)(type & 0xFF) << 33) | ((ulong)(clubFinderId & 0xFFFFFFFF))), dbId);
        }

        public static ObjectGuid CreateToolsClient(uint mapId, uint serverId, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.ToolsClient << 58) | (ulong)mapId), (ulong)(((ulong)(serverId & 0xFFFFFF) << 40) | (counter & 0xFFFFFFFFFF)));
        }

        public static ObjectGuid CreateWorldLayer(uint arg1, ushort arg2, byte arg3, uint arg4)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.WorldLayer << 58) | ((ulong)(arg1 & 0xFFFFFFFF) << 10) | (ulong)(arg2 & 0x1FFu)), (ulong)(((ulong)(arg3 & 0xFF) << 24) | (ulong)(arg4 & 0x7FFFFF)));
        }

        public static ObjectGuid CreateLMMLobby(uint realmId, uint arg2, byte arg3, byte arg4, ulong counter)
        {
            return new ObjectGuid((ulong)(((ulong)HighGuid.LMMLobby << 58)
                | ((ulong)GetRealmIdForObjectGuid(realmId) << 42)
                | ((ulong)(arg2 & 0xFFFFFFFF) << 26)
                | ((ulong)(arg3 & 0xFF) << 18)
                | ((ulong)(arg4 & 0xFF) << 10)),
                counter);
        }

        static uint GetRealmIdForObjectGuid(uint realmId)
        {
            if (realmId != 0)
                return realmId;

            return Global.WorldMgr.GetRealmId().Index;
        }
    }

    class ObjectGuidInfo
    {
        static Dictionary<HighGuid, string> Names = new();
        static Dictionary<HighGuid, Func<HighGuid, ObjectGuid, string>> ClientFormatFunction = new();
        static Dictionary<HighGuid, Func<HighGuid, string, ObjectGuid>> ClientParseFunction = new();

        static ObjectGuidInfo()
        {
            SET_GUID_INFO(HighGuid.Null, FormatNull, ParseNull);
            SET_GUID_INFO(HighGuid.Uniq, FormatUniq, ParseUniq);
            SET_GUID_INFO(HighGuid.Player, FormatPlayer, ParsePlayer);
            SET_GUID_INFO(HighGuid.Item, FormatItem, ParseItem);
            SET_GUID_INFO(HighGuid.WorldTransaction, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.StaticDoor, FormatTransport, ParseTransport);
            SET_GUID_INFO(HighGuid.Transport, FormatTransport, ParseTransport);
            SET_GUID_INFO(HighGuid.Conversation, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.Creature, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.Vehicle, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.Pet, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.GameObject, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.DynamicObject, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.AreaTrigger, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.Corpse, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.LootObject, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.SceneObject, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.Scenario, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.AIGroup, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.DynamicDoor, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.ClientActor, FormatClientActor, ParseClientActor);
            SET_GUID_INFO(HighGuid.Vignette, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.CallForHelp, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.AIResource, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.AILock, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.AILockTicket, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.ChatChannel, FormatChatChannel, ParseChatChannel);
            SET_GUID_INFO(HighGuid.Party, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.Guild, FormatGuild, ParseGuild);
            SET_GUID_INFO(HighGuid.WowAccount, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.BNetAccount, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.GMTask, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.MobileSession, FormatMobileSession, ParseMobileSession);
            SET_GUID_INFO(HighGuid.RaidGroup, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.Spell, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.Mail, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.WebObj, FormatWebObj, ParseWebObj);
            SET_GUID_INFO(HighGuid.LFGObject, FormatLFGObject, ParseLFGObject);
            SET_GUID_INFO(HighGuid.LFGList, FormatLFGList, ParseLFGList);
            SET_GUID_INFO(HighGuid.UserRouter, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.PVPQueueGroup, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.UserClient, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.PetBattle, FormatClient, ParseClient);
            SET_GUID_INFO(HighGuid.UniqUserClient, FormatClient, ParseClient);
            SET_GUID_INFO(HighGuid.BattlePet, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.CommerceObj, FormatGlobal, ParseGlobal);
            SET_GUID_INFO(HighGuid.ClientSession, FormatClient, ParseClient);
            SET_GUID_INFO(HighGuid.Cast, FormatWorldObject, ParseWorldObject);
            SET_GUID_INFO(HighGuid.ClientConnection, FormatClient, ParseClient);
            SET_GUID_INFO(HighGuid.ClubFinder, FormatClubFinder, ParseClubFinder);
            SET_GUID_INFO(HighGuid.ToolsClient, FormatToolsClient, ParseToolsClient);
            SET_GUID_INFO(HighGuid.WorldLayer, FormatWorldLayer, ParseWorldLayer);
            SET_GUID_INFO(HighGuid.ArenaTeam, FormatGuild, ParseGuild);
            SET_GUID_INFO(HighGuid.LMMParty, FormatClient, ParseClient);
            SET_GUID_INFO(HighGuid.LMMLobby, FormatLMMLobby, ParseLMMLobby);
        }

        static void SET_GUID_INFO(HighGuid type, Func<HighGuid, ObjectGuid, string> format, Func<HighGuid, string, ObjectGuid> parse)
        {
            Names[type] = type.ToString();
            ClientFormatFunction[type] = format;
            ClientParseFunction[type] = parse;
        }

        public static string Format(ObjectGuid guid)
        {
            if (guid.GetHigh() >= HighGuid.Count)
                return "Uniq-WOWGUID_TO_STRING_FAILED";

            if (ClientFormatFunction[guid.GetHigh()] == null)
                return "Uniq-WOWGUID_TO_STRING_FAILED";

            return ClientFormatFunction[guid.GetHigh()](guid.GetHigh(), guid);
        }

        public static ObjectGuid Parse(string guidString)
        {
            int typeEnd = guidString.IndexOf('-');
            if (typeEnd == -1)
                return ObjectGuid.FromStringFailed;

            if (!Enum.TryParse<HighGuid>(guidString.Substring(0, typeEnd), out HighGuid type))
                return ObjectGuid.FromStringFailed;

            if (type >= HighGuid.Count)
                return ObjectGuid.FromStringFailed;

            return ClientParseFunction[type](type, guidString.Substring(typeEnd + 1));
        }

        static string FormatNull(HighGuid typeName, ObjectGuid guid)
        {
            return "0000000000000000";
        }

        static ObjectGuid ParseNull(HighGuid type, string guidString)
        {
            return ObjectGuid.Empty;
        }

        static string FormatUniq(HighGuid typeName, ObjectGuid guid)
        {
            string[] uniqNames =
            {
                null,
                "WOWGUID_UNIQUE_PROBED_DELETE",
                "WOWGUID_UNIQUE_JAM_TEMP",
                "WOWGUID_TO_STRING_FAILED",
                "WOWGUID_FROM_STRING_FAILED",
                "WOWGUID_UNIQUE_SERVER_SELF",
                "WOWGUID_UNIQUE_MAGIC_SELF",
                "WOWGUID_UNIQUE_MAGIC_PET",
                "WOWGUID_UNIQUE_INVALID_TRANSPORT",
                "WOWGUID_UNIQUE_AMMO_ID",
                "WOWGUID_SPELL_TARGET_TRADE_ITEM",
                "WOWGUID_SCRIPT_TARGET_INVALID",
                "WOWGUID_SCRIPT_TARGET_NONE",
                null,
                "WOWGUID_FAKE_MODERATOR",
                null,
                null,
                "WOWGUID_UNIQUE_ACCOUNT_OBJ_INITIALIZATION"
            };

            ulong id = guid.GetCounter();
            if ((int)id >= uniqNames.Length)
                id = 3;

            return $"{typeName}-{uniqNames[id]}";
        }

        static ObjectGuid ParseUniq(HighGuid type, string guidString)
        {
            string[] uniqNames =
            {
                null,
                "WOWGUID_UNIQUE_PROBED_DELETE",
                "WOWGUID_UNIQUE_JAM_TEMP",
                "WOWGUID_TO_STRING_FAILED",
                "WOWGUID_FROM_STRING_FAILED",
                "WOWGUID_UNIQUE_SERVER_SELF",
                "WOWGUID_UNIQUE_MAGIC_SELF",
                "WOWGUID_UNIQUE_MAGIC_PET",
                "WOWGUID_UNIQUE_INVALID_TRANSPORT",
                "WOWGUID_UNIQUE_AMMO_ID",
                "WOWGUID_SPELL_TARGET_TRADE_ITEM",
                "WOWGUID_SCRIPT_TARGET_INVALID",
                "WOWGUID_SCRIPT_TARGET_NONE",
                null,
                "WOWGUID_FAKE_MODERATOR",
                null,
                null,
                "WOWGUID_UNIQUE_ACCOUNT_OBJ_INITIALIZATION"
            };

            for (int id = 0; id < uniqNames.Length; ++id)
            {
                if (uniqNames[id] == null)
                    continue;

                if (guidString.Equals(uniqNames[id]))
                    return ObjectGuidFactory.CreateUniq((ulong)id);
            }

            return ObjectGuid.FromStringFailed;
        }

        static string FormatPlayer(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-0x{guid.GetLowValue():X16}";
        }

        static ObjectGuid ParsePlayer(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 2)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !ulong.TryParse(split[1], out ulong dbId))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreatePlayer(realmId, dbId);
        }

        static string FormatItem(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(uint)(guid.GetHighValue() >> 18) & 0xFFFFFF}-0x{guid.GetLowValue():X16}";
        }

        static ObjectGuid ParseItem(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 3)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !uint.TryParse(split[1], out uint arg1) || !ulong.TryParse(split[2], out ulong dbId))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateItem(realmId, dbId);
        }

        static string FormatWorldObject(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetSubType()}-{guid.GetRealmId()}-{guid.GetMapId()}-{(uint)(guid.GetLowValue() >> 40) & 0xFFFFFF}-{guid.GetEntry()}-0x{guid.GetCounter():X10}";
        }

        static ObjectGuid ParseWorldObject(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 6)
                return ObjectGuid.FromStringFailed;

            if (!byte.TryParse(split[0], out byte subType) || !uint.TryParse(split[1], out uint realmId) || !ushort.TryParse(split[2], out ushort mapId) ||
                !uint.TryParse(split[3], out uint serverId) || !uint.TryParse(split[4], out uint id) || !ulong.TryParse(split[5], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateWorldObject(type, subType, realmId, mapId, serverId, id, counter);
        }

        static string FormatTransport(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{(guid.GetHighValue() >> 38) & 0xFFFFF}-0x{guid.GetLowValue():X16}";
        }

        static ObjectGuid ParseTransport(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 2)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint id) || !ulong.TryParse(split[1], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateTransport(type, counter);
        }

        static string FormatClientActor(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 26) & 0xFFFFFF}-{guid.GetLowValue()}";
        }

        static ObjectGuid ParseClientActor(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 3)
                return ObjectGuid.FromStringFailed;

            if (!ushort.TryParse(split[0], out ushort ownerType) || !ushort.TryParse(split[1], out ushort ownerId) || !uint.TryParse(split[2], out uint counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateClientActor(ownerType, ownerId, counter);
        }

        static string FormatChatChannel(HighGuid typeName, ObjectGuid guid)
        {
            uint builtIn = (uint)(guid.GetHighValue() >> 25) & 0x1;
            uint trade = (uint)(guid.GetHighValue() >> 24) & 0x1;
            uint zoneId = (uint)(guid.GetHighValue() >> 10) & 0x3FFF;
            uint factionGroupMask = (uint)(guid.GetHighValue() >> 4) & 0x3F;
            return $"{typeName}-{guid.GetRealmId()}-{builtIn}-{trade}-{zoneId}-{factionGroupMask}-0x{guid.GetLowValue():X8}";
        }

        static ObjectGuid ParseChatChannel(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 6)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !uint.TryParse(split[1], out uint builtIn) || !uint.TryParse(split[2], out uint trade) ||
                !ushort.TryParse(split[3], out ushort zoneId) || !byte.TryParse(split[4], out byte factionGroupMask) || !ulong.TryParse(split[5], out ulong id))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateChatChannel(realmId, builtIn != 0, trade != 0, zoneId, factionGroupMask, id);
        }

        static string FormatGlobal(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetHighValue() & 0x3FFFFFFFFFFFFFF}-0x{guid.GetLowValue():X12}";
        }

        static ObjectGuid ParseGlobal(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 2)
                return ObjectGuid.FromStringFailed;

            if (!ulong.TryParse(split[0], out ulong dbIdHigh) || !ulong.TryParse(split[1], out ulong dbIdLow))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateGlobal(type, dbIdHigh, dbIdLow);
        }

        static string FormatGuild(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-0x{guid.GetLowValue():X12}";
        }

        static ObjectGuid ParseGuild(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 2)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !ulong.TryParse(split[1], out ulong dbId))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateGuild(type, realmId, dbId);
        }

        static string FormatMobileSession(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 33) & 0x1FF}-0x{guid.GetLowValue():X8}";
        }

        static ObjectGuid ParseMobileSession(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 3)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !ushort.TryParse(split[1], out ushort arg1) || !ulong.TryParse(split[2], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateMobileSession(realmId, arg1, counter);
        }

        static string FormatWebObj(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 37) & 0x1F}-{(guid.GetHighValue() >> 35) & 0x3}-0x{guid.GetLowValue():X12}";
        }

        static ObjectGuid ParseWebObj(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 4)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !byte.TryParse(split[1], out byte arg1) || !byte.TryParse(split[2], out byte arg2) || !ulong.TryParse(split[3], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateWebObj(realmId, arg1, arg2, counter);
        }

        static string FormatLFGObject(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{(guid.GetHighValue() >> 54) & 0xF}-{(guid.GetHighValue() >> 50) & 0xF}-{(guid.GetHighValue() >> 46) & 0xF}-" +
                $"{(guid.GetHighValue() >> 38) & 0xFF}-{(guid.GetHighValue() >> 37) & 0x1}-{(guid.GetHighValue() >> 35) & 0x3}-0x{guid.GetLowValue():X6}";
        }

        static ObjectGuid ParseLFGObject(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 7)
                return ObjectGuid.FromStringFailed;

            if (!byte.TryParse(split[0], out byte arg1) || !byte.TryParse(split[1], out byte arg2) || !byte.TryParse(split[2], out byte arg3) ||
                !byte.TryParse(split[3], out byte arg4) || !byte.TryParse(split[4], out byte arg5) || !byte.TryParse(split[5], out byte arg6) || !ulong.TryParse(split[6], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateLFGObject(arg1, arg2, arg3, arg4, arg5 != 0, arg6, counter);
        }

        static string FormatLFGList(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{(guid.GetHighValue() >> 54) & 0xF}-0x{guid.GetLowValue():X6}";
        }

        static ObjectGuid ParseLFGList(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 2)
                return ObjectGuid.FromStringFailed;

            if (!byte.TryParse(split[0], out byte arg1) || !ulong.TryParse(split[1], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateLFGList(arg1, counter);
        }

        static string FormatClient(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(guid.GetHighValue() >> 10) & 0xFFFFFFFF}-0x{guid.GetLowValue():X12}";
        }

        static ObjectGuid ParseClient(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 3)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !uint.TryParse(split[1], out uint arg1) || !ulong.TryParse(split[2], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateClient(type, realmId, arg1, counter);
        }

        static string FormatClubFinder(HighGuid typeName, ObjectGuid guid)
        {
            uint type = (uint)(guid.GetHighValue() >> 33) & 0xFF;
            uint clubFinderId = (uint)(guid.GetHighValue() & 0xFFFFFFFF);
            if (type == 1) // guild
                return $"{typeName}-{type}-{clubFinderId}-{guid.GetRealmId()}-{guid.GetLowValue()}";

            return $"{typeName}-{type}-{clubFinderId}-0x{guid.GetLowValue():X16}";
        }

        static ObjectGuid ParseClubFinder(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length < 1)
                return ObjectGuid.FromStringFailed;

            if (!byte.TryParse(split[0], out byte typeNum))
                return ObjectGuid.FromStringFailed;

            uint clubFinderId = 0;
            uint realmId = 0;
            ulong dbId = 0;

            switch (typeNum)
            {
                case 0: // club
                    if (split.Length < 3)
                        return ObjectGuid.FromStringFailed;
                    if (!uint.TryParse(split[0], out clubFinderId) || !ulong.TryParse(split[1], out dbId))
                        return ObjectGuid.FromStringFailed;
                    break;
                case 1: // guild
                    if (split.Length < 4)
                        return ObjectGuid.FromStringFailed;
                    if (!uint.TryParse(split[0], out clubFinderId) || !uint.TryParse(split[1], out realmId) || !ulong.TryParse(split[2], out dbId))
                        return ObjectGuid.FromStringFailed;
                    break;
                default:
                    return ObjectGuid.FromStringFailed;
            }

            return ObjectGuidFactory.CreateClubFinder(realmId, typeNum, clubFinderId, dbId);
        }

        static string FormatToolsClient(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetMapId()}-{(uint)(guid.GetLowValue() >> 40) & 0xFFFFFF}-{guid.GetCounter():X10}";
        }

        static ObjectGuid ParseToolsClient(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 3)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint mapId) || !uint.TryParse(split[1], out uint serverId) || !ulong.TryParse(split[2], out ulong counter))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateToolsClient(mapId, serverId, counter);
        }

        static string FormatWorldLayer(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{(uint)((guid.GetHighValue() >> 10) & 0xFFFFFFFF)}-{(uint)(guid.GetHighValue() & 0x1FF)}-{(uint)((guid.GetLowValue() >> 24) & 0xFF)}-{(uint)(guid.GetLowValue() & 0x7FFFFF)}";
        }

        static ObjectGuid ParseWorldLayer(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 4)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint arg1) || !ushort.TryParse(split[1], out ushort arg2) || !byte.TryParse(split[2], out byte arg3) || !uint.TryParse(split[0], out uint arg4))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateWorldLayer(arg1, arg2, arg3, arg4);
        }

        static string FormatLMMLobby(HighGuid typeName, ObjectGuid guid)
        {
            return $"{typeName}-{guid.GetRealmId()}-{(uint)(guid.GetHighValue() >> 26) & 0xFFFFFF}-{(uint)(guid.GetHighValue() >> 18) & 0xFF}-{(uint)(guid.GetHighValue() >> 10) & 0xFF}-{guid.GetLowValue():X}";
        }

        static ObjectGuid ParseLMMLobby(HighGuid type, string guidString)
        {
            string[] split = guidString.Split('-');
            if (split.Length != 5)
                return ObjectGuid.FromStringFailed;

            if (!uint.TryParse(split[0], out uint realmId) || !uint.TryParse(split[1], out uint arg2) || !byte.TryParse(split[2], out byte arg3) || !byte.TryParse(split[0], out byte arg4) || !ulong.TryParse(split[0], out ulong arg5))
                return ObjectGuid.FromStringFailed;

            return ObjectGuidFactory.CreateLMMLobby(realmId, arg2, arg3, arg4, arg5);
        }
    }

    public class Legacy
    {
        public enum LegacyTypeId
        {
            Object          = 0,
            Item            = 1,
            Container       = 2,
            Unit            = 3,
            Player          = 4,
            GameObject      = 5,
            DynamicObject   = 6,
            Corpse          = 7,
            AreaTrigger     = 8,
            SceneObject     = 9,
            Conversation    = 10,
            Max
        }

        public static uint ConvertLegacyTypeID(uint legacyTypeID) => (LegacyTypeId)legacyTypeID switch
        {
            LegacyTypeId.Object => (uint)TypeId.Object,
            LegacyTypeId.Item => (uint)TypeId.Item,
            LegacyTypeId.Container => (uint)TypeId.Container,
            LegacyTypeId.Unit => (uint)TypeId.Unit,
            LegacyTypeId.Player => (uint)TypeId.Player,
            LegacyTypeId.GameObject => (uint)TypeId.GameObject,
            LegacyTypeId.DynamicObject => (uint)TypeId.DynamicObject,
            LegacyTypeId.Corpse => (uint)TypeId.Corpse,
            LegacyTypeId.AreaTrigger => (uint)TypeId.AreaTrigger,
            LegacyTypeId.SceneObject => (uint)TypeId.SceneObject,
            LegacyTypeId.Conversation => (uint)TypeId.Conversation,
            _ => (uint)TypeId.Object
        };

        public static uint ConvertLegacyTypeMask(uint legacyTypeMask)
        {
            uint typeMask = 0;
            for (uint i = (uint)LegacyTypeId.Object; i < (uint)LegacyTypeId.Max; i = i + 1)
                if ((legacyTypeMask & (1 << (int)i)) != 0)
                    typeMask |= 1u << (int)ConvertLegacyTypeID(i);

            return typeMask;
        }
    }
}
