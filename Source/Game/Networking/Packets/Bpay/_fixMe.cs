// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Networking.Packets.Bpay
{



    /* class BattlePayVasPurchaseStarted final : public ServerPacket
    {
    public:
        BattlePayVasPurchaseStarted() : ServerPacket(SMSG_BATTLE_PAY_VAS_PURCHASE_STARTED, 4 + 4 + 16 + 8 + 4 + 4) { }

        WorldPacket const* Write() override;

        VasPurchase VasPurchase;
        uint32 UnkInt = 0;
    }; */

    /* class CharacterClassTrialCreate final : public ServerPacket
    {
    public:
        CharacterClassTrialCreate() : ServerPacket(SMSG_CHARACTER_CLASS_TRIAL_CREATE, 4) { }

        WorldPacket const* Write() override;

        uint32 Result = 0;
    }; */

    /* class BattlePayQueryClassTrialResult final : public ClientPacket
    {
    public:
        BattlePayQueryClassTrialResult(WorldPacket packet) : ClientPacket(CMSG_BATTLE_PAY_QUERY_CLASS_TRIAL_BOOST_RESULT, packet) { }

        void Read() override { }
    }; */

    /* class BattlePayCharacterUpgradeQueued final : public ServerPacket
    {
    public:
        BattlePayCharacterUpgradeQueued() : ServerPacket(SMSG_CHARACTER_UPGRADE_QUEUED, 4 + 16) { }

        WorldPacket const* Write() override;

        std::vector<uint32> EquipmentItems;
        ObjectGuid Character;
    }; */

    /* class BattlePayTrialBoostCharacter final : public ClientPacket
    {
    public:
        BattlePayTrialBoostCharacter(WorldPacket packet) : ClientPacket(CMSG_BATTLE_PAY_TRIAL_BOOST_CHARACTER, packet) { }

        void Read() override;

        ObjectGuid Character;
        uint32 SpecializationID = 0;
    }; */

    /* class BattlePayVasPurchaseList final : public ServerPacket
    {
    public:
        BattlePayVasPurchaseList() : ServerPacket(SMSG_BATTLE_PAY_VAS_PURCHASE_LIST, 4) { }

        WorldPacket const* Write() override;

        std::vector<VasPurchase> VasPurchase;
    }; */

    /* class PurchaseDetails final : public ServerPacket
    {
    public:
        PurchaseDetails() : ServerPacket(SMSG_BATTLE_PAY_PURCHASE_DETAILS, 20) { }

        WorldPacket const* Write() override;

        uint64 UnkLong = 0;
        uint32 UnkInt = 0;
        uint32 VasPurchaseProgress = 0;
        std::string Key;
        std::string Key2;
    }; */

    /* class PurchaseUnk final : public ServerPacket
    {
    public:
        PurchaseUnk() : ServerPacket(SMSG_BATTLE_PAY_PURCHASE_UNK, 20) { }

        WorldPacket const* Write() override;

        uint32 UnkInt = 0;
        std::string Key;
        uint8 UnkByte = 0;
    }; */


    /* class PurchaseDetailsResponse final : public ClientPacket
    {
    public:
        PurchaseDetailsResponse(WorldPacket packet) : ClientPacket(CMSG_BATTLE_PAY_PURCHASE_DETAILS_RESPONSE, packet) { }

        void Read() override;

        uint8 UnkByte = 0;
    }; */

    /* class PurchaseUnkResponse final : public ClientPacket
    {
    public:
        PurchaseUnkResponse(WorldPacket packet) : ClientPacket(CMSG_BATTLE_PAY_PURCHASE_UNK_RESPONSE, packet) { }

        void Read() override;

        std::string Key;
        std::string Key2;
    }; */

}
