// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Maps;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
    public class Pet : Guardian
    {
        private const int PET_FOCUS_REGEN_INTERVAL = 4 * Time.InMilliseconds;
        private const int HAPPINESS_LEVEL_SIZE = 333000;
        private const float PET_XP_FACTOR = 0.05f;
        private readonly List<uint> _autospells = new();

        private DeclinedName _declinedname;
        private int _duration; // Time until unsummon (used mostly for summoned guardians and not used for controlled pets)
        private uint _focusRegenTimer;
        private GroupUpdatePetFlags _groupUpdateMask;
        private bool _loading;
        private ushort _petSpecialization;

        private PetType _petType;

        public Pet(Player owner, PetType type = PetType.Max) : base(null, owner, true)
        {
            _petType = type;

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
            _focusRegenTimer = PET_FOCUS_REGEN_INTERVAL;
        }

        public bool Removed { get; set; }

        public Dictionary<uint, PetSpell> PetSpells { get; set; } = new();

        public override void Dispose()
        {
            _declinedname = null;
            base.Dispose();
        }

        public override void AddToWorld()
        {
            //- Register the pet for Guid lookup
            if (!IsInWorld)
            {
                // Register the pet for Guid lookup
                base.AddToWorld();
                InitializeAI();
                ZoneScript zoneScript = GetZoneScript() != null ? GetZoneScript() : GetInstanceScript();

                zoneScript?.OnCreatureCreate(this);
            }

            // Prevent stuck pets when zoning. Pets default to "follow" when added to world
            // so we'll reset Flags and let the AI handle things
            if (GetCharmInfo() != null &&
                GetCharmInfo().HasCommandState(CommandStates.Follow))
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
                    if (stable.ActivePets[activeSlot] != null &&
                        stable.ActivePets[activeSlot].PetNumber == petnumber)
                        return Tuple.Create(stable.ActivePets[activeSlot], PetSaveMode.FirstActiveSlot + activeSlot);

                for (var stableSlot = 0; stableSlot < stable.StabledPets.Length; ++stableSlot)
                    if (stable.StabledPets[stableSlot] != null &&
                        stable.StabledPets[stableSlot].PetNumber == petnumber)
                        return Tuple.Create(stable.StabledPets[stableSlot], PetSaveMode.FirstStableSlot + stableSlot);

                foreach (var pet in stable.UnslottedPets)
                    if (pet.PetNumber == petnumber)
                        return Tuple.Create(pet, PetSaveMode.NotInSlot);
            }
            else if (slot.HasValue)
            {
                // Current pet
                if (slot == PetSaveMode.AsCurrent)
                    if (stable.GetCurrentActivePetIndex().HasValue &&
                        stable.ActivePets[stable.GetCurrentActivePetIndex().Value] != null)
                        return Tuple.Create(stable.ActivePets[stable.GetCurrentActivePetIndex().Value], (PetSaveMode)stable.GetCurrentActivePetIndex());

                if (slot >= PetSaveMode.FirstActiveSlot &&
                    slot < PetSaveMode.LastActiveSlot)
                    if (stable.ActivePets[(int)slot.Value] != null)
                        return Tuple.Create(stable.ActivePets[(int)slot.Value], slot.Value);

                if (slot >= PetSaveMode.FirstStableSlot &&
                    slot < PetSaveMode.LastStableSlot)
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
            _loading = true;

            PetStable petStable = owner.GetPetStable();

            ulong ownerid = owner.GetGUID().GetCounter();
            (PetStable.PetInfo petInfo, PetSaveMode slot) = GetLoadPetInfo(petStable, petEntry, petnumber, forcedSlot);

            if (petInfo == null ||
                (slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot))
            {
                _loading = false;

                return false;
            }

            // Don't try to reload the current pet
            if (petStable.GetCurrentPet() != null &&
                owner.GetPet() != null &&
                petStable.GetCurrentPet().PetNumber == petInfo.PetNumber)
                return false;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(petInfo.CreatedBySpellId, owner.GetMap().GetDifficultyID());

            bool isTemporarySummon = spellInfo != null && spellInfo.GetDuration() > 0;

            if (current && isTemporarySummon)
                return false;

            if (petInfo.Type == PetType.Hunter)
            {
                CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(petInfo.CreatureId);

                if (creatureInfo == null ||
                    !creatureInfo.IsTameable(owner.CanTameExoticPets()))
                    return false;
            }

            if (current && owner.IsPetNeedBeTemporaryUnsummoned())
            {
                owner.SetTemporaryUnsummonedPetNumber(petInfo.PetNumber);

                return false;
            }

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
                    Log.outError(LogFilter.Pet,
                                 "Pet (guidlow {0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})",
                                 GetGUID().ToString(),
                                 GetEntry(),
                                 GetPositionX(),
                                 GetPositionY());

                    return false;
                }

                map.AddToMap(ToCreature());

                return true;
            }

            GetCharmInfo().SetPetNumber(petInfo.PetNumber, IsPermanentPetFor(owner));

            SetDisplayId(petInfo.DisplayId);
            SetNativeDisplayId(petInfo.DisplayId);
            uint petlevel = petInfo.Level;
            ReplaceAllNpcFlags(NPCFlags.None);
            ReplaceAllNpcFlags2(NPCFlags2.None);
            SetName(petInfo.Name);

            switch (GetPetType())
            {
                case PetType.Summon:
                    petlevel = owner.GetLevel();

                    SetClass(Class.Mage);
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
                        Log.outError(LogFilter.Pet, "Pet have incorrect Type ({0}) for pet loading.", GetPetType());

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

            if (GetPetType() == PetType.Summon &&
                !current) //all (?) summon pets come with full health when called, but not when they are current
            {
                SetFullPower(PowerType.Mana);
            }
            else
            {
                uint savedhealth = petInfo.Health;
                uint savedmana = petInfo.Mana;

                if (savedhealth == 0 &&
                    GetPetType() == PetType.Hunter)
                {
                    SetDeathState(DeathState.JustDied);
                }
                else
                {
                    SetHealth(savedhealth);
                    SetPower(PowerType.Mana, (int)savedmana);
                }
            }

            // set current pet as current
            // 0-4=current
            // PET_SAVE_NOT_IN_SLOT(-1) = not stable Slot (summoning))
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
            else if (PetSaveMode.FirstActiveSlot <= slot &&
                     slot <= PetSaveMode.LastActiveSlot)
            {
                var activePetIndex = Array.FindIndex(petStable.ActivePets, pet => pet?.PetNumber == petnumber);

                Cypher.Assert(activePetIndex != -1);

                // Check that we either have no pet (unsummoned by player) or it matches temporarily unsummoned pet by server (for example on flying Mount)
                Cypher.Assert(!petStable.CurrentPetIndex.HasValue || petStable.CurrentPetIndex == activePetIndex);

                petStable.SetCurrentActivePetIndex((uint)activePetIndex);
            }

            // Send fake summon spell cast - this is needed for correct cooldown application for spells
            // Example: 46584 - without this cooldown (which should be set always when pet is loaded) isn't set clientside
            // @todo pets should be summoned from real cast instead of just faking it?
            if (petInfo.CreatedBySpellId != 0)
            {
                SpellGo spellGo = new();
                SpellCastData castData = spellGo.Cast;

                castData.CasterGUID = owner.GetGUID();
                castData.CasterUnit = owner.GetGUID();
                castData.CastID = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, owner.GetMapId(), petInfo.CreatedBySpellId, map.GenerateLowGuid(HighGuid.Cast));
                castData.SpellID = (int)petInfo.CreatedBySpellId;
                castData.CastFlags = SpellCastFlags.Unk9;
                castData.CastTime = Time.GetMSTime();
                owner.SendMessageToSet(spellGo, true);
            }

            owner.SetMinion(this, true);

            if (!isTemporarySummon)
                GetCharmInfo().LoadPetActionBar(petInfo.ActionBar);

            map.AddToMap(ToCreature());

            //set last used pet number (for use in BG's)
            if (owner.IsPlayer() &&
                IsControlled() &&
                !IsTemporarySummoned() &&
                (GetPetType() == PetType.Summon || GetPetType() == PetType.Hunter))
                owner.ToPlayer().SetLastPetNumber(petInfo.PetNumber);

            var session = owner.GetSession();
            var lastSaveTime = petInfo.LastSaveTime;
            var specializationId = petInfo.SpecializationId;

            owner.GetSession()
                 .AddQueryHolderCallback(DB.Characters.DelayQueryHolder(new PetLoadQueryHolder(ownerid, petInfo.PetNumber)))
                 .AfterComplete(holder =>
                                {
                                    if (session.GetPlayer() != owner ||
                                        owner.GetPet() != this)
                                        return;

                                    // passing previous checks ensure that 'this' is still valid
                                    if (Removed)
                                        return;

                                    uint timediff = (uint)(GameTime.GetGameTime() - lastSaveTime);
                                    _LoadAuras(holder.GetResult(PetLoginQueryLoad.Auras), holder.GetResult(PetLoginQueryLoad.AuraEffects), timediff);

                                    // load Action bar, if _data broken will fill later by default spells.
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
                                        CleanupActionBar(); // remove unknown spells from Action bar after load

                                        owner.PetSpellInitialize();
                                    }


                                    SetGroupUpdateFlag(GroupUpdatePetFlags.Full);

                                    if (GetPetType() == PetType.Hunter)
                                    {
                                        SQLResult result = holder.GetResult(PetLoginQueryLoad.DeclinedNames);

                                        if (!result.IsEmpty())
                                        {
                                            _declinedname = new DeclinedName();

                                            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                                                _declinedname.Name[i] = result.Read<string>(i);
                                        }
                                    }

                                    // must be after SetMinion (owner Guid check)
                                    LoadTemplateImmunities();
                                    _loading = false;
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
            if (mode == PetSaveMode.AsCurrent &&
                owner.GetTemporaryUnsummonedPetNumber() != 0 &&
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
            // save Auras before possibly removing them    
            _SaveAuras(trans);

            if (mode == PetSaveMode.AsCurrent)
            {
                var activeSlot = owner.GetPetStable().GetCurrentActivePetIndex();

                if (activeSlot.HasValue)
                    mode = (PetSaveMode)activeSlot;
            }

            // stable and not in Slot saves
            if (mode < PetSaveMode.FirstActiveSlot ||
                mode >= PetSaveMode.LastActiveSlot)
                RemoveAllAuras();

            _SaveSpells(trans);
            GetSpellHistory().SaveToDB<Pet>(trans);
            DB.Characters.CommitTransaction(trans);

            // current/stable/not_in_slot
            if (mode != PetSaveMode.AsDeleted)
            {
                ulong ownerLowGUID = GetOwnerGUID().GetCounter();
                trans = new SQLTransaction();

                // remove current _data
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                // save pet
                string actionBar = GenerateActionBarData();

                Cypher.Assert(owner.GetPetStable().GetCurrentPet() != null && owner.GetPetStable().GetCurrentPet().PetNumber == GetCharmInfo().GetPetNumber());
                FillPetInfo(owner.GetPetStable().GetCurrentPet());

                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                stmt.AddValue(1, GetEntry());
                stmt.AddValue(2, ownerLowGUID);
                stmt.AddValue(3, GetNativeDisplayId());
                stmt.AddValue(4, GetLevel());
                stmt.AddValue(5, UnitData.PetExperience);
                stmt.AddValue(6, (byte)GetReactState());
                stmt.AddValue(7, (owner.GetPetStable().GetCurrentActivePetIndex().HasValue ? (short)owner.GetPetStable().GetCurrentActivePetIndex().Value : (short)PetSaveMode.NotInSlot));
                stmt.AddValue(8, GetName());
                stmt.AddValue(9, HasPetFlag(UnitPetFlags.CanBeRenamed) ? 0 : 1);
                stmt.AddValue(10, curhealth);
                stmt.AddValue(11, curmana);

                stmt.AddValue(12, actionBar);

                stmt.AddValue(13, GameTime.GetGameTime());
                stmt.AddValue(14, UnitData.CreatedBySpell);
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

        public void FillPetInfo(PetStable.PetInfo petInfo)
        {
            petInfo.PetNumber = GetCharmInfo().GetPetNumber();
            petInfo.CreatureId = GetEntry();
            petInfo.DisplayId = GetNativeDisplayId();
            petInfo.Level = (byte)GetLevel();
            petInfo.Experience = UnitData.PetExperience;
            petInfo.ReactState = GetReactState();
            petInfo.Name = GetName();
            petInfo.WasRenamed = !HasPetFlag(UnitPetFlags.CanBeRenamed);
            petInfo.Health = (uint)GetHealth();
            petInfo.Mana = (uint)GetPower(PowerType.Mana);
            petInfo.ActionBar = GenerateActionBarData();
            petInfo.LastSaveTime = (uint)GameTime.GetGameTime();
            petInfo.CreatedBySpellId = UnitData.CreatedBySpell;
            petInfo.Type = GetPetType();
            petInfo.SpecializationId = GetSpecialization();
        }

        public static void DeleteFromDB(uint petNumber)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELLS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
            stmt.AddValue(0, petNumber);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
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
            if (Removed) // pet already removed, just wait in remove queue, no updates
                return;

            if (_loading)
                return;

            switch (DeathState)
            {
                case DeathState.Corpse:
                    {
                        if (GetPetType() != PetType.Hunter ||
                            CorpseRemoveTime <= GameTime.GetGameTime())
                        {
                            Remove(PetSaveMode.NotInSlot); //hunters' pets never get removed because of death, NEVER!

                            return;
                        }

                        break;
                    }
                case DeathState.Alive:
                    {
                        // unsummon pet that lost owner
                        Player owner = GetOwner();

                        if (owner == null ||
                            (!IsWithinDistInMap(owner, GetMap().GetVisibilityRange()) && !IsPossessed()) ||
                            (IsControlled() && owner.GetPetGUID().IsEmpty()))
                        {
                            Remove(PetSaveMode.NotInSlot, true);

                            return;
                        }

                        if (IsControlled())
                            if (owner.GetPetGUID() != GetGUID())
                            {
                                Log.outError(LogFilter.Pet, $"Pet {GetEntry()} is not pet of owner {GetOwner().GetName()}, removed");
                                Cypher.Assert(GetPetType() != PetType.Hunter, $"Unexpected unlinked pet found for owner {owner.GetSession().GetPlayerInfo()}");
                                Remove(PetSaveMode.NotInSlot);

                                return;
                            }

                        if (_duration > 0)
                        {
                            if (_duration > diff)
                            {
                                _duration -= (int)diff;
                            }
                            else
                            {
                                Remove(GetPetType() != PetType.Summon ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);

                                return;
                            }
                        }

                        //regenerate focus for hunter pets or energy for deathknight's ghoul
                        if (_focusRegenTimer != 0)
                        {
                            if (_focusRegenTimer > diff)
                                _focusRegenTimer -= diff;
                            else
                                switch (GetPowerType())
                                {
                                    case PowerType.Focus:
                                        Regenerate(PowerType.Focus);
                                        _focusRegenTimer += PET_FOCUS_REGEN_INTERVAL - diff;

                                        if (_focusRegenTimer == 0)
                                            ++_focusRegenTimer;

                                        // Reset if large diff (lag) causes focus to get 'stuck'
                                        if (_focusRegenTimer > PET_FOCUS_REGEN_INTERVAL)
                                            _focusRegenTimer = PET_FOCUS_REGEN_INTERVAL;

                                        break;
                                    default:
                                        _focusRegenTimer = 0;

                                        break;
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

            uint nextLvlXP = UnitData.PetNextLevelExperience;
            uint curXP = UnitData.PetExperience;
            uint newXP = curXP + xp;

            // Check how much XP the pet should receive, and hand off have any left from previous levelups
            while (newXP >= nextLvlXP && petlevel < maxlevel)
            {
                // Subtract newXP from amount needed for nextlevel, and give pet the level
                newXP -= nextLvlXP;
                ++petlevel;

                GivePetLevel((int)petlevel);

                nextLvlXP = UnitData.PetNextLevelExperience;
            }

            // Not affected by special conditions - give it new XP
            SetPetExperience(petlevel < maxlevel ? newXP : 0);
        }

        public void GivePetLevel(int level)
        {
            if (level == 0 ||
                level == GetLevel())
                return;

            if (GetPetType() == PetType.Hunter)
            {
                SetPetExperience(0);
                SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel((uint)level) * PET_XP_FACTOR));
            }

            InitStatsForLevel((uint)level);
            InitLevelupSpellsForLevel();
        }

        public bool CreateBaseAtCreature(Creature creature)
        {
            Cypher.Assert(creature);

            if (!CreateBaseAtTamed(creature.GetCreatureTemplate(), creature.GetMap()))
                return false;

            Relocate(creature.GetPositionX(), creature.GetPositionY(), creature.GetPositionZ(), creature.GetOrientation());

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Pet,
                             "Pet (guidlow {0}, entry {1}) not created base at creature. Suggested coordinates isn't valid (X: {2} Y: {3})",
                             GetGUID().ToString(),
                             GetEntry(),
                             GetPositionX(),
                             GetPositionY());

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

        public bool LearnSpell(uint spellId)
        {
            // prevent duplicated entires in spell book
            if (!AddSpell(spellId))
                return false;

            if (!_loading)
            {
                PetLearnedSpells packet = new();
                packet.Spells.Add(spellId);
                GetOwner().SendPacket(packet);
                GetOwner().PetSpellInitialize();
            }

            return true;
        }

        public bool RemoveSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
        {
            var petSpell = PetSpells.LookupByKey(spellId);

            if (petSpell == null)
                return false;

            if (petSpell.State == PetSpellState.Removed)
                return false;

            if (petSpell.State == PetSpellState.New)
                PetSpells.Remove(spellId);
            else
                petSpell.State = PetSpellState.Removed;

            RemoveAurasDueToSpell(spellId);

            if (learnPrev)
            {
                uint prev_id = Global.SpellMgr.GetPrevSpellInChain(spellId);

                if (prev_id != 0)
                    LearnSpell(prev_id);
                else
                    learnPrev = false;
            }

            // if remove last rank or non-ranked then update Action bar at server and client if need
            if (clearActionBar &&
                !learnPrev &&
                GetCharmInfo().RemoveSpellFromActionBar(spellId))
                if (!_loading)
                {
                    // need update Action bar for last removed rank
                    Unit owner = GetOwner();

                    if (owner)
                        if (owner.IsTypeId(TypeId.Player))
                            owner.ToPlayer().PetSpellInitialize();
                }

            return true;
        }

        public void InitPetCreateSpells()
        {
            GetCharmInfo().InitPetActionBar();
            PetSpells.Clear();

            LearnPetPassives();
            InitLevelupSpellsForLevel();

            CastPetAuras(false);
        }

        public void ToggleAutocast(SpellInfo spellInfo, bool apply)
        {
            if (!spellInfo.IsAutocastable())
                return;

            var petSpell = PetSpells.LookupByKey(spellInfo.Id);

            if (petSpell == null)
                return;

            var hasSpell = _autospells.Contains(spellInfo.Id);

            if (apply)
            {
                if (!hasSpell)
                {
                    _autospells.Add(spellInfo.Id);

                    if (petSpell.Active != ActiveStates.Enabled)
                    {
                        petSpell.Active = ActiveStates.Enabled;

                        if (petSpell.State != PetSpellState.New)
                            petSpell.State = PetSpellState.Changed;
                    }
                }
            }
            else
            {
                if (hasSpell)
                {
                    _autospells.Remove(spellInfo.Id);

                    if (petSpell.Active != ActiveStates.Disabled)
                    {
                        petSpell.Active = ActiveStates.Disabled;

                        if (petSpell.State != PetSpellState.New)
                            petSpell.State = PetSpellState.Changed;
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
                        case Class.Deathknight:
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
            Cypher.Assert(map);
            SetMap(map);

            // TODO: counter should be constructed as (summon_count << 32) | petNumber
            _Create(ObjectGuid.Create(HighGuid.Pet, map.GetId(), entry, guidlow));

            SpawnId = guidlow;
            OriginalEntry = entry;

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
            var petSpell = PetSpells.LookupByKey(spell);

            return petSpell != null && petSpell.State != PetSpellState.Removed;
        }

        public void CastPetAura(PetAura aura)
        {
            uint auraId = aura.GetAura(GetEntry());

            if (auraId == 0)
                return;

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

            if (auraId == 35696) // Demonic Knowledge
                args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(aura.GetDamage(), GetStat(Stats.Stamina) + GetStat(Stats.Intellect)));

            CastSpell(this, auraId, args);
        }

        public void SynchronizeLevelWithOwner()
        {
            Unit owner = GetOwner();

            if (!owner ||
                !owner.IsTypeId(TypeId.Player))
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

            if (creatureFamily != null &&
                creatureFamily.MinScale > 0.0f &&
                GetPetType() == PetType.Hunter)
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

        public override void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            base.SetDisplayId(modelId, displayScale);

            if (!IsControlled())
                return;

            SetGroupUpdateFlag(GroupUpdatePetFlags.ModelId);
        }

        public PetType GetPetType()
        {
            return _petType;
        }

        public void SetPetType(PetType type)
        {
            _petType = type;
        }

        public bool IsControlled()
        {
            return GetPetType() == PetType.Summon || GetPetType() == PetType.Hunter;
        }

        public bool IsTemporarySummoned()
        {
            return _duration > 0;
        }

        public override bool IsLoading()
        {
            return _loading;
        }

        public override byte GetPetAutoSpellSize()
        {
            return (byte)_autospells.Count;
        }

        public override uint GetPetAutoSpellOnPos(byte pos)
        {
            if (pos >= _autospells.Count)
                return 0;
            else
                return _autospells[pos];
        }

        public void SetDuration(uint dur)
        {
            _duration = (int)dur;
        }

        public int GetDuration()
        {
            return _duration;
        }

        public void SetPetExperience(uint xp)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetExperience), xp);
        }

        public void SetPetNextLevelExperience(uint xp)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNextLevelExperience), xp);
        }

        public ushort GetSpecialization()
        {
            return _petSpecialization;
        }

        public GroupUpdatePetFlags GetGroupUpdateFlag()
        {
            return _groupUpdateMask;
        }

        public void SetGroupUpdateFlag(GroupUpdatePetFlags flag)
        {
            if (GetOwner().GetGroup())
            {
                _groupUpdateMask |= flag;
                GetOwner().SetGroupUpdateFlag(GroupUpdateFlags.Pet);
            }
        }

        public void ResetGroupUpdateFlag()
        {
            _groupUpdateMask = GroupUpdatePetFlags.None;

            if (GetOwner().GetGroup())
                GetOwner().RemoveGroupUpdateFlag(GroupUpdateFlags.Pet);
        }

        public void SetSpecialization(uint spec)
        {
            if (_petSpecialization == spec)
                return;

            // remove all the old spec's specalization spells, set the new spec, then add the new spec's spells
            // clearActionBars is false because we'll be updating the pet actionbar later so we don't have to do it now
            RemoveSpecializationSpells(false);

            if (!CliDB.ChrSpecializationStorage.ContainsKey(spec))
            {
                _petSpecialization = 0;

                return;
            }

            _petSpecialization = (ushort)spec;
            LearnSpecializationSpells();

            // resend SMSG_PET_SPELLS_MESSAGE to remove old specialization spells from the pet Action bar
            CleanupActionBar();
            GetOwner().PetSpellInitialize();

            SetPetSpecialization setPetSpecialization = new();
            setPetSpecialization.SpecID = _petSpecialization;
            GetOwner().SendPacket(setPetSpecialization);
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nPetType: {GetPetType()} PetNumber: {GetCharmInfo().GetPetNumber()}";
        }

        public DeclinedName GetDeclinedNames()
        {
            return _declinedname;
        }

        private bool CreateBaseAtTamed(CreatureTemplate cinfo, Map map)
        {
            Log.outDebug(LogFilter.Pet, "CreateBaseForTamed");

            if (!Create(map.GenerateLowGuid(HighGuid.Pet), map, cinfo.Entry, Global.ObjectMgr.GeneratePetNumber()))
                return false;

            SetPetNameTimestamp(0);
            SetPetExperience(0);
            SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel(GetLevel() + 1) * PET_XP_FACTOR));
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

        private void _LoadSpells(SQLResult result)
        {
            if (!result.IsEmpty())
                do
                {
                    AddSpell(result.Read<uint>(0), (ActiveStates)result.Read<byte>(1), PetSpellState.Unchanged);
                } while (result.NextRow());
        }

        private void _SaveSpells(SQLTransaction trans)
        {
            foreach (var pair in PetSpells.ToList())
            {
                // prevent saving family passives to DB
                if (pair.Value.Type == PetSpellType.Family)
                    continue;

                PreparedStatement stmt;

                switch (pair.Value.State)
                {
                    case PetSpellState.Removed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        PetSpells.Remove(pair.Key);

                        continue;
                    case PetSpellState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, (byte)pair.Value.Active);
                        trans.Append(stmt);

                        break;
                    case PetSpellState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, (byte)pair.Value.Active);
                        trans.Append(stmt);

                        break;
                    case PetSpellState.Unchanged:
                        continue;
                }

                pair.Value.State = PetSpellState.Unchanged;
            }
        }

        private void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
        {
            Log.outDebug(LogFilter.Pet, "Loading Auras for {0}", GetGUID().ToString());

            ObjectGuid casterGuid = default;
            ObjectGuid itemGuid = default;
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();

            if (!effectResult.IsEmpty())
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

            if (!auraResult.IsEmpty())
                do
                {
                    // NULL Guid stored - pet is the caster of the spell - see Pet._SaveAuras
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

                    if (difficulty != Difficulty.None &&
                        !CliDB.DifficultyStorage.ContainsKey(difficulty))
                    {
                        Log.outError(LogFilter.Pet, $"Pet._LoadAuras: Unknown difficulty {difficulty} (spellid {key.SpellId}), ignore.");

                        continue;
                    }

                    // negative effects should continue counting down after logout
                    if (remainTime != -1 &&
                        (!spellInfo.IsPositive() || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
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
                    {
                        remainCharges = 0;
                    }

                    var info = effectInfo[key];
                    ObjectGuid castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, GetMapId(), spellInfo.Id, GetMap().GenerateLowGuid(HighGuid.Cast));

                    AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask, this);
                    createInfo.SetCasterGUID(casterGuid);
                    createInfo.SetBaseAmount(info.BaseAmounts);

                    Aura aura = Aura.TryCreate(createInfo);

                    if (aura != null)
                    {
                        if (!aura.CanBeSaved())
                        {
                            aura.Remove();

                            continue;
                        }

                        aura.SetLoadedState(maxDuration, remainTime, remainCharges, stackCount, recalculateMask, info.Amounts);
                        aura.ApplyForTargets();
                        Log.outInfo(LogFilter.Pet, "Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
                    }
                } while (auraResult.NextRow());
        }

        private void _SaveAuras(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            trans.Append(stmt);

            byte index;

            foreach (var pair in GetOwnedAuras())
            {
                Aura aura = pair.Value;

                // check if the aura has to be saved
                if (!aura.CanBeSaved() ||
                    IsPetAura(aura))
                    continue;

                uint recalculateMask;
                AuraKey key = aura.GenerateKey(out recalculateMask);

                // don't save Guid of caster in case we are caster of the spell - Guid for pet is generated every pet load, so it won't match saved Guid anyways
                if (key.Caster == GetGUID())
                    key.Caster.Clear();

                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_AURA);
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
                    if (effect != null)
                    {
                        index = 0;
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_AURA_EFFECT);
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

        private bool AddSpell(uint spellId, ActiveStates active = ActiveStates.Decide, PetSpellState state = PetSpellState.New, PetSpellType type = PetSpellType.Normal)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

            if (spellInfo == null)
            {
                // do pet spell book cleanup
                if (state == PetSpellState.Unchanged) // spell load case
                {
                    Log.outError(LogFilter.Pet, "addSpell: Non-existed in SpellStore spell #{0} request, deleting for all pets in `pet_spell`.", spellId);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_PET_SPELL);

                    stmt.AddValue(0, spellId);

                    DB.Characters.Execute(stmt);
                }
                else
                {
                    Log.outError(LogFilter.Pet, "addSpell: Non-existed in SpellStore spell #{0} request.", spellId);
                }

                return false;
            }

            var petSpell = PetSpells.LookupByKey(spellId);

            if (petSpell != null)
            {
                if (petSpell.State == PetSpellState.Removed)
                {
                    state = PetSpellState.Changed;
                }
                else
                {
                    if (state == PetSpellState.Unchanged &&
                        petSpell.State != PetSpellState.Unchanged)
                    {
                        // can be in case spell loading but learned at some previous spell loading
                        petSpell.State = PetSpellState.Unchanged;

                        if (active == ActiveStates.Enabled)
                            ToggleAutocast(spellInfo, true);
                        else if (active == ActiveStates.Disabled)
                            ToggleAutocast(spellInfo, false);

                        return false;
                    }
                }
            }

            PetSpell newspell = new();
            newspell.State = state;
            newspell.Type = type;

            if (active == ActiveStates.Decide) // active was not used before, so we save it's autocast/passive State here
            {
                if (spellInfo.IsAutocastable())
                    newspell.Active = ActiveStates.Disabled;
                else
                    newspell.Active = ActiveStates.Passive;
            }
            else
            {
                newspell.Active = active;
            }

            // talent: unlearn all other talent ranks (high and low)
            if (spellInfo.IsRanked())
                foreach (var pair in PetSpells)
                {
                    if (pair.Value.State == PetSpellState.Removed)
                        continue;

                    SpellInfo oldRankSpellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

                    if (oldRankSpellInfo == null)
                        continue;

                    if (spellInfo.IsDifferentRankOf(oldRankSpellInfo))
                    {
                        // replace by new high rank
                        if (spellInfo.IsHighRankOf(oldRankSpellInfo))
                        {
                            newspell.Active = pair.Value.Active;

                            if (newspell.Active == ActiveStates.Enabled)
                                ToggleAutocast(oldRankSpellInfo, false);

                            UnlearnSpell(pair.Key, false, false);

                            break;
                        }
                        // ignore new lesser rank
                        else
                        {
                            return false;
                        }
                    }
                }

            PetSpells[spellId] = newspell;

            if (spellInfo.IsPassive() &&
                (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState)))
                CastSpell(this, spellId, true);
            else
                GetCharmInfo().AddSpellToActionBar(spellInfo);

            if (newspell.Active == ActiveStates.Enabled)
                ToggleAutocast(spellInfo, true);

            return true;
        }

        private void LearnSpells(List<uint> spellIds)
        {
            PetLearnedSpells packet = new();

            foreach (uint spell in spellIds)
            {
                if (!AddSpell(spell))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!_loading)
                GetOwner().SendPacket(packet);
        }

        private void InitLevelupSpellsForLevel()
        {
            uint level = GetLevel();
            var levelupSpells = GetCreatureTemplate().Family != 0 ? Global.SpellMgr.GetPetLevelupSpellList(GetCreatureTemplate().Family) : null;

            if (levelupSpells != null)
                // PetLevelupSpellSet ordered by levels, process in reversed order
                foreach (var pair in levelupSpells)
                    // will called first if level down
                    if (pair.Key > level)
                        UnlearnSpell(pair.Value, true); // will learn prev rank if any
                                                        // will called if level up
                    else
                        LearnSpell(pair.Value); // will unlearn prev rank if any

            // default spells (can be not learned if pet level (as owner level decrease result for example) less first possible in normal game)
            PetDefaultSpellsEntry defSpells = Global.SpellMgr.GetPetDefaultSpellsEntry((int)GetEntry());

            if (defSpells != null)
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

        private bool UnlearnSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
        {
            if (RemoveSpell(spellId, learnPrev, clearActionBar))
            {
                if (!_loading)
                {
                    PetUnlearnedSpells packet = new();
                    packet.Spells.Add(spellId);
                    GetOwner().SendPacket(packet);
                }

                return true;
            }

            return false;
        }

        private void UnlearnSpells(List<uint> spellIds, bool learnPrev, bool clearActionBar)
        {
            PetUnlearnedSpells packet = new();

            foreach (uint spell in spellIds)
            {
                if (!RemoveSpell(spell, learnPrev, clearActionBar))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!_loading)
                GetOwner().SendPacket(packet);
        }

        private void CleanupActionBar()
        {
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                UnitActionBarEntry ab = GetCharmInfo().GetActionBarEntry(i);

                if (ab != null)
                    if (ab.GetAction() != 0 &&
                        ab.IsActionBarForSpell())
                    {
                        if (!HasSpell(ab.GetAction()))
                        {
                            GetCharmInfo().SetActionBar(i, 0, ActiveStates.Passive);
                        }
                        else if (ab.GetActiveState() == ActiveStates.Enabled)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ab.GetAction(), Difficulty.None);

                            if (spellInfo != null)
                                ToggleAutocast(spellInfo, true);
                        }
                    }
            }
        }

        // Get all passive spells in our skill line
        private void LearnPetPassives()
        {
            CreatureTemplate cInfo = GetCreatureTemplate();

            if (cInfo == null)
                return;

            CreatureFamilyRecord cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cInfo.Family);

            if (cFamily == null)
                return;

            var petStore = Global.SpellMgr.PetFamilySpellsStorage.LookupByKey(cInfo.Family);

            if (petStore != null)
                // For general hunter pets skill 270
                // Passive 01~10, Passive 00 (20782, not used), Ferocious Inspiration (34457)
                // Scale 01~03 (34902~34904, bonus from owner, not used)
                foreach (var spellId in petStore)
                    AddSpell(spellId, ActiveStates.Decide, PetSpellState.New, PetSpellType.Family);
        }

        private void CastPetAuras(bool current)
        {
            Player owner = GetOwner();

            if (!IsPermanentPetFor(owner))
                return;

            foreach (var pa in owner.PetAuras)
                if (!current &&
                    pa.IsRemovedOnChangePet())
                    owner.RemovePetAura(pa);
                else
                    CastPetAura(pa);
        }

        private bool IsPetAura(Aura aura)
        {
            Player owner = GetOwner();

            // if the owner has that pet aura, return true
            foreach (var petAura in owner.PetAuras)
                if (petAura.GetAura(GetEntry()) == aura.GetId())
                    return true;

            return false;
        }

        private void LearnSpellHighRank(uint spellid)
        {
            LearnSpell(spellid);
            uint next = Global.SpellMgr.GetNextSpellInChain(spellid);

            if (next != 0)
                LearnSpellHighRank(next);
        }

        private void LearnSpecializationSpells()
        {
            List<uint> learnedSpells = new();

            List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(_petSpecialization);

            if (specSpells != null)
                foreach (var specSpell in specSpells)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID, Difficulty.None);

                    if (spellInfo == null ||
                        spellInfo.SpellLevel > GetLevel())
                        continue;

                    learnedSpells.Add(specSpell.SpellID);
                }

            LearnSpells(learnedSpells);
        }

        private void RemoveSpecializationSpells(bool clearActionBar)
        {
            List<uint> unlearnedSpells = new();

            for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
            {
                ChrSpecializationRecord specialization = Global.DB2Mgr.GetChrSpecializationByIndex(0, i);

                if (specialization != null)
                {
                    List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization.Id);

                    if (specSpells != null)
                        foreach (var specSpell in specSpells)
                            unlearnedSpells.Add(specSpell.SpellID);
                }

                ChrSpecializationRecord specialization1 = Global.DB2Mgr.GetChrSpecializationByIndex(Class.Max, i);

                if (specialization1 != null)
                {
                    List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization1.Id);

                    if (specSpells != null)
                        foreach (var specSpell in specSpells)
                            unlearnedSpells.Add(specSpell.SpellID);
                }
            }

            UnlearnSpells(unlearnedSpells, true, clearActionBar);
        }

        private string GenerateActionBarData()
        {
            StringBuilder ss = new();

            for (byte i = SharedConst.ActionBarIndexStart; i < SharedConst.ActionBarIndexEnd; ++i)
                ss.AppendFormat("{0} {1} ", (uint)GetCharmInfo().GetActionBarEntry(i).GetActiveState(), (uint)GetCharmInfo().GetActionBarEntry(i).GetAction());

            return ss.ToString();
        }
    }
}