/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.LearnTalents)]
        void HandleLearnTalents(LearnTalents packet)
        {
            LearnTalentFailed learnTalentFailed = new LearnTalentFailed();
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
                    anythingLearned = true;
            }

            if (learnTalentFailed.Reason != 0)
                SendPacket(learnTalentFailed);

            if (anythingLearned)
                GetPlayer().SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.LearnPvpTalents)]
        void HandleLearnPvpTalents(LearnPvpTalents packet)
        {
            LearnPvpTalentFailed learnPvpTalentFailed = new LearnPvpTalentFailed();
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
                    anythingLearned = true;
            }

            if (learnPvpTalentFailed.Reason != 0)
                SendPacket(learnPvpTalentFailed);

            if (anythingLearned)
                _player.SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.ConfirmRespecWipe)]
        void HandleConfirmRespecWipe(ConfirmRespecWipe confirmRespecWipe)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(confirmRespecWipe.RespecMaster, NPCFlags.Trainer, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTalentWipeConfirm - {0} not found or you can't interact with him.", confirmRespecWipe.RespecMaster.ToString());
                return;
            }

            if (confirmRespecWipe.RespecType != SpecResetType.Talents)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleConfirmRespecWipe - reset type {0} is not implemented.", confirmRespecWipe.RespecType);
                return;
            }

            if (!unit.CanResetTalents(_player))
                return;

            if (!_player.PlayerTalkClass.GetGossipMenu().HasMenuItemType((uint)GossipOption.Unlearntalents))
                return;

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            if (!GetPlayer().ResetTalents())
            {
                GetPlayer().SendRespecWipeConfirm(ObjectGuid.Empty, 0);
                return;
            }

            GetPlayer().SendTalentsInfoData();
            unit.CastSpell(GetPlayer(), 14867, true);                  //spell: "Untalent Visual Effect"
        }

        [WorldPacketHandler(ClientOpcodes.UnlearnSkill)]
        void HandleUnlearnSkill(UnlearnSkill packet)
        {
            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(packet.SkillLine, GetPlayer().GetRace(), GetPlayer().GetClass());
            if (rcEntry == null || !rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.Unlearnable))
                return;

            GetPlayer().SetSkill(packet.SkillLine, 0, 0, 0);
        }
    }
}
