// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.ShadeOfAran
{
    internal struct SpellIds
    {
        public const uint Frostbolt = 29954;
        public const uint Fireball = 29953;
        public const uint Arcmissle = 29955;
        public const uint Chainsofice = 29991;
        public const uint Dragonsbreath = 29964;
        public const uint Massslow = 30035;
        public const uint FlameWreath = 29946;
        public const uint AoeCs = 29961;
        public const uint Playerpull = 32265;
        public const uint Aexplosion = 29973;
        public const uint MassPoly = 29963;
        public const uint BlinkCenter = 29967;
        public const uint Elementals = 29962;
        public const uint Conjure = 29975;
        public const uint Drink = 30024;
        public const uint Potion = 32453;
        public const uint AoePyroblast = 29978;

        public const uint CircularBlizzard = 29951;
        public const uint Waterbolt = 31012;
        public const uint ShadowPyro = 29978;
    }

    internal struct CreatureIds
    {
        public const uint WaterElemental = 17167;
        public const uint ShadowOfAran = 18254;
        public const uint AranBlizzard = 17161;
    }

    internal struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayFlamewreath = 1;
        public const uint SayBlizzard = 2;
        public const uint SayExplosion = 3;
        public const uint SayDrink = 4;
        public const uint SayElementals = 5;
        public const uint SayKill = 6;
        public const uint SayTimeover = 7;
        public const uint SayDeath = 8;
        public const uint SayAtiesh = 9;
    }

    internal enum SuperSpell
    {
        Flame = 0,
        Blizzard,
        Ae
    }

    [Script]
    internal class boss_aran : ScriptedAI
    {
        private static readonly uint[] AtieshStaves =
        {
            22589, //ItemAtieshMage,
			22630, //ItemAtieshWarlock,
			22631, //ItemAtieshPriest,
			22632  //ItemAtieshDruid,
		};

        private readonly ObjectGuid[] FlameWreathTarget = new ObjectGuid[3];
        private readonly float[] FWTargPosX = new float[3];
        private readonly float[] FWTargPosY = new float[3];

        private readonly InstanceScript instance;

        private uint ArcaneCooldown;
        private uint BerserkTimer;
        private uint CloseDoorTimer; // Don't close the door right on aggro in case some people are still entering.

        private uint CurrentNormalSpell;
        private bool Drinking;

        private uint DrinkInterruptTimer;
        private bool DrinkInturrupted;

        private bool ElementalsSpawned;
        private uint FireCooldown;
        private uint FlameWreathCheckTime;

        private uint FlameWreathTimer;
        private uint FrostCooldown;

        private SuperSpell LastSuperSpell;
        private uint NormalCastTimer;

        private uint SecondarySpellTimer;
        private bool SeenAtiesh;
        private uint SuperCastTimer;

        public boss_aran(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            Initialize();

            // Not in progress
            instance.SetBossState(DataTypes.Aran, EncounterState.NotStarted);
            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoLibraryDoor), true);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            instance.SetBossState(DataTypes.Aran, EncounterState.Done);
            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoLibraryDoor), true);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayAggro);

            instance.SetBossState(DataTypes.Aran, EncounterState.InProgress);
            instance.HandleGameObject(instance.GetGuidData(DataTypes.GoLibraryDoor), false);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (CloseDoorTimer != 0)
            {
                if (CloseDoorTimer <= diff)
                {
                    instance.HandleGameObject(instance.GetGuidData(DataTypes.GoLibraryDoor), false);
                    CloseDoorTimer = 0;
                }
                else
                {
                    CloseDoorTimer -= diff;
                }
            }

            //Cooldowns for casts
            if (ArcaneCooldown != 0)
            {
                if (ArcaneCooldown >= diff)
                    ArcaneCooldown -= diff;
                else ArcaneCooldown = 0;
            }

            if (FireCooldown != 0)
            {
                if (FireCooldown >= diff)
                    FireCooldown -= diff;
                else FireCooldown = 0;
            }

            if (FrostCooldown != 0)
            {
                if (FrostCooldown >= diff)
                    FrostCooldown -= diff;
                else FrostCooldown = 0;
            }

            if (!Drinking &&
                me.GetMaxPower(PowerType.Mana) != 0 &&
                me.GetPowerPct(PowerType.Mana) < 20.0f)
            {
                Drinking = true;
                me.InterruptNonMeleeSpells(false);

                Talk(TextIds.SayDrink);

                if (!DrinkInturrupted)
                {
                    DoCast(me, SpellIds.MassPoly, new CastSpellExtraArgs(true));
                    DoCast(me, SpellIds.Conjure, new CastSpellExtraArgs(false));
                    DoCast(me, SpellIds.Drink, new CastSpellExtraArgs(false));
                    me.SetStandState(UnitStandStateType.Sit);
                    DrinkInterruptTimer = 10000;
                }
            }

            //Drink Interrupt
            if (Drinking && DrinkInturrupted)
            {
                Drinking = false;
                me.RemoveAura(SpellIds.Drink);
                me.SetStandState(UnitStandStateType.Stand);
                me.SetPower(PowerType.Mana, me.GetMaxPower(PowerType.Mana) - 32000);
                DoCast(me, SpellIds.Potion, new CastSpellExtraArgs(false));
            }

            //Drink Interrupt Timer
            if (Drinking && !DrinkInturrupted)
            {
                if (DrinkInterruptTimer >= diff)
                {
                    DrinkInterruptTimer -= diff;
                }
                else
                {
                    me.SetStandState(UnitStandStateType.Stand);
                    DoCast(me, SpellIds.Potion, new CastSpellExtraArgs(true));
                    DoCast(me, SpellIds.AoePyroblast, new CastSpellExtraArgs(false));
                    DrinkInturrupted = true;
                    Drinking = false;
                }
            }

            //Don't execute any more code if we are drinking
            if (Drinking)
                return;

            //Normal casts
            if (NormalCastTimer <= diff)
            {
                if (!me.IsNonMeleeSpellCast(false))
                {
                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                    if (!target)
                        return;

                    uint[] Spells = new uint[3];
                    byte AvailableSpells = 0;

                    //Check for what spells are not on cooldown
                    if (ArcaneCooldown == 0)
                    {
                        Spells[AvailableSpells] = SpellIds.Arcmissle;
                        ++AvailableSpells;
                    }

                    if (FireCooldown == 0)
                    {
                        Spells[AvailableSpells] = SpellIds.Fireball;
                        ++AvailableSpells;
                    }

                    if (FrostCooldown == 0)
                    {
                        Spells[AvailableSpells] = SpellIds.Frostbolt;
                        ++AvailableSpells;
                    }

                    //If no available spells wait 1 second and try again
                    if (AvailableSpells != 0)
                    {
                        CurrentNormalSpell = Spells[RandomHelper.Rand32() % AvailableSpells];
                        DoCast(target, CurrentNormalSpell);
                    }
                }

                NormalCastTimer = 1000;
            }
            else
            {
                NormalCastTimer -= diff;
            }

            if (SecondarySpellTimer <= diff)
            {
                switch (RandomHelper.URand(0, 1))
                {
                    case 0:
                        DoCast(me, SpellIds.AoeCs);

                        break;
                    case 1:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                        if (target)
                            DoCast(target, SpellIds.Chainsofice);

                        break;
                }

                SecondarySpellTimer = RandomHelper.URand(5000, 20000);
            }
            else
            {
                SecondarySpellTimer -= diff;
            }

            if (SuperCastTimer <= diff)
            {
                SuperSpell[] Available = new SuperSpell[2];

                switch (LastSuperSpell)
                {
                    case SuperSpell.Ae:
                        Available[0] = SuperSpell.Flame;
                        Available[1] = SuperSpell.Blizzard;

                        break;
                    case SuperSpell.Flame:
                        Available[0] = SuperSpell.Ae;
                        Available[1] = SuperSpell.Blizzard;

                        break;
                    case SuperSpell.Blizzard:
                        Available[0] = SuperSpell.Flame;
                        Available[1] = SuperSpell.Ae;

                        break;
                    default:
                        Available[0] = 0;
                        Available[1] = 0;

                        break;
                }

                LastSuperSpell = Available[RandomHelper.URand(0, 1)];

                switch (LastSuperSpell)
                {
                    case SuperSpell.Ae:
                        Talk(TextIds.SayExplosion);

                        DoCast(me, SpellIds.BlinkCenter, new CastSpellExtraArgs(true));
                        DoCast(me, SpellIds.Playerpull, new CastSpellExtraArgs(true));
                        DoCast(me, SpellIds.Massslow, new CastSpellExtraArgs(true));
                        DoCast(me, SpellIds.Aexplosion, new CastSpellExtraArgs(false));

                        break;

                    case SuperSpell.Flame:
                        Talk(TextIds.SayFlamewreath);

                        FlameWreathTimer = 20000;
                        FlameWreathCheckTime = 500;

                        FlameWreathTarget[0].Clear();
                        FlameWreathTarget[1].Clear();
                        FlameWreathTarget[2].Clear();

                        FlameWreathEffect();

                        break;

                    case SuperSpell.Blizzard:
                        Talk(TextIds.SayBlizzard);

                        Creature pSpawn = me.SummonCreature(CreatureIds.AranBlizzard, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(25));

                        if (pSpawn)
                        {
                            pSpawn.SetFaction(me.GetFaction());
                            pSpawn.CastSpell(pSpawn, SpellIds.CircularBlizzard, false);
                        }

                        break;
                }

                SuperCastTimer = RandomHelper.URand(35000, 40000);
            }
            else
            {
                SuperCastTimer -= diff;
            }

            if (!ElementalsSpawned &&
                HealthBelowPct(40))
            {
                ElementalsSpawned = true;

                for (uint i = 0; i < 4; ++i)
                {
                    Creature unit = me.SummonCreature(CreatureIds.WaterElemental, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(90));

                    if (unit)
                    {
                        unit.Attack(me.GetVictim(), true);
                        unit.SetFaction(me.GetFaction());
                    }
                }

                Talk(TextIds.SayElementals);
            }

            if (BerserkTimer <= diff)
            {
                for (uint i = 0; i < 5; ++i)
                {
                    Creature unit = me.SummonCreature(CreatureIds.ShadowOfAran, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));

                    if (unit)
                    {
                        unit.Attack(me.GetVictim(), true);
                        unit.SetFaction(me.GetFaction());
                    }
                }

                Talk(TextIds.SayTimeover);

                BerserkTimer = 60000;
            }
            else
            {
                BerserkTimer -= diff;
            }

            //Flame Wreath check
            if (FlameWreathTimer != 0)
            {
                if (FlameWreathTimer >= diff)
                    FlameWreathTimer -= diff;
                else FlameWreathTimer = 0;

                if (FlameWreathCheckTime <= diff)
                {
                    for (byte i = 0; i < 3; ++i)
                    {
                        if (FlameWreathTarget[i].IsEmpty())
                            continue;

                        Unit unit = Global.ObjAccessor.GetUnit(me, FlameWreathTarget[i]);

                        if (unit && !unit.IsWithinDist2d(FWTargPosX[i], FWTargPosY[i], 3))
                        {
                            unit.CastSpell(unit,
                                           20476,
                                           new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                               .SetOriginalCaster(me.GetGUID()));

                            unit.CastSpell(unit, 11027, true);
                            FlameWreathTarget[i].Clear();
                        }
                    }

                    FlameWreathCheckTime = 500;
                }
                else
                {
                    FlameWreathCheckTime -= diff;
                }
            }

            if (ArcaneCooldown != 0 &&
                FireCooldown != 0 &&
                FrostCooldown != 0)
                DoMeleeAttackIfReady();
        }

        public override void DamageTaken(Unit pAttacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!DrinkInturrupted &&
                Drinking &&
                damage != 0)
                DrinkInturrupted = true;
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            //We only care about interrupt effects and only if they are durring a spell currently being cast
            if (!spellInfo.HasEffect(SpellEffectName.InterruptCast) ||
                !me.IsNonMeleeSpellCast(false))
                return;

            //Interrupt effect
            me.InterruptNonMeleeSpells(false);

            //Normally we would set the cooldown equal to the spell duration
            //but we do not have access to the DurationStore

            switch (CurrentNormalSpell)
            {
                case SpellIds.Arcmissle:
                    ArcaneCooldown = 5000;

                    break;
                case SpellIds.Fireball:
                    FireCooldown = 5000;

                    break;
                case SpellIds.Frostbolt:
                    FrostCooldown = 5000;

                    break;
            }
        }

        public override void MoveInLineOfSight(Unit who)
        {
            base.MoveInLineOfSight(who);

            if (SeenAtiesh ||
                me.IsInCombat() ||
                me.GetDistance2d(who) > me.GetAttackDistance(who) + 10.0f)
                return;

            Player player = who.ToPlayer();

            if (!player)
                return;

            foreach (uint id in AtieshStaves)
            {
                if (!PlayerHasWeaponEquipped(player, id))
                    continue;

                SeenAtiesh = true;
                Talk(TextIds.SayAtiesh);
                me.SetFacingTo(me.GetAbsoluteAngle(player));
                me.ClearUnitState(UnitState.Moving);
                me.GetMotionMaster().MoveDistract(7 * Time.InMilliseconds, me.GetAbsoluteAngle(who));

                break;
            }
        }

        private void Initialize()
        {
            SecondarySpellTimer = 5000;
            NormalCastTimer = 0;
            SuperCastTimer = 35000;
            BerserkTimer = 720000;
            CloseDoorTimer = 15000;

            LastSuperSpell = (SuperSpell)(RandomHelper.Rand32() % 3);

            FlameWreathTimer = 0;
            FlameWreathCheckTime = 0;

            CurrentNormalSpell = 0;
            ArcaneCooldown = 0;
            FireCooldown = 0;
            FrostCooldown = 0;

            DrinkInterruptTimer = 10000;

            ElementalsSpawned = false;
            Drinking = false;
            DrinkInturrupted = false;
        }

        private void FlameWreathEffect()
        {
            List<Unit> targets = new();

            //store the threat list in a different container
            foreach (var refe in me.GetThreatManager().GetSortedThreatList())
            {
                Unit target = refe.GetVictim();

                if (refe.GetVictim().IsPlayer() &&
                    refe.GetVictim().IsAlive())
                    targets.Add(target);
            }

            //cut down to size if we have more than 3 targets
            targets.RandomResize(3);

            uint i = 0;

            foreach (var unit in targets)
                if (unit)
                {
                    FlameWreathTarget[i] = unit.GetGUID();
                    FWTargPosX[i] = unit.GetPositionX();
                    FWTargPosY[i] = unit.GetPositionY();
                    DoCast(unit, SpellIds.FlameWreath, new CastSpellExtraArgs(true));
                    ++i;
                }
        }

        private bool PlayerHasWeaponEquipped(Player player, uint itemEntry)
        {
            Item item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (item && item.GetEntry() == itemEntry)
                return true;

            return false;
        }
    }

    [Script]
    internal class water_elemental : ScriptedAI
    {
        public water_elemental(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(2000 + (RandomHelper.Rand32() % 3000)),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Waterbolt);
                                    task.Repeat(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
                                });
        }

        public override void JustEngagedWith(Unit who)
        {
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }
}