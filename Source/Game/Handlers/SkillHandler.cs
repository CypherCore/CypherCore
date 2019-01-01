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
using Game.DataStorage;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.LearnTalents)]
        void HandleLearnTalents(LearnTalents packet)
        {
            LearnTalentsFailed learnTalentsFailed = new LearnTalentsFailed();
            bool anythingLearned = false;
            foreach (uint talentId in packet.Talents)
            {
                TalentLearnResult result = _player.LearnTalent(talentId, ref learnTalentsFailed.SpellID);
                if (result != 0)
                {
                    if (learnTalentsFailed.Reason == 0)
                        learnTalentsFailed.Reason = (uint)result;

                    learnTalentsFailed.Talents.Add((ushort)talentId);
                }
                else
                    anythingLearned = true;
            }

            if (learnTalentsFailed.Reason != 0)
                SendPacket(learnTalentsFailed);

            if (anythingLearned)
                GetPlayer().SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.LearnPvpTalents)]
        void HandleLearnPvpTalents(LearnPvpTalents packet)
        {
            LearnPvpTalentsFailed learnPvpTalentsFailed = new LearnPvpTalentsFailed();
            bool anythingLearned = false;
            foreach (var pvpTalent in packet.Talents)
            {
                TalentLearnResult result = _player.LearnPvpTalent(pvpTalent.PvPTalentID, pvpTalent.Slot, ref learnPvpTalentsFailed.SpellID);
                if (result != 0)
                {
                    if (learnPvpTalentsFailed.Reason == 0)
                        learnPvpTalentsFailed.Reason = (uint)result;

                    learnPvpTalentsFailed.Talents.Add(pvpTalent);
                }
                else
                    anythingLearned = true;
            }

            if (learnPvpTalentsFailed.Reason != 0)
                SendPacket(learnPvpTalentsFailed);

            if (anythingLearned)
                _player.SendTalentsInfoData();
        }

        [WorldPacketHandler(ClientOpcodes.ConfirmRespecWipe)]
        void HandleConfirmRespecWipe(ConfirmRespecWipe confirmRespecWipe)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(confirmRespecWipe.RespecMaster, NPCFlags.Trainer);
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
