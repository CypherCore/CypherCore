// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(48020)] // 48020 - Demonic Circle: Teleport
    internal class spell_warl_demonic_circle_teleport : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }

        private void HandleTeleport(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetTarget().ToPlayer();

            if (player)
            {
                GameObject circle = player.GetGameObject(WarlockSpells.DEMONIC_CIRCLE_SUMMON);

                if (circle)
                {
                    player.NearTeleportTo(circle.GetPositionX(), circle.GetPositionY(), circle.GetPositionZ(), circle.GetOrientation());
                    player.RemoveMovementImpairingAuras(false);
                }
            }
        }
    }
}