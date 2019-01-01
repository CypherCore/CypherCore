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

using Framework.Collections;
using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class QueryPetition : ClientPacket
    {
        public QueryPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionID = _worldPacket.ReadUInt32();
            ItemGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ItemGUID;
        public uint PetitionID;
    }

    public class QueryPetitionResponse : ServerPacket
    {
        public QueryPetitionResponse() : base(ServerOpcodes.QueryPetitionResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(PetitionID);
            _worldPacket.WriteBit(Allow);
            _worldPacket.FlushBits();

            if (Allow)
                Info.Write(_worldPacket);
        }

        public uint PetitionID = 0;
        public bool Allow = false;
        public PetitionInfo Info;
    }

    public class PetitionShowList : ClientPacket
    {
        public PetitionShowList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionUnit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetitionUnit;
    }

    public class ServerPetitionShowList : ServerPacket
    {
        public ServerPetitionShowList() : base(ServerOpcodes.PetitionShowList) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt32(Price);
        }

        public ObjectGuid Unit;
        public uint Price = 0;
    }

    public class PetitionBuy : ClientPacket
    {
        public PetitionBuy(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint titleLen = _worldPacket.ReadBits<uint>(7);

            Unit = _worldPacket.ReadPackedGuid();
            Title = _worldPacket.ReadString(titleLen);
        }

        public ObjectGuid Unit;
        public string Title;
    }

    public class PetitionShowSignatures : ClientPacket
    {
        public PetitionShowSignatures(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Item = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Item;
    }

    public class ServerPetitionShowSignatures : ServerPacket
    {
        public ServerPetitionShowSignatures() : base(ServerOpcodes.PetitionShowSignatures)
        {
            Signatures = new List<PetitionSignature>();
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
            _worldPacket.WritePackedGuid(Owner);
            _worldPacket.WritePackedGuid(OwnerAccountID);
            _worldPacket.WriteInt32(PetitionID);

            _worldPacket.WriteUInt32(Signatures.Count);
            foreach (PetitionSignature signature in Signatures)
            {
                _worldPacket.WritePackedGuid(signature.Signer);
                _worldPacket.WriteInt32(signature.Choice);
            }
        }

        public ObjectGuid Item;
        public ObjectGuid Owner;
        public ObjectGuid OwnerAccountID;
        public int PetitionID = 0;
        public List<PetitionSignature> Signatures;

        public struct PetitionSignature
        {
            public ObjectGuid Signer;
            public int Choice;
        }
    }

    public class SignPetition : ClientPacket
    {
        public SignPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionGUID = _worldPacket.ReadPackedGuid();
            Choice = _worldPacket.ReadUInt8();
        }

        public ObjectGuid PetitionGUID;
        public byte Choice;
    }

    public class PetitionSignResults : ServerPacket
    {
        public PetitionSignResults() : base(ServerOpcodes.PetitionSignResults) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Item);
            _worldPacket.WritePackedGuid(Player);

            _worldPacket.WriteBits(Error, 4);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Item;
        public ObjectGuid Player;
        public PetitionSigns Error = 0;
    }

    public class PetitionAlreadySigned : ServerPacket
    {
        public PetitionAlreadySigned() : base(ServerOpcodes.PetitionAlreadySigned) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SignerGUID);
        }

        public ObjectGuid SignerGUID;
    }

    public class DeclinePetition : ClientPacket
    {
        public DeclinePetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetitionGUID;
    }

    public class TurnInPetition : ClientPacket
    {
        public TurnInPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Item = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Item;
    }

    public class TurnInPetitionResult : ServerPacket
    {
        public TurnInPetitionResult() : base(ServerOpcodes.TurnInPetitionResult) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Result, 4);
            _worldPacket.FlushBits();
        }

        public PetitionTurns Result = 0; // PetitionError
    }

    public class OfferPetition : ClientPacket
    {
        public OfferPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
            TargetPlayer = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetPlayer;
        public ObjectGuid ItemGUID;
    }

    public class OfferPetitionError : ServerPacket
    {
        public OfferPetitionError() : base(ServerOpcodes.OfferPetitionError) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
        }

        public ObjectGuid PlayerGUID;
    }

    public class PetitionRenameGuild : ClientPacket
    {
        public PetitionRenameGuild(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionGuid = _worldPacket.ReadPackedGuid();

            _worldPacket.ResetBitPos();
            uint nameLen = _worldPacket.ReadBits<uint>(7);

            NewGuildName = _worldPacket.ReadString(nameLen);
        }

        public ObjectGuid PetitionGuid;
        public string NewGuildName;
    }

    public class PetitionRenameGuildResponse : ServerPacket
    {
        public PetitionRenameGuildResponse() : base(ServerOpcodes.PetitionRenameGuildResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetitionGuid);

            _worldPacket.WriteBits(NewGuildName.GetByteCount(), 7);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(NewGuildName);
        }

        public ObjectGuid PetitionGuid;
        public string NewGuildName;
    }

    public class PetitionInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(PetitionID);
            data.WritePackedGuid(Petitioner);

            data.WriteInt32(MinSignatures);
            data.WriteInt32(MaxSignatures);
            data.WriteInt32(DeadLine);
            data.WriteInt32(IssueDate);
            data.WriteInt32(AllowedGuildID);
            data.WriteInt32(AllowedClasses);
            data.WriteInt32(AllowedRaces);
            data.WriteInt16(AllowedGender);
            data.WriteInt32(AllowedMinLevel);
            data.WriteInt32(AllowedMaxLevel);
            data.WriteInt32(NumChoices);
            data.WriteInt32(StaticType);
            data.WriteUInt32(Muid);

            data.WriteBits(Title.GetByteCount(), 7);
            data.WriteBits(BodyText.GetByteCount(), 12);

            for (byte i = 0; i < Choicetext.Length; i++)
                data.WriteBits(Choicetext[i].GetByteCount(), 6);

            data.FlushBits();

            for (byte i = 0; i < Choicetext.Length; i++)
                data.WriteString(Choicetext[i]);

            data.WriteString(Title);
            data.WriteString(BodyText);
        }

        public int PetitionID;
        public ObjectGuid Petitioner;
        public string Title;
        public string BodyText;
        public int MinSignatures;
        public int MaxSignatures;
        public int DeadLine;
        public int IssueDate;
        public int AllowedGuildID;
        public int AllowedClasses;
        public int AllowedRaces;
        public short AllowedGender;
        public int AllowedMinLevel;
        public int AllowedMaxLevel;
        public int NumChoices;
        public int StaticType;
        public uint Muid = 0;
        public StringArray Choicetext = new StringArray(10);
    }
}
