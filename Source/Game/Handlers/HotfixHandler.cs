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
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
        void HandleDBQueryBulk(DBQueryBulk dbQuery)
        {
            var store = Global.DB2Mgr.GetStorage(dbQuery.TableHash);

            foreach (var record in dbQuery.Queries)
            {
                var dbReply = new DBReply();
                dbReply.TableHash = dbQuery.TableHash;
                dbReply.RecordID = record.RecordID;

                if (store != null && store.HasRecord(record.RecordID))
                {
                    dbReply.Status = HotfixRecord.Status.Valid;
                    dbReply.Timestamp = (uint)GameTime.GetGameTime();
                    store.WriteRecord(record.RecordID, GetSessionDbcLocale(), dbReply.Data);

                    var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(dbQuery.TableHash, record.RecordID, GetSessionDbcLocale());
                    foreach (var optionalData in optionalDataEntries)
                    {
                        dbReply.Data.WriteUInt32(optionalData.Key);
                        dbReply.Data.WriteBytes(optionalData.Data);
                    }
                }
                else
                {
                    Log.outTrace(LogFilter.Network, "CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
                    dbReply.Timestamp = (uint)Time.UnixTime;
                }

                SendPacket(dbReply);
            }
        }

        void SendAvailableHotfixes()
        {
            SendPacket(new AvailableHotfixes(Global.WorldMgr.GetRealmId().GetAddress(), Global.DB2Mgr.GetHotfixCount(), Global.DB2Mgr.GetHotfixData()));
        }

        [WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
        void HandleHotfixRequest(HotfixRequest hotfixQuery)
        {
            var hotfixes = Global.DB2Mgr.GetHotfixData();

            var hotfixQueryResponse = new HotfixConnect();
            foreach (var hotfixRecord in hotfixQuery.Hotfixes)
            {
                var serverHotfixIndex = hotfixes.IndexOf(hotfixRecord);
                if (serverHotfixIndex != -1)
                {
                    var hotfixData = new HotfixConnect.HotfixData();
                    hotfixData.Record = hotfixes[serverHotfixIndex];
                    if (hotfixData.Record.HotfixStatus == HotfixRecord.Status.Valid)
                    {
                        var storage = Global.DB2Mgr.GetStorage(hotfixRecord.TableHash);
                        if (storage != null && storage.HasRecord((uint)hotfixRecord.RecordID))
                        {
                            var pos = hotfixQueryResponse.HotfixContent.GetSize();
                            storage.WriteRecord((uint)hotfixRecord.RecordID, GetSessionDbcLocale(), hotfixQueryResponse.HotfixContent);

                            var optionalDataEntries = Global.DB2Mgr.GetHotfixOptionalData(hotfixRecord.TableHash, (uint)hotfixRecord.RecordID, GetSessionDbcLocale());
                            foreach (var optionalData in optionalDataEntries)
                            {
                                hotfixQueryResponse.HotfixContent.WriteUInt32(optionalData.Key);
                                hotfixQueryResponse.HotfixContent.WriteBytes(optionalData.Data);
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
                        }
                    }

                    hotfixQueryResponse.Hotfixes.Add(hotfixData);
                }
            }

            SendPacket(hotfixQueryResponse);
        }
    }
}
