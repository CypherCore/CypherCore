// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.LearnTalents, Processing = PacketProcessing.Inplace)]
        private void HandleLearnTalents(LearnTalents packet)
        {
            LearnTalentFailed learnTalentFailed = new();
            bool anythingLearned = false;

            foreach (uint talentId in packet.Talents)
            {
                TalentLearnResult result = _player.LearnTalent(talentId, ref learnTalentFailed.SpellID);

                if (result != 0)
                {
                    if (learnTalentFailed.Reason == 0)
                        learnTalentFailed.Reason = (uint)result;

                    learnTalentFailed.Talents.Add((ushort)talentId);
                }
                else
                {
                    anythingLearned = true;
                }
            }

            if (learnTalentFailed.Reason != 0)
                SendPacket(learnTalentFailed);

            if (anythingLearned)
                GetPlayer().SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.LearnPvpTalents, Processing = PacketProcessing.Inplace)]
        private void HandleLearnPvpTalents(LearnPvpTalents packet)
        {
            LearnPvpTalentFailed learnPvpTalentFailed = new();
            bool anythingLearned = false;

            foreach (var pvpTalent in packet.Talents)
            {
                TalentLearnResult result = _player.LearnPvpTalent(pvpTalent.PvPTalentID, pvpTalent.Slot, ref learnPvpTalentFailed.SpellID);

                if (result != 0)
                {
                    if (learnPvpTalentFailed.Reason == 0)
                        learnPvpTalentFailed.Reason = (uint)result;

                    learnPvpTalentFailed.Talents.Add(pvpTalent);
                }
                else
                {
                    anythingLearned = true;
                }
            }

            if (learnPvpTalentFailed.Reason != 0)
                SendPacket(learnPvpTalentFailed);

            if (anythingLearned)
                _player.SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.ConfirmRespecWipe)]
        private void HandleConfirmRespecWipe(ConfirmRespecWipe confirmRespecWipe)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(confirmRespecWipe.RespecMaster, NPCFlags.Trainer, NPCFlags2.None);

            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTalentWipeConfirm - {0} not found or you can't interact with him.", confirmRespecWipe.RespecMaster.ToString());

                return;
            }

            if (confirmRespecWipe.RespecType != SpecResetType.Talents)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleConfirmRespecWipe - reset Type {0} is not implemented.", confirmRespecWipe.RespecType);

                return;
            }

            if (!unit.CanResetTalents(_player))
                return;

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            if (!GetPlayer().ResetTalents())
                return;

            GetPlayer().SendTalentsInfoData();
            unit.CastSpell(GetPlayer(), 14867, true); //spell: "Untalent Visual Effect"
        }

        [WorldPacketHandler(ClientOpcodes.UnlearnSkill, Processing = PacketProcessing.Inplace)]
        private void HandleUnlearnSkill(UnlearnSkill packet)
        {
            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(packet.SkillLine, GetPlayer().GetRace(), GetPlayer().GetClass());

            if (rcEntry == null ||
                !rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.Unlearnable))
                return;

            GetPlayer().SetSkill(packet.SkillLine, 0, 0, 0);
        }

        [WorldPacketHandler(ClientOpcodes.TradeSkillSetFavorite, Processing = PacketProcessing.Inplace)]
        private void HandleTradeSkillSetFavorite(TradeSkillSetFavorite tradeSkillSetFavorite)
        {
            if (!_player.HasSpell(tradeSkillSetFavorite.RecipeID))
                return;

            _player.SetSpellFavorite(tradeSkillSetFavorite.RecipeID, tradeSkillSetFavorite.IsFavorite);
        }
    }
}