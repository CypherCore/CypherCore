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
using Game.AI;
using Game.Network.Packets;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit
    {
        public void AddPetAura(PetAura petSpell)
        {
            if (!IsTypeId(TypeId.Player))
                return;

            m_petAuras.Add(petSpell);
            Pet pet = ToPlayer().GetPet();
            if (pet)
                pet.CastPetAura(petSpell);
        }

        public void RemovePetAura(PetAura petSpell)
        {
            if (!IsTypeId(TypeId.Player))
                return;

            m_petAuras.Remove(petSpell);
            Pet pet = ToPlayer().GetPet();
            if (pet)
                pet.RemoveAurasDueToSpell(petSpell.GetAura(pet.GetEntry()));
        }

        public CharmInfo GetCharmInfo() { return m_charmInfo; }

        public CharmInfo InitCharmInfo()
        {
            if (m_charmInfo == null)
                m_charmInfo = new CharmInfo(this);

            return m_charmInfo;
        }

        void DeleteCharmInfo()
        {
            if (m_charmInfo == null)
                return;

            m_charmInfo.RestoreState();
            m_charmInfo = null;
        }

        public void UpdateCharmAI()
        {
            switch (GetTypeId())
            {
                case TypeId.Unit:
                    if (i_disabledAI != null) // disabled AI must be primary AI
                    {
                        if (!IsCharmed())
                        {
                            i_AI = i_disabledAI;
                            i_disabledAI = null;

                            if (IsTypeId(TypeId.Unit))
                                ToCreature().GetAI().OnCharmed(false);
                        }
                    }
                    else
                    {
                        if (IsCharmed())
                        {
                            i_disabledAI = i_AI;
                            if (isPossessed() || IsVehicle())
                                i_AI = new PossessedAI(ToCreature());
                            else
                                i_AI = new PetAI(ToCreature());
                        }
                    }
                    break;
                case TypeId.Player:
                    {
                        if (IsCharmed()) // if we are currently being charmed, then we should apply charm AI
                        {
                            i_disabledAI = i_AI;

                            UnitAI newAI = null;
                            // first, we check if the creature's own AI specifies an override playerai for its owned players
                            Unit charmer = GetCharmer();
                            if (charmer)
                            {
                                Creature creatureCharmer = charmer.ToCreature();
                                if (creatureCharmer)
                                {
                                    PlayerAI charmAI = creatureCharmer.IsAIEnabled ? creatureCharmer.GetAI().GetAIForCharmedPlayer(ToPlayer()) : null;
                                    if (charmAI != null)
                                        newAI = charmAI;
                                }
                                else
                                {
                                    Log.outError(LogFilter.Misc, "Attempt to assign charm AI to player {0} who is charmed by non-creature {1}.", GetGUID().ToString(), GetCharmerGUID().ToString());
                                }
                            }
                            if (newAI == null) // otherwise, we default to the generic one
                                newAI = new SimpleCharmedPlayerAI(ToPlayer());
                            i_AI = newAI;
                            newAI.OnCharmed(true);
                        }
                        else
                        {
                            if (i_AI != null)
                            {
                                // we allow the charmed PlayerAI to clean up
                                i_AI.OnCharmed(false);
                            }
                            else
                            {
                                Log.outError(LogFilter.Misc, "Attempt to remove charm AI from player {0} who doesn't currently have charm AI.", GetGUID().ToString());
                            }
                            // and restore our previous PlayerAI (if we had one)
                            i_AI = i_disabledAI;
                            i_disabledAI = null;
                            // IsAIEnabled gets handled in the caller
                        }
                        break;
                    }
                default:
                    Log.outError(LogFilter.Misc, "Attempt to update charm AI for unit {0}, which is neither player nor creature.", GetGUID().ToString());
                    break;
            }

        }

        public void SetMinion(Minion minion, bool apply)
        {
            Log.outDebug(LogFilter.Unit, "SetMinion {0} for {1}, apply {2}", minion.GetEntry(), GetEntry(), apply);

            if (apply)
            {
                if (!minion.GetOwnerGUID().IsEmpty())
                {
                    Log.outFatal(LogFilter.Unit, "SetMinion: Minion {0} is not the minion of owner {1}", minion.GetEntry(), GetEntry());
                    return;
                }

                minion.SetOwnerGUID(GetGUID());

                if (!m_Controlled.Contains(minion))
                    m_Controlled.Add(minion);

                if (IsTypeId(TypeId.Player))
                {
                    minion.m_ControlledByPlayer = true;
                    minion.SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
                }

                // Can only have one pet. If a new one is summoned, dismiss the old one.
                if (minion.IsGuardianPet())
                {
                    Guardian oldPet = GetGuardianPet();
                    if (oldPet)
                    {
                        if (oldPet != minion && (oldPet.IsPet() || minion.IsPet() || oldPet.GetEntry() != minion.GetEntry()))
                        {
                            // remove existing minion pet
                            if (oldPet.IsPet())
                                ((Pet)oldPet).Remove(PetSaveMode.AsCurrent);
                            else
                                oldPet.UnSummon();
                            SetPetGUID(minion.GetGUID());
                            SetMinionGUID(ObjectGuid.Empty);
                        }
                    }
                    else
                    {
                        SetPetGUID(minion.GetGUID());
                        SetMinionGUID(ObjectGuid.Empty);
                    }
                }

                if (minion.HasUnitTypeMask(UnitTypeMask.Guardian))
                    AddGuidValue(UnitFields.Summon, minion.GetGUID());

                if (minion.m_Properties != null && minion.m_Properties.Title == SummonType.Minipet)
                {
                    SetCritterGUID(minion.GetGUID());
                    if (GetTypeId() == TypeId.Player)
                        minion.SetGuidValue(UnitFields.BattlePetCompanionGuid, GetGuidValue(ActivePlayerFields.SummonedBattlePetId));
                }

                // PvP, FFAPvP
                minion.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag));

                // FIXME: hack, speed must be set only at follow
                if (IsTypeId(TypeId.Player) && minion.IsPet())
                    for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                        minion.SetSpeedRate(i, m_speed_rate[(int)i]);

                // Send infinity cooldown - client does that automatically but after relog cooldown needs to be set again
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(minion.GetUInt32Value(UnitFields.CreatedBySpell));
                if (spellInfo != null && spellInfo.IsCooldownStartedOnEvent())
                    GetSpellHistory().StartCooldown(spellInfo, 0, null, true);
            }
            else
            {
                if (minion.GetOwnerGUID() != GetGUID())
                {
                    Log.outFatal(LogFilter.Unit, "SetMinion: Minion {0} is not the minion of owner {1}", minion.GetEntry(), GetEntry());
                    return;
                }

                m_Controlled.Remove(minion);

                if (minion.m_Properties != null && minion.m_Properties.Title == SummonType.Minipet)
                {
                    if (GetCritterGUID() == minion.GetGUID())
                        SetCritterGUID(ObjectGuid.Empty);
                }

                if (minion.IsGuardianPet())
                {
                    if (GetPetGUID() == minion.GetGUID())
                        SetPetGUID(ObjectGuid.Empty);
                }
                else if (minion.IsTotem())
                {
                    // All summoned by totem minions must disappear when it is removed.
                    SpellInfo spInfo = Global.SpellMgr.GetSpellInfo(minion.ToTotem().GetSpell());
                    if (spInfo != null)
                    {
                        foreach (SpellEffectInfo effect in spInfo.GetEffectsForDifficulty(Difficulty.None))
                        {
                            if (effect == null || effect.Effect != SpellEffectName.Summon)
                                continue;

                            RemoveAllMinionsByEntry((uint)effect.MiscValue);
                        }
                    }
                }

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(minion.GetUInt32Value(UnitFields.CreatedBySpell));
                // Remove infinity cooldown
                if (spellInfo != null && spellInfo.IsCooldownStartedOnEvent())
                    GetSpellHistory().SendCooldownEvent(spellInfo);

                if (RemoveGuidValue(UnitFields.Summon, minion.GetGUID()))
                {
                    // Check if there is another minion
                    foreach (var unit in m_Controlled)
                    {
                        // do not use this check, creature do not have charm guid
                        if (GetGUID() == unit.GetCharmerGUID())
                            continue;

                        Cypher.Assert(unit.GetOwnerGUID() == GetGUID());
                        if (unit.GetOwnerGUID() != GetGUID())
                        {
                            Cypher.Assert(false);
                        }
                        Cypher.Assert(unit.IsTypeId(TypeId.Unit));

                        if (!unit.HasUnitTypeMask(UnitTypeMask.Guardian))
                            continue;

                        if (AddGuidValue(UnitFields.Summon, unit.GetGUID()))
                        {
                            // show another pet bar if there is no charm bar
                            if (IsTypeId(TypeId.Player) && GetCharmGUID().IsEmpty())
                            {
                                if (unit.IsPet())
                                    ToPlayer().PetSpellInitialize();
                                else
                                    ToPlayer().CharmSpellInitialize();
                            }
                        }
                        break;
                    }
                }
            }
        }

        public bool SetCharmedBy(Unit charmer, CharmType type, AuraApplication aurApp = null)
        {
            if (!charmer)
                return false;

            // dismount players when charmed
            if (IsTypeId(TypeId.Player))
                RemoveAurasByType(AuraType.Mounted);

            if (charmer.IsTypeId(TypeId.Player))
                charmer.RemoveAurasByType(AuraType.Mounted);

            Cypher.Assert(type != CharmType.Possess || charmer.IsTypeId(TypeId.Player));
            Cypher.Assert((type == CharmType.Vehicle) == IsVehicle());

            Log.outDebug(LogFilter.Unit, "SetCharmedBy: charmer {0} (GUID {1}), charmed {2} (GUID {3}), type {4}.", charmer.GetEntry(), charmer.GetGUID().ToString(), GetEntry(), GetGUID().ToString(), type);

            if (this == charmer)
            {
                Log.outFatal(LogFilter.Unit, "Unit:SetCharmedBy: Unit {0} (GUID {1}) is trying to charm itself!", GetEntry(), GetGUID().ToString());
                return false;
            }

            if (IsTypeId(TypeId.Player) && ToPlayer().GetTransport())
            {
                Log.outFatal(LogFilter.Unit, "Unit:SetCharmedBy: Player on transport is trying to charm {0} (GUID {1})", GetEntry(), GetGUID().ToString());
                return false;
            }

            // Already charmed
            if (!GetCharmerGUID().IsEmpty())
            {
                Log.outFatal(LogFilter.Unit, "Unit:SetCharmedBy: {0} (GUID {1}) has already been charmed but {2} (GUID {3}) is trying to charm it!", GetEntry(), GetGUID().ToString(), charmer.GetEntry(), charmer.GetGUID().ToString());
                return false;
            }

            CastStop();
            CombatStop(); // @todo CombatStop(true) may cause crash (interrupt spells)
            DeleteThreatList();

            Player playerCharmer = charmer.ToPlayer();

            // Charmer stop charming
            if (playerCharmer)
            {
                playerCharmer.StopCastingCharm();
                playerCharmer.StopCastingBindSight();
            }

            // Charmed stop charming
            if (IsTypeId(TypeId.Player))
            {
                ToPlayer().StopCastingCharm();
                ToPlayer().StopCastingBindSight();
            }

            // StopCastingCharm may remove a possessed pet?
            if (!IsInWorld)
            {
                Log.outFatal(LogFilter.Unit, "Unit:SetCharmedBy: {0} (GUID {1}) is not in world but {2} (GUID {3}) is trying to charm it!", GetEntry(), GetGUID().ToString(), charmer.GetEntry(), charmer.GetGUID().ToString());
                return false;
            }

            // charm is set by aura, and aura effect remove handler was called during apply handler execution
            // prevent undefined behaviour
            if (aurApp != null && aurApp.GetRemoveMode() != 0)
                return false;

            _oldFactionId = getFaction();
            SetFaction(charmer.getFaction());

            // Set charmed
            charmer.SetCharm(this, true);

            if (IsTypeId(TypeId.Unit))
            {
                ToCreature().GetAI().OnCharmed(true);
                GetMotionMaster().MoveIdle();
            }
            else
            {
                Player player = ToPlayer();
                if (player)
                {
                    if (player.isAFK())
                        player.ToggleAFK();

                    if (charmer.IsTypeId(TypeId.Unit)) // we are charmed by a creature
                    {
                        // change AI to charmed AI on next Update tick
                        NeedChangeAI = true;
                        if (IsAIEnabled)
                        {
                            IsAIEnabled = false;
                            player.GetAI().OnCharmed(true);
                        }
                    }

                    player.SetClientControl(this, false);
                }
            }

            // charm is set by aura, and aura effect remove handler was called during apply handler execution
            // prevent undefined behaviour
            if (aurApp != null && aurApp.GetRemoveMode() != 0)
                return false;

            // Pets already have a properly initialized CharmInfo, don't overwrite it.
            if (type != CharmType.Vehicle && GetCharmInfo() == null)
            {
                InitCharmInfo();
                if (type == CharmType.Possess)
                    GetCharmInfo().InitPossessCreateSpells();
                else
                    GetCharmInfo().InitCharmCreateSpells();
            }

            if (playerCharmer)
            {
                switch (type)
                {
                    case CharmType.Vehicle:
                        SetFlag(UnitFields.Flags, UnitFlags.PlayerControlled);
                        playerCharmer.SetClientControl(this, true);
                        playerCharmer.VehicleSpellInitialize();
                        break;
                    case CharmType.Possess:
                        AddUnitState(UnitState.Possessed);
                        SetFlag(UnitFields.Flags, UnitFlags.PlayerControlled);
                        charmer.SetFlag(UnitFields.Flags, UnitFlags.RemoveClientControl);
                        playerCharmer.SetClientControl(this, true);
                        playerCharmer.PossessSpellInitialize();
                        break;
                    case CharmType.Charm:
                        if (IsTypeId(TypeId.Unit) && charmer.GetClass() == Class.Warlock)
                        {
                            CreatureTemplate cinfo = ToCreature().GetCreatureTemplate();
                            if (cinfo != null && cinfo.CreatureType == CreatureType.Demon)
                            {
                                // to prevent client crash
                                SetByteValue(UnitFields.Bytes0, 1, (byte)Class.Mage);

                                // just to enable stat window
                                if (GetCharmInfo() != null)
                                    GetCharmInfo().SetPetNumber(Global.ObjectMgr.GeneratePetNumber(), true);

                                // if charmed two demons the same session, the 2nd gets the 1st one's name
                                SetUInt32Value(UnitFields.PetNameTimestamp, (uint)Time.UnixTime); // cast can't be helped
                            }
                        }
                        playerCharmer.CharmSpellInitialize();
                        break;
                    default:
                    case CharmType.Convert:
                        break;
                }
            }
            return true;
        }

        public void RemoveCharmedBy(Unit charmer)
        {
            if (!IsCharmed())
                return;

            if (!charmer)
                charmer = GetCharmer();
            if (charmer != GetCharmer()) // one aura overrides another?
                return;

            CharmType type;
            if (HasUnitState(UnitState.Possessed))
                type = CharmType.Possess;
            else if (charmer && charmer.IsOnVehicle(this))
                type = CharmType.Vehicle;
            else
                type = CharmType.Charm;

            CastStop();
            CombatStop(); // @todo CombatStop(true) may cause crash (interrupt spells)
            getHostileRefManager().deleteReferences();
            DeleteThreatList();

            if (_oldFactionId != 0)
            {
                SetFaction(_oldFactionId);
                _oldFactionId = 0;
            }
            else
                RestoreFaction();

            GetMotionMaster().InitDefault();

            Creature creature = ToCreature();
            if (creature)
            {
                // Creature will restore its old AI on next update
                if (creature.GetAI() != null)
                    creature.GetAI().OnCharmed(false);

                // Vehicle should not attack its passenger after he exists the seat
                if (type != CharmType.Vehicle)
                    LastCharmerGUID = charmer ? charmer.GetGUID() : ObjectGuid.Empty;
            }

            // If charmer still exists
            if (!charmer)
                return;

            Cypher.Assert(type != CharmType.Possess || charmer.IsTypeId(TypeId.Player));
            Cypher.Assert(type != CharmType.Vehicle || (IsTypeId(TypeId.Unit) && IsVehicle()));

            charmer.SetCharm(this, false);

            Player playerCharmer = charmer.ToPlayer();
            if (playerCharmer)
            {
                switch (type)
                {
                    case CharmType.Vehicle:
                        playerCharmer.SetClientControl(this, false);
                        playerCharmer.SetClientControl(charmer, true);
                        RemoveFlag(UnitFields.Flags, UnitFlags.PlayerControlled);
                        break;
                    case CharmType.Possess:
                        playerCharmer.SetClientControl(this, false);
                        playerCharmer.SetClientControl(charmer, true);
                        charmer.RemoveFlag(UnitFields.Flags, UnitFlags.RemoveClientControl);
                        RemoveFlag(UnitFields.Flags, UnitFlags.PlayerControlled);
                        ClearUnitState(UnitState.Possessed);
                        break;
                    case CharmType.Charm:
                        if (IsTypeId(TypeId.Unit) && charmer.GetClass() == Class.Warlock)
                        {
                            CreatureTemplate cinfo = ToCreature().GetCreatureTemplate();
                            if (cinfo != null && cinfo.CreatureType == CreatureType.Demon)
                            {
                                SetByteValue(UnitFields.Bytes0, 1, (byte)cinfo.UnitClass);
                                if (GetCharmInfo() != null)
                                    GetCharmInfo().SetPetNumber(0, true);
                                else
                                    Log.outError(LogFilter.Unit, "Aura:HandleModCharm: target={0} with typeid={1} has a charm aura but no charm info!", GetGUID(), GetTypeId());
                            }
                        }
                        break;
                    case CharmType.Convert:
                        break;
                }
            }

            Player player = ToPlayer();
            if (player)
            {
                if (charmer.IsTypeId(TypeId.Unit)) // charmed by a creature, this means we had PlayerAI
                {
                    NeedChangeAI = true;
                    IsAIEnabled = false;
                }
                player.SetClientControl(this, true);
            }

            // a guardian should always have charminfo
            if (playerCharmer && this != charmer.GetFirstControlled())
                playerCharmer.SendRemoveControlBar();
            else if (IsTypeId(TypeId.Player) || (IsTypeId(TypeId.Unit) && !IsGuardian()))
                DeleteCharmInfo();
        }

        public void GetAllMinionsByEntry(List<TempSummon> Minions, uint entry)
        {
            for (var i = 0; i < m_Controlled.Count; ++i)
            {
                Unit unit = m_Controlled[i];
                if (unit.GetEntry() == entry && unit.IsSummon()) // minion, actually
                    Minions.Add(unit.ToTempSummon());
            }
        }

        public void RemoveAllMinionsByEntry(uint entry)
        {
            for (var i = 0; i < m_Controlled.Count; ++i)
            {
                Unit unit = m_Controlled[i];
                if (unit.GetEntry() == entry && unit.IsTypeId(TypeId.Unit)
                    && unit.ToCreature().IsSummon()) // minion, actually
                    unit.ToTempSummon().UnSummon();
                // i think this is safe because i have never heard that a despawned minion will trigger a same minion
            }
        }

        public void SetCharm(Unit charm, bool apply)
        {
            if (apply)
            {
                if (IsTypeId(TypeId.Player))
                {
                    if (!AddGuidValue(UnitFields.Charm, charm.GetGUID()))
                        Log.outFatal(LogFilter.Unit, "Player {0} is trying to charm unit {1}, but it already has a charmed unit {2}", GetName(), charm.GetEntry(), GetCharmGUID());

                    charm.m_ControlledByPlayer = true;
                    // @todo maybe we can use this flag to check if controlled by player
                    charm.SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
                }
                else
                    charm.m_ControlledByPlayer = false;

                // PvP, FFAPvP
                charm.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag));

                if (!charm.AddGuidValue(UnitFields.CharmedBy, GetGUID()))
                    Log.outFatal(LogFilter.Unit, "Unit {0} is being charmed, but it already has a charmer {1}", charm.GetEntry(), charm.GetCharmerGUID());

                _isWalkingBeforeCharm = charm.IsWalking();
                if (_isWalkingBeforeCharm)
                    charm.SetWalk(false);

                if (!m_Controlled.Contains(charm))
                    m_Controlled.Add(charm);
            }
            else
            {
                if (IsTypeId(TypeId.Player))
                {
                    if (!RemoveGuidValue(UnitFields.Charm, charm.GetGUID()))
                        Log.outFatal(LogFilter.Unit, "Player {0} is trying to uncharm unit {1}, but it has another charmed unit {2}", GetName(), charm.GetEntry(), GetCharmGUID());
                }

                if (!charm.RemoveGuidValue(UnitFields.CharmedBy, GetGUID()))
                    Log.outFatal(LogFilter.Unit, "Unit {0} is being uncharmed, but it has another charmer {1}", charm.GetEntry(), charm.GetCharmerGUID());
                Player player = charm.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (charm.IsTypeId(TypeId.Player))
                {
                    charm.m_ControlledByPlayer = true;
                    charm.SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
                    charm.ToPlayer().UpdatePvPState();
                }
                else if (player)
                {
                    charm.m_ControlledByPlayer = true;
                    charm.SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
                    charm.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, player.GetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag));
                }
                else
                {
                    charm.m_ControlledByPlayer = false;
                    charm.RemoveFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
                    charm.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, 0);
                }

                if (charm.IsWalking() != _isWalkingBeforeCharm)
                    charm.SetWalk(_isWalkingBeforeCharm);

                if (charm.IsTypeId(TypeId.Player) || !charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Minion)
                        || charm.GetOwnerGUID() != GetGUID())
                {
                    m_Controlled.Remove(charm);
                }
            }
        }

        public Unit GetFirstControlled()
        {
            // Sequence: charmed, pet, other guardians
            Unit unit = GetCharm();
            if (!unit)
            {
                ObjectGuid guid = GetMinionGUID();
                if (!guid.IsEmpty())
                    unit = Global.ObjAccessor.GetUnit(this, guid);
            }

            return unit;
        }

        public void RemoveCharmAuras()
        {
            RemoveAurasByType(AuraType.ModCharm);
            RemoveAurasByType(AuraType.ModPossessPet);
            RemoveAurasByType(AuraType.ModPossess);
            RemoveAurasByType(AuraType.AoeCharm);
        }

        public void RemoveAllControlled()
        {
            // possessed pet and vehicle
            if (IsTypeId(TypeId.Player))
                ToPlayer().StopCastingCharm();

            while (!m_Controlled.Empty())
            {
                Unit target = m_Controlled.First();
                m_Controlled.RemoveAt(0);
                if (target.GetCharmerGUID() == GetGUID())
                    target.RemoveCharmAuras();
                else if (target.GetOwnerGUID() == GetGUID() && target.IsSummon())
                    target.ToTempSummon().UnSummon();
                else
                    Log.outError(LogFilter.Unit, "Unit {0} is trying to release unit {1} which is neither charmed nor owned by it", GetEntry(), target.GetEntry());
            }
            if (!GetPetGUID().IsEmpty())
                Log.outFatal(LogFilter.Unit, "Unit {0} is not able to release its pet {1}", GetEntry(), GetPetGUID());
            if (!GetMinionGUID().IsEmpty())
                Log.outFatal(LogFilter.Unit, "Unit {0} is not able to release its minion {1}", GetEntry(), GetMinionGUID());
            if (!GetCharmGUID().IsEmpty())
                Log.outFatal(LogFilter.Unit, "Unit {0} is not able to release its charm {1}", GetEntry(), GetCharmGUID());
        }

        public void SendPetActionFeedback(uint spellId, ActionFeedback msg)
        {
            Unit owner = GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            PetActionFeedback petActionFeedback = new PetActionFeedback();
            petActionFeedback.SpellID = spellId;
            petActionFeedback.Response = msg;
            owner.ToPlayer().SendPacket(petActionFeedback);
        }

        public void SendPetTalk(PetTalk pettalk)
        {
            Unit owner = GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            PetActionSound petActionSound = new PetActionSound();
            petActionSound.UnitGUID = GetGUID();
            petActionSound.Action = pettalk;
            owner.ToPlayer().SendPacket(petActionSound);
        }

        public void SendPetAIReaction(ObjectGuid guid)
        {
            Unit owner = GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            AIReaction packet = new AIReaction();
            packet.UnitGUID = guid;
            packet.Reaction = AiReaction.Hostile;

            owner.ToPlayer().SendPacket(packet);
        }

        public Pet CreateTamedPetFrom(Creature creatureTarget, uint spell_id = 0)
        {
            if (!IsTypeId(TypeId.Player))
                return null;

            Pet pet = new Pet(ToPlayer(), PetType.Hunter);

            if (!pet.CreateBaseAtCreature(creatureTarget))
                return null;

            uint level = creatureTarget.GetLevelForTarget(this) + 5 < getLevel() ? (getLevel() - 5) : creatureTarget.GetLevelForTarget(this);

            InitTamedPet(pet, level, spell_id);

            return pet;
        }

        public Pet CreateTamedPetFrom(uint creatureEntry, uint spell_id = 0)
        {
            if (!IsTypeId(TypeId.Player))
                return null;

            CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);
            if (creatureInfo == null)
                return null;

            Pet pet = new Pet(ToPlayer(), PetType.Hunter);

            if (!pet.CreateBaseAtCreatureInfo(creatureInfo, this) || !InitTamedPet(pet, getLevel(), spell_id))
                return null;

            return pet;
        }

        bool InitTamedPet(Pet pet, uint level, uint spell_id)
        {
            pet.SetCreatorGUID(GetGUID());
            pet.SetFaction(getFaction());
            pet.SetUInt32Value(UnitFields.CreatedBySpell, spell_id);

            if (IsTypeId(TypeId.Player))
                pet.SetUInt32Value(UnitFields.Flags, (uint)UnitFlags.PvpAttackable);

            if (!pet.InitStatsForLevel(level))
            {
                Log.outError(LogFilter.Unit, "Pet:InitStatsForLevel() failed for creature (Entry: {0})!", pet.GetEntry());
                return false;
            }

            PhasingHandler.InheritPhaseShift(pet, this);

            pet.GetCharmInfo().SetPetNumber(Global.ObjectMgr.GeneratePetNumber(), true);
            // this enables pet details window (Shift+P)
            pet.InitPetCreateSpells();
            pet.SetFullHealth();
            return true;
        }
    }
}
