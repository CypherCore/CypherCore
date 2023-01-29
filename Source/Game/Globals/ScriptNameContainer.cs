// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Game
{
    internal class ScriptNameContainer
    {
        public class Entry
        {
            public uint Id;
            public bool IsScriptDatabaseBound;
            public string Name;

            public Entry(uint id, bool isScriptDatabaseBound, string name)
            {
                Id = id;
                IsScriptDatabaseBound = isScriptDatabaseBound;
                Name = name;
            }
        }

        private readonly List<Entry> _indexToName = new();
        private readonly Dictionary<string, Entry> _nameToIndex = new();

        public ScriptNameContainer()
        {
            // We insert an empty placeholder here so we can use the
            // script Id 0 as dummy for "no script found".
            uint id = Insert("", false);

            Cypher.Assert(id == 0);
        }

        public uint Insert(string scriptName, bool isScriptNameBound)
        {
            Entry entry = new((uint)_nameToIndex.Count, isScriptNameBound, scriptName);
            var result = _nameToIndex.TryAdd(scriptName, entry);

            if (result)
            {
                Cypher.Assert(_nameToIndex.Count <= int.MaxValue);
                _indexToName.Add(entry);
            }

            return _nameToIndex[scriptName].Id;
        }

        public int GetSize()
        {
            return _indexToName.Count;
        }

        public Entry Find(uint index)
        {
            return index < _indexToName.Count ? _indexToName[(int)index] : null;
        }

        public Entry Find(string name)
        {
            // assume "" is the first element
            if (name.IsEmpty())
                return null;

            return _nameToIndex.LookupByKey(name);
        }

        public List<string> GetAllDBScriptNames()
        {
            List<string> scriptNames = new();

            foreach (var (name, entry) in _nameToIndex)
                if (entry.IsScriptDatabaseBound)
                    scriptNames.Add(name);

            return scriptNames;
        }
    }
}