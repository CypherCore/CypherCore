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
using Game;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class ObjectAccessor : Singleton<ObjectAccessor>
{
    object _lockObject = new object();

    Dictionary<ObjectGuid, Player> _players = new Dictionary<ObjectGuid, Player>();
    Dictionary<ObjectGuid, Transport> _transports = new Dictionary<ObjectGuid, Transport>();

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

    static Transport GetTransportOnMap(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetTransport(guid);
    }

    Transport GetTransport(ObjectGuid guid)
    {
        return _transports.LookupByKey(guid);
    }

    static DynamicObject GetDynamicObject(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetDynamicObject(guid);
    }

    static AreaTrigger GetAreaTrigger(WorldObject u, ObjectGuid guid)
    {
        return u.GetMap().GetAreaTrigger(guid);
    }

    static Conversation GetConversation(WorldObject u, ObjectGuid guid)
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

    public Transport FindTransport(ObjectGuid guid)
    {
        lock (_lockObject)
            return _transports.LookupByKey(guid);
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
    public void AddObject(Transport obj)
    {
        lock (_lockObject)
            _transports[obj.GetGUID()] = obj;
    }

    public void RemoveObject(Player obj)
    {
        lock (_lockObject)
        {
            PlayerNameMapHolder.Remove(obj);
            _players.Remove(obj.GetGUID());
        }
    }
    public void RemoveObject(Transport obj)
    {
        lock (_lockObject)
            _transports.Remove(obj.GetGUID());
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

    static Dictionary<string, Player> _playerNameMap = new Dictionary<string, Player>();
}
