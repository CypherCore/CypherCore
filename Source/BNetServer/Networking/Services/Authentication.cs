// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Bgs.Protocol.Account.V2;
using Bgs.Protocol.Authentication.V2.Client;
using Framework;
using Framework.ClientBuild;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Google.Protobuf;
using System;
using System.Text.Json;

namespace BNetServer.Networking
{
    public partial class Session
    {
        BattlenetRpcErrorCode HandleLogon(uint program, string platform, string locale, uint applicationVersion, string deviceId)
        {
            if (program != 0x576f57) //WoW
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in with game other than WoW (using {program})!");
                return BattlenetRpcErrorCode.BadProgram;
            }

            if (!ClientBuildHelper.IsValid(platform))
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in from an unsupported platform (using {platform})!");
                return BattlenetRpcErrorCode.BadPlatform;
            }

            if (!SharedConst.IsValidLocale(locale.ToEnum<Locale>()))
            {
                Log.outDebug(LogFilter.Session, $"Battlenet.LogonRequest: {GetClientInfo()} attempted to log in with unsupported locale (using {locale})!");
                return BattlenetRpcErrorCode.BadLocale;
            }

            TimeSpan timezoneOffset = TimeSpan.Zero;
            if (!deviceId.IsEmpty())
            {
                var doc = JsonSerializer.Deserialize<JsonDocument>(deviceId);
                if (doc != null)
                {
                    var itr = doc.RootElement.GetProperty("UTCO");
                    {
                        if (itr.TryGetUInt32(out uint value))
                            timezoneOffset = Timezone.GetOffsetByHash(value);
                    }
                }
            }

