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

        /**********************/
        /***   UNIT MODEL   ***/
        /**********************/
        [AuraEffectHandler(AuraType.ModShapeshift)]
        private void HandleAuraModShapeshift(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
                return;

            SpellShapeshiftFormRecord shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetMiscValue());
            //ASSERT(shapeInfo, "Spell {0} uses unknown ShapeshiftForm (%u).", GetId(), GetMiscValue());

            Unit target = aurApp.GetTarget();
            ShapeShiftForm form = (ShapeShiftForm)GetMiscValue();
            uint modelid = target.GetModelForForm(form, GetId());

            if (apply)
            {
                // remove polymorph before changing display Id to keep new display Id
                switch (form)
                {
                    case ShapeShiftForm.CatForm:
                    case ShapeShiftForm.TreeOfLife:
                    case ShapeShiftForm.TravelForm:
                    case ShapeShiftForm.AquaticForm:
                    case ShapeShiftForm.BearForm:
                    case ShapeShiftForm.FlightFormEpic:
                    case ShapeShiftForm.FlightForm:
                    case ShapeShiftForm.MoonkinForm:
                        {
                            // remove movement affects
                            target.RemoveAurasByShapeShift();

                            // and polymorphic affects
                            if (target.IsPolymorphed())
                                target.RemoveAurasDueToSpell(target.GetTransformSpell());

                            break;
                        }
                    default:
                        break;
                }

                // remove other shapeshift before applying a new one
                target.RemoveAurasByType(AuraType.ModShapeshift, ObjectGuid.Empty, GetBase());

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;

                ShapeShiftForm prevForm = target.GetShapeshiftForm();
                target.SetShapeshiftForm(form);

                // add the shapeshift aura's boosts
                if (prevForm != form)
                    HandleShapeshiftBoosts(target, true);

                if (modelid > 0)
                {
                    SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransformSpell(), GetBase().GetCastDifficulty());

                    if (transformSpellInfo == null ||
                        !GetSpellInfo().IsPositive())
                        target.SetDisplayId(modelid);
                }

                if (!shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Shapeshifting, GetSpellInfo());
            }
            else
            {
                // reset model Id if no other Auras present
                // may happen when aura is applied on linked event on aura removal
                if (!target.HasAuraType(AuraType.ModShapeshift))
                {
                    target.SetShapeshiftForm(ShapeShiftForm.None);

                    if (target.GetClass() == Class.Druid)
                        // Remove movement impairing effects also when shifting out
                        target.RemoveAurasByShapeShift();
                }

                if (modelid > 0)
                    target.RestoreDisplayId(target.IsMounted());

                switch (form)
                {
                    // Nordrassil Harness - bonus
                    case ShapeShiftForm.BearForm:
                    case ShapeShiftForm.CatForm:
                        AuraEffect dummy = target.GetAuraEffect(37315, 0);

                        if (dummy != null)
                            target.CastSpell(target, 37316, new CastSpellExtraArgs(dummy));

                        break;
                    // Nordrassil Regalia - bonus
                    case ShapeShiftForm.MoonkinForm:
                        dummy = target.GetAuraEffect(37324, 0);

                        if (dummy != null)
                            target.CastSpell(target, 37325, new CastSpellExtraArgs(dummy));

                        break;
                    default:
                        break;
                }

                // remove the shapeshift aura's boosts
                HandleShapeshiftBoosts(target, apply);
            }

            Player playerTarget = target.ToPlayer();

            if (playerTarget != null)
            {
                playerTarget.SendMovementSetCollisionHeight(playerTarget.GetCollisionHeight(), UpdateCollisionHeightReason.Force);
                playerTarget.InitDataForForm();
            }
            else
            {
                target.UpdateDisplayPower();
            }

            if (target.GetClass() == Class.Druid)
            {
                // Dash
                AuraEffect aurEff = target.GetAuraEffect(AuraType.ModIncreaseSpeed, SpellFamilyNames.Druid, new FlagArray128(0, 0, 0x8));

                aurEff?.RecalculateAmount();

                // Disarm handling
                // If druid shifts while being disarmed we need to deal with that since forms aren't affected by disarm
                // and also HandleAuraModDisarm is not triggered
                if (!target.CanUseAttackType(WeaponAttackType.BaseAttack))
                {
                    Item pItem = target.ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                    if (pItem != null)
                        target.ToPlayer()._ApplyWeaponDamage(EquipmentSlot.MainHand, pItem, apply);
                }
            }

            // stop handling the effect if it was removed by linked event
            if (apply && aurApp.HasRemoveMode())
                return;

            if (target.IsTypeId(TypeId.Player))
                // Learn spells for shapeshift form - no need to send Action bars or add spells to spellbook
                for (byte i = 0; i < SpellConst.MaxShapeshift; ++i)
                {
                    if (shapeInfo.PresetSpellID[i] == 0)
                        continue;

                    if (apply)
                        target.ToPlayer().AddTemporarySpell(shapeInfo.PresetSpellID[i]);
                    else
                        target.ToPlayer().RemoveTemporarySpell(shapeInfo.PresetSpellID[i]);
                }
        }

        [AuraEffectHandler(AuraType.Transform)]
        private void HandleAuraTransform(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                // update active transform spell only when transform not set or not overwriting negative by positive case
                SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransformSpell(), GetBase().GetCastDifficulty());

                if (transformSpellInfo == null ||
                    !GetSpellInfo().IsPositive() ||
                    transformSpellInfo.IsPositive())
                {
                    target.SetTransformSpell(GetId());

                    // special case (spell specific functionality)
                    if (GetMiscValue() == 0)
                    {
                        bool isFemale = target.GetNativeGender() == Gender.Female;

                        switch (GetId())
                        {
                            // Orb of Deception
                            case 16739:
                                {
                                    if (!target.IsTypeId(TypeId.Player))
                                        return;

                                    switch (target.GetRace())
                                    {
                                        // Blood Elf
                                        case Race.BloodElf:
                                            target.SetDisplayId(isFemale ? 17830 : 17829u);

                                            break;
                                        // Orc
                                        case Race.Orc:
                                            target.SetDisplayId(isFemale ? 10140 : 10139u);

                                            break;
                                        // Troll
                                        case Race.Troll:
                                            target.SetDisplayId(isFemale ? 10134 : 10135u);

                                            break;
                                        // Tauren
                                        case Race.Tauren:
                                            target.SetDisplayId(isFemale ? 10147 : 10136u);

                                            break;
                                        // Undead
                                        case Race.Undead:
                                            target.SetDisplayId(isFemale ? 10145 : 10146u);

                                            break;
                                        // Draenei
                                        case Race.Draenei:
                                            target.SetDisplayId(isFemale ? 17828 : 17827u);

                                            break;
                                        // Dwarf
                                        case Race.Dwarf:
                                            target.SetDisplayId(isFemale ? 10142 : 10141u);

                                            break;
                                        // Gnome
                                        case Race.Gnome:
                                            target.SetDisplayId(isFemale ? 10149 : 10148u);

                                            break;
                                        // Human
                                        case Race.Human:
                                            target.SetDisplayId(isFemale ? 10138 : 10137u);

                                            break;
                                        // Night Elf
                                        case Race.NightElf:
                                            target.SetDisplayId(isFemale ? 10144 : 10143u);

                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                }
                            // Murloc costume
                            case 42365:
                                target.SetDisplayId(21723);

                                break;
                            // Dread Corsair
                            case 50517:
                            // Corsair Costume
                            case 51926:
                                {
                                    if (!target.IsTypeId(TypeId.Player))
                                        return;

                                    switch (target.GetRace())
                                    {
                                        // Blood Elf
                                        case Race.BloodElf:
                                            target.SetDisplayId(isFemale ? 25043 : 25032u);

                                            break;
                                        // Orc
                                        case Race.Orc:
                                            target.SetDisplayId(isFemale ? 25050 : 25039u);

                                            break;
                                        // Troll
                                        case Race.Troll:
                                            target.SetDisplayId(isFemale ? 25052 : 25041u);

                                            break;
                                        // Tauren
                                        case Race.Tauren:
                                            target.SetDisplayId(isFemale ? 25051 : 25040u);

                                            break;
                                        // Undead
                                        case Race.Undead:
                                            target.SetDisplayId(isFemale ? 25053 : 25042u);

                                            break;
                                        // Draenei
                                        case Race.Draenei:
                                            target.SetDisplayId(isFemale ? 25044 : 25033u);

                                            break;
                                        // Dwarf
                                        case Race.Dwarf:
                                            target.SetDisplayId(isFemale ? 25045 : 25034u);

                                            break;
                                        // Gnome
                                        case Race.Gnome:
                                            target.SetDisplayId(isFemale ? 25035 : 25046u);

                                            break;
                                        // Human
                                        case Race.Human:
                                            target.SetDisplayId(isFemale ? 25037 : 25048u);

                                            break;
                                        // Night Elf
                                        case Race.NightElf:
                                            target.SetDisplayId(isFemale ? 25038 : 25049u);

                                            break;
                                        default:
                                            break;
                                    }

                                    break;
                                }
                            // Pygmy Oil
                            case 53806:
                                target.SetDisplayId(22512);

                                break;
                            // Honor the Dead
                            case 65386:
                            case 65495:
                                target.SetDisplayId(isFemale ? 29204 : 29203u);

                                break;
                            // Darkspear Pride
                            case 75532:
                                target.SetDisplayId(isFemale ? 31738 : 31737u);

                                break;
                            // Gnomeregan Pride
                            case 75531:
                                target.SetDisplayId(isFemale ? 31655 : 31654u);

                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)GetMiscValue());

                        if (ci == null)
                        {
                            target.SetDisplayId(16358); // pig pink ^_^
                            Log.outError(LogFilter.Spells, "Auras: unknown creature Id = {0} (only need its modelid) From Spell Aura Transform in Spell ID = {1}", GetMiscValue(), GetId());
                        }
                        else
                        {
                            uint model_id = 0;
                            uint modelid = ObjectManager.ChooseDisplayId(ci).CreatureDisplayID;

                            if (modelid != 0)
                                model_id = modelid; // Will use the default model here

                            target.SetDisplayId(model_id);

                            // Dragonmaw Illusion (set Mount model also)
                            if (GetId() == 42016 &&
                                target.GetMountDisplayId() != 0 &&
                                !target.GetAuraEffectsByType(AuraType.ModIncreaseMountedFlightSpeed).Empty())
                                target.SetMountDisplayId(16314);
                        }
                    }
                }

                // polymorph case
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real) &&
                    target.IsTypeId(TypeId.Player) &&
                    target.IsPolymorphed())
                {
                    // for players, start regeneration after 1s (in polymorph fast regeneration case)
                    // only if caster is Player (after patch 2.4.2)
                    if (GetCasterGUID().IsPlayer())
                        target.ToPlayer().SetRegenTimerCount(1 * Time.InMilliseconds);

                    //dismount polymorphed Target (after patch 2.4.2)
                    if (target.IsMounted())
                        target.RemoveAurasByType(AuraType.Mounted);
                }
            }
            else
            {
                if (target.GetTransformSpell() == GetId())
                    target.SetTransformSpell(0);

                target.RestoreDisplayId(target.IsMounted());

                // Dragonmaw Illusion (restore Mount model)
                if (GetId() == 42016 &&
                    target.GetMountDisplayId() == 16314)
                    if (!target.GetAuraEffectsByType(AuraType.Mounted).Empty())
                    {
                        int cr_id = target.GetAuraEffectsByType(AuraType.Mounted)[0].GetMiscValue();
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)cr_id);

                        if (ci != null)
                        {
                            CreatureModel model = ObjectManager.ChooseDisplayId(ci);
                            Global.ObjectMgr.GetCreatureModelRandomGender(ref model, ci);

                            target.SetMountDisplayId(model.CreatureDisplayID);
                        }
                    }
            }
        }

        [AuraEffectHandler(AuraType.ModScale)]
        [AuraEffectHandler(AuraType.ModScale2)]
        private void HandleAuraModScale(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            aurApp.GetTarget().RecalculateObjectScale();
        }

        [AuraEffectHandler(AuraType.CloneCaster)]
        private void HandleAuraCloneCaster(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                Unit caster = GetCaster();

                if (caster == null ||
                    caster == target)
                    return;

                // What must be cloned? at least display and scale
                target.SetDisplayId(caster.GetDisplayId());
                //Target.SetObjectScale(caster.GetFloatValue(OBJECT_FIELD_SCALE_X)); // we need retail info about how scaling is handled (aura maybe?)
                target.SetUnitFlag2(UnitFlags2.MirrorImage);
            }
            else
            {
                target.SetDisplayId(target.GetNativeDisplayId());
                target.RemoveUnitFlag2(UnitFlags2.MirrorImage);
            }
        }

    }
}