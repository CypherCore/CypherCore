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
using Game.DataStorage;
using Game.Network;
using Game.Network.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.DbQueryBulk, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
        void HandleDBQueryBulk(DBQueryBulk dbQuery)
        {
            IDB2Storage store = Global.DB2Mgr.GetStorage(dbQuery.TableHash);
            if (store == null)
            {
                Log.outError(LogFilter.Network, "CMSG_DB_QUERY_BULK: {0} requested unsupported unknown hotfix type: {1}", GetPlayerInfo(), dbQuery.TableHash);
                return;
            }

            foreach (DBQueryBulk.DBQueryRecord record in dbQuery.Queries)
            {
                DBReply dbReply = new DBReply();
                dbReply.TableHash = dbQuery.TableHash;
                dbReply.RecordID = record.RecordID;

                if (store.HasRecord(record.RecordID))
                {
                    dbReply.Allow = true;
                    dbReply.Timestamp = (uint)Global.WorldMgr.GetGameTime();
                    store.WriteRecord(record.RecordID, GetSessionDbcLocale(), dbReply.Data);
                }
                else
                {
                    Log.outTrace(LogFilter.Network, "CMSG_DB_QUERY_BULK: {0} requested non-existing entry {1} in datastore: {2}", GetPlayerInfo(), record.RecordID, dbQuery.TableHash);
                    dbReply.Timestamp = (uint)Time.UnixTime;
                }

                SendPacket(dbReply);
            }
        }

        void SendAvailableHotfixes(int version)
        {
            SendPacket(new AvailableHotfixes(version, Global.DB2Mgr.GetHotfixData()));
        }

        [WorldPacketHandler(ClientOpcodes.HotfixRequest, Status = SessionStatus.Authed)]
        void HandleHotfixRequest(HotfixRequest hotfixQuery)
        {
            Dictionary<ulong, int> hotfixes = Global.DB2Mgr.GetHotfixData();

            HotfixResponse hotfixQueryResponse = new HotfixResponse();
            foreach (ulong hotfixId in hotfixQuery.Hotfixes)
            {
                int hotfix = hotfixes.LookupByKey(hotfixId);
                if (hotfix != 0)
                {
                    var storage = Global.DB2Mgr.GetStorage(MathFunctions.Pair64_HiPart(hotfixId));

                    HotfixResponse.HotfixData hotfixData = new HotfixResponse.HotfixData();
                    hotfixData.ID = hotfixId;
                    hotfixData.RecordID = hotfix;
                    if (storage != null && storage.HasRecord((uint)hotfixData.RecordID))
                    {
                        hotfixData.Data.HasValue = true;
                        storage.WriteRecord((uint)hotfixData.RecordID, GetSessionDbcLocale(), hotfixData.Data.Value);
                    }
                    else
                    {
                        byte[] blobData = Global.DB2Mgr.GetHotfixBlobData(MathFunctions.Pair64_HiPart(hotfixId), hotfix);
                        if (blobData != null)
                        {
                            hotfixData.Data.HasValue = true;
                            hotfixData.Data.Value.WriteBytes(blobData);
                        }
                    }

                    hotfixQueryResponse.Hotfixes.Add(hotfixData);
                }
            }

            SendPacket(hotfixQueryResponse);
        }
    }
}
