// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Events
{
    struct SpellIds
    {
        // Pilgrims Bounty Buff Food
        public const uint WellFedApTrigger = 65414;
        public const uint WellFedZmTrigger = 65412;
        public const uint WellFedHitTrigger = 65416;
        public const uint WellFedHasteTrigger = 65410;
        public const uint WellFedSpiritTrigger = 65415;

        // FeastSpells
        public const uint FeastOnTurkey = 61784;
        public const uint FeastOnCranberries = 61785;
        public const uint FeastOnSweetPotatoes = 61786;
        public const uint FeastOnPie = 61787;
        public const uint FeastOnStuffing = 61788;
        public const uint CranberryHelpins = 61841;
        public const uint TurkeyHelpins = 61842;
        public const uint StuffingHelpins = 61843;
        public const uint SweetPotatoHelpins = 61844;
        public const uint PieHelpins = 61845;
        public const uint OnPlateEatVisual = 61826;

        // TheTurkinator
        public const uint KillCounterVisual = 62015;
        public const uint KillCounterVisualMax = 62021;

        // SpiritOfSharing
        public const uint TheSpiritOfSharing = 61849;

        // BountifulTableMisc
        public const uint OnPlateTurkey = 61928;
        public const uint OnPlateCranberries = 61925;
        public const uint OnPlateStuffing = 61927;
        public const uint OnPlateSweetPotatoes = 61929;
        public const uint OnPlatePie = 61926;
        public const uint PassTheTurkey = 66373;
        public const uint PassTheCranberries = 66372;
        public const uint PassTheStuffing = 66375;
        public const uint PassTheSweetPotatoes = 66376;
        public const uint PassThePie = 66374;
        public const uint OnPlateVisualPie = 61825;
        public const uint OnPlateVisualCranberries = 61821;
        public const uint OnPlateVisualPotatoes = 61824;
        public const uint OnPlateVisualTurkey = 61822;
        public const uint OnPlateVisualStuffing = 61823;
        public const uint AServingOfCranberriesPlate = 61833;
        public const uint AServingOfTurkeyPlate = 61835;
        public const uint AServingOfStuffingPlate = 61836;
        public const uint AServingOfSweetPotatoesPlate = 61837;
        public const uint AServingOfPiePlate = 61838;
        public const uint AServingOfCranberriesChair = 61804;
        public const uint AServingOfTurkeyChair = 61807;
        public const uint AServingOfStuffingChair = 61806;
        public const uint AServingOfSweetPotatoesChair = 61808;
        public const uint AServingOfPieChair = 61805;
    }

    struct Misc
    {
        // TheTurkinator
        public const uint EmoteTurkeyHunter = 0;
        public const uint EmoteTurkeyDomination = 1;
        public const uint EmoteTurkeySlaughter = 2;
        public const uint EmoteTurkeyTriumph = 3;

        // BountifulTableMisc
        public const sbyte SeatPlayer = 0;
        public const sbyte SeatPlateHolder = 6;
        public const uint NpcBountifulTable = 32823;

    }

    [Script("spell_gen_slow_roasted_turkey", SpellIds.WellFedApTrigger)]
    [Script("spell_gen_cranberry_chutney", SpellIds.WellFedZmTrigger)]
    [Script("spell_gen_spice_bread_stuffing", SpellIds.WellFedHitTrigger)]
    [Script("spell_gen_pumpkin_pie", SpellIds.WellFedSpiritTrigger)]
    [Script("spell_gen_candied_sweet_potato", SpellIds.WellFedHasteTrigger)]
    class spell_pilgrims_bounty_buff_food : AuraScript
    {
        uint _triggeredSpellId;

        public spell_pilgrims_bounty_buff_food(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
        }

        void HandleTriggerSpell(AuraEffect aurEff)
        {
            PreventDefaultAction();
            if (_handled)
                return;

            _handled = true;
            GetTarget().CastSpell(GetTarget(), _triggeredSpellId, true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleTriggerSpell, 2, AuraType.PeriodicTriggerSpell));
        }

        bool _handled;
    }

    [Script]
    class spell_pilgrims_bounty_feast_on : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellEffect(((uint)spellInfo.GetEffect(0).CalcValue(), 0));
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();

            uint spellId = GetSpellInfo().Id switch
            {
                SpellIds.FeastOnTurkey => SpellIds.TurkeyHelpins,
                SpellIds.FeastOnCranberries => SpellIds.CranberryHelpins,
                SpellIds.FeastOnSweetPotatoes => SpellIds.SweetPotatoHelpins,
                SpellIds.FeastOnPie => SpellIds.PieHelpins,
                SpellIds.FeastOnStuffing => SpellIds.StuffingHelpins,
                _ => 0
            };

            if (spellId == 0)
                return;

            Vehicle vehicle = caster.GetVehicleKit();
            if (vehicle != null)
            {
                Unit target = vehicle.GetPassenger(0);
                if (target != null)
                {
                    Player player = target.ToPlayer();
                    if (player != null)
                    {
                        player.CastSpell(player, SpellIds.OnPlateEatVisual, true);
                        caster.CastSpell(player, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                            .SetOriginalCaster(player.GetGUID()));
                    }
                }
            }

            Aura aura = caster.GetAura((uint)GetEffectValue());
            if (aura != null)
            {
                if (aura.GetStackAmount() == 1)
                    caster.RemoveAurasDueToSpell((uint)aura.GetSpellInfo().GetEffect(0).CalcValue());
                aura.ModStackAmount(-1);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script] // 62014 - Turkey Tracker
    class spell_pilgrims_bounty_turkey_tracker : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.KillCounterVisual, SpellIds.KillCounterVisualMax);
        }

        void HandleScript(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();
            Unit target = GetHitUnit();

            if (target == null || caster == null)
                return;

            if (target.HasAura(SpellIds.KillCounterVisualMax))
                return;

            Aura aura = target.GetAura(GetSpellInfo().Id);
            if (aura != null)
            {
                switch (aura.GetStackAmount())
                {
                    case 10:
                        caster.GetAI().Talk(Misc.EmoteTurkeyHunter, target);
                        break;
                    case 20:
                        caster.GetAI().Talk(Misc.EmoteTurkeyDomination, target);
                        break;
                    case 30:
                        caster.GetAI().Talk(Misc.EmoteTurkeySlaughter, target);
                        break;
                    case 40:
                        caster.GetAI().Talk(Misc.EmoteTurkeyTriumph, target);
                        target.CastSpell(target, SpellIds.KillCounterVisualMax, true);
                        target.RemoveAurasDueToSpell(GetSpellInfo().Id);
                        break;
                    default:
                        return;
                }
                target.CastSpell(target, SpellIds.KillCounterVisual, true);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_pilgrims_bounty_well_fed_turkey", SpellIds.WellFedApTrigger)]
    [Script("spell_pilgrims_bounty_well_fed_cranberry", SpellIds.WellFedZmTrigger)]
    [Script("spell_pilgrims_bounty_well_fed_stuffing", SpellIds.WellFedHitTrigger)]
    [Script("spell_pilgrims_bounty_well_fed_sweet_potatoes", SpellIds.WellFedHasteTrigger)]
    [Script("spell_pilgrims_bounty_well_fed_pie", SpellIds.WellFedSpiritTrigger)]
    class spell_pilgrims_bounty_well_fed : SpellScript
    {
        uint _triggeredSpellId;

        public spell_pilgrims_bounty_well_fed(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
        }


        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_triggeredSpellId);
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player target = GetHitPlayer();
            if (target == null)
                return;

            Aura aura = target.GetAura(GetSpellInfo().Id);
            if (aura != null)
            {
                if (aura.GetStackAmount() == 5)
                    target.CastSpell(target, _triggeredSpellId, true);
            }

            Aura turkey = target.GetAura(SpellIds.TurkeyHelpins);
            Aura cranberies = target.GetAura(SpellIds.CranberryHelpins);
            Aura stuffing = target.GetAura(SpellIds.StuffingHelpins);
            Aura sweetPotatoes = target.GetAura(SpellIds.SweetPotatoHelpins);
            Aura pie = target.GetAura(SpellIds.PieHelpins);

            if ((turkey != null && turkey.GetStackAmount() == 5) && (cranberies != null && cranberies.GetStackAmount() == 5) && (stuffing != null && stuffing.GetStackAmount() == 5)
                && (sweetPotatoes != null && sweetPotatoes.GetStackAmount() == 5) && (pie != null && pie.GetStackAmount() == 5))
            {
                target.CastSpell(target, SpellIds.TheSpiritOfSharing, true);
                target.RemoveAurasDueToSpell(SpellIds.TurkeyHelpins);
                target.RemoveAurasDueToSpell(SpellIds.CranberryHelpins);
                target.RemoveAurasDueToSpell(SpellIds.StuffingHelpins);
                target.RemoveAurasDueToSpell(SpellIds.SweetPotatoHelpins);
                target.RemoveAurasDueToSpell(SpellIds.PieHelpins);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScript, 1, SpellEffectName.ScriptEffect));
        }
    }

    [Script("spell_pilgrims_bounty_on_plate_turkey", SpellIds.OnPlateTurkey, SpellIds.PassTheTurkey, SpellIds.OnPlateVisualTurkey, SpellIds.AServingOfTurkeyChair)]
    [Script("spell_pilgrims_bounty_on_plate_cranberries", SpellIds.OnPlateCranberries, SpellIds.PassTheCranberries, SpellIds.OnPlateVisualCranberries, SpellIds.AServingOfCranberriesChair)]
    [Script("spell_pilgrims_bounty_on_plate_stuffing", SpellIds.OnPlateStuffing, SpellIds.PassTheStuffing, SpellIds.OnPlateVisualStuffing, SpellIds.AServingOfStuffingChair)]
    [Script("spell_pilgrims_bounty_on_plate_sweet_potatoes", SpellIds.OnPlateSweetPotatoes, SpellIds.PassTheSweetPotatoes, SpellIds.OnPlateVisualPotatoes, SpellIds.AServingOfSweetPotatoesChair)]
    [Script("spell_pilgrims_bounty_on_plate_pie", SpellIds.OnPlatePie, SpellIds.PassThePie, SpellIds.OnPlateVisualPie, SpellIds.AServingOfPieChair)]
    class spell_pilgrims_bounty_on_plate : SpellScript
    {
        uint _triggeredSpellId1;
        uint _triggeredSpellId2;
        uint _triggeredSpellId3;
        uint _triggeredSpellId4;

        public spell_pilgrims_bounty_on_plate(uint triggeredSpellId1, uint triggeredSpellId2, uint triggeredSpellId3, uint triggeredSpellId4)
        {
            _triggeredSpellId1 = triggeredSpellId1;
            _triggeredSpellId2 = triggeredSpellId2;
            _triggeredSpellId3 = triggeredSpellId3;
            _triggeredSpellId4 = triggeredSpellId4;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_triggeredSpellId1, _triggeredSpellId2, _triggeredSpellId3, _triggeredSpellId4);
        }

        Vehicle GetTable(Unit target)
        {
            if (target.IsPlayer())
            {
                Unit vehBase = target.GetVehicleBase();
                if (vehBase != null)
                {
                    Vehicle table = vehBase.GetVehicle();
                    if (table != null)
                        if (table.GetCreatureEntry() == Misc.NpcBountifulTable)
                            return table;
                }
            }
            else
            {
                Vehicle veh = target.GetVehicle();
                if (veh != null)
                    if (veh.GetCreatureEntry() == Misc.NpcBountifulTable)
                        return veh;
            }

            return null;
        }

        Unit GetPlateInSeat(Vehicle table, sbyte seat)
        {
            Unit holderUnit = table.GetPassenger(Misc.SeatPlateHolder);
            if (holderUnit != null)
            {
                Vehicle holder = holderUnit.GetVehicleKit();
                if (holder != null)
                {
                    Unit plate = holder.GetPassenger(seat);
                    if (plate != null)
                        return plate;
                }
            }

            return null;
        }

        void HandleDummy(uint effIndex)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null || caster == target)
                return;

            Vehicle table = GetTable(caster);
            if (table == null || table != GetTable(target))
                return;

            Vehicle casterChair = caster.GetVehicleKit();
            if (casterChair != null)
            {
                Unit casterPlr = casterChair.GetPassenger(Misc.SeatPlayer);
                if (casterPlr != null)
                {
                    if (casterPlr == target)
                        return;

                    casterPlr.CastSpell(casterPlr, _triggeredSpellId2, true); //Credit for Sharing is Caring(always)

                    sbyte seat = target.GetTransSeat();
                    if (target.IsPlayer() && target.GetVehicleBase() != null)
                        seat = target.GetVehicleBase().GetTransSeat();

                    Unit plate = GetPlateInSeat(table, seat);
                    if (plate != null)
                    {
                        if (target.IsPlayer()) //Food Fight case
                        {
                            casterPlr.CastSpell(target, _triggeredSpellId1, true);
                            caster.CastSpell(target.GetVehicleBase(), _triggeredSpellId4, true); //CanEat-chair(always)
                        }
                        else
                        {
                            casterPlr.CastSpell(plate, _triggeredSpellId3, true); //Food Visual on plate
                            caster.CastSpell(target, _triggeredSpellId4, true); //CanEat-chair(always)
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script("spell_pilgrims_bounty_a_serving_of_cranberries", SpellIds.AServingOfCranberriesPlate)]
    [Script("spell_pilgrims_bounty_a_serving_of_turkey", SpellIds.AServingOfTurkeyPlate)]
    [Script("spell_pilgrims_bounty_a_serving_of_stuffing", SpellIds.AServingOfStuffingPlate)]
    [Script("spell_pilgrims_bounty_a_serving_of_potatoes", SpellIds.AServingOfSweetPotatoesPlate)]
    [Script("spell_pilgrims_bounty_a_serving_of_pie", SpellIds.AServingOfPiePlate)]
    class spell_pilgrims_bounty_a_serving_of : AuraScript
    {
        uint _triggeredSpellId;

        public spell_pilgrims_bounty_a_serving_of(uint triggeredSpellId)
        {
            _triggeredSpellId = triggeredSpellId;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_triggeredSpellId);
        }

        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, (uint)aurEff.GetAmount(), true);
            HandlePlate(target, true);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.RemoveAurasDueToSpell((uint)aurEff.GetAmount());
            HandlePlate(target, false);
        }

        void HandlePlate(Unit target, bool apply)
        {
            Vehicle table = target.GetVehicle();
            if (table != null)
            {
                Unit holderUnit = table.GetPassenger(Misc.SeatPlateHolder);
                if (holderUnit != null)
                {
                    Vehicle holder = holderUnit.GetVehicleKit();
                    if (holder != null)
                    {
                        Unit plate = holder.GetPassenger(target.GetTransSeat());
                        if (plate != null)
                        {
                            if (apply)
                                target.CastSpell(plate, _triggeredSpellId, true);
                            else
                                plate.RemoveAurasDueToSpell(_triggeredSpellId);
                        }
                    }
                }
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }
}