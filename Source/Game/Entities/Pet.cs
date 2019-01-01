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
using Game.Maps;
using Game.Network.Packets;
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

            m_unitTypeMask |= UnitTypeMask.Pet;
            if (type == PetType.Hunter)
                m_unitTypeMask |= UnitTypeMask.HunterPet;

            if (!m_unitTypeMask.HasAnyFlag(UnitTypeMask.ControlableGuardian))
            {
                m_unitTypeMask |= UnitTypeMask.ControlableGuardian;
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

        public bool LoadPetFromDB(Player owner, uint petEntry = 0, uint petnumber = 0, bool current = false)
        {
            m_loading = true;

            ulong ownerid = owner.GetGUID().GetCounter();

            PreparedStatement stmt = null;

            if (petnumber != 0)
            {
                // Known petnumber entry
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY);
                stmt.AddValue(0, ownerid);
                stmt.AddValue(1, petnumber);
            }
            else if (current)
            {
                // Current pet (slot 0)
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY_AND_SLOT);
                stmt.AddValue(0, ownerid);
                stmt.AddValue(1, PetSaveMode.AsCurrent);
            }
            else if (petEntry != 0)
            {
                // known petEntry entry (unique for summoned pet, but non unique for hunter pet (only from current or not stabled pets)
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY_AND_SLOT_2);
                stmt.AddValue(0, ownerid);
                stmt.AddValue(1, petEntry);
                stmt.AddValue(2, PetSaveMode.AsCurrent);
                stmt.AddValue(3, PetSaveMode.LastStableSlot);
            }
            else
            {
                // Any current or other non-stabled pet (for hunter "call pet")
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_BY_SLOT);
                stmt.AddValue(0, ownerid);
                stmt.AddValue(1, PetSaveMode.AsCurrent);
                stmt.AddValue(2, PetSaveMode.LastStableSlot);
            }

            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                m_loading = false;
                return false;
            }

            // update for case of current pet "slot = 0"
            petEntry = result.Read<uint>(1);
            if (petEntry == 0)
                return false;

            uint summonSpellId = result.Read<uint>(14);
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(summonSpellId);

            bool isTemporarySummon = spellInfo != null && spellInfo.GetDuration() > 0;
            if (current && isTemporarySummon)
                return false;

            PetType petType = (PetType)result.Read<byte>(15);
            if (petType == PetType.Hunter)
            {
                CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(petEntry);
                if (creatureInfo == null || !creatureInfo.IsTameable(owner.CanTameExoticPets()))
                    return false;
            }

            uint petId = result.Read<uint>(0);
            if (current && owner.IsPetNeedBeTemporaryUnsummoned())
            {
                owner.SetTemporaryUnsummonedPetNumber(petId);
                return false;
            }

            Map map = owner.GetMap();
            if (!Create(map.GenerateLowGuid(HighGuid.Pet), map, petEntry))
                return false;

            PhasingHandler.InheritPhaseShift(this, owner);

            setPetType(petType);
            SetFaction(owner.getFaction());
            SetUInt32Value(UnitFields.CreatedBySpell, summonSpellId);

            float px, py, pz;
            if (IsCritter())
            {
                owner.GetClosePoint(out px, out py, out pz, GetObjectSize(), SharedConst.PetFollowDist, GetFollowAngle());
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

            GetCharmInfo().SetPetNumber(petId, IsPermanentPetFor(owner));

            SetDisplayId(result.Read<uint>(3));
            SetNativeDisplayId(result.Read<uint>(3));
            uint petlevel = result.Read<ushort>(4);
            SetUInt64Value(UnitFields.NpcFlags, (ulong)NPCFlags.None);
            SetName(result.Read<string>(8));

            switch (getPetType())
            {
                case PetType.Summon:
                    petlevel = owner.getLevel();

                    SetByteValue(UnitFields.Bytes0, 1, (byte)Class.Mage);
                    SetUInt32Value(UnitFields.Flags, (uint)UnitFlags.PvpAttackable); // this enables popup window (pet dismiss, cancel)
                    break;
                case PetType.Hunter:
                    SetByteValue(UnitFields.Bytes0, 1, (byte)Class.Warrior);
                    SetByteValue(UnitFields.Bytes0, 3, (byte)Gender.None);
                    SetSheath(SheathState.Melee);
                    SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, (result.Read<bool>(9) ? UnitPetFlags.CanBeAbandoned : UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned));

                    SetUInt32Value(UnitFields.Flags, (uint)UnitFlags.PvpAttackable); // this enables popup window (pet abandon, cancel)
                    break;
                default:
                    if (!IsPetGhoul())
                        Log.outError(LogFilter.Pet, "Pet have incorrect type ({0}) for pet loading.", getPetType());
                    break;
            }

            SetUInt32Value(UnitFields.PetNameTimestamp, (uint)Time.UnixTime); // cast can't be helped here
            SetCreatorGUID(owner.GetGUID());

            InitStatsForLevel(petlevel);
            SetUInt32Value(UnitFields.PetExperience, result.Read<uint>(5));

            SynchronizeLevelWithOwner();

            // Set pet's position after setting level, its size depends on it
            owner.GetClosePoint(out px, out py, out pz, GetObjectSize(), SharedConst.PetFollowDist, GetFollowAngle());
            Relocate(px, py, pz, owner.GetOrientation());
            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Pet, "Pet ({0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})", GetGUID().ToString(), GetEntry(), GetPositionX(), GetPositionY());
                return false;
            }

            SetReactState((ReactStates)result.Read<byte>(6));
            SetCanModifyStats(true);

            if (getPetType() == PetType.Summon && !current)              //all (?) summon pets come with full health when called, but not when they are current
                SetFullPower(PowerType.Mana);
            else
            {
                uint savedhealth = result.Read<uint>(10);
                uint savedmana = result.Read<uint>(11);
                if (savedhealth == 0 && getPetType() == PetType.Hunter)
                    setDeathState(DeathState.JustDied);
                else
                {
                    SetHealth(savedhealth);
                    SetPower(PowerType.Mana, (int)savedmana);
                }
            }

            // set current pet as current
            // 0=current
            // 1..MAX_PET_STABLES in stable slot
            // PET_SAVE_NOT_IN_SLOT(100) = not stable slot (summoning))
            if (result.Read<byte>(7) != 0)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_SLOT_EXCLUDE_ID);
                stmt.AddValue(0, PetSaveMode.NotInSlot);
                stmt.AddValue(1, ownerid);
                stmt.AddValue(2, PetSaveMode.AsCurrent);
                stmt.AddValue(3, GetCharmInfo().GetPetNumber());
                DB.Characters.Execute(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_ID);
                stmt.AddValue(0, PetSaveMode.AsCurrent);
                stmt.AddValue(1, ownerid);
                stmt.AddValue(2, GetCharmInfo().GetPetNumber());
                DB.Characters.Execute(stmt);
            }

            // Send fake summon spell cast - this is needed for correct cooldown application for spells
            // Example: 46584 - without this cooldown (which should be set always when pet is loaded) isn't set clientside
            // @todo pets should be summoned from real cast instead of just faking it?
            if (summonSpellId != 0)
            {
                SpellGo spellGo = new SpellGo();
                SpellCastData castData = spellGo.Cast;

                castData.CasterGUID = owner.GetGUID();
                castData.CasterUnit = owner.GetGUID();
                castData.CastID = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, owner.GetMapId(), summonSpellId, map.GenerateLowGuid(HighGuid.Cast));
                castData.SpellID = (int)summonSpellId;
                castData.CastFlags = SpellCastFlags.Unk9;
                castData.CastTime = Time.GetMSTime();
                owner.SendMessageToSet(spellGo, true);
            }

            owner.SetMinion(this, true);
            map.AddToMap(ToCreature());

            uint timediff = (uint)(Time.UnixTime - result.Read<uint>(13));
            _LoadAuras(timediff);

            // load action bar, if data broken will fill later by default spells.
            if (!isTemporarySummon)
            {
                GetCharmInfo().LoadPetActionBar(result.Read<string>(12));

                _LoadSpells();
                _LoadSpellCooldowns();
                LearnPetPassives();
                InitLevelupSpellsForLevel();
                if (map.IsBattleArena())
                    RemoveArenaAuras();

                CastPetAuras(current);
            }

            Log.outDebug(LogFilter.Pet, "New Pet has guid {0}", GetGUID().ToString());

            ushort specId = result.Read<ushort>(16);
            ChrSpecializationRecord petSpec = CliDB.ChrSpecializationStorage.LookupByKey(specId);
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

            if (getPetType() == PetType.Hunter)
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_DECLINED_NAME);
                stmt.AddValue(0, owner.GetGUID().GetCounter());
                stmt.AddValue(1, GetCharmInfo().GetPetNumber());
                result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                {
                    _declinedname = new DeclinedName();
                    for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    {
                        _declinedname.name[i] = result.Read<string>(i);
                    }
                }
            }

            //set last used pet number (for use in BG's)
            if (owner.IsTypeId(TypeId.Player) && isControlled() && !isTemporarySummoned() && (getPetType() == PetType.Summon || getPetType() == PetType.Hunter))
                owner.ToPlayer().SetLastPetNumber(petId);

            m_loading = false;

            return true;
        }

        public void SavePetToDB(PetSaveMode mode)
        {
            if (GetEntry() == 0)
                return;

            // save only fully controlled creature
            if (!isControlled())
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
                if (getPetType() == PetType.Hunter)
                    return;

                // for warlock case
                mode = PetSaveMode.NotInSlot;
            }

            uint curhealth = (uint)GetHealth();
            int curmana = GetPower(PowerType.Mana);

            SQLTransaction trans = new SQLTransaction();
            // save auras before possibly removing them    
            _SaveAuras(trans);

            // stable and not in slot saves
            if (mode > PetSaveMode.AsCurrent)
                RemoveAllAuras();

            _SaveSpells(trans);
            GetSpellHistory().SaveToDB<Pet>(trans);
            DB.Characters.CommitTransaction(trans);

            // current/stable/not_in_slot
            if (mode >= PetSaveMode.AsCurrent)
            {
                ulong ownerLowGUID = GetOwnerGUID().GetCounter();
                trans = new SQLTransaction();

                // remove current data
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                trans.Append(stmt);

                // prevent duplicate using slot (except PET_SAVE_NOT_IN_SLOT)
                if (mode <= PetSaveMode.LastStableSlot)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_SLOT);
                    stmt.AddValue(0, PetSaveMode.NotInSlot);
                    stmt.AddValue(1, ownerLowGUID);
                    stmt.AddValue(2, mode);
                    trans.Append(stmt);
                }

                // prevent existence another hunter pet in PET_SAVE_AS_CURRENT and PET_SAVE_NOT_IN_SLOT
                if (getPetType() == PetType.Hunter && (mode == PetSaveMode.AsCurrent || mode > PetSaveMode.LastStableSlot))
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_SLOT);
                    stmt.AddValue(0, ownerLowGUID);
                    stmt.AddValue(1, PetSaveMode.AsCurrent);
                    stmt.AddValue(2, PetSaveMode.LastStableSlot);
                    trans.Append(stmt);
                }

                // save pet
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET);
                stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                stmt.AddValue(1, GetEntry());
                stmt.AddValue(2, ownerLowGUID);
                stmt.AddValue(3, GetNativeDisplayId());
                stmt.AddValue(4, getLevel());
                stmt.AddValue(5, GetUInt32Value(UnitFields.PetExperience));
                stmt.AddValue(6, GetReactState());
                stmt.AddValue(7, mode);
                stmt.AddValue(8, GetName());
                stmt.AddValue(9, HasByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, UnitPetFlags.CanBeRenamed) ? 0 : 1);
                stmt.AddValue(10, curhealth);
                stmt.AddValue(11, curmana);
                stmt.AddValue(12, GenerateActionBarData());
                stmt.AddValue(13, Time.UnixTime);
                stmt.AddValue(14, GetUInt32Value(UnitFields.CreatedBySpell));
                stmt.AddValue(15, getPetType());
                stmt.AddValue(16, m_petSpecialization);
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

        public static void DeleteFromDB(uint guidlow)
        {
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELLS);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
            stmt.AddValue(0, guidlow);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public override void setDeathState(DeathState s)
        {
            base.setDeathState(s);
            if (getDeathState() == DeathState.Corpse)
            {
                if (getPetType() == PetType.Hunter)
                {
                    // pet corpse non lootable and non skinnable
                    SetUInt32Value(ObjectFields.DynamicFlags, (uint)UnitDynFlags.None);
                    RemoveFlag(UnitFields.Flags, UnitFlags.Skinnable);
                }
            }
            else if (getDeathState() == DeathState.Alive)
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
                        if (getPetType() != PetType.Hunter || m_corpseRemoveTime <= Time.UnixTime)
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
                        if (owner == null || (!IsWithinDistInMap(owner, GetMap().GetVisibilityRange()) && !isPossessed()) || (isControlled() && owner.GetPetGUID().IsEmpty()))
                        {
                            Remove(PetSaveMode.NotInSlot, true);
                            return;
                        }

                        if (isControlled())
                        {
                            if (owner.GetPetGUID() != GetGUID())
                            {
                                Log.outError(LogFilter.Pet, "Pet {0} is not pet of owner {1}, removed", GetEntry(), GetOwner().GetName());
                                Remove(getPetType() == PetType.Hunter ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);
                                return;
                            }
                        }

                        if (m_duration > 0)
                        {
                            if (m_duration > diff)
                                m_duration -= (int)diff;
                            else
                            {
                                Remove(getPetType() != PetType.Summon ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);
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
            if (getPetType() != PetType.Hunter)
                return;

            if (xp < 1)
                return;

            if (!IsAlive())
                return;

            uint maxlevel = Math.Min(WorldConfig.GetUIntValue(WorldCfg.MaxPlayerLevel), GetOwner().getLevel());
            uint petlevel = getLevel();

            // If pet is detected to be at, or above(?) the players level, don't hand out XP
            if (petlevel >= maxlevel)
                return;

            uint nextLvlXP = GetUInt32Value(UnitFields.PetNextLevelExp);
            uint curXP = GetUInt32Value(UnitFields.PetExperience);
            uint newXP = curXP + xp;

            // Check how much XP the pet should receive, and hand off have any left from previous levelups
            while (newXP >= nextLvlXP && petlevel < maxlevel)
            {
                // Subtract newXP from amount needed for nextlevel, and give pet the level
                newXP -= nextLvlXP;
                ++petlevel;

                GivePetLevel((int)petlevel);

                nextLvlXP = GetUInt32Value(UnitFields.PetNextLevelExp);
            }
            // Not affected by special conditions - give it new XP
            SetUInt32Value(UnitFields.PetExperience, petlevel < maxlevel ? newXP : 0);
        }

        public void GivePetLevel(int level)
        {
            if (level == 0 || level == getLevel())
                return;

            if (getPetType() == PetType.Hunter)
            {
                SetUInt32Value(UnitFields.PetExperience, 0);
                SetUInt32Value(UnitFields.PetNextLevelExp, (uint)(Global.ObjectMgr.GetXPForLevel((uint)level) * PetXPFactor));
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
            if (!Create(map.GenerateLowGuid(HighGuid.Pet), map, cinfo.Entry))
                return false;

            SetUInt32Value(UnitFields.PetNameTimestamp, 0);
            SetUInt32Value(UnitFields.PetExperience, 0);
            SetUInt32Value(UnitFields.PetNextLevelExp, (uint)(Global.ObjectMgr.GetXPForLevel(getLevel() + 1) * PetXPFactor));
            SetUInt64Value(UnitFields.NpcFlags, (ulong)NPCFlags.None);

            if (cinfo.CreatureType == CreatureType.Beast)
            {
                SetByteValue(UnitFields.Bytes0, 1, (byte)Class.Warrior);
                SetByteValue(UnitFields.Bytes0, 3, (byte)Gender.None);
                SetUInt32Value(UnitFields.DisplayPower, (uint)PowerType.Focus);
                SetSheath(SheathState.Melee);
                SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PetFlags, (UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned));
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

        public uint GetCurrentFoodBenefitLevel(uint itemlevel)
        {
            // -5 or greater food level
            if (getLevel() <= itemlevel + 5)                         //possible to feed level 60 pet with level 55 level food for full effect
                return 35000;
            // -10..-6
            else if (getLevel() <= itemlevel + 10)                   //pure guess, but sounds good
                return 17000;
            // -14..-11
            else if (getLevel() <= itemlevel + 14)                   //level 55 food gets green on 70, makes sense to me
                return 8000;
            // -15 or less
            else
                return 0;                                           //food too low level
        }

        void _LoadSpellCooldowns()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_COOLDOWN);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            SQLResult cooldownsResult = DB.Characters.Query(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL_CHARGES);
            SQLResult chargesResult = DB.Characters.Query(stmt);

            GetSpellHistory().LoadFromDB<Pet>(cooldownsResult, chargesResult);
        }

        void _LoadSpells()
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SPELL);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            SQLResult result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    addSpell(result.Read<uint>(0), (ActiveStates)result.Read<byte>(1), PetSpellState.Unchanged);
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
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        m_spells.Remove(pair.Key);
                        continue;
                    case PetSpellState.Changed:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, pair.Value.active);
                        trans.Append(stmt);
                        break;
                    case PetSpellState.New:
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
                        stmt.AddValue(0, GetCharmInfo().GetPetNumber());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, pair.Value.active);
                        trans.Append(stmt);
                        break;
                    case PetSpellState.Unchanged:
                        continue;
                }
                pair.Value.state = PetSpellState.Unchanged;
            }
        }

        void _LoadAuras(uint timediff)
        {
            Log.outDebug(LogFilter.Pet, "Loading auras for {0}", GetGUID().ToString());

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA_EFFECT);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());

            ObjectGuid casterGuid = new ObjectGuid();
            ObjectGuid itemGuid = new ObjectGuid();
            Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new Dictionary<AuraKey, AuraLoadEffectInfo>();
            SQLResult result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    uint effectIndex = result.Read<byte>(3);
                    if (effectIndex < SpellConst.MaxEffects)
                    {
                        casterGuid.SetRawValue(result.Read<byte[]>(0));
                        if (casterGuid.IsEmpty())
                            casterGuid = GetGUID();

                        AuraKey key = new AuraKey(casterGuid, itemGuid, result.Read<uint>(1), result.Read<uint>(2));
                        if (!effectInfo.ContainsKey(key))
                            effectInfo[key] = new AuraLoadEffectInfo();

                        var info = effectInfo[key];
                        info.Amounts[effectIndex] = result.Read<int>(4);
                        info.BaseAmounts[effectIndex] = result.Read<int>(5);
                    }
                } while (result.NextRow());
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_AURA);
            stmt.AddValue(0, GetCharmInfo().GetPetNumber());
            result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    // NULL guid stored - pet is the caster of the spell - see Pet._SaveAuras
                    casterGuid.SetRawValue(result.Read<byte[]>(0));
                    if (casterGuid.IsEmpty())
                        casterGuid = GetGUID();

                    AuraKey key = new AuraKey(casterGuid, itemGuid, result.Read<uint>(1), result.Read<uint>(2));
                    uint recalculateMask = result.Read<uint>(3);
                    byte stackCount = result.Read<byte>(4);
                    int maxDuration = result.Read<int>(5);
                    int remainTime = result.Read<int>(6);
                    byte remainCharges = result.Read<byte>(7);

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId);
                    if (spellInfo == null)
                    {
                        Log.outError(LogFilter.Pet, "Unknown aura (spellid {0}), ignore.", key.SpellId);
                        continue;
                    }

                    // negative effects should continue counting down after logout
                    if (remainTime != -1 && !spellInfo.IsPositive())
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
                    Aura aura = Aura.TryCreate(spellInfo, castId, key.EffectMask, this, null, info.BaseAmounts, null, casterGuid);
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
                }
                while (result.NextRow());
            }
        }

        void _SaveAuras(SQLTransaction trans)
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
                if (!aura.CanBeSaved() || IsPetAura(aura))
                    continue;

                uint recalculateMask;
                AuraKey key = aura.GenerateKey(out recalculateMask);

                // don't save guid of caster in case we are caster of the spell - guid for pet is generated every pet load, so it won't match saved guid anyways
                if (key.Caster == GetGUID())
                    key.Caster.Clear();

                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_AURA);
                stmt.AddValue(index++, GetCharmInfo().GetPetNumber());
                stmt.AddValue(index++, key.Caster.GetRawValue());
                stmt.AddValue(index++, key.SpellId);
                stmt.AddValue(index++, key.EffectMask);
                stmt.AddValue(index++, recalculateMask);
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
        }

        bool addSpell(uint spellId, ActiveStates active = ActiveStates.Decide, PetSpellState state = PetSpellState.New, PetSpellType type = PetSpellType.Normal)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
            if (spellInfo == null)
            {
                // do pet spell book cleanup
                if (state == PetSpellState.Unchanged)                    // spell load case
                {
                    Log.outError(LogFilter.Pet, "addSpell: Non-existed in SpellStore spell #{0} request, deleting for all pets in `pet_spell`.", spellId);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_PET_SPELL);

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
                {
                    m_spells.Remove(spellId);
                    state = PetSpellState.Changed;
                }
                else if (state == PetSpellState.Unchanged && petSpell.state != PetSpellState.Unchanged)
                {
                    // can be in case spell loading but learned at some previous spell loading
                    petSpell.state = PetSpellState.Unchanged;

                    if (active == ActiveStates.Enabled)
                        ToggleAutocast(spellInfo, true);
                    else if (active == ActiveStates.Disabled)
                        ToggleAutocast(spellInfo, false);

                    return false;
                }
                else
                    return false;
            }

            PetSpell newspell = new PetSpell();
            newspell.state = state;
            newspell.type = type;

            if (active == ActiveStates.Decide)                               // active was not used before, so we save it's autocast/passive state here
            {
                if (spellInfo.IsAutocastable())
                    newspell.active = ActiveStates.Disabled;
                else
                    newspell.active = ActiveStates.Passive;
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

                    SpellInfo oldRankSpellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);

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

                            unlearnSpell(pair.Key, false, false);
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

        public bool learnSpell(uint spell_id)
        {
            // prevent duplicated entires in spell book
            if (!addSpell(spell_id))
                return false;

            if (!m_loading)
            {
                PetLearnedSpells packet = new PetLearnedSpells();
                packet.Spells.Add(spell_id);
                GetOwner().SendPacket(packet);
                GetOwner().PetSpellInitialize();
            }
            return true;
        }

        void learnSpells(List<uint> spellIds)
        {
            PetLearnedSpells packet = new PetLearnedSpells();

            foreach (uint spell in spellIds)
            {
                if (!addSpell(spell))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!m_loading)
                GetOwner().SendPacket(packet);
        }

        void InitLevelupSpellsForLevel()
        {
            uint level = getLevel();
            var levelupSpells = GetCreatureTemplate().Family != 0 ? Global.SpellMgr.GetPetLevelupSpellList(GetCreatureTemplate().Family) : null;
            if (levelupSpells != null)
            {
                // PetLevelupSpellSet ordered by levels, process in reversed order
                foreach (var pair in levelupSpells)
                {
                    // will called first if level down
                    if (pair.Key > level)
                        unlearnSpell(pair.Value, true);                 // will learn prev rank if any
                    // will called if level up
                    else
                        learnSpell(pair.Value);                        // will unlearn prev rank if any
                }
            }

            // default spells (can be not learned if pet level (as owner level decrease result for example) less first possible in normal game)
            PetDefaultSpellsEntry defSpells = Global.SpellMgr.GetPetDefaultSpellsEntry((int)GetEntry());
            if (defSpells != null)
            {
                for (byte i = 0; i < SharedConst.MaxCreatureSpellDataSlots; ++i)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(defSpells.spellid[i]);
                    if (spellInfo == null)
                        continue;

                    // will called first if level down
                    if (spellInfo.SpellLevel > level)
                        unlearnSpell(spellInfo.Id, true);
                    // will called if level up
                    else
                        learnSpell(spellInfo.Id);
                }
            }
        }

        bool unlearnSpell(uint spell_id, bool learn_prev, bool clear_ab = true)
        {
            if (removeSpell(spell_id, learn_prev, clear_ab))
            {
                if (!m_loading)
                {
                    PetUnlearnedSpells packet = new PetUnlearnedSpells();
                    packet.Spells.Add(spell_id);
                    GetOwner().SendPacket(packet);
                }
                return true;
            }
            return false;
        }

        void unlearnSpells(List<uint> spellIds, bool learn_prev, bool clear_ab)
        {
            PetUnlearnedSpells packet = new PetUnlearnedSpells();

            foreach (uint spell in spellIds)
            {
                if (!removeSpell(spell, learn_prev, clear_ab))
                    continue;

                packet.Spells.Add(spell);
            }

            if (!m_loading)
                GetOwner().SendPacket(packet);
        }

        public bool removeSpell(uint spell_id, bool learn_prev, bool clear_ab = true)
        {
            var petSpell = m_spells.LookupByKey(spell_id);
            if (petSpell == null)
                return false;

            if (petSpell.state == PetSpellState.Removed)
                return false;

            if (petSpell.state == PetSpellState.New)
                m_spells.Remove(spell_id);
            else
                petSpell.state = PetSpellState.Removed;

            RemoveAurasDueToSpell(spell_id);

            if (learn_prev)
            {
                uint prev_id = Global.SpellMgr.GetPrevSpellInChain(spell_id);
                if (prev_id != 0)
                    learnSpell(prev_id);
                else
                    learn_prev = false;
            }

            // if remove last rank or non-ranked then update action bar at server and client if need
            if (GetCharmInfo().RemoveSpellFromActionBar(spell_id) && !learn_prev && clear_ab)
            {
                if (!m_loading)
                {
                    // need update action bar for last removed rank
                    Unit owner = GetOwner();
                    if (owner)
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
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ab.GetAction());
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
            switch (getPetType())
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

        public bool Create(ulong guidlow, Map map, uint Entry)
        {
            Cypher.Assert(map);
            SetMap(map);

            _Create(ObjectGuid.Create(HighGuid.Pet, map.GetId(), Entry, guidlow));

            m_spawnId = guidlow;
            m_originalEntry = Entry;

            if (!InitEntry(Entry))
                return false;

            // Force regen flag for player pets, just like we do for players themselves
            SetFlag(UnitFields.Flags2, UnitFlags2.RegeneratePower);
            SetSheath(SheathState.Melee);

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
                foreach (var petSet in petStore)
                    addSpell(petSet, ActiveStates.Decide, PetSpellState.New, PetSpellType.Family);
            }
        }

        void CastPetAuras(bool current)
        {
            Unit owner = GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            if (!IsPermanentPetFor(owner.ToPlayer()))
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

            if (auraId == 35696)                                      // Demonic Knowledge
            {
                int basePoints = MathFunctions.CalculatePct(aura.GetDamage(), GetStat(Stats.Stamina) + GetStat(Stats.Intellect));
                CastCustomSpell(this, auraId, basePoints, 0, 0, true);
            }
            else
                CastSpell(this, auraId, true);
        }

        bool IsPetAura(Aura aura)
        {
            Unit owner = GetOwner();

            if (!owner || !owner.IsTypeId(TypeId.Player))
                return false;

            // if the owner has that pet aura, return true
            foreach (var petAura in owner.m_petAuras)
            {
                if (petAura.GetAura(GetEntry()) == aura.GetId())
                    return true;
            }
            return false;
        }

        void learnSpellHighRank(uint spellid)
        {
            learnSpell(spellid);
            uint next = Global.SpellMgr.GetNextSpellInChain(spellid);
            if (next != 0)
                learnSpellHighRank(next);
        }

        public void SynchronizeLevelWithOwner()
        {
            Unit owner = GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player))
                return;

            switch (getPetType())
            {
                // always same level
                case PetType.Summon:
                case PetType.Hunter:
                    GivePetLevel((int)owner.getLevel());
                    break;
                default:
                    break;
            }
        }

        public new Player GetOwner()
        {
            return base.GetOwner().ToPlayer();
        }

        public override void SetDisplayId(uint modelId, float displayScale = 1f)
        {
            base.SetDisplayId(modelId, displayScale);

            if (!isControlled())
                return;

            SetGroupUpdateFlag(GroupUpdatePetFlags.ModelId);
        }

        public PetType getPetType() { return m_petType; }
        void setPetType(PetType type) { m_petType = type; }
        public bool isControlled() { return getPetType() == PetType.Summon || getPetType() == PetType.Hunter; }
        public bool isTemporarySummoned() { return m_duration > 0; }

        public override bool IsLoading() { return m_loading; }

        public override byte GetPetAutoSpellSize() { return (byte)m_autospells.Count; }
        public override uint GetPetAutoSpellOnPos(byte pos)
        {
            if (pos >= m_autospells.Count)
                return 0;
            else
                return m_autospells[pos];
        }

        public void SetDuration(uint dur) { m_duration = (int)dur; }
        public int GetDuration() { return m_duration; }

        public ushort GetSpecialization() { return m_petSpecialization; }

        public GroupUpdatePetFlags GetGroupUpdateFlag() { return m_groupUpdateMask; }
        public void SetGroupUpdateFlag(GroupUpdatePetFlags flag)
        {
            if (GetOwner().GetGroup())
            {
                m_groupUpdateMask |= flag;
                GetOwner().SetGroupUpdateFlag(GroupUpdateFlags.Pet);
            }
        }
        public void ResetGroupUpdateFlag()
        {
            m_groupUpdateMask = GroupUpdatePetFlags.None;
            if (GetOwner().GetGroup())
                GetOwner().RemoveGroupUpdateFlag(GroupUpdateFlags.Pet);
        }

        void LearnSpecializationSpells()
        {
            List<uint> learnedSpells = new List<uint>();

            List<SpecializationSpellsRecord> specSpells = Global.DB2Mgr.GetSpecializationSpells(m_petSpecialization);
            if (specSpells != null)
            {
                foreach (var specSpell in specSpells)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID);
                    if (spellInfo == null || spellInfo.SpellLevel > getLevel())
                        continue;

                    learnedSpells.Add(specSpell.SpellID);
                }
            }

            learnSpells(learnedSpells);
        }

        void RemoveSpecializationSpells(bool clearActionBar)
        {
            List<uint> unlearnedSpells = new List<uint>();

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

            unlearnSpells(unlearnedSpells, true, clearActionBar);
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

            SetPetSpecialization setPetSpecialization = new SetPetSpecialization();
            setPetSpecialization.SpecID = m_petSpecialization;
            GetOwner().SendPacket(setPetSpecialization);
        }

        string GenerateActionBarData()
        {
            StringBuilder ss = new StringBuilder();

            for (byte i = SharedConst.ActionBarIndexStart; i < SharedConst.ActionBarIndexEnd; ++i)
            {
                ss.AppendFormat("{0} {1} ", (uint)GetCharmInfo().GetActionBarEntry(i).GetActiveState(), (uint)GetCharmInfo().GetActionBarEntry(i).GetAction());
            }

            return ss.ToString();
        }

        public DeclinedName GetDeclinedNames() { return _declinedname; }

        public new Dictionary<uint, PetSpell> m_spells = new Dictionary<uint, PetSpell>();
        List<uint> m_autospells = new List<uint>();
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

    public enum ActiveStates
    {
        Passive = 0x01,                                    // 0x01 - passive
        Disabled = 0x81,                                    // 0x80 - castable
        Enabled = 0xC1,                                    // 0x40 | 0x80 - auto cast + castable
        Command = 0x07,                                    // 0x01 | 0x02 | 0x04
        Reaction = 0x06,                                    // 0x02 | 0x04
        Decide = 0x00                                     // custom
    }
}
