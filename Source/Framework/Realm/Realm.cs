// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Realm;
using System;
using System.Collections.Generic;
using System.Net;

public class Realm : IEquatable<Realm>
{
    public void SetName(string name)
    {
        Name = name;
        NormalizedName = name;
        NormalizedName = NormalizedName.Replace(" ", "");
    }

    public IPAddress GetAddressForClient(IPAddress clientAddr)
    {
        // Attempt to send best address for client
        if (IPAddress.IsLoopback(clientAddr))
            return Addresses[1];

        return Addresses[0];
    }

    public uint GetConfigId()
    {
        return ConfigIdByType[Type];
    }

    uint[] ConfigIdByType =
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14
    };

    public override bool Equals(object obj)
    {
        return obj != null && obj is Realm && Equals((Realm)obj);
    }

    public bool Equals(Realm other)
    {
        return other.Port == Port
            && other.Name == Name
            && other.Type == Type
            && other.Flags == Flags
            && other.Timezone == Timezone
            && other.AllowedSecurityLevel == AllowedSecurityLevel
            && other.PopulationLevel == PopulationLevel;
    }

    public override int GetHashCode()
    {
        return new { Port, Name, Type, Flags, Timezone, AllowedSecurityLevel, PopulationLevel }.GetHashCode();
    }

    public RealmId Id;
    public uint Build;
    public List<IPAddress> Addresses = new();
    public ushort Port;
    public string Name;
    public string NormalizedName;
    public byte Type;
    public RealmFlags Flags;
    public byte Timezone;
    public AccountTypes AllowedSecurityLevel;
    public float PopulationLevel;
}

