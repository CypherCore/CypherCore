/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.AI
{
    public struct Spells
    {
        /* Generic */
        public const uint AutoShot = 75;
        public const uint Shoot = 3018;
        public const uint Throw = 2764;
        public const uint Wand = 5019;

        /* Warrior - Generic */
        public const uint BattleStance = 2457;
        public const uint BerserkerStance = 2458;
        public const uint DefensiveStance = 71;
        public const uint Charge = 11578;
        public const uint Intercept = 20252;
        public const uint EnragedRegen = 55694;
        public const uint IntimidatingShout = 5246;
        public const uint Pummel = 6552;
        public const uint ShieldBash = 72;
        public const uint Bloodrage = 2687;

        /* Warrior - Arms */
        public const uint SweepingStrikes = 12328;
        public const uint MortalStrike = 12294;
        public const uint Bladestorm = 46924;
        public const uint Rend = 47465;
        public const uint Retaliation = 20230;
        public const uint ShatteringThrow = 64382;
        public const uint ThunderClap = 47502;

        /* Warrior - Fury */
        public const uint DeathWish = 12292;
        public const uint Bloodthirst = 23881;
        public const uint PassiveTitansGrip = 46917;
        public const uint DemoShout = 47437;
        public const uint Execute = 47471;
        public const uint HeroicFury = 60970;
        public const uint Recklessness = 1719;
        public const uint PiercingHowl = 12323;

        /* Warrior - Protection */
        public const uint Vigilance = 50720;
        public const uint Devastate = 20243;
        public const uint Shockwave = 46968;
        public const uint ConcussionBlow = 12809;
        public const uint Disarm = 676;
        public const uint LastStand = 12975;
        public const uint ShieldBlock = 2565;
        public const uint ShieldSlam = 47488;
        public const uint ShieldWall = 871;
        public const uint Reflection = 23920;

        /* Paladin - Generic */
        public const uint PalAuraMastery = 31821;
        public const uint LayOnHands = 48788;
        public const uint BlessingOfMight = 48932;
        public const uint AvengingWrath = 31884;
        public const uint DivineProtection = 498;
        public const uint DivineShield = 642;
        public const uint HammerOfJustice = 10308;
        public const uint HandOfFreedom = 1044;
        public const uint HandOfProtection = 10278;
        public const uint HandOfSacrifice = 6940;

        /* Paladin - Holy*/
        public const uint PassiveIllumination = 20215;
        public const uint HolyShock = 20473;
        public const uint BeaconOfLight = 53563;
        public const uint Consecration = 48819;
        public const uint FlashOfLight = 48785;
        public const uint HolyLight = 48782;
        public const uint DivineFavor = 20216;
        public const uint DivineIllumination = 31842;

        /* Paladin - Protection */
        public const uint BlessOfSanc = 20911;
        public const uint HolyShield = 20925;
        public const uint AvengersShield = 48827;
        public const uint DivineSacrifice = 64205;
        public const uint HammerOfRighteous = 53595;
        public const uint RighteousFury = 25780;
        public const uint ShieldOfRighteous = 61411;

        /* Paladin - Retribution */
        public const uint SealOfCommand = 20375;
        public const uint CrusaderStrike = 35395;
        public const uint DivineStorm = 53385;
        public const uint Judgement = 20271;
        public const uint HammerOfWrath = 48806;

        /* Hunter - Generic */
        public const uint Deterrence = 19263;
        public const uint ExplosiveTrap = 49067;
        public const uint FreezingArrow = 60192;
        public const uint RapidFire = 3045;
        public const uint KillShot = 61006;
        public const uint MultiShot = 49048;
        public const uint ViperSting = 3034;

        /* Hunter - Beast Mastery */
        public const uint BestialWrath = 19574;
        public const uint PassiveBeastWithin = 34692;
        public const uint PassiveBeastMastery = 53270;

        /* Hunter - Marksmanship */
        public const uint AimedShot = 19434;
        public const uint PassiveTrueshotAura = 19506;
        public const uint ChimeraShot = 53209;
        public const uint ArcaneShot = 49045;
        public const uint SteadyShot = 49052;
        public const uint Readiness = 23989;
        public const uint SilencingShot = 34490;

        /* Hunter - Survival */
        public const uint PassiveLockAndLoad = 56344;
        public const uint WyvernSting = 19386;
        public const uint ExplosiveShot = 53301;
        public const uint BlackArrow = 3674;

        /* Rogue - Generic */
        public const uint Dismantle = 51722;
        public const uint Evasion = 26669;
        public const uint Kick = 1766;
        public const uint Vanish = 26889;
        public const uint Blind = 2094;
        public const uint CloakOfShadows = 31224;

        /* Rogue - Assassination */
        public const uint ColdBlood = 14177;
        public const uint Mutilate = 1329;
        public const uint HungerForBlood = 51662;
        public const uint Envenom = 57993;

        /* Rogue - Combat */
        public const uint SinisterStrike = 48637;
        public const uint BladeFlurry = 13877;
        public const uint AdrenalineRush = 13750;
        public const uint KillingSpree = 51690;
        public const uint Eviscerate = 48668;

        /* Rogue - Sublety */
        public const uint Hemorrhage = 16511;
        public const uint Premeditation = 14183;
        public const uint ShadowDance = 51713;
        public const uint Preparation = 14185;
        public const uint Shadowstep = 36554;

        /* Priest - Generic */
        public const uint FearWard = 6346;
        public const uint PowerWordFort = 48161;
        public const uint DivineSpirit = 48073;
        public const uint ShadowProtection = 48169;
        public const uint DivineHymn = 64843;
        public const uint HymnOfHope = 64901;
        public const uint ShadowWordDeath = 48158;
        public const uint PsychicScream = 10890;

        /* Priest - Discipline */
        public const uint PassiveSoulWarding = 63574;
        public const uint PowerInfusion = 10060;
        public const uint Penance = 47540;
        public const uint PainSuppression = 33206;
        public const uint InnerFocus = 14751;
        public const uint PowerWordShield = 48066;

        /* Priest - Holy */
        public const uint PassiveSpiritRedemption = 20711;
        public const uint DesperatePrayer = 19236;
        public const uint GuardianSpirit = 47788;
        public const uint FlashHeal = 48071;
        public const uint Renew = 48068;

        /* Priest - Shadow */
        public const uint VampiricEmbrace = 15286;
        public const uint Shadowform = 15473;
        public const uint VampiricTouch = 34914;
        public const uint MindFlay = 15407;
        public const uint MindBlast = 48127;
        public const uint ShadowWordPain = 48125;
        public const uint DevouringPlague = 48300;
        public const uint Dispersion = 47585;

        /* Death Knight - Generic */
        public const uint DeathGrip = 49576;
        public const uint Strangulate = 47476;
        public const uint EmpowerRuneWeap = 47568;
        public const uint IcebornFortitude = 48792;
        public const uint AntiMagicShell = 48707;
        public const uint DeathCoilDk = 49895;
        public const uint MindFreeze = 47528;
        public const uint IcyTouch = 49909;
        public const uint AuraFrostFever = 55095;
        public const uint PlagueStrike = 49921;
        public const uint AuraBloodPlague = 55078;
        public const uint Pestilence = 50842;

        /* Death Knight - Blood */
        public const uint RuneTap = 48982;
        public const uint Hysteria = 49016;
        public const uint HeartStrike = 55050;
        public const uint DeathStrike = 49924;
        public const uint BloodStrike = 49930;
        public const uint MarkOfBlood = 49005;
        public const uint VampiricBlood = 55233;

        /* Death Knight - Frost */
        public const uint PassiveIcyTalons = 50887;
        public const uint FrostStrike = 49143;
        public const uint HowlingBlast = 49184;
        public const uint UnbreakableArmor = 51271;
        public const uint Obliterate = 51425;
        public const uint Deathchill = 49796;

        /* Death Knight - Unholy */
        public const uint PassiveUnholyBlight = 49194;
        public const uint PassiveMasterOfGhoul = 52143;
        public const uint ScourgeStrike = 55090;
        public const uint DeathAndDecay = 49938;
        public const uint AntiMagicZone = 51052;
        public const uint SummonGargoyle = 49206;

        /* Shaman - Generic */
        public const uint Heroism = 32182;
        public const uint Bloodlust = 2825;
        public const uint GroundingTotem = 8177;

        /* Shaman - Elemental*/
        public const uint PassiveElementalFocus = 16164;
        public const uint TotemOfWrath = 30706;
        public const uint Thunderstorm = 51490;
        public const uint LightningBolt = 49238;
        public const uint EarthShock = 49231;
        public const uint FlameShock = 49233;
        public const uint LavaBurst = 60043;
        public const uint ChainLightning = 49271;
        public const uint ElementalMastery = 16166;

        /* Shaman - Enhancement */
        public const uint PassiveSpiritWeapons = 16268;
        public const uint LavaLash = 60103;
        public const uint FeralSpirit = 51533;
        public const uint AuraMaelstromWeapon = 53817;
        public const uint Stormstrike = 17364;
        public const uint ShamanisticRage = 30823;

        /* Shaman - Restoration*/
        public const uint ShaNatureSwift = 591;
        public const uint ManaTideTotem = 590;
        public const uint EarthShield = 49284;
        public const uint Riptide = 61295;
        public const uint HealingWave = 49273;
        public const uint LesserHealWave = 49276;
        public const uint TidalForce = 55198;

        /* Mage - Generic */
        public const uint DampenMagic = 43015;
        public const uint Evocation = 12051;
        public const uint ManaShield = 43020;
        public const uint MirrorImage = 55342;
        public const uint Spellsteal = 30449;
        public const uint Counterspell = 2139;
        public const uint IceBlock = 45438;

        /* Mage - Arcane */
        public const uint FocusMagic = 54646;
        public const uint ArcanePower = 12042;
        public const uint ArcaneBarrage = 44425;
        public const uint ArcaneBlast = 42897;
        public const uint AuraArcaneBlast = 36032;
        public const uint ArcaneMissiles = 42846;
        public const uint PresenceOfMind = 12043;

        /* Mage - Fire */
        public const uint Pyroblast = 11366;
        public const uint Combustion = 11129;
        public const uint LivingBomb = 44457;
        public const uint Fireball = 42833;
        public const uint FireBlast = 42873;
        public const uint DragonsBreath = 31661;
        public const uint BlastWave = 11113;

        /* Mage - Frost */
        public const uint IcyVeins = 12472;
        public const uint IceBarrier = 11426;
        public const uint DeepFreeze = 44572;
        public const uint FrostNova = 42917;
        public const uint Frostbolt = 42842;
        public const uint ColdSnap = 11958;
        public const uint IceLance = 42914;

        /* Warlock - Generic */
        public const uint Fear = 6215;
        public const uint HowlOfTerror = 17928;
        public const uint Corruption = 47813;
        public const uint DeathCoilW = 47860;
        public const uint ShadowBolt = 47809;
        public const uint Incinerate = 47838;
        public const uint Immolate = 47811;
        public const uint SeedOfCorruption = 47836;

        /* Warlock - Affliction */
        public const uint PassiveSiphonLife = 63108;
        public const uint UnstableAffliction = 30108;
        public const uint Haunt = 48181;
        public const uint CurseOfAgony = 47864;
        public const uint DrainSoul = 47855;

        /* Warlock - Demonology */
        public const uint SoulLink = 19028;
        public const uint DemonicEmpowerment = 47193;
        public const uint Metamorphosis = 59672;
        public const uint ImmolationAura = 50589;
        public const uint DemonCharge = 54785;
        public const uint AuraDecimation = 63167;
        public const uint AuraMoltenCore = 71165;
        public const uint SoulFire = 47825;

        /* Warlock - Destruction */
        public const uint Shadowburn = 17877;
        public const uint Conflagrate = 17962;
        public const uint ChaosBolt = 50796;
        public const uint Shadowfury = 47847;

        /* Druid - Generic */
        public const uint Barkskin = 22812;
        public const uint Innervate = 29166;

        /* Druid - Balance */
        public const uint InsectSwarm = 5570;
        public const uint MoonkinForm = 24858;
        public const uint Starfall = 48505;
        public const uint Typhoon = 61384;
        public const uint AuraEclipseLunar = 48518;
        public const uint Moonfire = 48463;
        public const uint Starfire = 48465;
        public const uint Wrath = 48461;

        /* Druid - Feral */
        public const uint CatForm = 768;
        public const uint SurvivalInstincts = 61336;
        public const uint Mangle = 33917;
        public const uint Berserk = 50334;
        public const uint MangleCat = 48566;
        public const uint FeralChargeCat = 49376;
        public const uint Rake = 48574;
        public const uint Rip = 49800;
        public const uint SavageRoar = 52610;
        public const uint TigerFury = 50213;
        public const uint Claw = 48570;
        public const uint Dash = 33357;
        public const uint Maim = 49802;

        /* Druid - Restoration */
        public const uint Swiftmend = 18562;
        public const uint TreeOfLife = 33891;
        public const uint WildGrowth = 48438;
        public const uint NatureSwiftness = 17116;
        public const uint Tranquility = 48447;
        public const uint Nourish = 50464;
        public const uint HealingTouch = 48378;
        public const uint Rejuvenation = 48441;
        public const uint Regrowth = 48443;
        public const uint Lifebloom = 48451;
    }

    public class PlayerAI : UnitAI
    {
        public PlayerAI(Player player) : base(player)
        {
            me = player;
            _selfSpec = player.GetUInt32Value(PlayerFields.CurrentSpecId);
            _isSelfHealer = IsPlayerHealer(player);
            _isSelfRangedAttacker = IsPlayerRangedAttacker(player);
        }

        bool IsPlayerHealer(Player who)
        {
            if (!who)
                return false;

            switch (who.GetClass())
            {
                case Class.Warrior:
                case Class.Hunter:
                case Class.Rogue:
                case Class.Deathknight:
                case Class.Mage:
                case Class.Warlock:
                case Class.DemonHunter:
                default:
                    return false;
                case Class.Paladin:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.PaladinHoly;
                case Class.Priest:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.PriestDiscipline || who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.PriestHoly;
                case Class.Shaman:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.ShamanRestoration;
                case Class.Monk:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.MonkMistweaver;
                case Class.Druid:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.DruidRestoration;
            }
        }
        bool IsPlayerRangedAttacker(Player who)
        {
            if (!who)
                return false;

            switch (who.GetClass())
            {
                case Class.Warrior:
                case Class.Paladin:
                case Class.Rogue:
                case Class.Deathknight:
                default:
                    return false;
                case Class.Mage:
                case Class.Warlock:
                    return true;
                case Class.Hunter:
                    {
                        // check if we have a ranged weapon equipped
                        Item rangedSlot = who.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.Ranged);

                        ItemTemplate rangedTemplate = rangedSlot ? rangedSlot.GetTemplate() : null;
                        if (rangedTemplate != null)
                            if (Convert.ToBoolean((1 << (int)rangedTemplate.GetSubClass()) & (int)ItemSubClassWeapon.MaskRanged))
                                return true;

                        return false;
                    }
                case Class.Priest:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.PriestShadow;
                case Class.Shaman:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.ShamanElemental;
                case Class.Druid:
                    return who.GetUInt32Value(PlayerFields.CurrentSpecId) == (uint)TalentSpecialization.DruidBalance;
            }
        }

        Tuple<Spell, Unit> VerifySpellCast(uint spellId, Unit target)
        {
            // Find highest spell rank that we know
            uint knownRank, nextRank;
            if (me.HasSpell(spellId))
            {
                // this will save us some lookups if the player has the highest rank (expected case)
                knownRank = spellId;
                nextRank = Global.SpellMgr.GetNextSpellInChain(spellId);
            }
            else
            {
                knownRank = 0;
                nextRank = Global.SpellMgr.GetFirstSpellInChain(spellId);
            }

            while (nextRank != 0 && me.HasSpell(nextRank))
            {
                knownRank = nextRank;
                nextRank = Global.SpellMgr.GetNextSpellInChain(knownRank);
            }

            if (knownRank == 0)
                return null;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(knownRank);
            if (spellInfo == null)
                return null;

            if (me.GetSpellHistory().HasGlobalCooldown(spellInfo))
                return null;

            Spell spell = new Spell(me, spellInfo, TriggerCastFlags.None);
            if (spell.CanAutoCast(target))
                return Tuple.Create(spell, target);

            return null;
        }

        Tuple<Spell, Unit> VerifySpellCast(uint spellId, SpellTarget target)
        {
            Unit pTarget = null;
            switch (target)
            {
                case SpellTarget.None:
                    break;
                case SpellTarget.Victim:
                    pTarget = me.GetVictim();
                    if (!pTarget)
                        return null;
                    break;
                case SpellTarget.Charmer:
                    pTarget = me.GetCharmer();
                    if (!pTarget)
                        return null;
                    break;
                case SpellTarget.Self:
                    pTarget = me;
                    break;
            }

            return VerifySpellCast(spellId, pTarget);
        }

        public Tuple<Spell, Unit> SelectSpellCast(List<Tuple<Tuple<Spell, Unit>, uint>> spells)
        {
            if (spells.Empty())
                return null;

            uint totalWeights = 0;
            foreach (var wSpell in spells)
                totalWeights += wSpell.Item2;

            Tuple<Spell, Unit> selected = null;
            uint randNum = RandomHelper.URand(0, totalWeights - 1);
            foreach (var wSpell in spells)
            {
                if (selected != null)
                {
                    //delete wSpell.first.first;
                    continue;
                }

                if (randNum < wSpell.Item2)
                    selected = wSpell.Item1;
                else
                {
                    randNum -= wSpell.Item2;
                    //delete wSpell.first.first;
                }
            }

            spells.Clear();
            return selected;
        }

        void VerifyAndPushSpellCast<T>(List<Tuple<Tuple<Spell, Unit>, uint>> spells, uint spellId, T target, uint weight) where T : Unit
        {
            Tuple<Spell, Unit> spell = VerifySpellCast(spellId, target);
            if (spell != null)
                spells.Add(Tuple.Create(spell, weight));
        }

        public void DoCastAtTarget(Tuple<Spell, Unit> spell)
        {
            SpellCastTargets targets = new SpellCastTargets();
            targets.SetUnitTarget(spell.Item2);
            spell.Item1.prepare(targets);
        }

        void DoRangedAttackIfReady()
        {
            if (me.HasUnitState(UnitState.Casting))
                return;

            if (!me.isAttackReady(WeaponAttackType.RangedAttack))
                return;

            Unit victim = me.GetVictim();
            if (!victim)
                return;

            uint rangedAttackSpell = 0;

            Item rangedItem = me.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.Ranged);
            ItemTemplate rangedTemplate = rangedItem ? rangedItem.GetTemplate() : null;
            if (rangedTemplate != null)
            {
                switch ((ItemSubClassWeapon)rangedTemplate.GetSubClass())
                {
                    case ItemSubClassWeapon.Bow:
                    case ItemSubClassWeapon.Gun:
                    case ItemSubClassWeapon.Crossbow:
                        rangedAttackSpell = Spells.Shoot;
                        break;
                    case ItemSubClassWeapon.Thrown:
                        rangedAttackSpell = Spells.Throw;
                        break;
                    case ItemSubClassWeapon.Wand:
                        rangedAttackSpell = Spells.Wand;
                        break;
                }
            }

            if (rangedAttackSpell == 0)
                return;

            me.CastSpell(victim, rangedAttackSpell, TriggerCastFlags.CastDirectly);
            me.resetAttackTimer(WeaponAttackType.RangedAttack);
        }

        public void DoAutoAttackIfReady()
        {
            if (IsRangedAttacker())
                DoRangedAttackIfReady();
            else
                DoMeleeAttackIfReady();
        }

        void CancelAllShapeshifts()
        {
            List<AuraEffect> shapeshiftAuras = me.GetAuraEffectsByType(AuraType.ModShapeshift);
            List<Aura> removableShapeshifts = new List<Aura>();
            foreach (AuraEffect auraEff in shapeshiftAuras)
            {
                Aura aura = auraEff.GetBase();
                if (aura == null)
                    continue;
                SpellInfo auraInfo = aura.GetSpellInfo();
                if (auraInfo == null)
                    continue;
                if (auraInfo.HasAttribute(SpellAttr0.CantCancel))
                    continue;
                if (!auraInfo.IsPositive() || auraInfo.IsPassive())
                    continue;
                removableShapeshifts.Add(aura);
            }

            foreach (Aura aura in removableShapeshifts)
                me.RemoveOwnedAura(aura, AuraRemoveMode.Cancel);
        }

        public Creature GetCharmer()
        {
            if (me.GetCharmerGUID().IsCreature())
                return ObjectAccessor.GetCreature(me, me.GetCharmerGUID());
            return null;
        }

        public override void OnCharmed(bool apply) { }

        // helper functions to determine player info
        bool IsHealer(Player who = null)
        {
            return (!who || who == me) ? _isSelfHealer : IsPlayerHealer(who);
        }
        public bool IsRangedAttacker(Player who = null) { return (!who || who == me) ? _isSelfRangedAttacker : IsPlayerRangedAttacker(who); }
        uint GetSpec(Player who = null) { return (!who || who == me) ? _selfSpec : who.GetUInt32Value(PlayerFields.CurrentSpecId); }
        void SetIsRangedAttacker(bool state) { _isSelfRangedAttacker = state; } // this allows overriding of the default ranged attacker detection

        public virtual Unit SelectAttackTarget() { return me.GetCharmer() ? me.GetCharmer().GetVictim() : null; }

        protected new Player me;
        uint _selfSpec;
        bool _isSelfHealer;
        bool _isSelfRangedAttacker;

        enum SpellTarget
        {
            None,
            Victim,
            Charmer,
            Self
        }
    }

    class SimpleCharmedPlayerAI : PlayerAI
    {
        public SimpleCharmedPlayerAI(Player player) : base(player)
        {
            _castCheckTimer = 500;
            _chaseCloser = false;
            _forceFacing = true;
        }

        public override Unit SelectAttackTarget()
        {
            Unit charmer = me.GetCharmer();
            if (charmer)
                return charmer.IsAIEnabled ? charmer.GetAI().SelectTarget(SelectAggroTarget.Random, 0, new UncontrolledTargetSelectPredicate()) : charmer.GetVictim();
            return null;
        }

        Tuple<Spell, Unit> SelectAppropriateCastForSpec()
        {
            List<Tuple<Tuple<Spell, Unit>, uint>> spells = new List<Tuple<Tuple<Spell, Unit>, uint>>();
            /*
            switch (me.getClass())
            {
                case CLASS_WARRIOR:
                    if (!me.IsWithinMeleeRange(me.GetVictim()))
                    {
                        VerifyAndPushSpellCast(spells, SPELL_CHARGE, TARGET_VICTIM, 15);
                        VerifyAndPushSpellCast(spells, SPELL_INTERCEPT, TARGET_VICTIM, 10);
                    }
                    VerifyAndPushSpellCast(spells, SPELL_ENRAGED_REGEN, TARGET_NONE, 3);
                    VerifyAndPushSpellCast(spells, SPELL_INTIMIDATING_SHOUT, TARGET_VICTIM, 4);
                    if (me.GetVictim() && me.GetVictim().HasUnitState(UNIT_STATE_CASTING))
                    {
                        VerifyAndPushSpellCast(spells, SPELL_PUMMEL, TARGET_VICTIM, 15);
                        VerifyAndPushSpellCast(spells, SPELL_SHIELD_BASH, TARGET_VICTIM, 15);
                    }
                    VerifyAndPushSpellCast(spells, SPELL_BLOODRAGE, TARGET_NONE, 5);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_WARRIOR_PROTECTION:
                            VerifyAndPushSpellCast(spells, SPELL_SHOCKWAVE, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_CONCUSSION_BLOW, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_DISARM, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_LAST_STAND, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_SHIELD_BLOCK, TARGET_NONE, 1);
                            VerifyAndPushSpellCast(spells, SPELL_SHIELD_SLAM, TARGET_VICTIM, 4);
                            VerifyAndPushSpellCast(spells, SPELL_SHIELD_WALL, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_SPELL_REFLECTION, TARGET_NONE, 3);
                            VerifyAndPushSpellCast(spells, SPELL_DEVASTATE, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_REND, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_THUNDER_CLAP, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_DEMO_SHOUT, TARGET_VICTIM, 1);
                            break;
                        case TALENT_SPEC_WARRIOR_ARMS:
                            VerifyAndPushSpellCast(spells, SPELL_SWEEPING_STRIKES, TARGET_NONE, 2);
                            VerifyAndPushSpellCast(spells, SPELL_MORTAL_STRIKE, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_BLADESTORM, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_REND, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_RETALIATION, TARGET_NONE, 3);
                            VerifyAndPushSpellCast(spells, SPELL_SHATTERING_THROW, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_SWEEPING_STRIKES, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_THUNDER_CLAP, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_EXECUTE, TARGET_VICTIM, 15);
                            break;
                        case TALENT_SPEC_WARRIOR_FURY:
                            VerifyAndPushSpellCast(spells, SPELL_DEATH_WISH, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_BLOODTHIRST, TARGET_VICTIM, 4);
                            VerifyAndPushSpellCast(spells, SPELL_DEMO_SHOUT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_EXECUTE, TARGET_VICTIM, 15);
                            VerifyAndPushSpellCast(spells, SPELL_HEROIC_FURY, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_RECKLESSNESS, TARGET_NONE, 8);
                            VerifyAndPushSpellCast(spells, SPELL_PIERCING_HOWL, TARGET_VICTIM, 2);
                            break;
                    }
                    break;
                case CLASS_PALADIN:
                    VerifyAndPushSpellCast(spells, SPELL_AURA_MASTERY, TARGET_NONE, 3);
                    VerifyAndPushSpellCast(spells, SPELL_LAY_ON_HANDS, TARGET_CHARMER, 8);
                    VerifyAndPushSpellCast(spells, SPELL_BLESSING_OF_MIGHT, TARGET_CHARMER, 8);
                    VerifyAndPushSpellCast(spells, SPELL_AVENGING_WRATH, TARGET_NONE, 5);
                    VerifyAndPushSpellCast(spells, SPELL_DIVINE_PROTECTION, TARGET_NONE, 4);
                    VerifyAndPushSpellCast(spells, SPELL_DIVINE_SHIELD, TARGET_NONE, 2);
                    VerifyAndPushSpellCast(spells, SPELL_HAMMER_OF_JUSTICE, TARGET_VICTIM, 6);
                    VerifyAndPushSpellCast(spells, SPELL_HAND_OF_FREEDOM, TARGET_SELF, 3);
                    VerifyAndPushSpellCast(spells, SPELL_HAND_OF_PROTECTION, TARGET_SELF, 1);
                    if (Creature* creatureCharmer = GetCharmer())
                    {
                        if (creatureCharmer.IsDungeonBoss() || creatureCharmer.isWorldBoss())
                            VerifyAndPushSpellCast(spells, SPELL_HAND_OF_SACRIFICE, creatureCharmer, 10);
                        else
                            VerifyAndPushSpellCast(spells, SPELL_HAND_OF_PROTECTION, creatureCharmer, 3);
                    }

                    switch (GetSpec())
                    {
                        case TALENT_SPEC_PALADIN_PROTECTION:
                            VerifyAndPushSpellCast(spells, SPELL_HAMMER_OF_RIGHTEOUS, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_DIVINE_SACRIFICE, TARGET_NONE, 2);
                            VerifyAndPushSpellCast(spells, SPELL_SHIELD_OF_RIGHTEOUS, TARGET_VICTIM, 4);
                            VerifyAndPushSpellCast(spells, SPELL_JUDGEMENT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_CONSECRATION, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_HOLY_SHIELD, TARGET_NONE, 1);
                            break;
                        case TALENT_SPEC_PALADIN_HOLY:
                            VerifyAndPushSpellCast(spells, SPELL_HOLY_SHOCK, TARGET_CHARMER, 3);
                            VerifyAndPushSpellCast(spells, SPELL_HOLY_SHOCK, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_FLASH_OF_LIGHT, TARGET_CHARMER, 4);
                            VerifyAndPushSpellCast(spells, SPELL_HOLY_LIGHT, TARGET_CHARMER, 3);
                            VerifyAndPushSpellCast(spells, SPELL_DIVINE_FAVOR, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_DIVINE_ILLUMINATION, TARGET_NONE, 3);
                            break;
                        case TALENT_SPEC_PALADIN_RETRIBUTION:
                            VerifyAndPushSpellCast(spells, SPELL_CRUSADER_STRIKE, TARGET_VICTIM, 4);
                            VerifyAndPushSpellCast(spells, SPELL_DIVINE_STORM, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_JUDGEMENT, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_HAMMER_OF_WRATH, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_RIGHTEOUS_FURY, TARGET_NONE, 2);
                            break;
                    }
                    break;
                case CLASS_HUNTER:
                    VerifyAndPushSpellCast(spells, SPELL_DETERRENCE, TARGET_NONE, 3);
                    VerifyAndPushSpellCast(spells, SPELL_EXPLOSIVE_TRAP, TARGET_NONE, 1);
                    VerifyAndPushSpellCast(spells, SPELL_FREEZING_ARROW, TARGET_VICTIM, 2);
                    VerifyAndPushSpellCast(spells, SPELL_RAPID_FIRE, TARGET_NONE, 10);
                    VerifyAndPushSpellCast(spells, SPELL_KILL_SHOT, TARGET_VICTIM, 10);
                    if (me.GetVictim() && me.GetVictim().getPowerType() == POWER_MANA && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_VIPER_STING, me.GetGUID()))
                        VerifyAndPushSpellCast(spells, SPELL_VIPER_STING, TARGET_VICTIM, 5);

                    switch (GetSpec())
                    {
                        case TALENT_SPEC_HUNTER_BEASTMASTER:
                            VerifyAndPushSpellCast(spells, SPELL_AIMED_SHOT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_ARCANE_SHOT, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_STEADY_SHOT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_MULTI_SHOT, TARGET_VICTIM, 2);
                            break;
                        case TALENT_SPEC_HUNTER_MARKSMAN:
                            VerifyAndPushSpellCast(spells, SPELL_AIMED_SHOT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_CHIMERA_SHOT, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_ARCANE_SHOT, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_STEADY_SHOT, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_READINESS, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_SILENCING_SHOT, TARGET_VICTIM, 5);
                            break;
                        case TALENT_SPEC_HUNTER_SURVIVAL:
                            VerifyAndPushSpellCast(spells, SPELL_EXPLOSIVE_SHOT, TARGET_VICTIM, 8);
                            VerifyAndPushSpellCast(spells, SPELL_BLACK_ARROW, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_MULTI_SHOT, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_STEADY_SHOT, TARGET_VICTIM, 1);
                            break;
                    }
                    break;
                case CLASS_ROGUE:
                {
                    VerifyAndPushSpellCast(spells, SPELL_DISMANTLE, TARGET_VICTIM, 8);
                    VerifyAndPushSpellCast(spells, SPELL_EVASION, TARGET_NONE, 8);
                    VerifyAndPushSpellCast(spells, SPELL_VANISH, TARGET_NONE, 4);
                    VerifyAndPushSpellCast(spells, SPELL_BLIND, TARGET_VICTIM, 2);
                    VerifyAndPushSpellCast(spells, SPELL_CLOAK_OF_SHADOWS, TARGET_NONE, 2);

                    uint32 builder, finisher;
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_ROGUE_ASSASSINATION:
                            builder = SPELL_MUTILATE, finisher = SPELL_ENVENOM;
                            VerifyAndPushSpellCast(spells, SPELL_COLD_BLOOD, TARGET_NONE, 20);
                            break;
                        case TALENT_SPEC_ROGUE_COMBAT:
                            builder = SPELL_SINISTER_STRIKE, finisher = SPELL_EVISCERATE;
                            VerifyAndPushSpellCast(spells, SPELL_ADRENALINE_RUSH, TARGET_NONE, 6);
                            VerifyAndPushSpellCast(spells, SPELL_BLADE_FLURRY, TARGET_NONE, 5);
                            VerifyAndPushSpellCast(spells, SPELL_KILLING_SPREE, TARGET_NONE, 25);
                            break;
                        case TALENT_SPEC_ROGUE_SUBTLETY:
                            builder = SPELL_HEMORRHAGE, finisher = SPELL_EVISCERATE;
                            VerifyAndPushSpellCast(spells, SPELL_PREPARATION, TARGET_NONE, 10);
                            if (!me.IsWithinMeleeRange(me.GetVictim()))
                                VerifyAndPushSpellCast(spells, SPELL_SHADOWSTEP, TARGET_VICTIM, 25);
                            VerifyAndPushSpellCast(spells, SPELL_SHADOW_DANCE, TARGET_NONE, 10);
                            break;
                    }

                    if (Unit* victim = me.GetVictim())
                    {
                        if (victim.HasUnitState(UNIT_STATE_CASTING))
                            VerifyAndPushSpellCast(spells, SPELL_KICK, TARGET_VICTIM, 25);

                        uint8 const cp = me.GetPower(POWER_COMBO_POINTS);
                        if (cp >= 4)
                            VerifyAndPushSpellCast(spells, finisher, TARGET_VICTIM, 10);
                        if (cp <= 4)
                            VerifyAndPushSpellCast(spells, builder, TARGET_VICTIM, 5);
                    }
                    break;
                }
                case CLASS_PRIEST:
                    VerifyAndPushSpellCast(spells, SPELL_FEAR_WARD, TARGET_SELF, 2);
                    VerifyAndPushSpellCast(spells, SPELL_POWER_WORD_FORT, TARGET_CHARMER, 1);
                    VerifyAndPushSpellCast(spells, SPELL_DIVINE_SPIRIT, TARGET_CHARMER, 1);
                    VerifyAndPushSpellCast(spells, SPELL_SHADOW_PROTECTION, TARGET_CHARMER, 2);
                    VerifyAndPushSpellCast(spells, SPELL_DIVINE_HYMN, TARGET_NONE, 5);
                    VerifyAndPushSpellCast(spells, SPELL_HYMN_OF_HOPE, TARGET_NONE, 5);
                    VerifyAndPushSpellCast(spells, SPELL_SHADOW_WORD_DEATH, TARGET_VICTIM, 1);
                    VerifyAndPushSpellCast(spells, SPELL_PSYCHIC_SCREAM, TARGET_VICTIM, 3);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_PRIEST_DISCIPLINE:
                            VerifyAndPushSpellCast(spells, SPELL_POWER_WORD_SHIELD, TARGET_CHARMER, 3);
                            VerifyAndPushSpellCast(spells, SPELL_INNER_FOCUS, TARGET_NONE, 3);
                            VerifyAndPushSpellCast(spells, SPELL_PAIN_SUPPRESSION, TARGET_CHARMER, 15);
                            VerifyAndPushSpellCast(spells, SPELL_POWER_INFUSION, TARGET_CHARMER, 10);
                            VerifyAndPushSpellCast(spells, SPELL_PENANCE, TARGET_CHARMER, 3);
                            VerifyAndPushSpellCast(spells, SPELL_FLASH_HEAL, TARGET_CHARMER, 1);
                            break;
                        case TALENT_SPEC_PRIEST_HOLY:
                            VerifyAndPushSpellCast(spells, SPELL_DESPERATE_PRAYER, TARGET_NONE, 3);
                            VerifyAndPushSpellCast(spells, SPELL_GUARDIAN_SPIRIT, TARGET_CHARMER, 5);
                            VerifyAndPushSpellCast(spells, SPELL_FLASH_HEAL, TARGET_CHARMER, 1);
                            VerifyAndPushSpellCast(spells, SPELL_RENEW, TARGET_CHARMER, 3);
                            break;
                        case TALENT_SPEC_PRIEST_SHADOW:
                            if (!me.HasAura(SPELL_SHADOWFORM))
                            {
                                VerifyAndPushSpellCast(spells, SPELL_SHADOWFORM, TARGET_NONE, 100);
                                break;
                            }
                            if (Unit* victim = me.GetVictim())
                            {
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_VAMPIRIC_TOUCH, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_VAMPIRIC_TOUCH, TARGET_VICTIM, 4);
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_SHADOW_WORD_PAIN, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_SHADOW_WORD_PAIN, TARGET_VICTIM, 3);
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_DEVOURING_PLAGUE, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_DEVOURING_PLAGUE, TARGET_VICTIM, 4);
                            }
                            VerifyAndPushSpellCast(spells, SPELL_MIND_BLAST, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_MIND_FLAY, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_DISPERSION, TARGET_NONE, 10);
                            break;
                    }
                    break;
                case CLASS_DEATH_KNIGHT:
                {
                    if (!me.IsWithinMeleeRange(me.GetVictim()))
                        VerifyAndPushSpellCast(spells, SPELL_DEATH_GRIP, TARGET_VICTIM, 25);
                    VerifyAndPushSpellCast(spells, SPELL_STRANGULATE, TARGET_VICTIM, 15);
                    VerifyAndPushSpellCast(spells, SPELL_EMPOWER_RUNE_WEAP, TARGET_NONE, 5);
                    VerifyAndPushSpellCast(spells, SPELL_ICEBORN_FORTITUDE, TARGET_NONE, 15);
                    VerifyAndPushSpellCast(spells, SPELL_ANTI_MAGIC_SHELL, TARGET_NONE, 10);

                    bool hasFF = false, hasBP = false;
                    if (Unit* victim = me.GetVictim())
                    {
                        if (victim.HasUnitState(UNIT_STATE_CASTING))
                            VerifyAndPushSpellCast(spells, SPELL_MIND_FREEZE, TARGET_VICTIM, 25);

                        hasFF = !!victim.GetAuraApplicationOfRankedSpell(AURA_FROST_FEVER, me.GetGUID()), hasBP = !!victim.GetAuraApplicationOfRankedSpell(AURA_BLOOD_PLAGUE, me.GetGUID());
                        if (hasFF && hasBP)
                            VerifyAndPushSpellCast(spells, SPELL_PESTILENCE, TARGET_VICTIM, 3);
                        if (!hasFF)
                            VerifyAndPushSpellCast(spells, SPELL_ICY_TOUCH, TARGET_VICTIM, 4);
                        if (!hasBP)
                            VerifyAndPushSpellCast(spells, SPELL_PLAGUE_STRIKE, TARGET_VICTIM, 4);
                    }
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_DEATHKNIGHT_BLOOD:
                            VerifyAndPushSpellCast(spells, SPELL_RUNE_TAP, TARGET_NONE, 2);
                            VerifyAndPushSpellCast(spells, SPELL_HYSTERIA, TARGET_SELF, 5);
                            if (Creature* creatureCharmer = GetCharmer())
                                if (!creatureCharmer.IsDungeonBoss() && !creatureCharmer.isWorldBoss())
                                    VerifyAndPushSpellCast(spells, SPELL_HYSTERIA, creatureCharmer, 15);
                            VerifyAndPushSpellCast(spells, SPELL_HEART_STRIKE, TARGET_VICTIM, 2);
                            if (hasFF && hasBP)
                                VerifyAndPushSpellCast(spells, SPELL_DEATH_STRIKE, TARGET_VICTIM, 8);
                            VerifyAndPushSpellCast(spells, SPELL_DEATH_COIL_DK, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_MARK_OF_BLOOD, TARGET_VICTIM, 20);
                            VerifyAndPushSpellCast(spells, SPELL_VAMPIRIC_BLOOD, TARGET_NONE, 10);
                            break;
                        case TALENT_SPEC_DEATHKNIGHT_FROST:
                            if (hasFF && hasBP)
                                VerifyAndPushSpellCast(spells, SPELL_OBLITERATE, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_HOWLING_BLAST, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_UNBREAKABLE_ARMOR, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_DEATHCHILL, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_FROST_STRIKE, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_BLOOD_STRIKE, TARGET_VICTIM, 1);
                            break;
                        case TALENT_SPEC_DEATHKNIGHT_UNHOLY:
                            if (hasFF && hasBP)
                                VerifyAndPushSpellCast(spells, SPELL_SCOURGE_STRIKE, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_DEATH_AND_DECAY, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_ANTI_MAGIC_ZONE, TARGET_NONE, 8);
                            VerifyAndPushSpellCast(spells, SPELL_SUMMON_GARGOYLE, TARGET_VICTIM, 7);
                            VerifyAndPushSpellCast(spells, SPELL_BLOOD_STRIKE, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_DEATH_COIL_DK, TARGET_VICTIM, 3);
                            break;
                    }
                    break;
                }
                case CLASS_SHAMAN:
                    VerifyAndPushSpellCast(spells, SPELL_HEROISM, TARGET_NONE, 25);
                    VerifyAndPushSpellCast(spells, SPELL_BLOODLUST, TARGET_NONE, 25);
                    VerifyAndPushSpellCast(spells, SPELL_GROUNDING_TOTEM, TARGET_NONE, 2);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_SHAMAN_RESTORATION:
                            if (Unit* charmer = me.GetCharmer())
                                if (!charmer.GetAuraApplicationOfRankedSpell(SPELL_EARTH_SHIELD, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_EARTH_SHIELD, charmer, 2);
                            if (me.HasAura(SPELL_SHA_NATURE_SWIFT))
                                VerifyAndPushSpellCast(spells, SPELL_HEALING_WAVE, TARGET_CHARMER, 20);
                            else
                                VerifyAndPushSpellCast(spells, SPELL_LESSER_HEAL_WAVE, TARGET_CHARMER, 1);
                            VerifyAndPushSpellCast(spells, SPELL_TIDAL_FORCE, TARGET_NONE, 4);
                            VerifyAndPushSpellCast(spells, SPELL_SHA_NATURE_SWIFT, TARGET_NONE, 4);
                            VerifyAndPushSpellCast(spells, SPELL_MANA_TIDE_TOTEM, TARGET_NONE, 3);
                            break;
                        case TALENT_SPEC_SHAMAN_ELEMENTAL:
                            if (Unit* victim = me.GetVictim())
                            {
                                if (victim.GetAuraOfRankedSpell(SPELL_FLAME_SHOCK, GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_LAVA_BURST, TARGET_VICTIM, 5);
                                else
                                    VerifyAndPushSpellCast(spells, SPELL_FLAME_SHOCK, TARGET_VICTIM, 3);
                            }
                            VerifyAndPushSpellCast(spells, SPELL_CHAIN_LIGHTNING, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_LIGHTNING_BOLT, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_ELEMENTAL_MASTERY, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_THUNDERSTORM, TARGET_NONE, 3);
                            break;
                        case TALENT_SPEC_SHAMAN_ENHANCEMENT:
                            if (Aura const* maelstrom = me.GetAura(AURA_MAELSTROM_WEAPON))
                                if (maelstrom.GetStackAmount() == 5)
                                    VerifyAndPushSpellCast(spells, SPELL_LIGHTNING_BOLT, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_STORMSTRIKE, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_EARTH_SHOCK, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_LAVA_LASH, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_SHAMANISTIC_RAGE, TARGET_NONE, 10);
                            break;
                    }
                    break;
                case CLASS_MAGE:
                    if (me.GetVictim() && me.GetVictim().HasUnitState(UNIT_STATE_CASTING))
                        VerifyAndPushSpellCast(spells, SPELL_COUNTERSPELL, TARGET_VICTIM, 25);
                    VerifyAndPushSpellCast(spells, SPELL_DAMPEN_MAGIC, TARGET_CHARMER, 2);
                    VerifyAndPushSpellCast(spells, SPELL_EVOCATION, TARGET_NONE, 3);
                    VerifyAndPushSpellCast(spells, SPELL_MANA_SHIELD, TARGET_NONE, 1);
                    VerifyAndPushSpellCast(spells, SPELL_MIRROR_IMAGE, TARGET_NONE, 3);
                    VerifyAndPushSpellCast(spells, SPELL_SPELLSTEAL, TARGET_VICTIM, 2);
                    VerifyAndPushSpellCast(spells, SPELL_ICE_BLOCK, TARGET_NONE, 1);
                    VerifyAndPushSpellCast(spells, SPELL_ICY_VEINS, TARGET_NONE, 3);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_MAGE_ARCANE:
                            if (Aura* abAura = me.GetAura(AURA_ARCANE_BLAST))
                                if (abAura.GetStackAmount() >= 3)
                                    VerifyAndPushSpellCast(spells, SPELL_ARCANE_MISSILES, TARGET_VICTIM, 7);
                            VerifyAndPushSpellCast(spells, SPELL_ARCANE_BLAST, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_ARCANE_BARRAGE, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_ARCANE_POWER, TARGET_NONE, 8);
                            VerifyAndPushSpellCast(spells, SPELL_PRESENCE_OF_MIND, TARGET_NONE, 7);
                            break;
                        case TALENT_SPEC_MAGE_FIRE:
                            if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_LIVING_BOMB))
                                VerifyAndPushSpellCast(spells, SPELL_LIVING_BOMB, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_COMBUSTION, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_FIREBALL, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_FIRE_BLAST, TARGET_VICTIM, 1);
                            VerifyAndPushSpellCast(spells, SPELL_DRAGONS_BREATH, TARGET_VICTIM, 2);
                            VerifyAndPushSpellCast(spells, SPELL_BLAST_WAVE, TARGET_VICTIM, 1);
                            break;
                        case TALENT_SPEC_MAGE_FROST:
                            VerifyAndPushSpellCast(spells, SPELL_DEEP_FREEZE, TARGET_VICTIM, 10);
                            VerifyAndPushSpellCast(spells, SPELL_FROST_NOVA, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_FROSTBOLT, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_COLD_SNAP, TARGET_VICTIM, 5);
                            if (me.GetVictim() && me.GetVictim().HasAuraState(AURA_STATE_FROZEN, nullptr, me))
                                VerifyAndPushSpellCast(spells, SPELL_ICE_LANCE, TARGET_VICTIM, 5);
                            break;
                    }
                    break;
                case CLASS_WARLOCK:
                    VerifyAndPushSpellCast(spells, SPELL_DEATH_COIL_W, TARGET_VICTIM, 2);
                    VerifyAndPushSpellCast(spells, SPELL_FEAR, TARGET_VICTIM, 2);
                    VerifyAndPushSpellCast(spells, SPELL_SEED_OF_CORRUPTION, TARGET_VICTIM, 4);
                    VerifyAndPushSpellCast(spells, SPELL_HOWL_OF_TERROR, TARGET_NONE, 2);
                    if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_CORRUPTION, me.GetGUID()))
                        VerifyAndPushSpellCast(spells, SPELL_CORRUPTION, TARGET_VICTIM, 10);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_WARLOCK_AFFLICTION:
                            if (Unit* victim = me.GetVictim())
                            {
                                VerifyAndPushSpellCast(spells, SPELL_SHADOW_BOLT, TARGET_VICTIM, 7);
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_UNSTABLE_AFFLICTION, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_UNSTABLE_AFFLICTION, TARGET_VICTIM, 8);
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_HAUNT, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_HAUNT, TARGET_VICTIM, 8);
                                if (!victim.GetAuraApplicationOfRankedSpell(SPELL_CURSE_OF_AGONY, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_CURSE_OF_AGONY, TARGET_VICTIM, 4);
                                if (victim.HealthBelowPct(25))
                                    VerifyAndPushSpellCast(spells, SPELL_DRAIN_SOUL, TARGET_VICTIM, 100);
                            }
                            break;
                        case TALENT_SPEC_WARLOCK_DEMONOLOGY:
                            VerifyAndPushSpellCast(spells, SPELL_METAMORPHOSIS, TARGET_NONE, 15);
                            VerifyAndPushSpellCast(spells, SPELL_SHADOW_BOLT, TARGET_VICTIM, 7);
                            if (me.HasAura(AURA_DECIMATION))
                                VerifyAndPushSpellCast(spells, SPELL_SOUL_FIRE, TARGET_VICTIM, 100);
                            if (me.HasAura(SPELL_METAMORPHOSIS))
                            {
                                VerifyAndPushSpellCast(spells, SPELL_IMMOLATION_AURA, TARGET_NONE, 30);
                                if (!me.IsWithinMeleeRange(me.GetVictim()))
                                    VerifyAndPushSpellCast(spells, SPELL_DEMON_CHARGE, TARGET_VICTIM, 20);
                            }
                            if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_IMMOLATE, me.GetGUID()))
                                VerifyAndPushSpellCast(spells, SPELL_IMMOLATE, TARGET_VICTIM, 5);
                            if (me.HasAura(AURA_MOLTEN_CORE))
                                VerifyAndPushSpellCast(spells, SPELL_INCINERATE, TARGET_VICTIM, 10);
                            break;
                        case TALENT_SPEC_WARLOCK_DESTRUCTION:
                            if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_IMMOLATE, me.GetGUID()))
                                VerifyAndPushSpellCast(spells, SPELL_IMMOLATE, TARGET_VICTIM, 8);
                            if (me.GetVictim() && me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_IMMOLATE, me.GetGUID()))
                                VerifyAndPushSpellCast(spells, SPELL_CONFLAGRATE, TARGET_VICTIM, 8);
                            VerifyAndPushSpellCast(spells, SPELL_SHADOWFURY, TARGET_VICTIM, 5);
                            VerifyAndPushSpellCast(spells, SPELL_CHAOS_BOLT, TARGET_VICTIM, 10);
                            VerifyAndPushSpellCast(spells, SPELL_SHADOWBURN, TARGET_VICTIM, 3);
                            VerifyAndPushSpellCast(spells, SPELL_INCINERATE, TARGET_VICTIM, 7);
                            break;
                    }
                    break;
                case CLASS_MONK:
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_MONK_BREWMASTER:
                        case TALENT_SPEC_MONK_BATTLEDANCER:
                        case TALENT_SPEC_MONK_MISTWEAVER:
                            break;
                    }
                    break;
                case CLASS_DRUID:
                    VerifyAndPushSpellCast(spells, SPELL_INNERVATE, TARGET_CHARMER, 5);
                    VerifyAndPushSpellCast(spells, SPELL_BARKSKIN, TARGET_NONE, 5);
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_DRUID_RESTORATION:
                            if (!me.HasAura(SPELL_TREE_OF_LIFE))
                            {
                                CancelAllShapeshifts();
                                VerifyAndPushSpellCast(spells, SPELL_TREE_OF_LIFE, TARGET_NONE, 100);
                                break;
                            }
                            VerifyAndPushSpellCast(spells, SPELL_TRANQUILITY, TARGET_NONE, 10);
                            VerifyAndPushSpellCast(spells, SPELL_NATURE_SWIFTNESS, TARGET_NONE, 7);
                            if (Creature* creatureCharmer = GetCharmer())
                            {
                                VerifyAndPushSpellCast(spells, SPELL_NOURISH, creatureCharmer, 5);
                                VerifyAndPushSpellCast(spells, SPELL_WILD_GROWTH, creatureCharmer, 5);
                                if (!creatureCharmer.GetAuraApplicationOfRankedSpell(SPELL_REJUVENATION, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_REJUVENATION, creatureCharmer, 8);
                                if (!creatureCharmer.GetAuraApplicationOfRankedSpell(SPELL_REGROWTH, me.GetGUID()))
                                    VerifyAndPushSpellCast(spells, SPELL_REGROWTH, creatureCharmer, 8);
                                uint8 lifebloomStacks = 0;
                                if (Aura const* lifebloom = creatureCharmer.GetAura(SPELL_LIFEBLOOM, me.GetGUID()))
                                    lifebloomStacks = lifebloom.GetStackAmount();
                                if (lifebloomStacks < 3)
                                    VerifyAndPushSpellCast(spells, SPELL_LIFEBLOOM, creatureCharmer, 5);
                                if (creatureCharmer.GetAuraApplicationOfRankedSpell(SPELL_REJUVENATION) ||
                                    creatureCharmer.GetAuraApplicationOfRankedSpell(SPELL_REGROWTH))
                                    VerifyAndPushSpellCast(spells, SPELL_SWIFTMEND, creatureCharmer, 10);
                                if (me.HasAura(SPELL_NATURE_SWIFTNESS))
                                    VerifyAndPushSpellCast(spells, SPELL_HEALING_TOUCH, creatureCharmer, 100);
                            }
                            break;
                        case TALENT_SPEC_DRUID_BALANCE:
                        {
                            if (!me.HasAura(SPELL_MOONKIN_FORM))
                            {
                                CancelAllShapeshifts();
                                VerifyAndPushSpellCast(spells, SPELL_MOONKIN_FORM, TARGET_NONE, 100);
                                break;
                            }
                            uint32 const mainAttackSpell = me.HasAura(AURA_ECLIPSE_LUNAR) ? SPELL_STARFIRE : SPELL_WRATH;
                            VerifyAndPushSpellCast(spells, SPELL_STARFALL, TARGET_NONE, 20);
                            VerifyAndPushSpellCast(spells, mainAttackSpell, TARGET_VICTIM, 10);
                            if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_INSECT_SWARM, me.GetGUID()))
                                VerifyAndPushSpellCast(spells, SPELL_INSECT_SWARM, TARGET_VICTIM, 7);
                            if (me.GetVictim() && !me.GetVictim().GetAuraApplicationOfRankedSpell(SPELL_MOONFIRE, me.GetGUID()))
                                VerifyAndPushSpellCast(spells, SPELL_MOONFIRE, TARGET_VICTIM, 5);
                            if (me.GetVictim() && me.GetVictim().HasUnitState(UNIT_STATE_CASTING))
                                VerifyAndPushSpellCast(spells, SPELL_TYPHOON, TARGET_NONE, 15);
                            break;
                        }
                        case TALENT_SPEC_DRUID_CAT:
                        case TALENT_SPEC_DRUID_BEAR:
                            if (!me.HasAura(SPELL_CAT_FORM))
                            {
                                CancelAllShapeshifts();
                                VerifyAndPushSpellCast(spells, SPELL_CAT_FORM, TARGET_NONE, 100);
                                break;
                            }
                            VerifyAndPushSpellCast(spells, SPELL_BERSERK, TARGET_NONE, 20);
                            VerifyAndPushSpellCast(spells, SPELL_SURVIVAL_INSTINCTS, TARGET_NONE, 15);
                            VerifyAndPushSpellCast(spells, SPELL_TIGER_FURY, TARGET_NONE, 15);
                            VerifyAndPushSpellCast(spells, SPELL_DASH, TARGET_NONE, 5);
                            if (Unit* victim = me.GetVictim())
                            {
                                uint8 const cp = me.GetPower(POWER_COMBO_POINTS);
                                if (victim.HasUnitState(UNIT_STATE_CASTING) && cp >= 1)
                                    VerifyAndPushSpellCast(spells, SPELL_MAIM, TARGET_VICTIM, 25);
                                if (!me.IsWithinMeleeRange(victim))
                                    VerifyAndPushSpellCast(spells, SPELL_FERAL_CHARGE_CAT, TARGET_VICTIM, 25);
                                if (cp >= 4)
                                    VerifyAndPushSpellCast(spells, SPELL_RIP, TARGET_VICTIM, 50);
                                if (cp <= 4)
                                {
                                    VerifyAndPushSpellCast(spells, SPELL_MANGLE_CAT, TARGET_VICTIM, 10);
                                    VerifyAndPushSpellCast(spells, SPELL_CLAW, TARGET_VICTIM, 5);
                                    if (!victim.GetAuraApplicationOfRankedSpell(SPELL_RAKE, me.GetGUID()))
                                        VerifyAndPushSpellCast(spells, SPELL_RAKE, TARGET_VICTIM, 8);
                                    if (!me.HasAura(SPELL_SAVAGE_ROAR))
                                        VerifyAndPushSpellCast(spells, SPELL_SAVAGE_ROAR, TARGET_NONE, 15);
                                }
                            }
                            break;
                    }
                    break;
                case CLASS_DEMON_HUNTER:
                    switch (GetSpec())
                    {
                        case TALENT_SPEC_DEMON_HUNTER_HAVOC:
                        case TALENT_SPEC_DEMON_HUNTER_VENGEANCE:
                            break;
                    }
                    break;
            }
            */
            return SelectSpellCast(spells);
        }

        const float CASTER_CHASE_DISTANCE = 28.0f;

        public override void UpdateAI(uint diff)
        {
            Creature charmer = GetCharmer();
            if (!charmer)
                return;

            //kill self if charm aura has infinite duration
            if (charmer.IsInEvadeMode())
            {
                var auras = me.GetAuraEffectsByType(AuraType.ModCharm);
                foreach (var effect in auras)
                {
                    if (effect.GetCasterGUID() == charmer.GetGUID() && effect.GetBase().IsPermanent())
                    {
                        me.KillSelf();
                        return;
                    }
                }
            }

            if (charmer.IsInCombat())
            {
                Unit target = me.GetVictim();
                if (!target || !charmer.IsValidAttackTarget(target) || target.HasBreakableByDamageCrowdControlAura())
                {
                    target = SelectAttackTarget();
                    if (!target)
                        return;

                    if (IsRangedAttacker())
                    {
                        _chaseCloser = !me.IsWithinLOSInMap(target);
                        if (_chaseCloser)
                            AttackStart(target);
                        else
                            AttackStartCaster(target, CASTER_CHASE_DISTANCE);
                    }
                    else
                        AttackStart(target);
                    _forceFacing = true;
                }

                if (me.IsStopped() && !me.HasUnitState(UnitState.CannotTurn))
                {
                    float targetAngle = me.GetAngle(target);
                    if (_forceFacing || Math.Abs(me.GetOrientation() - targetAngle) > 0.4f)
                    {
                        me.SetFacingTo(targetAngle);
                        _forceFacing = false;
                    }
                }

                if (_castCheckTimer <= diff)
                {
                    if (me.HasUnitState(UnitState.Casting))
                        _castCheckTimer = 0;
                    else
                    {
                        if (IsRangedAttacker())
                        { // chase to zero if the target isn't in line of sight
                            bool inLOS = me.IsWithinLOSInMap(target);
                            if (_chaseCloser != !inLOS)
                            {
                                _chaseCloser = !inLOS;
                                if (_chaseCloser)
                                    AttackStart(target);
                                else
                                    AttackStartCaster(target, CASTER_CHASE_DISTANCE);
                            }
                        }
                        Tuple<Spell, Unit> shouldCast = SelectAppropriateCastForSpec();
                        if (shouldCast != null)
                            DoCastAtTarget(shouldCast);
                        _castCheckTimer = 500;
                    }
                }
                else
                    _castCheckTimer -= diff;

                DoAutoAttackIfReady();
            }
            else
            {
                me.AttackStop();
                me.CastStop();
                me.StopMoving();
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveFollow(charmer, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
            }
        }

        public override void OnCharmed(bool apply)
        {
            if (apply)
            {
                me.CastStop();
                me.AttackStop();
                me.StopMoving();
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MovePoint(0, me.GetPosition(), false); // force re-sync of current position for all clients
            }
            else
            {
                me.CastStop();
                me.AttackStop();
                // @todo only voluntary movement (don't cancel stuff like death grip or charge mid-animation)
                me.GetMotionMaster().Clear();
                me.StopMoving();
            }
        }

        uint _castCheckTimer;
        bool _chaseCloser;
        bool _forceFacing;
    }

    struct UncontrolledTargetSelectPredicate : ISelector
    {
        public bool Check(Unit target)
        {
            return !target.HasBreakableByDamageCrowdControlAura();
        }
    }
}