            _locale = locale;
            _os = platform;
            _build = applicationVersion;
            _timezoneOffset = timezoneOffset;
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode HandleVerifyAuthToken(string authToken, Action<BattlenetRpcErrorCode> sendResponse, Action<AccountInfo, string> sendLogonComplete)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_INFO);
            stmt.AddValue(0, authToken);

            AccountInfo accountInfo = null;
            queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithChainingCallback((callback, result) =>
            {
                if (result.IsEmpty())
                {
                    sendResponse(BattlenetRpcErrorCode.Denied);
                    return;
                }

                accountInfo = new AccountInfo(result);

                if (accountInfo.LoginTicketExpiry < Time.UnixTime)
                {
                    sendResponse(BattlenetRpcErrorCode.TimedOut);
                    return;
                }

                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID);
                stmt.AddValue(0, accountInfo.Id);
                callback.SetNextQuery(DB.Login.AsyncQuery(stmt));
            }).WithChainingCallback((callback, characterCountsResult) =>
            {
                if (!characterCountsResult.IsEmpty())
                {
                    do
                    {
                        accountInfo?.GameAccounts[characterCountsResult.Read<uint>(0)]
                        .CharacterCounts[new RealmId(characterCountsResult.Read<byte>(3), characterCountsResult.Read<byte>(4), characterCountsResult.Read<uint>(2)).GetAddress()] = characterCountsResult.Read<byte>(1);

                    } while (characterCountsResult.NextRow());
                }

                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_LAST_PLAYER_CHARACTERS);
                stmt.AddValue(0, accountInfo.Id);
                callback.SetNextQuery(DB.Login.AsyncQuery(stmt));
            }).WithCallback(lastPlayerCharactersResult =>
            {
                if (!lastPlayerCharactersResult.IsEmpty())
                {
                    do
                    {
                        RealmId realmId = new(lastPlayerCharactersResult.Read<byte>(3), lastPlayerCharactersResult.Read<byte>(4), lastPlayerCharactersResult.Read<uint>(2));
                        LastPlayedCharacterInfo lastPlayedCharacter = accountInfo.GameAccounts[lastPlayerCharactersResult.Read<uint>(0)].LastPlayedCharacters[realmId.GetSubRegionAddress()];

                        lastPlayedCharacter.RealmId = realmId;
                        lastPlayedCharacter.CharacterName = lastPlayerCharactersResult.Read<string>(4);
                        lastPlayedCharacter.CharacterGUID = lastPlayerCharactersResult.Read<ulong>(5);
                        lastPlayedCharacter.LastPlayedTime = lastPlayerCharactersResult.Read<uint>(6);

                    } while (lastPlayerCharactersResult.NextRow());
                }

                string ip_address = GetRemoteIpAddress().ToString();

                string ipCountry = null;
                //IpLocationRecord location = sIPLocation.GetLocationRecord(ip_address);
                //if (location != null)
                // ipCountry = location.CountryCode;

                // If the IP is 'locked', check that the player comes indeed from the correct IP address
                if (accountInfo.IsLockedToIP)
                {
                    Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Account '{accountInfo.Login}' is locked to IP - '{accountInfo.LastIP}' is logging in from '{ip_address}'");

                    if (accountInfo.LastIP != ip_address)
                    {
                        sendResponse(BattlenetRpcErrorCode.RiskAccountLocked);
                        return;
                    }
                }
                else
                {
                    Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Account '{accountInfo.Login}' is not locked to ip");
                    if (accountInfo.LockCountry.IsEmpty() || accountInfo.LockCountry == "00")
                        Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Account '{accountInfo.Login}' is not locked to country");
                    else if (!accountInfo.LockCountry.IsEmpty() && !ipCountry.IsEmpty())
                    {
                        Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Account '{accountInfo.Login}' is locked to country: '{accountInfo.LockCountry}' Player country is '{ipCountry}'");

                        if (ipCountry != accountInfo.LockCountry)
                        {
                            sendResponse(BattlenetRpcErrorCode.RiskAccountLocked);
                            return;
                        }
                    }
                }

                // If the account is banned, reject the logon attempt
                if (accountInfo.IsBanned)
                {
                    if (accountInfo.IsPermanenetlyBanned)
                    {
                        Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Banned account '{accountInfo.Login}' tried to login!");
                        sendResponse(BattlenetRpcErrorCode.GameAccountBanned);
                        return;
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Session, $"[Session::HandleVerifyWebCredentials] Temporarily banned account '{accountInfo.Login}' tried to login!");
                        sendResponse(BattlenetRpcErrorCode.GameAccountSuspended);
                        return;
                    }
                }

                sendResponse(BattlenetRpcErrorCode.Ok);
                sendLogonComplete(accountInfo, ipCountry);

                _accountInfo = accountInfo;
                _ipCountry = ipCountry;
                _authed = true;
            }));

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode HandleGenerateAuthToken(Action<string> sendResponse)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_EXISTING_AUTHENTICATION_BY_ID);
            stmt.AddValue(0, GetAccountId());

            queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                // just send existing credentials back (not the best but it works for now with them being stored in db)
                sendResponse(result.Read<string>(0));
            }));

            return BattlenetRpcErrorCode.Ok;
        }

        [Service(OriginalHash.AuthenticationServiceV2, 1)]
        BattlenetRpcErrorCode HandleLogon(LogonRequest request, NoData response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            string deviceId = "";
            string cachedAuthToken = null;

            if (request.LogonOptions != null)
            {
                var logonOptions = request.LogonOptions;
                if (logonOptions.HasDeviceId)
                    deviceId = logonOptions.DeviceId;

                if (logonOptions.HasAuthToken)
                    cachedAuthToken = logonOptions.AuthToken.ToString();
            }

            BattlenetRpcErrorCode result = HandleLogon(request.TitleId, request.Platform, request.Locale, request.ApplicationVersion, deviceId);
            if (result == BattlenetRpcErrorCode.Ok)
            {
                if (!cachedAuthToken.IsEmpty())
                    return HandleVerifyAuthToken(cachedAuthToken, continuation);

                ExternalChallengeNotification externalChallenge = new()
                {
                    PayloadType = "web_auth_url",
                    Payload = ByteString.CopyFromUtf8($"http{(!Global.LoginServiceMgr.UsesDevWildcardCertificate() ? "s" : "")}://{Global.LoginService.GetHostnameForClient(GetRemoteIpAddress())}:{Global.LoginService.GetPort()}/bnetserver/login/")
                };

                SendRequest((uint)OriginalHash.AuthenticationListenerV2, 4, externalChallenge);
            }

            return result;
        }

        [Service(OriginalHash.AuthenticationServiceV2, 2)]
        BattlenetRpcErrorCode HandleVerifyAuthToken(VerifyAuthTokenRequest request, NoData response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (!request.HasAuthToken)
                return BattlenetRpcErrorCode.Denied;

            return HandleVerifyAuthToken(request.AuthToken.ToStringUtf8(), continuation);
        }

        [Service(OriginalHash.AuthenticationServiceV2, 3)]
        BattlenetRpcErrorCode HandleGenerateAuthToken(GenerateAuthTokenRequest request, GenerateAuthTokenResponse response, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            if (!IsAuthed())
                return BattlenetRpcErrorCode.Denied;

            return HandleGenerateAuthToken(webCredentials =>
            {
                response = new()
                {
                    AuthToken = ByteString.CopyFromUtf8(webCredentials)
                };
                continuation(this, BattlenetRpcErrorCode.Ok, response);
            });
        }

        BattlenetRpcErrorCode HandleVerifyAuthToken(string authToken, Action<Session, BattlenetRpcErrorCode, IMessage> continuation)
        {
            return HandleVerifyAuthToken(authToken, result =>
            {
                NoData response = new();
                continuation(this, result, response);
            },
            (AccountInfo accountInfo, string country) =>
            {
                LogonCompleteNotification logonResult = new()
                {
                    ErrorCode = 0,
                    Record = new()
                };

                LogonRecord logonRecord = logonResult.Record;
                logonRecord.AccountId = accountInfo.Id;
                foreach (var (id, gameAccountInfo) in accountInfo.GameAccounts)
                {
                    GameAccountHandle gameAccount = new()
                    {
                        Id = gameAccountInfo.Id,
                        TitleId = "WoW".ToFourCC(),
                        Region = 2
                    };
                    logonRecord.GameAccount.Add(gameAccount);
                }

                if (!country.IsEmpty())
                    logonRecord.GeoipCountry = country;

                logonRecord.SessionKey = ByteString.CopyFrom(RandomHelper.GetRandomBytes(64));

                SendRequest((uint)OriginalHash.AuthenticationListenerV2, 1, logonResult);
            });
        }
    }
}