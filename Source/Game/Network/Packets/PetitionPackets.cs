/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

        public ObjectGuid ItemGUID { get; set; }
        public uint PetitionID { get; set; } = 0;
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

        public uint PetitionID { get; set; } = 0;
        public bool Allow { get; set; } = false;
        public PetitionInfo Info { get; set; }
    }

    public class PetitionShowList : ClientPacket
    {
        public PetitionShowList(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionUnit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetitionUnit { get; set; }
    }

    public class ServerPetitionShowList : ServerPacket
    {
        public ServerPetitionShowList() : base(ServerOpcodes.PetitionShowList) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt32(Price);
        }

        public ObjectGuid Unit { get; set; }
        public uint Price { get; set; } = 0;
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

        public ObjectGuid Unit { get; set; }
        public string Title { get; set; }
    }

    public class PetitionShowSignatures : ClientPacket
    {
        public PetitionShowSignatures(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Item = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Item { get; set; }
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

        public ObjectGuid Item { get; set; }
        public ObjectGuid Owner { get; set; }
        public ObjectGuid OwnerAccountID { get; set; }
        public int PetitionID { get; set; } = 0;
        public List<PetitionSignature> Signatures { get; set; }

        public struct PetitionSignature
        {
            public ObjectGuid Signer { get; set; }
            public int Choice { get; set; }
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

        public ObjectGuid PetitionGUID { get; set; }
        public byte Choice { get; set; } = 0;
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

        public ObjectGuid Item { get; set; }
        public ObjectGuid Player { get; set; }
        public PetitionSigns Error { get; set; } = 0;
    }

    public class PetitionAlreadySigned : ServerPacket
    {
        public PetitionAlreadySigned() : base(ServerOpcodes.PetitionAlreadySigned) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(SignerGUID);
        }

        public ObjectGuid SignerGUID { get; set; }
    }

    public class DeclinePetition : ClientPacket
    {
        public DeclinePetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetitionGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetitionGUID { get; set; }
    }

    public class TurnInPetition : ClientPacket
    {
        public TurnInPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Item = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Item { get; set; }
    }

    public class TurnInPetitionResult : ServerPacket
    {
        public TurnInPetitionResult() : base(ServerOpcodes.TurnInPetitionResult) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Result, 4);
            _worldPacket.FlushBits();
        }

        public PetitionTurns Result { get; set; } = 0; // PetitionError
    }

    public class OfferPetition : ClientPacket
    {
        public OfferPetition(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemGUID = _worldPacket.ReadPackedGuid();
            TargetPlayer = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid TargetPlayer { get; set; }
        public ObjectGuid ItemGUID { get; set; }
    }

    public class OfferPetitionError : ServerPacket
    {
        public OfferPetitionError() : base(ServerOpcodes.OfferPetitionError) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGUID);
        }

        public ObjectGuid PlayerGUID { get; set; }
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

        public ObjectGuid PetitionGuid { get; set; }
        public string NewGuildName { get; set; }
    }

    public class PetitionRenameGuildResponse : ServerPacket
    {
        public PetitionRenameGuildResponse() : base(ServerOpcodes.PetitionRenameGuildResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetitionGuid);

            _worldPacket.WriteBits(NewGuildName.Length, 7);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(NewGuildName);
        }

        public ObjectGuid PetitionGuid { get; set; }
        public string NewGuildName { get; set; }
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

            data.WriteBits(Title.Length, 7);
            data.WriteBits(BodyText.Length, 12);

            for (byte i = 0; i < 10; i++)
                data.WriteBits(Choicetext[i].Length, 6);

            data.FlushBits();

            for (byte i = 0; i < 10; i++)
                data.WriteString(Choicetext[i]);

            data.WriteString(Title);
            data.WriteString(BodyText);
        }

        public int PetitionID { get; set; }
        public ObjectGuid Petitioner { get; set; }
        public string Title { get; set; }
        public string BodyText { get; set; }
        public int MinSignatures { get; set; }
        public int MaxSignatures { get; set; }
        public int DeadLine { get; set; }
        public int IssueDate { get; set; }
        public int AllowedGuildID { get; set; }
        public int AllowedClasses { get; set; }
        public int AllowedRaces { get; set; }
        public short AllowedGender { get; set; }
        public int AllowedMinLevel { get; set; }
        public int AllowedMaxLevel { get; set; }
        public int NumChoices { get; set; }
        public int StaticType { get; set; }
        public uint Muid { get; set; } = 0;
        public StringArray Choicetext { get; set; } = new StringArray(10);
    }
}
