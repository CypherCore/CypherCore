// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public interface IUnitAI
    {
        void AttackStart(Unit victim);
        void AttackStartCaster(Unit victim, float dist);
        bool CanAIAttack(Unit victim);
        void DamageDealt(Unit victim, ref double damage, DamageEffectType damageType);
        void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null);
        void DoAction(int action);
        SpellCastResult DoCast(uint spellId);
        SpellCastResult DoCast(Unit victim, uint spellId, CastSpellExtraArgs args = null);
        SpellCastResult DoCastAOE(uint spellId, CastSpellExtraArgs args = null);
        SpellCastResult DoCastSelf(uint spellId, CastSpellExtraArgs args = null);
        SpellCastResult DoCastVictim(uint spellId, CastSpellExtraArgs args = null);
        void DoMeleeAttackIfReady();
        bool DoSpellAttackIfReady(uint spellId);
        uint GetData(uint id = 0);
        string GetDebugInfo();
        ObjectGuid GetGUID(int id = 0);
        void HealDone(Unit to, double addhealth);
        void HealReceived(Unit by, double addhealth);
        void InitializeAI();
        void JustEnteredCombat(Unit who);
        void JustExitedCombat();
        void OnCharmed(bool isNew);
        void OnDespawn();
        void OnGameEvent(bool start, ushort eventId);
        void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra);
        void Reset();
        Unit SelectTarget(SelectTargetMethod targetType, uint offset = 0, float dist = 0, bool playerOnly = false, bool withTank = true, int aura = 0);
        Unit SelectTarget(SelectTargetMethod targetType, uint offset, ICheck<Unit> selector);
        Unit SelectTarget(SelectTargetMethod targetType, uint offset, UnitAI.SelectTargetDelegate selector);
        List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset = 0, float dist = 0, bool playerOnly = false, bool withTank = true, int aura = 0);
        List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset, UnitAI.SelectTargetDelegate selector);
        void SetData(uint id, uint value);
        void SetGUID(ObjectGuid guid, int id = 0);
        bool ShouldSparWith(Unit target);
        void SpellInterrupted(uint spellId, uint unTimeMs);
        void UpdateAI(uint diff);
    }
}