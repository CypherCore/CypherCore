// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Accounts;

namespace Game.Chat.Commands
{
    [CommandGroup("rbac")]
    internal class RbacComands
    {
        [Command("list", RBACPermissions.CommandRbacList, true)]
        private static bool HandleRBACListPermissionsCommand(CommandHandler handler, uint? permId)
        {
            if (!permId.HasValue)
            {
                var permissions = Global.AccountMgr.GetRBACPermissionList();
                handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);

                foreach (var (_, permission) in permissions)
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
            }
            else
            {
                RBACPermission permission = Global.AccountMgr.GetRBACPermission(permId.Value);

                if (permission == null)
                {
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId.Value);

                    return false;
                }

                handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);
                handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                handler.SendSysMessage(CypherStrings.RbacListPermsLinkedHeader);

                foreach (var linkedPerm in permission.GetLinkedPermissions())
                {
                    RBACPermission rbacPermission = Global.AccountMgr.GetRBACPermission(linkedPerm);

                    if (rbacPermission != null)
                        handler.SendSysMessage(CypherStrings.RbacListElement, rbacPermission.GetId(), rbacPermission.GetName());
                }
            }

            return true;
        }

        private static RBACCommandData GetRBACData(AccountIdentifier account)
        {
            if (account.IsConnected())
                return new RBACCommandData()
                {
                    rbac = account.GetConnectedSession().GetRBACData(),
                    needDelete = false
                };

            RBACData rbac = new(account.GetID(), account.GetName(), (int)Global.WorldMgr.GetRealmId().Index, (byte)Global.AccountMgr.GetSecurity(account.GetID(), (int)Global.WorldMgr.GetRealmId().Index));
            rbac.LoadFromDB();

            return new RBACCommandData()
            {
                rbac = rbac,
                needDelete = true
            };
        }

        [CommandGroup("account")]
        private class RbacAccountCommands
        {
            [Command("deny", RBACPermissions.CommandRbacAccPermDeny, true)]
            private static bool HandleRBACPermDenyCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
            {
                if (account == null)
                    account = AccountIdentifier.FromTarget(handler);

                if (account == null)
                    return false;

                if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                    return false;

                if (!realmId.HasValue)
                    realmId = -1;

                RBACCommandData data = GetRBACData(account);

                RBACCommandResult result = data.rbac.DenyPermission(permId, realmId.Value);
                RBACPermission permission = Global.AccountMgr.GetRBACPermission(permId);

                switch (result)
                {
                    case RBACCommandResult.CantAddAlreadyAdded:
                        handler.SendSysMessage(CypherStrings.RbacPermDeniedInList,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.InGrantedList:
                        handler.SendSysMessage(CypherStrings.RbacPermDeniedInGrantedList,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermDenied,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                        break;
                    default:
                        break;
                }

                return true;
            }

            [Command("grant", RBACPermissions.CommandRbacAccPermGrant, true)]
            private static bool HandleRBACPermGrantCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
            {
                if (account == null)
                    account = AccountIdentifier.FromTarget(handler);

                if (account == null)
                    return false;

                if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                    return false;

                if (!realmId.HasValue)
                    realmId = -1;

                RBACCommandData data = GetRBACData(account);

                RBACCommandResult result = data.rbac.GrantPermission(permId, realmId.Value);
                RBACPermission permission = Global.AccountMgr.GetRBACPermission(permId);

                switch (result)
                {
                    case RBACCommandResult.CantAddAlreadyAdded:
                        handler.SendSysMessage(CypherStrings.RbacPermGrantedInList,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.InDeniedList:
                        handler.SendSysMessage(CypherStrings.RbacPermGrantedInDeniedList,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermGranted,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                        break;
                    default:
                        break;
                }

                return true;
            }

            [Command("list", RBACPermissions.CommandRbacAccPermList, true)]
            private static bool HandleRBACPermListCommand(CommandHandler handler, AccountIdentifier account)
            {
                if (account == null)
                    account = AccountIdentifier.FromTarget(handler);

                if (account == null)
                    return false;

                RBACCommandData data = GetRBACData(account);

                handler.SendSysMessage(CypherStrings.RbacListHeaderGranted, data.rbac.GetId(), data.rbac.GetName());
                var granted = data.rbac.GetGrantedPermissions();

                if (granted.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                    foreach (var id in granted)
                    {
                        RBACPermission permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }

                handler.SendSysMessage(CypherStrings.RbacListHeaderDenied, data.rbac.GetId(), data.rbac.GetName());
                var denied = data.rbac.GetDeniedPermissions();

                if (denied.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                    foreach (var id in denied)
                    {
                        RBACPermission permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }

                handler.SendSysMessage(CypherStrings.RbacListHeaderBySecLevel, data.rbac.GetId(), data.rbac.GetName(), data.rbac.GetSecurityLevel());
                var defaultPermissions = Global.AccountMgr.GetRBACDefaultPermissions(data.rbac.GetSecurityLevel());

                if (defaultPermissions.Empty())
                    handler.SendSysMessage(CypherStrings.RbacListEmpty);
                else
                    foreach (var id in defaultPermissions)
                    {
                        RBACPermission permission = Global.AccountMgr.GetRBACPermission(id);
                        handler.SendSysMessage(CypherStrings.RbacListElement, permission.GetId(), permission.GetName());
                    }

                return true;
            }

            [Command("revoke", RBACPermissions.CommandRbacAccPermRevoke, true)]
            private static bool HandleRBACPermRevokeCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
            {
                if (account == null)
                    account = AccountIdentifier.FromTarget(handler);

                if (account == null)
                    return false;

                if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                    return false;

                if (!realmId.HasValue)
                    realmId = -1;

                RBACCommandData data = GetRBACData(account);

                RBACCommandResult result = data.rbac.RevokePermission(permId, realmId.Value);
                RBACPermission permission = Global.AccountMgr.GetRBACPermission(permId);

                switch (result)
                {
                    case RBACCommandResult.CantRevokeNotInList:
                        handler.SendSysMessage(CypherStrings.RbacPermRevokedNotInList,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.OK:
                        handler.SendSysMessage(CypherStrings.RbacPermRevoked,
                                               permId,
                                               permission.GetName(),
                                               realmId.Value,
                                               data.rbac.GetId(),
                                               data.rbac.GetName());

                        break;
                    case RBACCommandResult.IdDoesNotExists:
                        handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                        break;
                    default:
                        break;
                }

                return true;
            }
        }

        private class RBACCommandData
        {
            public bool needDelete;
            public RBACData rbac;
        }
    }
}