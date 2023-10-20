// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Events
{
    [Script]
    class spell_operation_gnomeregan_basic_orders_emote : AuraScript
    {
        const uint SpellTestSalute = 73835;
        const uint SpellTestRoar = 73836;
        const uint SpellTestCheer = 73725;
        const uint SpellTestDance = 73837;
        const uint SpellTestStopDance = 73886;

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit target = GetTarget();

            switch (GetId())
            {
                case SpellTestSalute:
                    target.HandleEmoteCommand(Emote.OneshotSalute);
                    break;
                case SpellTestRoar:
                    target.HandleEmoteCommand(Emote.OneshotRoar);
                    break;
                case SpellTestCheer:
                    target.HandleEmoteCommand(Emote.OneshotCheer);
                    break;
                case SpellTestDance:
                    target.SetEmoteState(Emote.StateDance);
                    break;
                case SpellTestStopDance:
                    target.SetEmoteState(Emote.StateNone);
                    break;
                default:
                    return;
            }
            Remove();
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }
    }
}