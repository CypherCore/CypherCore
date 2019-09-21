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
using Framework.Cryptography;
using Framework.Dynamic;
using Framework.IO;
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace Game.Network.Packets
{
    class Ping : ClientPacket
    {
        public Ping(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Serial = _worldPacket.ReadUInt32();
            Latency = _worldPacket.ReadUInt32();
        }
        
        public uint Serial;
        public uint Latency;
    }

    class Pong : ServerPacket
    {
        public Pong(uint serial) : base(ServerOpcodes.Pong)
        {
            Serial = serial;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Serial);
        }

        uint Serial;
    }

    class AuthChallenge : ServerPacket
    {
        public AuthChallenge() : base(ServerOpcodes.AuthChallenge) { }

        public override void Write()
        {
            _worldPacket.WriteBytes(DosChallenge);
            _worldPacket.WriteBytes(Challenge);
            _worldPacket.WriteUInt8(DosZeroBits);
        }

        public byte[] Challenge = new byte[16];
        public byte[] DosChallenge = new byte[32]; // Encryption seeds
        public byte DosZeroBits;
    }

    class AuthSession : ClientPacket
    {
        public AuthSession(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            DosResponse = _worldPacket.ReadUInt64();
            RegionID = _worldPacket.ReadUInt32();
            BattlegroupID = _worldPacket.ReadUInt32();
            RealmID = _worldPacket.ReadUInt32();

            for (var i = 0; i < LocalChallenge.GetLimit(); ++i)
                LocalChallenge[i] = _worldPacket.ReadUInt8();

            Digest = _worldPacket.ReadBytes(24);

            UseIPv6 = _worldPacket.HasBit();
            uint realmJoinTicketSize = _worldPacket.ReadUInt32();
            if (realmJoinTicketSize != 0)
                RealmJoinTicket = _worldPacket.ReadString(realmJoinTicketSize);
        }

        public uint RegionID;
        public uint BattlegroupID;
        public uint RealmID;
        public Array<byte> LocalChallenge = new Array<byte>(16);
        public byte[] Digest = new byte[24];
        public ulong DosResponse;
        public string RealmJoinTicket;
        public bool UseIPv6;
    }

    class AuthResponse : ServerPacket
    {
        public AuthResponse() : base(ServerOpcodes.AuthResponse)
        {
            WaitInfo = new Optional<AuthWaitInfo>();
            SuccessInfo = new Optional<AuthSuccessInfo>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WriteBit(SuccessInfo.HasValue);
            _worldPacket.WriteBit(WaitInfo.HasValue);
            _worldPacket.FlushBits();

            if (SuccessInfo.HasValue)
            {
                _worldPacket.WriteUInt32(SuccessInfo.Value.VirtualRealmAddress);
                _worldPacket.WriteInt32(SuccessInfo.Value.VirtualRealms.Count);
                _worldPacket.WriteUInt32(SuccessInfo.Value.TimeRested);
                _worldPacket.WriteUInt8(SuccessInfo.Value.ActiveExpansionLevel);
                _worldPacket.WriteUInt8(SuccessInfo.Value.AccountExpansionLevel);
                _worldPacket.WriteUInt32(SuccessInfo.Value.TimeSecondsUntilPCKick);
                _worldPacket.WriteInt32(SuccessInfo.Value.AvailableClasses.Count);
                _worldPacket.WriteInt32(SuccessInfo.Value.Templates.Count);
                _worldPacket.WriteUInt32(SuccessInfo.Value.CurrencyID);
                _worldPacket.WriteUInt32(SuccessInfo.Value.Time);

                foreach (var klass in SuccessInfo.Value.AvailableClasses)
                {
                    _worldPacket.WriteUInt8(klass.Key); // the current class
                    _worldPacket.WriteUInt8(klass.Value); // the required Expansion
                }

                _worldPacket.WriteBit(SuccessInfo.Value.IsExpansionTrial);
                _worldPacket.WriteBit(SuccessInfo.Value.ForceCharacterTemplate);
                _worldPacket.WriteBit(SuccessInfo.Value.NumPlayersHorde.HasValue);
                _worldPacket.WriteBit(SuccessInfo.Value.NumPlayersAlliance.HasValue);
                _worldPacket.WriteBit(SuccessInfo.Value.ExpansionTrialExpiration.HasValue);
                _worldPacket.FlushBits();

                {
                    _worldPacket.WriteUInt32(SuccessInfo.Value.Billing.BillingPlan);
                    _worldPacket.WriteUInt32(SuccessInfo.Value.Billing.TimeRemain);
                    _worldPacket.WriteUInt32(SuccessInfo.Value.Billing.Unknown735);
                    // 3x same bit is not a mistake - preserves legacy client behavior of BillingPlanFlags::SESSION_IGR
                    _worldPacket.WriteBit(SuccessInfo.Value.Billing.InGameRoom); // inGameRoom check in function checking which lua event to fire when remaining time is near end - BILLING_NAG_DIALOG vs IGR_BILLING_NAG_DIALOG
                    _worldPacket.WriteBit(SuccessInfo.Value.Billing.InGameRoom); // inGameRoom lua return from Script_GetBillingPlan
                    _worldPacket.WriteBit(SuccessInfo.Value.Billing.InGameRoom); // not used anywhere in the client
                    _worldPacket.FlushBits();
                }

                if (SuccessInfo.Value.NumPlayersHorde.HasValue)
                    _worldPacket.WriteUInt16(SuccessInfo.Value.NumPlayersHorde.Value);

                if (SuccessInfo.Value.NumPlayersAlliance.HasValue)
                    _worldPacket.WriteUInt16(SuccessInfo.Value.NumPlayersAlliance.Value);

                if(SuccessInfo.Value.ExpansionTrialExpiration.HasValue)
                    _worldPacket.WriteInt32(SuccessInfo.Value.ExpansionTrialExpiration.Value);

                foreach (VirtualRealmInfo virtualRealm in SuccessInfo.Value.VirtualRealms)
                    virtualRealm.Write(_worldPacket);

                foreach (var templat in SuccessInfo.Value.Templates)
                {
                    _worldPacket.WriteUInt32(templat.TemplateSetId);
                    _worldPacket.WriteInt32(templat.Classes.Count);
                    foreach (var templateClass in templat.Classes)
                    {
                        _worldPacket.WriteUInt8(templateClass.ClassID);
                        _worldPacket.WriteUInt8((byte)templateClass.FactionGroup);
                    }

                    _worldPacket.WriteBits(templat.Name.GetByteCount(), 7);
                    _worldPacket.WriteBits(templat.Description.GetByteCount(), 10);
                    _worldPacket.FlushBits();

                    _worldPacket.WriteString(templat.Name);
                    _worldPacket.WriteString(templat.Description);
                }
            }

            if (WaitInfo.HasValue)
                WaitInfo.Value.Write(_worldPacket);            
        }

        public Optional<AuthSuccessInfo> SuccessInfo; // contains the packet data in case that it has account information (It is never set when WaitInfo is set), otherwise its contents are undefined.
        public Optional<AuthWaitInfo> WaitInfo; // contains the queue wait information in case the account is in the login queue.
        public BattlenetRpcErrorCode Result; // the result of the authentication process, possible values are @ref BattlenetRpcErrorCode

        public class AuthSuccessInfo
        {
            public byte AccountExpansionLevel; // the current expansion of this account, the possible values are in @ref Expansions
            public byte ActiveExpansionLevel; // the current server expansion, the possible values are in @ref Expansions
            public uint TimeRested; // affects the return value of the GetBillingTimeRested() client API call, it is the number of seconds you have left until the experience points and loot you receive from creatures and quests is reduced. It is only used in the Asia region in retail, it's not implemented in TC and will probably never be.

            public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region. @todo implement
            public uint TimeSecondsUntilPCKick; // @todo research
            public uint CurrencyID; // this is probably used for the ingame shop. @todo implement
            public uint Time;

            public BillingInfo Billing;

            public List<VirtualRealmInfo> VirtualRealms = new List<VirtualRealmInfo>();     // list of realms connected to this one (inclusive) @todo implement
            public List<CharacterTemplate> Templates = new List<CharacterTemplate>(); // list of pre-made character templates. @todo implement

            public Dictionary<byte, byte> AvailableClasses; // the minimum AccountExpansion required to select the classes

            public bool IsExpansionTrial;
            public bool ForceCharacterTemplate; // forces the client to always use a character template when creating a new character. @see Templates. @todo implement
            public Optional<ushort> NumPlayersHorde; // number of horde players in this realm. @todo implement
            public Optional<ushort> NumPlayersAlliance; // number of alliance players in this realm. @todo implement
            public Optional<int> ExpansionTrialExpiration; // expansion trial expiration unix timestamp

            public struct BillingInfo
            {
                public uint BillingPlan;
                public uint TimeRemain;
                public uint Unknown735;
                public bool InGameRoom;
            }
        }
    }

    class WaitQueueUpdate : ServerPacket
    {
        public WaitQueueUpdate() : base(ServerOpcodes.WaitQueueUpdate) { }

        public override void Write()
        {
            WaitInfo.Write(_worldPacket);
        }

        public AuthWaitInfo WaitInfo;
    }

    class WaitQueueFinish : ServerPacket
    {
        public WaitQueueFinish() : base(ServerOpcodes.WaitQueueFinish) { }

        public override void Write() { }
    }

    class ConnectTo : ServerPacket
    {
        public ConnectTo() : base(ServerOpcodes.ConnectTo)
        {
            Payload = new ConnectPayload();
        }

        public override void Write()
        {
            ByteBuffer whereBuffer = new ByteBuffer();
            whereBuffer.WriteUInt8((byte)Payload.Where.Type);

            switch (Payload.Where.Type)
            {
                case AddressType.IPv4:
                    whereBuffer.WriteBytes(Payload.Where.IPv4);
                    break;
                case AddressType.IPv6:
                    whereBuffer.WriteBytes(Payload.Where.IPv6);
                    break;
                case AddressType.NamedSocket:
                    whereBuffer.WriteString(Payload.Where.NameSocket);
                    break;
                default:
                    break;
            }

            Sha256 hash = new Sha256();
            hash.Process(whereBuffer.GetData(), (int)whereBuffer.GetSize());
            hash.Process((uint)Payload.Where.Type);
            hash.Finish(BitConverter.GetBytes(Payload.Port));

            Payload.Signature = RsaCrypt.RSA.SignHash(hash.Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Reverse().ToArray();

            _worldPacket.WriteBytes(Payload.Signature, (uint)Payload.Signature.Length);
            _worldPacket.WriteBytes(whereBuffer);
            _worldPacket.WriteUInt16(Payload.Port);
            _worldPacket.WriteUInt32((uint)Serial);
            _worldPacket.WriteUInt8(Con);
            _worldPacket.WriteUInt64(Key);
        }

        public ulong Key;
        public ConnectToSerial Serial;
        public ConnectPayload Payload;
        public byte Con;

        public class ConnectPayload
        {
            public SocketAddress Where;
            public ushort Port;
            public byte[] Signature = new byte[256];
        }

        public struct SocketAddress
        {
            public AddressType Type;

            public byte[] IPv4;
            public byte[] IPv6;
            public string NameSocket;

        }

        public enum AddressType
        {
            IPv4 = 1,
            IPv6 = 2,
            NamedSocket = 3 // not supported by windows client
        }
    }

    class AuthContinuedSession : ClientPacket
    {
        public AuthContinuedSession(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            DosResponse = _worldPacket.ReadUInt64();
            Key = _worldPacket.ReadUInt64();
            LocalChallenge = _worldPacket.ReadBytes(16);
            Digest = _worldPacket.ReadBytes(24);
        }

        public ulong DosResponse;
        public ulong Key;
        public byte[] LocalChallenge = new byte[16];
        public byte[] Digest = new byte[24];
    }

    class ResumeComms : ServerPacket
    {
        public ResumeComms(ConnectionType connection) : base(ServerOpcodes.ResumeComms, connection) { }

        public override void Write() { }
    }

    class ConnectToFailed : ClientPacket
    {
        public ConnectToFailed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Serial = (ConnectToSerial)_worldPacket.ReadUInt32();
            Con = _worldPacket.ReadUInt8();
        }

        public ConnectToSerial Serial;
        byte Con;
    }

    class EnableEncryption : ServerPacket
    {
        byte[] EncryptionKey;
        bool Enabled;

        static byte[] EnableEncryptionSeed = { 0x90, 0x9C, 0xD0, 0x50, 0x5A, 0x2C, 0x14, 0xDD, 0x5C, 0x2C, 0xC0, 0x64, 0x14, 0xF3, 0xFE, 0xC9 };

        public EnableEncryption(byte[] encryptionKey, bool enabled) : base(ServerOpcodes.EnableEncryption)
        {
            EncryptionKey = encryptionKey;
            Enabled = enabled;
        }

        public override void Write()
        {
            HmacSha256 hash = new HmacSha256(EncryptionKey);
            hash.Process(BitConverter.GetBytes(Enabled), 1);
            hash.Finish(EnableEncryptionSeed, 16);

            _worldPacket.WriteBytes(RsaCrypt.RSA.SignHash(hash.Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Reverse().ToArray());
            _worldPacket.WriteBit(Enabled);
            _worldPacket.FlushBits();
        }
    }

    //Structs
    public struct AuthWaitInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(WaitCount);
            data.WriteUInt32(WaitTime);
            data.WriteBit(HasFCM);
            data.FlushBits();
        }

        public uint WaitCount; // position of the account in the login queue
        public uint WaitTime; // Wait time in login queue in minutes, if sent queued and this value is 0 client displays "unknown time"
        public bool HasFCM; // true if the account has a forced character migration pending. @todo implement
    }

    struct VirtualRealmNameInfo
    {
        public VirtualRealmNameInfo(bool isHomeRealm, bool isInternalRealm, string realmNameActual, string realmNameNormalized)
        {
            IsLocal = isHomeRealm;
            IsInternalRealm = isInternalRealm;
            RealmNameActual = realmNameActual;
            RealmNameNormalized = realmNameNormalized;
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(IsLocal);
            data.WriteBit(IsInternalRealm);
            data.WriteBits(RealmNameActual.GetByteCount(), 8);
            data.WriteBits(RealmNameNormalized.GetByteCount(), 8);
            data.FlushBits();

            data.WriteString(RealmNameActual);
            data.WriteString(RealmNameNormalized);
        }

        public bool IsLocal;                    // true if the realm is the same as the account's home realm
        public bool IsInternalRealm;            // @todo research
        public string RealmNameActual;     // the name of the realm
        public string RealmNameNormalized; // the name of the realm without spaces
    }

    struct VirtualRealmInfo
    {
        public VirtualRealmInfo(uint realmAddress, bool isHomeRealm, bool isInternalRealm, string realmNameActual, string realmNameNormalized)
        {

            RealmAddress = realmAddress;
            RealmNameInfo = new VirtualRealmNameInfo(isHomeRealm, isInternalRealm, realmNameActual, realmNameNormalized);
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(RealmAddress);
            RealmNameInfo.Write(data);
        }

        public uint RealmAddress;             // the virtual address of this realm, constructed as RealmHandle::Region << 24 | RealmHandle::Battlegroup << 16 | RealmHandle::Index
        public VirtualRealmNameInfo RealmNameInfo;
    }
}
