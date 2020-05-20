/*
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
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class PetitionManager : Singleton<PetitionManager>
    {
        Dictionary<ObjectGuid, Petition> _petitionStorage = new Dictionary<ObjectGuid, Petition>();

        PetitionManager() { }

        public void LoadPetitions()
        {
            uint oldMSTime = Time.GetMSTime();
            _petitionStorage.Clear();

            SQLResult result = DB.Characters.Query("SELECT petitionguid, ownerguid, name FROM petition");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 petitions.");
                return;
            }

            uint count = 0;
            do
            {
                AddPetition(ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0)), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)), result.Read<string>(2), true);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} petitions in: {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public void LoadSignatures()
        {
            uint oldMSTime = Time.GetMSTime();

            SQLResult result = DB.Characters.Query("SELECT petitionguid, player_account, playerguid FROM petition_sign");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Petition signs!");
                return;
            }

            uint count = 0;
            do
            {
                Petition petition = GetPetition(ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0)));
                if (petition == null)
                    continue;

                petition.AddSignature(petition.PetitionGuid, result.Read<uint>(1), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2)), true);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} Petition signs in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }

        public void AddPetition(ObjectGuid petitionGuid, ObjectGuid ownerGuid, string name, bool isLoading)
        {
            Petition p = new Petition();
            p.PetitionGuid = petitionGuid;
            p.ownerGuid = ownerGuid;
            p.petitionName = name;
            p.signatures.Clear();

            _petitionStorage[petitionGuid] = p;

            if (isLoading)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PETITION);
            stmt.AddValue(0, ownerGuid.GetCounter());
            stmt.AddValue(1, petitionGuid.GetCounter());
            stmt.AddValue(2, name);
            DB.Characters.Execute(stmt);
        }

        public void RemovePetition(ObjectGuid petitionGuid)
        {
            _petitionStorage.Remove(petitionGuid);

            // Delete From DB
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_BY_GUID);
            stmt.AddValue(0, petitionGuid.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_GUID);
            stmt.AddValue(0, petitionGuid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public Petition GetPetition(ObjectGuid petitionGuid)
        {
            return _petitionStorage.LookupByKey(petitionGuid);
        }

        public Petition GetPetitionByOwner(ObjectGuid ownerGuid)
        {
            return _petitionStorage.FirstOrDefault(p => p.Value.ownerGuid == ownerGuid).Value;
        }

        public void RemovePetitionsByOwner(ObjectGuid ownerGuid)
        {
            foreach (var key in _petitionStorage.Keys.ToList())
            {
                if (_petitionStorage[key].ownerGuid == ownerGuid)
                {
                    _petitionStorage.Remove(key);
                    break;
                }
            }

            SQLTransaction trans = new SQLTransaction();
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_BY_OWNER);
            stmt.AddValue(0, ownerGuid.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_OWNER);
            stmt.AddValue(0, ownerGuid.GetCounter());
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);
        }

        public void RemoveSignaturesBySigner(ObjectGuid signerGuid)
        {
            foreach (var petitionPair in _petitionStorage)
                petitionPair.Value.RemoveSignatureBySigner(signerGuid);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_PETITION_SIGNATURES);
            stmt.AddValue(0, signerGuid.GetCounter());
            DB.Characters.Execute(stmt);
        }
    }

    public class Petition
    {
        public ObjectGuid PetitionGuid;
        public ObjectGuid ownerGuid;
        public string petitionName;
        public List<(uint AccountId, ObjectGuid PlayerGuid)> signatures = new List<(uint AccountId, ObjectGuid PlayerGuid)>();

        public bool IsPetitionSignedByAccount(uint accountId)
        {
            foreach (var signature in signatures)
                if (signature.AccountId == accountId)
                    return true;

            return false;
        }

        public void AddSignature(ObjectGuid petitionGuid, uint accountId, ObjectGuid playerGuid, bool isLoading)
        {
            signatures.Add((accountId, playerGuid));

            if (isLoading)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PETITION_SIGNATURE);
            stmt.AddValue(0, ownerGuid.GetCounter());
            stmt.AddValue(1, petitionGuid.GetCounter());
            stmt.AddValue(2, playerGuid.GetCounter());
            stmt.AddValue(3, accountId);

            DB.Characters.Execute(stmt);
        }

        public void UpdateName(string newName)
        {
            petitionName = newName;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PETITION_NAME);
            stmt.AddValue(0, newName);
            stmt.AddValue(1, PetitionGuid.GetCounter());
            DB.Characters.Execute(stmt);
        }

        public void RemoveSignatureBySigner(ObjectGuid playerGuid)
        {
            foreach (var itr in signatures)
            {
                if (itr.PlayerGuid == playerGuid)
                {
                    signatures.Remove(itr);

                    // notify owner
                    Player owner = Global.ObjAccessor.FindConnectedPlayer(ownerGuid);
                    if (owner != null)
                        owner.GetSession().SendPetitionQuery(PetitionGuid);

                    break;
                }
            }
        }
    }
}
