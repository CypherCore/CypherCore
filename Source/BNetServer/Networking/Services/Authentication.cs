// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.Authentication.V1;
using Bgs.Protocol.Challenge.V1;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Google.Protobuf;
using System;

namespace BNetServer.Networking
{
    public partial class Session
    {
        [Service(OriginalHash.AuthenticationService, 1)]
        BattlenetRpcErrorCode HandleLogon(LogonRequest logonRequest, NoData response)
        {
            if (logonRequest.Program != "WoW")
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in with game other than WoW (using {logonRequest.Program})!");
                return BattlenetRpcErrorCode.BadProgram;
            }

            if (logonRequest.Platform != "Win" && logonRequest.Platform != "Wn64" && logonRequest.Platform != "Mc64")
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in from an unsupported platform (using {logonRequest.Platform})!");
                return BattlenetRpcErrorCode.BadPlatform;
            }

            if (!SharedConst.IsValidLocale(logonRequest.Locale.ToEnum<Locale>()))
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in with unsupported locale (using {logonRequest.Locale})!");
                return BattlenetRpcErrorCode.BadLocale;
            }

            locale = logonRequest.Locale;
            os = logonRequest.Platform;
            build = (uint)logonRequest.ApplicationVersion;

            var endpoint = Global.LoginServiceMgr.GetAddressForClient(GetRemoteIpEndPoint().Address);

            ChallengeExternalRequest externalChallenge = new();
            externalChallenge.PayloadType = "web_auth_url";
            externalChallenge.Payload = ByteString.CopyFromUtf8($"https://{endpoint.Address}:{endpoint.Port}/bnetserver/login/");

            SendRequest((uint)OriginalHash.ChallengeListener, 3, externalChallenge);
            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AuthenticationService, 7)]
        BattlenetRpcErrorCode HandleVerifyWebCredentials(VerifyWebCredentialsRequest verifyWebCredentialsRequest)
        {
            if (verifyWebCredentialsRequest.WebCredentials.IsEmpty)
                return BattlenetRpcErrorCode.Denied;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SelBnetAccountInfo);
            stmt.AddValue(0, verifyWebCredentialsRequest.WebCredentials.ToStringUtf8());

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
                return BattlenetRpcErrorCode.Denied;

            accountInfo = new AccountInfo(result);

            if (accountInfo.LoginTicketExpiry < Time.UnixTime)
                return BattlenetRpcErrorCode.TimedOut;

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID);
            stmt.AddValue(0, accountInfo.Id);

            SQLResult characterCountsResult = DB.Login.Query(stmt);
            if (!characterCountsResult.IsEmpty())
            {
                do
                {
                    var realmId = new RealmId(characterCountsResult.Read<byte>(3), characterCountsResult.Read<byte>(4), characterCountsResult.Read<uint>(2));
                    accountInfo.GameAccounts[characterCountsResult.Read<uint>(0)].CharacterCounts[realmId.GetAddress()] = characterCountsResult.Read<byte>(1);

                } while (characterCountsResult.NextRow());
            }

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SelBnetLastPlayerCharacters);
            stmt.AddValue(0, accountInfo.Id);

            SQLResult lastPlayerCharactersResult = DB.Login.Query(stmt);
            if (!lastPlayerCharactersResult.IsEmpty())
            {
                do
                {
                    var realmId = new RealmId(lastPlayerCharactersResult.Read<byte>(1), lastPlayerCharactersResult.Read<byte>(2), lastPlayerCharactersResult.Read<uint>(3));

                    LastPlayedCharacterInfo lastPlayedCharacter = new();
                    lastPlayedCharacter.RealmId = realmId;
                    lastPlayedCharacter.CharacterName = lastPlayerCharactersResult.Read<string>(4);
                    lastPlayedCharacter.CharacterGUID = lastPlayerCharactersResult.Read<ulong>(5);
                    lastPlayedCharacter.LastPlayedTime = lastPlayerCharactersResult.Read<uint>(6);

                    accountInfo.GameAccounts[lastPlayerCharactersResult.Read<uint>(0)].LastPlayedCharacters[realmId.GetSubRegionAddress()] = lastPlayedCharacter;

                } while (lastPlayerCharactersResult.NextRow());
            }

            string ip_address = GetRemoteIpEndPoint().ToString();

            // If the IP is 'locked', check that the player comes indeed from the correct IP address
            if (accountInfo.IsLockedToIP)
            {
                Log.outDebug(LogFilter.Session, $"Session.HandleVerifyWebCredentials: Account: {accountInfo.Login} is locked to IP: {accountInfo.LastIP} is logging in from IP: {ip_address}");

                if (accountInfo.LastIP != ip_address)
                    return BattlenetRpcErrorCode.RiskAccountLocked;
            }
            else
            {
                Log.outDebug(LogFilter.Session, $"Session.HandleVerifyWebCredentials: Account: {accountInfo.Login} is not locked to ip");
                if (accountInfo.LockCountry.IsEmpty() || accountInfo.LockCountry == "00")
                    Log.outDebug(LogFilter.Session, $"Session.HandleVerifyWebCredentials: Account: {accountInfo.Login} is not locked to country");
                else if (!accountInfo.LockCountry.IsEmpty() && !ipCountry.IsEmpty())
                {
                    Log.outDebug(LogFilter.Session, $"Session.HandleVerifyWebCredentials: Account: {accountInfo.Login} is locked to Country: {accountInfo.LockCountry} player Country: {ipCountry}");

                    if (ipCountry != accountInfo.LockCountry)
                        return BattlenetRpcErrorCode.RiskAccountLocked;
                }
            }

            // If the account is banned, reject the logon attempt
            if (accountInfo.IsBanned)
            {
                if (accountInfo.IsPermanenetlyBanned)
                {
                    Log.outDebug(LogFilter.Session, $"{GetClientInfo()} Session.HandleVerifyWebCredentials: Banned account {accountInfo.Login} tried to login!");
                    return BattlenetRpcErrorCode.GameAccountBanned;
                }
                else
                {
                    Log.outDebug(LogFilter.Session, $"{GetClientInfo()} Session.HandleVerifyWebCredentials: Temporarily banned account {accountInfo.Login} tried to login!");
                    return BattlenetRpcErrorCode.GameAccountSuspended;
                }
            }

            LogonResult logonResult = new();
            logonResult.ErrorCode = 0;
            logonResult.AccountId = new EntityId();
            logonResult.AccountId.Low = accountInfo.Id;
            logonResult.AccountId.High = 0x100000000000000;
            foreach (var pair in accountInfo.GameAccounts)
            {
                EntityId gameAccountId = new();
                gameAccountId.Low = pair.Value.Id;
                gameAccountId.High = 0x200000200576F57;
                logonResult.GameAccountId.Add(gameAccountId);
            }

            if (!ipCountry.IsEmpty())
                logonResult.GeoipCountry = ipCountry;

            logonResult.SessionKey = ByteString.CopyFrom(new byte[64].GenerateRandomKey(64));

            authed = true;

            SendRequest((uint)OriginalHash.AuthenticationListener, 5, logonResult);
            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AuthenticationService, 8)]
        BattlenetRpcErrorCode HandleGenerateWebCredentials(GenerateWebCredentialsRequest request, GenerateWebCredentialsResponse response)
        {
            if (!authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.Program != 0x576F57)
            {
                Log.outDebug(LogFilter.Session, $"[Battlenet::HandleGenerateWebCredentials] {GetClientInfo()} attempted to generate web cretentials with game other than WoW (using {(request.Program >> 24) & 0xFF}{(request.Program >> 16) & 0xFF}{(request.Program >> 8) & 0xFF}{request.Program & 0xFF})!");
                return BattlenetRpcErrorCode.BadProgram;
            }

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_EXISTING_AUTHENTICATION_BY_ID);
            stmt.AddValue(0, accountInfo.Id);

            queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                // just send existing credentials back (not the best but it works for now with them being stored in db)
                response.WebCredentials = ByteString.CopyFromUtf8(result.Read<string>(0));
            }));

            return BattlenetRpcErrorCode.Ok;
        }
    }
}