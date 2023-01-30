// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.BattlePets;
using Game.Combat;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Groups;
using Game.Guilds;
using Game.Loots;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Dos;
using Game.Maps.Notifiers;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.IQuest;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Spells
{
    public partial class Spell
    {
        public void DoCreateItem(uint itemId, ItemContext context = 0, List<uint> bonusListIds = null)
        {
            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            uint newitemid = itemId;
            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(newitemid);

            if (pProto == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);

                return;
            }

            uint num_to_add = (uint)Damage;

            if (num_to_add < 1)
                num_to_add = 1;

            if (num_to_add > pProto.GetMaxStackSize())
                num_to_add = pProto.GetMaxStackSize();

            // this is bad, should be done using spell_loot_template (and conditions)

            // the chance of getting a perfect result
            float perfectCreateChance = 0.0f;
            // the resulting perfect Item if successful
            uint perfectItemType = itemId;

            // get perfection capability and chance
            if (SkillPerfectItems.CanCreatePerfectItem(player, SpellInfo.Id, ref perfectCreateChance, ref perfectItemType))
                if (RandomHelper.randChance(perfectCreateChance)) // if the roll succeeds...
                    newitemid = perfectItemType;                  // the perfect Item replaces the regular one

            // init items_count to 1, since 1 Item will be created regardless of specialization
            int items_count = 1;
            // the chance to create additional items
            float additionalCreateChance = 0.0f;
            // the maximum number of created additional items
            byte additionalMaxNum = 0;

            // get the chance and maximum number for creating extra items
            if (SkillExtraItems.CanCreateExtraItems(player, SpellInfo.Id, ref additionalCreateChance, ref additionalMaxNum))
                // roll with this chance till we roll not to create or we create the max num
                while (RandomHelper.randChance(additionalCreateChance) && items_count <= additionalMaxNum)
                    ++items_count;

            // really will be created more items
            num_to_add *= (uint)items_count;

            // can the player store the new Item?
            List<ItemPosCount> dest = new();
            uint no_space;
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, newitemid, num_to_add, out no_space);

            if (msg != InventoryResult.Ok)
            {
                // convert to possible store amount
                if (msg == InventoryResult.InvFull ||
                    msg == InventoryResult.ItemMaxCount)
                {
                    num_to_add -= no_space;
                }
                else
                {
                    // if not created by another reason from full inventory or unique items amount limitation
                    player.SendEquipError(msg, null, null, newitemid);

                    return;
                }
            }

            if (num_to_add != 0)
            {
                // create the new Item and store it
                Item pItem = player.StoreNewItem(dest, newitemid, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(newitemid), null, context, bonusListIds);

                // was it successful? return error if not
                if (pItem == null)
                {
                    player.SendEquipError(InventoryResult.ItemNotFound);

                    return;
                }

                // set the "Crafted by ..." property of the Item
                if (pItem.GetTemplate().HasSignature())
                    pItem.SetCreator(player.GetGUID());

                // send info to the client
                player.SendNewItem(pItem, num_to_add, true, true);

                if (pItem.GetQuality() > ItemQuality.Epic ||
                    (pItem.GetQuality() == ItemQuality.Epic && pItem.GetItemLevel(player) >= GuildConst.MinNewsItemLevel))
                {
                    Guild guild = player.GetGuild();

                    guild?.AddGuildNews(GuildNews.ItemCrafted, player.GetGUID(), 0, pProto.GetId());
                }

                // we succeeded in creating at least one Item, so a levelup is possible
                player.UpdateCraftSkill(SpellInfo);
            }
        }

        [SpellEffectHandler(SpellEffectName.None)]
        [SpellEffectHandler(SpellEffectName.Portal)]
        [SpellEffectHandler(SpellEffectName.BindSight)]
        [SpellEffectHandler(SpellEffectName.CallPet)]
        [SpellEffectHandler(SpellEffectName.PortalTeleport)]
        [SpellEffectHandler(SpellEffectName.Dodge)]
        [SpellEffectHandler(SpellEffectName.Evade)]
        [SpellEffectHandler(SpellEffectName.Weapon)]
        [SpellEffectHandler(SpellEffectName.Defense)]
        [SpellEffectHandler(SpellEffectName.SpellDefense)]
        [SpellEffectHandler(SpellEffectName.Language)]
        [SpellEffectHandler(SpellEffectName.Spawn)]
        [SpellEffectHandler(SpellEffectName.Stealth)]
        [SpellEffectHandler(SpellEffectName.Detect)]
        [SpellEffectHandler(SpellEffectName.ForceCriticalHit)]
        [SpellEffectHandler(SpellEffectName.Attack)]
        [SpellEffectHandler(SpellEffectName.ThreatAll)]
        [SpellEffectHandler(SpellEffectName.Effect112)]
        [SpellEffectHandler(SpellEffectName.TeleportGraveyard)]
        [SpellEffectHandler(SpellEffectName.Effect122)]
        [SpellEffectHandler(SpellEffectName.Effect175)]
        [SpellEffectHandler(SpellEffectName.Effect178)]
        private void EffectUnused()
        {
        }

        private void EffectResurrectNew()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (CorpseTarget == null &&
                UnitTarget == null)
                return;

            Player player = null;

            if (CorpseTarget)
                player = Global.ObjAccessor.FindPlayer(CorpseTarget.GetOwnerGUID());
            else if (UnitTarget)
                player = UnitTarget.ToPlayer();

            if (player == null ||
                player.IsAlive() ||
                !player.IsInWorld)
                return;

            if (player.IsResurrectRequested()) // already have one active request
                return;

            int health = Damage;
            int mana = EffectInfo.MiscValue;
            ExecuteLogEffectResurrect(EffectInfo.Effect, player);
            player.SetResurrectRequestData(_caster, (uint)health, (uint)mana, 0);
            SendResurrectRequest(player);
        }

        [SpellEffectHandler(SpellEffectName.Instakill)]
        private void EffectInstaKill()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive())
                return;

            if (UnitTarget.IsTypeId(TypeId.Player))
                if (UnitTarget.ToPlayer().GetCommandStatus(PlayerCommandStates.God))
                    return;

            if (_caster == UnitTarget) // prevent interrupt message
                Finish();

            SpellInstakillLog data = new();
            data.Target = UnitTarget.GetGUID();
            data.Caster = _caster.GetGUID();
            data.SpellID = SpellInfo.Id;
            _caster.SendMessageToSet(data, true);

            Unit.Kill(GetUnitCasterForEffectHandlers(), UnitTarget, false);
        }

        [SpellEffectHandler(SpellEffectName.EnvironmentalDamage)]
        private void EffectEnvironmentalDMG()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive())
                return;

            // CalcAbsorbResist already in Player::EnvironmentalDamage
            if (UnitTarget.IsTypeId(TypeId.Player))
            {
                UnitTarget.ToPlayer().EnvironmentalDamage(EnviromentalDamage.Fire, (uint)Damage);
            }
            else
            {
                Unit unitCaster = GetUnitCasterForEffectHandlers();
                DamageInfo damageInfo = new(unitCaster, UnitTarget, (uint)Damage, SpellInfo, SpellInfo.GetSchoolMask(), DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                Unit.CalcAbsorbResist(damageInfo);

                SpellNonMeleeDamage log = new(unitCaster, UnitTarget, SpellInfo, SpellVisual, SpellInfo.GetSchoolMask(), CastId);
                log.Damage = damageInfo.GetDamage();
                log.OriginalDamage = (uint)Damage;
                log.Absorb = damageInfo.GetAbsorb();
                log.Resist = damageInfo.GetResist();

                unitCaster?.SendSpellNonMeleeDamageLog(log);
            }
        }

        [SpellEffectHandler(SpellEffectName.SchoolDamage)]
        private void EffectSchoolDmg()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (UnitTarget != null &&
                UnitTarget.IsAlive())
            {
                bool apply_direct_bonus = true;

                // Meteor like spells (divided Damage to targets)
                if (SpellInfo.HasAttribute(SpellCustomAttributes.ShareDamage))
                {
                    long count = GetUnitTargetCountForEffect(EffectInfo.EffectIndex);

                    // divide to all targets
                    if (count != 0)
                        Damage /= (int)count;
                }

                Unit unitCaster = GetUnitCasterForEffectHandlers();

                if (unitCaster != null && apply_direct_bonus)
                {
                    uint bonus = unitCaster.SpellDamageBonusDone(UnitTarget, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect, EffectInfo);
                    Damage = (int)(bonus + (uint)(bonus * Variance));
                    Damage = (int)UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect);
                }

                EffectDamage += Damage;
            }
        }

        [SpellEffectHandler(SpellEffectName.Dummy)]
        private void EffectDummy()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null &&
                GameObjTarget == null &&
                ItemTarget == null &&
                CorpseTarget == null)
                return;

            // pet Auras
            if (_caster.GetTypeId() == TypeId.Player)
            {
                PetAura petSpell = Global.SpellMgr.GetPetAura(SpellInfo.Id, (byte)EffectInfo.EffectIndex);

                if (petSpell != null)
                {
                    _caster.ToPlayer().AddPetAura(petSpell);

                    return;
                }
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectDummy({1})", SpellInfo.Id, EffectInfo.EffectIndex);
            _caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)SpellInfo.Id | (int)(EffectInfo.EffectIndex << 24)), _caster, UnitTarget);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell)]
        [SpellEffectHandler(SpellEffectName.TriggerSpellWithValue)]
        private void EffectTriggerSpell()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget &&
                _effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            uint triggered_spell_id = EffectInfo.TriggerSpell;

            // @todo move those to spell scripts
            if (EffectInfo.Effect == SpellEffectName.TriggerSpell &&
                _effectHandleMode == SpellEffectHandleMode.LaunchTarget)
                // special cases
                switch (triggered_spell_id)
                {
                    // Demonic Empowerment -- succubus
                    case 54437:
                        {
                            UnitTarget.RemoveMovementImpairingAuras(true);
                            UnitTarget.RemoveAurasByType(AuraType.ModStalked);
                            UnitTarget.RemoveAurasByType(AuraType.ModStun);

                            // Cast Lesser Invisibility
                            UnitTarget.CastSpell(UnitTarget, 7870, new CastSpellExtraArgs(this));

                            return;
                        }
                    // Brittle Armor - (need add max stack of 24575 Brittle Armor)
                    case 29284:
                        {
                            // Brittle Armor
                            SpellInfo spell = Global.SpellMgr.GetSpellInfo(24575, GetCastDifficulty());

                            if (spell == null)
                                return;

                            for (uint j = 0; j < spell.StackAmount; ++j)
                                _caster.CastSpell(UnitTarget, spell.Id, new CastSpellExtraArgs(this));

                            return;
                        }
                    // Mercurial Shield - (need add max stack of 26464 Mercurial Shield)
                    case 29286:
                        {
                            // Mercurial Shield
                            SpellInfo spell = Global.SpellMgr.GetSpellInfo(26464, GetCastDifficulty());

                            if (spell == null)
                                return;

                            for (uint j = 0; j < spell.StackAmount; ++j)
                                _caster.CastSpell(UnitTarget, spell.Id, new CastSpellExtraArgs(this));

                            return;
                        }
                }

            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerSpell: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());

            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerSpell spell {0} tried to trigger unknown spell {1}", SpellInfo.Id, triggered_spell_id);

                return;
            }

            SpellCastTargets targets = new();

            if (_effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(SpellInfo))
                    return;

                targets.SetUnitTarget(UnitTarget);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Launch)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(SpellInfo) &&
                    EffectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation))
                    targets.SetDst(Targets);

                Unit target = Targets.GetUnitTarget();

                if (target != null)
                {
                    targets.SetUnitTarget(target);
                }
                else
                {
                    Unit unit = _caster.ToUnit();

                    if (unit != null)
                    {
                        targets.SetUnitTarget(unit);
                    }
                    else
                    {
                        GameObject go = _caster.ToGameObject();

                        if (go != null)
                            targets.SetGOTarget(go);
                    }
                }
            }

            TimeSpan delay = TimeSpan.Zero;

            if (EffectInfo.Effect == SpellEffectName.TriggerSpell)
                delay = TimeSpan.FromMilliseconds(EffectInfo.MiscValue);

            var caster = _caster;
            var originalCaster = _originalCasterGUID;
            var castItemGuid = CastItemGUID;
            var originalCastId = CastId;
            var triggerSpell = EffectInfo.TriggerSpell;
            var effect = EffectInfo.Effect;
            var value = Damage;
            var itemLevel = CastItemLevel;

            _caster.Events.AddEventAtOffset(() =>
                                             {
                                                 targets.Update(caster); // refresh pointers stored in targets

                                                 // original caster Guid only for GO cast
                                                 CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                                 args.SetOriginalCaster(originalCaster);
                                                 args.OriginalCastId = originalCastId;
                                                 args.OriginalCastItemLevel = itemLevel;

                                                 if (!castItemGuid.IsEmpty() &&
                                                     Global.SpellMgr.GetSpellInfo(triggerSpell, caster.GetMap().GetDifficultyID()).HasAttribute(SpellAttr2.RetainItemCast))
                                                 {
                                                     Player triggeringAuraCaster = caster?.ToPlayer();

                                                     if (triggeringAuraCaster != null)
                                                         args.CastItem = triggeringAuraCaster.GetItemByGuid(castItemGuid);
                                                 }

                                                 // set basepoints for trigger with value effect
                                                 if (effect == SpellEffectName.TriggerSpellWithValue)
                                                     for (int i = 0; i < SpellConst.MaxEffects; ++i)
                                                         args.AddSpellMod(SpellValueMod.BasePoint0 + i, value);

                                                 caster.CastSpell(targets, triggerSpell, args);
                                             },
                                             delay);
        }

        [SpellEffectHandler(SpellEffectName.TriggerMissile)]
        [SpellEffectHandler(SpellEffectName.TriggerMissileSpellWithValue)]
        private void EffectTriggerMissileSpell()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget &&
                _effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = EffectInfo.TriggerSpell;

            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerMissileSpell: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());

            if (spellInfo == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.EffectTriggerMissileSpell spell {0} tried to trigger unknown spell {1}", SpellInfo.Id, triggered_spell_id);

                return;
            }

            SpellCastTargets targets = new();

            if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                if (!spellInfo.NeedsToBeTriggeredByCaster(SpellInfo))
                    return;

                targets.SetUnitTarget(UnitTarget);
            }
            else //if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (spellInfo.NeedsToBeTriggeredByCaster(SpellInfo) &&
                    EffectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return;

                if (spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.DestLocation))
                    targets.SetDst(Targets);

                Unit unit = _caster.ToUnit();

                if (unit != null)
                {
                    targets.SetUnitTarget(unit);
                }
                else
                {
                    GameObject go = _caster.ToGameObject();

                    if (go != null)
                        targets.SetGOTarget(go);
                }
            }

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetOriginalCaster(_originalCasterGUID);
            args.SetTriggeringSpell(this);

            // set basepoints for trigger with value effect
            if (EffectInfo.Effect == SpellEffectName.TriggerMissileSpellWithValue)
                for (int i = 0; i < SpellConst.MaxEffects; ++i)
                    args.AddSpellMod(SpellValueMod.BasePoint0 + i, Damage);

            // original caster Guid only for GO cast
            _caster.CastSpell(targets, spellInfo.Id, args);
        }

        [SpellEffectHandler(SpellEffectName.ForceCast)]
        [SpellEffectHandler(SpellEffectName.ForceCastWithValue)]
        [SpellEffectHandler(SpellEffectName.ForceCast2)]
        private void EffectForceCast()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            uint triggered_spell_id = EffectInfo.TriggerSpell;

            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectForceCast: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

                return;
            }

            // normal case
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());

            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, "Spell.EffectForceCast of spell {0}: triggering unknown spell Id {1}", SpellInfo.Id, triggered_spell_id);

                return;
            }

            if (EffectInfo.Effect == SpellEffectName.ForceCast &&
                Damage != 0)
                switch (SpellInfo.Id)
                {
                    case 52588: // Skeletal Gryphon Escape
                    case 48598: // Ride Flamebringer Cue
                        UnitTarget.RemoveAura((uint)Damage);

                        break;
                    case 52463: // Hide In Mine Car
                    case 52349: // Overtake
                        {
                            CastSpellExtraArgs args1 = new(TriggerCastFlags.FullMask);
                            args1.SetOriginalCaster(_originalCasterGUID);
                            args1.SetTriggeringSpell(this);
                            args1.AddSpellMod(SpellValueMod.BasePoint0, Damage);
                            UnitTarget.CastSpell(UnitTarget, spellInfo.Id, args1);

                            return;
                        }
                }

            switch (spellInfo.Id)
            {
                case 72298: // Malleable Goo Summon
                    UnitTarget.CastSpell(UnitTarget,
                                         spellInfo.Id,
                                         new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                             .SetOriginalCaster(_originalCasterGUID)
                                             .SetTriggeringSpell(this));

                    return;
            }

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetTriggeringSpell(this);

            // set basepoints for trigger with value effect
            if (EffectInfo.Effect == SpellEffectName.ForceCastWithValue)
                for (int i = 0; i < SpellConst.MaxEffects; ++i)
                    args.AddSpellMod(SpellValueMod.BasePoint0 + i, Damage);

            UnitTarget.CastSpell(_caster, spellInfo.Id, args);
        }

        [SpellEffectHandler(SpellEffectName.TriggerSpell2)]
        private void EffectTriggerRitualOfSummoning()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint triggered_spell_id = EffectInfo.TriggerSpell;

            if (triggered_spell_id == 0)
            {
                Log.outWarn(LogFilter.Spells, $"Spell::EffectTriggerRitualOfSummoning: Spell {SpellInfo.Id} [EffectIndex: {EffectInfo.EffectIndex}] does not have triggered spell.");

                return;
            }

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(triggered_spell_id, GetCastDifficulty());

            if (spellInfo == null)
            {
                Log.outError(LogFilter.Spells, $"EffectTriggerRitualOfSummoning of spell {SpellInfo.Id}: triggering unknown spell Id {triggered_spell_id}");

                return;
            }

            Finish();

            _caster.CastSpell((Unit)null, spellInfo.Id, new CastSpellExtraArgs().SetTriggeringSpell(this));
        }

        private void CalculateJumpSpeeds(SpellEffectInfo effInfo, float dist, out float speedXY, out float speedZ)
        {
            Unit unitCaster = GetUnitCasterForEffectHandlers();
            float runSpeed = unitCaster.IsControlledByPlayer() ? SharedConst.playerBaseMoveSpeed[(int)UnitMoveType.Run] : SharedConst.baseMoveSpeed[(int)UnitMoveType.Run];
            Creature creature = unitCaster.ToCreature();

            if (creature != null)
                runSpeed *= creature.GetCreatureTemplate().SpeedRun;

            float multiplier = effInfo.Amplitude;

            if (multiplier <= 0.0f)
                multiplier = 1.0f;

            speedXY = Math.Min(runSpeed * 3.0f * multiplier, Math.Max(28.0f, unitCaster.GetSpeed(UnitMoveType.Run) * 4.0f));

            float duration = dist / speedXY;
            float durationSqr = duration * duration;
            float minHeight = effInfo.MiscValue != 0 ? effInfo.MiscValue / 10.0f : 0.5f;      // Lower bound is blizzlike
            float maxHeight = effInfo.MiscValueB != 0 ? effInfo.MiscValueB / 10.0f : 1000.0f; // Upper bound is unknown
            float height;

            if (durationSqr < minHeight * 8 / MotionMaster.GRAVITY)
                height = minHeight;
            else if (durationSqr > maxHeight * 8 / MotionMaster.GRAVITY)
                height = maxHeight;
            else
                height = (float)(MotionMaster.GRAVITY * durationSqr / 8);

            speedZ = MathF.Sqrt((float)(2 * MotionMaster.GRAVITY * height));
        }

        [SpellEffectHandler(SpellEffectName.Jump)]
        private void EffectJump()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            if (UnitTarget == null)
                return;

            float speedXY, speedZ;
            CalculateJumpSpeeds(EffectInfo, unitCaster.GetExactDist2d(UnitTarget), out speedXY, out speedZ);
            JumpArrivalCastArgs arrivalCast = new();
            arrivalCast.SpellId = EffectInfo.TriggerSpell;
            arrivalCast.Target = UnitTarget.GetGUID();
            unitCaster.GetMotionMaster().MoveJump(UnitTarget, speedXY, speedZ, EventId.Jump, false, arrivalCast);
        }

        [SpellEffectHandler(SpellEffectName.JumpDest)]
        private void EffectJumpDest()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            if (!Targets.HasDst())
                return;

            float speedXY, speedZ;
            CalculateJumpSpeeds(EffectInfo, unitCaster.GetExactDist2d(DestTarget), out speedXY, out speedZ);
            JumpArrivalCastArgs arrivalCast = new();
            arrivalCast.SpellId = EffectInfo.TriggerSpell;
            unitCaster.GetMotionMaster().MoveJump(DestTarget, speedXY, speedZ, EventId.Jump, !Targets.GetObjectTargetGUID().IsEmpty(), arrivalCast);
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnits)]
        private void EffectTeleportUnits()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                UnitTarget.IsInFlight())
                return;

            // If not exist _data for dest location - return
            if (!Targets.HasDst())
            {
                Log.outError(LogFilter.Spells, "Spell.EffectTeleportUnits - does not have a destination for spellId {0}.", SpellInfo.Id);

                return;
            }

            // Init dest coordinates
            WorldLocation targetDest = new(DestTarget);

            if (targetDest.GetMapId() == 0xFFFFFFFF)
                targetDest.SetMapId(UnitTarget.GetMapId());

            if (targetDest.GetOrientation() == 0 &&
                Targets.GetUnitTarget())
                targetDest.SetOrientation(Targets.GetUnitTarget().GetOrientation());

            Player player = UnitTarget.ToPlayer();

            if (player != null)
            {
                // Custom loading screen
                uint customLoadingScreenId = (uint)EffectInfo.MiscValue;

                if (customLoadingScreenId != 0)
                    player.SendPacket(new CustomLoadScreen(SpellInfo.Id, customLoadingScreenId));
            }

            if (targetDest.GetMapId() == UnitTarget.GetMapId())
            {
                UnitTarget.NearTeleportTo(targetDest, UnitTarget == _caster);
            }
            else if (player != null)
            {
                player.TeleportTo(targetDest, UnitTarget == _caster ? TeleportToOptions.Spell : 0);
            }
            else
            {
                Log.outError(LogFilter.Spells, "Spell.EffectTeleportUnits - spellId {0} attempted to teleport creature to a different map.", SpellInfo.Id);

                return;
            }
        }

        [SpellEffectHandler(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen)]
        private void EffectTeleportUnitsWithVisualLoadingScreen()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            // If not exist _data for dest location - return
            if (!Targets.HasDst())
            {
                Log.outError(LogFilter.Spells, $"Spell::EffectTeleportUnitsWithVisualLoadingScreen - does not have a destination for spellId {SpellInfo.Id}.");

                return;
            }

            // Init dest coordinates
            WorldLocation targetDest = new(DestTarget);

            if (targetDest.GetMapId() == 0xFFFFFFFF)
                targetDest.SetMapId(UnitTarget.GetMapId());

            if (targetDest.GetOrientation() == 0 &&
                Targets.GetUnitTarget())
                targetDest.SetOrientation(Targets.GetUnitTarget().GetOrientation());

            if (EffectInfo.MiscValueB != 0)
            {
                Player playerTarget = UnitTarget.ToPlayer();

                playerTarget?.SendPacket(new SpellVisualLoadScreen(EffectInfo.MiscValueB, EffectInfo.MiscValue));
            }

            UnitTarget.Events.AddEventAtOffset(new DelayedSpellTeleportEvent(UnitTarget, targetDest, UnitTarget == _caster ? TeleportToOptions.Spell : 0, SpellInfo.Id), TimeSpan.FromMilliseconds(EffectInfo.MiscValue));
        }

        [SpellEffectHandler(SpellEffectName.ApplyAura)]
        [SpellEffectHandler(SpellEffectName.ApplyAuraOnPet)]
        private void EffectApplyAura()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (SpellAura == null ||
                UnitTarget == null)
                return;

            // register Target/effect on aura
            AuraApplication aurApp = SpellAura.GetApplicationOfTarget(UnitTarget.GetGUID());

            if (aurApp == null)
                aurApp = UnitTarget._CreateAuraApplication(SpellAura, 1u << (int)EffectInfo.EffectIndex);
            else
                aurApp.UpdateApplyEffectMask(aurApp.GetEffectsToApply() | (1u << (int)EffectInfo.EffectIndex), false);
        }

        [SpellEffectHandler(SpellEffectName.UnlearnSpecialization)]
        private void EffectUnlearnSpecialization()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();
            uint spellToUnlearn = EffectInfo.TriggerSpell;

            player.RemoveSpell(spellToUnlearn);

            Log.outDebug(LogFilter.Spells, "Spell: Player {0} has unlearned spell {1} from NpcGUID: {2}", player.GetGUID().ToString(), spellToUnlearn, _caster.GetGUID().ToString());
        }

        [SpellEffectHandler(SpellEffectName.PowerDrain)]
        private void EffectPowerDrain()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (EffectInfo.MiscValue < 0 ||
                EffectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType powerType = (PowerType)EffectInfo.MiscValue;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                UnitTarget.GetPowerType() != powerType ||
                Damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            // add spell Damage bonus
            if (unitCaster != null)
            {
                uint bonus = unitCaster.SpellDamageBonusDone(UnitTarget, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect, EffectInfo);
                Damage = (int)(bonus + (uint)(bonus * Variance));
                Damage = (int)UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect);
            }

            int newDamage = -(UnitTarget.ModifyPower(powerType, -Damage));

            // Don't restore from self drain
            float gainMultiplier = 0.0f;

            if (unitCaster != null &&
                unitCaster != UnitTarget)
            {
                gainMultiplier = EffectInfo.CalcValueMultiplier(unitCaster, this);
                int gain = (int)(newDamage * gainMultiplier);

                unitCaster.EnergizeBySpell(unitCaster, SpellInfo, gain, powerType);
            }

            ExecuteLogEffectTakeTargetPower(EffectInfo.Effect, UnitTarget, powerType, (uint)newDamage, gainMultiplier);
        }

        [SpellEffectHandler(SpellEffectName.SendEvent)]
        private void EffectSendEvent()
        {
            // we do not handle a flag dropping or clicking on flag in Battlegroundby sendevent system
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget &&
                _effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            WorldObject target = null;

            // call events for object Target if present
            if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                if (UnitTarget != null)
                    target = UnitTarget;
                else if (GameObjTarget != null)
                    target = GameObjTarget;
                else if (CorpseTarget != null)
                    target = CorpseTarget;
            }
            else // if (effectHandleMode == SpellEffectHandleMode.Hit)
            {
                // let's prevent executing effect handler twice in case when spell effect is capable of targeting an object
                // this check was requested by scripters, but it has some downsides:
                // now it's impossible to script (using sEventScripts) a cast which misses all targets
                // or to have an ability to script the moment spell hits dest (in a case when there are object targets present)
                if (EffectInfo.GetProvidedTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask))
                    return;

                // some spells have no Target entries in dbc and they use focus Target
                if (_focusObject != null)
                    target = _focusObject;
                // @todo there should be a possibility to pass dest Target to event script
            }

            Log.outDebug(LogFilter.Spells, "Spell ScriptStart {0} for spellid {1} in EffectSendEvent ", EffectInfo.MiscValue, SpellInfo.Id);

            GameEvents.Trigger((uint)EffectInfo.MiscValue, _caster, target);
        }

        [SpellEffectHandler(SpellEffectName.PowerBurn)]
        private void EffectPowerBurn()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (EffectInfo.MiscValue < 0 ||
                EffectInfo.MiscValue >= (int)PowerType.Max)
                return;

            PowerType powerType = (PowerType)EffectInfo.MiscValue;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                UnitTarget.GetPowerType() != powerType ||
                Damage < 0)
                return;

            int newDamage = -(UnitTarget.ModifyPower(powerType, -Damage));

            // NO - Not a typo - EffectPowerBurn uses effect value Multiplier - not effect Damage Multiplier
            float dmgMultiplier = EffectInfo.CalcValueMultiplier(GetUnitCasterForEffectHandlers(), this);

            // add log _data before multiplication (need power amount, not Damage)
            ExecuteLogEffectTakeTargetPower(EffectInfo.Effect, UnitTarget, powerType, (uint)newDamage, 0.0f);

            newDamage = (int)(newDamage * dmgMultiplier);

            EffectDamage += newDamage;
        }

        [SpellEffectHandler(SpellEffectName.Heal)]
        private void EffectHeal()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                Damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            // Skip if _originalCaster not available
            if (unitCaster == null)
                return;

            int addhealth = Damage;

            // Vessel of the Naaru (Vial of the Sunwell trinket)
            ///@todo: move this to scripts
            if (SpellInfo.Id == 45064)
            {
                // Amount of heal - depends from stacked Holy Energy
                int damageAmount = 0;
                AuraEffect aurEff = unitCaster.GetAuraEffect(45062, 0);

                if (aurEff != null)
                {
                    damageAmount += aurEff.GetAmount();
                    unitCaster.RemoveAurasDueToSpell(45062);
                }

                addhealth += damageAmount;
            }
            // Death Pact - return pct of max health to caster
            else if (SpellInfo.SpellFamilyName == SpellFamilyNames.Deathknight &&
                     SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00080000u))
            {
                addhealth = (int)unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, (uint)unitCaster.CountPctFromMaxHealth(Damage), DamageEffectType.Heal, EffectInfo);
            }
            else
            {
                uint bonus = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, (uint)addhealth, DamageEffectType.Heal, EffectInfo);
                addhealth = (int)(bonus + (uint)(bonus * Variance));
            }

            addhealth = (int)UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, (uint)addhealth, DamageEffectType.Heal);

            // Remove Grievious bite if fully healed
            if (UnitTarget.HasAura(48920) &&
                ((uint)(UnitTarget.GetHealth() + (ulong)addhealth) >= UnitTarget.GetMaxHealth()))
                UnitTarget.RemoveAura(48920);

            EffectHealing += addhealth;
        }

        [SpellEffectHandler(SpellEffectName.HealPct)]
        private void EffectHealPct()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                Damage < 0)
                return;

            uint heal = (uint)UnitTarget.CountPctFromMaxHealth(Damage);
            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster)
            {
                heal = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, heal, DamageEffectType.Heal, EffectInfo);
                heal = UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, heal, DamageEffectType.Heal);
            }

            EffectHealing += (int)heal;
        }

        [SpellEffectHandler(SpellEffectName.HealMechanical)]
        private void EffectHealMechanical()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                Damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            uint heal = (uint)Damage;

            if (unitCaster)
                heal = unitCaster.SpellHealingBonusDone(UnitTarget, SpellInfo, heal, DamageEffectType.Heal, EffectInfo);

            heal += (uint)(heal * Variance);

            if (unitCaster)
                heal = UnitTarget.SpellHealingBonusTaken(unitCaster, SpellInfo, heal, DamageEffectType.Heal);

            EffectHealing += (int)heal;
        }

        [SpellEffectHandler(SpellEffectName.HealthLeech)]
        private void EffectHealthLeech()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive() ||
                Damage < 0)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();
            uint bonus = 0;

            unitCaster?.SpellDamageBonusDone(UnitTarget, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect, EffectInfo);

            Damage = (int)(bonus + (uint)(bonus * Variance));

            if (unitCaster != null)
                Damage = (int)UnitTarget.SpellDamageBonusTaken(unitCaster, SpellInfo, (uint)Damage, DamageEffectType.SpellDirect);

            Log.outDebug(LogFilter.Spells, "HealthLeech :{0}", Damage);

            float healMultiplier = EffectInfo.CalcValueMultiplier(unitCaster, this);

            EffectDamage += Damage;

            DamageInfo damageInfo = new(unitCaster, UnitTarget, (uint)Damage, SpellInfo, SpellInfo.GetSchoolMask(), DamageEffectType.Direct, WeaponAttackType.BaseAttack);
            Unit.CalcAbsorbResist(damageInfo);
            uint absorb = damageInfo.GetAbsorb();
            Damage -= (int)absorb;

            // get max possible Damage, don't Count overkill for heal
            uint healthGain = (uint)(-UnitTarget.GetHealthGain(-Damage) * healMultiplier);

            if (unitCaster != null &&
                unitCaster.IsAlive())
            {
                healthGain = unitCaster.SpellHealingBonusDone(unitCaster, SpellInfo, healthGain, DamageEffectType.Heal, EffectInfo);
                healthGain = unitCaster.SpellHealingBonusTaken(unitCaster, SpellInfo, healthGain, DamageEffectType.Heal);

                HealInfo healInfo = new(unitCaster, unitCaster, healthGain, SpellInfo, SpellSchoolMask);
                unitCaster.HealBySpell(healInfo);
            }
        }

        [SpellEffectHandler(SpellEffectName.CreateItem)]
        private void EffectCreateItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            DoCreateItem(EffectInfo.ItemType, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
            ExecuteLogEffectCreateItem(EffectInfo.Effect, EffectInfo.ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateLoot)]
        private void EffectCreateItem2()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            ItemContext context = SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None;

            // Pick a random Item from spell_loot_template
            if (SpellInfo.IsLootCrafting())
            {
                player.AutoStoreLoot(SpellInfo.Id, LootStorage.Spell, context, false, true);
                player.UpdateCraftSkill(SpellInfo);
            }
            else // If there's no random loot entries for this spell, pick the Item associated with this spell
            {
                uint itemId = EffectInfo.ItemType;

                if (itemId != 0)
                    DoCreateItem(itemId, context);
            }

            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.CreateRandomItem)]
        private void EffectCreateRandomItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            // create some random items
            player.AutoStoreLoot(SpellInfo.Id, LootStorage.Spell, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
            // @todo ExecuteLogEffectCreateItem(i, GetEffect(i].ItemType);
        }

        [SpellEffectHandler(SpellEffectName.PersistentAreaAura)]
        private void EffectPersistentAA()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            // only handle at last effect
            for (uint i = EffectInfo.EffectIndex + 1; i < SpellInfo.GetEffects().Count; ++i)
                if (SpellInfo.GetEffect(i).IsEffect(SpellEffectName.PersistentAreaAura))
                    return;

            Cypher.Assert(DynObjAura == null);

            float radius = EffectInfo.CalcRadius(unitCaster);

            // Caster not in world, might be spell triggered from aura removal
            if (!unitCaster.IsInWorld)
                return;

            DynamicObject dynObj = new(false);

            if (!dynObj.CreateDynamicObject(unitCaster.GetMap().GenerateLowGuid(HighGuid.DynamicObject), unitCaster, SpellInfo, DestTarget, radius, DynamicObjectType.AreaSpell, SpellVisual))
            {
                dynObj.Dispose();

                return;
            }

            AuraCreateInfo createInfo = new(CastId, SpellInfo, GetCastDifficulty(), SpellConst.MaxEffectMask, dynObj);
            createInfo.SetCaster(unitCaster);
            createInfo.SetBaseAmount(SpellValue.EffectBasePoints);
            createInfo.SetCastItem(CastItemGUID, CastItemEntry, CastItemLevel);

            Aura aura = Aura.TryCreate(createInfo);

            if (aura != null)
            {
                DynObjAura = aura.ToDynObjAura();
                DynObjAura._RegisterForTargets();
            }
            else
            {
                return;
            }

            Cypher.Assert(DynObjAura.GetDynobjOwner());
            DynObjAura._ApplyEffectForTargets(EffectInfo.EffectIndex);
        }

        [SpellEffectHandler(SpellEffectName.Energize)]
        private void EffectEnergize()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                UnitTarget == null)
                return;

            if (!UnitTarget.IsAlive())
                return;

            if (EffectInfo.MiscValue < 0 ||
                EffectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType power = (PowerType)EffectInfo.MiscValue;

            if (UnitTarget.GetMaxPower(power) == 0)
                return;

            // Some level depends spells
            switch (SpellInfo.Id)
            {
                case 24571: // Blood Fury
                            // Instantly increases your rage by ${(300-10*$max(0,$PL-60))/10}.
                    Damage -= 10 * (int)Math.Max(0, Math.Min(30, unitCaster.GetLevel() - 60));

                    break;
                case 24532: // Burst of Energy
                            // Instantly increases your energy by ${60-4*$max(0,$min(15,$PL-60))}.
                    Damage -= 4 * (int)Math.Max(0, Math.Min(15, unitCaster.GetLevel() - 60));

                    break;
                case 67490: // Runic Mana Injector (mana gain increased by 25% for engineers - 3.2.0 patch change)
                    {
                        Player player = unitCaster.ToPlayer();

                        if (player != null)
                            if (player.HasSkill(SkillType.Engineering))
                                Damage = MathFunctions.AddPct(Damage, 25);

                        break;
                    }
                default:
                    break;
            }

            unitCaster.EnergizeBySpell(UnitTarget, SpellInfo, Damage, power);
        }

        [SpellEffectHandler(SpellEffectName.EnergizePct)]
        private void EffectEnergizePct()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                UnitTarget == null)
                return;

            if (!UnitTarget.IsAlive())
                return;

            if (EffectInfo.MiscValue < 0 ||
                EffectInfo.MiscValue >= (byte)PowerType.Max)
                return;

            PowerType power = (PowerType)EffectInfo.MiscValue;
            uint maxPower = (uint)UnitTarget.GetMaxPower(power);

            if (maxPower == 0)
                return;

            int gain = (int)MathFunctions.CalculatePct(maxPower, Damage);
            unitCaster.EnergizeBySpell(UnitTarget, SpellInfo, gain, power);
        }

        [SpellEffectHandler(SpellEffectName.OpenLock)]
        private void EffectOpenLock()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
            {
                Log.outDebug(LogFilter.Spells, "WORLD: Open Lock - No Player Caster!");

                return;
            }

            Player player = _caster.ToPlayer();

            uint lockId;
            ObjectGuid guid;

            // Get lockId
            if (GameObjTarget != null)
            {
                GameObjectTemplate goInfo = GameObjTarget.GetGoInfo();

                if (goInfo.GetNoDamageImmune() != 0 &&
                    player.HasUnitFlag(UnitFlags.Immune))
                    return;

                // Arathi Basin banner opening. // @todo Verify correctness of this check
                if ((goInfo.type == GameObjectTypes.Button && goInfo.Button.noDamageImmune != 0) ||
                    (goInfo.type == GameObjectTypes.Goober && goInfo.Goober.requireLOS != 0))
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    Battleground bg = player.GetBattleground();

                    if (bg)
                    {
                        bg.EventPlayerClickedOnFlag(player, GameObjTarget);

                        return;
                    }
                }
                else if (goInfo.type == GameObjectTypes.CapturePoint)
                {
                    GameObjTarget.AssaultCapturePoint(player);

                    return;
                }
                else if (goInfo.type == GameObjectTypes.FlagStand)
                {
                    //CanUseBattlegroundObject() already called in CheckCast()
                    // in Battlegroundcheck
                    Battleground bg = player.GetBattleground();

                    if (bg)
                    {
                        if (bg.GetTypeID(true) == BattlegroundTypeId.EY)
                            bg.EventPlayerClickedOnFlag(player, GameObjTarget);

                        return;
                    }
                }
                else if (goInfo.type == GameObjectTypes.NewFlag)
                {
                    GameObjTarget.Use(player);

                    return;
                }
                else if (SpellInfo.Id == 1842 &&
                         GameObjTarget.GetGoInfo().type == GameObjectTypes.Trap &&
                         GameObjTarget.GetOwner() != null)
                {
                    GameObjTarget.SetLootState(LootState.JustDeactivated);

                    return;
                }
                // @todo Add script for spell 41920 - Filling, becouse server it freze when use this spell
                // handle outdoor pvp object opening, return true if go was registered for handling
                // these objects must have been spawned by outdoorpvp!
                else if (GameObjTarget.GetGoInfo().type == GameObjectTypes.Goober &&
                         Global.OutdoorPvPMgr.HandleOpenGo(player, GameObjTarget))
                {
                    return;
                }

                lockId = goInfo.GetLockId();
                guid = GameObjTarget.GetGUID();
            }
            else if (ItemTarget != null)
            {
                lockId = ItemTarget.GetTemplate().GetLockID();
                guid = ItemTarget.GetGUID();
            }
            else
            {
                Log.outDebug(LogFilter.Spells, "WORLD: Open Lock - No GameObject/Item Target!");

                return;
            }

            SkillType skillId = SkillType.None;
            int reqSkillValue = 0;
            int skillValue = 0;

            SpellCastResult res = CanOpenLock(EffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

            if (res != SpellCastResult.SpellCastOk)
            {
                SendCastResult(res);

                return;
            }

            if (GameObjTarget != null)
            {
                GameObjTarget.Use(player);
            }
            else if (ItemTarget != null)
            {
                ItemTarget.SetItemFlag(ItemFieldFlags.Unlocked);
                ItemTarget.SetState(ItemUpdateState.Changed, ItemTarget.GetOwner());
            }

            // not allow use skill grow at Item base open
            if (CastItem == null &&
                skillId != SkillType.None)
            {
                // update skill if really known
                uint pureSkillValue = player.GetPureSkillValue(skillId);

                if (pureSkillValue != 0)
                {
                    if (GameObjTarget != null)
                    {
                        // Allow one skill-up until respawned
                        if (!GameObjTarget.IsInSkillupList(player.GetGUID()) &&
                            player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue, 1, GameObjTarget))
                            GameObjTarget.AddToSkillupList(player.GetGUID());
                    }
                    else if (ItemTarget != null)
                    {
                        // Do one skill-up
                        player.UpdateGatherSkill(skillId, pureSkillValue, (uint)reqSkillValue);
                    }
                }
            }

            ExecuteLogEffectOpenLock(EffectInfo.Effect, GameObjTarget != null ? GameObjTarget : (WorldObject)ItemTarget);
        }

        [SpellEffectHandler(SpellEffectName.SummonChangeItem)]
        private void EffectSummonChangeItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
                return;

            Player player = _caster.ToPlayer();

            // applied only to using Item
            if (CastItem == null)
                return;

            // ... only to Item in own inventory/bank/equip_slot
            if (CastItem.GetOwnerGUID() != player.GetGUID())
                return;

            uint newitemid = EffectInfo.ItemType;

            if (newitemid == 0)
                return;

            ushort pos = CastItem.GetPos();

            Item pNewItem = Item.CreateItem(newitemid, 1, CastItem.GetContext(), player);

            if (pNewItem == null)
                return;

            for (var j = EnchantmentSlot.Perm; j <= EnchantmentSlot.Temp; ++j)
                if (CastItem.GetEnchantmentId(j) != 0)
                    pNewItem.SetEnchantment(j, CastItem.GetEnchantmentId(j), CastItem.GetEnchantmentDuration(j), (uint)CastItem.GetEnchantmentCharges(j));

            if (CastItem._itemData.Durability < CastItem._itemData.MaxDurability)
            {
                double lossPercent = 1 - CastItem._itemData.Durability / CastItem._itemData.MaxDurability;
                player.DurabilityLoss(pNewItem, lossPercent);
            }

            if (player.IsInventoryPos(pos))
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = player.CanStoreItem(CastItem.GetBagSlot(), CastItem.GetSlot(), dest, pNewItem, true);

                if (msg == InventoryResult.Ok)
                {
                    player.DestroyItem(CastItem.GetBagSlot(), CastItem.GetSlot(), true);

                    // prevent crash at access and unexpected charges counting with Item update queue corrupt
                    if (CastItem == Targets.GetItemTarget())
                        Targets.SetItemTarget(null);

                    CastItem = null;
                    CastItemGUID.Clear();
                    CastItemEntry = 0;
                    CastItemLevel = -1;

                    player.StoreItem(dest, pNewItem, true);
                    player.SendNewItem(pNewItem, 1, true, false);
                    player.ItemAddedQuestCheck(newitemid, 1);

                    return;
                }
            }
            else if (Player.IsBankPos(pos))
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = player.CanBankItem(CastItem.GetBagSlot(), CastItem.GetSlot(), dest, pNewItem, true);

                if (msg == InventoryResult.Ok)
                {
                    player.DestroyItem(CastItem.GetBagSlot(), CastItem.GetSlot(), true);

                    // prevent crash at access and unexpected charges counting with Item update queue corrupt
                    if (CastItem == Targets.GetItemTarget())
                        Targets.SetItemTarget(null);

                    CastItem = null;
                    CastItemGUID.Clear();
                    CastItemEntry = 0;
                    CastItemLevel = -1;

                    player.BankItem(dest, pNewItem, true);

                    return;
                }
            }
            else if (Player.IsEquipmentPos(pos))
            {
                ushort dest;

                player.DestroyItem(CastItem.GetBagSlot(), CastItem.GetSlot(), true);

                InventoryResult msg = player.CanEquipItem(CastItem.GetSlot(), out dest, pNewItem, true);

                if (msg == InventoryResult.Ok ||
                    msg == InventoryResult.ClientLockedOut)
                {
                    if (msg == InventoryResult.ClientLockedOut)
                        dest = EquipmentSlot.MainHand;

                    // prevent crash at access and unexpected charges counting with Item update queue corrupt
                    if (CastItem == Targets.GetItemTarget())
                        Targets.SetItemTarget(null);

                    CastItem = null;
                    CastItemGUID.Clear();
                    CastItemEntry = 0;
                    CastItemLevel = -1;

                    player.EquipItem(dest, pNewItem, true);
                    player.AutoUnequipOffhandIfNeed();
                    player.SendNewItem(pNewItem, 1, true, false);
                    player.ItemAddedQuestCheck(newitemid, 1);

                    return;
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.Proficiency)]
        private void EffectProficiency()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
                return;

            Player p_target = _caster.ToPlayer();

            uint subClassMask = (uint)SpellInfo.EquippedItemSubClassMask;

            if (SpellInfo.EquippedItemClass == ItemClass.Weapon &&
                !Convert.ToBoolean(p_target.GetWeaponProficiency() & subClassMask))
            {
                p_target.AddWeaponProficiency(subClassMask);
                p_target.SendProficiency(ItemClass.Weapon, p_target.GetWeaponProficiency());
            }

            if (SpellInfo.EquippedItemClass == ItemClass.Armor &&
                !Convert.ToBoolean(p_target.GetArmorProficiency() & subClassMask))
            {
                p_target.AddArmorProficiency(subClassMask);
                p_target.SendProficiency(ItemClass.Armor, p_target.GetArmorProficiency());
            }
        }

        [SpellEffectHandler(SpellEffectName.Summon)]
        private void EffectSummonType()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint entry = (uint)EffectInfo.MiscValue;

            if (entry == 0)
                return;

            SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(EffectInfo.MiscValueB);

            if (properties == null)
            {
                Log.outError(LogFilter.Spells, "EffectSummonType: Unhandled summon Type {0}", EffectInfo.MiscValueB);

                return;
            }

            WorldObject caster = _caster;

            if (_originalCaster)
                caster = _originalCaster;

            ObjectGuid privateObjectOwner = caster.GetGUID();

            if (!properties.GetFlags().HasAnyFlag(SummonPropertiesFlags.OnlyVisibleToSummoner | SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
                privateObjectOwner = ObjectGuid.Empty;

            if (caster.IsPrivateObject())
                privateObjectOwner = caster.GetPrivateObjectOwner();

            if (properties.GetFlags().HasFlag(SummonPropertiesFlags.OnlyVisibleToSummonerGroup))
                if (caster.IsPlayer() &&
                    _originalCaster.ToPlayer().GetGroup())
                    privateObjectOwner = caster.ToPlayer().GetGroup().GetGUID();

            int duration = SpellInfo.CalcDuration(caster);

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            TempSummon summon = null;

            // determine how many units should be summoned
            uint numSummons;

            // some spells need to summon many units, for those spells number of summons is stored in effect value
            // however so far noone found a generic check to find all of those (there's no related _data in summonproperties.dbc
            // and in spell attributes, possibly we need to add a table for those)
            // so here's a list of MiscValueB values, which is currently most generic check
            switch (EffectInfo.MiscValueB)
            {
                case 64:
                case 61:
                case 1101:
                case 66:
                case 648:
                case 2301:
                case 1061:
                case 1261:
                case 629:
                case 181:
                case 715:
                case 1562:
                case 833:
                case 1161:
                case 713:
                    numSummons = (uint)(Damage > 0 ? Damage : 1);

                    break;
                default:
                    numSummons = 1;

                    break;
            }

            switch (properties.Control)
            {
                case SummonCategory.Wild:
                case SummonCategory.Ally:
                case SummonCategory.Unk:
                    if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup))
                    {
                        SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                        break;
                    }

                    switch (properties.Title)
                    {
                        case SummonTitle.Pet:
                        case SummonTitle.Guardian:
                        case SummonTitle.Runeblade:
                        case SummonTitle.Minion:
                            SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                            break;
                        // Summons a vehicle, but doesn't Force anyone to enter it (see SUMMON_CATEGORY_VEHICLE)
                        case SummonTitle.Vehicle:
                        case SummonTitle.Mount:
                            {
                                if (unitCaster == null)
                                    return;

                                summon = unitCaster.GetMap().SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id);

                                break;
                            }
                        case SummonTitle.LightWell:
                        case SummonTitle.Totem:
                            {
                                if (unitCaster == null)
                                    return;

                                summon = unitCaster.GetMap().SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                                if (summon == null ||
                                    !summon.IsTotem())
                                    return;

                                if (Damage != 0) // if not spell info, DB values used
                                {
                                    summon.SetMaxHealth((uint)Damage);
                                    summon.SetHealth((uint)Damage);
                                }

                                break;
                            }
                        case SummonTitle.Companion:
                            {
                                if (unitCaster == null)
                                    return;

                                summon = unitCaster.GetMap().SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                                if (summon == null ||
                                    !summon.HasUnitTypeMask(UnitTypeMask.Minion))
                                    return;

                                summon.SetImmuneToAll(true);

                                break;
                            }
                        default:
                            {
                                float radius = EffectInfo.CalcRadius();

                                TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

                                for (uint count = 0; count < numSummons; ++count)
                                {
                                    Position pos;

                                    if (count == 0)
                                        pos = DestTarget;
                                    else
                                        // randomize position for multiple summons
                                        pos = caster.GetRandomPoint(DestTarget, radius);

                                    summon = caster.GetMap().SummonCreature(entry, pos, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                                    if (summon == null)
                                        continue;

                                    summon.SetTempSummonType(summonType);

                                    if (properties.Control == SummonCategory.Ally)
                                        summon.SetOwnerGUID(caster.GetGUID());

                                    ExecuteLogEffectSummonObject(EffectInfo.Effect, summon);
                                }

                                return;
                            }
                    } //switch

                    break;
                case SummonCategory.Pet:
                    SummonGuardian(EffectInfo, entry, properties, numSummons, privateObjectOwner);

                    break;
                case SummonCategory.Puppet:
                    {
                        if (unitCaster == null)
                            return;

                        summon = unitCaster.GetMap().SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                        break;
                    }
                case SummonCategory.Vehicle:
                    {
                        if (unitCaster == null)
                            return;

                        // Summoning spells (usually triggered by npc_spellclick) that spawn a vehicle and that cause the clicker
                        // to cast a ride vehicle spell on the summoned unit.
                        summon = unitCaster.GetMap().SummonCreature(entry, DestTarget, properties, (uint)duration, unitCaster, SpellInfo.Id);

                        if (summon == null ||
                            !summon.IsVehicle())
                            return;

                        // The spell that this effect will trigger. It has SPELL_AURA_CONTROL_VEHICLE
                        uint spellId = SharedConst.VehicleSpellRideHardcoded;
                        int basePoints = EffectInfo.CalcValue();

                        if (basePoints > SharedConst.MaxVehicleSeats)
                        {
                            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)basePoints, GetCastDifficulty());

                            if (spellInfo != null &&
                                spellInfo.HasAura(AuraType.ControlVehicle))
                                spellId = spellInfo.Id;
                        }

                        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                        args.SetTriggeringSpell(this);

                        // if we have small value, it indicates Seat position
                        if (basePoints > 0 &&
                            basePoints < SharedConst.MaxVehicleSeats)
                            args.AddSpellMod(SpellValueMod.BasePoint0, basePoints);

                        unitCaster.CastSpell(summon, spellId, args);

                        break;
                    }
            }

            if (summon != null)
            {
                summon.SetCreatorGUID(caster.GetGUID());
                ExecuteLogEffectSummonObject(EffectInfo.Effect, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.LearnSpell)]
        private void EffectLearnSpell()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            if (!UnitTarget.IsTypeId(TypeId.Player))
            {
                if (UnitTarget.IsPet())
                    EffectLearnPetSpell();

                return;
            }

            Player player = UnitTarget.ToPlayer();

            if (CastItem != null &&
                EffectInfo.TriggerSpell == 0)
                foreach (var itemEffect in CastItem.GetEffects())
                {
                    if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                        continue;

                    bool dependent = false;

                    var speciesEntry = BattlePetMgr.GetBattlePetSpeciesBySpell((uint)itemEffect.SpellID);

                    if (speciesEntry != null)
                    {
                        player.GetSession().GetBattlePetMgr().AddPet(speciesEntry.Id, BattlePetMgr.SelectPetDisplay(speciesEntry), BattlePetMgr.RollPetBreed(speciesEntry.Id), BattlePetMgr.GetDefaultPetQuality(speciesEntry.Id));
                        // If the spell summons a battle pet, we fake that it has been learned and the battle pet is added
                        // marking as dependent prevents saving the spell to database (intended)
                        dependent = true;
                    }

                    player.LearnSpell((uint)itemEffect.SpellID, dependent);
                }

            if (EffectInfo.TriggerSpell != 0)
            {
                player.LearnSpell(EffectInfo.TriggerSpell, false);
                Log.outDebug(LogFilter.Spells, $"Spell: {player.GetGUID()} has learned spell {EffectInfo.TriggerSpell} from {_caster.GetGUID()}");
            }
        }

        [SpellEffectHandler(SpellEffectName.Dispel)]
        private void EffectDispel()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            // Create dispel mask by dispel Type
            uint dispel_type = (uint)EffectInfo.MiscValue;
            uint dispelMask = SpellInfo.GetDispelMask((DispelType)dispel_type);

            List<DispelableAura> dispelList = UnitTarget.GetDispellableAuraList(_caster, dispelMask, TargetMissInfo == SpellMissInfo.Reflect);

            if (dispelList.Empty())
                return;

            int remaining = dispelList.Count;

            // Ok if exist some buffs for dispel try dispel it
            List<DispelableAura> successList = new();

            DispelFailed dispelFailed = new();
            dispelFailed.CasterGUID = _caster.GetGUID();
            dispelFailed.VictimGUID = UnitTarget.GetGUID();
            dispelFailed.SpellID = SpellInfo.Id;

            // dispel N = Damage buffs (or while exist buffs for dispel)
            for (int count = 0; count < Damage && remaining > 0;)
            {
                // Random select buff for dispel
                var dispelableAura = dispelList[RandomHelper.IRand(0, remaining - 1)];

                if (dispelableAura.RollDispel())
                {
                    var successAura = successList.Find(dispelAura =>
                                                       {
                                                           if (dispelAura.GetAura().GetId() == dispelableAura.GetAura().GetId() &&
                                                               dispelAura.GetAura().GetCaster() == dispelableAura.GetAura().GetCaster())
                                                               return true;

                                                           return false;
                                                       });

                    byte dispelledCharges = 1;

                    if (dispelableAura.GetAura().GetSpellInfo().HasAttribute(SpellAttr1.DispelAllStacks))
                        dispelledCharges = dispelableAura.GetDispelCharges();

                    if (successAura == null)
                        successList.Add(new DispelableAura(dispelableAura.GetAura(), 0, dispelledCharges));
                    else
                        successAura.IncrementCharges();

                    if (!dispelableAura.DecrementCharge(dispelledCharges))
                    {
                        --remaining;
                        dispelList[remaining] = dispelableAura;
                    }
                }
                else
                {
                    dispelFailed.FailedSpells.Add(dispelableAura.GetAura().GetId());
                }

                ++count;
            }

            if (!dispelFailed.FailedSpells.Empty())
                _caster.SendMessageToSet(dispelFailed, true);

            if (successList.Empty())
                return;

            SpellDispellLog spellDispellLog = new();
            spellDispellLog.IsBreak = false; // TODO: use me
            spellDispellLog.IsSteal = false;

            spellDispellLog.TargetGUID = UnitTarget.GetGUID();
            spellDispellLog.CasterGUID = _caster.GetGUID();
            spellDispellLog.DispelledBySpellID = SpellInfo.Id;

            foreach (var dispelableAura in successList)
            {
                var dispellData = new SpellDispellData();
                dispellData.SpellID = dispelableAura.GetAura().GetId();
                dispellData.Harmful = false; // TODO: use me

                UnitTarget.RemoveAurasDueToSpellByDispel(dispelableAura.GetAura().GetId(), SpellInfo.Id, dispelableAura.GetAura().GetCasterGUID(), _caster, dispelableAura.GetDispelCharges());

                spellDispellLog.DispellData.Add(dispellData);
            }

            _caster.SendMessageToSet(spellDispellLog, true);

            CallScriptSuccessfulDispel(EffectInfo.EffectIndex);
        }

        [SpellEffectHandler(SpellEffectName.DualWield)]
        private void EffectDualWield()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            UnitTarget.SetCanDualWield(true);

            if (UnitTarget.IsTypeId(TypeId.Unit))
                UnitTarget.ToCreature().UpdateDamagePhysical(WeaponAttackType.OffAttack);
        }

        [SpellEffectHandler(SpellEffectName.Distract)]
        private void EffectDistract()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // Check for possible Target
            if (UnitTarget == null ||
                UnitTarget.IsEngaged())
                return;

            // Target must be OK to do this
            if (UnitTarget.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing))
                return;

            UnitTarget.GetMotionMaster().MoveDistract((uint)(Damage * Time.InMilliseconds), UnitTarget.GetAbsoluteAngle(DestTarget));
        }

        [SpellEffectHandler(SpellEffectName.Pickpocket)]
        private void EffectPickPocket()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            Creature creature = UnitTarget?.ToCreature();

            if (creature == null)
                return;

            if (creature.CanGeneratePickPocketLoot())
            {
                creature.StartPickPocketRefillTimer();

                creature.Loot = new Loot(creature.GetMap(), creature.GetGUID(), LootType.Pickpocketing, null);
                uint lootid = creature.GetCreatureTemplate().PickPocketId;

                if (lootid != 0)
                    creature.Loot.FillLoot(lootid, LootStorage.Pickpocketing, player, true);

                // Generate extra money for pick pocket loot
                uint a = RandomHelper.URand(0, creature.GetLevel() / 2);
                uint b = RandomHelper.URand(0, player.GetLevel() / 2);
                creature.Loot.Gold = (uint)(10 * (a + b) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
            }
            else if (creature.Loot != null)
            {
                if (creature.Loot.Loot_type == LootType.Pickpocketing &&
                    creature.Loot.IsLooted())
                    player.SendLootError(creature.Loot.GetGUID(), creature.GetGUID(), LootError.AlreadPickPocketed);

                return;
            }

            player.SendLoot(creature.Loot);
        }

        [SpellEffectHandler(SpellEffectName.AddFarsight)]
        private void EffectAddFarsight()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            float radius = EffectInfo.CalcRadius();
            int duration = SpellInfo.CalcDuration(_caster);

            // Caster not in world, might be spell triggered from aura removal
            if (!player.IsInWorld)
                return;

            DynamicObject dynObj = new(true);

            if (!dynObj.CreateDynamicObject(player.GetMap().GenerateLowGuid(HighGuid.DynamicObject), player, SpellInfo, DestTarget, radius, DynamicObjectType.FarsightFocus, SpellVisual))
                return;

            dynObj.SetDuration(duration);
            dynObj.SetCasterViewpoint();
        }

        [SpellEffectHandler(SpellEffectName.UntrainTalents)]
        private void EffectUntrainTalents()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                _caster.IsTypeId(TypeId.Player))
                return;

            ObjectGuid guid = _caster.GetGUID();

            if (!guid.IsEmpty()) // the trainer is the caster
                UnitTarget.ToPlayer().SendRespecWipeConfirm(guid, UnitTarget.ToPlayer().GetNextResetTalentsCost(), SpecResetType.Talents);
        }

        [SpellEffectHandler(SpellEffectName.TeleportUnitsFaceCaster)]
        private void EffectTeleUnitsFaceCaster()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            if (UnitTarget.IsInFlight())
                return;

            if (Targets.HasDst())
                UnitTarget.NearTeleportTo(DestTarget.GetPositionX(), DestTarget.GetPositionY(), DestTarget.GetPositionZ(), DestTarget.GetAbsoluteAngle(_caster), UnitTarget == _caster);
        }

        [SpellEffectHandler(SpellEffectName.SkillStep)]
        private void EffectLearnSkill()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget.IsTypeId(TypeId.Player))
                return;

            if (Damage < 1)
                return;

            uint skillid = (uint)EffectInfo.MiscValue;
            SkillRaceClassInfoRecord rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skillid, UnitTarget.GetRace(), UnitTarget.GetClass());

            if (rcEntry == null)
                return;

            SkillTiersEntry tier = Global.ObjectMgr.GetSkillTier(rcEntry.SkillTierID);

            if (tier == null)
                return;

            ushort skillval = UnitTarget.ToPlayer().GetPureSkillValue((SkillType)skillid);
            UnitTarget.ToPlayer().SetSkill(skillid, (uint)Damage, Math.Max(skillval, (ushort)1), tier.Value[Damage - 1]);
        }

        [SpellEffectHandler(SpellEffectName.PlayMovie)]
        private void EffectPlayMovie()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget.IsTypeId(TypeId.Player))
                return;

            uint movieId = (uint)EffectInfo.MiscValue;

            if (!CliDB.MovieStorage.ContainsKey(movieId))
                return;

            UnitTarget.ToPlayer().SendMovieStart(movieId);
        }

        [SpellEffectHandler(SpellEffectName.TradeSkill)]
        private void EffectTradeSkill()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
                return;
            // uint skillid =  GetEffect(i].MiscValue;
            // ushort skillmax = unitTarget.ToPlayer().(skillid);
            // _caster.ToPlayer().SetSkill(skillid, skillval?skillval:1, skillmax+75);
        }

        [SpellEffectHandler(SpellEffectName.EnchantItem)]
        private void EffectEnchantItemPerm()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (ItemTarget == null)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            // Handle vellums
            if (ItemTarget.IsVellum())
            {
                // destroy one vellum from stack
                uint count = 1;
                player.DestroyItemCount(ItemTarget, ref count, true);
                UnitTarget = player;
                // and add a scroll
                Damage = 1;
                DoCreateItem(EffectInfo.ItemType, SpellInfo.HasAttribute(SpellAttr0.IsTradeskill) ? ItemContext.TradeSkill : ItemContext.None);
                ItemTarget = null;
                Targets.SetItemTarget(null);
            }
            else
            {
                // do not increase skill if vellum used
                if (!(CastItem && CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost)))
                    player.UpdateCraftSkill(SpellInfo);

                uint enchant_id = (uint)EffectInfo.MiscValue;

                if (enchant_id == 0)
                    return;

                SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                if (pEnchant == null)
                    return;

                // Item can be in trade Slot and have owner diff. from caster
                Player item_owner = ItemTarget.GetOwner();

                if (item_owner == null)
                    return;

                if (item_owner != player &&
                    player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                    Log.outCommand(player.GetSession().GetAccountId(),
                                   "GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                                   player.GetName(),
                                   player.GetSession().GetAccountId(),
                                   ItemTarget.GetTemplate().GetName(),
                                   ItemTarget.GetEntry(),
                                   item_owner.GetName(),
                                   item_owner.GetSession().GetAccountId());

                // remove old enchanting before applying new if equipped
                item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Perm, false);

                ItemTarget.SetEnchantment(EnchantmentSlot.Perm, enchant_id, 0, 0, _caster.GetGUID());

                // add new enchanting if equipped
                item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Perm, true);

                item_owner.RemoveTradeableItem(ItemTarget);
                ItemTarget.ClearSoulboundTradeable(item_owner);
            }
        }

        [SpellEffectHandler(SpellEffectName.EnchantItemPrismatic)]
        private void EffectEnchantItemPrismatic()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (ItemTarget == null)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            uint enchantId = (uint)EffectInfo.MiscValue;

            if (enchantId == 0)
                return;

            SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);

            if (enchant == null)
                return;

            // support only enchantings with add socket in this Slot
            {
                bool add_socket = false;

                for (byte i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                    if (enchant.Effect[i] == ItemEnchantmentType.PrismaticSocket)
                    {
                        add_socket = true;

                        break;
                    }

                if (!add_socket)
                {
                    Log.outError(LogFilter.Spells,
                                 "Spell.EffectEnchantItemPrismatic: attempt apply enchant spell {0} with SPELL_EFFECT_ENCHANT_ITEM_PRISMATIC ({1}) but without ITEM_ENCHANTMENT_TYPE_PRISMATIC_SOCKET ({2}), not suppoted yet.",
                                 SpellInfo.Id,
                                 SpellEffectName.EnchantItemPrismatic,
                                 ItemEnchantmentType.PrismaticSocket);

                    return;
                }
            }

            // Item can be in trade Slot and have owner diff. from caster
            Player item_owner = ItemTarget.GetOwner();

            if (item_owner == null)
                return;

            if (item_owner != player &&
                player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                Log.outCommand(player.GetSession().GetAccountId(),
                               "GM {0} (Account: {1}) enchanting(perm): {2} (Entry: {3}) for player: {4} (Account: {5})",
                               player.GetName(),
                               player.GetSession().GetAccountId(),
                               ItemTarget.GetTemplate().GetName(),
                               ItemTarget.GetEntry(),
                               item_owner.GetName(),
                               item_owner.GetSession().GetAccountId());

            // remove old enchanting before applying new if equipped
            item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Prismatic, false);

            ItemTarget.SetEnchantment(EnchantmentSlot.Prismatic, enchantId, 0, 0, _caster.GetGUID());

            // add new enchanting if equipped
            item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Prismatic, true);

            item_owner.RemoveTradeableItem(ItemTarget);
            ItemTarget.ClearSoulboundTradeable(item_owner);
        }

        [SpellEffectHandler(SpellEffectName.EnchantItemTemporary)]
        private void EffectEnchantItemTmp()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (ItemTarget == null)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            uint enchant_id = (uint)EffectInfo.MiscValue;

            if (enchant_id == 0)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have 0 as enchanting Id", SpellInfo.Id, EffectInfo.EffectIndex);

                return;
            }

            SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

            if (pEnchant == null)
            {
                Log.outError(LogFilter.Spells, "Spell {0} Effect {1} (SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY) have not existed enchanting Id {2}", SpellInfo.Id, EffectInfo.EffectIndex, enchant_id);

                return;
            }

            // select enchantment duration
            uint duration = (uint)pEnchant.Duration;

            // Item can be in trade Slot and have owner diff. from caster
            Player item_owner = ItemTarget.GetOwner();

            if (item_owner == null)
                return;

            if (item_owner != player &&
                player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                Log.outCommand(player.GetSession().GetAccountId(),
                               "GM {0} (Account: {1}) enchanting(temp): {2} (Entry: {3}) for player: {4} (Account: {5})",
                               player.GetName(),
                               player.GetSession().GetAccountId(),
                               ItemTarget.GetTemplate().GetName(),
                               ItemTarget.GetEntry(),
                               item_owner.GetName(),
                               item_owner.GetSession().GetAccountId());

            // remove old enchanting before applying new if equipped
            item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Temp, false);

            ItemTarget.SetEnchantment(EnchantmentSlot.Temp, enchant_id, duration * 1000, 0, _caster.GetGUID());

            // add new enchanting if equipped
            item_owner.ApplyEnchantment(ItemTarget, EnchantmentSlot.Temp, true);
        }

        [SpellEffectHandler(SpellEffectName.Tamecreature)]
        private void EffectTameCreature()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                !unitCaster.GetPetGUID().IsEmpty())
                return;

            if (UnitTarget == null)
                return;

            if (!UnitTarget.IsTypeId(TypeId.Unit))
                return;

            Creature creatureTarget = UnitTarget.ToCreature();

            if (creatureTarget.IsPet())
                return;

            if (unitCaster.GetClass() != Class.Hunter)
                return;

            // cast finish successfully
            Finish();

            Pet pet = unitCaster.CreateTamedPetFrom(creatureTarget, SpellInfo.Id);

            if (pet == null) // in very specific State like near world end/etc.
                return;

            // "kill" original creature
            creatureTarget.DespawnOrUnsummon();

            uint level = (creatureTarget.GetLevelForTarget(_caster) < (_caster.GetLevelForTarget(creatureTarget) - 5)) ? (_caster.GetLevelForTarget(creatureTarget) - 5) : creatureTarget.GetLevelForTarget(_caster);

            // prepare visual effect for levelup
            pet.SetLevel(level - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetLevel(level);

            // caster have pet now
            unitCaster.SetMinion(pet, true);

            if (_caster.IsTypeId(TypeId.Player))
            {
                pet.SavePetToDB(PetSaveMode.AsCurrent);
                unitCaster.ToPlayer().PetSpellInitialize();
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonPet)]
        private void EffectSummonPet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player owner = null;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster != null)
            {
                owner = unitCaster.ToPlayer();

                if (owner == null &&
                    unitCaster.IsTotem())
                    owner = unitCaster.GetCharmerOrOwnerPlayerOrPlayerItself();
            }

            uint petentry = (uint)EffectInfo.MiscValue;

            if (owner == null)
            {
                SummonPropertiesRecord properties = CliDB.SummonPropertiesStorage.LookupByKey(67);

                if (properties != null)
                    SummonGuardian(EffectInfo, petentry, properties, 1, ObjectGuid.Empty);

                return;
            }

            Pet OldSummon = owner.GetPet();

            // if pet requested Type already exist
            if (OldSummon != null)
            {
                if (petentry == 0 ||
                    OldSummon.GetEntry() == petentry)
                {
                    // pet in corpse State can't be summoned
                    if (OldSummon.IsDead())
                        return;

                    Cypher.Assert(OldSummon.GetMap() == owner.GetMap());

                    float px, py, pz;
                    owner.GetClosePoint(out px, out py, out pz, OldSummon.GetCombatReach());

                    OldSummon.NearTeleportTo(px, py, pz, OldSummon.GetOrientation());

                    if (owner.IsTypeId(TypeId.Player) &&
                        OldSummon.IsControlled())
                        owner.ToPlayer().PetSpellInitialize();

                    return;
                }

                if (owner.IsTypeId(TypeId.Player))
                    owner.ToPlayer().RemovePet(OldSummon, PetSaveMode.NotInSlot, false);
                else
                    return;
            }

            PetSaveMode? petSlot = null;

            if (petentry == 0)
                petSlot = (PetSaveMode)Damage;

            float x, y, z;
            owner.GetClosePoint(out x, out y, out z, owner.GetCombatReach());
            Pet pet = owner.SummonPet(petentry, petSlot, x, y, z, owner.Orientation, 0, out bool isNew);

            if (pet == null)
                return;

            if (isNew)
            {
                if (_caster.IsCreature())
                {
                    if (_caster.ToCreature().IsTotem())
                        pet.SetReactState(ReactStates.Aggressive);
                    else
                        pet.SetReactState(ReactStates.Defensive);
                }

                pet.SetCreatedBySpell(SpellInfo.Id);

                // generate new Name for summon pet
                string new_name = Global.ObjectMgr.GeneratePetName(petentry);

                if (!string.IsNullOrEmpty(new_name))
                    pet.SetName(new_name);
            }

            ExecuteLogEffectSummonObject(EffectInfo.Effect, pet);
        }

        [SpellEffectHandler(SpellEffectName.LearnPetSpell)]
        private void EffectLearnPetSpell()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            if (UnitTarget.ToPlayer() != null)
            {
                EffectLearnSpell();

                return;
            }

            Pet pet = UnitTarget.ToPet();

            if (pet == null)
                return;

            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(EffectInfo.TriggerSpell, Difficulty.None);

            if (learn_spellproto == null)
                return;

            pet.LearnSpell(learn_spellproto.Id);
            pet.SavePetToDB(PetSaveMode.AsCurrent);
            pet.GetOwner().PetSpellInitialize();
        }

        [SpellEffectHandler(SpellEffectName.AttackMe)]
        private void EffectTaunt()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            // this effect use before aura Taunt apply for prevent taunt already attacking Target
            // for spell as marked "non effective at already attacking Target"
            if (!UnitTarget ||
                UnitTarget.IsTotem())
            {
                SendCastResult(SpellCastResult.DontReport);

                return;
            }

            // Hand of Reckoning can hit some entities that can't have a threat list (including players' pets)
            if (SpellInfo.Id == 62124)
                if (!UnitTarget.IsPlayer() &&
                    UnitTarget.GetTarget() != unitCaster.GetGUID())
                    unitCaster.CastSpell(UnitTarget, 67485, true);

            if (!UnitTarget.CanHaveThreatList())
            {
                SendCastResult(SpellCastResult.DontReport);

                return;
            }

            ThreatManager mgr = UnitTarget.GetThreatManager();

            if (mgr.GetCurrentVictim() == unitCaster)
            {
                SendCastResult(SpellCastResult.DontReport);

                return;
            }

            if (!mgr.IsThreatListEmpty())
                // Set threat equal to highest threat currently on Target
                mgr.MatchUnitThreatToHighestThreat(unitCaster);
        }

        [SpellEffectHandler(SpellEffectName.WeaponDamageNoSchool)]
        [SpellEffectHandler(SpellEffectName.WeaponPercentDamage)]
        [SpellEffectHandler(SpellEffectName.WeaponDamage)]
        [SpellEffectHandler(SpellEffectName.NormalizedWeaponDmg)]
        private void EffectWeaponDmg()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive())
                return;

            // multiple weapon dmg effect workaround
            // execute only the last weapon Damage
            // and handle all effects at once
            for (var j = EffectInfo.EffectIndex + 1; j < SpellInfo.GetEffects().Count; ++j)
                switch (SpellInfo.GetEffect(j).Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    case SpellEffectName.NormalizedWeaponDmg:
                    case SpellEffectName.WeaponPercentDamage:
                        return; // we must calculate only at last weapon effect
                }

            // some spell specific modifiers
            float totalDamagePercentMod = 1.0f; // applied to final bonus+weapon Damage
            int fixed_bonus = 0;
            int spell_bonus = 0; // bonus specific for spell

            switch (SpellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Shaman:
                    {
                        // Skyshatter Harness Item set bonus
                        // Stormstrike
                        AuraEffect aurEff = unitCaster.IsScriptOverriden(SpellInfo, 5634);

                        if (aurEff != null)
                            unitCaster.CastSpell((WorldObject)null, 38430, new CastSpellExtraArgs(aurEff));

                        break;
                    }
            }

            bool normalized = false;
            float weaponDamagePercentMod = 1.0f;

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                        fixed_bonus += CalculateDamage(spellEffectInfo, UnitTarget);

                        break;
                    case SpellEffectName.NormalizedWeaponDmg:
                        fixed_bonus += CalculateDamage(spellEffectInfo, UnitTarget);
                        normalized = true;

                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        weaponDamagePercentMod = MathFunctions.CalculatePct(weaponDamagePercentMod, CalculateDamage(spellEffectInfo, UnitTarget));

                        break;
                    default:
                        break; // not weapon Damage effect, just skip
                }

            // if (addPctMods) { percent mods are added in Unit::CalculateDamage } else { percent mods are added in Unit::MeleeDamageBonusDone }
            // this distinction is neccessary to properly inform the client about his autoattack Damage values from Script_UnitDamage
            bool addPctMods = !SpellInfo.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers) && SpellSchoolMask.HasAnyFlag(SpellSchoolMask.Normal);

            if (addPctMods)
            {
                UnitMods unitMod;

                switch (AttackType)
                {
                    default:
                    case WeaponAttackType.BaseAttack:
                        unitMod = UnitMods.DamageMainHand;

                        break;
                    case WeaponAttackType.OffAttack:
                        unitMod = UnitMods.DamageOffHand;

                        break;
                    case WeaponAttackType.RangedAttack:
                        unitMod = UnitMods.DamageRanged;

                        break;
                }

                float weapon_total_pct = unitCaster.GetPctModifierValue(unitMod, UnitModifierPctType.Total);

                if (fixed_bonus != 0)
                    fixed_bonus = (int)(fixed_bonus * weapon_total_pct);

                if (spell_bonus != 0)
                    spell_bonus = (int)(spell_bonus * weapon_total_pct);
            }

            uint weaponDamage = unitCaster.CalculateDamage(AttackType, normalized, addPctMods);

            // Sequence is important
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                // We assume that a spell have at most one fixed_bonus
                // and at most one weaponDamagePercentMod
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    case SpellEffectName.NormalizedWeaponDmg:
                        weaponDamage += (uint)fixed_bonus;

                        break;
                    case SpellEffectName.WeaponPercentDamage:
                        weaponDamage = (uint)(weaponDamage * weaponDamagePercentMod);

                        break;
                    default:
                        break; // not weapon Damage effect, just skip
                }

            weaponDamage += (uint)spell_bonus;
            weaponDamage = (uint)(weaponDamage * totalDamagePercentMod);

            // prevent negative Damage
            weaponDamage = Math.Max(weaponDamage, 0);

            // Add melee Damage bonuses (also check for negative)
            weaponDamage = unitCaster.MeleeDamageBonusDone(UnitTarget, weaponDamage, AttackType, DamageEffectType.SpellDirect, SpellInfo, EffectInfo);
            EffectDamage += (int)UnitTarget.MeleeDamageBonusTaken(unitCaster, weaponDamage, AttackType, DamageEffectType.SpellDirect, SpellInfo);
        }

        [SpellEffectHandler(SpellEffectName.Threat)]
        private void EffectThreat()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                !unitCaster.IsAlive())
                return;

            if (UnitTarget == null)
                return;

            if (!UnitTarget.CanHaveThreatList())
                return;

            UnitTarget.GetThreatManager().AddThreat(unitCaster, Damage, SpellInfo, true);
        }

        [SpellEffectHandler(SpellEffectName.HealMaxHealth)]
        private void EffectHealMaxHealth()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive())
                return;

            int addhealth;

            // Damage == 0 - heal for caster max health
            if (Damage == 0)
                addhealth = (int)unitCaster.GetMaxHealth();
            else
                addhealth = (int)(UnitTarget.GetMaxHealth() - UnitTarget.GetHealth());

            EffectHealing += addhealth;
        }

        [SpellEffectHandler(SpellEffectName.InterruptCast)]
        private void EffectInterruptCast()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsAlive())
                return;

            // @todo not all spells that used this effect apply cooldown at school spells
            // also exist case: apply cooldown to interrupted cast only and to all spells
            // there is no CURRENT_AUTOREPEAT_SPELL spells that can be interrupted
            for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.AutoRepeat; ++i)
            {
                Spell spell = UnitTarget.GetCurrentSpell(i);

                if (spell != null)
                {
                    SpellInfo curSpellInfo = spell.SpellInfo;

                    // check if we can interrupt spell
                    if ((spell.GetState() == SpellState.Casting || (spell.GetState() == SpellState.Preparing && spell.GetCastTime() > 0.0f)) &&
                        curSpellInfo.CanBeInterrupted(_caster, UnitTarget))
                    {
                        int duration = SpellInfo.GetDuration();
                        duration = UnitTarget.ModSpellDuration(SpellInfo, UnitTarget, duration, false, 1u << (int)EffectInfo.EffectIndex);
                        UnitTarget.GetSpellHistory().LockSpellSchool(curSpellInfo.GetSchoolMask(), TimeSpan.FromMilliseconds(duration));
                        HitMask |= ProcFlagsHit.Interrupt;
                        SendSpellInterruptLog(UnitTarget, curSpellInfo.Id);
                        UnitTarget.InterruptSpell(i, false);
                    }
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectWild)]
        private void EffectSummonObjectWild()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            WorldObject target = _focusObject;

            if (target == null)
                target = _caster;

            float x, y, z, o;

            if (Targets.HasDst())
            {
                DestTarget.GetPosition(out x, out y, out z, out o);
            }
            else
            {
                _caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = target.GetOrientation();
            }

            Map map = target.GetMap();

            Position pos = new(x, y, z, o);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);

            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, _caster);

            int duration = SpellInfo.CalcDuration(_caster);

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(SpellInfo.Id);

            ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

            // Wild object not have owner and check clickable by players
            map.AddToMap(go);

            if (go.GetGoType() == GameObjectTypes.FlagDrop)
            {
                Player player = _caster.ToPlayer();

                if (player != null)
                {
                    Battleground bg = player.GetBattleground();

                    if (bg)
                        bg.SetDroppedFlagGUID(go.GetGUID(), bg.GetPlayerTeam(player.GetGUID()) == Team.Alliance ? TeamId.Horde : TeamId.Alliance);
                }
            }

            GameObject linkedTrap = go.GetLinkedTrap();

            if (linkedTrap)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, _caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(SpellInfo.Id);

                ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.ScriptEffect)]
        private void EffectScriptEffect()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            // @todo we must implement hunter pet summon at login there (spell 6962)
            /// @todo: move this to scripts
            switch (SpellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    {
                        switch (SpellInfo.Id)
                        {
                            case 45204: // Clone Me!
                                _caster.CastSpell(UnitTarget, (uint)Damage, new CastSpellExtraArgs(true));

                                break;
                            // Shadow Flame (All script effects, not just end ones to prevent player from dodging the last triggered spell)
                            case 22539:
                            case 22972:
                            case 22975:
                            case 22976:
                            case 22977:
                            case 22978:
                            case 22979:
                            case 22980:
                            case 22981:
                            case 22982:
                            case 22983:
                            case 22984:
                            case 22985:
                                {
                                    if (UnitTarget == null ||
                                        !UnitTarget.IsAlive())
                                        return;

                                    // Onyxia Scale Cloak
                                    if (UnitTarget.HasAura(22683))
                                        return;

                                    // Shadow Flame
                                    _caster.CastSpell(UnitTarget, 22682, new CastSpellExtraArgs(this));

                                    return;
                                }
                            // Mug Transformation
                            case 41931:
                                {
                                    if (!_caster.IsTypeId(TypeId.Player))
                                        return;

                                    byte bag = 19;
                                    byte slot = 0;
                                    Item item;

                                    while (bag != 0) // 256 = 0 due to var Type
                                    {
                                        item = _caster.ToPlayer().GetItemByPos(bag, slot);

                                        if (item != null &&
                                            item.GetEntry() == 38587)
                                            break;

                                        ++slot;

                                        if (slot == 39)
                                        {
                                            slot = 0;
                                            ++bag;
                                        }
                                    }

                                    if (bag != 0)
                                    {
                                        if (_caster.ToPlayer().GetItemByPos(bag, slot).GetCount() == 1) _caster.ToPlayer().RemoveItem(bag, slot, true);
                                        else _caster.ToPlayer().GetItemByPos(bag, slot).SetCount(_caster.ToPlayer().GetItemByPos(bag, slot).GetCount() - 1);

                                        // Spell 42518 (Braufest - Gratisprobe des Braufest herstellen)
                                        _caster.CastSpell(_caster, 42518, new CastSpellExtraArgs(this));

                                        return;
                                    }

                                    break;
                                }
                            // Brutallus - Burn
                            case 45141:
                            case 45151:
                                {
                                    //Workaround for Range ... should be global for every ScriptEffect
                                    float radius = EffectInfo.CalcRadius();

                                    if (UnitTarget != null &&
                                        UnitTarget.IsTypeId(TypeId.Player) &&
                                        UnitTarget.GetDistance(_caster) >= radius &&
                                        !UnitTarget.HasAura(46394) &&
                                        UnitTarget != _caster)
                                        UnitTarget.CastSpell(UnitTarget, 46394, new CastSpellExtraArgs(this));

                                    break;
                                }
                            // Emblazon Runeblade
                            case 51770:
                                {
                                    if (_originalCaster == null)
                                        return;

                                    _originalCaster.CastSpell(_originalCaster, (uint)Damage, new CastSpellExtraArgs(false));

                                    break;
                                }
                            // Summon Ghouls On Scarlet Crusade
                            case 51904:
                                {
                                    if (!Targets.HasDst())
                                        return;

                                    float radius = EffectInfo.CalcRadius();

                                    for (byte i = 0; i < 15; ++i)
                                        _caster.CastSpell(_caster.GetRandomPoint(DestTarget, radius), 54522, new CastSpellExtraArgs(this));

                                    break;
                                }
                            case 52173: // Coyote Spirit Despawn
                            case 60243: // Blood Parrot Despawn
                                if (UnitTarget.IsTypeId(TypeId.Unit) &&
                                    UnitTarget.ToCreature().IsSummon())
                                    UnitTarget.ToTempSummon().UnSummon();

                                return;
                            case 57347: // Retrieving (Wintergrasp RP-GG pickup spell)
                                {
                                    if (UnitTarget == null ||
                                        !UnitTarget.IsTypeId(TypeId.Unit) ||
                                        !_caster.IsTypeId(TypeId.Player))
                                        return;

                                    UnitTarget.ToCreature().DespawnOrUnsummon();

                                    return;
                                }
                            case 57349: // Drop RP-GG (Wintergrasp RP-GG at death drop spell)
                                {
                                    if (!_caster.IsTypeId(TypeId.Player))
                                        return;

                                    // Delete Item from inventory at death
                                    _caster.ToPlayer().DestroyItemCount((uint)Damage, 5, true);

                                    return;
                                }
                            case 58941: // Rock Shards
                                if (UnitTarget != null &&
                                    _originalCaster != null)
                                {
                                    for (uint i = 0; i < 3; ++i)
                                    {
                                        _originalCaster.CastSpell(UnitTarget, 58689, new CastSpellExtraArgs(true));
                                        _originalCaster.CastSpell(UnitTarget, 58692, new CastSpellExtraArgs(true));
                                    }

                                    if (_originalCaster.GetMap().GetDifficultyID() == Difficulty.None)
                                    {
                                        _originalCaster.CastSpell(UnitTarget, 58695, new CastSpellExtraArgs(true));
                                        _originalCaster.CastSpell(UnitTarget, 58696, new CastSpellExtraArgs(true));
                                    }
                                    else
                                    {
                                        _originalCaster.CastSpell(UnitTarget, 60883, new CastSpellExtraArgs(true));
                                        _originalCaster.CastSpell(UnitTarget, 60884, new CastSpellExtraArgs(true));
                                    }
                                }

                                return;
                            case 62482: // Grab Crate
                                {
                                    if (unitCaster == null)
                                        return;

                                    if (UnitTarget != null)
                                    {
                                        Unit seat = unitCaster.GetVehicleBase();

                                        if (seat != null)
                                        {
                                            Unit parent = seat.GetVehicleBase();

                                            if (parent != null)
                                            {
                                                // @todo a hack, range = 11, should after some Time cast, otherwise too far
                                                unitCaster.CastSpell(parent, 62496, new CastSpellExtraArgs(this));
                                                UnitTarget.CastSpell(parent, (uint)Damage, new CastSpellExtraArgs().SetTriggeringSpell(this)); // DIFFICULTY_NONE, so effect always valid
                                            }
                                        }
                                    }

                                    return;
                                }
                        }

                        break;
                    }
            }

            // normal DB scripted effect
            Log.outDebug(LogFilter.Spells, "Spell ScriptStart spellid {0} in EffectScriptEffect({1})", SpellInfo.Id, EffectInfo.EffectIndex);
            _caster.GetMap().ScriptsStart(ScriptsType.Spell, (uint)((int)SpellInfo.Id | (int)(EffectInfo.EffectIndex << 24)), _caster, UnitTarget);
        }

        [SpellEffectHandler(SpellEffectName.Sanctuary)]
        [SpellEffectHandler(SpellEffectName.Sanctuary2)]
        private void EffectSanctuary()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            if (UnitTarget.IsPlayer() &&
                !UnitTarget.GetMap().IsDungeon())
                // stop all pve combat for players outside dungeons, suppress pvp combat
                UnitTarget.CombatStop(false, false);
            else
                // in dungeons (or for nonplayers), reset this unit on all enemies' threat lists
                foreach (var pair in UnitTarget.GetThreatManager().GetThreatenedByMeList())
                    pair.Value.ScaleThreat(0.0f);

            // makes spells cast before this Time fizzle
            UnitTarget.LastSanctuaryTime = GameTime.GetGameTimeMS();
        }

        [SpellEffectHandler(SpellEffectName.Duel)]
        private void EffectDuel()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !_caster.IsTypeId(TypeId.Player) ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player caster = _caster.ToPlayer();
            Player target = UnitTarget.ToPlayer();

            // caster or Target already have requested Duel
            if (caster.Duel != null ||
                target.Duel != null ||
                target.GetSocial() == null ||
                target.GetSocial().HasIgnore(caster.GetGUID(), caster.GetSession().GetAccountGUID()))
                return;

            // Players can only fight a Duel in zones with this flag
            AreaTableRecord casterAreaEntry = CliDB.AreaTableStorage.LookupByKey(caster.GetAreaId());

            if (casterAreaEntry != null &&
                !casterAreaEntry.HasFlag(AreaFlags.AllowDuels))
            {
                SendCastResult(SpellCastResult.NoDueling); // Dueling isn't allowed here

                return;
            }

            AreaTableRecord targetAreaEntry = CliDB.AreaTableStorage.LookupByKey(target.GetAreaId());

            if (targetAreaEntry != null &&
                !targetAreaEntry.HasFlag(AreaFlags.AllowDuels))
            {
                SendCastResult(SpellCastResult.NoDueling); // Dueling isn't allowed here

                return;
            }

            //CREATE DUEL FLAG OBJECT
            Map map = caster.GetMap();

            Position pos = new()
            {
                X = caster.GetPositionX() + (UnitTarget.GetPositionX() - caster.GetPositionX()) / 2,
                Y = caster.GetPositionY() + (UnitTarget.GetPositionY() - caster.GetPositionY()) / 2,
                Z = caster.GetPositionZ(),
                Orientation = caster.GetOrientation()
            };

            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f));

            GameObject go = GameObject.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 0, GameObjectState.Ready);

            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, caster);

            go.SetFaction(caster.GetFaction());
            go.SetLevel(caster.GetLevel() + 1);
            int duration = SpellInfo.CalcDuration(caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(SpellInfo.Id);

            ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

            caster.AddGameObject(go);
            map.AddToMap(go);
            //END

            // Send request
            DuelRequested packet = new();
            packet.ArbiterGUID = go.GetGUID();
            packet.RequestedByGUID = caster.GetGUID();
            packet.RequestedByWowAccount = caster.GetSession().GetAccountGUID();

            caster.SendPacket(packet);
            target.SendPacket(packet);

            // create Duel-info
            bool isMounted = (GetSpellInfo().Id == 62875);
            caster.Duel = new DuelInfo(target, caster, isMounted);
            target.Duel = new DuelInfo(caster, caster, isMounted);

            caster.SetDuelArbiter(go.GetGUID());
            target.SetDuelArbiter(go.GetGUID());

            Global.ScriptMgr.ForEach<IPlayerOnDuelRequest>(p => p.OnDuelRequest(target, caster));
        }

        [SpellEffectHandler(SpellEffectName.Stuck)]
        private void EffectStuck()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!WorldConfig.GetBoolValue(WorldCfg.CastUnstuck))
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            Log.outDebug(LogFilter.Spells, "Spell Effect: Stuck");
            Log.outInfo(LogFilter.Spells, "Player {0} (Guid {1}) used auto-unstuck future at map {2} ({3}, {4}, {5})", player.GetName(), player.GetGUID().ToString(), player.GetMapId(), player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());

            if (player.IsInFlight())
                return;

            // if player is dead without death timer is teleported to graveyard, otherwise not apply the effect
            if (player.IsDead())
            {
                if (player.GetDeathTimer() == 0)
                    player.RepopAtGraveyard();

                return;
            }

            // the player dies if hearthstone is in cooldown, else the player is teleported to home
            if (player.GetSpellHistory().HasCooldown(8690))
            {
                player.KillSelf();

                return;
            }

            player.TeleportTo(player.GetHomebind(), TeleportToOptions.Spell);

            // Stuck spell trigger Hearthstone cooldown
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(8690, GetCastDifficulty());

            if (spellInfo == null)
                return;

            Spell spell = new(player, spellInfo, TriggerCastFlags.FullMask);
            spell.SendSpellCooldown();
        }

        [SpellEffectHandler(SpellEffectName.SummonPlayer)]
        private void EffectSummonPlayer()
        {
            // workaround - this effect should not use Target map
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().SendSummonRequestFrom(unitCaster);
        }

        [SpellEffectHandler(SpellEffectName.ActivateObject)]
        private void EffectActivateObject()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (GameObjTarget == null)
                return;

            GameObjTarget.ActivateObject((GameObjectActions)EffectInfo.MiscValue, EffectInfo.MiscValueB, _caster, SpellInfo.Id, (int)EffectInfo.EffectIndex);
        }

        [SpellEffectHandler(SpellEffectName.ApplyGlyph)]
        private void EffectApplyGlyph()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            List<uint> glyphs = player.GetGlyphs(player.GetActiveTalentGroup());
            int replacedGlyph = glyphs.Count;

            for (int i = 0; i < glyphs.Count; ++i)
            {
                List<uint> activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphs[i]);

                if (activeGlyphBindableSpells.Contains(Misc.SpellId))
                {
                    replacedGlyph = i;
                    player.RemoveAurasDueToSpell(CliDB.GlyphPropertiesStorage.LookupByKey(glyphs[i]).SpellID);

                    break;
                }
            }

            uint glyphId = (uint)EffectInfo.MiscValue;

            if (replacedGlyph < glyphs.Count)
            {
                if (glyphId != 0)
                    glyphs[replacedGlyph] = glyphId;
                else
                    glyphs.RemoveAt(replacedGlyph);
            }
            else if (glyphId != 0)
            {
                glyphs.Add(glyphId);
            }

            player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.ChangeGlyph);

            GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);

            if (glyphProperties != null)
                player.CastSpell(player, glyphProperties.SpellID, new CastSpellExtraArgs(this));

            ActiveGlyphs activeGlyphs = new();
            activeGlyphs.Glyphs.Add(new GlyphBinding(Misc.SpellId, (ushort)glyphId));
            activeGlyphs.IsFullUpdate = false;
            player.SendPacket(activeGlyphs);
        }

        [SpellEffectHandler(SpellEffectName.EnchantHeldItem)]
        private void EffectEnchantHeldItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            // this is only Item spell effect applied to main-hand weapon of Target player (players in area)
            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player item_owner = UnitTarget.ToPlayer();
            Item item = item_owner.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (item == null)
                return;

            // must be equipped
            if (!item.IsEquipped())
                return;

            if (EffectInfo.MiscValue != 0)
            {
                uint enchant_id = (uint)EffectInfo.MiscValue;
                int duration = SpellInfo.GetDuration(); //Try duration index first ..

                if (duration == 0)
                    duration = Damage; //+1;            //Base points after ..

                if (duration == 0)
                    duration = 10 * Time.InMilliseconds; //10 seconds for enchants which don't have listed duration

                if (SpellInfo.Id == 14792) // Venomhide Poison
                    duration = 5 * Time.Minute * Time.InMilliseconds;

                SpellItemEnchantmentRecord pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                if (pEnchant == null)
                    return;

                // Always go to temp enchantment Slot
                EnchantmentSlot slot = EnchantmentSlot.Temp;

                // Enchantment will not be applied if a different one already exists
                if (item.GetEnchantmentId(slot) != 0 &&
                    item.GetEnchantmentId(slot) != enchant_id)
                    return;

                // Apply the temporary enchantment
                item.SetEnchantment(slot, enchant_id, (uint)duration, 0, _caster.GetGUID());
                item_owner.ApplyEnchantment(item, slot, true);
            }
        }

        [SpellEffectHandler(SpellEffectName.Disenchant)]
        private void EffectDisEnchant()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player caster = _caster.ToPlayer();

            if (caster != null)
            {
                caster.UpdateCraftSkill(SpellInfo);
                ItemTarget.loot = new Loot(caster.GetMap(), ItemTarget.GetGUID(), LootType.Disenchanting, null);
                ItemTarget.loot.FillLoot(ItemTarget.GetDisenchantLoot(caster).Id, LootStorage.Disenchant, caster, true);
                caster.SendLoot(ItemTarget.loot);
            }

            // Item will be removed at disenchanting end
        }

        [SpellEffectHandler(SpellEffectName.Inebriate)]
        private void EffectInebriate()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();
            byte currentDrunk = player.GetDrunkValue();
            int drunkMod = Damage;

            if (currentDrunk + drunkMod > 100)
            {
                currentDrunk = 100;

                if (RandomHelper.randChance() < 25.0f)
                    player.CastSpell(player, 67468, new CastSpellExtraArgs().SetTriggeringSpell(this)); // Drunken Vomit
            }
            else
            {
                currentDrunk += (byte)drunkMod;
            }

            player.SetDrunkValue(currentDrunk, CastItem != null ? CastItem.GetEntry() : 0);
        }

        [SpellEffectHandler(SpellEffectName.FeedPet)]
        private void EffectFeedPet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            Item foodItem = ItemTarget;

            if (foodItem == null)
                return;

            Pet pet = player.GetPet();

            if (pet == null)
                return;

            if (!pet.IsAlive())
                return;

            ExecuteLogEffectDestroyItem(EffectInfo.Effect, foodItem.GetEntry());

            int pct;
            int levelDiff = (int)pet.GetLevel() - (int)foodItem.GetTemplate().GetBaseItemLevel();

            if (levelDiff >= 30)
                return;
            else if (levelDiff >= 20)
                pct = (int)12.5; // we can't pass double so keeping the cast here for future references
            else if (levelDiff >= 10)
                pct = 25;
            else
                pct = 50;

            uint count = 1;
            player.DestroyItemCount(foodItem, ref count, true);
            // @todo fix crash when a spell has two effects, both pointed at the same Item Target

            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.SetTriggeringSpell(this);
            args.AddSpellMod(SpellValueMod.BasePoint0, pct);
            _caster.CastSpell(pet, EffectInfo.TriggerSpell, args);
        }

        [SpellEffectHandler(SpellEffectName.DismissPet)]
        private void EffectDismissPet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsPet())
                return;

            Pet pet = UnitTarget.ToPet();

            ExecuteLogEffectUnsummonObject(EffectInfo.Effect, pet);
            pet.Remove(PetSaveMode.NotInSlot);
        }

        [SpellEffectHandler(SpellEffectName.SummonObjectSlot1)]
        private void EffectSummonObject()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            byte slot = (byte)(EffectInfo.Effect - SpellEffectName.SummonObjectSlot1);
            ObjectGuid guid = unitCaster.ObjectSlot[slot];

            if (!guid.IsEmpty())
            {
                GameObject obj = unitCaster.GetMap().GetGameObject(guid);

                if (obj != null)
                {
                    // Recast case - null spell Id to make Auras not be removed on object remove from world
                    if (SpellInfo.Id == obj.GetSpellId())
                        obj.SetSpellId(0);

                    unitCaster.RemoveGameObject(obj, true);
                }

                unitCaster.ObjectSlot[slot].Clear();
            }

            float x, y, z, o;

            // If dest location if present
            if (Targets.HasDst())
            {
                DestTarget.GetPosition(out x, out y, out z, out o);
            }
            // Summon in random point all other units if location present
            else
            {
                unitCaster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = unitCaster.GetOrientation();
            }

            Map map = _caster.GetMap();
            Position pos = new(x, y, z, o);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject((uint)EffectInfo.MiscValue, map, pos, rotation, 255, GameObjectState.Ready);

            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, _caster);

            go.SetFaction(unitCaster.GetFaction());
            go.SetLevel(unitCaster.GetLevel());
            int duration = SpellInfo.CalcDuration(_caster);
            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(SpellInfo.Id);
            unitCaster.AddGameObject(go);

            ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

            map.AddToMap(go);

            unitCaster.ObjectSlot[slot] = go.GetGUID();
        }

        [SpellEffectHandler(SpellEffectName.Resurrect)]
        private void EffectResurrect()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (CorpseTarget == null &&
                UnitTarget == null)
                return;

            Player player = null;

            if (CorpseTarget)
                player = Global.ObjAccessor.FindPlayer(CorpseTarget.GetOwnerGUID());
            else if (UnitTarget)
                player = UnitTarget.ToPlayer();

            if (player == null ||
                player.IsAlive() ||
                !player.IsInWorld)
                return;

            if (player.IsResurrectRequested()) // already have one active request
                return;

            uint health = (uint)player.CountPctFromMaxHealth(Damage);
            uint mana = (uint)MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), Damage);

            ExecuteLogEffectResurrect(EffectInfo.Effect, player);

            player.SetResurrectRequestData(_caster, health, mana, 0);
            SendResurrectRequest(player);
        }

        [SpellEffectHandler(SpellEffectName.AddExtraAttacks)]
        private void EffectAddExtraAttacks()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsAlive())
                return;

            UnitTarget.AddExtraAttacks((uint)Damage);

            ExecuteLogEffectExtraAttacks(EffectInfo.Effect, UnitTarget, (uint)Damage);
        }

        [SpellEffectHandler(SpellEffectName.Parry)]
        private void EffectParry()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (_caster.IsTypeId(TypeId.Player))
                _caster.ToPlayer().SetCanParry(true);
        }

        [SpellEffectHandler(SpellEffectName.Block)]
        private void EffectBlock()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (_caster.IsTypeId(TypeId.Player))
                _caster.ToPlayer().SetCanBlock(true);
        }

        [SpellEffectHandler(SpellEffectName.Leap)]
        private void EffectLeap()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                UnitTarget.IsInFlight())
                return;

            if (!Targets.HasDst())
                return;

            Position pos = DestTarget.GetPosition();
            UnitTarget.NearTeleportTo(pos.X, pos.Y, pos.Z, pos.Orientation, UnitTarget == _caster);
        }

        [SpellEffectHandler(SpellEffectName.Reputation)]
        private void EffectReputation()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            int repChange = Damage;

            int factionId = EffectInfo.MiscValue;

            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(factionId);

            if (factionEntry == null)
                return;

            repChange = player.CalculateReputationGain(ReputationSource.Spell, 0, repChange, factionId);

            player.GetReputationMgr().ModifyReputation(factionEntry, repChange);
        }

        [SpellEffectHandler(SpellEffectName.QuestComplete)]
        private void EffectQuestComplete()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            uint questId = (uint)EffectInfo.MiscValue;

            if (questId != 0)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);

                if (quest == null)
                    return;

                ushort logSlot = player.FindQuestSlot(questId);

                if (logSlot < SharedConst.MaxQuestLogSize)
                    player.AreaExploredOrEventHappens(questId);
                else if (quest.HasFlag(QuestFlags.Tracking)) // Check if the quest is used as a serverside flag.
                    player.SetRewardedQuest(questId);        // If so, set status to rewarded without broadcasting it to client.
            }
        }

        [SpellEffectHandler(SpellEffectName.ForceDeselect)]
        private void EffectForceDeselect()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            float dist = _caster.GetVisibilityRange();

            // clear focus
            PacketSenderOwning<BreakTarget> breakTarget = new();
            breakTarget.Data.UnitGUID = _caster.GetGUID();
            breakTarget.Data.Write();

            var notifierBreak = new MessageDistDelivererToHostile<PacketSenderOwning<BreakTarget>>(unitCaster, breakTarget, dist);
            Cell.VisitWorldObjects(_caster, notifierBreak, dist);

            // and selection
            PacketSenderOwning<ClearTarget> clearTarget = new();
            clearTarget.Data.Guid = _caster.GetGUID();
            clearTarget.Data.Write();
            var notifierClear = new MessageDistDelivererToHostile<PacketSenderOwning<ClearTarget>>(unitCaster, clearTarget, dist);
            Cell.VisitWorldObjects(_caster, notifierClear, dist);

            // we should also Force pets to remove us from current Target
            List<Unit> attackerSet = new();

            foreach (var unit in unitCaster.GetAttackers())
                if (unit.GetTypeId() == TypeId.Unit &&
                    !unit.CanHaveThreatList())
                    attackerSet.Add(unit);

            foreach (var unit in attackerSet)
                unit.AttackStop();
        }

        [SpellEffectHandler(SpellEffectName.SelfResurrect)]
        private void EffectSelfResurrect()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (player == null ||
                !player.IsInWorld ||
                player.IsAlive())
                return;

            uint health;
            int mana = 0;

            // flat case
            if (Damage < 0)
            {
                health = (uint)-Damage;
                mana = EffectInfo.MiscValue;
            }
            // percent case
            else
            {
                health = (uint)player.CountPctFromMaxHealth(Damage);

                if (player.GetMaxPower(PowerType.Mana) > 0)
                    mana = MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), Damage);
            }

            player.ResurrectPlayer(0.0f);

            player.SetHealth(health);
            player.SetPower(PowerType.Mana, mana);
            player.SetPower(PowerType.Rage, 0);
            player.SetFullPower(PowerType.Energy);
            player.SetPower(PowerType.Focus, 0);

            player.SpawnCorpseBones();
        }

        [SpellEffectHandler(SpellEffectName.Skinning)]
        private void EffectSkinning()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget.IsTypeId(TypeId.Unit))
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            Creature creature = UnitTarget.ToCreature();
            int targetLevel = (int)creature.GetLevelForTarget(_caster);

            SkillType skill = creature.GetCreatureTemplate().GetRequiredLootSkill();

            creature.SetUnitFlag3(UnitFlags3.AlreadySkinned);
            creature.SetDynamicFlag(UnitDynFlags.Lootable);
            Loot loot = new(creature.GetMap(), creature.GetGUID(), LootType.Skinning, null);
            creature._personalLoot[player.GetGUID()] = loot;
            loot.FillLoot(creature.GetCreatureTemplate().SkinLootId, LootStorage.Skinning, player, true);
            player.SendLoot(loot);

            if (skill == SkillType.Skinning)
            {
                int reqValue;

                if (targetLevel <= 10)
                    reqValue = 1;
                else if (targetLevel < 20)
                    reqValue = (targetLevel - 10) * 10;
                else if (targetLevel <= 73)
                    reqValue = targetLevel * 5;
                else if (targetLevel < 80)
                    reqValue = targetLevel * 10 - 365;
                else if (targetLevel <= 84)
                    reqValue = targetLevel * 5 + 35;
                else if (targetLevel <= 87)
                    reqValue = targetLevel * 15 - 805;
                else if (targetLevel <= 92)
                    reqValue = (targetLevel - 62) * 20;
                else if (targetLevel <= 104)
                    reqValue = targetLevel * 5 + 175;
                else if (targetLevel <= 107)
                    reqValue = targetLevel * 15 - 905;
                else if (targetLevel <= 112)
                    reqValue = (targetLevel - 72) * 20;
                else if (targetLevel <= 122)
                    reqValue = (targetLevel - 32) * 10;
                else
                    reqValue = 900;

                // TODO: Specialize skillid for each expansion
                // new db field?
                // tied to one of existing expansion fields in creature_template?

                // Double chances for elites
                _caster.ToPlayer().UpdateGatherSkill(skill, (uint)Damage, (uint)reqValue, (uint)(creature.IsElite() ? 2 : 1));
            }
        }

        [SpellEffectHandler(SpellEffectName.Charge)]
        private void EffectCharge()
        {
            if (UnitTarget == null)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (_effectHandleMode == SpellEffectHandleMode.LaunchTarget)
            {
                // charge changes fall Time
                if (unitCaster.IsPlayer())
                    unitCaster.ToPlayer().SetFallInformation(0, _caster.GetPositionZ());

                float speed = MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) ? SpellInfo.Speed : MotionMaster.SPEED_CHARGE;
                SpellEffectExtraData spellEffectExtraData = null;

                if (EffectInfo.MiscValueB != 0)
                {
                    spellEffectExtraData = new SpellEffectExtraData();
                    spellEffectExtraData.Target = UnitTarget.GetGUID();
                    spellEffectExtraData.SpellVisualId = (uint)EffectInfo.MiscValueB;
                }

                // Spell is not using explicit Target - no generated path
                if (_preGeneratedPath == null)
                {
                    Position pos = UnitTarget.GetFirstCollisionPosition(UnitTarget.GetCombatReach(), UnitTarget.GetRelativeAngle(_caster.GetPosition()));

                    if (MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) &&
                        SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                        speed = pos.GetExactDist(_caster) / speed;

                    unitCaster.GetMotionMaster().MoveCharge(pos.X, pos.Y, pos.Z, speed, EventId.Charge, false, UnitTarget, spellEffectExtraData);
                }
                else
                {
                    if (MathFunctions.fuzzyGt(SpellInfo.Speed, 0.0f) &&
                        SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                    {
                        Vector3 pos = _preGeneratedPath.GetActualEndPosition();
                        speed = new Position(pos.X, pos.Y, pos.Z).GetExactDist(_caster) / speed;
                    }

                    unitCaster.GetMotionMaster().MoveCharge(_preGeneratedPath, speed, UnitTarget, spellEffectExtraData);
                }
            }

            if (_effectHandleMode == SpellEffectHandleMode.HitTarget)
            {
                // not all charge effects used in negative spells
                if (!SpellInfo.IsPositive() &&
                    _caster.IsTypeId(TypeId.Player))
                    unitCaster.Attack(UnitTarget, true);

                if (EffectInfo.TriggerSpell != 0)
                    _caster.CastSpell(UnitTarget,
                                      EffectInfo.TriggerSpell,
                                      new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                          .SetOriginalCaster(_originalCasterGUID)
                                          .SetTriggeringSpell(this));
            }
        }

        [SpellEffectHandler(SpellEffectName.ChargeDest)]
        private void EffectChargeDest()
        {
            if (DestTarget == null)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (_effectHandleMode == SpellEffectHandleMode.Launch)
            {
                Position pos = DestTarget.GetPosition();

                if (!unitCaster.IsWithinLOS(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()))
                {
                    float angle = unitCaster.GetRelativeAngle(pos.X, pos.Y);
                    float dist = unitCaster.GetDistance(pos);
                    pos = unitCaster.GetFirstCollisionPosition(dist, angle);
                }

                unitCaster.GetMotionMaster().MoveCharge(pos.X, pos.Y, pos.Z);
            }
            else if (_effectHandleMode == SpellEffectHandleMode.Hit)
            {
                if (EffectInfo.TriggerSpell != 0)
                    _caster.CastSpell(DestTarget,
                                      EffectInfo.TriggerSpell,
                                      new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                          .SetOriginalCaster(_originalCasterGUID)
                                          .SetTriggeringSpell(this));
            }
        }

        [SpellEffectHandler(SpellEffectName.KnockBack)]
        [SpellEffectHandler(SpellEffectName.KnockBackDest)]
        private void EffectKnockBack()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            if (_caster.GetAffectingPlayer())
            {
                Creature creatureTarget = UnitTarget.ToCreature();

                if (creatureTarget != null)
                    if (creatureTarget.IsWorldBoss() ||
                        creatureTarget.IsDungeonBoss())
                        return;
            }

            // Spells with SPELL_EFFECT_KNOCK_BACK (like Thunderstorm) can't knockback Target if Target has ROOT/STUN
            if (UnitTarget.HasUnitState(UnitState.Root | UnitState.Stunned))
                return;

            // Instantly interrupt non melee spells being casted
            if (UnitTarget.IsNonMeleeSpellCast(true))
                UnitTarget.InterruptNonMeleeSpells(true);

            float ratio = 0.1f;
            float speedxy = EffectInfo.MiscValue * ratio;
            float speedz = Damage * ratio;

            if (speedxy < 0.01f &&
                speedz < 0.01f)
                return;

            Position origin;

            if (EffectInfo.Effect == SpellEffectName.KnockBackDest)
            {
                if (Targets.HasDst())
                    origin = new Position(DestTarget.GetPosition());
                else
                    return;
            }
            else //if (effectInfo.Effect == SPELL_EFFECT_KNOCK_BACK)
            {
                origin = new Position(_caster.GetPosition());
            }

            UnitTarget.KnockbackFrom(origin, speedxy, speedz);

            Unit.ProcSkillsAndAuras(GetUnitCasterForEffectHandlers(), UnitTarget, new ProcFlagsInit(ProcFlags.None), new ProcFlagsInit(ProcFlags.None, ProcFlags2.Knockback), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, null, null);
        }

        [SpellEffectHandler(SpellEffectName.LeapBack)]
        private void EffectLeapBack()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (UnitTarget == null)
                return;

            float speedxy = EffectInfo.MiscValue / 10.0f;
            float speedz = Damage / 10.0f;
            // Disengage
            UnitTarget.JumpTo(speedxy, speedz, EffectInfo.PositionFacing);

            // changes fall Time
            if (_caster.GetTypeId() == TypeId.Player)
                _caster.ToPlayer().SetFallInformation(0, _caster.GetPositionZ());
        }

        [SpellEffectHandler(SpellEffectName.ClearQuest)]
        private void EffectQuestClear()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            uint quest_id = (uint)EffectInfo.MiscValue;

            Quest quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

            if (quest == null)
                return;

            QuestStatus oldStatus = player.GetQuestStatus(quest_id);

            // Player has never done this quest
            if (oldStatus == QuestStatus.None)
                return;

            // remove all quest entries for 'entry' from quest log
            for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
            {
                uint logQuest = player.GetQuestSlotQuestId(slot);

                if (logQuest == quest_id)
                {
                    player.SetQuestSlot(slot, 0);

                    // we ignore unequippable quest items in this case, it's still be equipped
                    player.TakeQuestSourceItem(logQuest, false);

                    if (quest.HasFlag(QuestFlags.Pvp))
                    {
                        player.PvpInfo.IsHostile = player.PvpInfo.IsInHostileArea || player.HasPvPForcingQuest();
                        player.UpdatePvPState();
                    }
                }
            }

            player.RemoveActiveQuest(quest_id, false);
            player.RemoveRewardedQuest(quest_id);

            Global.ScriptMgr.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(player, quest_id));
            Global.ScriptMgr.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None), quest.ScriptId);
        }

        [SpellEffectHandler(SpellEffectName.SendTaxi)]
        private void EffectSendTaxi()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().ActivateTaxiPathTo((uint)EffectInfo.MiscValue, SpellInfo.Id);
        }

        [SpellEffectHandler(SpellEffectName.PullTowards)]
        private void EffectPullTowards()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            Position pos = _caster.GetFirstCollisionPosition(_caster.GetCombatReach(), _caster.GetRelativeAngle(UnitTarget));

            // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance.
            float distXY = UnitTarget.GetExactDist(pos);

            // Avoid division by 0
            if (distXY < 0.001)
                return;

            float distZ = pos.GetPositionZ() - UnitTarget.GetPositionZ();
            float speedXY = EffectInfo.MiscValue != 0 ? EffectInfo.MiscValue / 10.0f : 30.0f;
            float speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.GRAVITY * distXY * distXY) / (2 * speedXY * distXY));

            if (!float.IsFinite(speedZ))
            {
                Log.outError(LogFilter.Spells, $"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS called with invalid speedZ. {GetDebugInfo()}");

                return;
            }

            UnitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
        }

        [SpellEffectHandler(SpellEffectName.PullTowardsDest)]
        private void EffectPullTowardsDest()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            if (!Targets.HasDst())
            {
                Log.outError(LogFilter.Spells, $"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST has no dest Target");

                return;
            }

            Position pos = Targets.GetDstPos();
            // This is a blizzlike mistake: this should be 2D distance according to projectile motion formulas, but Blizzard erroneously used 3D distance
            float distXY = UnitTarget.GetExactDist(pos);

            // Avoid division by 0
            if (distXY < 0.001)
                return;

            float distZ = pos.GetPositionZ() - UnitTarget.GetPositionZ();

            float speedXY = EffectInfo.MiscValue != 0 ? EffectInfo.MiscValue / 10.0f : 30.0f;
            float speedZ = (float)((2 * speedXY * speedXY * distZ + MotionMaster.GRAVITY * distXY * distXY) / (2 * speedXY * distXY));

            if (!float.IsFinite(speedZ))
            {
                Log.outError(LogFilter.Spells, $"Spell {SpellInfo.Id} with SPELL_EFFECT_PULL_TOWARDS_DEST called with invalid speedZ. {GetDebugInfo()}");

                return;
            }

            UnitTarget.JumpTo(speedXY, speedZ, 0.0f, pos);
        }

        [SpellEffectHandler(SpellEffectName.ChangeRaidMarker)]
        private void EffectChangeRaidMarker()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (!player ||
                !Targets.HasDst())
                return;

            Group group = player.GetGroup();

            if (!group ||
                (group.IsRaidGroup() && !group.IsLeader(player.GetGUID()) && !group.IsAssistant(player.GetGUID())))
                return;

            float x, y, z;
            DestTarget.GetPosition(out x, out y, out z);

            group.AddRaidMarker((byte)Damage, player.GetMapId(), x, y, z);
        }

        [SpellEffectHandler(SpellEffectName.DispelMechanic)]
        private void EffectDispelMechanic()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            int mechanic = EffectInfo.MiscValue;

            List<KeyValuePair<uint, ObjectGuid>> dispel_list = new();

            var auras = UnitTarget.GetOwnedAuras();

            foreach (var pair in auras)
            {
                Aura aura = pair.Value;

                if (aura.GetApplicationOfTarget(UnitTarget.GetGUID()) == null)
                    continue;

                if (RandomHelper.randChance(aura.CalcDispelChance(UnitTarget, !UnitTarget.IsFriendlyTo(_caster))))
                    if ((aura.GetSpellInfo().GetAllEffectsMechanicMask() & (1ul << mechanic)) != 0)
                        dispel_list.Add(new KeyValuePair<uint, ObjectGuid>(aura.GetId(), aura.GetCasterGUID()));
            }

            while (!dispel_list.Empty())
            {
                UnitTarget.RemoveAura(dispel_list[0].Key, dispel_list[0].Value, 0, AuraRemoveMode.EnemySpell);
                dispel_list.RemoveAt(0);
            }
        }

        [SpellEffectHandler(SpellEffectName.ResurrectPet)]
        private void EffectResurrectPet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (Damage < 0)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            // Maybe player dismissed dead pet or pet despawned?
            bool hadPet = true;

            if (player.GetPet() == null)
            {
                PetStable petStable = player.GetPetStable();
                var deadPetIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo?.Health == 0);

                PetSaveMode slot = (PetSaveMode)deadPetIndex;

                player.SummonPet(0, slot, 0f, 0f, 0f, 0f, 0);
                hadPet = false;
            }

            // TODO: Better to fail Hunter's "Revive Pet" at cast instead of here when casting ends
            Pet pet = player.GetPet(); // Attempt to get current pet

            if (pet == null ||
                pet.IsAlive())
                return;

            // If player did have a pet before reviving, teleport it
            if (hadPet)
            {
                // Reposition the pet's corpse before reviving so as not to grab aggro
                // We can use a different, more accurate version of GetClosePoint() since we have a pet
                // Will be used later to reposition the pet if we have one
                player.GetClosePoint(out float x, out float y, out float z, pet.GetCombatReach(), SharedConst.PetFollowDist, pet.GetFollowAngle());
                pet.NearTeleportTo(x, y, z, player.GetOrientation());
                pet.Relocate(x, y, z, player.GetOrientation()); // This is needed so SaveStayPosition() will get the proper coords.
            }

            pet.ReplaceAllDynamicFlags(UnitDynFlags.None);
            pet.RemoveUnitFlag(UnitFlags.Skinnable);
            pet.SetDeathState(DeathState.Alive);
            pet.ClearUnitState(UnitState.AllErasable);
            pet.SetHealth(pet.CountPctFromMaxHealth(Damage));

            // Reset things for when the AI to takes over
            CharmInfo ci = pet.GetCharmInfo();

            if (ci != null)
            {
                // In case the pet was at stay, we don't want it running back
                ci.SaveStayPosition();
                ci.SetIsAtStay(ci.HasCommandState(CommandStates.Stay));

                ci.SetIsFollowing(false);
                ci.SetIsCommandAttack(false);
                ci.SetIsCommandFollow(false);
                ci.SetIsReturning(false);
            }

            pet.SavePetToDB(PetSaveMode.AsCurrent);
        }

        [SpellEffectHandler(SpellEffectName.DestroyAllTotems)]
        private void EffectDestroyAllTotems()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            int mana = 0;

            for (byte slot = (int)SummonSlot.Totem; slot < SharedConst.MaxTotemSlot; ++slot)
            {
                if (unitCaster.SummonSlot[slot].IsEmpty())
                    continue;

                Creature totem = unitCaster.GetMap().GetCreature(unitCaster.SummonSlot[slot]);

                if (totem != null &&
                    totem.IsTotem())
                {
                    uint spell_id = totem.UnitData.CreatedBySpell;
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, GetCastDifficulty());

                    if (spellInfo != null)
                    {
                        var costs = spellInfo.CalcPowerCost(unitCaster, spellInfo.GetSchoolMask());
                        var m = costs.Find(cost => cost.Power == PowerType.Mana);

                        if (m != null)
                            mana += m.Amount;
                    }

                    totem.ToTotem().UnSummon();
                }
            }

            mana = MathFunctions.CalculatePct(mana, Damage);

            if (mana != 0)
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.SetTriggeringSpell(this);
                args.AddSpellMod(SpellValueMod.BasePoint0, mana);
                unitCaster.CastSpell(_caster, 39104, args);
            }
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamage)]
        private void EffectDurabilityDamage()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            int slot = EffectInfo.MiscValue;

            // -1 means all player equipped items and -2 all items
            if (slot < 0)
            {
                UnitTarget.ToPlayer().DurabilityPointsLossAll(Damage, (slot < -1));
                ExecuteLogEffectDurabilityDamage(EffectInfo.Effect, UnitTarget, -1, -1);

                return;
            }

            // invalid Slot value
            if (slot >= InventorySlots.BagEnd)
                return;

            Item item = UnitTarget.ToPlayer().GetItemByPos(InventorySlots.Bag0, (byte)slot);

            if (item != null)
            {
                UnitTarget.ToPlayer().DurabilityPointsLoss(item, Damage);
                ExecuteLogEffectDurabilityDamage(EffectInfo.Effect, UnitTarget, (int)item.GetEntry(), slot);
            }
        }

        [SpellEffectHandler(SpellEffectName.DurabilityDamagePct)]
        private void EffectDurabilityDamagePCT()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            int slot = EffectInfo.MiscValue;

            // FIXME: some spells effects have value -1/-2
            // Possibly its mean -1 all player equipped items and -2 all items
            if (slot < 0)
            {
                UnitTarget.ToPlayer().DurabilityLossAll(Damage / 100.0f, (slot < -1));

                return;
            }

            // invalid Slot value
            if (slot >= InventorySlots.BagEnd)
                return;

            if (Damage <= 0)
                return;

            Item item = UnitTarget.ToPlayer().GetItemByPos(InventorySlots.Bag0, (byte)slot);

            if (item != null)
                UnitTarget.ToPlayer().DurabilityLoss(item, Damage / 100.0f);
        }

        [SpellEffectHandler(SpellEffectName.ModifyThreatPercent)]
        private void EffectModifyThreatPercent()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                UnitTarget == null)
                return;

            UnitTarget.GetThreatManager().ModifyThreatByPercent(unitCaster, Damage);
        }

        [SpellEffectHandler(SpellEffectName.TransDoor)]
        private void EffectTransmitted()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            uint name_id = (uint)EffectInfo.MiscValue;

            var overrideSummonedGameObjects = unitCaster.GetAuraEffectsByType(AuraType.OverrideSummonedObject);

            foreach (AuraEffect aurEff in overrideSummonedGameObjects)
                if (aurEff.GetMiscValue() == name_id)
                {
                    name_id = (uint)aurEff.GetMiscValueB();

                    break;
                }

            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(name_id);

            if (goinfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Entry: {0}) not exist and not created at spell (ID: {1}) cast", name_id, SpellInfo.Id);

                return;
            }

            float fx, fy, fz, fo;

            if (Targets.HasDst())
            {
                DestTarget.GetPosition(out fx, out fy, out fz, out fo);
            }
            //FIXME: this can be better check for most objects but still hack
            else if (EffectInfo.HasRadius() &&
                     SpellInfo.Speed == 0)
            {
                float dis = EffectInfo.CalcRadius(unitCaster);
                unitCaster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultPlayerBoundingRadius, dis);
                fo = unitCaster.GetOrientation();
            }
            else
            {
                //GO is always friendly to it's creator, get range for friends
                float min_dis = SpellInfo.GetMinRange(true);
                float max_dis = SpellInfo.GetMaxRange(true);
                float dis = (float)RandomHelper.NextDouble() * (max_dis - min_dis) + min_dis;

                unitCaster.GetClosePoint(out fx, out fy, out fz, SharedConst.DefaultPlayerBoundingRadius, dis);
                fo = unitCaster.GetOrientation();
            }

            Map cMap = unitCaster.GetMap();

            // if gameobject is summoning object, it should be spawned right on caster's position
            if (goinfo.type == GameObjectTypes.Ritual)
                unitCaster.GetPosition(out fx, out fy, out fz, out fo);

            Position pos = new(fx, fy, fz, fo);
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(fo, 0.0f, 0.0f));

            GameObject go = GameObject.CreateGameObject(name_id, cMap, pos, rotation, 255, GameObjectState.Ready);

            if (!go)
                return;

            PhasingHandler.InheritPhaseShift(go, _caster);

            int duration = SpellInfo.CalcDuration(_caster);

            switch (goinfo.type)
            {
                case GameObjectTypes.FishingNode:
                    {
                        go.SetFaction(unitCaster.GetFaction());
                        ObjectGuid bobberGuid = go.GetGUID();
                        // client requires fishing bobber Guid in channel object Slot 0 to be usable
                        unitCaster.SetChannelObject(0, bobberGuid);
                        unitCaster.AddGameObject(go); // will removed at spell cancel

                        // end Time of range when possible catch fish (FISHING_BOBBER_READY_TIME..GetDuration(_spellInfo))
                        // start Time == fish-FISHING_BOBBER_READY_TIME (0..GetDuration(_spellInfo)-FISHING_BOBBER_READY_TIME)
                        int lastSec = 0;

                        switch (RandomHelper.IRand(0, 2))
                        {
                            case 0:
                                lastSec = 3;

                                break;
                            case 1:
                                lastSec = 7;

                                break;
                            case 2:
                                lastSec = 13;

                                break;
                        }

                        // Duration of the fishing bobber can't be higher than the Fishing channeling duration
                        duration = Math.Min(duration, duration - lastSec * Time.InMilliseconds + 5 * Time.InMilliseconds);

                        break;
                    }
                case GameObjectTypes.Ritual:
                    {
                        if (unitCaster.IsPlayer())
                        {
                            go.AddUniqueUse(unitCaster.ToPlayer());
                            unitCaster.AddGameObject(go); // will be removed at spell cancel
                        }

                        break;
                    }
                case GameObjectTypes.DuelArbiter: // 52991
                    unitCaster.AddGameObject(go);

                    break;
                case GameObjectTypes.FishingHole:
                case GameObjectTypes.Chest:
                default:
                    break;
            }

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetOwnerGUID(unitCaster.GetGUID());
            go.SetSpellId(SpellInfo.Id);

            ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

            Log.outDebug(LogFilter.Spells, "AddObject at SpellEfects.cpp EffectTransmitted");

            cMap.AddToMap(go);
            GameObject linkedTrap = go.GetLinkedTrap();

            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, _caster);
                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(SpellInfo.Id);
                linkedTrap.SetOwnerGUID(unitCaster.GetGUID());

                ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.Prospecting)]
        private void EffectProspecting()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            if (ItemTarget == null ||
                !ItemTarget.GetTemplate().HasFlag(ItemFlags.IsProspectable))
                return;

            if (ItemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillProspecting))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Jewelcrafting);
                uint reqSkillValue = ItemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Jewelcrafting, SkillValue, reqSkillValue);
            }

            ItemTarget.loot = new Loot(player.GetMap(), ItemTarget.GetGUID(), LootType.Prospecting, null);
            ItemTarget.loot.FillLoot(ItemTarget.GetEntry(), LootStorage.Prospecting, player, true);
            player.SendLoot(ItemTarget.loot);
        }

        [SpellEffectHandler(SpellEffectName.Milling)]
        private void EffectMilling()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            if (ItemTarget == null ||
                !ItemTarget.GetTemplate().HasFlag(ItemFlags.IsMillable))
                return;

            if (ItemTarget.GetCount() < 5)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.SkillMilling))
            {
                uint SkillValue = player.GetPureSkillValue(SkillType.Inscription);
                uint reqSkillValue = ItemTarget.GetTemplate().GetRequiredSkillRank();
                player.UpdateGatherSkill(SkillType.Inscription, SkillValue, reqSkillValue);
            }

            ItemTarget.loot = new Loot(player.GetMap(), ItemTarget.GetGUID(), LootType.Milling, null);
            ItemTarget.loot.FillLoot(ItemTarget.GetEntry(), LootStorage.Milling, player, true);
            player.SendLoot(ItemTarget.loot);
        }

        [SpellEffectHandler(SpellEffectName.Skill)]
        private void EffectSkill()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Log.outDebug(LogFilter.Spells, "WORLD: SkillEFFECT");
        }

        /* There is currently no need for this effect. We handle it in Battleground.cpp
		   If we would handle the resurrection here, the spiritguide would instantly disappear as the
		   player revives, and so we wouldn't see the spirit heal visual effect on the npc.
		   This is why we use a half sec delay between the visual effect and the resurrection itself */
        private void EffectSpiritHeal()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;
        }

        // remove insignia spell effect
        [SpellEffectHandler(SpellEffectName.SkinPlayerCorpse)]
        private void EffectSkinPlayerCorpse()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: SkinPlayerCorpse");

            Player player = _caster.ToPlayer();
            Player target = null;

            if (UnitTarget != null)
                target = UnitTarget.ToPlayer();
            else if (CorpseTarget != null)
                target = Global.ObjAccessor.FindPlayer(CorpseTarget.GetOwnerGUID());

            if (player == null ||
                target == null ||
                target.IsAlive())
                return;

            target.RemovedInsignia(player);
        }

        [SpellEffectHandler(SpellEffectName.StealBeneficialBuff)]
        private void EffectStealBeneficialBuff()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Log.outDebug(LogFilter.Spells, "Effect: StealBeneficialBuff");

            if (UnitTarget == null ||
                UnitTarget == _caster) // can't steal from self
                return;

            List<DispelableAura> stealList = new();

            // Create dispel mask by dispel Type
            uint dispelMask = SpellInfo.GetDispelMask((DispelType)EffectInfo.MiscValue);
            var auras = UnitTarget.GetOwnedAuras();

            foreach (var map in auras)
            {
                Aura aura = map.Value;
                AuraApplication aurApp = aura.GetApplicationOfTarget(UnitTarget.GetGUID());

                if (aurApp == null)
                    continue;

                if (Convert.ToBoolean(aura.GetSpellInfo().GetDispelMask() & dispelMask))
                {
                    // Need check for passive? this
                    if (!aurApp.IsPositive() ||
                        aura.IsPassive() ||
                        aura.GetSpellInfo().HasAttribute(SpellAttr4.CannotBeStolen))
                        continue;

                    // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
                    int chance = aura.CalcDispelChance(UnitTarget, !UnitTarget.IsFriendlyTo(_caster));

                    if (chance == 0)
                        continue;

                    // The charges / stack amounts don't Count towards the total number of Auras that can be dispelled.
                    // Ie: A dispel on a Target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) . 50% chance to dispell
                    // Polymorph instead of 1 / (5 + 1) . 16%.
                    bool dispelCharges = aura.GetSpellInfo().HasAttribute(SpellAttr7.DispelCharges);
                    byte charges = dispelCharges ? aura.GetCharges() : aura.GetStackAmount();

                    if (charges > 0)
                        stealList.Add(new DispelableAura(aura, chance, charges));
                }
            }

            if (stealList.Empty())
                return;

            int remaining = stealList.Count;

            // Ok if exist some buffs for dispel try dispel it
            List<Tuple<uint, ObjectGuid, int>> successList = new();

            DispelFailed dispelFailed = new();
            dispelFailed.CasterGUID = _caster.GetGUID();
            dispelFailed.VictimGUID = UnitTarget.GetGUID();
            dispelFailed.SpellID = SpellInfo.Id;

            // dispel N = Damage buffs (or while exist buffs for dispel)
            for (int count = 0; count < Damage && remaining > 0;)
            {
                // Random select buff for dispel
                var dispelableAura = stealList[RandomHelper.IRand(0, remaining - 1)];

                if (dispelableAura.RollDispel())
                {
                    byte stolenCharges = 1;

                    if (dispelableAura.GetAura().GetSpellInfo().HasAttribute(SpellAttr1.DispelAllStacks))
                        stolenCharges = dispelableAura.GetDispelCharges();

                    successList.Add(Tuple.Create(dispelableAura.GetAura().GetId(), dispelableAura.GetAura().GetCasterGUID(), (int)stolenCharges));

                    if (!dispelableAura.DecrementCharge(stolenCharges))
                    {
                        --remaining;
                        stealList[remaining] = dispelableAura;
                    }
                }
                else
                {
                    dispelFailed.FailedSpells.Add(dispelableAura.GetAura().GetId());
                }

                ++count;
            }

            if (!dispelFailed.FailedSpells.Empty())
                _caster.SendMessageToSet(dispelFailed, true);

            if (successList.Empty())
                return;

            SpellDispellLog spellDispellLog = new();
            spellDispellLog.IsBreak = false; // TODO: use me
            spellDispellLog.IsSteal = true;

            spellDispellLog.TargetGUID = UnitTarget.GetGUID();
            spellDispellLog.CasterGUID = _caster.GetGUID();
            spellDispellLog.DispelledBySpellID = SpellInfo.Id;

            foreach (var (spellId, auraCaster, stolenCharges) in successList)
            {
                var dispellData = new SpellDispellData();
                dispellData.SpellID = spellId;
                dispellData.Harmful = false; // TODO: use me

                UnitTarget.RemoveAurasDueToSpellBySteal(spellId, auraCaster, _caster, stolenCharges);

                spellDispellLog.DispellData.Add(dispellData);
            }

            _caster.SendMessageToSet(spellDispellLog, true);
        }

        [SpellEffectHandler(SpellEffectName.KillCredit)]
        private void EffectKillCreditPersonal()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().KilledMonsterCredit((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.KillCredit2)]
        private void EffectKillCredit()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            int creatureEntry = EffectInfo.MiscValue;

            if (creatureEntry != 0)
                UnitTarget.ToPlayer().RewardPlayerAndGroupAtEvent((uint)creatureEntry, UnitTarget);
        }

        [SpellEffectHandler(SpellEffectName.QuestFail)]
        private void EffectQuestFail()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().FailQuest((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.QuestStart)]
        private void EffectQuestStart()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            Player player = UnitTarget.ToPlayer();

            if (!player)
                return;

            Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)EffectInfo.MiscValue);

            if (quest != null)
            {
                if (!player.CanTakeQuest(quest, false))
                    return;

                if (quest.IsAutoAccept() &&
                    player.CanAddQuest(quest, false))
                {
                    player.AddQuestAndCheckCompletion(quest, null);
                    player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GetGUID(), true, true);
                }
                else
                {
                    player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, player.GetGUID(), true, false);
                }
            }
        }

        [SpellEffectHandler(SpellEffectName.CreateTamedPet)]
        private void EffectCreateTamedPet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player) ||
                !UnitTarget.GetPetGUID().IsEmpty() ||
                UnitTarget.GetClass() != Class.Hunter)
                return;

            uint creatureEntry = (uint)EffectInfo.MiscValue;
            Pet pet = UnitTarget.CreateTamedPetFrom(creatureEntry, SpellInfo.Id);

            if (pet == null)
                return;

            // relocate
            float px, py, pz;
            UnitTarget.GetClosePoint(out px, out py, out pz, pet.GetCombatReach(), SharedConst.PetFollowDist, pet.GetFollowAngle());
            pet.Relocate(px, py, pz, UnitTarget.GetOrientation());

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // unitTarget has pet now
            UnitTarget.SetMinion(pet, true);

            if (UnitTarget.IsTypeId(TypeId.Player))
            {
                pet.SavePetToDB(PetSaveMode.AsCurrent);
                UnitTarget.ToPlayer().PetSpellInitialize();
            }
        }

        [SpellEffectHandler(SpellEffectName.DiscoverTaxi)]
        private void EffectDiscoverTaxi()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            uint nodeid = (uint)EffectInfo.MiscValue;

            if (CliDB.TaxiNodesStorage.ContainsKey(nodeid))
                UnitTarget.ToPlayer().GetSession().SendDiscoverNewTaxiNode(nodeid);
        }

        [SpellEffectHandler(SpellEffectName.TitanGrip)]
        private void EffectTitanGrip()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (_caster.IsTypeId(TypeId.Player))
                _caster.ToPlayer().SetCanTitanGrip(true, (uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.RedirectThreat)]
        private void EffectRedirectThreat()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (UnitTarget != null)
                unitCaster.GetThreatManager().RegisterRedirectThreat(SpellInfo.Id, UnitTarget.GetGUID(), (uint)Damage);
        }

        [SpellEffectHandler(SpellEffectName.GameObjectDamage)]
        private void EffectGameObjectDamage()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (GameObjTarget == null)
                return;

            FactionTemplateRecord casterFaction = _caster.GetFactionTemplateEntry();
            FactionTemplateRecord targetFaction = CliDB.FactionTemplateStorage.LookupByKey(GameObjTarget.GetFaction());

            // Do not allow to Damage GO's of friendly factions (ie: Wintergrasp Walls/Ulduar Storm Beacons)
            if (targetFaction == null ||
                (casterFaction != null && !casterFaction.IsFriendlyTo(targetFaction)))
                GameObjTarget.ModifyHealth(-Damage, _caster, GetSpellInfo().Id);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectRepair)]
        private void EffectGameObjectRepair()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (GameObjTarget == null)
                return;

            GameObjTarget.ModifyHealth(Damage, _caster);
        }

        [SpellEffectHandler(SpellEffectName.GameobjectSetDestructionState)]
        private void EffectGameObjectSetDestructionState()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (GameObjTarget == null)
                return;

            GameObjTarget.SetDestructibleState((GameObjectDestructibleState)EffectInfo.MiscValue, _caster, true);
        }

        private void SummonGuardian(SpellEffectInfo effect, uint entry, SummonPropertiesRecord properties, uint numGuardians, ObjectGuid privateObjectOwner)
        {
            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (unitCaster.IsTotem())
                unitCaster = unitCaster.ToTotem().GetOwner();

            // in another case summon new
            float radius = 5.0f;
            int duration = SpellInfo.CalcDuration(_originalCaster);

            //TempSummonType summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;
            Map map = unitCaster.GetMap();

            for (uint count = 0; count < numGuardians; ++count)
            {
                Position pos;

                if (count == 0)
                    pos = DestTarget;
                else
                    // randomize position for multiple summons
                    pos = unitCaster.GetRandomPoint(DestTarget, radius);

                TempSummon summon = map.SummonCreature(entry, pos, properties, (uint)duration, unitCaster, SpellInfo.Id, 0, privateObjectOwner);

                if (summon == null)
                    return;

                if (summon.HasUnitTypeMask(UnitTypeMask.Guardian))
                {
                    uint level = summon.GetLevel();

                    if (properties != null &&
                        !properties.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
                        level = unitCaster.GetLevel();

                    // level of pet summoned using engineering Item based at engineering skill level
                    if (CastItem && unitCaster.IsPlayer())
                    {
                        ItemTemplate proto = CastItem.GetTemplate();

                        if (proto != null)
                            if (proto.GetRequiredSkill() == (uint)SkillType.Engineering)
                            {
                                ushort skill202 = unitCaster.ToPlayer().GetSkillValue(SkillType.Engineering);

                                if (skill202 != 0)
                                    level = skill202 / 5u;
                            }
                    }

                    ((Guardian)summon).InitStatsForLevel(level);
                }

                if (summon.HasUnitTypeMask(UnitTypeMask.Minion) &&
                    Targets.HasDst())
                    ((Minion)summon).SetFollowAngle(unitCaster.GetAbsoluteAngle(summon.GetPosition()));

                if (summon.GetEntry() == 27893)
                {
                    VisibleItem weapon = _caster.ToPlayer().PlayerData.VisibleItems[EquipmentSlot.MainHand];

                    if (weapon.ItemID != 0)
                    {
                        summon.SetDisplayId(11686);
                        summon.SetVirtualItem(0, weapon.ItemID, weapon.ItemAppearanceModID, weapon.ItemVisual);
                    }
                    else
                    {
                        summon.SetDisplayId(1126);
                    }
                }

                ExecuteLogEffectSummonObject(effect.Effect, summon);
            }
        }

        [SpellEffectHandler(SpellEffectName.AllowRenamePet)]
        private void EffectRenamePet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Unit) ||
                !UnitTarget.IsPet() ||
                UnitTarget.ToPet().GetPetType() != PetType.Hunter)
                return;

            UnitTarget.SetPetFlag(UnitPetFlags.CanBeRenamed);
        }

        [SpellEffectHandler(SpellEffectName.PlayMusic)]
        private void EffectPlayMusic()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            uint soundid = (uint)EffectInfo.MiscValue;

            if (!CliDB.SoundKitStorage.ContainsKey(soundid))
            {
                Log.outError(LogFilter.Spells, "EffectPlayMusic: Sound (Id: {0}) not exist in spell {1}.", soundid, SpellInfo.Id);

                return;
            }

            UnitTarget.ToPlayer().SendPacket(new PlayMusic(soundid));
        }

        [SpellEffectHandler(SpellEffectName.TalentSpecSelect)]
        private void EffectActivateSpec()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();
            uint specID = Misc.SpecializationId;
            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(specID);

            // Safety checks done in Spell::CheckCast
            if (!spec.IsPetSpecialization())
                player.ActivateTalentGroup(spec);
            else
                player.GetPet().SetSpecialization(specID);
        }

        [SpellEffectHandler(SpellEffectName.PlaySound)]
        private void EffectPlaySound()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            Player player = UnitTarget.ToPlayer();

            if (!player)
                return;

            switch (SpellInfo.Id)
            {
                case 91604: // Restricted Flight Area
                    player.GetSession().SendNotification(CypherStrings.ZoneNoflyzone);

                    break;
                default:
                    break;
            }

            uint soundId = (uint)EffectInfo.MiscValue;

            if (!CliDB.SoundKitStorage.ContainsKey(soundId))
            {
                Log.outError(LogFilter.Spells, "EffectPlaySound: Sound (Id: {0}) not exist in spell {1}.", soundId, SpellInfo.Id);

                return;
            }

            player.PlayDirectSound(soundId, player);
        }

        [SpellEffectHandler(SpellEffectName.RemoveAura)]
        [SpellEffectHandler(SpellEffectName.RemoveAura2)]
        private void EffectRemoveAura()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            // there may be need of specifying casterguid of removed Auras
            UnitTarget.RemoveAurasDueToSpell(EffectInfo.TriggerSpell);
        }

        [SpellEffectHandler(SpellEffectName.DamageFromMaxHealthPCT)]
        private void EffectDamageFromMaxHealthPCT()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            EffectDamage += (int)UnitTarget.CountPctFromMaxHealth(Damage);
        }

        [SpellEffectHandler(SpellEffectName.GiveCurrency)]
        private void EffectGiveCurrency()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            if (!CliDB.CurrencyTypesStorage.ContainsKey(EffectInfo.MiscValue))
                return;

            UnitTarget.ToPlayer().ModifyCurrency((uint)EffectInfo.MiscValue, Damage);
        }

        [SpellEffectHandler(SpellEffectName.CastButton)]
        private void EffectCastButtons()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (player == null)
                return;

            int button_id = EffectInfo.MiscValue + 132;
            int n_buttons = EffectInfo.MiscValueB;

            for (; n_buttons != 0; --n_buttons, ++button_id)
            {
                ActionButton ab = player.GetActionButton((byte)button_id);

                if (ab == null ||
                    ab.GetButtonType() != ActionButtonType.Spell)
                    continue;

                //! Action Button _data is unverified when it's set so it can be "hacked"
                //! to contain invalid spells, so filter here.
                uint spell_id = (uint)ab.GetAction();

                if (spell_id == 0)
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, GetCastDifficulty());

                if (spellInfo == null)
                    continue;

                if (!player.HasSpell(spell_id) ||
                    player.GetSpellHistory().HasCooldown(spell_id))
                    continue;

                if (!spellInfo.HasAttribute(SpellAttr9.SummonPlayerTotem))
                    continue;

                CastSpellExtraArgs args = new(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress | TriggerCastFlags.CastDirectly | TriggerCastFlags.DontReportCastError);
                args.OriginalCastId = CastId;
                args.CastDifficulty = GetCastDifficulty();
                _caster.CastSpell(_caster, spellInfo.Id, args);
            }
        }

        [SpellEffectHandler(SpellEffectName.RechargeItem)]
        private void EffectRechargeItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null)
                return;

            Player player = UnitTarget.ToPlayer();

            if (player == null)
                return;

            Item item = player.GetItemByEntry(EffectInfo.ItemType);

            if (item != null)
            {
                foreach (ItemEffectRecord itemEffect in item.GetEffects())
                    if (itemEffect.LegacySlotIndex <= item._itemData.SpellCharges.GetSize())
                        item.SetSpellCharges(itemEffect.LegacySlotIndex, itemEffect.Charges);

                item.SetState(ItemUpdateState.Changed, player);
            }
        }

        [SpellEffectHandler(SpellEffectName.Bind)]
        private void EffectBind()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();

            WorldLocation homeLoc = new();
            uint areaId = player.GetAreaId();

            if (EffectInfo.MiscValue != 0)
                areaId = (uint)EffectInfo.MiscValue;

            if (Targets.HasDst())
            {
                homeLoc.WorldRelocate(DestTarget);
            }
            else
            {
                homeLoc.Relocate(player.GetPosition());
                homeLoc.SetMapId(player.GetMapId());
            }

            player.SetHomebind(homeLoc, areaId);
            player.SendBindPointUpdate();

            Log.outDebug(LogFilter.Spells, $"EffectBind: New _homebind: {homeLoc}, AreaId: {areaId}");

            // zone update
            player.SendPlayerBound(_caster.GetGUID(), areaId);
        }

        [SpellEffectHandler(SpellEffectName.TeleportToReturnPoint)]
        private void EffectTeleportToReturnPoint()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = UnitTarget.ToPlayer();

            if (player != null)
            {
                WorldLocation dest = player.GetStoredAuraTeleportLocation((uint)EffectInfo.MiscValue);

                if (dest != null)
                    player.TeleportTo(dest, UnitTarget == _caster ? TeleportToOptions.Spell | TeleportToOptions.NotLeaveCombat : 0);
            }
        }

        [SpellEffectHandler(SpellEffectName.SummonRafFriend)]
        private void EffectSummonRaFFriend()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!_caster.IsTypeId(TypeId.Player) ||
                UnitTarget == null ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            _caster.CastSpell(UnitTarget, EffectInfo.TriggerSpell, new CastSpellExtraArgs(this));
        }

        [SpellEffectHandler(SpellEffectName.UnlockGuildVaultTab)]
        private void EffectUnlockGuildVaultTab()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            // Safety checks done in Spell.CheckCast
            Player caster = _caster.ToPlayer();
            Guild guild = caster.GetGuild();

            guild?.HandleBuyBankTab(caster.GetSession(), (byte)(Damage - 1)); // Bank tabs start at zero internally
        }

        [SpellEffectHandler(SpellEffectName.SummonPersonalGameobject)]
        private void EffectSummonPersonalGameObject()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            uint goId = (uint)EffectInfo.MiscValue;

            if (goId == 0)
                return;

            float x, y, z, o;

            if (Targets.HasDst())
            {
                DestTarget.GetPosition(out x, out y, out z, out o);
            }
            else
            {
                _caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius);
                o = _caster.GetOrientation();
            }

            Map map = _caster.GetMap();
            Position pos = new(x, y, z, o);
            Quaternion rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
            GameObject go = GameObject.CreateGameObject(goId, map, pos, rot, 255, GameObjectState.Ready);

            if (!go)
            {
                Log.outWarn(LogFilter.Spells, $"SpellEffect Failed to summon personal gameobject. SpellId {SpellInfo.Id}, effect {EffectInfo.EffectIndex}");

                return;
            }

            PhasingHandler.InheritPhaseShift(go, _caster);

            int duration = SpellInfo.CalcDuration(_caster);

            go.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
            go.SetSpellId(SpellInfo.Id);
            go.SetPrivateObjectOwner(_caster.GetGUID());

            ExecuteLogEffectSummonObject(EffectInfo.Effect, go);

            map.AddToMap(go);

            GameObject linkedTrap = go.GetLinkedTrap();

            if (linkedTrap != null)
            {
                PhasingHandler.InheritPhaseShift(linkedTrap, _caster);

                linkedTrap.SetRespawnTime(duration > 0 ? duration / Time.InMilliseconds : 0);
                linkedTrap.SetSpellId(SpellInfo.Id);

                ExecuteLogEffectSummonObject(EffectInfo.Effect, linkedTrap);
            }
        }

        [SpellEffectHandler(SpellEffectName.ResurrectWithAura)]
        private void EffectResurrectWithAura()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsInWorld)
                return;

            Player target = UnitTarget.ToPlayer();

            if (target == null)
                return;

            if (UnitTarget.IsAlive())
                return;

            if (target.IsResurrectRequested()) // already have one active request
                return;

            uint health = (uint)target.CountPctFromMaxHealth(Damage);
            uint mana = (uint)MathFunctions.CalculatePct(target.GetMaxPower(PowerType.Mana), Damage);
            uint resurrectAura = 0;

            if (Global.SpellMgr.HasSpellInfo(EffectInfo.TriggerSpell, Difficulty.None))
                resurrectAura = EffectInfo.TriggerSpell;

            if (resurrectAura != 0 &&
                target.HasAura(resurrectAura))
                return;

            ExecuteLogEffectResurrect(EffectInfo.Effect, target);
            target.SetResurrectRequestData(_caster, health, mana, resurrectAura);
            SendResurrectRequest(target);
        }

        [SpellEffectHandler(SpellEffectName.CreateAreaTrigger)]
        private void EffectCreateAreaTrigger()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                !Targets.HasDst())
                return;

            int duration = GetSpellInfo().CalcDuration(GetCaster());
            AreaTrigger.CreateAreaTrigger((uint)EffectInfo.MiscValue, unitCaster, null, GetSpellInfo(), DestTarget.GetPosition(), duration, SpellVisual, CastId);
        }

        [SpellEffectHandler(SpellEffectName.RemoveTalent)]
        private void EffectRemoveTalent()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            TalentRecord talent = CliDB.TalentStorage.LookupByKey(Misc.TalentId);

            if (talent == null)
                return;

            Player player = UnitTarget ? UnitTarget.ToPlayer() : null;

            if (player == null)
                return;

            player.RemoveTalent(talent);
            player.SendTalentsInfoData();
        }

        [SpellEffectHandler(SpellEffectName.DestroyItem)]
        private void EffectDestroyItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Player player = UnitTarget.ToPlayer();
            Item item = player.GetItemByEntry(EffectInfo.ItemType);

            if (item)
                player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
        }

        [SpellEffectHandler(SpellEffectName.LearnGarrisonBuilding)]
        private void EffectLearnGarrisonBuilding()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = UnitTarget.ToPlayer().GetGarrison();

            garrison?.LearnBlueprint((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateGarrison)]
        private void EffectCreateGarrison()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().CreateGarrison((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateConversation)]
        private void EffectCreateConversation()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                !Targets.HasDst())
                return;

            Conversation.CreateConversation((uint)EffectInfo.MiscValue, unitCaster, DestTarget.GetPosition(), ObjectGuid.Empty, GetSpellInfo());
        }

        [SpellEffectHandler(SpellEffectName.CancelConversation)]
        private void EffectCancelConversation()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget)
                return;

            List<WorldObject> objs = new();
            ObjectEntryAndPrivateOwnerIfExistsCheck check = new(UnitTarget.GetGUID(), (uint)EffectInfo.MiscValue);
            WorldObjectListSearcher checker = new(UnitTarget, objs, check, GridMapTypeMask.Conversation);
            Cell.VisitGridObjects(UnitTarget, checker, 100.0f);

            foreach (WorldObject obj in objs)
            {
                Conversation convo = obj.ToConversation();

                convo?.Remove();
            }
        }

        [SpellEffectHandler(SpellEffectName.AddGarrisonFollower)]
        private void EffectAddGarrisonFollower()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = UnitTarget.ToPlayer().GetGarrison();

            garrison?.AddFollower((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateHeirloomItem)]
        private void EffectCreateHeirloomItem()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = _caster.ToPlayer();

            if (!player)
                return;

            CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();

            if (collectionMgr == null)
                return;

            List<uint> bonusList = new();
            bonusList.Add(collectionMgr.GetHeirloomBonus(Misc.Data0));

            DoCreateItem(Misc.Data0, ItemContext.None, bonusList);
            ExecuteLogEffectCreateItem(EffectInfo.Effect, Misc.Data0);
        }

        [SpellEffectHandler(SpellEffectName.ActivateGarrisonBuilding)]
        private void EffectActivateGarrisonBuilding()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            Garrison garrison = UnitTarget.ToPlayer().GetGarrison();

            garrison?.ActivateBuilding((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.GrantBattlepetLevel)]
        private void EffectGrantBattlePetLevel()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster == null)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsCreature())
                return;

            playerCaster.GetSession().GetBattlePetMgr().GrantBattlePetLevel(UnitTarget.GetBattlePetCompanionGUID(), (ushort)Damage);
        }

        [SpellEffectHandler(SpellEffectName.GiveExperience)]
        private void EffectGiveExperience()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = UnitTarget?.ToPlayer();

            if (!playerTarget)
                return;

            uint xp = Quest.XPValue(playerTarget, (uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB);
            playerTarget.GiveXP(xp, null);
        }

        [SpellEffectHandler(SpellEffectName.GiveRestedEcperienceBonus)]
        private void EffectGiveRestedExperience()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = UnitTarget?.ToPlayer();

            if (!playerTarget)
                return;

            // effect value is number of resting hours
            playerTarget.GetRestMgr().AddRestBonus(RestTypes.XP, Damage * Time.Hour * playerTarget.GetRestMgr().CalcExtraPerSec(RestTypes.XP, 0.125f));
        }

        [SpellEffectHandler(SpellEffectName.HealBattlepetPct)]
        private void EffectHealBattlePetPct()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            BattlePetMgr battlePetMgr = UnitTarget.ToPlayer().GetSession().GetBattlePetMgr();

            battlePetMgr?.HealBattlePetsPct((byte)Damage);
        }

        [SpellEffectHandler(SpellEffectName.EnableBattlePets)]
        private void EffectEnableBattlePets()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsPlayer())
                return;

            Player player = UnitTarget.ToPlayer();
            player.SetPlayerFlag(PlayerFlags.PetBattlesUnlocked);
            player.GetSession().GetBattlePetMgr().UnlockSlot(BattlePetSlots.Slot0);
        }

        [SpellEffectHandler(SpellEffectName.ChangeBattlepetQuality)]
        private void EffectChangeBattlePetQuality()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster == null)
                return;

            if (UnitTarget == null ||
                !UnitTarget.IsCreature())
                return;

            var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < Damage);

            BattlePetBreedQuality quality = BattlePetBreedQuality.Poor;

            if (qualityRecord != null)
                quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

            playerCaster.GetSession().GetBattlePetMgr().ChangeBattlePetQuality(UnitTarget.GetBattlePetCompanionGUID(), quality);
        }

        [SpellEffectHandler(SpellEffectName.LaunchQuestChoice)]
        private void EffectLaunchQuestChoice()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsPlayer())
                return;

            UnitTarget.ToPlayer().SendPlayerChoice(GetCaster().GetGUID(), EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.UncageBattlepet)]
        private void EffectUncageBattlePet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!CastItem ||
                !_caster ||
                !_caster.IsTypeId(TypeId.Player))
                return;

            uint speciesId = CastItem.GetModifier(ItemModifier.BattlePetSpeciesId);
            ushort breed = (ushort)(CastItem.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF);
            BattlePetBreedQuality quality = (BattlePetBreedQuality)((CastItem.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF);
            ushort level = (ushort)CastItem.GetModifier(ItemModifier.BattlePetLevel);
            uint displayId = CastItem.GetModifier(ItemModifier.BattlePetDisplayId);

            BattlePetSpeciesRecord speciesEntry = CliDB.BattlePetSpeciesStorage.LookupByKey(speciesId);

            if (speciesEntry == null)
                return;

            Player player = _caster.ToPlayer();
            BattlePetMgr battlePetMgr = player.GetSession().GetBattlePetMgr();

            if (battlePetMgr == null)
                return;

            if (battlePetMgr.GetMaxPetLevel() < level)
            {
                battlePetMgr.SendError(BattlePetError.TooHighLevelToUncage, speciesEntry.CreatureID);
                SendCastResult(SpellCastResult.CantAddBattlePet);

                return;
            }

            if (battlePetMgr.HasMaxPetCount(speciesEntry, player.GetGUID()))
            {
                battlePetMgr.SendError(BattlePetError.CantHaveMorePetsOfThatType, speciesEntry.CreatureID);
                SendCastResult(SpellCastResult.CantAddBattlePet);

                return;
            }

            battlePetMgr.AddPet(speciesId, displayId, breed, quality, level);

            player.SendPlaySpellVisual(player, SharedConst.SpellVisualUncagePet, 0, 0, 0.0f, false);

            player.DestroyItem(CastItem.GetBagSlot(), CastItem.GetSlot(), true);
            CastItem = null;
        }

        [SpellEffectHandler(SpellEffectName.UpgradeHeirloom)]
        private void EffectUpgradeHeirloom()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Player player = _caster.ToPlayer();

            if (player)
            {
                CollectionMgr collectionMgr = player.GetSession().GetCollectionMgr();

                collectionMgr?.UpgradeHeirloom(Misc.Data0, CastItemEntry);
            }
        }

        [SpellEffectHandler(SpellEffectName.ApplyEnchantIllusion)]
        private void EffectApplyEnchantIllusion()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!ItemTarget)
                return;

            Player player = _caster.ToPlayer();

            if (!player ||
                player.GetGUID() != ItemTarget.GetOwnerGUID())
                return;

            ItemTarget.SetState(ItemUpdateState.Changed, player);
            ItemTarget.SetModifier(ItemModifier.EnchantIllusionAllSpecs, (uint)EffectInfo.MiscValue);

            if (ItemTarget.IsEquipped())
                player.SetVisibleItemSlot(ItemTarget.GetSlot(), ItemTarget);

            player.RemoveTradeableItem(ItemTarget);
            ItemTarget.ClearSoulboundTradeable(player);
        }

        [SpellEffectHandler(SpellEffectName.UpdatePlayerPhase)]
        private void EffectUpdatePlayerPhase()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            PhasingHandler.OnConditionChange(UnitTarget);
        }

        [SpellEffectHandler(SpellEffectName.UpdateZoneAurasPhases)]
        private void EffectUpdateZoneAurasAndPhases()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsTypeId(TypeId.Player))
                return;

            UnitTarget.ToPlayer().UpdateAreaDependentAuras(UnitTarget.GetAreaId());
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPower)]
        private void EffectGiveArtifactPower()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster == null)
                return;

            Aura artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

            if (artifactAura != null)
            {
                Item artifact = playerCaster.GetItemByGuid(artifactAura.GetCastItemGUID());

                if (artifact)
                    artifact.GiveArtifactXp((ulong)Damage, CastItem, (ArtifactCategory)EffectInfo.MiscValue);
            }
        }

        [SpellEffectHandler(SpellEffectName.GiveArtifactPowerNoBonus)]
        private void EffectGiveArtifactPowerNoBonus()
        {
            if (_effectHandleMode != SpellEffectHandleMode.LaunchTarget)
                return;

            if (!UnitTarget ||
                !_caster.IsTypeId(TypeId.Player))
                return;

            Aura artifactAura = UnitTarget.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

            if (artifactAura != null)
            {
                Item artifact = UnitTarget.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());

                if (artifact)
                    artifact.GiveArtifactXp((ulong)Damage, CastItem, 0);
            }
        }

        [SpellEffectHandler(SpellEffectName.PlaySceneScriptPackage)]
        private void EffectPlaySceneScriptPackage()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
                return;

            _caster.ToPlayer().GetSceneMgr().PlaySceneByPackageId((uint)EffectInfo.MiscValue, SceneFlags.PlayerNonInteractablePhased, DestTarget);
        }

        private bool IsUnitTargetSceneObjectAura(Spell spell, TargetInfo target)
        {
            if (target.TargetGUID != spell.GetCaster().GetGUID())
                return false;

            foreach (SpellEffectInfo spellEffectInfo in spell.GetSpellInfo().GetEffects())
                if ((target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
                    spellEffectInfo.IsUnitOwnedAuraEffect())
                    return true;

            return false;
        }

        [SpellEffectHandler(SpellEffectName.CreateSceneObject)]
        private void EffectCreateSceneObject()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (!unitCaster ||
                !Targets.HasDst())
                return;

            SceneObject sceneObject = SceneObject.CreateSceneObject((uint)EffectInfo.MiscValue, unitCaster, DestTarget.GetPosition(), ObjectGuid.Empty);

            if (sceneObject != null)
            {
                bool hasAuraTargetingCaster = UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));

                if (hasAuraTargetingCaster)
                    sceneObject.SetCreatedBySpellCast(CastId);
            }
        }

        [SpellEffectHandler(SpellEffectName.CreatePersonalSceneObject)]
        private void EffectCreatePrivateSceneObject()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (!unitCaster ||
                !Targets.HasDst())
                return;

            SceneObject sceneObject = SceneObject.CreateSceneObject((uint)EffectInfo.MiscValue, unitCaster, DestTarget.GetPosition(), unitCaster.GetGUID());

            if (sceneObject != null)
            {
                bool hasAuraTargetingCaster = UniqueTargetInfo.Any(target => IsUnitTargetSceneObjectAura(this, target));

                if (hasAuraTargetingCaster)
                    sceneObject.SetCreatedBySpellCast(CastId);
            }
        }

        [SpellEffectHandler(SpellEffectName.PlayScene)]
        private void EffectPlayScene()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            if (_caster.GetTypeId() != TypeId.Player)
                return;

            _caster.ToPlayer().GetSceneMgr().PlayScene((uint)EffectInfo.MiscValue, DestTarget);
        }

        [SpellEffectHandler(SpellEffectName.GiveHonor)]
        private void EffectGiveHonor()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                UnitTarget.GetTypeId() != TypeId.Player)
                return;

            PvPCredit packet = new();
            packet.Honor = Damage;
            packet.OriginalHonor = Damage;

            Player playerTarget = UnitTarget.ToPlayer();
            playerTarget.AddHonorXP((uint)Damage);
            playerTarget.SendPacket(packet);
        }

        [SpellEffectHandler(SpellEffectName.JumpCharge)]
        private void EffectJumpCharge()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Launch)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            if (unitCaster.IsInFlight())
                return;

            JumpChargeParams jumpParams = Global.ObjectMgr.GetJumpChargeParams(EffectInfo.MiscValue);

            if (jumpParams == null)
                return;

            float speed = jumpParams.Speed;

            if (jumpParams.TreatSpeedAsMoveTimeSeconds)
                speed = unitCaster.GetExactDist(DestTarget) / jumpParams.Speed;

            JumpArrivalCastArgs arrivalCast = null;

            if (EffectInfo.TriggerSpell != 0)
            {
                arrivalCast = new JumpArrivalCastArgs();
                arrivalCast.SpellId = EffectInfo.TriggerSpell;
            }

            SpellEffectExtraData effectExtra = null;

            if (jumpParams.SpellVisualId.HasValue ||
                jumpParams.ProgressCurveId.HasValue ||
                jumpParams.ParabolicCurveId.HasValue)
            {
                effectExtra = new SpellEffectExtraData();

                if (jumpParams.SpellVisualId.HasValue)
                    effectExtra.SpellVisualId = jumpParams.SpellVisualId.Value;

                if (jumpParams.ProgressCurveId.HasValue)
                    effectExtra.ProgressCurveId = jumpParams.ProgressCurveId.Value;

                if (jumpParams.ParabolicCurveId.HasValue)
                    effectExtra.ParabolicCurveId = jumpParams.ParabolicCurveId.Value;
            }

            unitCaster.GetMotionMaster().MoveJumpWithGravity(DestTarget, speed, jumpParams.JumpGravity, EventId.Jump, false, arrivalCast, effectExtra);
        }

        [SpellEffectHandler(SpellEffectName.LearnTransmogSet)]
        private void EffectLearnTransmogSet()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsPlayer())
                return;

            UnitTarget.ToPlayer().GetSession().GetCollectionMgr().AddTransmogSet((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.LearnAzeriteEssencePower)]
        private void EffectLearnAzeriteEssencePower()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerTarget = UnitTarget?.ToPlayer();

            if (!playerTarget)
                return;

            Item heartOfAzeroth = playerTarget.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

            if (heartOfAzeroth == null)
                return;

            AzeriteItem azeriteItem = heartOfAzeroth.ToAzeriteItem();

            if (azeriteItem == null)
                return;

            // remove old rank and apply new one
            if (azeriteItem.IsEquipped())
            {
                SelectedAzeriteEssences selectedEssences = azeriteItem.GetSelectedAzeriteEssences();

                if (selectedEssences != null)
                    for (int slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
                        if (selectedEssences.AzeriteEssenceID[slot] == EffectInfo.MiscValue)
                        {
                            bool major = (AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(slot).Type == AzeriteItemMilestoneType.MajorEssence;
                            playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)EffectInfo.MiscValue, SharedConst.MaxAzeriteEssenceRank, major, false);
                            playerTarget.ApplyAzeriteEssence(azeriteItem, (uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB, major, false);

                            break;
                        }
            }

            azeriteItem.SetEssenceRank((uint)EffectInfo.MiscValue, (uint)EffectInfo.MiscValueB);
            azeriteItem.SetState(ItemUpdateState.Changed, playerTarget);
        }

        [SpellEffectHandler(SpellEffectName.CreatePrivateConversation)]
        private void EffectCreatePrivateConversation()
        {
            if (_effectHandleMode != SpellEffectHandleMode.Hit)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null ||
                !unitCaster.IsPlayer())
                return;

            Conversation.CreateConversation((uint)EffectInfo.MiscValue, unitCaster, DestTarget.GetPosition(), unitCaster.GetGUID(), GetSpellInfo());
        }

        [SpellEffectHandler(SpellEffectName.SendChatMessage)]
        private void EffectSendChatMessage()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Unit unitCaster = GetUnitCasterForEffectHandlers();

            if (unitCaster == null)
                return;

            uint broadcastTextId = (uint)EffectInfo.MiscValue;

            if (!CliDB.BroadcastTextStorage.ContainsKey(broadcastTextId))
                return;

            ChatMsg chatType = (ChatMsg)EffectInfo.MiscValueB;
            unitCaster.Talk(broadcastTextId, chatType, Global.CreatureTextMgr.GetRangeForChatType(chatType), UnitTarget);
        }

        [SpellEffectHandler(SpellEffectName.GrantBattlepetExperience)]
        private void EffectGrantBattlePetExperience()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster == null)
                return;

            if (!UnitTarget ||
                !UnitTarget.IsCreature())
                return;

            playerCaster.GetSession().GetBattlePetMgr().GrantBattlePetExperience(UnitTarget.GetBattlePetCompanionGUID(), (ushort)Damage, BattlePetXpSource.SpellEffect);
        }

        [SpellEffectHandler(SpellEffectName.LearnTransmogIllusion)]
        private void EffectLearnTransmogIllusion()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player player = UnitTarget?.ToPlayer();

            if (player == null)
                return;

            uint illusionId = (uint)EffectInfo.MiscValue;

            if (!CliDB.TransmogIllusionStorage.ContainsKey(illusionId))
                return;

            player.GetSession().GetCollectionMgr().AddTransmogIllusion(illusionId);
        }

        [SpellEffectHandler(SpellEffectName.ModifyAuraStacks)]
        private void EffectModifyAuraStacks()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Aura targetAura = UnitTarget.GetAura(EffectInfo.TriggerSpell);

            if (targetAura == null)
                return;

            switch (EffectInfo.MiscValue)
            {
                case 0:
                    targetAura.ModStackAmount(Damage);

                    break;
                case 1:
                    targetAura.SetStackAmount((byte)Damage);

                    break;
                default:
                    break;
            }
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldown)]
        private void EffectModifyCooldown()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            UnitTarget.GetSpellHistory().ModifyCooldown(EffectInfo.TriggerSpell, TimeSpan.FromMilliseconds(Damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldowns)]
        private void EffectModifyCooldowns()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            UnitTarget.GetSpellHistory()
                      .ModifyCoooldowns(itr =>
                                        {
                                            SpellInfo spellOnCooldown = Global.SpellMgr.GetSpellInfo(itr.SpellId, Difficulty.None);

                                            if ((int)spellOnCooldown.SpellFamilyName != EffectInfo.MiscValue)
                                                return false;

                                            int bitIndex = EffectInfo.MiscValueB - 1;

                                            if (bitIndex < 0 ||
                                                bitIndex >= sizeof(uint) * 8)
                                                return false;

                                            FlagArray128 reqFlag = new();
                                            reqFlag[bitIndex / 32] = 1u << (bitIndex % 32);

                                            return (spellOnCooldown.SpellFamilyFlags & reqFlag);
                                        },
                                        TimeSpan.FromMilliseconds(Damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCooldownsByCategory)]
        private void EffectModifyCooldownsByCategory()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            UnitTarget.GetSpellHistory().ModifyCoooldowns(itr => Global.SpellMgr.GetSpellInfo(itr.SpellId, Difficulty.None).CategoryId == EffectInfo.MiscValue, TimeSpan.FromMilliseconds(Damage));
        }

        [SpellEffectHandler(SpellEffectName.ModifyCharges)]
        private void EffectModifySpellCharges()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            for (int i = 0; i < Damage; ++i)
                UnitTarget.GetSpellHistory().RestoreCharge((uint)EffectInfo.MiscValue);
        }

        [SpellEffectHandler(SpellEffectName.CreateTraitTreeConfig)]
        private void EffectCreateTraitTreeConfig()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player target = UnitTarget?.ToPlayer();

            if (target == null)
                return;

            TraitConfigPacket newConfig = new();
            newConfig.Type = TraitMgr.GetConfigTypeForTree(EffectInfo.MiscValue);

            if (newConfig.Type != TraitConfigType.Generic)
                return;

            newConfig.TraitSystemID = CliDB.TraitTreeStorage.LookupByKey(EffectInfo.MiscValue).TraitSystemID;
            target.CreateTraitConfig(newConfig);
        }

        [SpellEffectHandler(SpellEffectName.ChangeActiveCombatTraitConfig)]
        private void EffectChangeActiveCombatTraitConfig()
        {
            if (_effectHandleMode != SpellEffectHandleMode.HitTarget)
                return;

            Player target = UnitTarget?.ToPlayer();

            if (target == null)
                return;

            if (CustomArg is not TraitConfigPacket)
                return;

            target.UpdateTraitConfig(CustomArg as TraitConfigPacket, Damage, false);
        }
    }
}