// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Spells;

namespace Game.Scripting.Interfaces
{
    public interface IBaseSpellScript
    {
        byte m_currentScriptState { get; set; }
        string m_scriptName { get; set; }
        uint m_scriptSpellId { get; set; }

        bool Load();
        void Register();
        void Unload();
        bool Validate(SpellInfo spellInfo);
        bool ValidateSpellInfo(params uint[] spellIds);
        string _GetScriptName();
        void _Init(string scriptname, uint spellId);
        void _Register();
        void _Unload();
        bool _Validate(SpellInfo entry);
    }
}