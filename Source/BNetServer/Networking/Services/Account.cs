// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.Account.V2;
using Bgs.Protocol.Account.V2.Client;
using Framework.Constants;
using Google.Protobuf;
using System;

namespace BNetServer.Networking
{
    public partial class Session
    {
        [Service(OriginalHash.AccountServiceV2, 101)]
        BattlenetRpcErrorCode HandleGetAccountInfo(GetAccountInfoRequest request, GetAccountInfoResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            Bgs.Protocol.Account.V2.AccountInfo info = new()
            {
                AccountId = GetAccountId()
            };
            info.Flags.Add((uint)Bgs.Protocol.Account.V2.AccountInfo.Types.Flag.IsHiddenFromFriendFinder);

            response.Info = info;

            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AccountServiceV2, 104)]
        BattlenetRpcErrorCode HandleGetRestriction(GetRestrictionRequest request, GetRestrictionResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AccountServiceV2, 201)]
        BattlenetRpcErrorCode HandleGetGameAccountInfo(GetGameAccountInfoRequest request, GetGameAccountInfoResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (request.GameAccount != null)
            {
                GameAccountInfo gameAccountInfo = GetGameAccountInfo((uint)request.GameAccount.Id);
                if (gameAccountInfo != null)
                {
                    Bgs.Protocol.Account.V2.GameAccountInfo info = new()
                    {
                        AccountId = request.GameAccount.Id,
                        Name = gameAccountInfo.DisplayName
                    };
                    response.Info = info;
                }
            }

            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AccountServiceV2, 203)]
        BattlenetRpcErrorCode HandleGetGameAccountRestriction(GetGameAccountRestrictionRequest request, GetGameAccountRestrictionResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (request.GameAccount != null)
            {
                GameAccountInfo gameAccountInfo = GetGameAccountInfo((uint)request.GameAccount.Id);
                if (gameAccountInfo != null)
                {
                    if (gameAccountInfo.IsPermanenetlyBanned)
                    {
                        Restriction restriction = new()
                        {
                            TitleId = "WoW".ToFourCC(),
                            Type = (uint)RestrictionType.LoginBanned,
                            CreatedTimeMs = (ulong)(gameAccountInfo.BanDate * Time.InMilliseconds)
                        };
                        response.Restrictions.Add(restriction);
                    }

                    if (gameAccountInfo.IsBanned)
                    {
                        Restriction restriction = new()
                        {
                            TitleId = "WoW".ToFourCC(),
                            Type = (uint)RestrictionType.LoginSuspended,
                            CreatedTimeMs = (ulong)(gameAccountInfo.BanDate * Time.InMilliseconds),
                            ExpireTimeMs = (ulong)(gameAccountInfo.UnbanDate * Time.InMilliseconds)
                        };
                        response.Restrictions.Add(restriction);
                    }
                }
            }

            return BattlenetRpcErrorCode.Ok;
        }
    }
}
