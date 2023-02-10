// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Game.Scripting.Interfaces;

namespace Game.Scripting
{
    public class ScriptRegistry
    {
        private readonly Dictionary<uint, IScriptObject> _scriptMap = new();

        // Counter used for code-only scripts.
        private uint _scriptIdCounter;

        public void AddScript(IScriptObject script)
        {
            Cypher.Assert(script != null);

            if (!script.IsDatabaseBound())
            {
                // We're dealing with a code-only script; just add it.
                _scriptMap[Interlocked.Increment(ref _scriptIdCounter)] = script;
                Global.ScriptMgr.IncrementScriptCount();

                return;
            }

            // Get an ID for the script. An ID only exists if it's a script that is assigned in the database
            // through a script Name (or similar).
            uint id = Global.ObjectMgr.GetScriptId(script.GetName());

            if (id != 0)
            {
                // Try to find an existing script.
                bool existing = false;

                lock (_scriptMap)
                {
                    if (_scriptMap.Values.Contains(script)) // its already in here
                        return;

                    foreach (var it in _scriptMap)
                        if (it.Value.GetName() == script.GetName())
                        {
                            existing = true;

                            break;
                        }
                }

                // If the script isn't assigned . assign it!
                if (!existing)
                {
                    lock (_scriptMap)
                    {
                        _scriptMap[id] = script;
                    }

                    Global.ScriptMgr.IncrementScriptCount();
                }
                else
                {
                    // If the script is already assigned . delete it!
                    Log.outWarn(LogFilter.Scripts, "Script '{0}' already assigned with the same script Name, so the script can't work.", script.GetName());
                }
            }
            else
            {
                // The script uses a script Name from database, but isn't assigned to anything.
                Log.outWarn(LogFilter.Sql, "Script named '{0}' does not have a script Name assigned in database.", script.GetName());
            }
        }

        // Gets a script by its ID (assigned by ObjectMgr).
        public T GetScriptById<T>(uint id) where T : IScriptObject
        {
            lock (_scriptMap)
            {
                return (T)_scriptMap.LookupByKey(id);
            }
        }

        public bool Empty()
        {
            lock (_scriptMap)
            {
                return _scriptMap.Empty();
            }
        }

        public void Unload()
        {
            lock (_scriptMap)
            {
                _scriptMap.Clear();
            }
        }
    }
}