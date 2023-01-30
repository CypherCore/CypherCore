// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells
{
    public class GOTargetInfo : TargetInfoBase
    {
        public ObjectGuid TargetGUID;
        public ulong TimeDelay { get; set; }

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            GameObject go = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToGameObject() : ObjectAccessor.GetGameObject(spell.GetCaster(), TargetGUID);

            if (go == null)
                return;

            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, null, go, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            //AI functions
            go.GetAI()?.SpellHit(spell.GetCaster(), spell.SpellInfo);

            if (spell.GetCaster().IsCreature() &&
                spell.GetCaster().ToCreature().IsAIEnabled())
                spell.GetCaster().ToCreature().GetAI().SpellHitTarget(go, spell.SpellInfo);
            else if (spell.GetCaster().IsGameObject() &&
                     spell.GetCaster().ToGameObject().GetAI() != null)
                spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(go, spell.SpellInfo);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }
}