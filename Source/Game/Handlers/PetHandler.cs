﻿/*
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
using Framework.Database;
using Game.AI;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.DismissCritter)]
        private void HandleDismissCritter(DismissCritter packet)
        {
            Unit pet = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), packet.CritterGUID);
            if (!pet)
            {
                Log.outDebug(LogFilter.Network, "Vanitypet {0} does not exist - player '{1}' ({2} / account: {3}) attempted to dismiss it (possibly lagged out)",
                    packet.CritterGUID.ToString(), GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), GetAccountId());
                return;
            }

            if (GetPlayer().GetCritterGUID() == pet.GetGUID())
            {
                if (pet.IsTypeId(TypeId.Unit) && pet.ToCreature().IsSummon())
                    pet.ToTempSummon().UnSummon();
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestPetInfo)]
        private void HandleRequestPetInfo(RequestPetInfo packet)
        {
        }

        [WorldPacketHandler(ClientOpcodes.PetAction)]
        private void HandlePetAction(PetAction packet)
        {
            var guid1 = packet.PetGUID;         //pet guid
            var guid2 = packet.TargetGUID;      //tag guid

            var spellid = UnitActionBarEntry.UNIT_ACTION_BUTTON_ACTION(packet.Action);
            var flag = (ActiveStates)UnitActionBarEntry.UNIT_ACTION_BUTTON_TYPE(packet.Action);             //delete = 0x07 CastSpell = C1

            // used also for charmed creature
            var pet = Global.ObjAccessor.GetUnit(GetPlayer(), guid1);
            if (!pet)
            {
                Log.outError(LogFilter.Network, "HandlePetAction: {0} doesn't exist for {1}", guid1.ToString(), GetPlayer().GetGUID().ToString());
                return;
            }

            if (pet != GetPlayer().GetFirstControlled())
            {
                Log.outError(LogFilter.Network, "HandlePetAction: {0} does not belong to {1}", guid1.ToString(), GetPlayer().GetGUID().ToString());
                return;
            }

            if (!pet.IsAlive())
            {
                var spell = (flag == ActiveStates.Enabled || flag == ActiveStates.Passive) ? Global.SpellMgr.GetSpellInfo(spellid, pet.GetMap().GetDifficultyID()) : null;
                if (spell == null)
                    return;
                if (!spell.HasAttribute(SpellAttr0.CastableWhileDead))
                    return;
            }

            // @todo allow control charmed player?
            if (pet.IsTypeId(TypeId.Player) && !(flag == ActiveStates.Command && spellid == (uint)CommandStates.Attack))
                return;

            if (GetPlayer().m_Controlled.Count == 1)
                HandlePetActionHelper(pet, guid1, spellid, flag, guid2, packet.ActionPosition.X, packet.ActionPosition.Y, packet.ActionPosition.Z);
            else
            {
                //If a pet is dismissed, m_Controlled will change
                var controlled = new List<Unit>();
                foreach (var unit in GetPlayer().m_Controlled)
                    if (unit.GetEntry() == pet.GetEntry() && unit.IsAlive())
                        controlled.Add(unit);

                foreach (var unit in controlled)
                    HandlePetActionHelper(unit, guid1, spellid, flag, guid2, packet.ActionPosition.X, packet.ActionPosition.Y, packet.ActionPosition.Z);
            }
        }

        [WorldPacketHandler(ClientOpcodes.PetStopAttack)]
        private void HandlePetStopAttack(PetStopAttack packet)
        {
            Unit pet = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), packet.PetGUID);
            if (!pet)
            {
                Log.outError(LogFilter.Network, "HandlePetStopAttack: {0} does not exist", packet.PetGUID.ToString());
                return;
            }

            if (pet != GetPlayer().GetPet() && pet != GetPlayer().GetCharm())
            {
                Log.outError(LogFilter.Network, "HandlePetStopAttack: {0} isn't a pet or charmed creature of player {1}", packet.PetGUID.ToString(), GetPlayer().GetName());
                return;
            }

            if (!pet.IsAlive())
                return;

            pet.AttackStop();
            pet.ClearInPetCombat();
        }

        private void HandlePetActionHelper(Unit pet, ObjectGuid guid1, uint spellid, ActiveStates flag, ObjectGuid guid2, float x, float y, float z)
        {
            var charmInfo = pet.GetCharmInfo();
            if (charmInfo == null)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetAction(petGuid: {0}, tagGuid: {1}, spellId: {2}, flag: {3}): object (GUID: {4} Entry: {5} TypeId: {6}) is considered pet-like but doesn't have a charminfo!",
                    guid1, guid2, spellid, flag, pet.GetGUID().ToString(), pet.GetEntry(), pet.GetTypeId());
                return;
            }

            switch (flag)
            {
                case ActiveStates.Command:                                   //0x07
                    switch ((CommandStates)spellid)
                    {
                        case CommandStates.Stay:                          //flat=1792  //STAY
                            pet.StopMoving();
                            pet.GetMotionMaster().Clear(false);
                            pet.GetMotionMaster().MoveIdle();
                            charmInfo.SetCommandState(CommandStates.Stay);

                            charmInfo.SetIsCommandAttack(false);
                            charmInfo.SetIsAtStay(true);
                            charmInfo.SetIsCommandFollow(false);
                            charmInfo.SetIsFollowing(false);
                            charmInfo.SetIsReturning(false);
                            charmInfo.SaveStayPosition();
                            break;
                        case CommandStates.Follow:                        //spellid=1792  //FOLLOW
                            pet.AttackStop();
                            pet.InterruptNonMeleeSpells(false);
                            pet.ClearInPetCombat();
                            pet.GetMotionMaster().MoveFollow(GetPlayer(), SharedConst.PetFollowDist, pet.GetFollowAngle());
                            charmInfo.SetCommandState(CommandStates.Follow);

                            charmInfo.SetIsCommandAttack(false);
                            charmInfo.SetIsAtStay(false);
                            charmInfo.SetIsReturning(true);
                            charmInfo.SetIsCommandFollow(true);
                            charmInfo.SetIsFollowing(false);
                            break;
                        case CommandStates.Attack:                        //spellid=1792  //ATTACK
                            {
                                // Can't attack if owner is pacified
                                if (GetPlayer().HasAuraType(AuraType.ModPacify))
                                {
                                    // @todo Send proper error message to client
                                    return;
                                }

                                // only place where pet can be player
                                var TargetUnit = Global.ObjAccessor.GetUnit(GetPlayer(), guid2);
                                if (!TargetUnit)
                                    return;

                                var owner = pet.GetOwner();
                                if (owner)
                                    if (!owner.IsValidAttackTarget(TargetUnit))
                                        return;

                                pet.ClearUnitState(UnitState.Follow);
                                // This is true if pet has no target or has target but targets differs.
                                if (pet.GetVictim() != TargetUnit || !pet.GetCharmInfo().IsCommandAttack())
                                {
                                    if (pet.GetVictim())
                                        pet.AttackStop();

                                    if (!pet.IsTypeId(TypeId.Player) && pet.ToCreature().IsAIEnabled)
                                    {
                                        charmInfo.SetIsCommandAttack(true);
                                        charmInfo.SetIsAtStay(false);
                                        charmInfo.SetIsFollowing(false);
                                        charmInfo.SetIsCommandFollow(false);
                                        charmInfo.SetIsReturning(false);

                                        var AI = pet.ToCreature().GetAI();
                                        if (AI is PetAI)
                                            ((PetAI)AI)._AttackStart(TargetUnit); // force target switch
                                        else
                                            AI.AttackStart(TargetUnit);

                                        //10% chance to play special pet attack talk, else growl
                                        if (pet.IsPet() && pet.ToPet().GetPetType() == PetType.Summon && pet != TargetUnit && RandomHelper.IRand(0, 100) < 10)
                                            pet.SendPetTalk(PetTalk.Attack);
                                        else
                                        {
                                            // 90% chance for pet and 100% chance for charmed creature
                                            pet.SendPetAIReaction(guid1);
                                        }
                                    }
                                    else                                // charmed player
                                    {
                                        charmInfo.SetIsCommandAttack(true);
                                        charmInfo.SetIsAtStay(false);
                                        charmInfo.SetIsFollowing(false);
                                        charmInfo.SetIsCommandFollow(false);
                                        charmInfo.SetIsReturning(false);

                                        pet.Attack(TargetUnit, true);
                                        pet.SendPetAIReaction(guid1);
                                    }
                                }
                                break;
                            }
                        case CommandStates.Abandon:                       // abandon (hunter pet) or dismiss (summoned pet)
                            if (pet.GetCharmerGUID() == GetPlayer().GetGUID())
                                GetPlayer().StopCastingCharm();
                            else if (pet.GetOwnerGUID() == GetPlayer().GetGUID())
                            {
                                Cypher.Assert(pet.IsTypeId(TypeId.Unit));
                                if (pet.IsPet())
                                {
                                    if (pet.ToPet().GetPetType() == PetType.Hunter)
                                        GetPlayer().RemovePet(pet.ToPet(), PetSaveMode.AsDeleted);
                                    else
                                        //dismissing a summoned pet is like killing them (this prevents returning a soulshard...)
                                        pet.SetDeathState(DeathState.Corpse);
                                }
                                else if (pet.HasUnitTypeMask(UnitTypeMask.Minion))
                                {
                                    ((Minion)pet).UnSummon();
                                }
                            }
                            break;
                        case CommandStates.MoveTo:
                            pet.StopMoving();
                            pet.GetMotionMaster().Clear(false);
                            pet.GetMotionMaster().MovePoint(0, x, y, z);
                            charmInfo.SetCommandState(CommandStates.MoveTo);

                            charmInfo.SetIsCommandAttack(false);
                            charmInfo.SetIsAtStay(true);
                            charmInfo.SetIsFollowing(false);
                            charmInfo.SetIsReturning(false);
                            charmInfo.SaveStayPosition();
                            break;
                        default:
                            Log.outError(LogFilter.Network, "WORLD: unknown PET flag Action {0} and spellid {1}.", flag, spellid);
                            break;
                    }
                    break;
                case ActiveStates.Reaction:                                  // 0x6
                    switch ((ReactStates)spellid)
                    {
                        case ReactStates.Passive:                         //passive
                            pet.AttackStop();
                            pet.ClearInPetCombat();
                            goto case ReactStates.Defensive;
                        case ReactStates.Defensive:                       //recovery
                        case ReactStates.Aggressive:                      //activete
                            if (pet.IsTypeId(TypeId.Unit))
                                pet.ToCreature().SetReactState((ReactStates)spellid);
                            break;
                    }
                    break;
                case ActiveStates.Disabled:                                  // 0x81    spell (disabled), ignore
                case ActiveStates.Passive:                                   // 0x01
                case ActiveStates.Enabled:                                   // 0xC1    spell
                    {
                        Unit unit_target = null;

                        if (!guid2.IsEmpty())
                            unit_target = Global.ObjAccessor.GetUnit(GetPlayer(), guid2);

                        // do not cast unknown spells
                        var spellInfo = Global.SpellMgr.GetSpellInfo(spellid, pet.GetMap().GetDifficultyID());
                        if (spellInfo == null)
                        {
                            Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", spellid);
                            return;
                        }

                        foreach (var effect in spellInfo.GetEffects())
                        {
                            if (effect != null && (effect.TargetA.GetTarget() == Targets.UnitSrcAreaEnemy || effect.TargetA.GetTarget() == Targets.UnitDestAreaEnemy || effect.TargetA.GetTarget() == Targets.DestDynobjEnemy))
                                return;
                        }

                        // do not cast not learned spells
                        if (!pet.HasSpell(spellid) || spellInfo.IsPassive())
                            return;

                        //  Clear the flags as if owner clicked 'attack'. AI will reset them
                        //  after AttackStart, even if spell failed
                        if (pet.GetCharmInfo() != null)
                        {
                            pet.GetCharmInfo().SetIsAtStay(false);
                            pet.GetCharmInfo().SetIsCommandAttack(true);
                            pet.GetCharmInfo().SetIsReturning(false);
                            pet.GetCharmInfo().SetIsFollowing(false);
                        }

                        var spell = new Spell(pet, spellInfo, TriggerCastFlags.None);

                        var result = spell.CheckPetCast(unit_target);

                        //auto turn to target unless possessed
                        if (result == SpellCastResult.UnitNotInfront && !pet.IsPossessed() && !pet.IsVehicle())
                        {
                            var unit_target2 = spell.m_targets.GetUnitTarget();
                            if (unit_target)
                            {
                                if (!pet.IsFocusing())
                                    pet.SetInFront(unit_target);
                                var player = unit_target.ToPlayer();
                                if (player)
                                    pet.SendUpdateToPlayer(player);
                            }
                            else if (unit_target2)
                            {
                                if (!pet.IsFocusing())
                                    pet.SetInFront(unit_target2);
                                var player = unit_target2.ToPlayer();
                                if (player)
                                    pet.SendUpdateToPlayer(player);
                            }
                            var powner = pet.GetCharmerOrOwner();
                            if (powner)
                            {
                                var player = powner.ToPlayer();
                                if (player)
                                    pet.SendUpdateToPlayer(player);
                            }

                            result = SpellCastResult.SpellCastOk;
                        }

                        if (result == SpellCastResult.SpellCastOk)
                        {
                            unit_target = spell.m_targets.GetUnitTarget();

                            //10% chance to play special pet attack talk, else growl
                            //actually this only seems to happen on special spells, fire shield for imp, torment for voidwalker, but it's stupid to check every spell
                            if (pet.IsPet() && (pet.ToPet().GetPetType() == PetType.Summon) && (pet != unit_target) && (RandomHelper.IRand(0, 100) < 10))
                                pet.SendPetTalk(PetTalk.SpecialSpell);
                            else
                            {
                                pet.SendPetAIReaction(guid1);
                            }

                            if (unit_target && !GetPlayer().IsFriendlyTo(unit_target) && !pet.IsPossessed() && !pet.IsVehicle())
                            {
                                // This is true if pet has no target or has target but targets differs.
                                if (pet.GetVictim() != unit_target)
                                {
                                    pet.GetMotionMaster().Clear();
                                    if (pet.ToCreature().IsAIEnabled)
                                    {
                                        var AI = pet.ToCreature().GetAI();
                                        var petAI = (PetAI)AI;
                                        if (petAI != null)
                                            petAI._AttackStart(unit_target); // force victim switch
                                        else
                                            AI.AttackStart(unit_target);
                                    }
                                }
                            }

                            spell.Prepare(spell.m_targets);
                        }
                        else
                        {
                            if (pet.IsPossessed() || pet.IsVehicle()) // @todo: confirm this check
                                Spell.SendCastResult(GetPlayer(), spellInfo, spell.m_SpellVisual, spell.m_castId, result);
                            else
                                spell.SendPetCastResult(result);

                            if (!pet.GetSpellHistory().HasCooldown(spellid))
                                pet.GetSpellHistory().ResetCooldown(spellid, true);

                            spell.Finish(false);
                            spell.Dispose();

                            // reset specific flags in case of spell fail. AI will reset other flags
                            if (pet.GetCharmInfo() != null)
                                pet.GetCharmInfo().SetIsCommandAttack(false);
                        }
                        break;
                    }
                default:
                    Log.outError(LogFilter.Network, "WORLD: unknown PET flag Action {0} and spellid {1}.", flag, spellid);
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.QueryPetName)]
        private void HandleQueryPetName(QueryPetName packet)
        {
            SendQueryPetNameResponse(packet.UnitGUID);
        }

        private void SendQueryPetNameResponse(ObjectGuid guid)
        {
            var response = new QueryPetNameResponse();
            response.UnitGUID = guid;

            Creature unit = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), guid);
            if (unit)
            {
                response.Allow = true;
                response.Timestamp = unit.m_unitData.PetNameTimestamp;
                response.Name = unit.GetName();

                var pet = unit.ToPet();
                if (pet)
                {
                    DeclinedName names = pet.GetDeclinedNames();
                    if (names != null)
                    {
                        response.HasDeclined = true;
                        response.DeclinedNames = names;
                    }
                }
            }

            GetPlayer().SendPacket(response);
        }

        private bool CheckStableMaster(ObjectGuid guid)
        {
            // spell case or GM
            if (guid == GetPlayer().GetGUID())
            {
                if (!GetPlayer().IsGameMaster() && !GetPlayer().HasAuraType(AuraType.OpenStable))
                {
                    Log.outDebug(LogFilter.Network, "{0} attempt open stable in cheating way.", guid.ToString());
                    return false;
                }
            }
            // stable master case
            else
            {
                if (!GetPlayer().GetNPCIfCanInteractWith(guid, NPCFlags.StableMaster, NPCFlags2.None))
                {
                    Log.outDebug(LogFilter.Network, "Stablemaster {0} not found or you can't interact with him.", guid.ToString());
                    return false;
                }
            }
            return true;
        }

        [WorldPacketHandler(ClientOpcodes.PetSetAction)]
        private void HandlePetSetAction(PetSetAction packet)
        {
            var petguid = packet.PetGUID;
            var pet = Global.ObjAccessor.GetUnit(GetPlayer(), petguid);
            if (!pet || pet != GetPlayer().GetFirstControlled())
            {
                Log.outError(LogFilter.Network, "HandlePetSetAction: Unknown {0} or pet owner {1}", petguid.ToString(), GetPlayer().GetGUID().ToString());
                return;
            }

            var charmInfo = pet.GetCharmInfo();
            if (charmInfo == null)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetSetAction: {0} is considered pet-like but doesn't have a charminfo!", pet.GetGUID().ToString());
                return;
            }

            var position = packet.Index;
            var actionData = packet.Action;

            var spell_id = UnitActionBarEntry.UNIT_ACTION_BUTTON_ACTION(actionData);
            var act_state = (ActiveStates)UnitActionBarEntry.UNIT_ACTION_BUTTON_TYPE(actionData);

            Log.outDebug(LogFilter.Network, "Player {0} has changed pet spell action. Position: {1}, Spell: {2}, State: {3}", GetPlayer().GetName(), position, spell_id, act_state);


            //if it's act for spell (en/disable/cast) and there is a spell given (0 = remove spell) which pet doesn't know, don't add
            if (!((act_state == ActiveStates.Enabled || act_state == ActiveStates.Disabled || act_state == ActiveStates.Passive) && spell_id != 0 && !pet.HasSpell(spell_id)))
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, pet.GetMap().GetDifficultyID());
                if (spellInfo != null)
                {
                    //sign for autocast
                    if (act_state == ActiveStates.Enabled)
                    {
                        if (pet.GetTypeId() == TypeId.Unit && pet.IsPet())
                            ((Pet)pet).ToggleAutocast(spellInfo, true);
                        else
                        {
                            foreach (var unit in GetPlayer().m_Controlled)
                                if (unit.GetEntry() == pet.GetEntry())
                                    unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, true);
                        }
                    }
                    //sign for no/turn off autocast
                    else if (act_state == ActiveStates.Disabled)
                    {
                        if (pet.GetTypeId() == TypeId.Unit && pet.IsPet())
                            pet.ToPet().ToggleAutocast(spellInfo, false);
                        else
                        {
                            foreach (var unit in GetPlayer().m_Controlled)
                                if (unit.GetEntry() == pet.GetEntry())
                                    unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, false);
                        }
                    }
                }

                charmInfo.SetActionBar((byte)position, spell_id, act_state);
            }
        }

        [WorldPacketHandler(ClientOpcodes.PetRename)]
        private void HandlePetRename(PetRename packet)
        {
            var petguid = packet.RenameData.PetGUID;
            var isdeclined = packet.RenameData.HasDeclinedNames;
            var name = packet.RenameData.NewName;

            var pet = ObjectAccessor.GetPet(GetPlayer(), petguid);
            // check it!
            if (!pet || !pet.IsPet() || pet.ToPet().GetPetType() != PetType.Hunter || !pet.HasPetFlag(UnitPetFlags.CanBeRenamed) ||
                pet.GetOwnerGUID() != GetPlayer().GetGUID() || pet.GetCharmInfo() == null)
                return;

            PetNameInvalidReason res = ObjectManager.CheckPetName(name);
            if (res != PetNameInvalidReason.Success)
            {
                SendPetNameInvalid(res, name, null);
                return;
            }

            if (Global.ObjectMgr.IsReservedName(name))
            {
                SendPetNameInvalid(PetNameInvalidReason.Reserved, name, null);
                return;
            }

            pet.SetName(name);
            pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Name);
            pet.RemovePetFlag(UnitPetFlags.CanBeRenamed);

            PreparedStatement stmt;
            var trans = new SQLTransaction();
            if (isdeclined)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
                stmt.AddValue(0, pet.GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_PET_DECLINEDNAME);
                stmt.AddValue(0, pet.GetCharmInfo().GetPetNumber());
                stmt.AddValue(1, GetPlayer().GetGUID().ToString());

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    stmt.AddValue(i + 1, packet.RenameData.DeclinedNames.name[i]);

                trans.Append(stmt);
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_NAME);
            stmt.AddValue(0, name);
            stmt.AddValue(1, GetPlayer().GetGUID().ToString());
            stmt.AddValue(2, pet.GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            pet.SetPetNameTimestamp((uint)Time.UnixTime); // cast can't be helped
        }

        [WorldPacketHandler(ClientOpcodes.PetAbandon)]
        private void HandlePetAbandon(PetAbandon packet)
        {
            if (!GetPlayer().IsInWorld)
                return;

            // pet/charmed
            Creature pet = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), packet.Pet);
            if (pet && pet.ToPet() && pet.ToPet().GetPetType() == PetType.Hunter)
            {
                _player.RemovePet((Pet)pet, PetSaveMode.AsDeleted);
            }
        }

        [WorldPacketHandler(ClientOpcodes.PetSpellAutocast)]
        private void HandlePetSpellAutocast(PetSpellAutocast packet)
        {
            Creature pet = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), packet.PetGUID);
            if (!pet)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocast: {0} not found.", packet.PetGUID.ToString());
                return;
            }

            if (pet != GetPlayer().GetGuardianPet() && pet != GetPlayer().GetCharm())
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocast: {0} isn't pet of player {1} ({2}).",
                    packet.PetGUID.ToString(), GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            var spellInfo = Global.SpellMgr.GetSpellInfo(packet.SpellID, pet.GetMap().GetDifficultyID());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocast: Unknown spell id {0} used by {1}.", packet.SpellID, packet.PetGUID.ToString());
                return;
            }

            // do not add not learned spells/ passive spells
            if (!pet.HasSpell(packet.SpellID) || !spellInfo.IsAutocastable())
                return;

            var charmInfo = pet.GetCharmInfo();
            if (charmInfo == null)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocastOpcod: object {0} is considered pet-like but doesn't have a charminfo!", pet.GetGUID().ToString());
                return;
            }

            if (pet.IsPet())
                pet.ToPet().ToggleAutocast(spellInfo, packet.AutocastEnabled);
            else
                charmInfo.ToggleCreatureAutocast(spellInfo, packet.AutocastEnabled);

            charmInfo.SetSpellAutocast(spellInfo, packet.AutocastEnabled);
        }

        [WorldPacketHandler(ClientOpcodes.PetCastSpell)]
        private void HandlePetCastSpell(PetCastSpell petCastSpell)
        {
            var caster = Global.ObjAccessor.GetUnit(GetPlayer(), petCastSpell.PetGUID);
            if (!caster)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetCastSpell: Caster {0} not found.", petCastSpell.PetGUID.ToString());
                return;
            }

            var spellInfo = Global.SpellMgr.GetSpellInfo(petCastSpell.Cast.SpellID, caster.GetMap().GetDifficultyID());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetCastSpell: unknown spell id {0} tried to cast by {1}", petCastSpell.Cast.SpellID, petCastSpell.PetGUID.ToString());
                return;
            }

            // This opcode is also sent from charmed and possessed units (players and creatures)
            if (caster != GetPlayer().GetGuardianPet() && caster != GetPlayer().GetCharm())
            {
                Log.outError(LogFilter.Network, "WorldSession.HandlePetCastSpell: {0} isn't pet of player {1} ({2}).", petCastSpell.PetGUID.ToString(), GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            // do not cast not learned spells
            if (!caster.HasSpell(spellInfo.Id) || spellInfo.IsPassive())
                return;

            var targets = new SpellCastTargets(caster, petCastSpell.Cast);
            caster.ClearUnitState(UnitState.Follow);

            var spell = new Spell(caster, spellInfo, TriggerCastFlags.None);
            spell.m_fromClient = true;
            spell.m_misc.Data0 = petCastSpell.Cast.Misc[0];
            spell.m_misc.Data1 = petCastSpell.Cast.Misc[1];
            spell.m_targets = targets;

            var result = spell.CheckPetCast(null);

            if (result == SpellCastResult.SpellCastOk)
            {
                var creature = caster.ToCreature();
                if (creature)
                {
                    var pet = creature.ToPet();
                    if (pet)
                    {
                        // 10% chance to play special pet attack talk, else growl
                        // actually this only seems to happen on special spells, fire shield for imp, torment for voidwalker, but it's stupid to check every spell
                        if (pet.GetPetType() == PetType.Summon && (RandomHelper.IRand(0, 100) < 10))
                            pet.SendPetTalk(PetTalk.SpecialSpell);
                        else
                            pet.SendPetAIReaction(petCastSpell.PetGUID);
                    }
                }

                var spellPrepare = new SpellPrepare();
                spellPrepare.ClientCastID = petCastSpell.Cast.CastID;
                spellPrepare.ServerCastID = spell.m_castId;
                SendPacket(spellPrepare);

                spell.Prepare(targets);
            }
            else
            {
                spell.SendPetCastResult(result);

                if (!caster.GetSpellHistory().HasCooldown(spellInfo.Id))
                    caster.GetSpellHistory().ResetCooldown(spellInfo.Id, true);

                spell.Finish(false);
                spell.Dispose();
            }
        }

        private void SendPetNameInvalid(PetNameInvalidReason error, string name, DeclinedName declinedName)
        {
            var petNameInvalid = new PetNameInvalid();
            petNameInvalid.Result = error;
            petNameInvalid.RenameData.NewName = name;
            for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                petNameInvalid.RenameData.DeclinedNames.name[i] = declinedName.name[i];

            SendPacket(petNameInvalid);
        }
    }
}
