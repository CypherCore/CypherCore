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
using Framework.Database;
using Game.Entities;
using Game.Guilds;
using Game.Network;
using Game.Network.Packets;
using System.Collections.Generic;
using System.Text;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.PetitionBuy)]
        void HandlePetitionBuy(PetitionBuy packet)
        {
            // prevent cheating
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Petitioner);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandlePetitionBuyOpcode - {0} not found or you can't interact with him.", packet.Unit.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            uint charterItemID = GuildConst.CharterItemId;
            int cost = WorldConfig.GetIntValue(WorldCfg.CharterCostGuild);

            // do not let if already in guild.
            if (GetPlayer().GetGuildId() != 0)
                return;

            if (Global.GuildMgr.GetGuildByName(packet.Title))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.Title);
                return;
            }

            if (Global.ObjectMgr.IsReservedName(packet.Title) || !ObjectManager.IsValidCharterName(packet.Title))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.Title);
                return;
            }

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(charterItemID);
            if (pProto == null)
            {
                GetPlayer().SendBuyError(BuyResult.CantFindItem, null, charterItemID);
                return;
            }

            if (!GetPlayer().HasEnoughMoney(cost))
            {                                                       //player hasn't got enough money
                GetPlayer().SendBuyError(BuyResult.NotEnoughtMoney, creature, charterItemID);
                return;
            }

            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = GetPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, charterItemID, pProto.GetBuyCount());
            if (msg != InventoryResult.Ok)
            {
                GetPlayer().SendEquipError(msg, null, null, charterItemID);
                return;
            }

            GetPlayer().ModifyMoney(-cost);
            Item charter = GetPlayer().StoreNewItem(dest, charterItemID, true);
            if (!charter)
                return;

            charter.SetUInt32Value(ItemFields.Enchantment, (uint)charter.GetGUID().GetCounter());
            // ITEM_FIELD_ENCHANTMENT_1_1 is guild/arenateam id
            // ITEM_FIELD_ENCHANTMENT_1_1+1 is current signatures count (showed on item)
            charter.SetState(ItemUpdateState.Changed, GetPlayer());
            GetPlayer().SendNewItem(charter, 1, true, false);

            // a petition is invalid, if both the owner and the type matches
            // we checked above, if this player is in an arenateam, so this must be
            // datacorruption
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_BY_OWNER);
            stmt.AddValue(0, GetPlayer().GetGUID().GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            StringBuilder ssInvalidPetitionGUIDs = new StringBuilder();

            if (!result.IsEmpty())
            {
                do
                {
                    ssInvalidPetitionGUIDs.AppendFormat("'{0}', ", result.Read<uint>(0));
                } while (result.NextRow());
            }

            // delete petitions with the same guid as this one
            ssInvalidPetitionGUIDs.AppendFormat("'{0}'", charter.GetGUID().GetCounter());

            Log.outDebug(LogFilter.Network, "Invalid petition GUIDs: {0}", ssInvalidPetitionGUIDs.ToString());
            SQLTransaction trans = new SQLTransaction();
            trans.Append("DELETE FROM petition WHERE petitionguid IN ({0})", ssInvalidPetitionGUIDs.ToString());
            trans.Append("DELETE FROM petition_sign WHERE petitionguid IN ({0})", ssInvalidPetitionGUIDs.ToString());

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PETITION);
            stmt.AddValue(0, GetPlayer().GetGUID().GetCounter());
            stmt.AddValue(1, charter.GetGUID().GetCounter());
            stmt.AddValue(2, packet.Title);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.PetitionShowSignatures)]
        void HandlePetitionShowSignatures(PetitionShowSignatures packet)
        {
            Log.outDebug(LogFilter.Network, "Received opcode CMSG_PETITION_SHOW_SIGNATURES");

            // if has guild => error, return;
            if (GetPlayer().GetGuildId() != 0)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIGNATURE);
            stmt.AddValue(0, packet.Item.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            ServerPetitionShowSignatures signaturesPacket = new ServerPetitionShowSignatures();
            signaturesPacket.Item = packet.Item;
            signaturesPacket.Owner = GetPlayer().GetGUID();
            signaturesPacket.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, ObjectManager.GetPlayerAccountIdByGUID(GetPlayer().GetGUID()));
            signaturesPacket.PetitionID = (int)packet.Item.GetCounter();  // @todo verify that...

            do
            {
                ObjectGuid signerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));

                ServerPetitionShowSignatures.PetitionSignature signature = new ServerPetitionShowSignatures.PetitionSignature();
                signature.Signer = signerGUID;
                signature.Choice = 0;
                signaturesPacket.Signatures.Add(signature);
            }
            while (result.NextRow());

            SendPacket(signaturesPacket);
        }

        [WorldPacketHandler(ClientOpcodes.QueryPetition)]
        void HandleQueryPetition(QueryPetition packet)
        {
            SendPetitionQuery(packet.ItemGUID);
        }

        public void SendPetitionQuery(ObjectGuid petitionGUID)
        {
            ObjectGuid ownerGUID = ObjectGuid.Empty;
            string title = "NO_NAME_FOR_GUID";

            QueryPetitionResponse responsePacket = new QueryPetitionResponse();
            responsePacket.PetitionID = (uint)petitionGUID.GetCounter();  // PetitionID (in Trinity always same as GUID_LOPART(petition guid))

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION);
            stmt.AddValue(0, petitionGUID.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                ownerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                title = result.Read<string>(1);
            }
            else
            {
                Log.outDebug(LogFilter.Network, "CMSG_PETITION_Select failed for petition ({0})", petitionGUID.ToString());
                return;
            }

            int reqSignatures = WorldConfig.GetIntValue(WorldCfg.MinPetitionSigns);

            PetitionInfo petitionInfo = new PetitionInfo();
            petitionInfo.PetitionID = (int)petitionGUID.GetCounter();
            petitionInfo.Petitioner = ownerGUID;
            petitionInfo.MinSignatures = reqSignatures;
            petitionInfo.MaxSignatures = reqSignatures;
            petitionInfo.Title = title;

            responsePacket.Allow = true;
            responsePacket.Info = petitionInfo;

            SendPacket(responsePacket);
        }

        [WorldPacketHandler(ClientOpcodes.PetitionRenameGuild)]
        void HandlePetitionRenameGuild(PetitionRenameGuild packet)
        {
            Item item = GetPlayer().GetItemByGuid(packet.PetitionGuid);
            if (!item)
                return;

            if (Global.GuildMgr.GetGuildByName(packet.NewGuildName))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.NewGuildName);
                return;
            }
            if (Global.ObjectMgr.IsReservedName(packet.NewGuildName) || !ObjectManager.IsValidCharterName(packet.NewGuildName))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.NewGuildName);
                return;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PETITION_NAME);
            stmt.AddValue(0, packet.NewGuildName);
            stmt.AddValue(1, packet.PetitionGuid.GetCounter());
            DB.Characters.Execute(stmt);

            PetitionRenameGuildResponse renameResponse = new PetitionRenameGuildResponse();
            renameResponse.PetitionGuid = packet.PetitionGuid;
            renameResponse.NewGuildName = packet.NewGuildName;
            SendPacket(renameResponse);
        }

        [WorldPacketHandler(ClientOpcodes.SignPetition)]
        void HandleSignPetition(SignPetition packet)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIGNATURES);
            stmt.AddValue(0, packet.PetitionGUID.GetCounter());
            stmt.AddValue(1, packet.PetitionGUID.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Network, "Petition {0} is not found for player {1} {2}", packet.PetitionGUID.ToString(), GetPlayer().GetGUID().ToString(), GetPlayer().GetName());
                return;
            }

            ObjectGuid ownerGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
            //ulong signs = result.Read<ulong>(1);

            if (ownerGuid == GetPlayer().GetGUID())
                return;

            // not let enemies sign guild charter
            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && GetPlayer().GetTeam() != ObjectManager.GetPlayerTeamByGUID(ownerGuid))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);
                return;
            }

            if (GetPlayer().GetGuildId() != 0)
            {
                Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, GetPlayer().GetName());
                return;
            }
            if (GetPlayer().GetGuildIdInvited() != 0)
            {
                Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, GetPlayer().GetName());
                return;
            }

            // Client doesn't allow to sign petition two times by one character, but not check sign by another character from same account
            // not allow sign another player from already sign player account
            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIG_BY_ACCOUNT);
            stmt.AddValue(0, GetAccountId());
            stmt.AddValue(1, packet.PetitionGUID.GetCounter());
            result = DB.Characters.Query(stmt);

            PetitionSignResults signResult = new PetitionSignResults();
            signResult.Player = GetPlayer().GetGUID();
            signResult.Item = packet.PetitionGUID;

            if (!result.IsEmpty())
            {
                signResult.Error = PetitionSigns.AlreadySigned;

                // close at signer side
                SendPacket(signResult);
                return;
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PETITION_SIGNATURE);
            stmt.AddValue(0, ownerGuid.GetCounter());
            stmt.AddValue(1, packet.PetitionGUID.GetCounter());
            stmt.AddValue(2, GetPlayer().GetGUID().GetCounter());
            stmt.AddValue(3, GetAccountId());
            DB.Characters.Execute(stmt);

            Log.outDebug(LogFilter.Network, "PETITION SIGN: {0} by player: {1} ({2} Account: {3})", packet.PetitionGUID.ToString(), GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetAccountId());

            signResult.Error = PetitionSigns.Ok;

            // close at signer side
            SendPacket(signResult);

            // update for owner if online
            Player owner = Global.ObjAccessor.FindPlayer(ownerGuid);
            if (owner)
                owner.SendPacket(signResult);
        }

        [WorldPacketHandler(ClientOpcodes.DeclinePetition)]
        void HandleDeclinePetition(DeclinePetition packet)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_OWNER_BY_GUID);
            stmt.AddValue(0, packet.PetitionGUID.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
                return;

            ObjectGuid ownerguid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));

            Player owner = Global.ObjAccessor.FindPlayer(ownerguid);
            if (owner)                                               // petition owner online
            {
                // Disabled because packet isn't handled by the client in any way
                /*
                WorldPacket data = new WorldPacket(ServerOpcodes.PetitionDecline);
                data.WritePackedGuid(GetPlayer().GetGUID());
                owner.SendPacket(data);
                */
            }
        }

        [WorldPacketHandler(ClientOpcodes.OfferPetition)]
        void HandleOfferPetition(OfferPetition packet)
        {
            Player player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetPlayer);
            if (!player)
                return;

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && GetPlayer().GetTeam() != player.GetTeam())
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);
                return;
            }

            if (player.GetGuildId() != 0)
            {
                Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, GetPlayer().GetName());
                return;
            }

            if (player.GetGuildIdInvited() != 0)
            {
                Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, GetPlayer().GetName());
                return;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIGNATURE);
            stmt.AddValue(0, packet.ItemGUID.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            ServerPetitionShowSignatures signaturesPacket = new ServerPetitionShowSignatures();
            signaturesPacket.Item = packet.ItemGUID;
            signaturesPacket.Owner = GetPlayer().GetGUID();
            signaturesPacket.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, player.GetSession().GetAccountId());
            signaturesPacket.PetitionID = (int)packet.ItemGUID.GetCounter();  // @todo verify that...

            do
            {
                ObjectGuid signerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));

                ServerPetitionShowSignatures.PetitionSignature signature = new ServerPetitionShowSignatures.PetitionSignature();
                signature.Signer = signerGUID;
                signature.Choice = 0;
                signaturesPacket.Signatures.Add(signature);
            }
            while (result.NextRow());

            player.SendPacket(signaturesPacket);
        }

        [WorldPacketHandler(ClientOpcodes.TurnInPetition)]
        void HandleTurnInPetition(TurnInPetition packet)
        {
            // Check if player really has the required petition charter
            Item item = GetPlayer().GetItemByGuid(packet.Item);
            if (!item)
                return;

            // Get petition data from db
            ObjectGuid ownerguid;
            string name;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION);
            stmt.AddValue(0, packet.Item.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                ownerguid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                name = result.Read<string>(1);
            }
            else
            {
                Log.outError(LogFilter.Network, "Player {0} ({1}) tried to turn in petition ({2}) that is not present in the database", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.Item.ToString());
                return;
            }

            // Only the petition owner can turn in the petition
            if (GetPlayer().GetGUID() != ownerguid)
                return;

            TurnInPetitionResult resultPacket = new TurnInPetitionResult();

            // Check if player is already in a guild
            if (GetPlayer().GetGuildId() != 0)
            {
                resultPacket.Result = PetitionTurns.AlreadyInGuild;
                GetPlayer().SendPacket(resultPacket);
                return;
            }

            // Check if guild name is already taken
            if (Global.GuildMgr.GetGuildByName(name))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, name);
                return;
            }

            // Get petition signatures from db
            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PETITION_SIGNATURE);
            stmt.AddValue(0, packet.Item.GetCounter());
            result = DB.Characters.Query(stmt);

            List<ObjectGuid> guids = new List<ObjectGuid>();
            if (!result.IsEmpty())
            {
                do
                {
                    guids.Add(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)));
                }
                while (result.NextRow());
            }

            uint requiredSignatures = WorldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

            // Notify player if signatures are missing
            if (guids.Count < requiredSignatures)
            {
                resultPacket.Result = PetitionTurns.NeedMoreSignatures;
                SendPacket(resultPacket);
                return;
            }
            // Proceed with guild/arena team creation

            // Delete charter item
            GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);

            // Create guild
            Guild guild = new Guild();
            if (!guild.Create(GetPlayer(), name))
                return;

            // Register guild and add guild master
            Global.GuildMgr.AddGuild(guild);

            Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.Success, name);

            SQLTransaction trans = new SQLTransaction();

            // Add members from signatures
            foreach (var guid in guids)
                guild.AddMember(trans, guid);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_BY_GUID);
            stmt.AddValue(0, packet.Item.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_GUID);
            stmt.AddValue(0, packet.Item.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            // created
            Log.outDebug(LogFilter.Network, "Player {0} ({1}) turning in petition {2}", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.Item.ToString());

            resultPacket.Result = PetitionTurns.Ok;
            SendPacket(resultPacket);
        }

        [WorldPacketHandler(ClientOpcodes.PetitionShowList)]
        void HandlePetitionShowList(PetitionShowList packet)
        {
            SendPetitionShowList(packet.PetitionUnit);
        }

        public void SendPetitionShowList(ObjectGuid guid)
        {
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(guid, NPCFlags.Petitioner);
            if (!creature)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandlePetitionShowListOpcode - {0} not found or you can't interact with him.", guid.ToString());
                return;
            }

            WorldPacket data = new WorldPacket(ServerOpcodes.PetitionShowList);
            data.WritePackedGuid(guid);                                           // npc guid

            ServerPetitionShowList packet = new ServerPetitionShowList();
            packet.Unit = guid;
            packet.Price = WorldConfig.GetUIntValue(WorldCfg.CharterCostGuild);
            SendPacket(packet);
        }
    }
}
