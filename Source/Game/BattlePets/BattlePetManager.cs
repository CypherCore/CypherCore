// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.BattlePets
{
    public class BattlePetMgr
    {
        public BattlePetMgr(WorldSession owner)
        {
            _owner = owner;
            for (byte i = 0; i < (int)BattlePetSlots.Count; ++i)
            {
                BattlePetSlot slot = new();
                slot.Index = i;
                _slots.Add(slot);
            }
        }

        public static void Initialize()
        {
            SQLResult result = DB.Login.Query("SELECT MAX(guid) FROM battle_pets");
            if (!result.IsEmpty())
                Global.ObjectMgr.GetGenerator(HighGuid.BattlePet).Set(result.Read<ulong>(0) + 1);

            foreach (var battlePetSpecies in CliDB.BattlePetSpeciesStorage.Values)
            {
                uint creatureId = battlePetSpecies.CreatureID;
                if (creatureId != 0)
                    _battlePetSpeciesByCreature[creatureId] = battlePetSpecies;
            }

            foreach (var battlePetBreedState in CliDB.BattlePetBreedStateStorage.Values)
            {
                if (!BattlePetBreedStates.ContainsKey(battlePetBreedState.BattlePetBreedID))
                    BattlePetBreedStates[battlePetBreedState.BattlePetBreedID] = new Dictionary<BattlePetState, int>();

                BattlePetBreedStates[battlePetBreedState.BattlePetBreedID][(BattlePetState)battlePetBreedState.BattlePetStateID] = battlePetBreedState.Value;
            }

            foreach (var battlePetSpeciesState in CliDB.BattlePetSpeciesStateStorage.Values)
            {
                if (!BattlePetSpeciesStates.ContainsKey(battlePetSpeciesState.BattlePetSpeciesID))
                    BattlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID] = new Dictionary<BattlePetState, int>();

                BattlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID][(BattlePetState)battlePetSpeciesState.BattlePetStateID] = battlePetSpeciesState.Value;
            }

            LoadAvailablePetBreeds();
            LoadDefaultPetQualities();
        }

        static void LoadAvailablePetBreeds()
        {
            SQLResult result = DB.World.Query("SELECT speciesId, breedId FROM battle_pet_breeds");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 battle pet breeds. DB table `battle_pet_breeds` is empty.");
                return;
            }

            uint count = 0;
            do
            {
                uint speciesId = result.Read<uint>(0);
                ushort breedId = result.Read<ushort>(1);

                if (!CliDB.BattlePetSpeciesStorage.ContainsKey(speciesId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing BattlePetSpecies.db2 entry {0} was referenced in `battle_pet_breeds` by row ({1}, {2}).", speciesId, speciesId, breedId);
                    continue;
                }

                // TODO: verify breed id (3 - 12 (male) or 3 - 22 (male and female)) if needed

                _availableBreedsPerSpecies.Add(speciesId, (byte)breedId);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} battle pet breeds.", count);
        }

        static void LoadDefaultPetQualities()
        {
            SQLResult result = DB.World.Query("SELECT speciesId, quality FROM battle_pet_quality");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 battle pet qualities. DB table `battle_pet_quality` is empty.");
                return;
            }

            do
            {
                uint speciesId = result.Read<uint>(0);
                BattlePetBreedQuality quality = (BattlePetBreedQuality)result.Read<byte>(1);

                var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(speciesId);
                if (battlePetSpecies == null)
                {
                    Log.outError(LogFilter.Sql, $"Non-existing BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` by row ({speciesId}, {quality}).");
                    continue;
                }

                if (quality >= BattlePetBreedQuality.Max)
                {
                    Log.outError(LogFilter.Sql, $"BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` with non-existing quality {quality}).");
                    continue;
                }

                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.WellKnown) && quality > BattlePetBreedQuality.Rare)
                {
                    Log.outError(LogFilter.Sql, $"Learnable BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` with invalid quality {quality}. Maximum allowed quality is BattlePetBreedQuality::Rare.");
                    continue;
                }

                _defaultQualityPerSpecies[speciesId] = quality;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} battle pet qualities.", _defaultQualityPerSpecies.Count);
        }

        public static void AddBattlePetSpeciesBySpell(uint spellId, BattlePetSpeciesRecord speciesEntry)
        {
            _battlePetSpeciesBySpell[spellId] = speciesEntry;
        }

        public static BattlePetSpeciesRecord GetBattlePetSpeciesByCreature(uint creatureId)
        {
            return _battlePetSpeciesByCreature.LookupByKey(creatureId);
        }

        public static BattlePetSpeciesRecord GetBattlePetSpeciesBySpell(uint spellId)
        {
            return _battlePetSpeciesBySpell.LookupByKey(spellId);
        }

        public static ushort RollPetBreed(uint species)
        {
            var list = _availableBreedsPerSpecies.LookupByKey(species);
            if (list.Empty())
                return 3; // default B/B

            return list.SelectRandom();
        }

        public static BattlePetBreedQuality GetDefaultPetQuality(uint species)
        {
            if (!_defaultQualityPerSpecies.ContainsKey(species))
                return BattlePetBreedQuality.Poor; // Default

            return _defaultQualityPerSpecies[species];
        }

        public static uint SelectPetDisplay(BattlePetSpeciesRecord speciesEntry)
        {
            CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(speciesEntry.CreatureID);
            if (creatureTemplate != null)
            {
                if (!speciesEntry.HasFlag(BattlePetSpeciesFlags.RandomDisplay))
                {
                    CreatureModel creatureModel = creatureTemplate.GetRandomValidModel();
                    if (creatureModel != null)
                        return creatureModel.CreatureDisplayID;
                }
            }

            return 0;
        }

        public void LoadFromDB(SQLResult petsResult, SQLResult slotsResult)
        {
            if (!petsResult.IsEmpty())
            {
                do
                {
                    uint species = petsResult.Read<uint>(1);
                    ObjectGuid ownerGuid = !petsResult.IsNull(11) ? ObjectGuid.Create(HighGuid.Player, petsResult.Read<ulong>(11)) : ObjectGuid.Empty;

                    BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(species);
                    if (speciesEntry != null)
                    {
                        if (speciesEntry.HasFlag(BattlePetSpeciesFlags.NotAccountWide))
                        {
                            if (ownerGuid.IsEmpty())
                            {
                                Log.outError(LogFilter.Misc, $"Battlenet account with id {_owner.GetBattlenetAccountId()} has battle pet of species {species} with BattlePetSpeciesFlags::NotAccountWide but no owner");
                                continue;
                            }
                        }
                        else
                        {
                            if (!ownerGuid.IsEmpty())
                            {
                                Log.outError(LogFilter.Misc, $"Battlenet account with id {_owner.GetBattlenetAccountId()} has battle pet of species {species} without BattlePetSpeciesFlags::NotAccountWide but with owner");
                                continue;
                            }
                        }

                        if (HasMaxPetCount(speciesEntry, ownerGuid))
                        {
                            if (ownerGuid.IsEmpty())
                                Log.outError(LogFilter.Misc, $"Battlenet account with id {_owner.GetBattlenetAccountId()} has more than maximum battle pets of species {species}");
                            else
                                Log.outError(LogFilter.Misc, $"Battlenet account with id {_owner.GetBattlenetAccountId()} has more than maximum battle pets of species {species} for player {ownerGuid}");

                            continue;
                        }

                        BattlePet pet = new();
                        pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, petsResult.Read<ulong>(0));
                        pet.PacketInfo.Species = species;
                        pet.PacketInfo.Breed = petsResult.Read<ushort>(2);
                        pet.PacketInfo.DisplayID = petsResult.Read<uint>(3);
                        pet.PacketInfo.Level = petsResult.Read<ushort>(4);
                        pet.PacketInfo.Exp = petsResult.Read<ushort>(5);
                        pet.PacketInfo.Health = petsResult.Read<uint>(6);
                        pet.PacketInfo.Quality = petsResult.Read<byte>(7);
                        pet.PacketInfo.Flags = petsResult.Read<ushort>(8);
                        pet.PacketInfo.Name = petsResult.Read<string>(9);
                        pet.NameTimestamp = petsResult.Read<long>(10);
                        pet.PacketInfo.CreatureID = speciesEntry.CreatureID;

                        if (!petsResult.IsNull(13))
                        {
                            pet.DeclinedName = new();
                            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                                pet.DeclinedName.name[i] = petsResult.Read<string>(13 + i);
                        }

                        if (!ownerGuid.IsEmpty())
                        {
                            BattlePetStruct.BattlePetOwnerInfo battlePetOwnerInfo = new();
                            battlePetOwnerInfo.Guid = ownerGuid;
                            var ownerRealm = Global.RealmMgr.GetRealm(new Framework.Realm.RealmId(petsResult.Read<uint>(12)));
                            if (ownerRealm != null)
                                battlePetOwnerInfo.PlayerVirtualRealm = battlePetOwnerInfo.PlayerNativeRealm = ownerRealm.Id.GetAddress();
                            pet.PacketInfo.OwnerInfo = battlePetOwnerInfo;
                        }

                        pet.SaveInfo = BattlePetSaveInfo.Unchanged;
                        pet.CalculateStats();
                        _pets[pet.PacketInfo.Guid.GetCounter()] = pet;
                    }
                } while (petsResult.NextRow());
            }

            if (!slotsResult.IsEmpty())
            {
                byte i = 0; // slots.GetRowCount() should equal MAX_BATTLE_PET_SLOTS

                do
                {
                    _slots[i].Index = slotsResult.Read<byte>(0);
                    var battlePet = _pets.LookupByKey(slotsResult.Read<ulong>(1));
                    if (battlePet != null)
                        _slots[i].Pet = battlePet.PacketInfo;
                    _slots[i].Locked = slotsResult.Read<bool>(2);
                    i++;
                } while (slotsResult.NextRow());
            }
        }

        public void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt;

            foreach (var pair in _pets)
            {
                switch (pair.Value.SaveInfo)
                {
                    case BattlePetSaveInfo.New:
                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PETS);
                        stmt.AddValue(0, pair.Key);
                        stmt.AddValue(1, _owner.GetBattlenetAccountId());
                        stmt.AddValue(2, pair.Value.PacketInfo.Species);
                        stmt.AddValue(3, pair.Value.PacketInfo.Breed);
                        stmt.AddValue(4, pair.Value.PacketInfo.DisplayID);
                        stmt.AddValue(5, pair.Value.PacketInfo.Level);
                        stmt.AddValue(6, pair.Value.PacketInfo.Exp);
                        stmt.AddValue(7, pair.Value.PacketInfo.Health);
                        stmt.AddValue(8, pair.Value.PacketInfo.Quality);
                        stmt.AddValue(9, pair.Value.PacketInfo.Flags);
                        stmt.AddValue(10, pair.Value.PacketInfo.Name);
                        stmt.AddValue(11, pair.Value.NameTimestamp);
                        if (pair.Value.PacketInfo.OwnerInfo.HasValue)
                        {
                            stmt.AddValue(12, pair.Value.PacketInfo.OwnerInfo.Value.Guid.GetCounter());
                            stmt.AddValue(13, Global.RealmMgr.GetCurrentRealmId().Index);
                        }
                        else
                        {
                            stmt.AddNull(12);
                            stmt.AddNull(13);
                        }

                        trans.Append(stmt);

                        if (pair.Value.DeclinedName != null)
                        {
                            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_DECLINED_NAME);
                            stmt.AddValue(0, pair.Key);

                            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                                stmt.AddValue(i + 1, pair.Value.DeclinedName.name[i]);

                            trans.Append(stmt);
                        }


                        pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;
                        break;
                    case BattlePetSaveInfo.Changed:
                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BATTLE_PETS);
                        stmt.AddValue(0, pair.Value.PacketInfo.Level);
                        stmt.AddValue(1, pair.Value.PacketInfo.Exp);
                        stmt.AddValue(2, pair.Value.PacketInfo.Health);
                        stmt.AddValue(3, pair.Value.PacketInfo.Quality);
                        stmt.AddValue(4, pair.Value.PacketInfo.Flags);
                        stmt.AddValue(5, pair.Value.PacketInfo.Name);
                        stmt.AddValue(6, pair.Value.NameTimestamp);
                        stmt.AddValue(7, _owner.GetBattlenetAccountId());
                        stmt.AddValue(8, pair.Key);
                        trans.Append(stmt);

                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME);
                        stmt.AddValue(0, pair.Key);
                        trans.Append(stmt);

                        if (pair.Value.DeclinedName != null)
                        {
                            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_DECLINED_NAME);
                            stmt.AddValue(0, pair.Key);

                            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                                stmt.AddValue(i + 1, pair.Value.DeclinedName.name[i]);

                            trans.Append(stmt);
                        }

                        pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;
                        break;
                    case BattlePetSaveInfo.Removed:
                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME);
                        stmt.AddValue(0, pair.Key);
                        trans.Append(stmt);

                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS);
                        stmt.AddValue(0, _owner.GetBattlenetAccountId());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);
                        _pets.Remove(pair.Key);
                        break;
                }
            }

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_SLOTS);
            stmt.AddValue(0, _owner.GetBattlenetAccountId());
            trans.Append(stmt);

            foreach (var slot in _slots)
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_SLOTS);
                stmt.AddValue(0, slot.Index);
                stmt.AddValue(1, _owner.GetBattlenetAccountId());
                stmt.AddValue(2, slot.Pet.Guid.GetCounter());
                stmt.AddValue(3, slot.Locked);
                trans.Append(stmt);
            }
        }

        public BattlePet GetPet(ObjectGuid guid)
        {
            return _pets.LookupByKey(guid.GetCounter());
        }

        public void AddPet(uint species, uint display, ushort breed, BattlePetBreedQuality quality, ushort level = 1)
        {
            BattlePetSpeciesRecord battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(species);
            if (battlePetSpecies == null) // should never happen
                return;

            if (!battlePetSpecies.HasFlag(BattlePetSpeciesFlags.WellKnown)) // Not learnable
                return;

            BattlePet pet = new();
            pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, Global.ObjectMgr.GetGenerator(HighGuid.BattlePet).Generate());
            pet.PacketInfo.Species = species;
            pet.PacketInfo.CreatureID = battlePetSpecies.CreatureID;
            pet.PacketInfo.DisplayID = display;
            pet.PacketInfo.Level = level;
            pet.PacketInfo.Exp = 0;
            pet.PacketInfo.Flags = 0;
            pet.PacketInfo.Breed = breed;
            pet.PacketInfo.Quality = (byte)quality;
            pet.PacketInfo.Name = "";
            pet.CalculateStats();
            pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

            Player player = _owner.GetPlayer();
            if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.NotAccountWide))
            {
                BattlePetStruct.BattlePetOwnerInfo battlePetOwnerInfo = new();
                battlePetOwnerInfo.Guid = player.GetGUID();
                battlePetOwnerInfo.PlayerVirtualRealm = player.m_playerData.VirtualPlayerRealm;
                battlePetOwnerInfo.PlayerNativeRealm = Global.WorldMgr.GetVirtualRealmAddress();
                pet.PacketInfo.OwnerInfo = battlePetOwnerInfo;
            }

            pet.SaveInfo = BattlePetSaveInfo.New;

            _pets[pet.PacketInfo.Guid.GetCounter()] = pet;

            List<BattlePet> updates = [pet];
            SendUpdates(updates, true);

            player.UpdateCriteria(CriteriaType.UniquePetsOwned);
            player.UpdateCriteria(CriteriaType.LearnedNewPet, species);
        }

        public void RemovePet(ObjectGuid guid)
        {
            if (!HasJournalLock())
                return;

            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            pet.SaveInfo = BattlePetSaveInfo.Removed;
        }

        public void ClearFanfare(ObjectGuid guid)
        {
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            pet.PacketInfo.Flags &= (ushort)~BattlePetDbFlags.FanfareNeeded;

            if (pet.SaveInfo != BattlePetSaveInfo.New)
                pet.SaveInfo = BattlePetSaveInfo.Changed;
        }

        public void ModifyName(ObjectGuid guid, string name, DeclinedName declinedName)
        {
            if (!HasJournalLock())
                return;

            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            pet.PacketInfo.Name = name;
            pet.NameTimestamp = GameTime.GetGameTime();

            pet.DeclinedName = new DeclinedName();
            if (declinedName != null)
                pet.DeclinedName = declinedName;

            if (pet.SaveInfo != BattlePetSaveInfo.New)
                pet.SaveInfo = BattlePetSaveInfo.Changed;

            // Update the timestamp if the battle pet is summoned
            Creature summonedBattlePet = _owner.GetPlayer().GetSummonedBattlePet();
            if (summonedBattlePet != null)
                if (summonedBattlePet.GetBattlePetCompanionGUID() == guid)
                    summonedBattlePet.SetBattlePetCompanionNameTimestamp((uint)pet.NameTimestamp);
        }

        bool IsPetInSlot(ObjectGuid guid)
        {
            foreach (BattlePetSlot slot in _slots)
                if (slot.Pet.Guid == guid)
                    return true;

            return false;
        }

        public byte GetPetCount(BattlePetSpeciesRecord battlePetSpecies, ObjectGuid ownerGuid)
        {
            return (byte)_pets.Values.Count(battlePet =>
            {
                if (battlePet.PacketInfo.Species != battlePetSpecies.Id)
                    return false;

                if (battlePet.SaveInfo == BattlePetSaveInfo.Removed)
                    return false;

                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.NotAccountWide))
                    if (!ownerGuid.IsEmpty() && battlePet.PacketInfo.OwnerInfo.HasValue)
                        if (battlePet.PacketInfo.OwnerInfo.Value.Guid != ownerGuid)
                            return false;

                return true;
            });
        }

        public bool HasMaxPetCount(BattlePetSpeciesRecord battlePetSpecies, ObjectGuid ownerGuid)
        {
            int maxPetsPerSpecies = battlePetSpecies.HasFlag(BattlePetSpeciesFlags.LegacyAccountUnique) ? 1 : SharedConst.DefaultMaxBattlePetsPerSpecies;
            return GetPetCount(battlePetSpecies, ownerGuid) >= maxPetsPerSpecies;
        }

        public uint GetPetUniqueSpeciesCount()
        {
            HashSet<uint> speciesIds = new();
            foreach (var pair in _pets)
                speciesIds.Add(pair.Value.PacketInfo.Species);

            return (uint)speciesIds.Count;
        }

        public void UnlockSlot(BattlePetSlots slot)
        {
            if (slot >= BattlePetSlots.Count)
                return;

            byte slotIndex = (byte)slot;
            if (!_slots[slotIndex].Locked)
                return;

            _slots[slotIndex].Locked = false;

            PetBattleSlotUpdates updates = new();
            updates.Slots.Add(_slots[slotIndex]);
            updates.AutoSlotted = false; // what's this?
            updates.NewSlot = true; // causes the "new slot unlocked" bubble to appear
            _owner.SendPacket(updates);
        }

        public ushort GetMaxPetLevel()
        {
            ushort level = 0;
            foreach (var pet in _pets)
                if (pet.Value.SaveInfo != BattlePetSaveInfo.Removed)
                    level = Math.Max(level, pet.Value.PacketInfo.Level);

            return level;
        }

        public void CageBattlePet(ObjectGuid guid)
        {
            if (!HasJournalLock())
                return;

            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (battlePetSpecies != null)
                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.NotTradable))
                    return;

            if (IsPetInSlot(guid))
                return;

            if (pet.PacketInfo.Health < pet.PacketInfo.MaxHealth)
                return;

            List<ItemPosCount> dest = new();

            if (_owner.GetPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, SharedConst.BattlePetCageItemId, 1) != InventoryResult.Ok)
                return;

            Item item = _owner.GetPlayer().StoreNewItem(dest, SharedConst.BattlePetCageItemId, true);
            if (item == null)
                return;

            item.SetModifier(ItemModifier.BattlePetSpeciesId, pet.PacketInfo.Species);
            item.SetModifier(ItemModifier.BattlePetBreedData, (uint)(pet.PacketInfo.Breed | (pet.PacketInfo.Quality << 24)));
            item.SetModifier(ItemModifier.BattlePetLevel, pet.PacketInfo.Level);
            item.SetModifier(ItemModifier.BattlePetDisplayId, pet.PacketInfo.DisplayID);

            _owner.GetPlayer().SendNewItem(item, 1, true, false);

            RemovePet(guid);

            BattlePetDeleted deletePet = new();
            deletePet.PetGuid = guid;
            _owner.SendPacket(deletePet);

            // Battle pet despawns if it's summoned
            Player player = _owner.GetPlayer();
            Creature summonedBattlePet = player.GetSummonedBattlePet();
            if (summonedBattlePet != null)
            {
                if (summonedBattlePet.GetBattlePetCompanionGUID() == guid)
                {
                    summonedBattlePet.DespawnOrUnsummon();
                    player.SetBattlePetData(null);
                }
            }
        }

        public void ChangeBattlePetQuality(ObjectGuid guid, BattlePetBreedQuality quality)
        {
            if (!HasJournalLock())
                return;

            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            if (quality > BattlePetBreedQuality.Rare)
                return;

            var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (battlePetSpecies != null)
                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.CantBattle))
                    return;

            byte qualityValue = (byte)quality;
            if (pet.PacketInfo.Quality >= qualityValue)
                return;

            pet.PacketInfo.Quality = qualityValue;
            pet.CalculateStats();
            pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

            if (pet.SaveInfo != BattlePetSaveInfo.New)
                pet.SaveInfo = BattlePetSaveInfo.Changed;

            List<BattlePet> updates = [pet];
            SendUpdates(updates, false);

            // UF::PlayerData::CurrentBattlePetBreedQuality isn't updated (Intended)
            // _owner.GetPlayer().SetCurrentBattlePetBreedQuality(qualityValue);
        }

        public void GrantBattlePetExperience(ObjectGuid guid, ushort xp, BattlePetXpSource xpSource)
        {
            if (!HasJournalLock())
                return;

            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            if (xp <= 0 || xpSource >= BattlePetXpSource.Count)
                return;

            var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (battlePetSpecies != null)
                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.CantBattle))
                    return;

            ushort level = pet.PacketInfo.Level;
            if (level >= SharedConst.MaxBattlePetLevel)
                return;

            var xpEntry = CliDB.BattlePetXPGameTable.GetRow(level);
            if (xpEntry == null)
                return;

            Player player = _owner.GetPlayer();
            ushort nextLevelXp = (ushort)CliDB.GetBattlePetXPPerLevel(xpEntry);

            if (xpSource == BattlePetXpSource.PetBattle)
                xp = (ushort)(xp * player.GetTotalAuraMultiplier(AuraType.ModBattlePetXpPct));

            xp += pet.PacketInfo.Exp;

            while (xp >= nextLevelXp && level < SharedConst.MaxBattlePetLevel)
            {
                xp -= nextLevelXp;

                xpEntry = CliDB.BattlePetXPGameTable.GetRow(++level);
                if (xpEntry == null)
                    return;

                nextLevelXp = (ushort)CliDB.GetBattlePetXPPerLevel(xpEntry);

                player.UpdateCriteria(CriteriaType.BattlePetReachLevel, pet.PacketInfo.Species, level);
                if (xpSource == BattlePetXpSource.PetBattle)
                    player.UpdateCriteria(CriteriaType.ActivelyEarnPetLevel, pet.PacketInfo.Species, level);
            }

            pet.PacketInfo.Level = level;
            pet.PacketInfo.Exp = (ushort)(level < SharedConst.MaxBattlePetLevel ? xp : 0);
            pet.CalculateStats();
            pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

            if (pet.SaveInfo != BattlePetSaveInfo.New)
                pet.SaveInfo = BattlePetSaveInfo.Changed;

            List<BattlePet> updates = [pet];
            SendUpdates(updates, false);
        }

        public void GrantBattlePetLevel(ObjectGuid guid, ushort grantedLevels)
        {
            if (!HasJournalLock())
                return;

            var pet = GetPet(guid);
            if (pet == null)
                return;

            var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (battlePetSpecies != null)
                if (battlePetSpecies.HasFlag(BattlePetSpeciesFlags.CantBattle))
                    return;

            ushort level = pet.PacketInfo.Level;
            if (level >= SharedConst.MaxBattlePetLevel)
                return;

            while (grantedLevels > 0 && level < SharedConst.MaxBattlePetLevel)
            {
                ++level;
                --grantedLevels;

                _owner.GetPlayer().UpdateCriteria(CriteriaType.BattlePetReachLevel, pet.PacketInfo.Species, level);
            }

            pet.PacketInfo.Level = level;
            if (level >= SharedConst.MaxBattlePetLevel)
                pet.PacketInfo.Exp = 0;
            pet.CalculateStats();
            pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

            if (pet.SaveInfo != BattlePetSaveInfo.New)
                pet.SaveInfo = BattlePetSaveInfo.Changed;

            List<BattlePet> updates = [pet];
            SendUpdates(updates, false);
        }

        public void HealBattlePetsPct(byte pct)
        {
            // TODO: After each Pet Battle, any injured companion will automatically
            // regain 50 % of the damage that was taken during combat
            List<BattlePet> updates = new();

            foreach (var (_, pet) in _pets)
            {
                if (pet.PacketInfo.Health == pet.PacketInfo.MaxHealth)
                    continue;

                pet.PacketInfo.Health += MathFunctions.CalculatePct(pet.PacketInfo.MaxHealth, pct);
                // don't allow Health to be greater than MaxHealth
                pet.PacketInfo.Health = Math.Min(pet.PacketInfo.Health, pet.PacketInfo.MaxHealth);
                if (pet.SaveInfo != BattlePetSaveInfo.New)
                    pet.SaveInfo = BattlePetSaveInfo.Changed;

                updates.Add(pet);
            }

            SendUpdates(updates, false);
        }

        public void UpdateBattlePetData(ObjectGuid guid)
        {
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            Player player = _owner.GetPlayer();

            // Update battle pet related update fields
            Creature summonedBattlePet = player.GetSummonedBattlePet();
            if (summonedBattlePet != null)
            {
                if (summonedBattlePet.GetBattlePetCompanionGUID() == guid)
                {
                    summonedBattlePet.SetWildBattlePetLevel(pet.PacketInfo.Level);
                    player.SetBattlePetData(pet);
                }
            }
        }

        public void SummonPet(ObjectGuid guid)
        {
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (speciesEntry == null)
                return;

            Player player = _owner.GetPlayer();
            player.SetBattlePetData(pet);

            CastSpellExtraArgs args = new();
            uint summonSpellId = speciesEntry.SummonSpellID;
            if (summonSpellId == 0)
            {
                summonSpellId = SharedConst.SpellSummonBattlePet;
                args.AddSpellMod(SpellValueMod.BasePoint0, (int)speciesEntry.CreatureID);
            }
            player.CastSpell(_owner.GetPlayer(), summonSpellId, args);
        }

        public void DismissPet()
        {
            Player player = _owner.GetPlayer();
            Creature summonedBattlePet = player.GetSummonedBattlePet();
            if (summonedBattlePet != null)
            {
                summonedBattlePet.DespawnOrUnsummon();
                player.SetBattlePetData(null);
            }
        }

        public void SendJournal()
        {
            if (!HasJournalLock())
                SendJournalLockStatus();

            BattlePetJournal battlePetJournal = new();
            battlePetJournal.Trap = _trapLevel;
            battlePetJournal.HasJournalLock = _hasJournalLock;

            foreach (var pet in _pets)
                if (pet.Value.SaveInfo != BattlePetSaveInfo.Removed)
                    if (!pet.Value.PacketInfo.OwnerInfo.HasValue || pet.Value.PacketInfo.OwnerInfo.Value.Guid == _owner.GetPlayer().GetGUID())
                        battlePetJournal.Pets.Add(pet.Value.PacketInfo);

            battlePetJournal.Slots = _slots;
            _owner.SendPacket(battlePetJournal);
        }

        void SendUpdates(List<BattlePet> pets, bool petAdded)
        {
            BattlePetUpdates updates = new();
            foreach (var pet in pets)
                updates.Pets.Add(pet.PacketInfo);

            updates.PetAdded = petAdded;
            _owner.SendPacket(updates);
        }

        public void SendError(BattlePetError error, uint creatureId)
        {
            BattlePetErrorPacket battlePetError = new();
            battlePetError.Result = error;
            battlePetError.CreatureID = creatureId;
            _owner.SendPacket(battlePetError);
        }

        public void SendJournalLockStatus()
        {
            if (!IsJournalLockAcquired())
                ToggleJournalLock(true);

            if (HasJournalLock())
                _owner.SendPacket(new BattlePetJournalLockAcquired());
            else
                _owner.SendPacket(new BattlePetJournalLockDenied());
        }

        public bool IsJournalLockAcquired()
        {
            return Global.WorldMgr.IsBattlePetJournalLockAcquired(_owner.GetBattlenetAccountGUID());
        }

        public BattlePetSlot GetSlot(BattlePetSlots slot) { return slot < BattlePetSlots.Count ? _slots[(byte)slot] : null; }
        WorldSession GetOwner() { return _owner; }

        public ushort GetTrapLevel() { return _trapLevel; }
        public List<BattlePetSlot> GetSlots() { return _slots; }

        public bool HasJournalLock() { return _hasJournalLock; }
        public void ToggleJournalLock(bool on) { _hasJournalLock = on; }

        public bool IsBattlePetSystemEnabled() { return GetSlot(BattlePetSlots.Slot0).Locked != true; }

        WorldSession _owner;
        bool _hasJournalLock;
        ushort _trapLevel;
        Dictionary<ulong, BattlePet> _pets = new();
        List<BattlePetSlot> _slots = new();

        public static Dictionary<uint, Dictionary<BattlePetState, int>> BattlePetBreedStates = new();
        public static Dictionary<uint, Dictionary<BattlePetState, int>> BattlePetSpeciesStates = new();
        static Dictionary<uint, BattlePetSpeciesRecord> _battlePetSpeciesByCreature = new();
        static Dictionary<uint, BattlePetSpeciesRecord> _battlePetSpeciesBySpell = new();
        static MultiMap<uint, byte> _availableBreedsPerSpecies = new();
        static Dictionary<uint, BattlePetBreedQuality> _defaultQualityPerSpecies = new();
    }

    public class BattlePet
    {
        public void CalculateStats()
        {
            // get base breed stats
            var breedState = BattlePetMgr.BattlePetBreedStates.LookupByKey(PacketInfo.Breed);
            if (breedState == null) // non existing breed id
                return;

            float health = breedState[BattlePetState.StatStamina];
            float power = breedState[BattlePetState.StatPower];
            float speed = breedState[BattlePetState.StatSpeed];

            // modify stats depending on species - not all pets have this
            var speciesState = BattlePetMgr.BattlePetSpeciesStates.LookupByKey(PacketInfo.Species);
            if (speciesState != null)
            {
                health += speciesState[BattlePetState.StatStamina];
                power += speciesState[BattlePetState.StatPower];
                speed += speciesState[BattlePetState.StatSpeed];
            }

            // modify stats by quality
            foreach (var battlePetBreedQuality in CliDB.BattlePetBreedQualityStorage.Values)
            {
                if (battlePetBreedQuality.QualityEnum == PacketInfo.Quality)
                {
                    health *= battlePetBreedQuality.StateMultiplier;
                    power *= battlePetBreedQuality.StateMultiplier;
                    speed *= battlePetBreedQuality.StateMultiplier;
                    break;
                }
                // TOOD: add check if pet has existing quality
            }

            // scale stats depending on level
            health *= PacketInfo.Level;
            power *= PacketInfo.Level;
            speed *= PacketInfo.Level;

            // set stats
            // round, ceil or floor? verify this
            PacketInfo.MaxHealth = (uint)((Math.Round(health / 20) + 100));
            PacketInfo.Power = (uint)(Math.Round(power / 100));
            PacketInfo.Speed = (uint)(Math.Round(speed / 100));
        }

        public BattlePetStruct PacketInfo;
        public long NameTimestamp;
        public DeclinedName DeclinedName;
        public BattlePetSaveInfo SaveInfo;
    }
}
