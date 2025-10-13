// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entities
{
    public class Pet : Guardian
    {
        const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
        const int HappinessLevelSize = 333000;
        const float PetXPFactor = 0.05f;

        public Pet(Player owner, PetType type = PetType.Max) : base(null, owner, true)
        {
            m_petType = type;

            Cypher.Assert(GetOwner().IsTypeId(TypeId.Player));

            UnitTypeMask |= UnitTypeMask.Pet;
            if (type == PetType.Hunter)
                UnitTypeMask |= UnitTypeMask.HunterPet;

            if (!UnitTypeMask.HasAnyFlag(UnitTypeMask.ControlableGuardian))
            {
                UnitTypeMask |= UnitTypeMask.ControlableGuardian;
                InitCharmInfo();
            }

            SetName("Pet");
            m_focusRegenTimer = PetFocusRegenInterval;
        }

        public override void Dispose()
        {
            _declinedname = null;
            base.Dispose();
        }

        public override void AddToWorld()
        {
            //- Register the pet for guid lookup
            if (!IsInWorld)
            {
                // Register the pet for guid lookup
                base.AddToWorld();
                InitializeAI();
                ZoneScript zoneScript = GetZoneScript() != null ? GetZoneScript() : GetInstanceScript();
                if (zoneScript != null)
                    zoneScript.OnCreatureCreate(this);
            }

            // Prevent stuck pets when zoning. Pets default to "follow" when added to world
            // so we'll reset flags and let the AI handle things
            if (GetCharmInfo() != null && GetCharmInfo().HasCommandState(CommandStates.Follow))
            {
                GetCharmInfo().SetIsCommandAttack(false);
                GetCharmInfo().SetIsCommandFollow(false);
                GetCharmInfo().SetIsAtStay(false);
                GetCharmInfo().SetIsFollowing(false);
                GetCharmInfo().SetIsReturning(false);
            }
        }

        public override void RemoveFromWorld()
        {
            // Remove the pet from the accessor
            if (IsInWorld)
            {
                // Don't call the function for Creature, normal mobs + totems go in a different storage
                base.RemoveFromWorld();
                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public static Tuple<PetStable.PetInfo, PetSaveMode> GetLoadPetInfo(PetStable stable, uint petEntry, uint petnumber, PetSaveMode? slot)
        {
            if (petnumber != 0)
            {
                // Known petnumber entry
                for (var activeSlot = 0; activeSlot < stable.ActivePets.Length; ++activeSlot)
                    if (stable.ActivePets[activeSlot] != null && stable.ActivePets[activeSlot].PetNumber == petnumber)
                        return Tuple.Create(stable.ActivePets[activeSlot], PetSaveMode.FirstActiveSlot + activeSlot);

                for (var stableSlot = 0; stableSlot < stable.StabledPets.Length; ++stableSlot)
                    if (stable.StabledPets[stableSlot] != null && stable.StabledPets[stableSlot].PetNumber == petnumber)
                        return Tuple.Create(stable.StabledPets[stableSlot], PetSaveMode.FirstStableSlot + stableSlot);

                foreach (var pet in stable.UnslottedPets)
                    if (pet.PetNumber == petnumber)
                        return Tuple.Create(pet, PetSaveMode.NotInSlot);
            }
            else if (slot.HasValue)
            {
                // Current pet
                if (slot == PetSaveMode.AsCurrent)
                    if (stable.GetCurrentActivePetIndex().HasValue && stable.ActivePets[stable.GetCurrentActivePetIndex().Value] != null)
                        return Tuple.Create(stable.ActivePets[stable.GetCurrentActivePetIndex().Value], (PetSaveMode)stable.GetCurrentActivePetIndex());

                if (slot >= PetSaveMode.FirstActiveSlot && slot < PetSaveMode.LastActiveSlot)
                    if (stable.ActivePets[(int)slot.Value] != null)
                        return Tuple.Create(stable.ActivePets[(int)slot.Value], slot.Value);

                if (slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot)
                    if (stable.StabledPets[(int)slot.Value] != null)
                        return Tuple.Create(stable.StabledPets[(int)slot.Value], slot.Value);
            }
            else if (petEntry != 0)
            {
                // known petEntry entry (unique for summoned pet, but non unique for hunter pet (only from current or not stabled pets)

                foreach (var pet in stable.UnslottedPets)
                    if (pet.CreatureId == petEntry)
                        return Tuple.Create(pet, PetSaveMode.NotInSlot);
            }
            else
            {
                // Any current or other non-stabled pet (for hunter "call pet")
                if (stable.ActivePets[0] != null)
                    return Tuple.Create(stable.ActivePets[0], PetSaveMode.FirstActiveSlot);

                if (!stable.UnslottedPets.Empty())
                    return Tuple.Create(stable.UnslottedPets.First(), PetSaveMode.NotInSlot);
            }

            return Tuple.Create<PetStable.PetInfo, PetSaveMode>(null, PetSaveMode.AsDeleted);
        }

        public bool LoadPetFromDB(Player owner, uint petEntry = 0, uint petnumber = 0, bool current = false, PetSaveMode? forcedSlot = null)
        {
            m_loading = true;

            PetStable petStable = owner.GetPetStable();

            ulong ownerid = owner.GetGUID().GetCounter();
            (PetStable.PetInfo petInfo, PetSaveMode slot) = GetLoadPetInfo(petStable, petEntry, petnumber, forcedSlot);
            if (petInfo == null || (slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot))
            {
                m_loading = false;
                return false;
            }

            // Don't try to reload the current pet
            if (petStable.GetCurrentPet() != null && owner.GetPet() != null && petStable.GetCurrentPet().PetNumber == petInfo.PetNumber)
                return false;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(petInfo.CreatedBySpellId, owner.GetMap().GetDifficultyID());

            bool isTemporarySummon = spellInfo != null && spellInfo.GetDuration() > 0;
            if (current && isTemporarySummon)
                return false;

            if (petInfo.Type == PetType.Hunter)
            {
                CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(petInfo.CreatureId);
                if (creatureInfo == null)
                    return false;

                CreatureDifficulty creatureDifficulty = creatureInfo.GetDifficulty(Difficulty.None);
                if (creatureDifficulty == null || !creatureInfo.IsTameable(owner.CanTameExoticPets(), creatureDifficulty))
                    return false;
            }

            if (current && owner.IsPetNeedBeTemporaryUnsummoned())
            {
                owner.SetTemporaryUnsummonedPetNumber(petInfo.PetNumber);
                return false;
            }

            owner.SetTemporaryUnsummonedPetNumber(0);

            Map map = owner.GetMap();
            ulong guid = map.GenerateLowGuid(HighGuid.Pet);

            if (!Create(guid, map, petInfo.CreatureId, petInfo.PetNumber))
                return false;

            PhasingHandler.InheritPhaseShift(this, owner);

            SetPetType(petInfo.Type);
            SetFaction(owner.GetFaction());
            SetCreatedBySpell(petInfo.CreatedBySpellId);

            float px, py, pz;
            if (IsCritter())
            {
                owner.GetClosePoint(out px, out py, out pz, GetCombatReach(), SharedConst.PetFollowDist, GetFollowAngle());
                Relocate(px, py, pz, owner.GetOrientation());

                if (!IsPositionValid())
                {
                    Log.outError(LogFilter.Pet, "Pet (guidlow {0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})",
                        GetGUID().ToString(), GetEntry(), GetPositionX(), GetPositionY());
                    return false;
                }

                map.AddToMap(ToCreature());
                return true;
            }

            GetCharmInfo().SetPetNumber(petInfo.PetNumber, IsPermanentPetFor(owner));

            SetDisplayId(petInfo.DisplayId, true);
            uint petlevel = petInfo.Level;
            ReplaceAllNpcFlags(NPCFlags.None);
            ReplaceAllNpcFlags2(NPCFlags2.None);
            SetName(petInfo.Name);

            switch (GetPetType())
            {
                case PetType.Summon:
                    petlevel = owner.GetLevel();

                    ReplaceAllUnitFlags(UnitFlags.PlayerControlled); // this enables popup window (pet dismiss, cancel)
                    break;
                case PetType.Hunter:
                    SetClass(Class.Warrior);
                    SetGender(Gender.None);
                    SetSheath(SheathState.Melee);
                    ReplaceAllPetFlags(petInfo.WasRenamed ? UnitPetFlags.CanBeAbandoned : UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned);
                    ReplaceAllUnitFlags(UnitFlags.PlayerControlled); // this enables popup window (pet abandon, cancel)
                    break;
                default:
                    if (!IsPetGhoul())
                        Log.outError(LogFilter.Pet, "Pet have incorrect type ({0}) for pet loading.", GetPetType());
                    break;
            }

            SetPetNameTimestamp((uint)GameTime.GetGameTime()); // cast can't be helped here
            SetCreatorGUID(owner.GetGUID());

            InitStatsForLevel(petlevel);
            SetPetExperience(petInfo.Experience);

            SynchronizeLevelWithOwner();

            // Set pet's position after setting level, its size depends on it
            owner.GetClosePoint(out px, out py, out pz, GetCombatReach(), SharedConst.PetFollowDist, GetFollowAngle());
            Relocate(px, py, pz, owner.GetOrientation());
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Pet, "Pet ({0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})", GetGUID().ToString(), GetEntry(), GetPositionX(), GetPositionY());
                return false;
            }

            SetReactState(petInfo.ReactState);
            SetCanModifyStats(true);

            if (GetPetType() == PetType.Summon && !current)              //all (?) summon pets come with full health when called, but not when they are current
                SetFullPower(PowerType.Mana);
            else
            {
                uint savedhealth = petInfo.Health;
                uint savedmana = petInfo.Mana;
                if (savedhealth == 0 && GetPetType() == PetType.Hunter)
                    SetDeathState(DeathState.JustDied);
                else
                {
                    SetHealth(savedhealth);
                    SetPower(PowerType.Mana, (int)savedmana);
                }
            }

            // set current pet as current
            // 0-4=current
            // PET_SAVE_NOT_IN_SLOT(-1) = not stable slot (summoning))
            if (slot == PetSaveMode.NotInSlot)
            {
                uint petInfoNumber = petInfo.PetNumber;
                if (petStable.CurrentPetIndex != 0)
                    owner.RemovePet(null, PetSaveMode.NotInSlot);

                var unslottedPetIndex = petStable.UnslottedPets.FindIndex(unslottedPet => unslottedPet.PetNumber == petInfoNumber);
                Cypher.Assert(!petStable.CurrentPetIndex.HasValue);
                Cypher.Assert(unslottedPetIndex != -1);

                petStable.SetCurrentUnslottedPetIndex((uint)unslottedPetIndex);
            }
            else if (PetSaveMode.FirstActiveSlot <= slot && slot <= PetSaveMode.LastActiveSlot)
            {
                var activePetIndex = Array.FindIndex(petStable.ActivePets, pet => pet?.PetNumber == petInfo.PetNumber);

                Cypher.Assert(activePetIndex != -1);

                petStable.SetCurrentActivePetIndex((uint)activePetIndex);
            }

            owner.SetMinion(this, true);

            if (!isTemporarySummon)
                GetCharmInfo().LoadPetActionBar(petInfo.ActionBar);

            map.AddToMap(ToCreature());

            //set last used pet number (for use in BG's)
            if (owner.IsPlayer() && IsControlled() && !IsTemporarySummoned() && (GetPetType() == PetType.Summon || GetPetType() == PetType.Hunter))
                owner.ToPlayer().SetLastPetNumber(petInfo.PetNumber);

            var session = owner.GetSession();
            var lastSaveTime = petInfo.LastSaveTime;
            var specializationId = petInfo.SpecializationId;
            owner.GetSession().AddQueryHolderCallback(DB.Characters.DelayQueryHolder(new PetLoadQueryHolder(ownerid, petInfo.PetNumber)))
                .AfterComplete(holder =>
                {
                    if (session.GetPlayer() != owner || owner.GetPet() != this)
                        return;

                    // passing previous checks ensure that 'this' is still valid
                    if (m_removed)
                        return;

                    uint timediff = (uint)(GameTime.GetGameTime() - lastSaveTime);
                    _LoadAuras(holder.GetResult(PetLoginQueryLoad.Auras), holder.GetResult(PetLoginQueryLoad.AuraEffects), timediff);

                    // load action bar, if data broken will fill later by default spells.
                    if (!isTemporarySummon)
                    {
                        _LoadSpells(holder.GetResult(PetLoginQueryLoad.Spells));
                        GetSpellHistory().LoadFromDB<Pet>(holder.GetResult(PetLoginQueryLoad.Cooldowns), holder.GetResult(PetLoginQueryLoad.Charges));
                        LearnPetPassives();
                        InitLevelupSpellsForLevel();
                        if (GetMap().IsBattleArena())
                            RemoveArenaAuras();

                        CastPetAuras(current);
                    }

                    Log.outDebug(LogFilter.Pet, $"New Pet has {GetGUID()}");

                    ushort specId = specializationId;
                    var petSpec = CliDB.ChrSpecializationStorage.LookupByKey(specId);
                    if (petSpec != null)
                        specId = (ushort)Global.DB2Mgr.GetChrSpecializationByIndex(owner.HasAuraType(AuraType.OverridePetSpecs) ? Class.Max : 0, petSpec.OrderIndex).Id;

                    SetSpecialization(specId);

                    // The SetSpecialization function will run these functions if the pet's spec is not 0
                    if (GetSpecialization() == 0)
                    {
                        CleanupActionBar();                                     // remove unknown spells from action bar after load

                        owner.PetSpellInitialize();
                    }


                    SetGroupUpdateFlag(GroupUpdatePetFlags.Full);

                    if (GetPetType() == PetType.Hunter)
                    {
                        SQLResult result = holder.GetResult(PetLoginQueryLoad.DeclinedNames);
                        if (!result.IsEmpty())
                        {
                            _declinedname = new();
                            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                                _declinedname.name[i] = result.Read<string>(i);
                        }
                    }

                    if (owner.IsMounted())
                        owner.DisablePetControlsOnMount(ReactStates.Passive, CommandStates.Follow);

                    // must be after SetMinion (owner guid check)
                    LoadTemplateImmunities(0);
                    m_loading = false;
                });

            return true;
        }

        public void SavePetToDB(PetSaveMode mode)
        {
            if (GetEntry() == 0)
                return;

            // save only fully controlled creature
            if (!IsControlled())
                return;

            // not save not player pets
            if (!GetOwnerGUID().IsPlayer())
                return;

            Player owner = GetOwner();
            if (owner == null)
                return;

            // not save pet as current if another pet temporary unsummoned
            if (mode == PetSaveMode.AsCurrent && owner.GetTemporaryUnsummonedPetNumber() != 0 &&
                owner.GetTemporaryUnsummonedPetNumber() != GetCharmInfo().GetPetNumber())
            {
                // pet will lost anyway at restore temporary unsummoned
                if (GetPetType() == PetType.Hunter)
                    return;

                // for warlock case
                mode = PetSaveMode.NotInSlot;
            }

            uint curhealth = (uint)GetHealth();
            int curmana = GetPower(PowerType.Mana);

            SQLTransaction trans = new();
            // save auras before possibly removing them    
            _SaveAuras(trans);

            if (mode == PetSaveMode.AsCurrent)
            {
                var activeSlot = owner.GetPetStable().GetCurrentActivePetIndex();
                if (activeSlot.HasValue)
                    mode = (PetSaveMode)activeSlot;
            }

            // stable and not in slot saves
            if (mode < PetSaveMode.FirstActiveSlot || mode >= PetSaveMode.LastActiveSlot)
                RemoveAllAuras();

            _SaveSpells(trans);
            GetSpellHistory().SaveToDB<Pet>(trans);
            DB.Characters.CommitTransaction(trans);

            // current/stable/not_in_slot
            if (mode != PetSaveMode.AsDeleted)
            {
                ulong ownerLowGUID = GetOwnerGUID().GetCounter();
                trans = new SQLTransaction();

                // remove current data
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                // save pet
                string actionBar = GenerateActionBarData();

                Cypher.Assert(owner.GetPetStable().GetCurrentPet() != null && owner.GetPetStable().GetCurrentPet().PetNumber == GetCharmInfo().GetPetNumber());
                FillPetInfo(owner.GetPetStable().GetCurrentPet(), owner.GetTemporaryPetReactState());

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                stmt.AddValue(1, GetEntry());
                stmt.AddValue(2, ownerLowGUID);
                stmt.AddValue(3, GetNativeDisplayId());
                stmt.AddValue(4, GetLevel());
                stmt.AddValue(5, m_unitData.PetExperience);
                stmt.AddValue(6, (byte)owner.GetTemporaryPetReactState().GetValueOrDefault(GetReactState()));
                stmt.AddValue(7, (owner.GetPetStable().GetCurrentActivePetIndex().HasValue ? (short)owner.GetPetStable().GetCurrentActivePetIndex().Value : (short)PetSaveMode.NotInSlot));
                stmt.AddValue(8, GetName());
                stmt.AddValue(9, HasPetFlag(UnitPetFlags.CanBeRenamed) ? 0 : 1);
                stmt.AddValue(10, curhealth);
                stmt.AddValue(11, curmana);

                stmt.AddValue(12, actionBar);

                stmt.AddValue(13, GameTime.GetGameTime());
                stmt.AddValue(14, m_unitData.CreatedBySpell);
                stmt.AddValue(15, (byte)GetPetType());
                stmt.AddValue(16, GetSpecialization());
                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);
            }
            // delete
            else
            {
                RemoveAllAuras();
                DeleteFromDB(GetCharmInfo().GetPetNumber());
            }
        }

        public void FillPetInfo(PetStable.PetInfo petInfo, ReactStates? forcedReactState = null)
        {
            petInfo.PetNumber = GetCharmInfo().GetPetNumber();
            petInfo.CreatureId = GetEntry();
            petInfo.DisplayId = GetNativeDisplayId();
            petInfo.Level = (byte)GetLevel();
            petInfo.Experience = m_unitData.PetExperience;
            petInfo.ReactState = forcedReactState.GetValueOrDefault(GetReactState());
            petInfo.Name = GetName();
            petInfo.WasRenamed = !HasPetFlag(UnitPetFlags.CanBeRenamed);
            petInfo.Health = (uint)GetHealth();
            petInfo.Mana = (uint)GetPower(PowerType.Mana);
            petInfo.ActionBar = GenerateActionBarData();
            petInfo.LastSaveTime = (uint)GameTime.GetGameTime();
            petInfo.CreatedBySpellId = m_unitData.CreatedBySpell;
            petInfo.Type = GetPetType();
            petInfo.SpecializationId = GetSpecialization();
        }
        
        public static void DeleteFromDB(uint petNumber)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELLS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public override void SetDeathState(DeathState s)
        {
            base.SetDeathState(s);
            if (GetDeathState() == DeathState.Corpse)
            {
                if (GetPetType() == PetType.Hunter)
                {
                    // pet corpse non lootable and non skinnable
                    ReplaceAllDynamicFlags(UnitDynFlags.None);
                    RemoveUnitFlag(UnitFlags.Skinnable);
                }
            }
            else if (GetDeathState() == DeathState.Alive)
            {
                CastPetAuras(true);
            }
        }

        public override void Update(uint diff)
        {
            if (m_removed)                                           // pet already removed, just wait in remove queue, no updates
                return;

            if (m_loading)
                return;

            switch (m_deathState)
            {
                case DeathState.Corpse:
                {
                    if (GetPetType() != PetType.Hunter || m_corpseRemoveTime <= GameTime.GetGameTime())
                    {
                        Remove(PetSaveMode.NotInSlot);               //hunters' pets never get removed because of death, NEVER!
                        return;
                    }
                    break;
                }
                case DeathState.Alive:
                {
                    // unsummon pet that lost owner
                    Player owner = GetOwner();
                    if (owner == null || (!IsWithinDistInMap(owner, GetMap().GetVisibilityRange()) && !IsPossessed()) || (IsControlled() && owner.GetPetGUID().IsEmpty()))
                    {
                        Remove(PetSaveMode.NotInSlot, true);
                        return;
                    }

                    if (IsControlled())
                    {
                        if (owner.GetPetGUID() != GetGUID())
                        {
                            Log.outError(LogFilter.Pet, $"Pet {GetEntry()} is not pet of owner {GetOwner().GetName()}, removed");
                            Cypher.Assert(GetPetType() != PetType.Hunter, $"Unexpected unlinked pet found for owner {owner.GetSession().GetPlayerInfo()}");
                            Remove(PetSaveMode.NotInSlot);
                            return;
                        }
                    }

                    if (m_duration > 0)
                    {
                        if (m_duration > diff)
                            m_duration -= (int)diff;
                        else
                        {
                            Remove(GetPetType() != PetType.Summon ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);
                            return;
                        }
                    }

                    //regenerate focus for hunter pets or energy for deathknight's ghoul
                    if (m_focusRegenTimer != 0)
                    {
                        if (m_focusRegenTimer > diff)
                            m_focusRegenTimer -= diff;
                        else
                        {
                            switch (GetPowerType())
                            {
                                case PowerType.Focus:
                                    Regenerate(PowerType.Focus);
                                    m_focusRegenTimer += PetFocusRegenInterval - diff;
                                    if (m_focusRegenTimer == 0)
                                        ++m_focusRegenTimer;

                                    // Reset if large diff (lag) causes focus to get 'stuck'
                                    if (m_focusRegenTimer > PetFocusRegenInterval)
                                        m_focusRegenTimer = PetFocusRegenInterval;
                                    break;
                                default:
                                    m_focusRegenTimer = 0;
                                    break;
                            }
                        }
                    }
                    break;
                }
                default:
                    break;
            }
            base.Update(diff);
        }

        public void Remove(PetSaveMode mode, bool returnreagent = false)
        {
            GetOwner().RemovePet(this, mode, returnreagent);
        }

        public void GivePetXP(uint xp)
        {
            if (GetPetType() != PetType.Hunter)
                return;

            if (xp < 1)
                return;

            if (!IsAlive())
                return;

            uint maxlevel = Math.Min(WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel), GetOwner().GetLevel());
            uint petlevel = GetLevel();

            // If pet is detected to be at, or above(?) the players level, don't hand out XP
            if (petlevel >= maxlevel)
                return;

            uint nextLvlXP = m_unitData.PetNextLevelExperience;
            uint curXP = m_unitData.PetExperience;
            uint newXP = curXP + xp;

            // Check how much XP the pet should receive, and hand off have any left from previous levelups
            while (newXP >= nextLvlXP && petlevel < maxlevel)
            {
                // Subtract newXP from amount needed for nextlevel, and give pet the level
                newXP -= nextLvlXP;
                ++petlevel;

                GivePetLevel((int)petlevel);

                nextLvlXP = m_unitData.PetNextLevelExperience;
            }
            // Not affected by special conditions - give it new XP
            SetPetExperience(petlevel < maxlevel ? newXP : 0);
        }

        public void GivePetLevel(int level)
        {
            if (level == 0 || level == GetLevel())
                return;

            if (GetPetType() == PetType.Hunter)
            {
                SetPetExperience(0);
                SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel((uint)level) * PetXPFactor));
            }

            InitStatsForLevel((uint)level);
            InitLevelupSpellsForLevel();
        }

        public bool CreateBaseAtCreature(Creature creature)
        {
            Cypher.Assert(creature != null);

            if (!CreateBaseAtTamed(creature.GetCreatureTemplate(), creature.GetMap()))
                return false;

            Relocate(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Pet, "Pet (guidlow {0}, entry {1}) not created base at creature. Suggested coordinates isn't valid (X: {2} Y: {3})",
                    GetGUID().ToString(), GetEntry(), GetPositionX(), GetPositionY());
                return false;
            }

            CreatureTemplate cinfo = GetCreatureTemplate();
            if (cinfo == null)
            {
                Log.outError(LogFilter.Pet, "CreateBaseAtCreature() failed, creatureInfo is missing!");
                return false;
            }

            SetDisplayId(creature.GetDisplayId());
            CreatureFamilyRecord cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cinfo.Family);
            if (cFamily != null)
                SetName(cFamily.Name[GetOwner().GetSession().GetSessionDbcLocale()]);
            else
                SetName(creature.GetName(Global.WorldMgr.GetDefaultDbcLocale()));

            return true;
        }

        public bool CreateBaseAtCreatureInfo(CreatureTemplate cinfo, Unit owner)
        {
            if (!CreateBaseAtTamed(cinfo, owner.GetMap()))
                return false;

            CreatureFamilyRecord cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cinfo.Family);
            if (cFamily != null)
                SetName(cFamily.Name[GetOwner().GetSession().GetSessionDbcLocale()]);

            Relocate(owner.GetPositionX(), owner.GetPositionY(), owner.GetPositionZ(), owner.GetOrientation());
            return true;
        }

        bool CreateBaseAtTamed(CreatureTemplate cinfo, Map map)
        {
            Log.outDebug(LogFilter.Pet, "CreateBaseForTamed");
            if (!Create(map.GenerateLowGuid(HighGuid.Pet), map, cinfo.Entry, Global.ObjectMgr.GeneratePetNumber()))
                return false;

            SetPetNameTimestamp(0);
            SetPetExperience(0);
            SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel(GetLevel() + 1) * PetXPFactor));
            ReplaceAllNpcFlags(NPCFlags.None);
            ReplaceAllNpcFlags2(NPCFlags2.None);

            if (cinfo.CreatureType == CreatureType.Beast)
            {
                SetClass(Class.Warrior);
                SetGender(Gender.None);
                SetPowerType(PowerType.Focus);
                SetSheath(SheathState.Melee);
                ReplaceAllPetFlags(UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned);
            }

            return true;
        }

        public bool HaveInDiet(ItemTemplate item)
        {
            if (item.FoodType == 0)
                return false;

            CreatureTemplate cInfo = GetCreatureTemplate();
            if (cInfo == null)
                return false;

            CreatureFamilyRecord cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cInfo.Family);
            if (cFamily == null)
                return false;

            uint diet = cFamily.PetFoodMask;
            uint FoodMask = (uint)(1 << ((int)item.FoodType - 1));
            return diet.HasAnyFlag(FoodMask);
        }

        void _LoadSpells(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    AddSpell(result.Read<uint>(0), (ActiveStates)result.Read<byte>(1), PetSpellState.Unchanged);
                }
                while (result.NextRow());
            }
        }

        void _SaveSpells(SQLTransaction trans)
        {
            foreach (var pair in m_spells.ToList())
            {
                // prevent saving family passives to DB
                if (pair.Value.type == PetSpellType.Family)
                    continue;

                PreparedStatement stmt;

                switch (pair.Value.state)
                {
                    case PetSpellState.Removed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        m_spells.Remove(pair.Key);
                        continue;
                    case PetSpellState.Changed:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, (byte)pair.Value.active);
                        trans.Append(stmt);
                        break;
                    case PetSpellState.New:
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, (byte)pair.Value.active);
                        trans.Append(stmt);
                        break;
                    case PetSpellState.Unchanged:
                        continue;
                }
                pair.Value.state = PetSpellState.Unchanged;
            }
        }

        void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Pet, "Loading auras for {0}", GetGUID().ToString());

            ObjectGuid casterGuid = default;
            ObjectGuid itemGuid = default;
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();

            if (!effectResult.IsEmpty())
            {
                do
                {
                    uint effectIndex = effectResult.Read<byte>(3);
                    if (effectIndex < SpellConst.MaxEffects)
                    {
                        casterGuid.SetRawValue(effectResult.Read<byte[]>(0));
                        if (casterGuid.IsEmpty())
                            casterGuid = GetGUID();

                        AuraKey key = new(casterGuid, itemGuid, effectResult.Read<uint>(1), effectResult.Read<uint>(2));
                        if (!effectInfo.ContainsKey(key))
                            effectInfo[key] = new AuraLoadEffectInfo();

                        var info = effectInfo[key];
                        info.Amounts[effectIndex] = effectResult.Read<int>(4);
                        info.BaseAmounts[effectIndex] = effectResult.Read<int>(5);
                    }
                } while (effectResult.NextRow());
            }

            if (!auraResult.IsEmpty())
            {
                do
                {
                    // NULL guid stored - pet is the caster of the spell - see Pet._SaveAuras
                    casterGuid.SetRawValue(auraResult.Read<byte[]>(0));
                    if (casterGuid.IsEmpty())
                        casterGuid = GetGUID();

                    AuraKey key = new(casterGuid, itemGuid, auraResult.Read<uint>(1), auraResult.Read<uint>(2));
                    uint recalculateMask = auraResult.Read<uint>(3);
                    Difficulty difficulty = (Difficulty)auraResult.Read<byte>(4);
                    byte stackCount = auraResult.Read<byte>(5);
                    int maxDuration = auraResult.Read<int>(6);
                    int remainTime = auraResult.Read<int>(7);
                    byte remainCharges = auraResult.Read<byte>(8);

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId, difficulty);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Pet, "Pet._LoadAuras: Unknown aura (spellid {0}), ignore.", key.SpellId);
                        continue;
                    }

                    if (difficulty != Difficulty.None && !CliDB.DifficultyStorage.ContainsKey(difficulty))
                    {
                        Log.outError(LogFilter.Pet, $"Pet._LoadAuras: Unknown difficulty {difficulty} (spellid {key.SpellId}), ignore.");
                        continue;
                    }

                    // negative effects should continue counting down after logout
                    if (remainTime != -1 && (!spellInfo.IsPositive() || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
                    {
                        if (remainTime / Time.InMilliseconds <= timediff)
                            continue;

                        remainTime -= (int)timediff * Time.InMilliseconds;
                    }

                    // prevent wrong values of remaincharges
                    if (spellInfo.ProcCharges != 0)
                    {
                        if (remainCharges <= 0)
                            remainCharges = (byte)spellInfo.ProcCharges;
                    }
                    else
                        remainCharges = 0;

                    var info = effectInfo[key];
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));

                    AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask, this);
                    createInfo.SetCasterGUID(casterGuid);
                    createInfo.SetBaseAmount(info.BaseAmounts);
                    createInfo.SetStackAmount(stackCount);

                    Aura aura = Aura.TryCreate(createInfo);
                    if (aura != null)
                    {
                        if (!aura.CanBeSaved())
                        {
                            aura.Remove();
                            continue;
                        }
                        aura.SetLoadedState(maxDuration, remainTime, remainCharges, recalculateMask, info.Amounts);
                        aura.ApplyForTargets();
                        Log.outInfo(LogFilter.Pet, "Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
                    }
                }
                while (auraResult.NextRow());
            }
        }

        void _SaveAuras(SQLTransaction trans)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            byte index;
            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;

                // check if the aura has to be saved
                if (!aura.CanBeSaved() || IsPetAura(aura))
                    continue;

                uint recalculateMask;
                AuraKey key = aura.GenerateKey(out recalculateMask);

                // don't save guid of caster in case we are caster of the spell - guid for pet is generated every pet load, so it won't match saved guid anyways
                if (key.Caster == GetGUID())
                    key.Caster.Clear();

                index = 0;
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_AURA);
                stmt.AddValue(index++, GetCharmInfo().GetPetNumber());
                stmt.AddValue(index++, key.Caster.GetRawValue());
                stmt.AddValue(index++, key.SpellId);
                stmt.AddValue(index++, key.EffectMask);
                stmt.AddValue(index++, recalculateMask);
                stmt.AddValue(index++, (byte)aura.GetCastDifficulty());
                stmt.AddValue(index++, aura.GetStackAmount());
                stmt.AddValue(index++, aura.GetMaxDuration());
                stmt.AddValue(index++, aura.GetDuration());
                stmt.AddValue(index++, aura.GetCharges());
                trans.Append(stmt);

                foreach (AuraEffect effect in aura.GetAuraEffects())
                {
                    if (effect != null)
                    {
                        index = 0;
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PET_AURA_EFFECT);
                        stmt.AddValue(index++, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(index++, key.Caster.GetRawValue());
                        stmt.AddValue(index++, key.SpellId);
                        stmt.AddValue(index++, key.EffectMask);
                        stmt.AddValue(index++, effect.GetEffIndex());
                        stmt.AddValue(index++, effect.GetAmount());
                        stmt.AddValue(index++, effect.GetBaseAmount());
                        trans.Append(stmt);
                    }
                }
            }
        }

        bool AddSpell(uint spellId, ActiveStates active = ActiveStates.Decide, PetSpellState state = PetSpellState.New, PetSpellType type = PetSpellType.Normal)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo == null)
            {
                // do pet spell book cleanup
                if (state == PetSpellState.Unchanged)                    // spell load case
                {
                    Log.outError(LogFilter.Pet, "addSpell: Non-existed in SpellStore spell #{0} request, deleting for all pets in `pet_spell`.", spellId);

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_PET_SPELL);

                    stmt.AddValue(0, spellId);

                    DB.Characters.Execute(stmt);
                }
                else
                    Log.outError(LogFilter.Pet, "addSpell: Non-existed in SpellStore spell #{0} request.", spellId);

                return false;
            }

            var petSpell = m_spells.LookupByKey(spellId);
            if (petSpell != null)
            {
                if (petSpell.state == PetSpellState.Removed)
                    state = PetSpellState.Changed;
                else
                {
                    if (state == PetSpellState.Unchanged && petSpell.state != PetSpellState.Unchanged)
                    {
                        // can be in case spell loading but learned at some previous spell loading
                        petSpell.state = PetSpellState.Unchanged;

                        if (active == ActiveStates.Enabled)
                            ToggleAutocast(spellInfo, true);
                        else if (active == ActiveStates.Disabled)
                            ToggleAutocast(spellInfo, false);

                        return false;
                    }
                }
            }

            PetSpell newspell = new();
            newspell.state = state;
            newspell.type = type;

            if (active == ActiveStates.Decide)                               // active was not used before, so we save it's autocast/passive state here
            {
                if (!spellInfo.IsAutocastable())
                    newspell.active = ActiveStates.Passive;
                else if (spellInfo.IsAutocastEnabledByDefault())
                    newspell.active = ActiveStates.Enabled;
                else
                    newspell.active = ActiveStates.Disabled;
            }
            else
                newspell.active = active;

            // talent: unlearn all other talent ranks (high and low)
            if (spellInfo.IsRanked())
            {
                foreach (var pair in m_spells)
                {
                    if (pair.Value.state == PetSpellState.Removed)
                        continue;

                    SpellInfo oldRankSpellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

                    if (oldRankSpellInfo == null)
                        continue;

                    if (spellInfo.IsDifferentRankOf(oldRankSpellInfo))
                    {
                        // replace by new high rank
                        if (spellInfo.IsHighRankOf(oldRankSpellInfo))
                        {
                            newspell.active = pair.Value.active;

                            if (newspell.active == ActiveStates.Enabled)
                                ToggleAutocast(oldRankSpellInfo, false);

                            UnlearnSpell(pair.Key, false, false);
                            break;
                        }
                        // ignore new lesser rank
                        else
                            return false;
                    }
                }
            }

            m_spells[spellId] = newspell;

            if (spellInfo.IsPassive() && (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState)))
                CastSpell(this, spellId, true);
            else
                GetCharmInfo().AddSpellToActionBar(spellInfo);

            if (newspell.active == ActiveStates.Enabled)
                ToggleAutocast(spellInfo, true);

            return true;
        }

        public bool LearnSpell(uint spellId)
        {
            // prevent duplicated entires in spell book
            if (!AddSpell(spellId))
                return false;

            if (!m_loading)
            {
                PetLearnedSpells packet = new();
                packet.Spells.Add(spellId);
                GetOwner().SendPacket(packet);
                GetOwner().PetSpellInitialize();
            }
            return true;
        }

        void LearnSpells(List<uint> spellIds)
        {
            PetLearnedSpells packet = new();

            foreach (uint spell in spellIds)
            {
                if (!AddSpell(spell))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!m_loading)
                GetOwner().SendPacket(packet);
        }

        void InitLevelupSpellsForLevel()
        {
            uint level = GetLevel();
            var levelupSpells = GetCreatureTemplate().Family != 0 ? Global.SpellMgr.GetPetLevelupSpellList(GetCreatureTemplate().Family) : null;
            if (levelupSpells != null)
            {
                // PetLevelupSpellSet ordered by levels, process in reversed order
                foreach (var pair in levelupSpells)
                {
                    // will called first if level down
                    if (pair.Key > level)
                        UnlearnSpell(pair.Value, true);                 // will learn prev rank if any
                    // will called if level up
                    else
                        LearnSpell(pair.Value);                        // will unlearn prev rank if any
                }
            }

            // default spells (can be not learned if pet level (as owner level decrease result for example) less first possible in normal game)
            PetDefaultSpellsEntry defSpells = Global.SpellMgr.GetPetDefaultSpellsEntry((int)GetEntry());
            if (defSpells != null)
            {
                foreach (uint spellId in defSpells.spellid)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
                    if (spellInfo == null)
                        continue;

                    // will called first if level down
                    if (spellInfo.SpellLevel > level)
                        UnlearnSpell(spellInfo.Id, true);
                    // will called if level up
                    else
                        LearnSpell(spellInfo.Id);
                }
            }
        }

        bool UnlearnSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
        {
            if (RemoveSpell(spellId, learnPrev, clearActionBar))
            {
                if (!m_loading)
                {
                    PetUnlearnedSpells packet = new();
                    packet.Spells.Add(spellId);
                    GetOwner().SendPacket(packet);
                }
                return true;
            }
            return false;
        }

        void UnlearnSpells(List<uint> spellIds, bool learnPrev, bool clearActionBar)
        {
            PetUnlearnedSpells packet = new();

            foreach (uint spell in spellIds)
            {
                if (!RemoveSpell(spell, learnPrev, clearActionBar))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!m_loading)
                GetOwner().SendPacket(packet);
        }

        public bool RemoveSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
        {
            var petSpell = m_spells.LookupByKey(spellId);
            if (petSpell == null)
                return false;

            if (petSpell.state == PetSpellState.Removed)
                return false;

            if (petSpell.state == PetSpellState.New)
                m_spells.Remove(spellId);
            else
                petSpell.state = PetSpellState.Removed;

            RemoveAurasDueToSpell(spellId);

            if (learnPrev)
            {
                uint prev_id = Global.SpellMgr.GetPrevSpellInChain(spellId);
                if (prev_id != 0)
                    LearnSpell(prev_id);
                else
                    learnPrev = false;
            }

            // if remove last rank or non-ranked then update action bar at server and client if need
            if (clearActionBar && !learnPrev && GetCharmInfo().RemoveSpellFromActionBar(spellId))
            {
                if (!m_loading)
                {
                    // need update action bar for last removed rank
                    Unit owner = GetOwner();
                    if (owner != null)
                        if (owner.IsTypeId(TypeId.Player))
                            owner.ToPlayer().PetSpellInitialize();
                }
            }

            return true;
        }

        void CleanupActionBar()
        {
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                UnitActionBarEntry ab = GetCharmInfo().GetActionBarEntry(i);
                if (ab != null)
                    if (ab.GetAction() != 0 && ab.IsActionBarForSpell())
                    {
                        if (!HasSpell(ab.GetAction()))
                            GetCharmInfo().SetActionBar(i, 0, ActiveStates.Passive);
                        else if (ab.GetActiveState() == ActiveStates.Enabled)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ab.GetAction(), Difficulty.None);
                            if (spellInfo != null)
                                ToggleAutocast(spellInfo, true);
                        }
                    }
            }
        }

        public void InitPetCreateSpells()
        {
            GetCharmInfo().InitPetActionBar();
            m_spells.Clear();

            LearnPetPassives();
            InitLevelupSpellsForLevel();

            CastPetAuras(false);
        }

        public void ToggleAutocast(SpellInfo spellInfo, bool apply)
        {
            if (!spellInfo.IsAutocastable())
                return;

            var petSpell = m_spells.LookupByKey(spellInfo.Id);
            if (petSpell == null)
                return;

            var hasSpell = m_autospells.Contains(spellInfo.Id);

            if (apply)
            {
                if (!hasSpell)
                {
                    m_autospells.Add(spellInfo.Id);

                    if (petSpell.active != ActiveStates.Enabled)
                    {
                        petSpell.active = ActiveStates.Enabled;
                        if (petSpell.state != PetSpellState.New)
                            petSpell.state = PetSpellState.Changed;
                    }
                }
            }
            else
            {
                if (hasSpell)
                {
                    m_autospells.Remove(spellInfo.Id);
                    if (petSpell.active != ActiveStates.Disabled)
                    {
                        petSpell.active = ActiveStates.Disabled;
                        if (petSpell.state != PetSpellState.New)
                            petSpell.state = PetSpellState.Changed;
                    }
                }
            }
        }

        public bool IsPermanentPetFor(Player owner)
        {
            switch (GetPetType())
            {
                case PetType.Summon:
                    switch (owner.GetClass())
                    {
                        case Class.Warlock:
                            return GetCreatureTemplate().CreatureType == CreatureType.Demon;
                        case Class.DeathKnight:
                            return GetCreatureTemplate().CreatureType == CreatureType.Undead;
                        case Class.Mage:
                            return GetCreatureTemplate().CreatureType == CreatureType.Elemental;
                        default:
                            return false;
                    }
                case PetType.Hunter:
                    return true;
                default:
                    return false;
            }
        }

        public bool Create(ulong guidlow, Map map, uint entry, uint petNumber)
        {
            Cypher.Assert(map != null);
            SetMap(map);

            // TODO: counter should be constructed as (summon_count << 32) | petNumber
            _Create(ObjectGuid.Create(HighGuid.Pet, map.GetId(), entry, guidlow));

            m_spawnId = guidlow;
            m_originalEntry = entry;

            if (!InitEntry(entry))
                return false;

            // Force regen flag for player pets, just like we do for players themselves
            SetUnitFlag2(UnitFlags2.RegeneratePower);
            SetSheath(SheathState.Melee);

            GetThreatManager().Initialize();

            return true;
        }

        public override bool HasSpell(uint spell)
        {
            var petSpell = m_spells.LookupByKey(spell);
            return petSpell != null && petSpell.state != PetSpellState.Removed;
        }

        // Get all passive spells in our skill line
        void LearnPetPassives()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();
            if (cInfo == null)
                return;

            CreatureFamilyRecord cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cInfo.Family);
            if (cFamily == null)
                return;

            var petStore = Global.SpellMgr.PetFamilySpellsStorage.LookupByKey(cInfo.Family);
            if (petStore != null)
            {
                // For general hunter pets skill 270
                // Passive 01~10, Passive 00 (20782, not used), Ferocious Inspiration (34457)
                // Scale 01~03 (34902~34904, bonus from owner, not used)
                foreach (var spellId in petStore)
                    AddSpell(spellId, ActiveStates.Decide, PetSpellState.New, PetSpellType.Family);
            }
        }

        void CastPetAuras(bool current)
        {
            Player owner = GetOwner();

            if (!IsPermanentPetFor(owner))
                return;

            foreach (var pa in owner.m_petAuras)
            {
                if (!current && pa.IsRemovedOnChangePet())
                    owner.RemovePetAura(pa);
                else
                    CastPetAura(pa);
            }
        }

        public void CastPetAura(PetAura aura)
        {
            uint auraId = aura.GetAura(GetEntry());
            if (auraId == 0)
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

            if (auraId == 35696)                                      // Demonic Knowledge
                args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(aura.GetDamage(), GetStat(Stats.Stamina) + GetStat(Stats.Intellect)));

            CastSpell(this, auraId, args);
        }

        bool IsPetAura(Aura aura)
        {
            Player owner = GetOwner();

            // if the owner has that pet aura, return true
            foreach (var petAura in owner.m_petAuras)
                if (petAura.GetAura(GetEntry()) == aura.GetId())
                    return true;

            return false;
        }

        void LearnSpellHighRank(uint spellid)
        {
            LearnSpell(spellid);
            uint next = Global.SpellMgr.GetNextSpellInChain(spellid);
            if (next != 0)
                LearnSpellHighRank(next);
        }

        public void SynchronizeLevelWithOwner()
        {
            Unit owner = GetOwner();
            if (owner == null || !owner.IsTypeId(TypeId.Player))
                return;

            switch (GetPetType())
            {
                // always same level
                case PetType.Summon:
                case PetType.Hunter:
                    GivePetLevel((int)owner.GetLevel());
                    break;
                default:
                    break;
            }
        }

        public new Player GetOwner()
        {
            return base.GetOwner().ToPlayer();
        }

        public override float GetNativeObjectScale()
        {
            var creatureFamily = CliDB.CreatureFamilyStorage.LookupByKey(GetCreatureTemplate().Family);
            if (creatureFamily != null && creatureFamily.MinScale > 0.0f && GetPetType() == PetType.Hunter)
            {
                float scale;
                if (GetLevel() >= creatureFamily.MaxScaleLevel)
                    scale = creatureFamily.MaxScale;
                else if (GetLevel() <= creatureFamily.MinScaleLevel)
                    scale = creatureFamily.MinScale;
                else
                    scale = creatureFamily.MinScale + (float)(GetLevel() - creatureFamily.MinScaleLevel) / creatureFamily.MaxScaleLevel * (creatureFamily.MaxScale - creatureFamily.MinScale);

                return scale;
            }

            return base.GetNativeObjectScale();
        }

        public override void SetDisplayId(uint modelId, bool setNative = false)
        {
            base.SetDisplayId(modelId, setNative);

            if (!IsControlled())
                return;

            SetGroupUpdateFlag(GroupUpdatePetFlags.ModelId);
        }

        public PetType GetPetType() { return m_petType; }
        public void SetPetType(PetType type) { m_petType = type; }
        public bool IsControlled() { return GetPetType() == PetType.Summon || GetPetType() == PetType.Hunter; }
        public bool IsTemporarySummoned() { return m_duration > 0; }

        public override bool IsLoading() { return m_loading; }

        public override int GetPetAutoSpellSize() { return m_autospells.Count; }
        public override uint GetPetAutoSpellOnPos(byte pos)
        {
            if (pos >= m_autospells.Count)
                return 0;
            else
                return m_autospells[pos];
        }

        public void SetDuration(uint dur) { m_duration = (int)dur; }
        public int GetDuration() { return m_duration; }

        public void SetPetExperience(uint xp) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetExperience), xp); }
        public void SetPetNextLevelExperience(uint xp) { SetUpdateFieldValue(m_values.ModifyValue(m_unitData).ModifyValue(m_unitData.PetNextLevelExperience), xp); }

        public ushort GetSpecialization() { return m_petSpecialization; }

        public GroupUpdatePetFlags GetGroupUpdateFlag() { return m_groupUpdateMask; }
        public void SetGroupUpdateFlag(GroupUpdatePetFlags flag)
        {
            if (GetOwner().GetGroup() != null)
            {
                m_groupUpdateMask |= flag;
                GetOwner().SetGroupUpdateFlag(GroupUpdateFlags.Pet);
            }
        }
        public void ResetGroupUpdateFlag()
        {
            m_groupUpdateMask = GroupUpdatePetFlags.None;
            if (GetOwner().GetGroup() != null)
                GetOwner().RemoveGroupUpdateFlag(GroupUpdateFlags.Pet);
        }

        void LearnSpecializationSpells()
        {
            List<uint> learnedSpells = new();

            List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(m_petSpecialization);
            if (specSpells != null)
            {
                foreach (var specSpell in specSpells)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID, Difficulty.None);
                    if (spellInfo == null || spellInfo.SpellLevel > GetLevel())
                        continue;

                    learnedSpells.Add(specSpell.SpellID);
                }
            }

            LearnSpells(learnedSpells);
        }

        void RemoveSpecializationSpells(bool clearActionBar)
        {
            List<uint> unlearnedSpells = new();

            for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                ChrSpecializationRecord specialization = Global.DB2Mgr.GetChrSpecializationByIndex(0, i);
                if (specialization != null)
                {
                    List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization.Id);
                    if (specSpells != null)
                    {
                        foreach (var specSpell in specSpells)
                            unlearnedSpells.Add(specSpell.SpellID);
                    }
                }

                ChrSpecializationRecord specialization1 = Global.DB2Mgr.GetChrSpecializationByIndex(Class.Max, i);
                if (specialization1 != null)
                {
                    List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization1.Id);
                    if (specSpells != null)
                    {
                        foreach (var specSpell in specSpells)
                            unlearnedSpells.Add(specSpell.SpellID);
                    }
                }
            }

            UnlearnSpells(unlearnedSpells, true, clearActionBar);
        }

        public void SetSpecialization(uint spec)
        {
            if (m_petSpecialization == spec)
                return;

            // remove all the old spec's specalization spells, set the new spec, then add the new spec's spells
            // clearActionBars is false because we'll be updating the pet actionbar later so we don't have to do it now
            RemoveSpecializationSpells(false);
            if (!CliDB.ChrSpecializationStorage.ContainsKey(spec))
            {
                m_petSpecialization = 0;
                return;
            }

            m_petSpecialization = (ushort)spec;
            LearnSpecializationSpells();

            // resend SMSG_PET_SPELLS_MESSAGE to remove old specialization spells from the pet action bar
            CleanupActionBar();
            GetOwner().PetSpellInitialize();

            SetPetSpecialization setPetSpecialization = new();
            setPetSpecialization.SpecID = m_petSpecialization;
            GetOwner().SendPacket(setPetSpecialization);
        }

        string GenerateActionBarData()
        {
            StringBuilder ss = new();

            for (byte i = SharedConst.ActionBarIndexStart; i < SharedConst.ActionBarIndexEnd; ++i)
            {
                ss.AppendFormat("{0} {1} ", (uint)GetCharmInfo().GetActionBarEntry(i).GetActiveState(), (uint)GetCharmInfo().GetActionBarEntry(i).GetAction());
            }

            return ss.ToString();
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nPetType: {GetPetType()} PetNumber: {GetCharmInfo().GetPetNumber()}";
        }

        public DeclinedName GetDeclinedNames() { return _declinedname; }

        public new Dictionary<uint, PetSpell> m_spells = new();
        List<uint> m_autospells = new();
        public bool m_removed;

        PetType m_petType;
        int m_duration;                                 // time until unsummon (used mostly for summoned guardians and not used for controlled pets)
        bool m_loading;
        uint m_focusRegenTimer;
        GroupUpdatePetFlags m_groupUpdateMask;

        DeclinedName _declinedname;
        ushort m_petSpecialization;
    }
    public class PetSpell
    {
        public ActiveStates active;
        public PetSpellState state;
        public PetSpellType type;
    }

    public class PetStable
    {
        static uint UnslottedPetIndexMask = 0x80000000;

        public class PetInfo
        {
            public string Name;
            public string ActionBar;
            public uint PetNumber;
            public uint CreatureId;
            public uint DisplayId;
            public uint Experience;
            public uint Health;
            public uint Mana;
            public uint LastSaveTime;
            public uint CreatedBySpellId;
            public ushort SpecializationId;
            public byte Level = 0;
            public ReactStates ReactState;
            public PetType Type = PetType.Max;
            public bool WasRenamed;
        }

        public uint? CurrentPetIndex;                                   // index into ActivePets or UnslottedPets if highest bit is set
        public PetInfo[] ActivePets = new PetInfo[SharedConst.MaxActivePets];      // PET_SAVE_FIRST_ACTIVE_SLOT - PET_SAVE_LAST_ACTIVE_SLOT
        public PetInfo[] StabledPets = new PetInfo[SharedConst.MaxPetStables];     // PET_SAVE_FIRST_STABLE_SLOT - PET_SAVE_LAST_STABLE_SLOT
        public List<PetInfo> UnslottedPets = new();                             // PET_SAVE_NOT_IN_SLOT

        public PetInfo GetCurrentPet()
        {
            if (!CurrentPetIndex.HasValue)
                return null;

            uint? activePetIndex = GetCurrentActivePetIndex();
            if (activePetIndex.HasValue)
                return ActivePets[activePetIndex.Value] != null ? ActivePets[activePetIndex.Value] : null;

            uint? unslottedPetIndex = GetCurrentUnslottedPetIndex();
            if (unslottedPetIndex.HasValue)
                return unslottedPetIndex < UnslottedPets.Count ? UnslottedPets[(int)unslottedPetIndex.Value] : null;

            return null;
        }

        public uint? GetCurrentActivePetIndex() { return CurrentPetIndex.HasValue && ((CurrentPetIndex & UnslottedPetIndexMask) == 0) ? CurrentPetIndex : null; }
        public void SetCurrentActivePetIndex(uint index) { CurrentPetIndex = index; }
        uint? GetCurrentUnslottedPetIndex() { return CurrentPetIndex.HasValue && ((CurrentPetIndex & UnslottedPetIndexMask) != 0) ? (CurrentPetIndex & ~UnslottedPetIndexMask) : null; }
        public void SetCurrentUnslottedPetIndex(uint index) { CurrentPetIndex = index | UnslottedPetIndexMask; }
    }
    
    public enum ActiveStates
    {
        Passive = 0x01,                                    // 0x01 - passive
        Disabled = 0x81,                                    // 0x80 - castable
        Enabled = 0xC1,                                    // 0x40 | 0x80 - auto cast + castable
        Command = 0x07,                                    // 0x01 | 0x02 | 0x04
        Reaction = 0x06,                                    // 0x02 | 0x04
        Decide = 0x00                                     // custom
    }

    enum PetLoginQueryLoad
    {
        DeclinedNames,
        Auras,
        AuraEffects,
        Spells,
        Cooldowns,
        Charges
    }

    class PetLoadQueryHolder : SQLQueryHolder<PetLoginQueryLoad>
    {
        public PetLoadQueryHolder(ulong ownerGuid, uint petNumber)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
            stmt.AddValue(0, ownerGuid);
            stmt.AddValue(1, petNumber);
            SetQuery(PetLoginQueryLoad.DeclinedNames, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Auras, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.AuraEffects, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Spells, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Cooldowns, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
            stmt.AddValue(0, petNumber);
            SetQuery(PetLoginQueryLoad.Charges, stmt);
        }
    }
}
