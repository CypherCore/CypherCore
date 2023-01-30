// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;

namespace Game.Spells.Auras.EffectHandlers
{
    public partial class AuraEffect
    {
        /*********************************************************/
        /***                    OTHERS                         ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.Dummy)]
        private void HandleAuraDummy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Reapply))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            // pet Auras
            if (target.GetTypeId() == TypeId.Player &&
                mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                PetAura petSpell = Global.SpellMgr.GetPetAura(GetId(), (byte)GetEffIndex());

                if (petSpell != null)
                {
                    if (apply)
                        target.ToPlayer().AddPetAura(petSpell);
                    else
                        target.ToPlayer().RemovePetAura(petSpell);
                }
            }

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply))
            {
                // AT APPLY
                if (apply)
                    switch (GetId())
                    {
                        case 1515: // Tame beast
                                   // FIX_ME: this is 2.0.12 threat effect replaced in 2.1.x by dummy aura, must be checked for correctness
                            if (caster != null &&
                                target.CanHaveThreatList())
                                target.GetThreatManager().AddThreat(caster, 10.0f);

                            break;
                        case 13139: // net-o-matic
                                    // root to self part of (root_target.charge.root_self sequence
                            caster?.CastSpell(caster, 13138, new CastSpellExtraArgs(this));

                            break;
                        case 34026: // kill command
                            {
                                Unit pet = target.GetGuardianPet();

                                if (pet == null)
                                    break;

                                target.CastSpell(target, 34027, new CastSpellExtraArgs(this));

                                // set 3 stacks and 3 charges (to make all Auras not disappear at once)
                                Aura owner_aura = target.GetAura(34027, GetCasterGUID());
                                Aura pet_aura = pet.GetAura(58914, GetCasterGUID());

                                if (owner_aura != null)
                                {
                                    owner_aura.SetStackAmount((byte)owner_aura.GetSpellInfo().StackAmount);

                                    if (pet_aura != null)
                                    {
                                        pet_aura.SetCharges(0);
                                        pet_aura.SetStackAmount((byte)owner_aura.GetSpellInfo().StackAmount);
                                    }
                                }

                                break;
                            }
                        case 37096: // Blood Elf Illusion
                            {
                                if (caster != null)
                                {
                                    if (caster.GetGender() == Gender.Female)
                                        caster.CastSpell(target, 37095, new CastSpellExtraArgs(this)); // Blood Elf Disguise
                                    else
                                        caster.CastSpell(target, 37093, new CastSpellExtraArgs(this));
                                }

                                break;
                            }
                        case 39850:                          // Rocket Blast
                            if (RandomHelper.randChance(20)) // backfire stun
                                target.CastSpell(target, 51581, new CastSpellExtraArgs(this));

                            break;
                        case 43873: // Headless Horseman Laugh
                            target.PlayDistanceSound(11965);

                            break;
                        case 46354: // Blood Elf Illusion
                            if (caster != null)
                            {
                                if (caster.GetGender() == Gender.Female)
                                    caster.CastSpell(target, 46356, new CastSpellExtraArgs(this));
                                else
                                    caster.CastSpell(target, 46355, new CastSpellExtraArgs(this));
                            }

                            break;
                        case 46361: // Reinforced Net
                            if (caster != null)
                                target.GetMotionMaster().MoveFall();

                            break;
                    }
                // AT REMOVE
                else
                    switch (_spellInfo.SpellFamilyName)
                    {
                        case SpellFamilyNames.Generic:
                            switch (GetId())
                            {
                                case 2584: // Waiting to Resurrect
                                           // Waiting to resurrect spell cancel, we must remove player from resurrect queue
                                    if (target.IsTypeId(TypeId.Player))
                                    {
                                        Battleground bg = target.ToPlayer().GetBattleground();

                                        if (bg)
                                            bg.RemovePlayerFromResurrectQueue(target.GetGUID());

                                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(target.GetMap(), target.GetZoneId());

                                        bf?.RemovePlayerFromResurrectQueue(target.GetGUID());
                                    }

                                    break;
                                case 36730: // Flame Strike
                                    target.CastSpell(target, 36731, new CastSpellExtraArgs(this));

                                    break;
                                case 43681: // Inactive
                                    {
                                        if (!target.IsTypeId(TypeId.Player) ||
                                            aurApp.GetRemoveMode() != AuraRemoveMode.Expire)
                                            return;

                                        if (target.GetMap().IsBattleground())
                                            target.ToPlayer().LeaveBattleground();

                                        break;
                                    }
                                case 42783: // Wrath of the Astromancer
                                    target.CastSpell(target, (uint)GetAmount(), new CastSpellExtraArgs(this));

                                    break;
                                case 46308: // Burning Winds casted only at creatures at spawn
                                    target.CastSpell(target, 47287, new CastSpellExtraArgs(this));

                                    break;
                                case 52172: // Coyote Spirit Despawn Aura
                                case 60244: // Blood Parrot Despawn Aura
                                    target.CastSpell((Unit)null, (uint)GetAmount(), new CastSpellExtraArgs(this));

                                    break;
                                case 91604: // Restricted Flight Area
                                    if (aurApp.GetRemoveMode() == AuraRemoveMode.Expire)
                                        target.CastSpell(target, 58601, new CastSpellExtraArgs(this));

                                    break;
                            }

                            break;
                        case SpellFamilyNames.Deathknight:
                            // Summon Gargoyle (Dismiss Gargoyle at remove)
                            if (GetId() == 61777)
                                target.CastSpell(target, (uint)GetAmount(), new CastSpellExtraArgs(this));

                            break;
                        default:
                            break;
                    }
            }

            // AT APPLY & REMOVE

            switch (_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    {
                        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                            break;

                        switch (GetId())
                        {
                            // Recently Bandaged
                            case 11196:
                                target.ApplySpellImmune(GetId(), SpellImmunity.Mechanic, (uint)GetMiscValue(), apply);

                                break;
                            // Unstable Power
                            case 24658:
                                {
                                    uint spellId = 24659;

                                    if (apply && caster != null)
                                    {
                                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());
                                        CastSpellExtraArgs args = new();
                                        args.TriggerFlags = TriggerCastFlags.FullMask;
                                        args.OriginalCaster = GetCasterGUID();
                                        args.OriginalCastId = GetBase().GetCastId();
                                        args.CastDifficulty = GetBase().GetCastDifficulty();

                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell.Id, args);

                                        break;
                                    }

                                    target.RemoveAurasDueToSpell(spellId);

                                    break;
                                }
                            // Restless Strength
                            case 24661:
                                {
                                    uint spellId = 24662;

                                    if (apply && caster != null)
                                    {
                                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());
                                        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                        args.OriginalCaster = GetCasterGUID();
                                        args.OriginalCastId = GetBase().GetCastId();
                                        args.CastDifficulty = GetBase().GetCastDifficulty();

                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell.Id, args);

                                        break;
                                    }

                                    target.RemoveAurasDueToSpell(spellId);

                                    break;
                                }
                            // Tag Murloc
                            case 30877:
                                {
                                    // Tag/untag Blacksilt Scout
                                    target.SetEntry((uint)(apply ? 17654 : 17326));

                                    break;
                                }
                            case 57819: // Argent Champion
                            case 57820: // Ebon Champion
                            case 57821: // Champion of the Kirin Tor
                            case 57822: // Wyrmrest Champion
                                {
                                    if (!caster ||
                                        !caster.IsTypeId(TypeId.Player))
                                        break;

                                    uint FactionID = 0;

                                    if (apply)
                                        switch (_spellInfo.Id)
                                        {
                                            case 57819:
                                                FactionID = 1106; // Argent Crusade

                                                break;
                                            case 57820:
                                                FactionID = 1098; // Knights of the Ebon Blade

                                                break;
                                            case 57821:
                                                FactionID = 1090; // Kirin Tor

                                                break;
                                            case 57822:
                                                FactionID = 1091; // The Wyrmrest Accord

                                                break;
                                        }

                                    caster.ToPlayer().SetChampioningFaction(FactionID);

                                    break;
                                }
                            // LK Intro VO (1)
                            case 58204:
                                if (target.IsTypeId(TypeId.Player))
                                {
                                    // Play part 1
                                    if (apply)
                                        target.PlayDirectSound(14970, target.ToPlayer());
                                    // continue in 58205
                                    else
                                        target.CastSpell(target, 58205, new CastSpellExtraArgs(this));
                                }

                                break;
                            // LK Intro VO (2)
                            case 58205:
                                if (target.IsTypeId(TypeId.Player))
                                {
                                    // Play part 2
                                    if (apply)
                                        target.PlayDirectSound(14971, target.ToPlayer());
                                    // Play part 3
                                    else
                                        target.PlayDirectSound(14972, target.ToPlayer());
                                }

                                break;
                        }

                        break;
                    }
            }
        }

        [AuraEffectHandler(AuraType.ChannelDeathItem)]
        private void HandleChannelDeathItem(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (apply || aurApp.GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Unit caster = GetCaster();

            if (caster == null ||
                !caster.IsTypeId(TypeId.Player))
                return;

            Player plCaster = caster.ToPlayer();
            Unit target = aurApp.GetTarget();

            // Item amount
            if (GetAmount() <= 0)
                return;

            if (GetSpellEffectInfo().ItemType == 0)
                return;

            // Soul Shard
            if (GetSpellEffectInfo().ItemType == 6265)
                // Soul Shard only from units that grant XP or honor
                if (!plCaster.IsHonorOrXPTarget(target) ||
                    target.IsTypeId(TypeId.Unit) && !target.ToCreature().IsTappedBy(plCaster))
                    return;

            //Adding items
            uint noSpaceForCount;
            uint count = (uint)GetAmount();

            List<ItemPosCount> dest = new();
            InventoryResult msg = plCaster.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, GetSpellEffectInfo().ItemType, count, out noSpaceForCount);

            if (msg != InventoryResult.Ok)
            {
                count -= noSpaceForCount;
                plCaster.SendEquipError(msg, null, null, GetSpellEffectInfo().ItemType);

                if (count == 0)
                    return;
            }

            Item newitem = plCaster.StoreNewItem(dest, GetSpellEffectInfo().ItemType, true);

            if (newitem == null)
            {
                plCaster.SendEquipError(InventoryResult.ItemNotFound);

                return;
            }

            plCaster.SendNewItem(newitem, count, true, true);
        }

        [AuraEffectHandler(AuraType.BindSight)]
        private void HandleBindSight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (caster == null ||
                !caster.IsTypeId(TypeId.Player))
                return;

            caster.ToPlayer().SetViewpoint(target, apply);
        }

        [AuraEffectHandler(AuraType.ForceReaction)]
        private void HandleForceReaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            Player player = target.ToPlayer();

            if (player == null)
                return;

            uint factionId = (uint)GetMiscValue();
            ReputationRank factionRank = (ReputationRank)GetAmount();

            player.GetReputationMgr().ApplyForceReaction(factionId, factionRank, apply);
            player.GetReputationMgr().SendForceReactions();

            // stop fighting at apply (if forced rank friendly) or at remove (if real rank friendly)
            if (apply && factionRank >= ReputationRank.Friendly ||
                !apply && player.GetReputationRank(factionId) >= ReputationRank.Friendly)
                player.StopAttackFaction(factionId);
        }

        [AuraEffectHandler(AuraType.Empathy)]
        private void HandleAuraEmpathy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            if (target.GetCreatureType() == CreatureType.Beast)
            {
                if (apply)
                    target.SetDynamicFlag(UnitDynFlags.SpecialInfo);
                else
                    target.RemoveDynamicFlag(UnitDynFlags.SpecialInfo);
            }
        }

        [AuraEffectHandler(AuraType.ModFaction)]
        private void HandleAuraModFaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetFaction((uint)GetMiscValue());

                if (target.IsTypeId(TypeId.Player))
                    target.RemoveUnitFlag(UnitFlags.PlayerControlled);
            }
            else
            {
                target.RestoreFaction();

                if (target.IsTypeId(TypeId.Player))
                    target.SetUnitFlag(UnitFlags.PlayerControlled);
            }
        }

        [AuraEffectHandler(AuraType.LearnSpell)]
        private void HandleLearnSpell(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (player == null)
                return;

            if (apply)
                player.LearnSpell((uint)GetMiscValue(), true, 0, true);
            else
                player.RemoveSpell((uint)GetMiscValue(), false, false, true);
        }

        [AuraEffectHandler(AuraType.ComprehendLanguage)]
        private void HandleComprehendLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag2(UnitFlags2.ComprehendLang);
            }
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.ComprehendLang);
            }
        }

        [AuraEffectHandler(AuraType.ModAlternativeDefaultLanguage)]
        private void HandleModAlternativeDefaultLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
            }
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
            }
        }

        [AuraEffectHandler(AuraType.Linked)]
        private void HandleAuraLinked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            Unit target = aurApp.GetTarget();

            uint triggeredSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggeredSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo == null)
                return;

            Unit caster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? GetCaster() : target;

            if (!caster)
                return;

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                if (apply)
                {
                    CastSpellExtraArgs args = new(this);

                    if (GetAmount() != 0) // If amount avalible cast with basepoints (Crypt Fever for example)
                        args.AddSpellMod(SpellValueMod.BasePoint0, GetAmount());

                    caster.CastSpell(target, triggeredSpellId, args);
                }
                else
                {
                    ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? GetCasterGUID() : target.GetGUID();
                    target.RemoveAura(triggeredSpellId, casterGUID);
                }
            }
            else if (mode.HasAnyFlag(AuraEffectHandleModes.Reapply) && apply)
            {
                ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? GetCasterGUID() : target.GetGUID();
                // change the stack amount to be equal to stack amount of our aura
                Aura triggeredAura = target.GetAura(triggeredSpellId, casterGUID);

                triggeredAura?.ModStackAmount(GetBase().GetStackAmount() - triggeredAura.GetStackAmount());
            }
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnPowerPct)]
        private void HandleTriggerSpellOnPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) ||
                !apply)
                return;

            Unit target = aurApp.GetTarget();

            int effectAmount = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;
            float powerAmountPct = MathFunctions.GetPctOf(target.GetPower((PowerType)GetMiscValue()), target.GetMaxPower((PowerType)GetMiscValue()));

            switch ((AuraTriggerOnPowerChangeDirection)GetMiscValueB())
            {
                case AuraTriggerOnPowerChangeDirection.Gain:
                    if (powerAmountPct < effectAmount)
                        return;

                    break;
                case AuraTriggerOnPowerChangeDirection.Loss:
                    if (powerAmountPct > effectAmount)
                        return;

                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnPowerAmount)]
        private void HandleTriggerSpellOnPowerAmount(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) ||
                !apply)
                return;

            Unit target = aurApp.GetTarget();

            int effectAmount = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;
            float powerAmount = target.GetPower((PowerType)GetMiscValue());

            switch ((AuraTriggerOnPowerChangeDirection)GetMiscValueB())
            {
                case AuraTriggerOnPowerChangeDirection.Gain:
                    if (powerAmount < effectAmount)
                        return;

                    break;
                case AuraTriggerOnPowerChangeDirection.Loss:
                    if (powerAmount > effectAmount)
                        return;

                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnExpire)]
        private void HandleTriggerSpellOnExpire(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.Real) ||
                apply ||
                aurApp.GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            aurApp.GetTarget().CastSpell(aurApp.GetTarget(), GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(this));
        }

        [AuraEffectHandler(AuraType.OpenStable)]
        private void HandleAuraOpenStable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player) ||
                !target.IsInWorld)
                return;

            if (apply)
                target.ToPlayer().GetSession().SendStablePet(target.GetGUID());

            // client auto close stable dialog at !apply aura
        }

        [AuraEffectHandler(AuraType.ModFakeInebriate)]
        private void HandleAuraModFakeInebriation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.InvisibilityDetect.AddFlag(InvisibilityType.Drunk);
                target.InvisibilityDetect.AddValue(InvisibilityType.Drunk, GetAmount());

                Player playerTarget = target.ToPlayer();

                if (playerTarget)
                    playerTarget.ApplyModFakeInebriation(GetAmount(), true);
            }
            else
            {
                bool removeDetect = !target.HasAuraType(AuraType.ModFakeInebriate);

                target.InvisibilityDetect.AddValue(InvisibilityType.Drunk, -GetAmount());

                Player playerTarget = target.ToPlayer();

                if (playerTarget != null)
                {
                    playerTarget.ApplyModFakeInebriation(GetAmount(), false);

                    if (removeDetect)
                        removeDetect = playerTarget.GetDrunkValue() == 0;
                }

                if (removeDetect)
                    target.InvisibilityDetect.DelFlag(InvisibilityType.Drunk);
            }

            // call functions which may have additional effects after changing State of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.OverrideSpells)]
        private void HandleAuraOverrideSpells(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null ||
                !target.IsInWorld)
                return;

            uint overrideId = (uint)GetMiscValue();

            if (apply)
            {
                target.SetOverrideSpellsId(overrideId);
                OverrideSpellDataRecord overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);

                if (overrideSpells != null)
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        uint spellId = overrideSpells.Spells[i];

                        if (spellId != 0)
                            target.AddTemporarySpell(spellId);
                    }
            }
            else
            {
                target.SetOverrideSpellsId(0);
                OverrideSpellDataRecord overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);

                if (overrideSpells != null)
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        uint spellId = overrideSpells.Spells[i];

                        if (spellId != 0)
                            target.RemoveTemporarySpell(spellId);
                    }
            }
        }

        [AuraEffectHandler(AuraType.SetVehicleId)]
        private void HandleAuraSetVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsInWorld)
                return;

            int vehicleId = GetMiscValue();

            if (apply)
            {
                if (!target.CreateVehicleKit((uint)vehicleId, 0))
                    return;
            }
            else if (target.GetVehicleKit() != null)
            {
                target.RemoveVehicleKit();
            }

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
                target.ToPlayer().SendOnCancelExpectedVehicleRideAura();
        }

        [AuraEffectHandler(AuraType.PreventResurrection)]
        private void HandlePreventResurrection(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
            else if (!target.GetMap().Instanceable())
                target.SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
        }

        [AuraEffectHandler(AuraType.Mastery)]
        private void HandleMastery(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            target.UpdateMastery();
        }

        private void HandlePeriodicTriggerSpellAuraTick(Unit target, Unit caster)
        {
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandlePeriodicTriggerSpellAuraTick: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");

                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? caster : target;

                if (triggerCaster != null)
                {
                    triggerCaster.CastSpell(target, triggerSpellId, new CastSpellExtraArgs(this));
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
            {
                Log.outError(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
            }
        }

        private void HandlePeriodicTriggerSpellWithValueAuraTick(Unit target, Unit caster)
        {
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandlePeriodicTriggerSpellWithValueAuraTick: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");

                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? caster : target;

                if (triggerCaster != null)
                {
                    CastSpellExtraArgs args = new(this);

                    for (int i = 0; i < SpellConst.MaxEffects; ++i)
                        args.AddSpellMod(SpellValueMod.BasePoint0 + i, GetAmount());

                    triggerCaster.CastSpell(target, triggerSpellId, args);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
            {
                Log.outError(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
            }
        }

        private void HandlePeriodicDamageAurasTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated) ||
                target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);

                return;
            }

            // Consecrate ticks can miss and will not show up in the combat log
            // dynobj Auras must always have a caster
            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            CleanDamage cleanDamage = new(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? GetBase().GetStackAmount() : 1u;

            // ignore non positive values (can be result apply spellmods to aura Damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
            Global.ScriptMgr.ForEach<IUnitModifyPeriodicDamageAurasTick>(p => p.ModifyPeriodicDamageAurasTick(target, caster, ref damage));

            switch (GetAuraType())
            {
                case AuraType.PeriodicDamage:
                    {
                        if (caster != null)
                            damage = caster.SpellDamageBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                        damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT);

                        // There is a Chance to make a Soul Shard when Drain soul does Damage
                        if (caster != null &&
                            GetSpellInfo().SpellFamilyName == SpellFamilyNames.Warlock &&
                            GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                        {
                            if (caster.IsTypeId(TypeId.Player) &&
                                caster.ToPlayer().IsHonorOrXPTarget(target))
                                caster.CastSpell(caster, 95810, new CastSpellExtraArgs(this));
                        }
                        else if (GetSpellInfo().SpellFamilyName == SpellFamilyNames.Generic)
                        {
                            switch (GetId())
                            {
                                case 70911: // Unbound Plague
                                case 72854: // Unbound Plague
                                case 72855: // Unbound Plague
                                case 72856: // Unbound Plague
                                    damage *= (uint)Math.Pow(1.25f, _ticksDone);

                                    break;
                                default:
                                    break;
                            }
                        }

                        break;
                    }
                case AuraType.PeriodicWeaponPercentDamage:
                    {
                        WeaponAttackType attackType = GetSpellInfo().GetAttackType();

                        damage = MathFunctions.CalculatePct(caster.CalculateDamage(attackType, false, true), GetAmount());

                        // Add melee Damage bonuses (also check for negative)
                        if (caster != null)
                            damage = caster.MeleeDamageBonusDone(target, damage, attackType, DamageEffectType.DOT, GetSpellInfo());

                        damage = target.MeleeDamageBonusTaken(caster, damage, attackType, DamageEffectType.DOT, GetSpellInfo());

                        break;
                    }
                case AuraType.PeriodicDamagePercent:
                    // ceil obtained value, it may happen that 10 ticks for 10% Damage may not kill owner
                    damage = (uint)Math.Ceiling(MathFunctions.CalculatePct((float)target.GetMaxHealth(), damage));
                    damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT);

                    break;
                default:
                    break;
            }

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

            if (crit)
                damage = Unit.SpellCriticalDamageBonus(caster, _spellInfo, damage, target);

            // Calculate armor mitigation
            if (Unit.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo()))
            {
                uint damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetAttackType(), GetBase().GetCasterLevel());
                cleanDamage.MitigatedDamage += damage - damageReducedArmor;
                damage = damageReducedArmor;
            }

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
                if (GetSpellEffectInfo().IsTargetingArea() ||
                    GetSpellEffectInfo().IsAreaAuraEffect() ||
                    GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) ||
                    GetSpellInfo().HasAttribute(SpellAttr5.TreatAsAreaEffect))
                    damage = (uint)target.CalculateAOEAvoidance((int)damage, (uint)_spellInfo.SchoolMask, GetBase().GetCastItemGUID());

            int dmg = (int)damage;

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) &&
                caster != null &&
                caster.CanApplyResilience())
                Unit.ApplyResilience(target, ref dmg);

            damage = (uint)dmg;

            DamageInfo damageInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            Unit.CalcAbsorbResist(damageInfo);
            damage = damageInfo.GetDamage();

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();
            Unit.DealDamageMods(caster, target, ref damage, ref absorb);

            // Set trigger flag
            ProcFlagsInit procAttacker = new(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim.Or(ProcFlags.TakeAnyDamage);
            }

            int overkill = (int)(damage - target.GetHealth());

            if (overkill < 0)
                overkill = 0;

            SpellPeriodicAuraLogInfo pInfo = new(this, damage, (uint)dmg, (uint)overkill, absorb, resist, 0.0f, crit);

            Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), true);

            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);
            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicHealthLeechAuraTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated) ||
                target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);

                return;
            }

            // dynobj Auras must always have a caster
            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            CleanDamage cleanDamage = new(0, 0, GetSpellInfo().GetAttackType(), MeleeHitOutcome.Normal);

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? GetBase().GetStackAmount() : 1u;

            // ignore negative values (can be result apply spellmods to aura Damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            if (caster)
                damage = caster.SpellDamageBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

            damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT);

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

            if (crit)
                damage = Unit.SpellCriticalDamageBonus(caster, _spellInfo, damage, target);

            // Calculate armor mitigation
            if (Unit.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo()))
            {
                uint damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetAttackType(), GetBase().GetCasterLevel());
                cleanDamage.MitigatedDamage += damage - damageReducedArmor;
                damage = damageReducedArmor;
            }

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
                if (GetSpellEffectInfo().IsTargetingArea() ||
                    GetSpellEffectInfo().IsAreaAuraEffect() ||
                    GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) ||
                    GetSpellInfo().HasAttribute(SpellAttr5.TreatAsAreaEffect))
                    damage = (uint)target.CalculateAOEAvoidance((int)damage, (uint)_spellInfo.SchoolMask, GetBase().GetCastItemGUID());

            int dmg = (int)damage;

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) &&
                caster != null &&
                caster.CanApplyResilience())
                Unit.ApplyResilience(target, ref dmg);

            damage = (uint)dmg;

            DamageInfo damageInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, GetSpellInfo().GetAttackType());
            Unit.CalcAbsorbResist(damageInfo);

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();

            // SendSpellNonMeleeDamageLog expects non-absorbed/non-resisted Damage
            SpellNonMeleeDamage log = new(caster, target, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().GetSchoolMask(), GetBase().GetCastId());
            log.Damage = damage;
            log.OriginalDamage = (uint)dmg;
            log.Absorb = absorb;
            log.Resist = resist;
            log.PeriodicLog = true;

            if (crit)
                log.HitInfo |= HitInfo.CriticalHit;

            // Set trigger flag
            ProcFlagsInit procAttacker = new(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim.Or(ProcFlags.TakeAnyDamage);
            }

            int new_damage = (int)Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), false);
            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);

            // process caster heal from now on (must be in world)
            if (!caster ||
                !caster.IsAlive())
                return;

            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            uint heal = caster.SpellHealingBonusDone(caster, GetSpellInfo(), (uint)(new_damage * gainMultiplier), DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);
            heal = caster.SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.DOT);

            HealInfo healInfo = new(caster, caster, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);

            caster.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());
            Unit.ProcSkillsAndAuras(caster, caster, new ProcFlagsInit(ProcFlags.DealHelpfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHelpfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);

            caster.SendSpellNonMeleeDamageLog(log);
        }

        private void HandlePeriodicHealthFunnelAuraTick(Unit target, Unit caster)
        {
            if (caster == null ||
                !caster.IsAlive() ||
                !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);

                return;
            }

            uint damage = (uint)Math.Max(GetAmount(), 0);

            // do not kill health donator
            if (caster.GetHealth() < damage)
                damage = (uint)caster.GetHealth() - 1;

            if (damage == 0)
                return;

            caster.ModifyHealth(-(int)damage);
            Log.outDebug(LogFilter.Spells, "PeriodicTick: donator {0} Target {1} Damage {2}.", caster.GetEntry(), target.GetEntry(), damage);

            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            damage = (uint)(damage * gainMultiplier);

            HealInfo healInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);
            Unit.ProcSkillsAndAuras(caster, target, new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, ProcFlagsHit.Normal, null, null, healInfo);
        }

        private void HandlePeriodicHealAurasTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);

                return;
            }

            // don't regen when permanent aura Target has full power
            if (GetBase().IsPermanent() &&
                target.IsFullHealth())
                return;

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? GetBase().GetStackAmount() : 1u;

            // ignore negative values (can be result apply spellmods to aura Damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            if (GetAuraType() == AuraType.ObsModHealth)
                damage = (uint)target.CountPctFromMaxHealth((int)damage);
            else if (caster != null)
                damage = caster.SpellHealingBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

            damage = target.SpellHealingBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT);

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

            if (crit)
                damage = (uint)Unit.SpellCriticalHealingBonus(caster, _spellInfo, (int)damage, target);

            Log.outDebug(LogFilter.Spells,
                         "PeriodicTick: {0} (TypeId: {1}) heal of {2} (TypeId: {3}) for {4} health inflicted by {5}",
                         GetCasterGUID().ToString(),
                         GetCaster().GetTypeId(),
                         target.GetGUID().ToString(),
                         target.GetTypeId(),
                         damage,
                         GetId());

            uint heal = damage;

            HealInfo healInfo = new(caster, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            Unit.CalcHealAbsorb(healInfo);
            Unit.DealHeal(healInfo);

            SpellPeriodicAuraLogInfo pInfo = new(this, heal, damage, heal - healInfo.GetEffectiveHeal(), healInfo.GetAbsorb(), 0, 0.0f, crit);
            target.SendPeriodicAuraLog(pInfo);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());

            // %-based heal - does not proc Auras
            if (GetAuraType() == AuraType.ObsModHealth)
                return;

            ProcFlagsInit procAttacker = new(ProcFlags.DealHelpfulPeriodic);
            ProcFlagsInit procVictim = new(ProcFlags.TakeHelpfulPeriodic);
            ProcFlagsHit hitMask = crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;

            // ignore Item heals
            if (GetBase().GetCastItemGUID().IsEmpty())
                Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);
        }

        private void HandlePeriodicManaLeechAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (caster == null ||
                !caster.IsAlive() ||
                !target.IsAlive() ||
                target.GetPowerType() != powerType)
                return;

            if (target.HasUnitState(UnitState.Isolated) ||
                target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);

                return;
            }

            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            // ignore negative values (can be result apply spellmods to aura Damage
            int drainAmount = Math.Max(GetAmount(), 0);

            int drainedAmount = -target.ModifyPower(powerType, -drainAmount);
            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)drainedAmount, (uint)drainAmount, 0, 0, 0, gainMultiplier, false);

            int gainAmount = (int)(drainedAmount * gainMultiplier);
            int gainedAmount = 0;

            if (gainAmount != 0)
            {
                gainedAmount = caster.ModifyPower(powerType, gainAmount);

                // energize is not modified by threat modifiers
                if (!GetSpellInfo().HasAttribute(SpellAttr4.NoHelpfulThreat))
                    target.GetThreatManager().AddThreat(caster, gainedAmount * 0.5f, GetSpellInfo(), true);
            }

            // Drain Mana
            if (caster.GetGuardianPet() != null &&
                _spellInfo.SpellFamilyName == SpellFamilyNames.Warlock &&
                _spellInfo.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00000010))
            {
                int manaFeedVal = 0;
                AuraEffect aurEff = GetBase().GetEffect(1);

                if (aurEff != null)
                    manaFeedVal = aurEff.GetAmount();

                if (manaFeedVal > 0)
                {
                    int feedAmount = MathFunctions.CalculatePct(gainedAmount, manaFeedVal);

                    CastSpellExtraArgs args = new(this);
                    args.AddSpellMod(SpellValueMod.BasePoint0, feedAmount);
                    caster.CastSpell(caster, 32554, args);
                }
            }

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandleObsModPowerAuraTick(Unit target, Unit caster)
        {
            PowerType powerType;

            if (GetMiscValue() == (int)PowerType.All)
                powerType = target.GetPowerType();
            else
                powerType = (PowerType)GetMiscValue();

            if (!target.IsAlive() ||
                target.GetMaxPower(powerType) == 0)
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);

                return;
            }

            // don't regen when permanent aura Target has full power
            if (GetBase().IsPermanent() &&
                target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura Damage
            int amount = Math.Max(GetAmount(), 0) * target.GetMaxPower(powerType) / 100;

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);

            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicEnergizeAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (!target.IsAlive() ||
                target.GetMaxPower(powerType) == 0)
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);

                return;
            }

            // don't regen when permanent aura Target has full power
            if (GetBase().IsPermanent() &&
                target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura Damage
            int amount = Math.Max(GetAmount(), 0);

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);
            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicPowerBurnAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (caster == null ||
                !target.IsAlive() ||
                target.GetPowerType() != powerType)
                return;

            if (target.HasUnitState(UnitState.Isolated) ||
                target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);

                return;
            }

            // ignore negative values (can be result apply spellmods to aura Damage
            int damage = Math.Max(GetAmount(), 0);

            uint gain = (uint)-target.ModifyPower(powerType, -damage);

            float dmgMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellInfo spellProto = GetSpellInfo();
            // maybe has to be sent different to client, but not by SMSG_PERIODICAURALOG
            SpellNonMeleeDamage damageInfo = new(caster, target, spellProto, GetBase().GetSpellVisual(), spellProto.SchoolMask, GetBase().GetCastId());
            damageInfo.PeriodicLog = true;
            // no SpellDamageBonus for burn mana
            caster.CalculateSpellDamageTaken(damageInfo, (int)(gain * dmgMultiplier), spellProto);

            Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);

            // Set trigger flag
            ProcFlagsInit procAttacker = new(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = Unit.CreateProcHitMask(damageInfo, SpellMissInfo.None);
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.NoDmgHeal;

            if (damageInfo.Damage != 0)
            {
                procVictim.Or(ProcFlags.TakeAnyDamage);
                spellTypeMask |= ProcFlagsSpellType.Damage;
            }

            caster.DealSpellDamage(damageInfo, true);

            DamageInfo dotDamageInfo = new(damageInfo, DamageEffectType.DOT, WeaponAttackType.BaseAttack, hitMask);
            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, spellTypeMask, ProcFlagsSpellPhase.Hit, hitMask, null, dotDamageInfo, null);

            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        private bool CanPeriodicTickCrit()
        {
            if (GetSpellInfo().HasAttribute(SpellAttr2.CantCrit))
                return false;

            return true;
        }

        private float CalcPeriodicCritChance(Unit caster)
        {
            if (!caster ||
                !CanPeriodicTickCrit())
                return 0.0f;

            Player modOwner = caster.GetSpellModOwner();

            if (!modOwner)
                return 0.0f;

            float critChance = modOwner.SpellCritChanceDone(null, this, GetSpellInfo().GetSchoolMask(), GetSpellInfo().GetAttackType());

            return Math.Max(0.0f, critChance);
        }

        private void HandleBreakableCCAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            int damageLeft = (int)(GetAmount() - eventInfo.GetDamageInfo().GetDamage());

            if (damageLeft <= 0)
                aurApp.GetTarget().RemoveAura(aurApp);
            else
                ChangeAmount(damageLeft);
        }

        private void HandleProcTriggerSpellAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit triggerCaster = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");

                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo != null)
            {
                Log.outDebug(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Triggering spell {triggeredSpellInfo.Id} from aura {GetId()} proc");
                triggerCaster.CastSpell(triggerTarget, triggeredSpellInfo.Id, new CastSpellExtraArgs(this).SetTriggeringSpell(eventInfo.GetProcSpell()));
            }
            else if (triggerSpellId != 0 &&
                     GetAuraType() != AuraType.Dummy)
            {
                Log.outError(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Spell {GetId()} has non-existent spell {triggerSpellId} in EffectTriggered[{GetEffIndex()}] and is therefore not triggered.");
            }
        }

        private void HandleProcTriggerSpellWithValueAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit triggerCaster = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");

                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo != null)
            {
                CastSpellExtraArgs args = new(this);
                args.SetTriggeringSpell(eventInfo.GetProcSpell());
                args.AddSpellMod(SpellValueMod.BasePoint0, GetAmount());
                triggerCaster.CastSpell(triggerTarget, triggerSpellId, args);
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Triggering spell {0} with value {1} from aura {2} proc", triggeredSpellInfo.Id, GetAmount(), GetId());
            }
            else
            {
                Log.outError(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Spell {GetId()} has non-existent spell {triggerSpellId} in EffectTriggered[{GetEffIndex()}] and is therefore not triggered.");
            }
        }

        private void HandleProcTriggerDamageAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit target = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();

            if (triggerTarget.HasUnitState(UnitState.Isolated) ||
                triggerTarget.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(triggerTarget, target);

                return;
            }

            SpellNonMeleeDamage damageInfo = new(target, triggerTarget, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().SchoolMask, GetBase().GetCastId());
            int damage = (int)target.SpellDamageBonusDone(triggerTarget, GetSpellInfo(), (uint)GetAmount(), DamageEffectType.SpellDirect, GetSpellEffectInfo());
            damage = (int)triggerTarget.SpellDamageBonusTaken(target, GetSpellInfo(), (uint)damage, DamageEffectType.SpellDirect);
            target.CalculateSpellDamageTaken(damageInfo, damage, GetSpellInfo());
            Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);
            target.DealSpellDamage(damageInfo, true);
            target.SendSpellNonMeleeDamageLog(damageInfo);
        }

        [AuraEffectHandler(AuraType.ForceWeather)]
        private void HandleAuraForceWeather(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SendPacket(new WeatherPkt((WeatherState)GetMiscValue(), 1.0f));
            else
                target.GetMap().SendZoneWeather(target.GetZoneId(), target);
        }

        [AuraEffectHandler(AuraType.EnableAltPower)]
        private void HandleEnableAltPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            int altPowerId = GetMiscValue();
            UnitPowerBarRecord powerEntry = CliDB.UnitPowerBarStorage.LookupByKey(altPowerId);

            if (powerEntry == null)
                return;

            if (apply)
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, (int)powerEntry.MaxPower);
            else
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, 0);
        }

        [AuraEffectHandler(AuraType.ModSpellCategoryCooldown)]
        private void HandleModSpellCategoryCooldown(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (player)
                player.SendSpellCategoryCooldowns();
        }

        [AuraEffectHandler(AuraType.ShowConfirmationPrompt)]
        [AuraEffectHandler(AuraType.ShowConfirmationPromptWithDifficulty)]
        private void HandleShowConfirmationPrompt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (!player)
                return;

            if (apply)
                player.AddTemporarySpell(_effectInfo.TriggerSpell);
            else
                player.RemoveTemporarySpell(_effectInfo.TriggerSpell);
        }

        [AuraEffectHandler(AuraType.OverridePetSpecs)]
        private void HandleOverridePetSpecs(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (!player)
                return;

            if (player.GetClass() != Class.Hunter)
                return;

            Pet pet = player.GetPet();

            if (!pet)
                return;

            ChrSpecializationRecord currSpec = CliDB.ChrSpecializationStorage.LookupByKey(pet.GetSpecialization());

            if (currSpec == null)
                return;

            pet.SetSpecialization(Global.DB2Mgr.GetChrSpecializationByIndex(apply ? Class.Max : 0, currSpec.OrderIndex).Id);
        }

        [AuraEffectHandler(AuraType.AllowUsingGameobjectsWhileMounted)]
        private void HandleAllowUsingGameobjectsWhileMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SetPlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
            else if (!target.HasAuraType(AuraType.AllowUsingGameobjectsWhileMounted))
                target.RemovePlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
        }

        [AuraEffectHandler(AuraType.PlayScene)]
        private void HandlePlayScene(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();

            if (!player)
                return;

            if (apply)
                player.GetSceneMgr().PlayScene((uint)GetMiscValue());
            else
                player.GetSceneMgr().CancelSceneBySceneId((uint)GetMiscValue());
        }

        [AuraEffectHandler(AuraType.AreaTrigger)]
        private void HandleCreateAreaTrigger(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                AreaTrigger.CreateAreaTrigger((uint)GetMiscValue(), GetCaster(), target, GetSpellInfo(), target, GetBase().GetDuration(), GetBase().GetSpellVisual(), ObjectGuid.Empty, this);
            }
            else
            {
                Unit caster = GetCaster();

                if (caster)
                    caster.RemoveAreaTrigger(this);
            }
        }

        [AuraEffectHandler(AuraType.PvpTalents)]
        private void HandleAuraPvpTalents(AuraApplication auraApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = auraApp.GetTarget().ToPlayer();

            if (target)
            {
                if (apply)
                    target.TogglePvpTalents(true);
                else if (!target.HasAuraType(AuraType.PvpTalents))
                    target.TogglePvpTalents(false);
            }
        }

        [AuraEffectHandler(AuraType.LinkedSummon)]
        private void HandleLinkedSummon(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(GetSpellEffectInfo().TriggerSpell, GetBase().GetCastDifficulty());

            if (triggerSpellInfo == null)
                return;

            // on apply cast summon spell
            if (apply)
            {
                CastSpellExtraArgs args = new(this);
                args.CastDifficulty = triggerSpellInfo.Difficulty;
                target.CastSpell(target, triggerSpellInfo.Id, args);
            }
            // on unapply we need to search for and remove the summoned creature
            else
            {
                List<uint> summonedEntries = new();

                foreach (var spellEffectInfo in triggerSpellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.Summon))
                    {
                        uint summonEntry = (uint)spellEffectInfo.MiscValue;

                        if (summonEntry != 0)
                            summonedEntries.Add(summonEntry);
                    }

                // we don't know if there can be multiple summons for the same effect, so consider only 1 summon for each effect
                // most of the spells have multiple effects with the same summon spell Id for multiple spawns, so right now it's safe to assume there's only 1 spawn per effect
                foreach (uint summonEntry in summonedEntries)
                {
                    List<Creature> nearbyEntries = target.GetCreatureListWithEntryInGrid(summonEntry);

                    foreach (var creature in nearbyEntries)
                        if (creature.GetOwner() == target)
                        {
                            creature.DespawnOrUnsummon();

                            break;
                        }
                        else
                        {
                            TempSummon tempSummon = creature.ToTempSummon();

                            if (tempSummon)
                                if (tempSummon.GetSummoner() == target)
                                {
                                    tempSummon.DespawnOrUnsummon();

                                    break;
                                }
                        }
                }
            }
        }

        [AuraEffectHandler(AuraType.SetFFAPvp)]
        private void HandleSetFFAPvP(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            target.UpdatePvPState(true);
        }

        [AuraEffectHandler(AuraType.ModOverrideZonePvpType)]
        private void HandleModOverrideZonePVPType(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SetOverrideZonePVPType((ZonePVPTypeOverride)GetMiscValue());
            else if (target.HasAuraType(AuraType.ModOverrideZonePvpType))
                target.SetOverrideZonePVPType((ZonePVPTypeOverride)target.GetAuraEffectsByType(AuraType.ModOverrideZonePvpType).Last().GetMiscValue());
            else
                target.SetOverrideZonePVPType(ZonePVPTypeOverride.None);

            target.UpdateHostileAreaState(CliDB.AreaTableStorage.LookupByKey(target.GetZoneId()));
            target.UpdatePvPState();
        }

        [AuraEffectHandler(AuraType.BattleGroundPlayerPositionFactional)]
        [AuraEffectHandler(AuraType.BattleGroundPlayerPosition)]
        private void HandleBattlegroundPlayerPosition(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            BattlegroundMap battlegroundMap = target.GetMap().ToBattlegroundMap();

            if (battlegroundMap == null)
                return;

            Battleground bg = battlegroundMap.GetBG();

            if (bg == null)
                return;

            if (apply)
            {
                BattlegroundPlayerPosition playerPosition = new();
                playerPosition.Guid = target.GetGUID();
                playerPosition.ArenaSlot = (sbyte)GetMiscValue();
                playerPosition.Pos = target.GetPosition();

                if (GetAuraType() == AuraType.BattleGroundPlayerPositionFactional)
                    playerPosition.IconID = target.GetEffectiveTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconHordeFlag : BattlegroundConst.PlayerPositionIconAllianceFlag;
                else if (GetAuraType() == AuraType.BattleGroundPlayerPosition)
                    playerPosition.IconID = target.GetEffectiveTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconAllianceFlag : BattlegroundConst.PlayerPositionIconHordeFlag;
                else
                    Log.outWarn(LogFilter.Spells, $"Unknown aura effect {GetAuraType()} handled by HandleBattlegroundPlayerPosition.");

                bg.AddPlayerPosition(playerPosition);
            }
            else
            {
                bg.RemovePlayerPosition(target.GetGUID());
            }
        }

        [AuraEffectHandler(AuraType.StoreTeleportReturnPoint)]
        private void HandleStoreTeleportReturnPoint(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player playerTarget = aurApp.GetTarget().ToPlayer();

            if (playerTarget == null)
                return;

            if (apply)
                playerTarget.AddStoredAuraTeleportLocation(GetSpellInfo().Id);
            else if (!playerTarget.GetSession().IsLogingOut())
                playerTarget.RemoveStoredAuraTeleportLocation(GetSpellInfo().Id);
        }

        [AuraEffectHandler(AuraType.MountRestrictions)]
        private void HandleMountRestrictions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            aurApp.GetTarget().UpdateMountCapability();
        }

        [AuraEffectHandler(AuraType.CosmeticMounted)]
        private void HandleCosmeticMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (apply)
                aurApp.GetTarget().SetCosmeticMountDisplayId((uint)GetMiscValue());
            else
                aurApp.GetTarget().SetCosmeticMountDisplayId(0); // set cosmetic Mount to 0, even if multiple Auras are active; tested with zandalari racial + divine steed

            Player playerTarget = aurApp.GetTarget().ToPlayer();

            if (playerTarget == null)
                return;

            playerTarget.SendMovementSetCollisionHeight(playerTarget.GetCollisionHeight(), UpdateCollisionHeightReason.Force);
        }

        [AuraEffectHandler(AuraType.SuppressItemPassiveEffectBySpellLabel)]
        private void HandleSuppressItemPassiveEffectBySpellLabel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            List<Aura> suppressedAuras = new();

            foreach (var appliedAura in aurApp.GetTarget().GetOwnedAuras())
                if (appliedAura.Value.GetSpellInfo().HasLabel((uint)GetMiscValue()))
                    suppressedAuras.Add(appliedAura.Value);

            // Refresh applications
            foreach (Aura aura in suppressedAuras)
                aura.ApplyForTargets();
        }

        [AuraEffectHandler(AuraType.ForceBeathBar)]
        private void HandleForceBreathBar(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player playerTarget = aurApp.GetTarget().ToPlayer();

            if (playerTarget == null)
                return;

            playerTarget.UpdatePositionData();
        }
    }
}