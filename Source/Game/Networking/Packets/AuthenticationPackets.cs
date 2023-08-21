// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Cryptography;
using Framework.Cryptography.Ed25519;
using Framework.IO;
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Game.Networking.Packets
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
        public Array<byte> LocalChallenge = new(16);
        public byte[] Digest = new byte[24];
        public ulong DosResponse;
        public string RealmJoinTicket;
        public bool UseIPv6;
    }

    class AuthResponse : ServerPacket
    {
        public AuthResponse() : base(ServerOpcodes.AuthResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WriteBit(SuccessInfo != null);
            _worldPacket.WriteBit(WaitInfo.HasValue);
            _worldPacket.FlushBits();

            if (SuccessInfo != null)
            {
                _worldPacket.WriteUInt32(SuccessInfo.VirtualRealmAddress);
                _worldPacket.WriteInt32(SuccessInfo.VirtualRealms.Count);
                _worldPacket.WriteUInt32(SuccessInfo.TimeRested);
                _worldPacket.WriteUInt8(SuccessInfo.ActiveExpansionLevel);
                _worldPacket.WriteUInt8(SuccessInfo.AccountExpansionLevel);
                _worldPacket.WriteUInt32(SuccessInfo.TimeSecondsUntilPCKick);
                _worldPacket.WriteInt32(SuccessInfo.AvailableClasses.Count);
                _worldPacket.WriteInt32(SuccessInfo.Templates.Count);
                _worldPacket.WriteUInt32(SuccessInfo.CurrencyID);
                _worldPacket.WriteInt64(SuccessInfo.Time);

                foreach (var raceClassAvailability in SuccessInfo.AvailableClasses)
                {
                    _worldPacket.WriteUInt8(raceClassAvailability.RaceID);
                    _worldPacket.WriteInt32(raceClassAvailability.Classes.Count);

                    foreach (var classAvailability in raceClassAvailability.Classes)
                    {
                        _worldPacket.WriteUInt8(classAvailability.ClassID);
                        _worldPacket.WriteUInt8(classAvailability.ActiveExpansionLevel);
                        _worldPacket.WriteUInt8(classAvailability.AccountExpansionLevel);
                        _worldPacket.WriteUInt8(classAvailability.MinActiveExpansionLevel);
                    }
                }

                _worldPacket.WriteBit(SuccessInfo.IsExpansionTrial);
                _worldPacket.WriteBit(SuccessInfo.ForceCharacterTemplate);
                _worldPacket.WriteBit(SuccessInfo.NumPlayersHorde.HasValue);
                _worldPacket.WriteBit(SuccessInfo.NumPlayersAlliance.HasValue);
                _worldPacket.WriteBit(SuccessInfo.ExpansionTrialExpiration.HasValue);
                _worldPacket.WriteBit(SuccessInfo.NewBuildKeys != null);
                _worldPacket.FlushBits();

                {
                    _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.BillingPlan);
                    _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.TimeRemain);
                    _worldPacket.WriteUInt32(SuccessInfo.GameTimeInfo.Unknown735);
                    // 3x same bit is not a mistake - preserves legacy client behavior of BillingPlanFlags::SESSION_IGR
                    _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom check in function checking which lua event to fire when remaining time is near end - BILLING_NAG_DIALOG vs IGR_BILLING_NAG_DIALOG
                    _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // inGameRoom lua return from Script_GetBillingPlan
                    _worldPacket.WriteBit(SuccessInfo.GameTimeInfo.InGameRoom); // not used anywhere in the client
                    _worldPacket.FlushBits();
                }

                if (SuccessInfo.NumPlayersHorde.HasValue)
                    _worldPacket.WriteUInt16(SuccessInfo.NumPlayersHorde.Value);

                if (SuccessInfo.NumPlayersAlliance.HasValue)
                    _worldPacket.WriteUInt16(SuccessInfo.NumPlayersAlliance.Value);

                if(SuccessInfo.ExpansionTrialExpiration.HasValue)
                    _worldPacket.WriteInt64(SuccessInfo.ExpansionTrialExpiration.Value);

                if (SuccessInfo.NewBuildKeys != null)
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        _worldPacket.WriteUInt8(SuccessInfo.NewBuildKeys.NewBuildKey[i]);
                        _worldPacket.WriteUInt8(SuccessInfo.NewBuildKeys.SomeKey[i]);
                    }
                }

                foreach (VirtualRealmInfo virtualRealm in SuccessInfo.VirtualRealms)
                    virtualRealm.Write(_worldPacket);

                foreach (var templat in SuccessInfo.Templates)
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

        public AuthSuccessInfo SuccessInfo; // contains the packet data in case that it has account information (It is never set when WaitInfo is set), otherwise its contents are undefined.
        public AuthWaitInfo? WaitInfo; // contains the queue wait information in case the account is in the login queue.
        public BattlenetRpcErrorCode Result; // the result of the authentication process, possible values are @ref BattlenetRpcErrorCode

        public class AuthSuccessInfo
        {
            public byte ActiveExpansionLevel; // the current server expansion, the possible values are in @ref Expansions
            public byte AccountExpansionLevel; // the current expansion of this account, the possible values are in @ref Expansions
            public uint TimeRested; // affects the return value of the GetBillingTimeRested() client API call, it is the number of seconds you have left until the experience points and loot you receive from creatures and quests is reduced. It is only used in the Asia region in retail, it's not implemented in TC and will probably never be.

            public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region. @todo implement
            public uint TimeSecondsUntilPCKick; // @todo research
            public uint CurrencyID; // this is probably used for the ingame shop. @todo implement
            public long Time;

            public GameTime GameTimeInfo;

            public List<VirtualRealmInfo> VirtualRealms = new();     // list of realms connected to this one (inclusive) @todo implement
            public List<CharacterTemplate> Templates = new(); // list of pre-made character templates. @todo implement

            public List<RaceClassAvailability> AvailableClasses; // the minimum AccountExpansion required to select the classes

            public bool IsExpansionTrial;
            public bool ForceCharacterTemplate; // forces the client to always use a character template when creating a new character. @see Templates. @todo implement
            public ushort? NumPlayersHorde; // number of horde players in this realm. @todo implement
            public ushort? NumPlayersAlliance; // number of alliance players in this realm. @todo implement
            public long? ExpansionTrialExpiration; // expansion trial expiration unix timestamp
            public NewBuild NewBuildKeys;

            public struct GameTime
            {
                public uint BillingPlan;
                public uint TimeRemain;
                public uint Unknown735;
                public bool InGameRoom;
            }

            public class NewBuild
            {
                public Array<byte> NewBuildKey = new Array<byte>(16);
                public Array<byte> SomeKey = new Array<byte>(16);
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
            ByteBuffer whereBuffer = new();
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

            Sha256 hash = new();
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

    class EnterEncryptedMode : ServerPacket
    {
        byte[] EncryptionKey;
        bool Enabled;
        static byte[] expandedPrivateKey;

        static byte[] EnableEncryptionSeed = { 0x90, 0x9C, 0xD0, 0x50, 0x5A, 0x2C, 0x14, 0xDD, 0x5C, 0x2C, 0xC0, 0x64, 0x14, 0xF3, 0xFE, 0xC9 };
        static byte[] EnableEncryptionContext = { 0xA7, 0x1F, 0xB6, 0x9B, 0xC9, 0x7C, 0xDD, 0x96, 0xE9, 0xBB, 0xB8, 0x21, 0x39, 0x8D, 0x5A, 0xD4 };

        static byte[] EnterEncryptedModePrivateKey =
        {
            0x08, 0xBD, 0xC7, 0xA3, 0xCC, 0xC3, 0x4F, 0x3F,
            0x6A, 0x0B, 0xFF, 0xCF, 0x31, 0xC1, 0xB6, 0x97,
            0x69, 0x1E, 0x72, 0x9A, 0x0A, 0xAB, 0x2C, 0x77,
            0xC3, 0x6F, 0x8A, 0xE7, 0x5A, 0x9A, 0xA7, 0xC9
        };

        static EnterEncryptedMode()
        {
            expandedPrivateKey = Ed25519.ExpandedPrivateKeyFromSeed(EnterEncryptedModePrivateKey);
        }

        public EnterEncryptedMode(byte[] encryptionKey, bool enabled) : base(ServerOpcodes.EnterEncryptedMode)
        {
            EncryptionKey = encryptionKey;
            Enabled = enabled;
        }

        public override void Write()
        {
            HmacSha256 toSign = new(EncryptionKey);
            toSign.Process(BitConverter.GetBytes(Enabled), 1);
            toSign.Finish(EnableEncryptionSeed, 16);

            _worldPacket.WriteBytes(Ed25519.Sign(toSign.Digest, expandedPrivateKey, 0, EnableEncryptionContext));
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
