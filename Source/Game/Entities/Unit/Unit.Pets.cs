// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Movement;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public partial class Unit
    {
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
            if (IsCharmed())
            {
                UnitAI newAI = null;
                if (IsPlayer())
                {
                    Unit charmer = GetCharmer();
                    if (charmer != null)
                    {
                        // first, we check if the creature's own AI specifies an override playerai for its owned players
                        Creature creatureCharmer = charmer.ToCreature();
                        if (creatureCharmer != null)
                        {
                            CreatureAI charmerAI = creatureCharmer.GetAI();
                            if (charmerAI != null)
                                newAI = charmerAI.GetAIForCharmedPlayer(ToPlayer());
                        }
                        else
                            Log.outError(LogFilter.Misc, $"Attempt to assign charm AI to player {GetGUID()} who is charmed by non-creature {GetCharmerGUID()}.");
                    }
                    if (newAI == null) // otherwise, we default to the generic one
                        newAI = new SimpleCharmedPlayerAI(ToPlayer());
                }
                else
                {
                    Cypher.Assert(IsCreature());
                    if (IsPossessed() || IsVehicle())
                        newAI = new PossessedAI(ToCreature());
                    else
                        newAI = new PetAI(ToCreature());
                }

                Cypher.Assert(newAI != null);
                SetAI(newAI);
                newAI.OnCharmed(true);
            }
            else
            {
                RestoreDisabledAI();
                // Hack: this is required because we want to call OnCharmed(true) on the restored AI
                RefreshAI();
                UnitAI ai = GetAI();
                if (ai != null)
                    ai.OnCharmed(true);
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

                if (!IsInWorld)
                {
                    Log.outFatal(LogFilter.Unit, $"SetMinion: Minion being added to owner not in world. Minion: {minion.GetGUID()}, Owner: {GetDebugInfo()}");
                    return;
                }

                minion.SetOwnerGUID(GetGUID());

                if (!m_Controlled.Contains(minion))
                    m_Controlled.Add(minion);

                if (IsTypeId(TypeId.Player))
                {
                    minion.m_ControlledByPlayer = true;
                    minion.SetUnitFlag(UnitFlags.PlayerControlled);
                }

                // Can only have one pet. If a new one is summoned, dismiss the old one.
                if (minion.IsGuardianPet())
                {
                    Guardian oldPet = GetGuardianPet();
                    if (oldPet != null)
                    {
                        if (oldPet != minion && (oldPet.IsPet() || minion.IsPet() || oldPet.GetEntry() != minion.GetEntry()))
                        {
                            // remove existing minion pet
                            Pet oldPetAsPet = oldPet.ToPet();
                            if (oldPetAsPet != null)
                                oldPetAsPet.Remove(PetSaveMode.NotInSlot);
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

                if (minion.HasUnitTypeMask(UnitTypeMask.ControlableGuardian))
                    if (GetMinionGUID().IsEmpty())
                        SetMinionGUID(minion.GetGUID());

                var properties = minion.m_Properties;
                if (properties != null && properties.Title == SummonTitle.Companion)
                {
                    SetCritterGUID(minion.GetGUID());
                    Player thisPlayer = ToPlayer();
                    if (thisPlayer != null)
                    {
                        if (properties.HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
                        {
                            var pet = thisPlayer.GetSession().GetBattlePetMgr().GetPet(thisPlayer.GetSummonedBattlePetGUID());
                            if (pet != null)
                            {
                                minion.SetBattlePetCompanionGUID(thisPlayer.GetSummonedBattlePetGUID());
                                minion.SetBattlePetCompanionNameTimestamp((uint)pet.NameTimestamp);
                                minion.SetWildBattlePetLevel(pet.PacketInfo.Level);

                                uint display = pet.PacketInfo.DisplayID;
                                if (display != 0)
                                    minion.SetDisplayId(display, true);
                            }
                        }
                    }
                }

                // PvP, FFAPvP
                minion.ReplaceAllPvpFlags(GetPvpFlags());

                // FIXME: hack, speed must be set only at follow
                if (IsTypeId(TypeId.Player) && minion.IsPet())
                    for (UnitMoveType i = 0; i < UnitMoveType.Max; ++i)
                        minion.SetSpeedRate(i, m_speed_rate[(int)i]);

                // Send infinity cooldown - client does that automatically but after relog cooldown needs to be set again
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(minion.m_unitData.CreatedBySpell, Difficulty.None);
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

                if (minion.m_Properties != null && minion.m_Properties.Title == SummonTitle.Companion)
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
                    SpellInfo spInfo = Global.SpellMgr.GetSpellInfo(minion.ToTotem().GetSpell(), Difficulty.None);
                    if (spInfo != null)
                    {
                        foreach (var spellEffectInfo in spInfo.GetEffects())
                        {
                            if (spellEffectInfo == null || !spellEffectInfo.IsEffect(SpellEffectName.Summon))
                                continue;

                            RemoveAllMinionsByEntry((uint)spellEffectInfo.MiscValue);
                        }
                    }
                }

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(minion.m_unitData.CreatedBySpell, Difficulty.None);
                // Remove infinity cooldown
                if (spellInfo != null && spellInfo.IsCooldownStartedOnEvent())
                    GetSpellHistory().SendCooldownEvent(spellInfo);

                if (GetMinionGUID() == minion.GetGUID())
                {
                    SetMinionGUID(ObjectGuid.Empty);
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

                        SetMinionGUID(unit.GetGUID());
                        // show another pet bar if there is no charm bar
                        if (GetTypeId() == TypeId.Player && GetCharmedGUID().IsEmpty())
                        {
                            if (unit.IsPet())
                                ToPlayer().PetSpellInitialize();
                            else
                                ToPlayer().CharmSpellInitialize();
                        }
                        break;
                    }
                }
            }

            UpdatePetCombatState();
        }

        public bool SetCharmedBy(Unit charmer, CharmType type, AuraApplication aurApp = null)
        {
            if (charmer == null)
                return false;

            // dismount players when charmed
            if (IsTypeId(TypeId.Player))
                RemoveAurasByType(AuraType.Mounted);

            if (charmer.IsTypeId(TypeId.Player))
                charmer.RemoveAurasByType(AuraType.Mounted);

            Cypher.Assert(type != CharmType.Possess || charmer.IsTypeId(TypeId.Player));
            Cypher.Assert((type == CharmType.Vehicle) == (GetVehicleKit() != null && GetVehicleKit().IsControllableVehicle()));

            Log.outDebug(LogFilter.Unit, "SetCharmedBy: charmer {0} (GUID {1}), charmed {2} (GUID {3}), type {4}.", charmer.GetEntry(), charmer.GetGUID().ToString(), GetEntry(), GetGUID().ToString(), type);

            if (this == charmer)
            {
                Log.outFatal(LogFilter.Unit, "Unit:SetCharmedBy: Unit {0} (GUID {1}) is trying to charm itself!", GetEntry(), GetGUID().ToString());
                return false;
            }

            if (IsPlayer() && ToPlayer().GetTransport() != null)
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
            AttackStop();

            Player playerCharmer = charmer.ToPlayer();

            // Charmer stop charming
            if (playerCharmer != null)
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

            _oldFactionId = GetFaction();
            SetFaction(charmer.GetFaction());

            // Pause any Idle movement
            PauseMovement(0, 0, false);

            // Remove any active voluntary movement
            GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

            // Stop any remaining spline, if no involuntary movement is found
            Func<MovementGenerator, bool> criteria = movement => movement.Priority == MovementGeneratorPriority.Highest;
            if (!GetMotionMaster().HasMovementGenerator(criteria))
                StopMoving();

            // Set charmed
            charmer.SetCharm(this, true);

            Player player = ToPlayer();
            if (player != null)
            {
                if (player.IsAFK())
                    player.ToggleAFK();

                player.SetClientControl(this, false);
            }

            // charm is set by aura, and aura effect remove handler was called during apply handler execution
            // prevent undefined behaviour
            if (aurApp != null && aurApp.GetRemoveMode() != 0)
            {
                // properly clean up charm changes up to this point to avoid leaving the unit in partially charmed state
                SetFaction(_oldFactionId);
                GetMotionMaster().InitializeDefault();
                charmer.SetCharm(this, false);
                return false;
            }

            // Pets already have a properly initialized CharmInfo, don't overwrite it.
            if (type != CharmType.Vehicle && GetCharmInfo() == null)
            {
                InitCharmInfo();
                if (type == CharmType.Possess)
                    GetCharmInfo().InitPossessCreateSpells();
                else
                    GetCharmInfo().InitCharmCreateSpells();
            }

            if (playerCharmer != null)
            {
                switch (type)
                {
                    case CharmType.Vehicle:
                        SetUnitFlag(UnitFlags.Possessed);
                        playerCharmer.SetClientControl(this, true);
                        playerCharmer.VehicleSpellInitialize();
                        break;
                    case CharmType.Possess:
                        SetUnitFlag(UnitFlags.Possessed);
                        charmer.SetUnitFlag(UnitFlags.RemoveClientControl);
                        playerCharmer.SetClientControl(this, true);
                        playerCharmer.PossessSpellInitialize();
                        AddUnitState(UnitState.Possessed);
                        break;
                    case CharmType.Charm:
                        if (IsTypeId(TypeId.Unit) && charmer.GetClass() == Class.Warlock)
                        {
                            CreatureTemplate cinfo = ToCreature().GetCreatureTemplate();
                            if (cinfo != null && cinfo.CreatureType == CreatureType.Demon)
                            {
                                // to prevent client crash
                                SetClass(Class.Mage);

                                // just to enable stat window
                                if (GetCharmInfo() != null)
                                    GetCharmInfo().SetPetNumber(Global.ObjectMgr.GeneratePetNumber(), true);

                                // if charmed two demons the same session, the 2nd gets the 1st one's name
                                SetPetNameTimestamp((uint)GameTime.GetGameTime()); // cast can't be helped
                            }
                        }
                        playerCharmer.CharmSpellInitialize();
                        break;
                    default:
                    case CharmType.Convert:
                        break;
                }
            }

            AddUnitState(UnitState.Charmed);

            if (!IsPlayer() || !charmer.IsPlayer())
            {
                // AI will schedule its own change if appropriate
                UnitAI ai = GetAI();
                if (ai != null)
                    ai.OnCharmed(false);
                else
                    ScheduleAIChange();
            }
            return true;
        }

        public void RemoveCharmedBy(Unit charmer)
        {
            if (!IsCharmed())
                return;

            if (charmer != null)
                Cypher.Assert(charmer == GetCharmer());
            else
                charmer = GetCharmer();

            Cypher.Assert(charmer != null);

            CharmType type;
            if (HasUnitState(UnitState.Possessed))
                type = CharmType.Possess;
            else if (charmer.IsOnVehicle(this))
                type = CharmType.Vehicle;
            else
                type = CharmType.Charm;

            CastStop();
            AttackStop();

            if (_oldFactionId != 0)
            {
                SetFaction(_oldFactionId);
                _oldFactionId = 0;
            }
            else
                RestoreFaction();

            ///@todo Handle SLOT_IDLE motion resume
            GetMotionMaster().InitializeDefault();

            // Vehicle should not attack its passenger after he exists the seat
            if (type != CharmType.Vehicle)
                LastCharmerGUID = charmer.GetGUID();

            Cypher.Assert(type != CharmType.Possess || charmer.IsTypeId(TypeId.Player));
            Cypher.Assert(type != CharmType.Vehicle || (IsTypeId(TypeId.Unit) && IsVehicle()));

            charmer.SetCharm(this, false);
            m_combatManager.RevalidateCombat();

            Player playerCharmer = charmer.ToPlayer();
            if (playerCharmer != null)
            {
                switch (type)
                {
                    case CharmType.Vehicle:
                        playerCharmer.SetClientControl(this, false);
                        playerCharmer.SetClientControl(charmer, true);
                        RemoveUnitFlag(UnitFlags.Possessed);
                        break;
                    case CharmType.Possess:
                        ClearUnitState(UnitState.Possessed);
                        playerCharmer.SetClientControl(this, false);
                        playerCharmer.SetClientControl(charmer, true);
                        charmer.RemoveUnitFlag(UnitFlags.RemoveClientControl);
                        RemoveUnitFlag(UnitFlags.Possessed);
                        break;
                    case CharmType.Charm:
                        if (IsTypeId(TypeId.Unit) && charmer.GetClass() == Class.Warlock)
                        {
                            CreatureTemplate cinfo = ToCreature().GetCreatureTemplate();
                            if (cinfo != null && cinfo.CreatureType == CreatureType.Demon)
                            {
                                SetClass((Class)cinfo.UnitClass);
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
            if (player != null)
                player.SetClientControl(this, true);

            if (playerCharmer != null && this != charmer.GetFirstControlled())
                playerCharmer.SendRemoveControlBar();

            // a guardian should always have charminfo
            if (!IsGuardian())
                DeleteCharmInfo();

            // reset confused movement for example
            ApplyControlStatesIfNeeded();

            if (!IsPlayer() || charmer.IsCreature())
            {
                UnitAI charmedAI = GetAI();
                if (charmedAI != null)
                    charmedAI.OnCharmed(false); // AI will potentially schedule a charm ai update
                else
                    ScheduleAIChange();
            }
        }

        public List<TempSummon> GetAllMinionsByEntry(uint entry)
        {
            List<TempSummon> minions = new();
            for (var i = 0; i < m_Controlled.Count; ++i)
            {
                Unit unit = m_Controlled[i];
                if (unit.GetEntry() == entry && unit.IsSummon()) // minion, actually
                    minions.Add(unit.ToTempSummon());
            }

            return minions;
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
                    Cypher.Assert(GetCharmedGUID().IsEmpty(), $"Player {GetName()} is trying to charm unit {charm.GetEntry()}, but it already has a charmed unit {GetCharmedGUID()}");
                    SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Charm), charm.GetGUID());
                    m_charmed = charm;

                    charm.m_ControlledByPlayer = true;
                    // @todo maybe we can use this flag to check if controlled by player
                    charm.SetUnitFlag(UnitFlags.PlayerControlled);
                }
                else
                    charm.m_ControlledByPlayer = false;

                // PvP, FFAPvP
                charm.ReplaceAllPvpFlags(GetPvpFlags());

                Cypher.Assert(charm.GetCharmerGUID().IsEmpty(), $"Unit {charm.GetEntry()} is being charmed, but it already has a charmer {charm.GetCharmerGUID()}");
                charm.SetUpdateFieldValue(charm.m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CharmedBy), GetGUID());
                charm.m_charmer = this;

                _isWalkingBeforeCharm = charm.IsWalking();
                if (_isWalkingBeforeCharm)
                    charm.SetWalk(false);

                if (!m_Controlled.Contains(charm))
                    m_Controlled.Add(charm);
            }
            else
            {
                charm.ClearUnitState(UnitState.Charmed);

                if (IsPlayer())
                {
                    Cypher.Assert(GetCharmedGUID() == charm.GetGUID(), $"Player {GetName()} is trying to uncharm unit {charm.GetEntry()}, but it has another charmed unit {GetCharmedGUID()}");
                    SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.Charm), ObjectGuid.Empty);
                    m_charmed = null;
                }

                Cypher.Assert(charm.GetCharmerGUID() == GetGUID(), $"Unit {charm.GetEntry()} is being uncharmed, but it has another charmer {charm.GetCharmerGUID()}");
                charm.SetUpdateFieldValue(charm.m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.CharmedBy), ObjectGuid.Empty);
                charm.m_charmer = null;

                Player player = charm.GetCharmerOrOwnerPlayerOrPlayerItself();
                if (charm.IsTypeId(TypeId.Player))
                {
                    charm.m_ControlledByPlayer = true;
                    charm.SetUnitFlag(UnitFlags.PlayerControlled);
                    charm.ToPlayer().UpdatePvPState();
                }
                else if (player != null)
                {
                    charm.m_ControlledByPlayer = true;
                    charm.SetUnitFlag(UnitFlags.PlayerControlled);
                    charm.ReplaceAllPvpFlags(player.GetPvpFlags());
                }
                else
                {
                    charm.m_ControlledByPlayer = false;
                    charm.RemoveUnitFlag(UnitFlags.PlayerControlled);
                    charm.ReplaceAllPvpFlags(UnitPVPStateFlags.None);
                }

                if (charm.IsWalking() != _isWalkingBeforeCharm)
                    charm.SetWalk(_isWalkingBeforeCharm);

                if (charm.IsTypeId(TypeId.Player) || !charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Minion)
                        || charm.GetOwnerGUID() != GetGUID())
                {
                    m_Controlled.Remove(charm);
                }
            }

            UpdatePetCombatState();
        }

        public Unit GetFirstControlled()
        {
            // Sequence: charmed, pet, other guardians
            Unit unit = GetCharmed();
            if (unit == null)
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
            if (!GetCharmedGUID().IsEmpty())
                Log.outFatal(LogFilter.Unit, "Unit {0} is not able to release its charm {1}", GetEntry(), GetCharmedGUID());
            if (!IsPet()) // pets don't use the flag for this
                RemoveUnitFlag(UnitFlags.PetInCombat); // m_controlled is now empty, so we know none of our minions are in combat
        }

        public void SendPetActionFeedback(PetActionFeedback msg, uint spellId)
        {
            Unit owner = GetOwner();
            if (owner == null || !owner.IsTypeId(TypeId.Player))
                return;

            PetActionFeedbackPacket petActionFeedback = new();
            petActionFeedback.SpellID = spellId;
            petActionFeedback.Response = msg;
            owner.ToPlayer().SendPacket(petActionFeedback);
        }

        public void SendPetTalk(PetTalk pettalk)
        {
            Unit owner = GetOwner();
            if (owner == null || !owner.IsTypeId(TypeId.Player))
                return;

            PetActionSound petActionSound = new();
            petActionSound.UnitGUID = GetGUID();
            petActionSound.Action = pettalk;
            owner.ToPlayer().SendPacket(petActionSound);
        }

        public void SendPetAIReaction(ObjectGuid guid)
        {
            Unit owner = GetOwner();
            if (owner == null || !owner.IsTypeId(TypeId.Player))
                return;

            AIReaction packet = new();
            packet.UnitGUID = guid;
            packet.Reaction = AiReaction.Hostile;

            owner.ToPlayer().SendPacket(packet);
        }

        public Pet CreateTamedPetFrom(Creature creatureTarget, uint spell_id = 0)
        {
            if (!IsTypeId(TypeId.Player))
                return null;

            Pet pet = new(ToPlayer(), PetType.Hunter);

            if (!pet.CreateBaseAtCreature(creatureTarget))
                return null;

            uint level = creatureTarget.GetLevelForTarget(this) + 5 < GetLevel() ? (GetLevel() - 5) : creatureTarget.GetLevelForTarget(this);

            if (!InitTamedPet(pet, level, spell_id))
            {
                pet.Dispose();
                return null;
            }

            return pet;
        }

        public Pet CreateTamedPetFrom(uint creatureEntry, uint spell_id = 0)
        {
            if (!IsTypeId(TypeId.Player))
                return null;

            CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);
            if (creatureInfo == null)
                return null;

            Pet pet = new(ToPlayer(), PetType.Hunter);

            if (!pet.CreateBaseAtCreatureInfo(creatureInfo, this) || !InitTamedPet(pet, GetLevel(), spell_id))
                return null;

            return pet;
        }

        bool InitTamedPet(Pet pet, uint level, uint spell_id)
        {
            Player player = ToPlayer();
            PetStable petStable = player.GetOrInitPetStable();

            var freeActiveSlot = Array.FindIndex(petStable.ActivePets, petInfo => petInfo == null);
            if (freeActiveSlot == -1)
                return false;

            pet.SetCreatorGUID(GetGUID());
            pet.SetFaction(GetFaction());
            pet.SetCreatedBySpell(spell_id);            
            pet.SetUnitFlag(UnitFlags.PlayerControlled);

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

            petStable.SetCurrentActivePetIndex((uint)freeActiveSlot);

            PetStable.PetInfo petInfo = new();
            pet.FillPetInfo(petInfo);
            player.AddPetToUpdateFields(petInfo, (PetSaveMode)petStable.GetCurrentActivePetIndex(), PetStableFlags.Active);
            petStable.ActivePets[freeActiveSlot] = petInfo;
            return true;
        }

        public void UpdatePetCombatState()
        {
            Cypher.Assert(!IsPet()); // player pets do not use UNIT_FLAG_PET_IN_COMBAT for this purpose - but player pets should also never have minions of their own to call this

            bool state = false;
            foreach (Unit minion in m_Controlled)
            {
                if (minion.IsInCombat())
                {
                    state = true;
                    break;
                }
            }

            if (state)
                SetUnitFlag(UnitFlags.PetInCombat);
            else
                RemoveUnitFlag(UnitFlags.PetInCombat);
        }
    }
}
