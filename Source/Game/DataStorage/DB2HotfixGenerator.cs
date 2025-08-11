// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Game.DataStorage
{
    class DB2HotfixGenerator<T> where T : new()
    {
        DB6Storage<T> _storage;
        uint _count;

        public DB2HotfixGenerator(DB6Storage<T> storage)
        {
            _storage = storage;
        }

        public void ApplyHotfix(uint id, Action<T> fixer, bool notifyClient = false) { ApplyHotfix([id, 1], fixer, notifyClient); }

        public uint GetAppliedHotfixesCount() { return _count; }

        void ApplyHotfix(Span<uint> ids, Action<T> fixer, bool notifyClient)
        {
            foreach (uint id in ids)
            {
                T entry = _storage.LookupByKey(id);
                if (entry == null)
                {
                    LogMissingRecord(_storage.GetName(), id);
                    continue;
                }

                fixer(entry);
                ++_count;

                if (notifyClient)
                    AddClientHotfix(_storage.GetTableHash(), id);
            }
        }

        void LogMissingRecord(string storageName, uint recordId)
        {
            Log.outError(LogFilter.Server, $"Hotfix specified for {storageName} row id {recordId} which does not exist");
        }

        void AddClientHotfix(uint tableHash, uint recordId)
        {
            Global.DB2Mgr.InsertNewHotfix(tableHash, recordId);
        }
    }
}
