// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System.Linq;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
        void HandleDBQueryBulk(DBQueryBulk dbQuery)
        {
            IDB2Storage store = Global.DB2Mgr.GetStorage(dbQuery.TableHash);

            foreach (DBQueryBulk.DBQueryRecord record in dbQuery.Queries)
            {
                DBReply dbReply = new();
                dbReply.TableHash = dbQuery.TableHash;
                dbReply.RecordID = record.RecordID;

                if (store != null && store.HasRecord(record.RecordID))
                {
                    dbReply.Status = HotfixRecord.Status.Valid;
                    dbReply.Timestamp = (uint)GameTime.GetGameTime();
                    store.WriteRecord(record.RecordID, GetSessionDbcLocale(), dbReply.Data);

                    var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(dbQuery.TableHash, record.RecordID, GetSessionDbcLocale());
                    foreach (HotfixOptionalData optionalData in optionalDataEntries)
                    {
                        dbReply.Data.WriteUInt32(optionalData.Key);
                        dbReply.Data.WriteBytes(optionalData.Data);
                    }
                }
                else
                {
                    Log.outTrace(LogFilter.Network, "CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
                    dbReply.Timestamp = (uint)GameTime.GetGameTime();
                }

                SendPacket(dbReply);
            }
        }

        void SendAvailableHotfixes()
        {
            AvailableHotfixes availableHotfixes = new();
            availableHotfixes.VirtualRealmAddress = Global.WorldMgr.GetRealmId().GetAddress();

            foreach (var (_, push) in Global.DB2Mgr.GetHotfixData())
            {
                if ((push.AvailableLocalesMask & (1 << (int)GetSessionDbcLocale())) == 0)
                    continue;

                availableHotfixes.Hotfixes.Add(push.Records.First().ID);
            }

            SendPacket(availableHotfixes);
        }

        [WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
        void HandleHotfixRequest(HotfixRequest hotfixQuery)
        {
            var hotfixes = Global.DB2Mgr.GetHotfixData();

            HotfixConnect hotfixQueryResponse = new();
            foreach (var hotfixId in hotfixQuery.Hotfixes)
            {
                var hotfixRecords = hotfixes.LookupByKey(hotfixId);
                if (hotfixRecords != null)
                {
                    foreach (var hotfixRecord in hotfixRecords.Records)
                    {
                        if ((hotfixRecord.AvailableLocalesMask & (1 << (int)GetSessionDbcLocale())) == 0)
                            continue;

                        HotfixConnect.HotfixData hotfixData = new();
                        hotfixData.Record = hotfixRecord;
                        if (hotfixRecord.HotfixStatus == HotfixRecord.Status.Valid)
                        {
                            var storage = Global.DB2Mgr.GetStorage(hotfixRecord.TableHash);
                            if (storage != null && storage.HasRecord((uint)hotfixRecord.RecordID))
                            {
                                var pos = hotfixQueryResponse.HotfixContent.GetSize();
                                storage.WriteRecord((uint)hotfixRecord.RecordID, GetSessionDbcLocale(), hotfixQueryResponse.HotfixContent);

                                var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(hotfixRecord.TableHash, (uint)hotfixRecord.RecordID, GetSessionDbcLocale());
                                if (optionalDataEntries != null)
                                {
                                    foreach (var optionalData in optionalDataEntries)
                                    {
                                        hotfixQueryResponse.HotfixContent.WriteUInt32(optionalData.Key);
                                        hotfixQueryResponse.HotfixContent.WriteBytes(optionalData.Data);
                                    }
                                }

                                hotfixData.Size = hotfixQueryResponse.HotfixContent.GetSize() - pos;
                            }
                            else
                            {
                                var blobData = Global.DB2Mgr.GetHotfixBlobData(hotfixRecord.TableHash, hotfixRecord.RecordID, GetSessionDbcLocale());
                                if (blobData != null)
                                {
                                    hotfixData.Size = (uint)blobData.Length;
                                    hotfixQueryResponse.HotfixContent.WriteBytes(blobData);
                                }
                                else
                                    // Do not send Status::Valid when we don't have a hotfix blob for current locale
                                    hotfixData.Record.HotfixStatus = storage != null ? HotfixRecord.Status.RecordRemoved : HotfixRecord.Status.Invalid;
                            }
                        }

                        hotfixQueryResponse.Hotfixes.Add(hotfixData);
                    }
                }
            }

            SendPacket(hotfixQueryResponse);
        }
    }
}
