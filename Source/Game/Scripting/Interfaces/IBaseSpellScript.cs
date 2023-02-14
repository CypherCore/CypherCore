// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Spells;

namespace Game.Scripting.Interfaces
{
    public interface IBaseSpellScript
    {
        byte CurrentScriptState { get; set; }
        string ScriptName { get; set; }
        uint ScriptSpellId { get; set; }

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