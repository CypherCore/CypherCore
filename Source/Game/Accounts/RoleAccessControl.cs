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
using System.Collections.Generic;
using System.Linq;

namespace Game.Accounts
{
    public class RBACData
    {
        public RBACData(uint id, string name, int realmId, byte secLevel = 255)
        {
            _id = id;
            _name = name;
            _realmId = realmId;
            _secLevel = secLevel;
        }

        public RBACCommandResult GrantPermission(uint permissionId, int realmId = 0)
        {
            // Check if permission Id exists
            RBACPermission perm = Global.AccountMgr.GetRBACPermission(permissionId);
            if (perm == null)
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.IdDoesNotExists;
            }

            // Check if already added in denied list
            if (HasDeniedPermission(permissionId))
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in deny list",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.InDeniedList;
            }

            // Already added?
            if (HasGrantedPermission(permissionId))
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already granted",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.CantAddAlreadyAdded;
            }

            AddGrantedPermission(permissionId);

            // Do not save to db when loading data from DB (realmId = 0)
            if (realmId != 0)
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                               GetId(), GetName(), permissionId, realmId);
                SavePermission(permissionId, true, realmId);
                CalculateNewPermissions();
            }
            else
                Log.outDebug(LogFilter.Rbac, "RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                               GetId(), GetName(), permissionId, realmId);

            return RBACCommandResult.OK;
        }

        public RBACCommandResult DenyPermission(uint permissionId, int realmId = 0)
        {
            // Check if permission Id exists
            RBACPermission perm = Global.AccountMgr.GetRBACPermission(permissionId);
            if (perm == null)
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.IdDoesNotExists;
            }

            // Check if already added in granted list
            if (HasGrantedPermission(permissionId))
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in grant list",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.InGrantedList;
            }

            // Already added?
            if (HasDeniedPermission(permissionId))
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already denied",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.CantAddAlreadyAdded;
            }

            AddDeniedPermission(permissionId);

            // Do not save to db when loading data from DB (realmId = 0)
            if (realmId != 0)
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                               GetId(), GetName(), permissionId, realmId);
                SavePermission(permissionId, false, realmId);
                CalculateNewPermissions();
            }
            else
                Log.outDebug(LogFilter.Rbac, "RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                               GetId(), GetName(), permissionId, realmId);

            return RBACCommandResult.OK;
        }

        void SavePermission(uint permission, bool granted, int realmId)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_RBAC_ACCOUNT_PERMISSION);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, permission);
            stmt.AddValue(2, granted);
            stmt.AddValue(3, realmId);
            DB.Login.Execute(stmt);
        }

        public RBACCommandResult RevokePermission(uint permissionId, int realmId = 0)
        {
            // Check if it's present in any list
            if (!HasGrantedPermission(permissionId) && !HasDeniedPermission(permissionId))
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Not granted or revoked",
                               GetId(), GetName(), permissionId, realmId);
                return RBACCommandResult.CantRevokeNotInList;
            }

            RemoveGrantedPermission(permissionId);
            RemoveDeniedPermission(permissionId);

            // Do not save to db when loading data from DB (realmId = 0)
            if (realmId != 0)
            {
                Log.outDebug(LogFilter.Rbac, "RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                               GetId(), GetName(), permissionId, realmId);
                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_RBAC_ACCOUNT_PERMISSION);
                stmt.AddValue(0, GetId());
                stmt.AddValue(1, permissionId);
                stmt.AddValue(2, realmId);
                DB.Login.Execute(stmt);

                CalculateNewPermissions();
            }
            else
                Log.outDebug(LogFilter.Rbac, "RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                               GetId(), GetName(), permissionId, realmId);

            return RBACCommandResult.OK;
        }

        public void LoadFromDB()
        {
            ClearData();

            Log.outDebug(LogFilter.Rbac, "RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", GetId(), GetName());
            // Load account permissions (granted and denied) that affect current realm
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetRealmId());

            LoadFromDBCallback(DB.Login.Query(stmt));
        }

        public QueryCallback LoadFromDBAsync()
        {
            ClearData();

            Log.outDebug(LogFilter.Rbac, "RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", GetId(), GetName());
            // Load account permissions (granted and denied) that affect current realm
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
            stmt.AddValue(0, GetId());
            stmt.AddValue(1, GetRealmId());

            return DB.Login.AsyncQuery(stmt);
        }

        public void LoadFromDBCallback(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                do
                {
                    if (result.Read<bool>(1))
                        GrantPermission(result.Read<uint>(0));
                    else
                        DenyPermission(result.Read<uint>(0));

                } while (result.NextRow());
            }

            // Add default permissions
            List<uint> permissions = Global.AccountMgr.GetRBACDefaultPermissions(_secLevel);
            foreach (var id in permissions)
                GrantPermission(id);

            // Force calculation of permissions
            CalculateNewPermissions();
        }

        void CalculateNewPermissions()
        {
            Log.outDebug(LogFilter.Rbac, "RBACData.CalculateNewPermissions [Id: {0} Name: {1}]", GetId(), GetName());

            // Get the list of granted permissions
            _globalPerms = GetGrantedPermissions();
            ExpandPermissions(_globalPerms);
            List<uint> revoked = GetDeniedPermissions();
            ExpandPermissions(revoked);
            RemovePermissions(_globalPerms, revoked);
        }

        void AddPermissions(List<uint> permsFrom, List<uint> permsTo)
        {
            foreach (var id in permsFrom)
                permsTo.Add(id);
        }

        /// <summary>
        /// Removes a list of permissions from another list
        /// </summary>
        /// <param name="permsFrom"></param>
        /// <param name="permsToRemove"></param>
        void RemovePermissions(List<uint> permsFrom, List<uint> permsToRemove)
        {
            foreach (var id in permsToRemove)
                permsFrom.Remove(id);
        }

        void ExpandPermissions(List<uint> permissions)
        {
            List<uint> toCheck = new List<uint>(permissions);
            permissions.Clear();

            while (!toCheck.Empty())
            {
                // remove the permission from original list
                uint permissionId = toCheck.FirstOrDefault();
                toCheck.RemoveAt(0);

                RBACPermission permission = Global.AccountMgr.GetRBACPermission(permissionId);
                if (permission == null)
                    continue;

                // insert into the final list (expanded list)
                permissions.Add(permissionId);

                // add all linked permissions (that are not already expanded) to the list of permissions to be checked
                List<uint> linkedPerms = permission.GetLinkedPermissions();
                foreach (var id in linkedPerms)
                    if (!permissions.Contains(id))
                        toCheck.Add(id);
            }

            //Log.outDebug(LogFilter.General, "RBACData:ExpandPermissions: Expanded: {0}", GetDebugPermissionString(permissions));
        }

        void ClearData()
        {
            _grantedPerms.Clear();
            _deniedPerms.Clear();
            _globalPerms.Clear();
        }

        // Gets the Name of the Object
        public string GetName() { return _name; }
        // Gets the Id of the Object
        public uint GetId() { return _id; }

        public bool HasPermission(RBACPermissions permission)
        {
            return _globalPerms.Contains((uint)permission);
        }

        // Returns all the granted permissions (after computation)
        public List<uint> GetPermissions() { return _globalPerms; }
        // Returns all the granted permissions
        public List<uint> GetGrantedPermissions() { return _grantedPerms; }
        // Returns all the denied permissions
        public List<uint> GetDeniedPermissions() { return _deniedPerms; }

        public void SetSecurityLevel(byte id)
        {
            _secLevel = id;
            LoadFromDB();
        }

        public byte GetSecurityLevel() { return _secLevel; }
        int GetRealmId() { return _realmId; }

        // Checks if a permission is granted
        bool HasGrantedPermission(uint permissionId)
        {
            return _grantedPerms.Contains(permissionId);
        }

        // Checks if a permission is denied
        bool HasDeniedPermission(uint permissionId)
        {
            return _deniedPerms.Contains(permissionId);
        }

        // Adds a new granted permission
        void AddGrantedPermission(uint permissionId)
        {
            _grantedPerms.Add(permissionId);
        }

        // Removes a granted permission
        void RemoveGrantedPermission(uint permissionId)
        {
            _grantedPerms.Remove(permissionId);
        }

        // Adds a new denied permission
        void AddDeniedPermission(uint permissionId)
        {
            _deniedPerms.Add(permissionId);
        }

        // Removes a denied permission
        void RemoveDeniedPermission(uint permissionId)
        {
            _deniedPerms.Remove(permissionId);
        }

        uint _id;                                        // Account id
        string _name;                                 // Account name
        int _realmId;                                    // RealmId Affected
        byte _secLevel;                                   // Account SecurityLevel
        List<uint> _grantedPerms = new List<uint>();             // Granted permissions
        List<uint> _deniedPerms = new List<uint>();              // Denied permissions
        List<uint> _globalPerms = new List<uint>();              // Calculated permissions
    }

    public class RBACPermission
    {
        public RBACPermission(uint id = 0, string name = "")
        {
            _id = id;
            _name = name;
        }

        // Gets the Name of the Object
        public string GetName() { return _name; }
        // Gets the Id of the Object
        public uint GetId() { return _id; }

        // Gets the Permissions linked to this permission
        public List<uint> GetLinkedPermissions() { return _perms; }
        // Adds a new linked Permission
        public void AddLinkedPermission(uint id) { _perms.Add(id); }
        // Removes a linked Permission
        void RemoveLinkedPermission(uint id) { _perms.Remove(id); }


        uint _id;                                 // id of the object
        string _name;                             // name of the object
        List<uint> _perms = new List<uint>();     // Set of permissions
    }

    public enum RBACCommandResult
    {
        OK,
        CantAddAlreadyAdded,
        CantRevokeNotInList,
        InGrantedList,
        InDeniedList,
        IdDoesNotExists
    }
}
