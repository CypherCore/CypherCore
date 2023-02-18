// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(47319)]
        public class npc_warl_demonic_gateway : CreatureAI
        {
            public EventMap events = new();
            public bool firstTick = true;

            public npc_warl_demonic_gateway(Creature creature) : base(creature)
            {
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                if (firstTick)
                {
                    me.CastSpell(me, WarlockSpells.DEMONIC_GATEWAY_VISUAL, true);

                    //todo me->SetInteractSpellId(WARLOCK_DEMONIC_GATEWAY_ACTIVATE);
                    me.SetUnitFlag(UnitFlags.NonAttackable);
                    me.SetNpcFlag(NPCFlags.SpellClick);
                    me.SetReactState(ReactStates.Passive);
                    me.SetControlled(true, UnitState.Root);

                    firstTick = false;
                }
            }

            public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
            {
                if (clicker.IsPlayer())
                {
                    // don't allow using the gateway while having specific Auras
                    uint[] aurasToCheck =
                    {
                            121164, 121175, 121176, 121177
                        }; // Orbs of Power @ Temple of Kotmogu

                    foreach (var auraToCheck in aurasToCheck)
                        if (clicker.HasAura(auraToCheck))
                            return;

                    TeleportTarget(clicker, true);
                }

                return;
            }

            public void TeleportTarget(Unit target, bool allowAnywhere)
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                // only if Target stepped through the portal
                if (!allowAnywhere &&
                    me.GetDistance2d(target) > 1.0f)
                    return;

                // check if Target wasn't recently teleported
                if (target.HasAura(WarlockSpells.DEMONIC_GATEWAY_DEBUFF))
                    return;

                // only if in same party
                if (!target.IsInRaidWith(owner))
                    return;

                // not allowed while CC'ed
                if (!target.CanFreeMove())
                    return;

                uint otherGateway = me.GetEntry() == WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE : WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN;
                uint teleportSpell = me.GetEntry() == WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? WarlockSpells.DEMONIC_GATEWAY_JUMP_GREEN : WarlockSpells.DEMONIC_GATEWAY_JUMP_PURPLE;

                var gateways = me.GetCreatureListWithEntryInGrid(otherGateway, 100.0f);

                foreach (var gateway in gateways)
                {
                    if (gateway.GetOwnerGUID() != me.GetOwnerGUID())
                        continue;

                    target.CastSpell(gateway, teleportSpell, true);

                    if (target.HasAura(WarlockSpells.PLANESWALKER))
                        target.CastSpell(target, WarlockSpells.PLANESWALKER_BUFF, true);

                    // Item - Warlock PvP Set 4P Bonus: "Your allies can use your Demonic Gateway again 15 sec sooner"
                    int amount = owner.GetAuraEffect(WarlockSpells.PVP_4P_BONUS, 0).GetAmount();

                    if (amount > 0)
                    {
                        Aura aura = target.GetAura(WarlockSpells.DEMONIC_GATEWAY_DEBUFF);

                        aura?.SetDuration(aura.GetDuration() - amount * Time.InMilliseconds);
                    }

                    break;
                }
            }
        }
    }
}