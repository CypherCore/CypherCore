// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;
using Game.Spells;

namespace Game.Scripting.Interfaces
{
    public interface IAuraScript : IBaseSpellScript
    {
        Aura GetAura();
        Difficulty GetCastDifficulty();
        Unit GetCaster();
        ObjectGuid GetCasterGUID();
        int GetDuration();
        AuraEffect GetEffect(byte effIndex);
        SpellEffectInfo GetEffectInfo(uint effIndex);
        GameObject GetGObjCaster();
        uint GetId();
        int GetMaxDuration();
        WorldObject GetOwner();
        SpellInfo GetSpellInfo();
        byte GetStackAmount();
        Unit GetTarget();
        AuraApplication GetTargetApplication();
        Unit GetUnitOwner();
        bool HasEffect(byte effIndex);
        bool IsExpired();
        bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default);
        void PreventDefaultAction();
        void Remove(AuraRemoveMode removeMode = AuraRemoveMode.None);
        void SetDuration(int duration, bool withMods = false);
        void SetMaxDuration(int duration);
        void _FinishScriptCall();
        bool _IsDefaultActionPrevented();
        bool _Load(Aura aura);
        void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null);
    }
}