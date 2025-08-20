// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.PetitionBuy)]
        void HandlePetitionBuy(PetitionBuy packet)
        {
            // prevent cheating
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Petitioner, NPCFlags2.None);
            if (creature == null)
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

            if (Global.GuildMgr.GetGuildByName(packet.Title) != null)
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

            List<ItemPosCount> dest = new();
            InventoryResult msg = GetPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, charterItemID, pProto.GetBuyCount());
            if (msg != InventoryResult.Ok)
            {
                GetPlayer().SendEquipError(msg, null, null, charterItemID);
                return;
            }

            GetPlayer().ModifyMoney(-cost);
            Item charter = GetPlayer().StoreNewItem(dest, charterItemID, true);
            if (charter == null)
                return;

            charter.SetPetitionId((uint)charter.GetGUID().GetCounter());
            charter.SetState(ItemUpdateState.Changed, GetPlayer());
            GetPlayer().SendNewItem(charter, 1, true, false);

            // a petition is invalid, if both the owner and the type matches
            // we checked above, if this player is in an arenateam, so this must be
            // datacorruption
            Petition petition = Global.PetitionMgr.GetPetitionByOwner(_player.GetGUID());
            if (petition != null)
            {
                // clear from petition store
                Global.PetitionMgr.RemovePetition(petition.PetitionGuid);
                Log.outDebug(LogFilter.Network, $"Invalid petition GUID: {petition.PetitionGuid.GetCounter()}");
            }

            // fill petition store
            Global.PetitionMgr.AddPetition(charter.GetGUID(), _player.GetGUID(), packet.Title, false);
        }

        [WorldPacketHandler(ClientOpcodes.PetitionShowSignatures)]
        void HandlePetitionShowSignatures(PetitionShowSignatures packet)
        {
            Petition petition = Global.PetitionMgr.GetPetition(packet.Item);
            if (petition == null)
            {
                Log.outDebug(LogFilter.PlayerItems, $"Petition {packet.Item} is not found for player {GetPlayer().GetGUID().GetCounter()} {GetPlayer().GetName()}");
                return;
            }

            // if has guild => error, return;
            if (_player.GetGuildId() != 0)
                return;

            SendPetitionSigns(petition, _player);
        }

        void SendPetitionSigns(Petition petition, Player sendTo)
        {
            ServerPetitionShowSignatures signaturesPacket = new();
            signaturesPacket.Item = petition.PetitionGuid;
            signaturesPacket.Owner = petition.ownerGuid;
            signaturesPacket.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(petition.ownerGuid));
            signaturesPacket.PetitionID = (int)petition.PetitionGuid.GetCounter();

            foreach (var signature in petition.Signatures)
            {
                ServerPetitionShowSignatures.PetitionSignature signaturePkt = new();
                signaturePkt.Signer = signature.PlayerGuid;
                signaturePkt.Choice = 0;
                signaturesPacket.Signatures.Add(signaturePkt);
            }

            SendPacket(signaturesPacket);
        }

        [WorldPacketHandler(ClientOpcodes.QueryPetition)]
        void HandleQueryPetition(QueryPetition packet)
        {
            SendPetitionQuery(packet.ItemGUID);
        }

        public void SendPetitionQuery(ObjectGuid petitionGuid)
        {
            QueryPetitionResponse responsePacket = new();
            responsePacket.PetitionID = (uint)petitionGuid.GetCounter();  // PetitionID (in Trinity always same as GUID_LOPART(petition guid))

            Petition petition = Global.PetitionMgr.GetPetition(petitionGuid);
            if (petition == null)
            {
                responsePacket.Allow = false;
                SendPacket(responsePacket);
                Log.outDebug(LogFilter.Network, $"CMSG_PETITION_Select failed for petition ({petitionGuid})");
                return;
            }

            uint reqSignatures = WorldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

            PetitionInfo petitionInfo = new();
            petitionInfo.PetitionID = (int)petitionGuid.GetCounter();
            petitionInfo.Petitioner = petition.ownerGuid;
            petitionInfo.MinSignatures = reqSignatures;
            petitionInfo.MaxSignatures = reqSignatures;
            petitionInfo.Title = petition.PetitionName;

            responsePacket.Allow = true;
            responsePacket.Info = petitionInfo;

            SendPacket(responsePacket);
        }

        [WorldPacketHandler(ClientOpcodes.PetitionRenameGuild)]
        void HandlePetitionRenameGuild(PetitionRenameGuild packet)
        {
            Item item = GetPlayer().GetItemByGuid(packet.PetitionGuid);
            if (item == null)
                return;

            Petition petition = Global.PetitionMgr.GetPetition(packet.PetitionGuid);
            if (petition == null)
            {
                Log.outDebug(LogFilter.Network, $"CMSG_PETITION_QUERY failed for petition {packet.PetitionGuid}");
                return;
            }

            if (Global.GuildMgr.GetGuildByName(packet.NewGuildName) != null)
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.NewGuildName);
                return;
            }
            if (Global.ObjectMgr.IsReservedName(packet.NewGuildName) || !ObjectManager.IsValidCharterName(packet.NewGuildName))
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.NewGuildName);
                return;
            }

            // update petition storage
            petition.UpdateName(packet.NewGuildName);

            PetitionRenameGuildResponse renameResponse = new();
            renameResponse.PetitionGuid = packet.PetitionGuid;
            renameResponse.NewGuildName = packet.NewGuildName;
            SendPacket(renameResponse);
        }

        [WorldPacketHandler(ClientOpcodes.SignPetition)]
        void HandleSignPetition(SignPetition packet)
        {
            Petition petition = Global.PetitionMgr.GetPetition(packet.PetitionGUID);
            if (petition == null)
            {
                Log.outError(LogFilter.Network, $"Petition {packet.PetitionGUID} is not found for player {GetPlayer().GetGUID()} {GetPlayer().GetName()}");
                return;
            }

            ObjectGuid ownerGuid = petition.ownerGuid;
            int signs = petition.Signatures.Count;

            if (ownerGuid == GetPlayer().GetGUID())
                return;

            // not let enemies sign guild charter
            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && GetPlayer().GetTeam() != Global.CharacterCacheStorage.GetCharacterTeamByGuid(ownerGuid))
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

            if (++signs > 10)                                          // client signs maximum
                return;

            // Client doesn't allow to sign petition two times by one character, but not check sign by another character from same account
            // not allow sign another player from already sign player account

            PetitionSignResults signResult = new();
            signResult.Player = GetPlayer().GetGUID();
            signResult.Item = packet.PetitionGUID;

            bool isSigned = petition.IsPetitionSignedByAccount(GetAccountId());
            if (isSigned)
            {
                signResult.Error = PetitionSigns.AlreadySigned;

                // close at signer side
                SendPacket(signResult);

                // update for owner if online
                Player owner = Global.ObjAccessor.FindConnectedPlayer(ownerGuid);
                if (owner != null)
                    owner.GetSession().SendPacket(signResult);
                return;
            }

            // fill petition store
            petition.AddSignature(GetAccountId(), _player.GetGUID(), false);

            Log.outDebug(LogFilter.Network, "PETITION SIGN: {0} by player: {1} ({2} Account: {3})", packet.PetitionGUID.ToString(), GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetAccountId());

            signResult.Error = PetitionSigns.Ok;
            SendPacket(signResult);

            // update signs count on charter
            Item item = _player.GetItemByGuid(packet.PetitionGUID);
            if (item != null)
            {
                item.SetPetitionNumSignatures((uint)signs);
                item.SetState(ItemUpdateState.Changed, _player);
            }

            // update for owner if online
            Player owner1 = Global.ObjAccessor.FindPlayer(ownerGuid);
            if (owner1 != null)
                owner1.SendPacket(signResult);
        }

        [WorldPacketHandler(ClientOpcodes.DeclinePetition)]
        void HandleDeclinePetition(DeclinePetition packet)
        {
            // Disabled because packet isn't handled by the client in any way
            /*
            Petition petition = sPetitionMgr.GetPetition(packet.PetitionGUID);
            if (petition == null)
                return;

            // petition owner online
            Player owner = Global.ObjAccessor.FindConnectedPlayer(petition.ownerGuid);
            if (owner != null)                                               // petition owner online
            {
                PetitionDeclined packet = new PetitionDeclined();
                packet.Decliner = _player.GetGUID();
                owner.GetSession().SendPacket(packet);
            }
            */
        }

        [WorldPacketHandler(ClientOpcodes.OfferPetition)]
        void HandleOfferPetition(OfferPetition packet)
        {
            Player player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetPlayer);
            if (player == null)
                return;

            Petition petition = Global.PetitionMgr.GetPetition(packet.ItemGUID);
            if (petition == null)
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

            SendPetitionSigns(petition, player);
        }

        [WorldPacketHandler(ClientOpcodes.TurnInPetition)]
        void HandleTurnInPetition(TurnInPetition packet)
        {
            // Check if player really has the required petition charter
            Item item = GetPlayer().GetItemByGuid(packet.Item);
            if (item == null)
                return;

            Petition petition = Global.PetitionMgr.GetPetition(packet.Item);
            if (petition == null)
            {
                Log.outError(LogFilter.Network, "Player {0} ({1}) tried to turn in petition ({2}) that is not present in the database", GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), packet.Item.ToString());
                return;
            }

            string name = petition.PetitionName; // we need a copy, Guild::AddMember invalidates petition

            // Only the petition owner can turn in the petition
            if (GetPlayer().GetGUID() != petition.ownerGuid)
                return;

            TurnInPetitionResult resultPacket = new();

            // Check if player is already in a guild
            if (GetPlayer().GetGuildId() != 0)
            {
                resultPacket.Result = PetitionTurns.AlreadyInGuild;
                SendPacket(resultPacket);
                return;
            }

            // Check if guild name is already taken
            if (Global.GuildMgr.GetGuildByName(name) != null)
            {
                Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, name);
                return;
            }

            var signatures = petition.Signatures; // we need a copy, Guild::AddMember invalidates petition
            uint requiredSignatures = WorldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

            // Notify player if signatures are missing
            if (signatures.Count < requiredSignatures)
            {
                resultPacket.Result = PetitionTurns.NeedMoreSignatures;
                SendPacket(resultPacket);
                return;
            }
            // Proceed with guild/arena team creation

            // Delete charter item
            GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);

            // Create guild
            Guild guild = new();
            if (!guild.Create(GetPlayer(), name))
                return;

            // Register guild and add guild master
            Global.GuildMgr.AddGuild(guild);

            Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.Success, name);

            SQLTransaction trans = new();

            // Add members from signatures
            foreach (var signature in signatures)
                guild.AddMember(trans, signature.PlayerGuid);

            DB.Characters.CommitTransaction(trans);

            Global.PetitionMgr.RemovePetition(packet.Item);

            // created
            Log.outDebug(LogFilter.Network, $"Player {GetPlayer().GetName()} ({GetPlayer().GetGUID()}) turning in petition {packet.Item}");

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
            Creature creature = GetPlayer().GetNPCIfCanInteractWith(guid, NPCFlags.Petitioner, NPCFlags2.None);
            if (creature == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandlePetitionShowListOpcode - {0} not found or you can't interact with him.", guid.ToString());
                return;
            }

            GetPlayer().PlayerTalkClass.GetInteractionData().StartInteraction(guid, PlayerInteractionType.PetitionVendor);

            ServerPetitionShowList packet = new();
            packet.Unit = guid;
            packet.Price = WorldConfig.GetUIntValue(WorldCfg.CharterCostGuild);
            SendPacket(packet);
        }
    }
}
