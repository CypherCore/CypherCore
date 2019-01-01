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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
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
            for (byte i = 0; i < SharedConst.MaxPetBattleSlots; ++i)
            {
                BattlePetSlot slot = new BattlePetSlot();
                slot.Index = i;
                _slots.Add(slot);
            }
        }

        public static void Initialize()
        {
            SQLResult result = DB.Login.Query("SELECT MAX(guid) FROM battle_pets");
            if (!result.IsEmpty())
                Global.ObjectMgr.GetGenerator(HighGuid.BattlePet).Set(result.Read<ulong>(0) + 1);

            foreach (var battlePetBreedState in CliDB.BattlePetBreedStateStorage.Values)
            {
                if (!_battlePetBreedStates.ContainsKey(battlePetBreedState.BattlePetBreedID))
                    _battlePetBreedStates[battlePetBreedState.BattlePetBreedID] = new Dictionary<BattlePetState, int>();

                _battlePetBreedStates[battlePetBreedState.BattlePetBreedID][(BattlePetState)battlePetBreedState.BattlePetStateID] = battlePetBreedState.Value;
            }

            foreach (var battlePetSpeciesState in CliDB.BattlePetSpeciesStateStorage.Values)
            {
                if (!_battlePetSpeciesStates.ContainsKey(battlePetSpeciesState.BattlePetSpeciesID))
                    _battlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID] = new Dictionary<BattlePetState, int>();

                _battlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID][(BattlePetState)battlePetSpeciesState.BattlePetStateID] = battlePetSpeciesState.Value;
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
                byte quality = result.Read<byte>(1);

                if (!CliDB.BattlePetSpeciesStorage.ContainsKey(speciesId))
                {
                    Log.outError(LogFilter.Sql, "Non-existing BattlePetSpecies.db2 entry {0} was referenced in `battle_pet_quality` by row ({1}, {2}).", speciesId, speciesId, quality);
                    continue;
                }

                // TODO: verify quality (0 - 3 for player pets or 0 - 5 for both player and tamer pets) if needed

                _defaultQualityPerSpecies[speciesId] = quality;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} battle pet qualities.", _defaultQualityPerSpecies.Count);
        }

        public static ushort RollPetBreed(uint species)
        {
            var list = _availableBreedsPerSpecies.LookupByKey(species);
            if (list.Empty())
                return 3; // default B/B

            return list.SelectRandom();
        }

        public static byte GetDefaultPetQuality(uint species)
        {
            if (!_defaultQualityPerSpecies.ContainsKey(species))
                return 0; // default poor

            return _defaultQualityPerSpecies[species];
        }

        public void LoadFromDB(SQLResult petsResult, SQLResult slotsResult)
        {
            if (!petsResult.IsEmpty())
            {
                do
                {
                    uint species = petsResult.Read<uint>(1);

                    BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(species);
                    if (speciesEntry != null)
                    {
                        if (GetPetCount(species) >= SharedConst.MaxBattlePetsPerSpecies)
                        {
                            Log.outError(LogFilter.Misc, "Battlenet account with id {0} has more than 3 battle pets of species {1}", _owner.GetBattlenetAccountId(), species);
                            continue;
                        }

                        BattlePet pet = new BattlePet();
                        pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, petsResult.Read<ulong>(0));
                        pet.PacketInfo.Species = species;
                        pet.PacketInfo.Breed = petsResult.Read<ushort>(2);
                        pet.PacketInfo.Level = petsResult.Read<ushort>(3);
                        pet.PacketInfo.Exp = petsResult.Read<ushort>(4);
                        pet.PacketInfo.Health = petsResult.Read<uint>(5);
                        pet.PacketInfo.Quality = petsResult.Read<byte>(6);
                        pet.PacketInfo.Flags = petsResult.Read<ushort>(7);
                        pet.PacketInfo.Name = petsResult.Read<string>(8);
                        pet.PacketInfo.CreatureID = speciesEntry.CreatureID;
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
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BATTLE_PETS);
                        stmt.AddValue(0, pair.Key);
                        stmt.AddValue(1, _owner.GetBattlenetAccountId());
                        stmt.AddValue(2, pair.Value.PacketInfo.Species);
                        stmt.AddValue(3, pair.Value.PacketInfo.Breed);
                        stmt.AddValue(4, pair.Value.PacketInfo.Level);
                        stmt.AddValue(5, pair.Value.PacketInfo.Exp);
                        stmt.AddValue(6, pair.Value.PacketInfo.Health);
                        stmt.AddValue(7, pair.Value.PacketInfo.Quality);
                        stmt.AddValue(8, pair.Value.PacketInfo.Flags);
                        stmt.AddValue(9, pair.Value.PacketInfo.Name);
                        trans.Append(stmt);
                        pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;
                        break;
                    case BattlePetSaveInfo.Changed:
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BATTLE_PETS);
                        stmt.AddValue(0, pair.Value.PacketInfo.Level);
                        stmt.AddValue(1, pair.Value.PacketInfo.Exp);
                        stmt.AddValue(2, pair.Value.PacketInfo.Health);
                        stmt.AddValue(3, pair.Value.PacketInfo.Quality);
                        stmt.AddValue(4, pair.Value.PacketInfo.Flags);
                        stmt.AddValue(5, pair.Value.PacketInfo.Name);
                        stmt.AddValue(6, _owner.GetBattlenetAccountId());
                        stmt.AddValue(7, pair.Key);
                        trans.Append(stmt);
                        pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;
                        break;
                    case BattlePetSaveInfo.Removed:
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS);
                        stmt.AddValue(0, _owner.GetBattlenetAccountId());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);
                        _pets.Remove(pair.Key);
                        break;
                }
            }

            stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_SLOTS);
            stmt.AddValue(0, _owner.GetBattlenetAccountId());
            trans.Append(stmt);

            foreach (var slot in _slots)
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_SLOTS);
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

        public void AddPet(uint species, uint creatureId, ushort level = 1)
        {
            ushort breed = 3;// default B/B
            byte quality = 0;

            if (_availableBreedsPerSpecies.ContainsKey(species))
                breed = _availableBreedsPerSpecies[species].SelectRandom();

            if (_defaultQualityPerSpecies.ContainsKey(species))
                quality = _defaultQualityPerSpecies[species];

            AddPet(species, creatureId, breed, quality, level);
        }

        public void AddPet(uint species, uint creatureId, ushort breed, byte quality, ushort level = 1)
        {
            BattlePetSpeciesRecord battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(species);
            if (battlePetSpecies == null) // should never happen
                return;

            BattlePet pet = new BattlePet();
            pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, Global.ObjectMgr.GetGenerator(HighGuid.BattlePet).Generate());
            pet.PacketInfo.Species = species;
            pet.PacketInfo.CreatureID = creatureId;
            pet.PacketInfo.Level = level;
            pet.PacketInfo.Exp = 0;
            pet.PacketInfo.Flags = 0;
            pet.PacketInfo.Breed = breed;
            pet.PacketInfo.Quality = quality;
            pet.PacketInfo.Name = "";
            pet.CalculateStats();
            pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;
            pet.SaveInfo = BattlePetSaveInfo.New;

            _pets[pet.PacketInfo.Guid.GetCounter()] = pet;

            List<BattlePet> updates = new List<BattlePet>();
            updates.Add(pet);
            SendUpdates(updates, true);

            _owner.GetPlayer().UpdateCriteria(CriteriaTypes.OwnBattlePet, species);
        }

        public void RemovePet(ObjectGuid guid)
        {
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            pet.SaveInfo = BattlePetSaveInfo.Removed;

            // spell is not unlearned on retail
            /*if (GetPetCount(pet.PacketInfo.Species) == 0)
                if (BattlePetSpeciesEntry const* speciesEntry = sBattlePetSpeciesStore.LookupEntry(pet.PacketInfo.Species))
                    _owner.GetPlayer().RemoveSpell(speciesEntry.SummonSpellID);*/
        }

        public byte GetPetCount(uint species)
        {
            return (byte)_pets.Values.Count(battlePet => battlePet.PacketInfo.Species == species && battlePet.SaveInfo != BattlePetSaveInfo.Removed);
        }

        public void UnlockSlot(byte slot)
        {
            if (!_slots[slot].Locked)
                return;

            _slots[slot].Locked = false;

            PetBattleSlotUpdates updates = new PetBattleSlotUpdates();
            updates.Slots.Add(_slots[slot]);
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
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            List<ItemPosCount> dest = new List<ItemPosCount>();

            if (_owner.GetPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, SharedConst.BattlePetCageItemId, 1) != InventoryResult.Ok)
                return;

            Item item = _owner.GetPlayer().StoreNewItem(dest, SharedConst.BattlePetCageItemId, true);
            if (!item)
                return;

            item.SetModifier(ItemModifier.BattlePetSpeciesId, pet.PacketInfo.Species);
            item.SetModifier(ItemModifier.BattlePetBreedData, (uint)(pet.PacketInfo.Breed | (pet.PacketInfo.Quality << 24)));
            item.SetModifier(ItemModifier.BattlePetLevel, pet.PacketInfo.Level);
            item.SetModifier(ItemModifier.BattlePetDisplayId, pet.PacketInfo.CreatureID);

            // FIXME: "You create: ." - item name missing in chat
            _owner.GetPlayer().SendNewItem(item, 1, true, false);

            RemovePet(guid);

            BattlePetDeleted deletePet = new BattlePetDeleted();
            deletePet.PetGuid = guid;
            _owner.SendPacket(deletePet);
        }

        public void HealBattlePetsPct(byte pct)
        {
            // TODO: After each Pet Battle, any injured companion will automatically
            // regain 50 % of the damage that was taken during combat
            List<BattlePet> updates = new List<BattlePet>();

            foreach (var pet in _pets.Values)
            {
                if (pet.PacketInfo.Health != pet.PacketInfo.MaxHealth)
                {
                    pet.PacketInfo.Health += MathFunctions.CalculatePct(pet.PacketInfo.MaxHealth, pct);
                    // don't allow Health to be greater than MaxHealth
                    pet.PacketInfo.Health = Math.Min(pet.PacketInfo.Health, pet.PacketInfo.MaxHealth);
                    if (pet.SaveInfo != BattlePetSaveInfo.New)
                        pet.SaveInfo = BattlePetSaveInfo.Changed;
                    updates.Add(pet);
                }
            }

            SendUpdates(updates, false);
        }

        public void SummonPet(ObjectGuid guid)
        {
            BattlePet pet = GetPet(guid);
            if (pet == null)
                return;

            BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);
            if (speciesEntry == null)
                return;

            // TODO: set proper CreatureID for spell DEFAULT_SUMMON_BATTLE_PET_SPELL (default EffectMiscValueA is 40721 - Murkimus the Gladiator)
            _owner.GetPlayer().SetGuidValue(ActivePlayerFields.SummonedBattlePetId, guid);
            _owner.GetPlayer().CastSpell(_owner.GetPlayer(), speciesEntry.SummonSpellID != 0 ? speciesEntry.SummonSpellID : SharedConst.DefaultSummonBattlePetSpell);

            // TODO: set pet level, quality... update fields
        }

        public void DismissPet()
        {
            Player ownerPlayer = _owner.GetPlayer();
            Creature pet = ObjectAccessor.GetCreatureOrPetOrVehicle(ownerPlayer, ownerPlayer.GetCritterGUID());
            if (pet && ownerPlayer.GetGuidValue(ActivePlayerFields.SummonedBattlePetId) == pet.GetGuidValue(UnitFields.BattlePetCompanionGuid))
            {
                pet.DespawnOrUnsummon();
                ownerPlayer.SetGuidValue(ActivePlayerFields.SummonedBattlePetId, ObjectGuid.Empty);
            }
        }

        public void SendJournal()
        {
            BattlePetJournal battlePetJournal = new BattlePetJournal();
            battlePetJournal.Trap = _trapLevel;

            foreach (var pet in _pets)
                if (pet.Value.SaveInfo != BattlePetSaveInfo.Removed)
                    battlePetJournal.Pets.Add(pet.Value.PacketInfo);

            battlePetJournal.Slots = _slots;
            _owner.SendPacket(battlePetJournal);
        }

        void SendUpdates(List<BattlePet> pets, bool petAdded)
        {
            BattlePetUpdates updates = new BattlePetUpdates();
            foreach (var pet in pets)
                updates.Pets.Add(pet.PacketInfo);

            updates.PetAdded = petAdded;
            _owner.SendPacket(updates);
        }

        public void SendError(BattlePetError error, uint creatureId)
        {
            BattlePetErrorPacket battlePetError = new BattlePetErrorPacket();
            battlePetError.Result = error;
            battlePetError.CreatureID = creatureId;
            _owner.SendPacket(battlePetError);
        }

        public BattlePetSlot GetSlot(byte slot) { return _slots[slot]; }
        WorldSession GetOwner() { return _owner; }

        public ushort GetTrapLevel() { return _trapLevel; }
        public List<BattlePetSlot> GetSlots() { return _slots; }

        WorldSession _owner;
        ushort _trapLevel;
        Dictionary<ulong, BattlePet> _pets = new Dictionary<ulong, BattlePet>();
        List<BattlePetSlot> _slots = new List<BattlePetSlot>();

        static Dictionary<uint, Dictionary<BattlePetState, int>> _battlePetBreedStates = new Dictionary<uint, Dictionary<BattlePetState, int>>();
        static Dictionary<uint, Dictionary<BattlePetState, int>> _battlePetSpeciesStates = new Dictionary<uint, Dictionary<BattlePetState, int>>();
        static MultiMap<uint, byte> _availableBreedsPerSpecies = new MultiMap<uint, byte>();
        static Dictionary<uint, byte> _defaultQualityPerSpecies = new Dictionary<uint, byte>();

        public class BattlePet
        {
            public void CalculateStats()
            {
                float health = 0.0f;
                float power = 0.0f;
                float speed = 0.0f;

                // get base breed stats
                var breedState = _battlePetBreedStates.LookupByKey(PacketInfo.Breed);
                if (breedState == null) // non existing breed id
                    return;

                health = breedState[BattlePetState.StatStamina];
                power = breedState[BattlePetState.StatPower];
                speed = breedState[BattlePetState.StatSpeed];

                // modify stats depending on species - not all pets have this
                var speciesState = _battlePetSpeciesStates.LookupByKey(PacketInfo.Species);
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
            public BattlePetSaveInfo SaveInfo;
        }
    }
}
