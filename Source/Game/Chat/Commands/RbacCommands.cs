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
using Framework.IO;
using Game.Accounts;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("rbac", RBACPermissions.CommandRbac, true)]
    class RbacComands
    {
        [Command("list", RBACPermissions.CommandRbacList, true)]
        static bool HandleRBACListPermissionsCommand(StringArguments args, CommandHandler handler)
        {
            var id = args.NextUInt32();

            if (id == 0)
            {
                var permissions = Global.AccountMgr.GetRBACPermissionList();
                handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);
                foreach (var permission in permissions.Values)
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
            }
            else
            {
                var permission = Global.AccountMgr.GetRBACPermission(id);
                if (permission == null)
                {
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, id);
                    return false;
                }

                handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);
                handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                handler.SendSysMessage(CypherStrings.RbacListPermsLinkedHeader);
                var permissions = permission.GetLinkedPermissions();
                foreach (var permissionId in permissions)
                {
                    var rbacPermission = Global.AccountMgr.GetRBACPermission(permissionId);
                    if (rbacPermission != null)
                        handler.SendSysMessage(CypherStrings.RbacListElement, rbacPermission.GetId(), rbacPermission.GetName());
                }
            }

            return true;
        }

        [CommandGroup("account", RBACPermissions.CommandRbacAcc, true)]
        class RbacAccountCommands
        {
            [Command("grant", RBACPermissions.CommandRbacAccPermGrant, true)]
            static bool HandleRBACPermGrantCommand(StringArguments args, CommandHandler handler)
            {
                var command = ReadParams(args, handler);
                if (command == null)
                    return false;

                var result = command.rbac.GrantPermission(command.id, command.realmId);
                var permission = Global.AccountMgr.GetRBACPermission(command.id);

                switch (result)
                {
                    case RBACCommandResult.CantAddAlreadyAdded:
                        handler.SendSysMessage(CypherStrings.RbacPermGrantedInList, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.InDeniedList:
                        handler.SendSysMessage(CypherStrings.RbacPermGrantedInDeniedList, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermGranted, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, command.id);
                        break;
                    default:
                        break;
                }

                return true;
            }

            [Command("deny", RBACPermissions.CommandRbacAccPermDeny, true)]
            static bool HandleRBACPermDenyCommand(StringArguments args, CommandHandler handler)
            {
                var command = ReadParams(args, handler);

                if (command == null)
                    return false;

                var result = command.rbac.DenyPermission(command.id, command.realmId);
                var  permission = Global.AccountMgr.GetRBACPermission(command.id);

                switch (result)
                {
                    case RBACCommandResult.CantAddAlreadyAdded:
                        handler.SendSysMessage(CypherStrings.RbacPermDeniedInList, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.InGrantedList:
                        handler.SendSysMessage(CypherStrings.RbacPermDeniedInGrantedList, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermDenied, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, command.id);
                        break;
                    default:
                        break;
                }

                return true;
            }

            [Command("revoke", RBACPermissions.CommandRbacAccPermRevoke, true)]
            static bool HandleRBACPermRevokeCommand(StringArguments args, CommandHandler handler)
            {
                var command = ReadParams(args, handler);

                if (command == null)
                    return false;

                var result = command.rbac.RevokePermission(command.id, command.realmId);
                var  permission = Global.AccountMgr.GetRBACPermission(command.id);

                switch (result)
                {
                    case RBACCommandResult.CantRevokeNotInList:
                        handler.SendSysMessage(CypherStrings.RbacPermRevokedNotInList, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermRevoked, command.id, permission.GetName(),
                            command.realmId, command.rbac.GetId(), command.rbac.GetName());
                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, command.id);
                        break;
                    default:
                        break;
                }

                return true;
            }

            [Command("list", RBACPermissions.CommandRbacAccPermList, true)]
            static bool HandleRBACPermListCommand(StringArguments args, CommandHandler handler)
            {
                var command = ReadParams(args, handler, false);

                if (command == null)
                    return false;

                handler.SendSysMessage(CypherStrings.RbacListHeaderGranted, command.rbac.GetId(), command.rbac.GetName());
                var granted = command.rbac.GetGrantedPermissions();
                if (granted.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                {
                    foreach (var id in granted)
                    {
                        var  permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }
                }

                handler.SendSysMessage(CypherStrings.RbacListHeaderDenied, command.rbac.GetId(), command.rbac.GetName());
                var denied = command.rbac.GetDeniedPermissions();
                if (denied.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                {
                    foreach (var id in denied)
                    {
                        var  permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }
                }
                handler.SendSysMessage(CypherStrings.RbacListHeaderBySecLevel, command.rbac.GetId(), command.rbac.GetName(), command.rbac.GetSecurityLevel());
                var defaultPermissions = Global.AccountMgr.GetRBACDefaultPermissions(command.rbac.GetSecurityLevel());
                if (defaultPermissions.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                {
                    foreach (var id in defaultPermissions)
                    {
                        var  permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }
                }

                return true;
            }
        }

        static RBACCommandData ReadParams(StringArguments args, CommandHandler handler, bool checkParams = true)
        {
            if (args.Empty())
                return null;

            var param1 = args.NextString();
            var param2 = args.NextString();
            var param3 = args.NextString();

            var realmId = -1;
            uint accountId;
            string accountName;
            uint id = 0;
            RBACData rdata = null;
            var useSelectedPlayer = false;

            if (checkParams)
            {
                if (string.IsNullOrEmpty(param3))
                {
                    if (!int.TryParse(param2, out realmId))
                        return null;

                    if (!uint.TryParse(param1, out id))
                        return null;

                    useSelectedPlayer = true;
                }
                else
                {
                    if (!uint.TryParse(param2, out id))
                        return null;

                    if (!int.TryParse(param3, out realmId))
                        return null;
                }

                if (id == 0)
                {
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, id);
                    return null;
                }

                if (realmId < -1 || realmId == 0)
                {
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterRealm, realmId);
                    return null;
                }
            }
            else if (string.IsNullOrEmpty(param1))
                useSelectedPlayer = true;

            if (useSelectedPlayer)
            {
                var player = handler.GetSelectedPlayer();
                if (!player)
                    return null;

                rdata = player.GetSession().GetRBACData();
                accountId = rdata.GetId();
                Global.AccountMgr.GetName(accountId, out accountName);
            }
            else
            {
                accountName = param1;
                accountId = Global.AccountMgr.GetId(accountName);

                if (accountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                    return null;
                }
            }

            if (checkParams && handler.HasLowerSecurityAccount(null, accountId, true))
                return null;

            var data = new RBACCommandData();

            if (rdata == null)
            {
                data.rbac = new RBACData(accountId, accountName, (int)Global.WorldMgr.GetRealm().Id.Index, (byte)Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.GetRealm().Id.Index));
                data.rbac.LoadFromDB();
                data.needDelete = true;
            }
            else
                data.rbac = rdata;

            data.id = id;
            data.realmId = realmId;
            return data;
        }

        class RBACCommandData
        {
            public uint id;
            public int realmId;
            public RBACData rbac;
            public bool needDelete;
        }
    }
}
