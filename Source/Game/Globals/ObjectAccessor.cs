// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;

public class ObjectAccessor : Singleton<ObjectAccessor>
{
    object _lockObject = new();

    Dictionary<ObjectGuid, Player> _players = new();

    ObjectAccessor() { }

    public WorldObject GetWorldObject(WorldObject p, ObjectGuid guid)
    {
        switch (guid.GetHigh())
        {
            case HighGuid.Player:
                return GetPlayer(p, guid);
            case HighGuid.Transport:
            case HighGuid.GameObject:
                return GetGameObject(p, guid);
            case HighGuid.Vehicle:
            case HighGuid.Creature:
                return GetCreature(p, guid);
            case HighGuid.Pet:
                return GetPet(p, guid);
            case HighGuid.DynamicObject:
                return GetDynamicObject(p, guid);
            case HighGuid.AreaTrigger:
                return GetAreaTrigger(p, guid);
            case HighGuid.Corpse:
                return GetCorpse(p, guid);
            case HighGuid.SceneObject:
                return GetSceneObject(p, guid);
            case HighGuid.Conversation:
                return GetConversation(p, guid);
            default:
                return null;
        }
    }

    public WorldObject GetObjectByTypeMask(WorldObject p, ObjectGuid guid, TypeMask typemask)
    {
        switch (guid.GetHigh())
        {
            case HighGuid.Item:
                if (typemask.HasAnyFlag(TypeMask.Item) && p.IsTypeId(TypeId.Player))
                    return ((Player)p).GetItemByGuid(guid);
                break;
            case HighGuid.Player:
                if (typemask.HasAnyFlag(TypeMask.Player))
                    return GetPlayer(p, guid);
                break;
            case HighGuid.Transport:
            case HighGuid.GameObject:
                if (typemask.HasAnyFlag(TypeMask.GameObject))
                    return GetGameObject(p, guid);
                break;
            case HighGuid.Creature:
            case HighGuid.Vehicle:
                if (typemask.HasAnyFlag(TypeMask.Unit))
                    return GetCreature(p, guid);
                break;
            case HighGuid.Pet:
                if (typemask.HasAnyFlag(TypeMask.Unit))
                    return GetPet(p, guid);
                break;
            case HighGuid.DynamicObject:
                if (typemask.HasAnyFlag(TypeMask.DynamicObject))
                    return GetDynamicObject(p, guid);
                break;
            case HighGuid.AreaTrigger:
                if (typemask.HasAnyFlag(TypeMask.AreaTrigger))
                    return GetAreaTrigger(p, guid);
                break;
            case HighGuid.SceneObject:
                if (typemask.HasAnyFlag(TypeMask.SceneObject))
                    return GetSceneObject(p, guid);
                break;
            case HighGuid.Conversation:
                if (typemask.HasAnyFlag(TypeMask.Conversation))
                    return GetConversation(p, guid);
                break;
            case HighGuid.Corpse:
                break;
        }

        return null;
    }

    public static Corpse GetCorpse(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetCorpse(guid);
    }

    public static GameObject GetGameObject(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetGameObject(guid);
    }

    public static Transport GetTransport(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetTransport(guid);
    }

    static DynamicObject GetDynamicObject(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetDynamicObject(guid);
    }

    static AreaTrigger GetAreaTrigger(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetAreaTrigger(guid);
    }

    static SceneObject GetSceneObject(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetSceneObject(guid);
    }
    
    public static Conversation GetConversation(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetConversation(guid);
    }

    public Unit GetUnit(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPlayer())
            return GetPlayer(u, guid);

        if (guid.IsPet())
            return GetPet(u, guid);

        return GetCreature(u, guid);
    }

    public static Creature GetCreature(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetCreature(guid);
    }

    public static Pet GetPet(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetPet(guid);
    }

    public Player GetPlayer(Map m, ObjectGuid guid)
    {
        Player player = _players.LookupByKey(guid);
        if (player)
            if (player.IsInWorld && player.GetMap() == m)
                return player;

        return null;
    }

    public Player GetPlayer(WorldObject u, ObjectGuid guid)
    {
        return GetPlayer(u.GetMap(), guid);
    }

    public static Creature GetCreatureOrPetOrVehicle(WorldObject u, ObjectGuid guid)
    {
        if (guid.IsPet())
            return GetPet(u, guid);

        if (guid.IsCreatureOrVehicle())
            return GetCreature(u, guid);

        return null;
    }

    // these functions return objects if found in whole world
    // ACCESS LIKE THAT IS NOT THREAD SAFE
    public Player FindPlayer(ObjectGuid guid)
    {
        Player player = FindConnectedPlayer(guid);
        return player && player.IsInWorld ? player : null;
    }
    public Player FindPlayerByName(string name)
    {
        Player player = PlayerNameMapHolder.Find(name);
        if (!player || !player.IsInWorld)
            return null;

        return player;
    }
    public Player FindPlayerByLowGUID(ulong lowguid)
    {
        ObjectGuid guid = ObjectGuid.Create(HighGuid.Player, lowguid);
        return FindPlayer(guid);
    }
    // this returns Player even if he is not in world, for example teleporting
    public Player FindConnectedPlayer(ObjectGuid guid)
    {
        lock (_lockObject)
            return _players.LookupByKey(guid);
    }
    public Player FindConnectedPlayerByName(string name)
    {
        return PlayerNameMapHolder.Find(name);
    }

    public void SaveAllPlayers()
    {
        lock (_lockObject)
        {
            foreach (var pl in GetPlayers())
                pl.SaveToDB();
        }
    }

    public ICollection<Player> GetPlayers()
    {
        lock (_lockObject)
            return _players.Values;
    }

    public void AddObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Insert(obj);
            _players[obj.GetGUID()] = obj;
        }
    }

    public void RemoveObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Remove(obj);
            _players.Remove(obj.GetGUID());
        }
    }
}

class PlayerNameMapHolder
{
    public static void Insert(Player p)
    {
        _playerNameMap[p.GetName()] = p;
    }

    public static void Remove(Player p)
    {
        _playerNameMap.Remove(p.GetName());
    }

    public static Player Find(string name)
    {
        if (!ObjectManager.NormalizePlayerName(ref name))
            return null;

        return _playerNameMap.LookupByKey(name);
    }

    static Dictionary<string, Player> _playerNameMap = new();
}
