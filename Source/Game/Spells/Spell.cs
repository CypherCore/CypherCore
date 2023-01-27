// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.ISpell;

namespace Game.Spells
{
	public partial class Spell : IDisposable
	{
		private static List<ISpellScript> _dummy = new();
		private static List<(ISpellScript, ISpellEffect)> _dummySpellEffects = new();
		private readonly Dictionary<Type, List<ISpellScript>> _spellScriptsByType = new();
		private Dictionary<uint, Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>> _effectHandlers = new();

		private List<SpellScript> _loadedScripts = new();

		public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGUID = default, ObjectGuid originalCastId = default)
		{
			_spellInfo     = info;
			_caster        = (info.HasAttribute(SpellAttr6.OriginateFromController) && caster.GetCharmerOrOwner() != null ? caster.GetCharmerOrOwner() : caster);
			_spellValue    = new SpellValue(_spellInfo, caster);
			_castItemLevel = -1;

			if (IsIgnoringCooldowns())
				_castFlagsEx |= SpellCastFlagsEx.IgnoreCooldown;

			_castId                          = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, _caster.GetMapId(), _spellInfo.Id, _caster.GetMap().GenerateLowGuid(HighGuid.Cast));
			_originalCastId                  = originalCastId;
			_SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(_spellInfo);

			_customError     = SpellCustomErrors.None;
			_fromClient      = false;
			_needComboPoints = _spellInfo.NeedsComboPoints();

			// Get data for type of attack
			_attackType = info.GetAttackType();

			_spellSchoolMask = _spellInfo.GetSchoolMask(); // Can be override for some spell (wand shoot for example)

			Player playerCaster = _caster.ToPlayer();

			if (playerCaster != null)
				// wand case
				if (_attackType == WeaponAttackType.RangedAttack)
					if ((playerCaster.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
					{
						Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

						if (pItem != null)
							_spellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.GetTemplate().GetDamageType());
					}

			Player modOwner = caster.GetSpellModOwner();

			if (modOwner != null)
				modOwner.ApplySpellMod(info, SpellModOp.Doses, ref _spellValue.AuraStackAmount, this);

			if (!originalCasterGUID.IsEmpty())
				_originalCasterGUID = originalCasterGUID;
			else
				_originalCasterGUID = _caster.GetGUID();

			if (_originalCasterGUID == _caster.GetGUID())
			{
				_originalCaster = _caster.ToUnit();
			}
			else
			{
				_originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGUID);

				if (_originalCaster != null &&
				    !_originalCaster.IsInWorld)
					_originalCaster = null;
			}

			_spellState         = SpellState.None;
			_triggeredCastFlags = triggerFlags;

			if (info.HasAttribute(SpellAttr2.DoNotReportSpellFailure))
				_triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.DontReportCastError;

			if (_spellInfo.HasAttribute(SpellAttr4.AllowCastWhileCasting))
				_triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.IgnoreCastInProgress;

			effectHandleMode = SpellEffectHandleMode.Launch;

			//Auto Shot & Shoot (wand)
			_autoRepeat = _spellInfo.IsAutoRepeatRangedSpell();

			// Determine if spell can be reflected back to the caster
			// Patch 1.2 notes: Spell Reflection no longer reflects abilities
			_canReflect = caster.IsUnit() && _spellInfo.DmgClass == SpellDmgClass.Magic && !_spellInfo.HasAttribute(SpellAttr0.IsAbility) && !_spellInfo.HasAttribute(SpellAttr1.NoReflection) && !_spellInfo.HasAttribute(SpellAttr0.NoImmunities) && !_spellInfo.IsPassive();

			CleanupTargetList();

			for (var i = 0; i < SpellConst.MaxEffects; ++i)
				_destTargets[i] = new SpellDestination(_caster);

			//not sure needed.
			_targets     = new SpellCastTargets();
			_appliedMods = new List<Aura>();
		}

		public virtual void Dispose()
		{
			// unload scripts
			for (var i = 0; i < _loadedScripts.Count; ++i)
				_loadedScripts[i]._Unload();

			if (_referencedFromCurrentSpell &&
			    _selfContainer &&
			    _selfContainer == this)
			{
				// Clean the reference to avoid later crash.
				// If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
				Log.outError(LogFilter.Spells, "SPELL: deleting spell for spell ID {0}. However, spell still referenced.", _spellInfo.Id);
				_selfContainer = null;
			}

			if (_caster && _caster.GetTypeId() == TypeId.Player)
				Cypher.Assert(_caster.ToPlayer()._spellModTakingSpell != this);
		}

		private void InitExplicitTargets(SpellCastTargets targets)
		{
			_targets = targets;

			// this function tries to correct spell explicit targets for spell
			// client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
			// this also makes sure that we correctly send explicit targets to client (removes redundant data)
			SpellCastTargetFlags neededTargets = _spellInfo.GetExplicitTargetMask();

			WorldObject target = _targets.GetObjectTarget();

			if (target != null)
			{
				// check if object target is valid with needed target flags
				// for unit case allow corpse target mask because player with not released corpse is a unit target
				if ((target.ToUnit() && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)) ||
				    (target.IsTypeId(TypeId.GameObject) && !neededTargets.HasFlag(SpellCastTargetFlags.GameobjectMask)) ||
				    (target.IsTypeId(TypeId.Corpse) && !neededTargets.HasFlag(SpellCastTargetFlags.CorpseMask)))
					_targets.RemoveObjectTarget();
			}
			else
			{
				// try to select correct unit target if not provided by client or by serverside cast
				if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
				{
					Unit unit = null;
					// try to use player selection as a target
					Player playerCaster = _caster.ToPlayer();

					if (playerCaster != null)
					{
						// selection has to be found and to be valid target for the spell
						Unit selectedUnit = Global.ObjAccessor.GetUnit(_caster, playerCaster.GetTarget());

						if (selectedUnit != null)
							if (_spellInfo.CheckExplicitTarget(_caster, selectedUnit) == SpellCastResult.SpellCastOk)
								unit = selectedUnit;
					}
					// try to use attacked unit as a target
					else if (_caster.IsTypeId(TypeId.Unit) &&
					         neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit))
					{
						unit = _caster.ToUnit().GetVictim();
					}

					// didn't find anything - let's use self as target
					if (unit == null &&
					    neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly))
						unit = _caster.ToUnit();

					_targets.SetUnitTarget(unit);
				}
			}

			// check if spell needs dst target
			if (neededTargets.HasFlag(SpellCastTargetFlags.DestLocation))
			{
				// and target isn't set
				if (!_targets.HasDst())
				{
					// try to use unit target if provided
					WorldObject targett = targets.GetObjectTarget();

					if (targett != null)
						_targets.SetDst(targett);
					// or use self if not available
					else
						_targets.SetDst(_caster);
				}
			}
			else
			{
				_targets.RemoveDst();
			}

			if (neededTargets.HasFlag(SpellCastTargetFlags.SourceLocation))
			{
				if (!targets.HasSrc())
					_targets.SetSrc(_caster);
			}
			else
			{
				_targets.RemoveSrc();
			}
		}

		private void SelectExplicitTargets()
		{
			// here go all explicit target changes made to explicit targets after spell prepare phase is finished
			Unit target = _targets.GetUnitTarget();

			if (target != null)
				// check for explicit target redirection, for Grounding Totem for example
				if (_spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitEnemy) ||
				    (_spellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.Unit) && !_caster.IsFriendlyTo(target)))
				{
					Unit redirect = null;

					switch (_spellInfo.DmgClass)
					{
						case SpellDmgClass.Magic:
							redirect = _caster.GetMagicHitRedirectTarget(target, _spellInfo);

							break;
						case SpellDmgClass.Melee:
						case SpellDmgClass.Ranged:
							// should gameobjects cast damagetype melee/ranged spells this needs to be changed
							redirect = _caster.ToUnit().GetMeleeHitRedirectTarget(target, _spellInfo);

							break;
						default:
							break;
					}

					if (redirect != null &&
					    (redirect != target))
						_targets.SetUnitTarget(redirect);
				}
		}

		public void SelectSpellTargets()
		{
			// select targets for cast phase
			SelectExplicitTargets();

			uint processedAreaEffectsMask = 0;

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				// not call for empty effect.
				// Also some spells use not used effect targets for store targets for dummy effect in triggered spells
				if (!spellEffectInfo.IsEffect())
					continue;

				// set expected type of implicit targets to be sent to client
				SpellCastTargetFlags implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.GetObjectType());

				if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
					_targets.SetTargetFlag(SpellCastTargetFlags.Unit);

				if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
					_targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

				SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, ref processedAreaEffectsMask);
				SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, ref processedAreaEffectsMask);

				// Select targets of effect based on effect type
				// those are used when no valid target could be added for spell effect based on spell target type
				// some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
				// some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
				// some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
				SelectEffectTypeImplicitTargets(spellEffectInfo);

				if (_targets.HasDst())
					AddDestTarget(_targets.GetDst(), spellEffectInfo.EffectIndex);

				if (spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.Unit ||
				    spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.UnitAndDest ||
				    spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.Unit ||
				    spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
				{
					if (_spellInfo.HasAttribute(SpellAttr1.RequireAllTargets))
					{
						bool noTargetFound = !_UniqueTargetInfo.Any(target => (target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0);

						if (noTargetFound)
						{
							SendCastResult(SpellCastResult.BadImplicitTargets);
							Finish(false);

							return;
						}
					}

					if (_spellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
					{
						bool anyNonImmuneTargetFound = _UniqueTargetInfo.Any(target => (target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 && target.MissCondition != SpellMissInfo.Immune && target.MissCondition != SpellMissInfo.Immune2);

						if (!anyNonImmuneTargetFound)
						{
							SendCastResult(SpellCastResult.Immune);
							Finish(false);

							return;
						}
					}
				}

				if (_spellInfo.IsChanneled())
				{
					// maybe do this for all spells?
					if (focusObject == null &&
					    _UniqueTargetInfo.Empty() &&
					    _UniqueGOTargetInfo.Empty() &&
					    _UniqueItemInfo.Empty() &&
					    !_targets.HasDst())
					{
						SendCastResult(SpellCastResult.BadImplicitTargets);
						Finish(false);

						return;
					}

					uint mask = (1u << (int)spellEffectInfo.EffectIndex);

					foreach (var ihit in _UniqueTargetInfo)
						if (Convert.ToBoolean(ihit.EffectMask & mask))
						{
							_channelTargetEffectMask |= mask;

							break;
						}
				}
			}

			ulong dstDelay = CalculateDelayMomentForDst(_spellInfo.LaunchDelay);

			if (dstDelay != 0)
				_delayMoment = dstDelay;
		}

		private ulong CalculateDelayMomentForDst(float launchDelay)
		{
			if (_targets.HasDst())
			{
				if (_targets.HasTraj())
				{
					float speed = _targets.GetSpeedXY();

					if (speed > 0.0f)
						return (ulong)(Math.Floor((_targets.GetDist2d() / speed + launchDelay) * 1000.0f));
				}
				else if (_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
				{
					return (ulong)(Math.Floor((_spellInfo.Speed + launchDelay) * 1000.0f));
				}
				else if (_spellInfo.Speed > 0.0f)
				{
					// We should not subtract caster size from dist calculation (fixes execution time desync with animation on client, eg. Malleable Goo cast by PP)
					float dist = _caster.GetExactDist(_targets.GetDstPos());

					return (ulong)(Math.Floor((dist / _spellInfo.Speed + launchDelay) * 1000.0f));
				}

				return (ulong)Math.Floor(launchDelay * 1000.0f);
			}

			return 0;
		}

		public void RecalculateDelayMomentForDst()
		{
			_delayMoment = CalculateDelayMomentForDst(0.0f);
			_caster._Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(GetDelayStart() + _delayMoment));
		}

		private void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, ref uint processedEffectMask)
		{
			if (targetType.GetTarget() == 0)
				return;

			uint effectMask = (1u << (int)spellEffectInfo.EffectIndex);

			// set the same target list for all effects
			// some spells appear to need this, however this requires more research
			switch (targetType.GetSelectionCategory())
			{
				case SpellTargetSelectionCategories.Nearby:
				case SpellTargetSelectionCategories.Cone:
				case SpellTargetSelectionCategories.Area:
				case SpellTargetSelectionCategories.Line:
				{
					// targets for effect already selected
					if (Convert.ToBoolean(effectMask & processedEffectMask))
						return;

					var effects = GetSpellInfo().GetEffects();

					// choose which targets we can select at once
					for (int j = (int)spellEffectInfo.EffectIndex + 1; j < effects.Count; ++j)
						if (effects[j].IsEffect() &&
						    spellEffectInfo.TargetA.GetTarget() == effects[j].TargetA.GetTarget() &&
						    spellEffectInfo.TargetB.GetTarget() == effects[j].TargetB.GetTarget() &&
						    spellEffectInfo.ImplicitTargetConditions == effects[j].ImplicitTargetConditions &&
						    spellEffectInfo.CalcRadius(_caster) == effects[j].CalcRadius(_caster) &&
						    CheckScriptEffectImplicitTargets(spellEffectInfo.EffectIndex, (uint)j))
							effectMask |= 1u << j;

					processedEffectMask |= effectMask;

					break;
				}
				default:
					break;
			}

			switch (targetType.GetSelectionCategory())
			{
				case SpellTargetSelectionCategories.Channel:
					SelectImplicitChannelTargets(spellEffectInfo, targetType);

					break;
				case SpellTargetSelectionCategories.Nearby:
					SelectImplicitNearbyTargets(spellEffectInfo, targetType, effectMask);

					break;
				case SpellTargetSelectionCategories.Cone:
					SelectImplicitConeTargets(spellEffectInfo, targetType, effectMask);

					break;
				case SpellTargetSelectionCategories.Area:
					SelectImplicitAreaTargets(spellEffectInfo, targetType, effectMask);

					break;
				case SpellTargetSelectionCategories.Traj:
					// just in case there is no dest, explanation in SelectImplicitDestDestTargets
					CheckDst();

					SelectImplicitTrajTargets(spellEffectInfo, targetType);

					break;
				case SpellTargetSelectionCategories.Line:
					SelectImplicitLineTargets(spellEffectInfo, targetType, effectMask);

					break;
				case SpellTargetSelectionCategories.Default:
					switch (targetType.GetObjectType())
					{
						case SpellTargetObjectTypes.Src:
							switch (targetType.GetReferenceType())
							{
								case SpellTargetReferenceTypes.Caster:
									_targets.SetSrc(_caster);

									break;
								default:
									Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_SRC");

									break;
							}

							break;
						case SpellTargetObjectTypes.Dest:
							switch (targetType.GetReferenceType())
							{
								case SpellTargetReferenceTypes.Caster:
									SelectImplicitCasterDestTargets(spellEffectInfo, targetType);

									break;
								case SpellTargetReferenceTypes.Target:
									SelectImplicitTargetDestTargets(spellEffectInfo, targetType);

									break;
								case SpellTargetReferenceTypes.Dest:
									SelectImplicitDestDestTargets(spellEffectInfo, targetType);

									break;
								default:
									Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT_DEST");

									break;
							}

							break;
						default:
							switch (targetType.GetReferenceType())
							{
								case SpellTargetReferenceTypes.Caster:
									SelectImplicitCasterObjectTargets(spellEffectInfo, targetType);

									break;
								case SpellTargetReferenceTypes.Target:
									SelectImplicitTargetObjectTargets(spellEffectInfo, targetType);

									break;
								default:
									Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target reference type for TARGET_TYPE_OBJECT");

									break;
							}

							break;
					}

					break;
				case SpellTargetSelectionCategories.Nyi:
					Log.outDebug(LogFilter.Spells, "SPELL: target type {0}, found in spellID {1}, effect {2} is not implemented yet!", _spellInfo.Id, spellEffectInfo.EffectIndex, targetType.GetTarget());

					break;
				default:
					Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select target category");

					break;
			}
		}

		private void SelectImplicitChannelTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
			{
				Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target reference type");

				return;
			}

			Spell channeledSpell = _originalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);

			if (channeledSpell == null)
			{
				Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", _spellInfo.Id, spellEffectInfo.EffectIndex);

				return;
			}

			switch (targetType.GetTarget())
			{
				case Targets.UnitChannelTarget:
				{
					foreach (ObjectGuid channelTarget in _originalCaster._unitData.ChannelObjects)
					{
						WorldObject target = Global.ObjAccessor.GetUnit(_caster, channelTarget);
						CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
						// unit target may be no longer avalible - teleported out of map for example
						Unit unitTarget = target ? target.ToUnit() : null;

						if (unitTarget)
							AddUnitTarget(unitTarget, 1u << (int)spellEffectInfo.EffectIndex);
						else
							Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell target for spell ID {0}, effect {1}", _spellInfo.Id, spellEffectInfo.EffectIndex);
					}

					break;
				}
				case Targets.DestChannelTarget:
				{
					if (channeledSpell._targets.HasDst())
					{
						_targets.SetDst(channeledSpell._targets);
					}
					else
					{
						List<ObjectGuid> channelObjects = _originalCaster._unitData.ChannelObjects;
						WorldObject      target         = !channelObjects.Empty() ? Global.ObjAccessor.GetWorldObject(_caster, channelObjects[0]) : null;

						if (target != null)
						{
							CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

							if (target)
							{
								SpellDestination dest = new(target);

								if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
									dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

								CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
								_targets.SetDst(dest);
							}
						}
						else
						{
							Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", _spellInfo.Id, spellEffectInfo.EffectIndex);
						}
					}

					break;
				}
				case Targets.DestChannelCaster:
				{
					SpellDestination dest = new(channeledSpell.GetCaster());

					if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
						dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

					CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
					_targets.SetDst(dest);

					break;
				}
				default:
					Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented target type");

					break;
			}
		}

		private void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
		{
			if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
			{
				Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target reference type");

				return;
			}

			float range = 0.0f;

			switch (targetType.GetCheckType())
			{
				case SpellTargetCheckTypes.Enemy:
					range = _spellInfo.GetMaxRange(false, _caster, this);

					break;
				case SpellTargetCheckTypes.Ally:
				case SpellTargetCheckTypes.Party:
				case SpellTargetCheckTypes.Raid:
				case SpellTargetCheckTypes.RaidClass:
					range = _spellInfo.GetMaxRange(true, _caster, this);

					break;
				case SpellTargetCheckTypes.Entry:
				case SpellTargetCheckTypes.Default:
					range = _spellInfo.GetMaxRange(IsPositive(), _caster, this);

					break;
				default:
					Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented selection check type");

					break;
			}

			List<Condition> condList = spellEffectInfo.ImplicitTargetConditions;

			// handle emergency case - try to use other provided targets if no conditions provided
			if (targetType.GetCheckType() == SpellTargetCheckTypes.Entry &&
			    (condList == null || condList.Empty()))
			{
				Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: no conditions entry for target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", _spellInfo.Id, spellEffectInfo.EffectIndex);

				switch (targetType.GetObjectType())
				{
					case SpellTargetObjectTypes.Gobj:
						if (_spellInfo.RequiresSpellFocus != 0)
						{
							if (focusObject != null)
							{
								AddGOTarget(focusObject, effMask);
							}
							else
							{
								SendCastResult(SpellCastResult.BadImplicitTargets);
								Finish(false);
							}

							return;
						}

						break;
					case SpellTargetObjectTypes.Dest:
						if (_spellInfo.RequiresSpellFocus != 0)
						{
							if (focusObject != null)
							{
								SpellDestination dest = new(focusObject);

								if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
									dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

								CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
								_targets.SetDst(dest);
							}
							else
							{
								SendCastResult(SpellCastResult.BadImplicitTargets);
								Finish(false);
							}

							return;
						}

						break;
					default:
						break;
				}
			}

			WorldObject target = SearchNearbyTarget(range, targetType.GetObjectType(), targetType.GetCheckType(), condList);

			if (target == null)
			{
				Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", _spellInfo.Id, spellEffectInfo.EffectIndex);
				SendCastResult(SpellCastResult.BadImplicitTargets);
				Finish(false);

				return;
			}

			CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

			if (!target)
			{
				Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {_spellInfo.Id} set NULL target, effect {spellEffectInfo.EffectIndex}");
				SendCastResult(SpellCastResult.BadImplicitTargets);
				Finish(false);

				return;
			}

			switch (targetType.GetObjectType())
			{
				case SpellTargetObjectTypes.Unit:
					Unit unitTarget = target.ToUnit();

					if (unitTarget != null)
					{
						AddUnitTarget(unitTarget, effMask, true, false);
					}
					else
					{
						Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {_spellInfo.Id} set object of wrong type, expected unit, got {target.GetGUID().GetHigh()}, effect {effMask}");
						SendCastResult(SpellCastResult.BadImplicitTargets);
						Finish(false);

						return;
					}

					break;
				case SpellTargetObjectTypes.Gobj:
					GameObject gobjTarget = target.ToGameObject();

					if (gobjTarget != null)
					{
						AddGOTarget(gobjTarget, effMask);
					}
					else
					{
						Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {_spellInfo.Id} set object of wrong type, expected gameobject, got {target.GetGUID().GetHigh()}, effect {effMask}");
						SendCastResult(SpellCastResult.BadImplicitTargets);
						Finish(false);

						return;
					}

					break;
				case SpellTargetObjectTypes.Corpse:
					Corpse corpseTarget = target.ToCorpse();

					if (corpseTarget != null)
					{
						AddCorpseTarget(corpseTarget, effMask);
					}
					else
					{
						Log.outDebug(LogFilter.Spells, $"Spell::SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {_spellInfo.Id} set object of wrong type, expected corpse, got {target.GetGUID().GetTypeId()}, effect {effMask}");
						SendCastResult(SpellCastResult.BadImplicitTargets);
						Finish(false);

						return;
					}

					break;
				case SpellTargetObjectTypes.Dest:
					SpellDestination dest = new(target);

					if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
						dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

					CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
					_targets.SetDst(dest);

					break;
				default:
					Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented target object type");

					break;
			}

			SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
		}

		private void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
		{
			Position coneSrc   = new(_caster);
			float    coneAngle = _spellInfo.ConeAngle;

			switch (targetType.GetReferenceType())
			{
				case SpellTargetReferenceTypes.Caster:
					break;
				case SpellTargetReferenceTypes.Dest:
					if (_caster.GetExactDist2d(_targets.GetDstPos()) > 0.1f)
						coneSrc.SetOrientation(_caster.GetAbsoluteAngle(_targets.GetDstPos()));

					break;
				default:
					break;
			}

			switch (targetType.GetTarget())
			{
				case Targets.UnitCone180DegEnemy:
					if (coneAngle == 0.0f)
						coneAngle = 180.0f;

					break;
				default:
					break;
			}

			List<WorldObject>      targets       = new();
			SpellTargetObjectTypes objectType    = targetType.GetObjectType();
			SpellTargetCheckTypes  selectionType = targetType.GetCheckType();

			var   condList = spellEffectInfo.ImplicitTargetConditions;
			float radius   = spellEffectInfo.CalcRadius(_caster) * _spellValue.RadiusMod;

			GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

			if (containerTypeMask != 0)
			{
				float extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
				var   spellCone         = new WorldObjectSpellConeTargetCheck(coneSrc, MathFunctions.DegToRad(coneAngle), _spellInfo.Width != 0 ? _spellInfo.Width : _caster.GetCombatReach(), radius, _caster, _spellInfo, selectionType, condList, objectType);
				var   searcher          = new WorldObjectListSearcher(_caster, targets, spellCone, containerTypeMask);
				SearchTargets(searcher, containerTypeMask, _caster, _caster.GetPosition(), radius + extraSearchRadius);

				CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

				if (!targets.Empty())
				{
					// Other special target selection goes here
					uint maxTargets = _spellValue.MaxAffectedTargets;

					if (maxTargets != 0)
						targets.RandomResize(maxTargets);

					foreach (var obj in targets)
						if (obj.IsUnit())
							AddUnitTarget(obj.ToUnit(), effMask, false);
						else if (obj.IsGameObject())
							AddGOTarget(obj.ToGameObject(), effMask);
						else if (obj.IsCorpse())
							AddCorpseTarget(obj.ToCorpse(), effMask);
				}
			}
		}

		private void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
		{
			WorldObject referer;

			switch (targetType.GetReferenceType())
			{
				case SpellTargetReferenceTypes.Src:
				case SpellTargetReferenceTypes.Dest:
				case SpellTargetReferenceTypes.Caster:
					referer = _caster;

					break;
				case SpellTargetReferenceTypes.Target:
					referer = _targets.GetUnitTarget();

					break;
				case SpellTargetReferenceTypes.Last:
				{
					referer = _caster;

					// find last added target for this effect
					foreach (var target in _UniqueTargetInfo)
						if (Convert.ToBoolean(target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)))
						{
							referer = Global.ObjAccessor.GetUnit(_caster, target.TargetGUID);

							break;
						}

					break;
				}
				default:
					Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");

					return;
			}

			if (referer == null)
				return;

			Position center;

			switch (targetType.GetReferenceType())
			{
				case SpellTargetReferenceTypes.Src:
					center = _targets.GetSrcPos();

					break;
				case SpellTargetReferenceTypes.Dest:
					center = _targets.GetDstPos();

					break;
				case SpellTargetReferenceTypes.Caster:
				case SpellTargetReferenceTypes.Target:
				case SpellTargetReferenceTypes.Last:
					center = referer.GetPosition();

					break;
				default:
					Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented target reference type");

					return;
			}

			float             radius  = spellEffectInfo.CalcRadius(_caster) * _spellValue.RadiusMod;
			List<WorldObject> targets = new();

			switch (targetType.GetTarget())
			{
				case Targets.UnitCasterAndPassengers:
					targets.Add(_caster);
					Unit unit = _caster.ToUnit();

					if (unit != null)
					{
						Vehicle vehicleKit = unit.GetVehicleKit();

						if (vehicleKit != null)
							for (sbyte seat = 0; seat < SharedConst.MaxVehicleSeats; ++seat)
							{
								Unit passenger = vehicleKit.GetPassenger(seat);

								if (passenger != null)
									targets.Add(passenger);
							}
					}

					break;
				case Targets.UnitTargetAllyOrRaid:
					Unit targetedUnit = _targets.GetUnitTarget();

					if (targetedUnit != null)
					{
						if (!_caster.IsUnit() ||
						    !_caster.ToUnit().IsInRaidWith(targetedUnit))
							targets.Add(_targets.GetUnitTarget());
						else
							SearchAreaTargets(targets, radius, targetedUnit, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);
					}

					break;
				case Targets.UnitCasterAndSummons:
					targets.Add(_caster);
					SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);

					break;
				default:
					SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);

					break;
			}

			if (targetType.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
			{
				SpellDestination dest = new(referer);

				if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
					dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

				CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);

				_targets.ModDst(dest);
			}

			CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

			if (targetType.GetTarget() == Targets.UnitSrcAreaFurthestEnemy)
				targets.Sort(new ObjectDistanceOrderPred(referer, false));

			if (!targets.Empty())
			{
				// Other special target selection goes here
				uint maxTargets = _spellValue.MaxAffectedTargets;

				if (maxTargets != 0)
				{
					if (targetType.GetTarget() != Targets.UnitSrcAreaFurthestEnemy)
						targets.RandomResize(maxTargets);
					else if (targets.Count > maxTargets)
						targets.Resize(maxTargets);
				}

				foreach (var obj in targets)
					if (obj.IsUnit())
						AddUnitTarget(obj.ToUnit(), effMask, false, true, center);
					else if (obj.IsGameObject())
						AddGOTarget(obj.ToGameObject(), effMask);
					else if (obj.IsCorpse())
						AddCorpseTarget(obj.ToCorpse(), effMask);
			}
		}

		private void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			SpellDestination dest = new(_caster);

			switch (targetType.GetTarget())
			{
				case Targets.DestCaster:
					break;
				case Targets.DestHome:
					Player playerCaster = _caster.ToPlayer();

					if (playerCaster != null)
						dest = new SpellDestination(playerCaster.GetHomebind());

					break;
				case Targets.DestDb:
					SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(_spellInfo.Id, spellEffectInfo.EffectIndex);

					if (st != null)
					{
						// @todo fix this check
						if (_spellInfo.HasEffect(SpellEffectName.TeleportUnits) ||
						    _spellInfo.HasEffect(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen) ||
						    _spellInfo.HasEffect(SpellEffectName.Bind))
							dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation, st.target_mapId);
						else if (st.target_mapId == _caster.GetMapId())
							dest = new SpellDestination(st.target_X, st.target_Y, st.target_Z, st.target_Orientation);
					}
					else
					{
						Log.outDebug(LogFilter.Spells, "SPELL: unknown target coordinates for spell ID {0}", _spellInfo.Id);
						WorldObject target = _targets.GetObjectTarget();

						if (target)
							dest = new SpellDestination(target);
					}

					break;
				case Targets.DestCasterFishing:
				{
					float minDist = _spellInfo.GetMinRange(true);
					float maxDist = _spellInfo.GetMaxRange(true);
					float dis     = (float)RandomHelper.NextDouble() * (maxDist - minDist) + minDist;
					float x, y, z;
					float angle = (float)RandomHelper.NextDouble() * (MathFunctions.PI * 35.0f / 180.0f) - (float)(Math.PI * 17.5f / 180.0f);
					_caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

					float      ground      = _caster.GetMapHeight(x, y, z);
					float      liquidLevel = MapConst.VMAPInvalidHeightValue;
					LiquidData liquidData  = new();

					if (_caster.GetMap().GetLiquidStatus(_caster.GetPhaseShift(), x, y, z, LiquidHeaderTypeFlags.AllLiquids, liquidData, _caster.GetCollisionHeight()) != 0)
						liquidLevel = liquidData.level;

					if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
					{
						SendCastResult(SpellCastResult.NotHere);
						SendChannelUpdate(0);
						Finish(false);

						return;
					}

					if (ground + 0.75 > liquidLevel)
					{
						SendCastResult(SpellCastResult.TooShallow);
						SendChannelUpdate(0);
						Finish(false);

						return;
					}

					dest = new SpellDestination(x, y, liquidLevel, _caster.GetOrientation());

					break;
				}
				case Targets.DestCasterFrontLeap:
				case Targets.DestCasterMovementDirection:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster == null)
						break;

					float dist  = spellEffectInfo.CalcRadius(unitCaster);
					float angle = targetType.CalcDirectionAngle();

					if (targetType.GetTarget() == Targets.DestCasterMovementDirection)
						switch (_caster._movementInfo.GetMovementFlags() & (MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight))
						{
							case MovementFlag.None:
							case MovementFlag.Forward:
							case MovementFlag.Forward | MovementFlag.Backward:
							case MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
							case MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
							case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
								angle = 0.0f;

								break;
							case MovementFlag.Backward:
							case MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
								angle = MathF.PI;

								break;
							case MovementFlag.StrafeLeft:
							case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft:
								angle = (MathF.PI / 2);

								break;
							case MovementFlag.Forward | MovementFlag.StrafeLeft:
								angle = (MathF.PI / 4);

								break;
							case MovementFlag.Backward | MovementFlag.StrafeLeft:
								angle = (3 * MathF.PI / 4);

								break;
							case MovementFlag.StrafeRight:
							case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeRight:
								angle = (-MathF.PI / 2);

								break;
							case MovementFlag.Forward | MovementFlag.StrafeRight:
								angle = (-MathF.PI / 4);

								break;
							case MovementFlag.Backward | MovementFlag.StrafeRight:
								angle = (-3 * MathF.PI / 4);

								break;
							default:
								angle = 0.0f;

								break;
						}

					Position pos = new(dest.Position);

					unitCaster.MovePositionToFirstCollision(pos, dist, angle);
					dest.Relocate(pos);

					break;
				}
				case Targets.DestCasterGround:
				case Targets.DestCasterGround2:
					dest.Position.posZ = _caster.GetMapWaterOrGroundLevel(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());

					break;
				case Targets.DestSummoner:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster != null)
					{
						TempSummon casterSummon = unitCaster.ToTempSummon();

						if (casterSummon != null)
						{
							WorldObject summoner = casterSummon.GetSummoner();

							if (summoner != null)
								dest = new SpellDestination(summoner);
						}
					}

					break;
				}
				default:
				{
					float dist    = spellEffectInfo.CalcRadius(_caster);
					float angl    = targetType.CalcDirectionAngle();
					float objSize = _caster.GetCombatReach();

					switch (targetType.GetTarget())
					{
						case Targets.DestCasterSummon:
							dist = SharedConst.PetFollowDist;

							break;
						case Targets.DestCasterRandom:
							if (dist > objSize)
								dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();

							break;
						case Targets.DestCasterFrontLeft:
						case Targets.DestCasterBackLeft:
						case Targets.DestCasterFrontRight:
						case Targets.DestCasterBackRight:
						{
							float DefaultTotemDistance = 3.0f;

							if (!spellEffectInfo.HasRadius() &&
							    !spellEffectInfo.HasMaxRadius())
								dist = DefaultTotemDistance;

							break;
						}
						default:
							break;
					}

					if (dist < objSize)
						dist = objSize;

					Position pos = new(dest.Position);
					_caster.MovePositionToFirstCollision(pos, dist, angl);

					dest.Relocate(pos);

					break;
				}
			}

			if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
				dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

			CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
			_targets.SetDst(dest);
		}

		private void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			WorldObject target = _targets.GetObjectTarget();

			SpellDestination dest = new(target);

			switch (targetType.GetTarget())
			{
				case Targets.DestTargetEnemy:
				case Targets.DestAny:
				case Targets.DestTargetAlly:
					break;
				default:
				{
					float angle = targetType.CalcDirectionAngle();
					float dist  = spellEffectInfo.CalcRadius(null);

					if (targetType.GetTarget() == Targets.DestRandom)
						dist *= (float)RandomHelper.NextDouble();

					Position pos = new(dest.Position);
					target.MovePositionToFirstCollision(pos, dist, angle);

					dest.Relocate(pos);
				}

					break;
			}

			if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
				dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

			CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
			_targets.SetDst(dest);
		}

		private void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			// set destination to caster if no dest provided
			// can only happen if previous destination target could not be set for some reason
			// (not found nearby target, or channel target for example
			// maybe we should abort the spell in such case?
			CheckDst();

			SpellDestination dest = _targets.GetDst();

			switch (targetType.GetTarget())
			{
				case Targets.DestDynobjEnemy:
				case Targets.DestDynobjAlly:
				case Targets.DestDynobjNone:
				case Targets.DestDest:
					break;
				case Targets.DestDestGround:
					dest.Position.posZ = _caster.GetMapHeight(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());

					break;
				default:
				{
					float angle = targetType.CalcDirectionAngle();
					float dist  = spellEffectInfo.CalcRadius(_caster);

					if (targetType.GetTarget() == Targets.DestRandom)
						dist *= (float)RandomHelper.NextDouble();

					Position pos = new(_targets.GetDstPos());
					_caster.MovePositionToFirstCollision(pos, dist, angle);

					dest.Relocate(pos);
				}

					break;
			}

			if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
				dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

			CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
			_targets.ModDst(dest);
		}

		private void SelectImplicitCasterObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			WorldObject target       = null;
			bool        checkIfValid = true;

			switch (targetType.GetTarget())
			{
				case Targets.UnitCaster:
					target       = _caster;
					checkIfValid = false;

					break;
				case Targets.UnitMaster:
					target = _caster.GetCharmerOrOwner();

					break;
				case Targets.UnitPet:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster != null)
						target = unitCaster.GetGuardianPet();

					break;
				}
				case Targets.UnitSummoner:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster != null)
						if (unitCaster.IsSummon())
							target = unitCaster.ToTempSummon().GetSummonerUnit();

					break;
				}
				case Targets.UnitVehicle:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster != null)
						target = unitCaster.GetVehicleBase();

					break;
				}
				case Targets.UnitPassenger0:
				case Targets.UnitPassenger1:
				case Targets.UnitPassenger2:
				case Targets.UnitPassenger3:
				case Targets.UnitPassenger4:
				case Targets.UnitPassenger5:
				case Targets.UnitPassenger6:
				case Targets.UnitPassenger7:
					Creature vehicleBase = _caster.ToCreature();

					if (vehicleBase != null &&
					    vehicleBase.IsVehicle())
						target = vehicleBase.GetVehicleKit().GetPassenger((sbyte)(targetType.GetTarget() - Targets.UnitPassenger0));

					break;
				case Targets.UnitTargetTapList:
					Creature creatureCaster = _caster.ToCreature();

					if (creatureCaster != null &&
					    !creatureCaster.GetTapList().Empty())
						target = Global.ObjAccessor.GetWorldObject(creatureCaster, creatureCaster.GetTapList().SelectRandom());

					break;
				case Targets.UnitOwnCritter:
				{
					Unit unitCaster = _caster.ToUnit();

					if (unitCaster != null)
						target = ObjectAccessor.GetCreatureOrPetOrVehicle(_caster, unitCaster.GetCritterGUID());

					break;
				}
				default:
					break;
			}

			CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

			if (target)
			{
				if (target.IsUnit())
					AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, checkIfValid);
				else if (target.IsGameObject())
					AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
				else if (target.IsCorpse())
					AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
			}
		}

		private void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			WorldObject target = _targets.GetObjectTarget();

			CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

			Item item = _targets.GetItemTarget();

			if (target != null)
			{
				if (target.IsUnit())
					AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, true, false);
				else if (target.IsGameObject())
					AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
				else if (target.IsCorpse())
					AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);

				SelectImplicitChainTargets(spellEffectInfo, targetType, target, 1u << (int)spellEffectInfo.EffectIndex);
			}
			// Script hook can remove object target and we would wrongly land here
			else if (item != null)
			{
				AddItemTarget(item, 1u << (int)spellEffectInfo.EffectIndex);
			}
		}

		private void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, uint effMask)
		{
			int    maxTargets = spellEffectInfo.ChainTargets;
			Player modOwner   = _caster.GetSpellModOwner();

			if (modOwner)
				modOwner.ApplySpellMod(_spellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

			if (maxTargets > 1)
			{
				// mark damage multipliers as used
				for (int k = (int)spellEffectInfo.EffectIndex; k < _spellInfo.GetEffects().Count; ++k)
					if (Convert.ToBoolean(effMask & (1 << (int)k)))
						_damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

				_applyMultiplierMask |= effMask;

				List<WorldObject> targets = new();
				SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo, targetType.GetTarget() == Targets.UnitChainhealAlly);

				// Chain primary target is added earlier
				CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

				Position losPosition = _spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster : target;

				foreach (var obj in targets)
				{
					Unit unitTarget = obj.ToUnit();

					if (unitTarget)
						AddUnitTarget(unitTarget, effMask, false, true, losPosition);

					if (!_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) &&
					    !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
						losPosition = obj;
				}
			}
		}

		private float Tangent(float x)
		{
			x = (float)Math.Tan(x);

			if (x < 100000.0f &&
			    x > -100000.0f) return x;

			if (x >= 100000.0f) return 100000.0f;
			if (x <= 100000.0f) return -100000.0f;

			return 0.0f;
		}

		private void SelectImplicitTrajTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
		{
			if (!_targets.HasTraj())
				return;

			float dist2d = _targets.GetDist2d();

			if (dist2d == 0)
				return;

			Position srcPos = _targets.GetSrcPos();
			srcPos.SetOrientation(_caster.GetOrientation());
			float srcToDestDelta = _targets.GetDstPos().posZ - srcPos.posZ;

			List<WorldObject> targets   = new();
			var               spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, srcPos, _caster, _spellInfo, targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, SpellTargetObjectTypes.None);
			var               searcher  = new WorldObjectListSearcher(_caster, targets, spellTraj);
			SearchTargets(searcher, GridMapTypeMask.All, _caster, srcPos, dist2d);

			if (targets.Empty())
				return;

			targets.Sort(new ObjectDistanceOrderPred(_caster));

			float b = Tangent(_targets.GetPitch());
			float a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);

			if (a > -0.0001f)
				a = 0f;

			// We should check if triggered spell has greater range (which is true in many cases, and initial spell has too short max range)
			// limit max range to 300 yards, sometimes triggered spells can have 50000yds
			float     bestDist         = _spellInfo.GetMaxRange(false);
			SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, GetCastDifficulty());

			if (triggerSpellInfo != null)
				bestDist = Math.Min(Math.Max(bestDist, triggerSpellInfo.GetMaxRange(false)), Math.Min(dist2d, 300.0f));

			// GameObjects don't cast traj
			Unit unitCaster = _caster.ToUnit();

			foreach (var obj in targets)
			{
				if (_spellInfo.CheckTarget(unitCaster, obj, true) != SpellCastResult.SpellCastOk)
					continue;

				Unit unitTarget = obj.ToUnit();

				if (unitTarget)
				{
					if (unitCaster == obj ||
					    unitCaster.IsOnVehicle(unitTarget) ||
					    unitTarget.GetVehicle())
						continue;

					Creature creatureTarget = unitTarget.ToCreature();

					if (creatureTarget)
						if (!creatureTarget.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CollideWithMissiles))
							continue;
				}

				float size      = Math.Max(obj.GetCombatReach(), 1.0f);
				float objDist2d = srcPos.GetExactDist2d(obj);
				float dz        = obj.GetPositionZ() - srcPos.posZ;

				float horizontalDistToTraj = (float)Math.Abs(objDist2d * Math.Sin(srcPos.GetRelativeAngle(obj)));
				float sizeFactor           = (float)Math.Cos((horizontalDistToTraj / size) * (Math.PI / 2.0f));
				float distToHitPoint       = (float)Math.Max(objDist2d * Math.Cos(srcPos.GetRelativeAngle(obj)) - size * sizeFactor, 0.0f);
				float height               = distToHitPoint * (a * distToHitPoint + b);

				if (Math.Abs(dz - height) > size + b / 2.0f + SpellConst.TrajectoryMissileSize)
					continue;

				if (distToHitPoint < bestDist)
				{
					bestDist = distToHitPoint;

					break;
				}
			}

			if (dist2d > bestDist)
			{
				float x = (float)(_targets.GetSrcPos().posX + Math.Cos(unitCaster.GetOrientation()) * bestDist);
				float y = (float)(_targets.GetSrcPos().posY + Math.Sin(unitCaster.GetOrientation()) * bestDist);
				float z = _targets.GetSrcPos().posZ + bestDist * (a * bestDist + b);

				SpellDestination dest = new(x, y, z, unitCaster.GetOrientation());

				if (_spellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
					dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

				CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
				_targets.ModDst(dest);
			}
		}

		private void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
		{
			List<WorldObject>      targets       = new();
			SpellTargetObjectTypes objectType    = targetType.GetObjectType();
			SpellTargetCheckTypes  selectionType = targetType.GetCheckType();

			Position dst;

			switch (targetType.GetReferenceType())
			{
				case SpellTargetReferenceTypes.Src:
					dst = _targets.GetSrcPos();

					break;
				case SpellTargetReferenceTypes.Dest:
					dst = _targets.GetDstPos();

					break;
				case SpellTargetReferenceTypes.Caster:
					dst = _caster;

					break;
				case SpellTargetReferenceTypes.Target:
					dst = _targets.GetUnitTarget();

					break;
				default:
					Cypher.Assert(false, "Spell.SelectImplicitLineTargets: received not implemented target reference type");

					return;
			}

			var   condList = spellEffectInfo.ImplicitTargetConditions;
			float radius   = spellEffectInfo.CalcRadius(_caster) * _spellValue.RadiusMod;

			GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

			if (containerTypeMask != 0)
			{
				WorldObjectSpellLineTargetCheck check    = new(_caster, dst, _spellInfo.Width != 0 ? _spellInfo.Width : _caster.GetCombatReach(), radius, _caster, _spellInfo, selectionType, condList, objectType);
				WorldObjectListSearcher         searcher = new(_caster, targets, check, containerTypeMask);
				SearchTargets(searcher, containerTypeMask, _caster, _caster, radius);

				CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

				if (!targets.Empty())
				{
					// Other special target selection goes here
					uint maxTargets = _spellValue.MaxAffectedTargets;

					if (maxTargets != 0)
						if (maxTargets < targets.Count)
						{
							targets.Sort(new ObjectDistanceOrderPred(_caster));
							targets.Resize(maxTargets);
						}

					foreach (var obj in targets)
						if (obj.IsUnit())
							AddUnitTarget(obj.ToUnit(), effMask, false);
						else if (obj.IsGameObject())
							AddGOTarget(obj.ToGameObject(), effMask);
						else if (obj.IsCorpse())
							AddCorpseTarget(obj.ToCorpse(), effMask);
				}
			}
		}

		private void SelectEffectTypeImplicitTargets(SpellEffectInfo spellEffectInfo)
		{
			// special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER, queue them on map for later execution
			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.SummonRafFriend:
				case SpellEffectName.SummonPlayer:
					if (_caster.IsTypeId(TypeId.Player) &&
					    !_caster.ToPlayer().GetTarget().IsEmpty())
					{
						WorldObject rafTarget = Global.ObjAccessor.FindPlayer(_caster.ToPlayer().GetTarget());

						CallScriptObjectTargetSelectHandlers(ref rafTarget, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

						// scripts may modify the target - recheck
						if (rafTarget != null &&
						    rafTarget.IsPlayer())
						{
							// target is not stored in target map for those spells
							// since we're completely skipping AddUnitTarget logic, we need to check immunity manually
							// eg. aura 21546 makes target immune to summons
							Player player = rafTarget.ToPlayer();

							if (player.IsImmunedToSpellEffect(_spellInfo, spellEffectInfo, null))
								return;

							var spell      = this;
							var targetGuid = rafTarget.GetGUID();

							rafTarget.GetMap()
							         .AddFarSpellCallback(map =>
							                              {
								                              Player player = Global.ObjAccessor.GetPlayer(map, targetGuid);

								                              if (player == null)
									                              return;

								                              // check immunity again in case it changed during update
								                              if (player.IsImmunedToSpellEffect(spell.GetSpellInfo(), spellEffectInfo, null))
									                              return;

								                              spell.HandleEffects(player, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
							                              });
						}
					}

					return;
				default:
					break;
			}

			// select spell implicit targets based on effect type
			if (spellEffectInfo.GetImplicitTargetType() == 0)
				return;

			SpellCastTargetFlags targetMask = spellEffectInfo.GetMissingTargetMask();

			if (targetMask == 0)
				return;

			WorldObject target = null;

			switch (spellEffectInfo.GetImplicitTargetType())
			{
				// add explicit object target or self to the target map
				case SpellEffectImplicitTargetTypes.Explicit:
					// player which not released his spirit is Unit, but target flag for it is TARGET_FLAG_CORPSE_MASK
					if (Convert.ToBoolean(targetMask & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
					{
						Unit unitTarget = _targets.GetUnitTarget();

						if (unitTarget != null)
						{
							target = unitTarget;
						}
						else if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.CorpseMask))
						{
							Corpse corpseTarget = _targets.GetCorpseTarget();

							if (corpseTarget != null)
								target = corpseTarget;
						}
						else //if (targetMask & TARGET_FLAG_UNIT_MASK)
						{
							target = _caster;
						}
					}

					if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.ItemMask))
					{
						Item itemTarget = _targets.GetItemTarget();

						if (itemTarget != null)
							AddItemTarget(itemTarget, (uint)(1 << (int)spellEffectInfo.EffectIndex));

						return;
					}

					if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.GameobjectMask))
						target = _targets.GetGOTarget();

					break;
				// add self to the target map
				case SpellEffectImplicitTargetTypes.Caster:
					if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.UnitMask))
						target = _caster;

					break;
				default:
					break;
			}

			CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

			if (target != null)
			{
				if (target.IsUnit())
					AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, false);
				else if (target.IsGameObject())
					AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
				else if (target.IsCorpse())
					AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
			}
		}

		public GridMapTypeMask GetSearcherTypeMask(SpellTargetObjectTypes objType, List<Condition> condList)
		{
			// this function selects which containers need to be searched for spell target
			GridMapTypeMask retMask = GridMapTypeMask.All;

			// filter searchers based on searched object type
			switch (objType)
			{
				case SpellTargetObjectTypes.Unit:
				case SpellTargetObjectTypes.UnitAndDest:
					retMask &= GridMapTypeMask.Player | GridMapTypeMask.Creature;

					break;
				case SpellTargetObjectTypes.Corpse:
				case SpellTargetObjectTypes.CorpseEnemy:
				case SpellTargetObjectTypes.CorpseAlly:
					retMask &= GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.Creature;

					break;
				case SpellTargetObjectTypes.Gobj:
				case SpellTargetObjectTypes.GobjItem:
					retMask &= GridMapTypeMask.GameObject;

					break;
				default:
					break;
			}

			if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
				retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;

			if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnGhosts))
				retMask &= GridMapTypeMask.Player;

			if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
				retMask &= ~GridMapTypeMask.Player;

			if (condList != null)
				retMask &= Global.ConditionMgr.GetSearcherTypeMaskForConditionList(condList);

			return retMask;
		}

		private void SearchTargets(Notifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
		{
			if (containerMask == 0)
				return;

			bool searchInGrid  = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.GameObject);
			bool searchInWorld = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.Player | GridMapTypeMask.Corpse);

			if (searchInGrid || searchInWorld)
			{
				float x = pos.GetPositionX();
				float y = pos.GetPositionY();

				CellCoord p    = GridDefines.ComputeCellCoord(x, y);
				Cell      cell = new(p);
				cell.SetNoCreate();

				Map map = referer.GetMap();

				if (searchInWorld)
					Cell.VisitWorldObjects(x, y, map, notifier, radius);

				if (searchInGrid)
					Cell.VisitGridObjects(x, y, map, notifier, radius);
			}
		}

		private WorldObject SearchNearbyTarget(float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
		{
			GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

			if (containerTypeMask == 0)
				return null;

			var check    = new WorldObjectSpellNearbyTargetCheck(range, _caster, _spellInfo, selectionType, condList, objectType);
			var searcher = new WorldObjectLastSearcher(_caster, check, containerTypeMask);
			SearchTargets(searcher, containerTypeMask, _caster, _caster.GetPosition(), range);

			return searcher.GetTarget();
		}

		private void SearchAreaTargets(List<WorldObject> targets, float range, Position position, WorldObject referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
		{
			var containerTypeMask = GetSearcherTypeMask(objectType, condList);

			if (containerTypeMask == 0)
				return;

			float extraSearchRadius = range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
			var   check             = new WorldObjectSpellAreaTargetCheck(range, position, _caster, referer, _spellInfo, selectionType, condList, objectType);
			var   searcher          = new WorldObjectListSearcher(_caster, targets, check, containerTypeMask);
			SearchTargets(searcher, containerTypeMask, _caster, position, range + extraSearchRadius);
		}

		private void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, SpellEffectInfo spellEffectInfo, bool isChainHeal)
		{
			// max dist for jump target selection
			float jumpRadius = 0.0f;

			switch (_spellInfo.DmgClass)
			{
				case SpellDmgClass.Ranged:
					// 7.5y for multi shot
					jumpRadius = 7.5f;

					break;
				case SpellDmgClass.Melee:
					// 5y for swipe, cleave and similar
					jumpRadius = 5.0f;

					break;
				case SpellDmgClass.None:
				case SpellDmgClass.Magic:
					// 12.5y for chain heal spell since 3.2 patch
					if (isChainHeal)
						jumpRadius = 12.5f;
					// 10y as default for magic chain spells
					else
						jumpRadius = 10.0f;

					break;
			}

			Player modOwner = _caster.GetSpellModOwner();

			if (modOwner)
				modOwner.ApplySpellMod(_spellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

			float searchRadius;

			if (_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster))
				searchRadius = GetMinMaxRange(false).maxRange;
			else if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
				searchRadius = jumpRadius;
			else
				searchRadius = jumpRadius * chainTargets;

			WorldObject       chainSource = _spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster : target;
			List<WorldObject> tempTargets = new();
			SearchAreaTargets(tempTargets, searchRadius, chainSource, _caster, objectType, selectType, spellEffectInfo.ImplicitTargetConditions);
			tempTargets.Remove(target);

			// remove targets which are always invalid for chain spells
			// for some spells allow only chain targets in front of caster (swipe for example)
			if (_spellInfo.HasAttribute(SpellAttr5.MeleeChainTargeting))
				tempTargets.RemoveAll(obj => !_caster.HasInArc(MathF.PI, obj));

			while (chainTargets != 0)
			{
				// try to get unit for next chain jump
				WorldObject found = null;

				// get unit with highest hp deficit in dist
				if (isChainHeal)
				{
					uint maxHPDeficit = 0;

					foreach (var obj in tempTargets)
					{
						Unit unitTarget = obj.ToUnit();

						if (unitTarget != null)
						{
							uint deficit = (uint)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());

							if ((deficit > maxHPDeficit || found == null) &&
							    chainSource.IsWithinDist(unitTarget, jumpRadius) &&
							    chainSource.IsWithinLOSInMap(unitTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
							{
								found        = obj;
								maxHPDeficit = deficit;
							}
						}
					}
				}
				// get closest object
				else
				{
					foreach (var obj in tempTargets)
						if (found == null)
						{
							if (chainSource.IsWithinDist(obj, jumpRadius) &&
							    chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
								found = obj;
						}
						else if (chainSource.GetDistanceOrder(obj, found) &&
						         chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						{
							found = obj;
						}
				}

				// not found any valid target - chain ends
				if (found == null)
					break;

				if (!_spellInfo.HasAttribute(SpellAttr2.ChainFromCaster) &&
				    !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
					chainSource = found;

				targets.Add(found);
				tempTargets.Remove(found);
				--chainTargets;
			}
		}

		private GameObject SearchSpellFocus()
		{
			var check    = new GameObjectFocusCheck(_caster, _spellInfo.RequiresSpellFocus);
			var searcher = new GameObjectSearcher(_caster, check);
			SearchTargets(searcher, GridMapTypeMask.GameObject, _caster, _caster, _caster.GetVisibilityRange());

			return searcher.GetTarget();
		}

		private void PrepareDataForTriggerSystem()
		{
			//==========================================================================================
			// Now fill data for trigger system, need know:
			// Create base triggers flags for Attacker and Victim (_procAttacker, _procVictim and _hitMask)
			//==========================================================================================

			_procVictim = _procAttacker = new ProcFlagsInit();

			// Get data for type of attack and fill base info for trigger
			switch (_spellInfo.DmgClass)
			{
				case SpellDmgClass.Melee:
					_procAttacker = new ProcFlagsInit(ProcFlags.DealMeleeAbility);

					if (_attackType == WeaponAttackType.OffAttack)
						_procAttacker.Or(ProcFlags.OffHandWeaponSwing);
					else
						_procAttacker.Or(ProcFlags.MainHandWeaponSwing);

					_procVictim = new ProcFlagsInit(ProcFlags.TakeMeleeAbility);

					break;
				case SpellDmgClass.Ranged:
					// Auto attack
					if (_spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
					{
						_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
						_procVictim   = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
					}
					else // Ranged spell attack
					{
						_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAbility);
						_procVictim   = new ProcFlagsInit(ProcFlags.TakeRangedAbility);
					}

					break;
				default:
					if (_spellInfo.EquippedItemClass == ItemClass.Weapon &&
					    Convert.ToBoolean(_spellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand)) &&
					    _spellInfo.HasAttribute(SpellAttr2.AutoRepeat)) // Wands auto attack
					{
						_procAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
						_procVictim   = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
					}

					break;
				// For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
				// Because spell positivity is dependant on target
			}
		}

		public void CleanupTargetList()
		{
			_UniqueTargetInfo.Clear();
			_UniqueGOTargetInfo.Clear();
			_UniqueItemInfo.Clear();
			_delayMoment = 0;
		}

		private void AddUnitTarget(Unit target, uint effectMask, bool checkIfValid = true, bool Implicit = true, Position losPosition = null)
		{
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				if (!spellEffectInfo.IsEffect() ||
				    !CheckEffectTarget(target, spellEffectInfo, losPosition))
					effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

			// no effects left
			if (effectMask == 0)
				return;

			if (checkIfValid)
				if (_spellInfo.CheckTarget(_caster, target, Implicit) != SpellCastResult.SpellCastOk) // skip stealth checks for AOE
					return;

			// Check for effect immune skip if immuned
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				if (target.IsImmunedToSpellEffect(_spellInfo, spellEffectInfo, _caster))
					effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

			ObjectGuid targetGUID = target.GetGUID();

			// Lookup target in already in list
			var index = _UniqueTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

			if (index != -1) // Found in list
			{
				// Immune effects removed from mask
				_UniqueTargetInfo[index].EffectMask |= effectMask;

				return;
			}

			// This is new target calculate data for him

			// Get spell hit result on target
			TargetInfo targetInfo = new();
			targetInfo.TargetGUID = targetGUID; // Store target GUID
			targetInfo.EffectMask = effectMask; // Store all effects not immune
			targetInfo.IsAlive    = target.IsAlive();

			// Calculate hit result
			WorldObject caster = _originalCaster ? _originalCaster : _caster;
			targetInfo.MissCondition = caster.SpellHitResult(target, _spellInfo, _canReflect && !(IsPositive() && _caster.IsFriendlyTo(target)));

			// Spell have speed - need calculate incoming time
			// Incoming time is zero for self casts. At least I think so.
			if (_caster != target)
			{
				float       hitDelay      = _spellInfo.LaunchDelay;
				WorldObject missileSource = _caster;

				if (_spellInfo.HasAttribute(SpellAttr4.BouncyChainMissiles))
				{
					var previousTargetInfo = _UniqueTargetInfo.FindLast(target => (target.EffectMask & effectMask) != 0);

					if (previousTargetInfo != null)
					{
						hitDelay = 0.0f; // this is not the first target in chain, LaunchDelay was already included

						WorldObject previousTarget = Global.ObjAccessor.GetWorldObject(_caster, previousTargetInfo.TargetGUID);

						if (previousTarget != null)
							missileSource = previousTarget;

						targetInfo.TimeDelay += previousTargetInfo.TimeDelay;
					}
				}

				if (_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
				{
					hitDelay += _spellInfo.Speed;
				}
				else if (_spellInfo.Speed > 0.0f)
				{
					// calculate spell incoming interval
					/// @todo this is a hack
					float dist = Math.Max(missileSource.GetDistance(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()), 5.0f);
					hitDelay += dist / _spellInfo.Speed;
				}

				targetInfo.TimeDelay += (ulong)Math.Floor(hitDelay * 1000.0f);
			}
			else
			{
				targetInfo.TimeDelay = 0L;
			}

			// If target reflect spell back to caster
			if (targetInfo.MissCondition == SpellMissInfo.Reflect)
			{
				// Calculate reflected spell result on caster (shouldn't be able to reflect gameobject spells)
				Unit unitCaster = _caster.ToUnit();
				targetInfo.ReflectResult = unitCaster.SpellHitResult(unitCaster, _spellInfo, false); // can't reflect twice

				// Proc spell reflect aura when missile hits the original target
				target._Events.AddEvent(new ProcReflectDelayed(target, _originalCasterGUID), target._Events.CalculateTime(TimeSpan.FromMilliseconds(targetInfo.TimeDelay)));

				// Increase time interval for reflected spells by 1.5
				targetInfo.TimeDelay += targetInfo.TimeDelay >> 1;
			}
			else
			{
				targetInfo.ReflectResult = SpellMissInfo.None;
			}

			// Calculate minimum incoming time
			if (targetInfo.TimeDelay != 0 &&
			    (_delayMoment == 0 || _delayMoment > targetInfo.TimeDelay))
				_delayMoment = targetInfo.TimeDelay;

			// Add target to list
			_UniqueTargetInfo.Add(targetInfo);
		}

		private void AddGOTarget(GameObject go, uint effectMask)
		{
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				if (!spellEffectInfo.IsEffect() ||
				    !CheckEffectTarget(go, spellEffectInfo))
					effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

			// no effects left
			if (effectMask == 0)
				return;

			ObjectGuid targetGUID = go.GetGUID();

			// Lookup target in already in list
			var index = _UniqueGOTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

			if (index != -1) // Found in list
			{
				// Add only effect mask
				_UniqueGOTargetInfo[index].EffectMask |= effectMask;

				return;
			}

			// This is new target calculate data for him
			GOTargetInfo target = new();
			target.TargetGUID = targetGUID;
			target.EffectMask = effectMask;

			// Spell have speed - need calculate incoming time
			if (_caster != go)
			{
				float hitDelay = _spellInfo.LaunchDelay;

				if (_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
				{
					hitDelay += _spellInfo.Speed;
				}
				else if (_spellInfo.Speed > 0.0f)
				{
					// calculate spell incoming interval
					float dist = Math.Max(_caster.GetDistance(go.GetPositionX(), go.GetPositionY(), go.GetPositionZ()), 5.0f);
					hitDelay += dist / _spellInfo.Speed;
				}

				target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
			}
			else
			{
				target.TimeDelay = 0UL;
			}

			// Calculate minimum incoming time
			if (target.TimeDelay != 0 &&
			    (_delayMoment == 0 || _delayMoment > target.TimeDelay))
				_delayMoment = target.TimeDelay;

			// Add target to list
			_UniqueGOTargetInfo.Add(target);
		}

		private void AddItemTarget(Item item, uint effectMask)
		{
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				if (!spellEffectInfo.IsEffect() ||
				    !CheckEffectTarget(item, spellEffectInfo))
					effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

			// no effects left
			if (effectMask == 0)
				return;

			// Lookup target in already in list
			var index = _UniqueItemInfo.FindIndex(target => target.TargetItem == item);

			if (index != -1) // Found in list
			{
				// Add only effect mask
				_UniqueItemInfo[index].EffectMask |= effectMask;

				return;
			}

			// This is new target add data

			ItemTargetInfo target = new();
			target.TargetItem = item;
			target.EffectMask = effectMask;

			_UniqueItemInfo.Add(target);
		}

		private void AddCorpseTarget(Corpse corpse, uint effectMask)
		{
			foreach (SpellEffectInfo spellEffectInfo in _spellInfo.GetEffects())
				if (!spellEffectInfo.IsEffect())
					effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

			// no effects left
			if (effectMask == 0)
				return;

			ObjectGuid targetGUID = corpse.GetGUID();

			// Lookup target in already in list
			var corpseTargetInfo = _UniqueCorpseTargetInfo.Find(target => { return target.TargetGUID == targetGUID; });

			if (corpseTargetInfo != null) // Found in list
			{
				// Add only effect mask
				corpseTargetInfo.EffectMask |= effectMask;

				return;
			}

			// This is new target calculate data for him
			CorpseTargetInfo target = new();
			target.TargetGUID = targetGUID;
			target.EffectMask = effectMask;

			// Spell have speed - need calculate incoming time
			if (_caster != corpse)
			{
				float hitDelay = _spellInfo.LaunchDelay;

				if (_spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
				{
					hitDelay += _spellInfo.Speed;
				}
				else if (_spellInfo.Speed > 0.0f)
				{
					// calculate spell incoming interval
					float dist = Math.Max(_caster.GetDistance(corpse.GetPositionX(), corpse.GetPositionY(), corpse.GetPositionZ()), 5.0f);
					hitDelay += dist / _spellInfo.Speed;
				}

				target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
			}
			else
			{
				target.TimeDelay = 0;
			}

			// Calculate minimum incoming time
			if (target.TimeDelay != 0 &&
			    (_delayMoment == 0 || _delayMoment > target.TimeDelay))
				_delayMoment = target.TimeDelay;

			// Add target to list
			_UniqueCorpseTargetInfo.Add(target);
		}

		private void AddDestTarget(SpellDestination dest, uint effIndex)
		{
			_destTargets[effIndex] = dest;
		}

		public long GetUnitTargetCountForEffect(uint effect)
		{
			return _UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.None && (targetInfo.EffectMask & (1 << (int)effect)) != 0);
		}

		public long GetGameObjectTargetCountForEffect(uint effect)
		{
			return _UniqueGOTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
		}

		public long GetItemTargetCountForEffect(uint effect)
		{
			return _UniqueItemInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
		}

		public long GetCorpseTargetCountForEffect(uint effect)
		{
			return _UniqueCorpseTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1u << (int)effect)) != 0);
		}

		public SpellMissInfo PreprocessSpellHit(Unit unit, TargetInfo hitInfo)
		{
			if (unit == null)
				return SpellMissInfo.Evade;

			// Target may have begun evading between launch and hit phases - re-check now
			Creature creatureTarget = unit.ToCreature();

			if (creatureTarget != null &&
			    creatureTarget.IsEvadingAttacks())
				return SpellMissInfo.Evade;

			// For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
			if (_spellInfo.HasHitDelay() &&
			    unit.IsImmunedToSpell(_spellInfo, _caster))
				return SpellMissInfo.Immune;

			CallScriptBeforeHitHandlers(hitInfo.MissCondition);

			Player player = unit.ToPlayer();

			if (player != null)
			{
				player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, _spellInfo.Id);
				player.UpdateCriteria(CriteriaType.BeSpellTarget, _spellInfo.Id, 0, 0, _caster);
				player.UpdateCriteria(CriteriaType.GainAura, _spellInfo.Id);
			}

			Player casterPlayer = _caster.ToPlayer();

			if (casterPlayer)
			{
				casterPlayer.StartCriteriaTimer(CriteriaStartEvent.CastSpell, _spellInfo.Id);
				casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, _spellInfo.Id, 0, 0, unit);
			}

			if (_caster != unit)
			{
				// Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
				if (_spellInfo.HasHitDelay() &&
				    unit.HasUnitFlag(UnitFlags.NonAttackable) &&
				    unit.GetCharmerOrOwnerGUID() != _caster.GetGUID())
					return SpellMissInfo.Evade;

				if (_caster.IsValidAttackTarget(unit, _spellInfo))
				{
					unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.HostileActionReceived);
				}
				else if (_caster.IsFriendlyTo(unit))
				{
					// for delayed spells ignore negative spells (after duel end) for friendly targets
					if (_spellInfo.HasHitDelay() &&
					    unit.IsPlayer() &&
					    !IsPositive() &&
					    !_caster.IsValidAssistTarget(unit, _spellInfo))
						return SpellMissInfo.Evade;

					// assisting case, healing and resurrection
					if (unit.HasUnitState(UnitState.AttackPlayer))
					{
						Player playerOwner = _caster.GetCharmerOrOwnerPlayerOrPlayerItself();

						if (playerOwner != null)
						{
							playerOwner.SetContestedPvP();
							playerOwner.UpdatePvP(true);
						}
					}

					if (_originalCaster &&
					    unit.IsInCombat() &&
					    _spellInfo.HasInitialAggro())
					{
						if (_originalCaster.HasUnitFlag(UnitFlags.PlayerControlled))          // only do explicit combat forwarding for PvP enabled units
							_originalCaster.GetCombatManager().InheritCombatStatesFrom(unit); // for creature v creature combat, the threat forward does it for us

						unit.GetThreatManager().ForwardThreatForAssistingMe(_originalCaster, 0.0f, null, true);
					}
				}
			}

			// original caster for auras
			WorldObject origCaster = _caster;

			if (_originalCaster)
				origCaster = _originalCaster;

			// check immunity due to diminishing returns
			if (Aura.BuildEffectMaskForOwner(_spellInfo, SpellConst.MaxEffectMask, unit) != 0)
			{
				foreach (var spellEffectInfo in _spellInfo.GetEffects())
					hitInfo.AuraBasePoints[spellEffectInfo.EffectIndex] = (_spellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 ? _spellValue.EffectBasePoints[spellEffectInfo.EffectIndex] : spellEffectInfo.CalcBaseValue(_originalCaster, unit, _castItemEntry, _castItemLevel);

				// Get Data Needed for Diminishing Returns, some effects may have multiple auras, so this must be done on spell hit, not aura add
				hitInfo.DRGroup = _spellInfo.GetDiminishingReturnsGroupForSpell();

				DiminishingLevels diminishLevel = DiminishingLevels.Level1;

				if (hitInfo.DRGroup != 0)
				{
					diminishLevel = unit.GetDiminishing(hitInfo.DRGroup);
					DiminishingReturnsType type = _spellInfo.GetDiminishingReturnsGroupType();

					// Increase Diminishing on unit, current informations for actually casts will use values above
					if (type == DiminishingReturnsType.All ||
					    (type == DiminishingReturnsType.Player && unit.IsAffectedByDiminishingReturns()))
						unit.IncrDiminishing(_spellInfo);
				}

				// Now Reduce spell duration using data received at spell hit
				// check whatever effects we're going to apply, diminishing returns only apply to negative aura effects
				hitInfo.Positive = true;

				if (origCaster == unit ||
				    !origCaster.IsFriendlyTo(unit))
					foreach (var spellEffectInfo in _spellInfo.GetEffects())
						// mod duration only for effects applying aura!
						if ((hitInfo.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
						    spellEffectInfo.IsUnitOwnedAuraEffect() &&
						    !_spellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
						{
							hitInfo.Positive = false;

							break;
						}

				hitInfo.AuraDuration = Aura.CalcMaxDuration(_spellInfo, origCaster);

				// unit is immune to aura if it was diminished to 0 duration
				if (!hitInfo.Positive &&
				    !unit.ApplyDiminishingToDuration(_spellInfo, ref hitInfo.AuraDuration, origCaster, diminishLevel))
					if (_spellInfo.GetEffects().All(effInfo => !effInfo.IsEffect() || effInfo.IsEffect(SpellEffectName.ApplyAura)))
						return SpellMissInfo.Immune;
			}

			return SpellMissInfo.None;
		}

		public void DoSpellEffectHit(Unit unit, SpellEffectInfo spellEffectInfo, TargetInfo hitInfo)
		{
			uint aura_effmask = Aura.BuildEffectMaskForOwner(_spellInfo, 1u << (int)spellEffectInfo.EffectIndex, unit);

			if (aura_effmask != 0)
			{
				WorldObject caster = _caster;

				if (_originalCaster)
					caster = _originalCaster;

				if (caster != null)
				{
					// delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
					if (hitInfo.HitAura == null)
					{
						bool resetPeriodicTimer = (_spellInfo.StackAmount < 2) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
						uint allAuraEffectMask  = Aura.BuildEffectMaskForOwner(_spellInfo, SpellConst.MaxEffectMask, unit);

						AuraCreateInfo createInfo = new(_castId, _spellInfo, GetCastDifficulty(), allAuraEffectMask, unit);
						createInfo.SetCasterGUID(caster.GetGUID());
						createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
						createInfo.SetCastItem(_castItemGUID, _castItemEntry, _castItemLevel);
						createInfo.SetPeriodicReset(resetPeriodicTimer);
						createInfo.SetOwnerEffectMask(aura_effmask);

						Aura aura = Aura.TryRefreshStackOrCreate(createInfo, false);

						if (aura != null)
						{
							hitInfo.HitAura = aura.ToUnitAura();

							// Set aura stack amount to desired value
							if (_spellValue.AuraStackAmount > 1)
							{
								if (!createInfo.IsRefresh)
									hitInfo.HitAura.SetStackAmount((byte)_spellValue.AuraStackAmount);
								else
									hitInfo.HitAura.ModStackAmount(_spellValue.AuraStackAmount);
							}

							hitInfo.HitAura.SetDiminishGroup(hitInfo.DRGroup);

							if (!_spellValue.Duration.HasValue)
							{
								hitInfo.AuraDuration = caster.ModSpellDuration(_spellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, hitInfo.HitAura.GetEffectMask());

								if (hitInfo.AuraDuration > 0)
								{
									hitInfo.AuraDuration *= (int)_spellValue.DurationMul;

									// Haste modifies duration of channeled spells
									if (_spellInfo.IsChanneled())
									{
										caster.ModSpellDurationTime(_spellInfo, ref hitInfo.AuraDuration, this);
									}
									else if (_spellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
									{
										int origDuration = hitInfo.AuraDuration;
										hitInfo.AuraDuration = 0;

										foreach (AuraEffect auraEff in hitInfo.HitAura.GetAuraEffects())
											if (auraEff != null)
											{
												int period = auraEff.GetPeriod();

												if (period != 0) // period is hastened by UNIT_MOD_CAST_SPEED
													hitInfo.AuraDuration = Math.Max(Math.Max(origDuration / period, 1) * period, hitInfo.AuraDuration);
											}

										// if there is no periodic effect
										if (hitInfo.AuraDuration == 0)
											hitInfo.AuraDuration = (int)(origDuration * _originalCaster._unitData.ModCastingSpeed);
									}
								}
							}
							else
							{
								hitInfo.AuraDuration = _spellValue.Duration.Value;
							}

							if (hitInfo.AuraDuration != hitInfo.HitAura.GetMaxDuration())
							{
								hitInfo.HitAura.SetMaxDuration(hitInfo.AuraDuration);
								hitInfo.HitAura.SetDuration(hitInfo.AuraDuration);
							}

							if (createInfo.IsRefresh)
								hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
						}
					}
					else
					{
						hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
					}
				}
			}

			spellAura = hitInfo.HitAura;
			HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
			spellAura = null;
		}

		public void DoTriggersOnSpellHit(Unit unit)
		{
			// handle SPELL_AURA_ADD_TARGET_TRIGGER auras
			// this is executed after spell proc spells on target hit
			// spells are triggered for each hit spell target
			// info confirmed with retail sniffs of permafrost and shadow weaving
			if (!_hitTriggerSpells.Empty())
			{
				int _duration = 0;

				foreach (var hit in _hitTriggerSpells)
					if (CanExecuteTriggersOnHit(unit, hit.triggeredByAura) &&
					    RandomHelper.randChance(hit.chance))
					{
						_caster.CastSpell(unit,
						                  hit.triggeredSpell.Id,
						                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
							                  .SetTriggeringSpell(this)
							                  .SetCastDifficulty(hit.triggeredSpell.Difficulty));

						Log.outDebug(LogFilter.Spells, "Spell {0} triggered spell {1} by SPELL_AURA_ADD_TARGET_TRIGGER aura", _spellInfo.Id, hit.triggeredSpell.Id);

						// SPELL_AURA_ADD_TARGET_TRIGGER auras shouldn't trigger auras without duration
						// set duration of current aura to the triggered spell
						if (hit.triggeredSpell.GetDuration() == -1)
						{
							Aura triggeredAur = unit.GetAura(hit.triggeredSpell.Id, _caster.GetGUID());

							if (triggeredAur != null)
							{
								// get duration from aura-only once
								if (_duration == 0)
								{
									Aura aur = unit.GetAura(_spellInfo.Id, _caster.GetGUID());
									_duration = aur != null ? aur.GetDuration() : -1;
								}

								triggeredAur.SetDuration(_duration);
							}
						}
					}
			}

			// trigger linked auras remove/apply
			// @todo remove/cleanup this, as this table is not documented and people are doing stupid things with it
			var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Hit, _spellInfo.Id);

			if (spellTriggered != null)
				foreach (var id in spellTriggered)
					if (id < 0)
						unit.RemoveAurasDueToSpell((uint)-id);
					else
						unit.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(_caster.GetGUID()).SetTriggeringSpell(this));
		}

		private bool UpdateChanneledTargetList()
		{
			// Not need check return true
			if (_channelTargetEffectMask == 0)
				return true;

			uint channelTargetEffectMask = _channelTargetEffectMask;
			uint channelAuraMask         = 0;

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
					channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

			channelAuraMask &= channelTargetEffectMask;

			float range = 0;

			if (channelAuraMask != 0)
			{
				range = _spellInfo.GetMaxRange(IsPositive());
				Player modOwner = _caster.GetSpellModOwner();

				if (modOwner != null)
					modOwner.ApplySpellMod(_spellInfo, SpellModOp.Range, ref range, this);

				// add little tolerance level
				range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
			}

			foreach (var targetInfo in _UniqueTargetInfo)
				if (targetInfo.MissCondition == SpellMissInfo.None &&
				    Convert.ToBoolean(channelTargetEffectMask & targetInfo.EffectMask))
				{
					Unit unit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);

					if (unit == null)
					{
						Unit unitCaster = _caster.ToUnit();

						if (unitCaster != null)
							unitCaster.RemoveChannelObject(targetInfo.TargetGUID);

						continue;
					}

					if (IsValidDeadOrAliveTarget(unit))
					{
						if (Convert.ToBoolean(channelAuraMask & targetInfo.EffectMask))
						{
							AuraApplication aurApp = unit.GetAuraApplication(_spellInfo.Id, _originalCasterGUID);

							if (aurApp != null)
							{
								if (_caster != unit &&
								    !_caster.IsWithinDistInMap(unit, range))
								{
									targetInfo.EffectMask &= ~aurApp.GetEffectMask();
									unit.RemoveAura(aurApp);
									Unit unitCaster = _caster.ToUnit();

									if (unitCaster != null)
										unitCaster.RemoveChannelObject(targetInfo.TargetGUID);

									continue;
								}
							}
							else // aura is dispelled
							{
								Unit unitCaster = _caster.ToUnit();

								if (unitCaster != null)
									unitCaster.RemoveChannelObject(targetInfo.TargetGUID);

								continue;
							}
						}

						channelTargetEffectMask &= ~targetInfo.EffectMask; // remove from need alive mask effect that have alive target
					}
				}

			// is all effects from _needAliveTargetMask have alive targets
			return channelTargetEffectMask == 0;
		}

		public SpellCastResult Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
		{
			if (_CastItem != null)
			{
				_castItemGUID  = _CastItem.GetGUID();
				_castItemEntry = _CastItem.GetEntry();

				Player owner = _CastItem.GetOwner();

				if (owner)
				{
					_castItemLevel = (int)_CastItem.GetItemLevel(owner);
				}
				else if (_CastItem.GetOwnerGUID() == _caster.GetGUID())
				{
					_castItemLevel = (int)_CastItem.GetItemLevel(_caster.ToPlayer());
				}
				else
				{
					SendCastResult(SpellCastResult.EquippedItem);
					Finish(false);

					return SpellCastResult.EquippedItem;
				}
			}

			InitExplicitTargets(targets);

			_spellState = SpellState.Preparing;

			if (triggeredByAura != null)
			{
				_triggeredByAuraSpell = triggeredByAura.GetSpellInfo();
				_castItemLevel        = triggeredByAura.GetBase().GetCastItemLevel();
			}

			// create and add update event for this spell
			_spellEvent = new SpellEvent(this);
			_caster._Events.AddEvent(_spellEvent, _caster._Events.CalculateTime(TimeSpan.FromMilliseconds(1)));

			// check disables
			if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, _spellInfo.Id, _caster))
			{
				SendCastResult(SpellCastResult.SpellUnavailable);
				Finish(false);

				return SpellCastResult.SpellUnavailable;
			}

			// Prevent casting at cast another spell (ServerSide check)
			if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) &&
			    _caster.ToUnit() != null &&
			    _caster.ToUnit().IsNonMeleeSpellCast(false, true, true, _spellInfo.Id == 75) &&
			    !_castId.IsEmpty())
			{
				SendCastResult(SpellCastResult.SpellInProgress);
				Finish(false);

				return SpellCastResult.SpellInProgress;
			}

			LoadScripts();

			// Fill cost data (not use power for item casts
			if (_CastItem == null)
				_powerCost = _spellInfo.CalcPowerCost(_caster, _spellSchoolMask, this);

			// Set combo point requirement
			if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) ||
			    _CastItem != null)
				_needComboPoints = false;

			int             param1 = 0, param2 = 0;
			SpellCastResult result = CheckCast(true, ref param1, ref param2);

			// target is checked in too many locations and with different results to handle each of them
			// handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC target checks
			if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) &&
			    result == SpellCastResult.BadTargets)
				result = SpellCastResult.SpellCastOk;

			if (result != SpellCastResult.SpellCastOk)
			{
				// Periodic auras should be interrupted when aura triggers a spell which can't be cast
				// for example bladestorm aura should be removed on disarm as of patch 3.3.5
				// channeled periodic spells should be affected by this (arcane missiles, penance, etc)
				// a possible alternative sollution for those would be validating aura target on unit state change
				if (triggeredByAura != null &&
				    triggeredByAura.IsPeriodic() &&
				    !triggeredByAura.GetBase().IsPassive())
				{
					SendChannelUpdate(0);
					triggeredByAura.GetBase().SetDuration(0);
				}

				if (param1 != 0 ||
				    param2 != 0)
					SendCastResult(result, param1, param2);
				else
					SendCastResult(result);

				// queue autorepeat spells for future repeating
				if (GetCurrentContainer() == CurrentSpellTypes.AutoRepeat &&
				    _caster.IsUnit())
					_caster.ToUnit().SetCurrentCastSpell(this);

				Finish(false);

				return result;
			}

			// Prepare data for triggers
			PrepareDataForTriggerSystem();

			_casttime = CallScriptCalcCastTimeHandlers(_spellInfo.CalcCastTime(this));

			if (_caster.IsUnit() &&
			    _caster.ToUnit().IsMoving())
			{
				result = CheckMovement();

				if (result != SpellCastResult.SpellCastOk)
				{
					SendCastResult(result);
					Finish(false);

					return result;
				}
			}

			// Creatures focus their target when possible
			if (_casttime != 0 &&
			    _caster.IsCreature() &&
			    !_spellInfo.IsNextMeleeSwingSpell() &&
			    !IsAutoRepeat() &&
			    !_caster.ToUnit().HasUnitFlag(UnitFlags.Possessed))
			{
				// Channeled spells and some triggered spells do not focus a cast target. They face their target later on via channel object guid and via spell attribute or not at all
				bool focusTarget = !_spellInfo.IsChanneled() && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing);

				if (focusTarget &&
				    _targets.GetObjectTarget() &&
				    _caster != _targets.GetObjectTarget())
					_caster.ToCreature().SetSpellFocus(this, _targets.GetObjectTarget());
				else
					_caster.ToCreature().SetSpellFocus(this, null);
			}

			CallScriptOnPrecastHandler();

			// set timer base at cast time
			ReSetTimer();

			Log.outDebug(LogFilter.Spells, "Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", _spellInfo.Id, _caster.GetEntry(), _originalCaster != null ? (int)_originalCaster.GetEntry() : -1, _triggeredCastFlags, _targets.GetTargetMask());

			if (_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
				SendSpellCooldown();

			//Containers for channeled spells have to be set
			// @todoApply this to all casted spells if needed
			// Why check duration? 29350: channelled triggers channelled
			if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) &&
			    (!_spellInfo.IsChanneled() || _spellInfo.GetMaxDuration() == 0))
			{
				Cast(true);
			}
			else
			{
				// commented out !_spellInfo->StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
				// as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
				// the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
				// because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
				bool willCastDirectly = _casttime == 0 && /*!_spellInfo->StartRecoveryTime && */ GetCurrentContainer() == CurrentSpellTypes.Generic;

				Unit unitCaster = _caster.ToUnit();

				if (unitCaster != null)
				{
					// stealth must be removed at cast starting (at show channel bar)
					// skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
					if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) &&
					    !_spellInfo.HasAttribute(SpellAttr2.NotAnAction))
						unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action, _spellInfo);

					// Do not register as current spell when requested to ignore cast in progress
					// We don't want to interrupt that other spell with cast time
					if (!willCastDirectly ||
					    !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))
						unitCaster.SetCurrentCastSpell(this);
				}

				SendSpellStart();

				if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
					TriggerGlobalCooldown();

				// Call CreatureAI hook OnSpellStart
				Creature caster = _caster.ToCreature();

				if (caster != null)
					if (caster.IsAIEnabled())
						caster.GetAI().OnSpellStart(GetSpellInfo());

				if (willCastDirectly)
					Cast(true);
			}

			return SpellCastResult.SpellCastOk;
		}

		public void Cancel()
		{
			if (_spellState == SpellState.Finished)
				return;

			SpellState oldState = _spellState;
			_spellState = SpellState.Finished;

			_autoRepeat = false;

			switch (oldState)
			{
				case SpellState.Preparing:
					CancelGlobalCooldown();
					goto case SpellState.Delayed;
				case SpellState.Delayed:
					SendInterrupted(0);
					SendCastResult(SpellCastResult.Interrupted);

					break;

				case SpellState.Casting:
					foreach (var ihit in _UniqueTargetInfo)
						if (ihit.MissCondition == SpellMissInfo.None)
						{
							Unit unit = _caster.GetGUID() == ihit.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, ihit.TargetGUID);

							if (unit != null)
								unit.RemoveOwnedAura(_spellInfo.Id, _originalCasterGUID, 0, AuraRemoveMode.Cancel);
						}

					SendChannelUpdate(0);
					SendInterrupted(0);
					SendCastResult(SpellCastResult.Interrupted);

					_appliedMods.Clear();

					break;

				default:
					break;
			}

			SetReferencedFromCurrent(false);

			if (_selfContainer != null &&
			    _selfContainer == this)
				_selfContainer = null;

			// originalcaster handles gameobjects/dynobjects for gob caster
			if (_originalCaster != null)
			{
				_originalCaster.RemoveDynObject(_spellInfo.Id);

				if (_spellInfo.IsChanneled()) // if not channeled then the object for the current cast wasn't summoned yet
					_originalCaster.RemoveGameObject(_spellInfo.Id, true);
			}

			//set state back so finish will be processed
			_spellState = oldState;

			Finish(false);
		}

		public void Cast(bool skipCheck = false)
		{
			Player modOwner     = _caster.GetSpellModOwner();
			Spell  lastSpellMod = null;

			if (modOwner)
			{
				lastSpellMod = modOwner._spellModTakingSpell;

				if (lastSpellMod)
					modOwner.SetSpellModTakingSpell(lastSpellMod, false);
			}

			_cast(skipCheck);

			if (lastSpellMod)
				modOwner.SetSpellModTakingSpell(lastSpellMod, true);
		}

		private void _cast(bool skipCheck = false)
		{
			if (!UpdatePointers())
			{
				// cancel the spell if UpdatePointers() returned false, something wrong happened there
				Cancel();

				return;
			}

			// cancel at lost explicit target during cast
			if (!_targets.GetObjectTargetGUID().IsEmpty() &&
			    _targets.GetObjectTarget() == null)
			{
				Cancel();

				return;
			}

			Player playerCaster = _caster.ToPlayer();

			if (playerCaster != null)
			{
				// now that we've done the basic check, now run the scripts
				// should be done before the spell is actually executed
				Global.ScriptMgr.ForEach<IPlayerOnSpellCast>(p => p.OnSpellCast(playerCaster, this, skipCheck));

				// As of 3.0.2 pets begin attacking their owner's target immediately
				// Let any pets know we've attacked something. Check DmgClass for harmful spells only
				// This prevents spells such as Hunter's Mark from triggering pet attack
				if (_spellInfo.DmgClass != SpellDmgClass.None)
				{
					Unit target = _targets.GetUnitTarget();

					if (target != null)
						foreach (Unit controlled in playerCaster._Controlled)
						{
							Creature cControlled = controlled.ToCreature();

							if (cControlled != null)
							{
								CreatureAI controlledAI = cControlled.GetAI();

								if (controlledAI != null)
									controlledAI.OwnerAttacked(target);
							}
						}
				}
			}

			SetExecutedCurrently(true);

			// Should this be done for original caster?
			Player modOwner = _caster.GetSpellModOwner();

			if (modOwner != null)
				// Set spell which will drop charges for triggered cast spells
				// if not successfully casted, will be remove in finish(false)
				modOwner.SetSpellModTakingSpell(this, true);

			CallScriptBeforeCastHandlers();

			// skip check if done already (for instant cast spells for example)
			if (!skipCheck)
			{
				void cleanupSpell(SpellCastResult result, int? param1 = null, int? param2 = null)
				{
					SendCastResult(result, param1, param2);
					SendInterrupted(0);

					if (modOwner)
						modOwner.SetSpellModTakingSpell(this, false);

					Finish(false);
					SetExecutedCurrently(false);
				}

				int             param1     = 0, param2 = 0;
				SpellCastResult castResult = CheckCast(false, ref param1, ref param2);

				if (castResult != SpellCastResult.SpellCastOk)
				{
					cleanupSpell(castResult, param1, param2);

					return;
				}

				// additional check after cast bar completes (must not be in CheckCast)
				// if trade not complete then remember it in trade data
				if (Convert.ToBoolean(_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
					if (modOwner)
					{
						TradeData my_trade = modOwner.GetTradeData();

						if (my_trade != null)
							if (!my_trade.IsInAcceptProcess())
							{
								// Spell will be casted at completing the trade. Silently ignore at this place
								my_trade.SetSpell(_spellInfo.Id, _CastItem);
								cleanupSpell(SpellCastResult.DontReport);

								return;
							}
					}

				// check diminishing returns (again, only after finish cast bar, tested on retail)
				Unit target = _targets.GetUnitTarget();

				if (target != null)
				{
					uint aura_effmask = 0;

					foreach (var spellEffectInfo in _spellInfo.GetEffects())
						if (spellEffectInfo.IsUnitOwnedAuraEffect())
							aura_effmask |= 1u << (int)spellEffectInfo.EffectIndex;

					if (aura_effmask != 0)
						if (_spellInfo.GetDiminishingReturnsGroupForSpell() != 0)
						{
							DiminishingReturnsType type = _spellInfo.GetDiminishingReturnsGroupType();

							if (type == DiminishingReturnsType.All ||
							    (type == DiminishingReturnsType.Player && target.IsAffectedByDiminishingReturns()))
							{
								Unit caster1 = _originalCaster ? _originalCaster : _caster.ToUnit();

								if (caster1 != null)
									if (target.HasStrongerAuraWithDR(_spellInfo, caster1))
									{
										cleanupSpell(SpellCastResult.AuraBounced);

										return;
									}
							}
						}
				}
			}

			// The spell focusing is making sure that we have a valid cast target guid when we need it so only check for a guid value here.
			Creature creatureCaster = _caster.ToCreature();

			if (creatureCaster != null)
				if (!creatureCaster.GetTarget().IsEmpty() &&
				    !creatureCaster.HasUnitFlag(UnitFlags.Possessed))
				{
					WorldObject target = Global.ObjAccessor.GetUnit(creatureCaster, creatureCaster.GetTarget());

					if (target != null)
						creatureCaster.SetInFront(target);
				}

			SelectSpellTargets();

			// Spell may be finished after target map check
			if (_spellState == SpellState.Finished)
			{
				SendInterrupted(0);

				if (_caster.IsTypeId(TypeId.Player))
					_caster.ToPlayer().SetSpellModTakingSpell(this, false);

				Finish(false);
				SetExecutedCurrently(false);

				return;
			}

			Unit unitCaster = _caster.ToUnit();

			if (unitCaster != null)
				if (_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
				{
					Creature pet = ObjectAccessor.GetCreature(_caster, unitCaster.GetPetGUID());

					if (pet != null)
						pet.DespawnOrUnsummon();
				}

			PrepareTriggersExecutedOnHit();

			CallScriptOnCastHandlers();

			// traded items have trade slot instead of guid in _itemTargetGUID
			// set to real guid to be sent later to the client
			_targets.UpdateTradeSlotItem();

			Player player = _caster.ToPlayer();

			if (player != null)
			{
				if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem) &&
				    _CastItem != null)
				{
					player.StartCriteriaTimer(CriteriaStartEvent.UseItem, _CastItem.GetEntry());
					player.UpdateCriteria(CriteriaType.UseItem, _CastItem.GetEntry());
				}

				player.UpdateCriteria(CriteriaType.CastSpell, _spellInfo.Id);
			}

			Item targetItem = _targets.GetItemTarget();

			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
			{
				// Powers have to be taken before SendSpellGo
				TakePower();
				TakeReagents(); // we must remove reagents before HandleEffects to allow place crafted item in same slot
			}
			else if (targetItem != null)
			{
				// Not own traded item (in trader trade slot) req. reagents including triggered spell case
				if (targetItem.GetOwnerGUID() != _caster.GetGUID())
					TakeReagents();
			}

			// CAST SPELL
			if (!_spellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
				SendSpellCooldown();

			if (_spellInfo.LaunchDelay == 0)
			{
				HandleLaunchPhase();
				_launchHandled = true;
			}

			// we must send smsg_spell_go packet before _castItem delete in TakeCastItem()...
			SendSpellGo();

			if (!_spellInfo.IsChanneled())
				if (creatureCaster != null)
					creatureCaster.ReleaseSpellFocus(this);

			// Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
			if ((_spellInfo.HasHitDelay() && !_spellInfo.IsChanneled()) ||
			    _spellInfo.HasAttribute(SpellAttr4.NoHarmfulThreat))
			{
				// Remove used for cast item if need (it can be already NULL after TakeReagents call
				// in case delayed spell remove item at cast delay start
				TakeCastItem();

				// Okay, maps created, now prepare flags
				_immediateHandled = false;
				_spellState       = SpellState.Delayed;
				SetDelayStart(0);

				unitCaster = _caster.ToUnit();

				if (unitCaster != null)
					if (unitCaster.HasUnitState(UnitState.Casting) &&
					    !unitCaster.IsNonMeleeSpellCast(false, false, true))
						unitCaster.ClearUnitState(UnitState.Casting);
			}
			else
			{
				// Immediate spell, no big deal
				HandleImmediate();
			}

			CallScriptAfterCastHandlers();

			var spell_triggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Cast, _spellInfo.Id);

			if (spell_triggered != null)
				foreach (var spellId in spell_triggered)
					if (spellId < 0)
					{
						unitCaster = _caster.ToUnit();

						if (unitCaster != null)
							unitCaster.RemoveAurasDueToSpell((uint)-spellId);
					}
					else
					{
						_caster.CastSpell(_targets.GetUnitTarget() ?? _caster, (uint)spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(this));
					}

			if (modOwner != null)
			{
				modOwner.SetSpellModTakingSpell(this, false);

				//Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
				if (_originalCaster != null &&
				    modOwner.GetCommandStatus(PlayerCommandStates.Cooldown))
				{
					_originalCaster.GetSpellHistory().ResetCooldown(_spellInfo.Id, true);
					_originalCaster.GetSpellHistory().RestoreCharge(_spellInfo.ChargeCategoryId);
				}
			}

			SetExecutedCurrently(false);

			if (!_originalCaster)
				return;

			// Handle procs on cast
			ProcFlagsInit procAttacker = _procAttacker;

			if (!procAttacker)
			{
				if (_spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
					else
						procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
				}
				else if (_spellInfo.HasAttribute(SpellAttr0.IsAbility))
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulAbility);
					else
						procAttacker.Or(ProcFlags.DealHarmfulSpell);
				}
				else
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulSpell);
					else
						procAttacker.Or(ProcFlags.DealHarmfulSpell);
				}
			}

			procAttacker.Or(ProcFlags2.CastSuccessful);

			ProcFlagsHit hitMask = _hitMask;

			if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
				hitMask |= ProcFlagsHit.Normal;

			if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) &&
			    !_spellInfo.HasAttribute(SpellAttr2.NotAnAction))
				_originalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed, _spellInfo);

			if (!_spellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
				Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

			// Call CreatureAI hook OnSpellCast
			Creature caster = _originalCaster.ToCreature();

			if (caster)
				if (caster.IsAIEnabled())
					caster.GetAI().OnSpellCast(GetSpellInfo());
		}

		private void DoProcessTargetContainer<T>(List<T> targetContainer) where T : TargetInfoBase
		{
			foreach (TargetInfoBase target in targetContainer)
				target.PreprocessTarget(this);

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				foreach (TargetInfoBase target in targetContainer)
					if ((target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
						target.DoTargetSpellHit(this, spellEffectInfo);
			}

			foreach (TargetInfoBase target in targetContainer)
				target.DoDamageAndTriggers(this);
		}

		private void HandleImmediate()
		{
			// start channeling if applicable
			if (_spellInfo.IsChanneled())
			{
				int duration = _spellInfo.GetDuration();

				if (duration > 0 ||
				    _spellValue.Duration.HasValue)
				{
					if (!_spellValue.Duration.HasValue)
					{
						// First mod_duration then haste - see Missile Barrage
						// Apply duration mod
						Player modOwner = _caster.GetSpellModOwner();

						if (modOwner != null)
							modOwner.ApplySpellMod(_spellInfo, SpellModOp.Duration, ref duration);

						duration = (int)(duration * _spellValue.DurationMul);

						// Apply haste mods
						_caster.ModSpellDurationTime(_spellInfo, ref duration, this);
					}
					else
					{
						duration = _spellValue.Duration.Value;
					}

					_channeledDuration = duration;
					SendChannelStart((uint)duration);
				}
				else if (duration == -1)
				{
					SendChannelStart(unchecked((uint)duration));
				}

				if (duration != 0)
				{
					_spellState = SpellState.Casting;
					// GameObjects shouldn't cast channeled spells
					_caster.ToUnit()?.AddInterruptMask(_spellInfo.ChannelInterruptFlags, _spellInfo.ChannelInterruptFlags2);
				}
			}

			PrepareTargetProcessing();

			// process immediate effects (items, ground, etc.) also initialize some variables
			_handle_immediate_phase();

			// consider spell hit for some spells without target, so they may proc on finish phase correctly
			if (_UniqueTargetInfo.Empty())
				_hitMask = ProcFlagsHit.Normal;
			else
				DoProcessTargetContainer(_UniqueTargetInfo);

			DoProcessTargetContainer(_UniqueGOTargetInfo);

			DoProcessTargetContainer(_UniqueCorpseTargetInfo);

			FinishTargetProcessing();

			// spell is finished, perform some last features of the spell here
			_handle_finish_phase();

			// Remove used for cast item if need (it can be already NULL after TakeReagents call
			TakeCastItem();

			if (_spellState != SpellState.Casting)
				Finish(true); // successfully finish spell cast (not last in case autorepeat or channel spell)
		}

		public ulong HandleDelayed(ulong offset)
		{
			if (!UpdatePointers())
			{
				// finish the spell if UpdatePointers() returned false, something wrong happened there
				Finish(false);

				return 0;
			}

			bool  single_missile = _targets.HasDst();
			ulong next_time      = 0;

			if (!_launchHandled)
			{
				ulong launchMoment = (ulong)Math.Floor(_spellInfo.LaunchDelay * 1000.0f);

				if (launchMoment > offset)
					return launchMoment;

				HandleLaunchPhase();
				_launchHandled = true;

				if (_delayMoment > offset)
				{
					if (single_missile)
						return _delayMoment;

					next_time = _delayMoment;

					if ((_UniqueTargetInfo.Count > 2 || (_UniqueTargetInfo.Count == 1 && _UniqueTargetInfo[0].TargetGUID == _caster.GetGUID())) ||
					    !_UniqueGOTargetInfo.Empty())
						offset = 0; // if LaunchDelay was present then the only target that has timeDelay = 0 is _caster - and that is the only target we want to process now
				}
			}

			if (single_missile && offset == 0)
				return _delayMoment;

			Player modOwner = _caster.GetSpellModOwner();

			if (modOwner != null)
				modOwner.SetSpellModTakingSpell(this, true);

			PrepareTargetProcessing();

			if (!_immediateHandled &&
			    offset != 0)
			{
				_handle_immediate_phase();
				_immediateHandled = true;
			}

			// now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
			{
				List<TargetInfo> delayedTargets = new();

				_UniqueTargetInfo.RemoveAll(target =>
				                            {
					                            if (single_missile || target.TimeDelay <= offset)
					                            {
						                            target.TimeDelay = offset;
						                            delayedTargets.Add(target);

						                            return true;
					                            }
					                            else if (next_time == 0 ||
					                                     target.TimeDelay < next_time)
					                            {
						                            next_time = target.TimeDelay;
					                            }

					                            return false;
				                            });

				DoProcessTargetContainer(delayedTargets);
			}

			// now recheck gameobject targeting correctness
			{
				List<GOTargetInfo> delayedGOTargets = new();

				_UniqueGOTargetInfo.RemoveAll(goTarget =>
				                              {
					                              if (single_missile || goTarget.TimeDelay <= offset)
					                              {
						                              goTarget.TimeDelay = offset;
						                              delayedGOTargets.Add(goTarget);

						                              return true;
					                              }
					                              else if (next_time == 0 ||
					                                       goTarget.TimeDelay < next_time)
					                              {
						                              next_time = goTarget.TimeDelay;
					                              }

					                              return false;
				                              });

				DoProcessTargetContainer(delayedGOTargets);
			}

			FinishTargetProcessing();

			if (modOwner)
				modOwner.SetSpellModTakingSpell(this, false);

			// All targets passed - need finish phase
			if (next_time == 0)
			{
				// spell is finished, perform some last features of the spell here
				_handle_finish_phase();

				Finish(true); // successfully finish spell cast

				// return zero, spell is finished now
				return 0;
			}
			else
			{
				// spell is unfinished, return next execution time
				return next_time;
			}
		}

		private void _handle_immediate_phase()
		{
			// handle some immediate features of the spell here
			HandleThreatSpells();

			// handle effects with SPELL_EFFECT_HANDLE_HIT mode
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				// don't do anything for empty effect
				if (!spellEffectInfo.IsEffect())
					continue;

				// call effect handlers to handle destination hit
				HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);
			}

			// process items
			DoProcessTargetContainer(_UniqueItemInfo);
		}

		private void _handle_finish_phase()
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster != null)
			{
				// Take for real after all targets are processed
				if (_needComboPoints)
					unitCaster.ClearComboPoints();

				// Real add combo points from effects
				if (_comboPointGain != 0)
					unitCaster.AddComboPoints(_comboPointGain);

				if (_spellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
					unitCaster.SetLastExtraAttackSpell(_spellInfo.Id);
			}

			// Handle procs on finish
			if (!_originalCaster)
				return;

			ProcFlagsInit procAttacker = _procAttacker;

			if (!procAttacker)
			{
				if (_spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
					else
						procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
				}
				else if (_spellInfo.HasAttribute(SpellAttr0.IsAbility))
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulAbility);
					else
						procAttacker.Or(ProcFlags.DealHarmfulAbility);
				}
				else
				{
					if (IsPositive())
						procAttacker.Or(ProcFlags.DealHelpfulSpell);
					else
						procAttacker.Or(ProcFlags.DealHarmfulSpell);
				}
			}

			if (!_spellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
				Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, _hitMask, this, null, null);
		}

		private void SendSpellCooldown()
		{
			if (!_caster.IsUnit())
				return;

			if (_CastItem)
				_caster.ToUnit().GetSpellHistory().HandleCooldowns(_spellInfo, _CastItem, this);
			else
				_caster.ToUnit().GetSpellHistory().HandleCooldowns(_spellInfo, _castItemEntry, this);

			if (IsAutoRepeat())
				_caster.ToUnit().ResetAttackTimer(WeaponAttackType.RangedAttack);
		}

		public void Update(uint difftime)
		{
			if (!UpdatePointers())
			{
				// cancel the spell if UpdatePointers() returned false, something wrong happened there
				Cancel();

				return;
			}

			if (!_targets.GetUnitTargetGUID().IsEmpty() &&
			    _targets.GetUnitTarget() == null)
			{
				Log.outDebug(LogFilter.Spells, "Spell {0} is cancelled due to removal of target.", _spellInfo.Id);
				Cancel();

				return;
			}

			// check if the player caster has moved before the spell finished
			// with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
			if (_timer != 0 &&
			    _caster.IsUnit() &&
			    _caster.ToUnit().IsMoving() &&
			    CheckMovement() != SpellCastResult.SpellCastOk)
				// if charmed by creature, trust the AI not to cheat and allow the cast to proceed
				// @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
				// however, checking what type of movement the spline is for every single spline would be really expensive
				if (!_caster.ToUnit().GetCharmerGUID().IsCreature())
					Cancel();

			switch (_spellState)
			{
				case SpellState.Preparing:
				{
					if (_timer > 0)
					{
						if (difftime >= _timer)
							_timer = 0;
						else
							_timer -= (int)difftime;
					}

					if (_timer == 0 &&
					    !_spellInfo.IsNextMeleeSwingSpell())
						// don't CheckCast for instant spells - done in spell.prepare, skip duplicate checks, needed for range checks for example
						Cast(_casttime == 0);

					break;
				}
				case SpellState.Casting:
				{
					if (_timer != 0)
					{
						// check if there are alive targets left
						if (!UpdateChanneledTargetList())
						{
							Log.outDebug(LogFilter.Spells, "Channeled spell {0} is removed due to lack of targets", _spellInfo.Id);
							_timer = 0;

							// Also remove applied auras
							foreach (TargetInfo target in _UniqueTargetInfo)
							{
								Unit unit = _caster.GetGUID() == target.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, target.TargetGUID);

								if (unit)
									unit.RemoveOwnedAura(_spellInfo.Id, _originalCasterGUID, 0, AuraRemoveMode.Cancel);
							}
						}

						if (_timer > 0)
						{
							if (difftime >= _timer)
								_timer = 0;
							else
								_timer -= (int)difftime;
						}
					}

					if (_timer == 0)
					{
						SendChannelUpdate(0);
						Finish();

						// We call the hook here instead of in Spell::finish because we only want to call it for completed channeling. Everything else is handled by interrupts
						Creature creatureCaster = _caster.ToCreature();

						if (creatureCaster != null)
							if (creatureCaster.IsAIEnabled())
								creatureCaster.GetAI().OnChannelFinished(_spellInfo);
					}

					break;
				}
				default:
					break;
			}
		}

		public void Finish(bool ok = true)
		{
			if (_spellState == SpellState.Finished)
				return;

			_spellState = SpellState.Finished;

			if (!_caster)
				return;

			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			// successful cast of the initial autorepeat spell is moved to idle state so that it is not deleted as long as autorepeat is active
			if (IsAutoRepeat() &&
			    unitCaster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat) == this)
				_spellState = SpellState.Idle;

			if (_spellInfo.IsChanneled())
				unitCaster.UpdateInterruptMask();

			if (unitCaster.HasUnitState(UnitState.Casting) &&
			    !unitCaster.IsNonMeleeSpellCast(false, false, true))
				unitCaster.ClearUnitState(UnitState.Casting);

			// Unsummon summon as possessed creatures on spell cancel
			if (_spellInfo.IsChanneled() &&
			    unitCaster.IsTypeId(TypeId.Player))
			{
				Unit charm = unitCaster.GetCharmed();

				if (charm != null)
					if (charm.IsTypeId(TypeId.Unit) &&
					    charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet) &&
					    charm._unitData.CreatedBySpell == _spellInfo.Id)
						((Puppet)charm).UnSummon();
			}

			Creature creatureCaster = unitCaster.ToCreature();

			if (creatureCaster != null)
				creatureCaster.ReleaseSpellFocus(this);

			if (!_spellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
				Unit.ProcSkillsAndAuras(unitCaster, null, new ProcFlagsInit(ProcFlags.CastEnded), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, this, null, null);

			if (!ok)
			{
				// on failure (or manual cancel) send TraitConfigCommitFailed to revert talent UI saved config selection
				if (_caster.IsPlayer() &&
				    _spellInfo.HasEffect(SpellEffectName.ChangeActiveCombatTraitConfig))
					if (_customArg is TraitConfig)
						_caster.ToPlayer().SendPacket(new TraitConfigCommitFailed((_customArg as TraitConfig).ID));

				return;
			}

			if (unitCaster.IsTypeId(TypeId.Unit) &&
			    unitCaster.ToCreature().IsSummon())
			{
				// Unsummon statue
				uint      spell     = unitCaster._unitData.CreatedBySpell;
				SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell, GetCastDifficulty());

				if (spellInfo != null &&
				    spellInfo.IconFileDataId == 134230)
				{
					Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", unitCaster.GetGUID().ToString(), _spellInfo.Id);

					// Avoid infinite loops with setDeathState(JUST_DIED) being called over and over
					// It might make sense to do this check in Unit::setDeathState() and all overloaded functions
					if (unitCaster.GetDeathState() != DeathState.JustDied)
						unitCaster.SetDeathState(DeathState.JustDied);

					return;
				}
			}

			if (IsAutoActionResetSpell())
				if (!_spellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
				{
					unitCaster.ResetAttackTimer(WeaponAttackType.BaseAttack);

					if (unitCaster.HaveOffhandWeapon())
						unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);

					unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
				}

			// potions disabled by client, send event "not in combat" if need
			if (unitCaster.IsTypeId(TypeId.Player))
				if (_triggeredByAuraSpell == null)
					unitCaster.ToPlayer().UpdatePotionCooldown(this);

			// Stop Attack for some spells
			if (_spellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat))
				unitCaster.AttackStop();
		}

		private static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, int? param1, int? param2, Player caster) where T : CastFailedBase
		{
			packet.CastID  = castId;
			packet.SpellID = (int)spellInfo.Id;
			packet.Reason  = result;

			switch (result)
			{
				case SpellCastResult.NotReady:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = 0; // unknown (value 1 update cooldowns on client flag)

					break;
				case SpellCastResult.RequiresSpellFocus:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = (int)spellInfo.RequiresSpellFocus; // SpellFocusObject.dbc id

					break;
				case SpellCastResult.RequiresArea: // AreaTable.dbc id
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						// hardcode areas limitation case
						switch (spellInfo.Id)
						{
							case 41617: // Cenarion Mana Salve
							case 41619: // Cenarion Healing Salve
								packet.FailedArg1 = 3905;

								break;
							case 41618: // Bottled Nethergon Energy
							case 41620: // Bottled Nethergon Vapor
								packet.FailedArg1 = 3842;

								break;
							case 45373: // Bloodberry Elixir
								packet.FailedArg1 = 4075;

								break;
							default: // default case (don't must be)
								packet.FailedArg1 = 0;

								break;
						}

					break;
				case SpellCastResult.Totems:
					if (param1.HasValue)
					{
						packet.FailedArg1 = (int)param1;

						if (param2.HasValue)
							packet.FailedArg2 = (int)param2;
					}
					else
					{
						if (spellInfo.Totem[0] != 0)
							packet.FailedArg1 = (int)spellInfo.Totem[0];

						if (spellInfo.Totem[1] != 0)
							packet.FailedArg2 = (int)spellInfo.Totem[1];
					}

					break;
				case SpellCastResult.TotemCategory:
					if (param1.HasValue)
					{
						packet.FailedArg1 = (int)param1;

						if (param2.HasValue)
							packet.FailedArg2 = (int)param2;
					}
					else
					{
						if (spellInfo.TotemCategory[0] != 0)
							packet.FailedArg1 = (int)spellInfo.TotemCategory[0];

						if (spellInfo.TotemCategory[1] != 0)
							packet.FailedArg2 = (int)spellInfo.TotemCategory[1];
					}

					break;
				case SpellCastResult.EquippedItemClass:
				case SpellCastResult.EquippedItemClassMainhand:
				case SpellCastResult.EquippedItemClassOffhand:
					if (param1.HasValue &&
					    param2.HasValue)
					{
						packet.FailedArg1 = (int)param1;
						packet.FailedArg2 = (int)param2;
					}
					else
					{
						packet.FailedArg1 = (int)spellInfo.EquippedItemClass;
						packet.FailedArg2 = spellInfo.EquippedItemSubClassMask;
					}

					break;
				case SpellCastResult.TooManyOfItem:
				{
					if (param1.HasValue)
					{
						packet.FailedArg1 = (int)param1;
					}
					else
					{
						uint item = 0;

						foreach (var spellEffectInfo in spellInfo.GetEffects())
							if (spellEffectInfo.ItemType != 0)
								item = spellEffectInfo.ItemType;

						ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item);

						if (proto != null &&
						    proto.GetItemLimitCategory() != 0)
							packet.FailedArg1 = (int)proto.GetItemLimitCategory();
					}

					break;
				}
				case SpellCastResult.PreventedByMechanic:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = (int)spellInfo.GetAllEffectsMechanicMask(); // SpellMechanic.dbc id

					break;
				case SpellCastResult.NeedExoticAmmo:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = spellInfo.EquippedItemSubClassMask; // seems correct...

					break;
				case SpellCastResult.NeedMoreItems:
					if (param1.HasValue &&
					    param2.HasValue)
					{
						packet.FailedArg1 = (int)param1;
						packet.FailedArg2 = (int)param2;
					}
					else
					{
						packet.FailedArg1 = 0; // Item id
						packet.FailedArg2 = 0; // Item count?
					}

					break;
				case SpellCastResult.MinSkill:
					if (param1.HasValue &&
					    param2.HasValue)
					{
						packet.FailedArg1 = (int)param1;
						packet.FailedArg2 = (int)param2;
					}
					else
					{
						packet.FailedArg1 = 0; // SkillLine.dbc id
						packet.FailedArg2 = 0; // required skill value
					}

					break;
				case SpellCastResult.FishingTooLow:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = 0; // required fishing skill

					break;
				case SpellCastResult.CustomError:
					packet.FailedArg1 = (int)customError;

					break;
				case SpellCastResult.Silenced:
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						packet.FailedArg1 = 0; // Unknown

					break;
				case SpellCastResult.Reagents:
				{
					if (param1.HasValue)
						packet.FailedArg1 = (int)param1;
					else
						for (uint i = 0; i < SpellConst.MaxReagents; i++)
						{
							if (spellInfo.Reagent[i] <= 0)
								continue;

							uint itemid    = (uint)spellInfo.Reagent[i];
							uint itemcount = spellInfo.ReagentCount[i];

							if (!caster.HasItemCount(itemid, itemcount))
							{
								packet.FailedArg1 = (int)itemid; // first missing item

								break;
							}
						}

					if (param2.HasValue)
						packet.FailedArg2 = (int)param2;
					else if (!param1.HasValue)
						foreach (var reagentsCurrency in spellInfo.ReagentsCurrency)
							if (!caster.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
							{
								packet.FailedArg1 = -1;
								packet.FailedArg2 = reagentsCurrency.CurrencyTypesID;

								break;
							}

					break;
				}
				case SpellCastResult.CantUntalent:
				{
					Cypher.Assert(param1.HasValue);
					packet.FailedArg1 = (int)param1;

					break;
				}
				// TODO: SPELL_FAILED_NOT_STANDING
				default:
					break;
			}
		}

		public void SendCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
		{
			if (result == SpellCastResult.SpellCastOk)
				return;

			if (!_caster.IsTypeId(TypeId.Player))
				return;

			if (_caster.ToPlayer().IsLoading()) // don't send cast results at loading time
				return;

			if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
				result = SpellCastResult.DontReport;

			CastFailed castFailed = new();
			castFailed.Visual = _SpellVisual;
			FillSpellCastFailedArgs(castFailed, _castId, _spellInfo, result, _customError, param1, param2, _caster.ToPlayer());
			_caster.ToPlayer().SendPacket(castFailed);
		}

		public void SendPetCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
		{
			if (result == SpellCastResult.SpellCastOk)
				return;

			Unit owner = _caster.GetCharmerOrOwner();

			if (!owner ||
			    !owner.IsTypeId(TypeId.Player))
				return;

			if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
				result = SpellCastResult.DontReport;

			PetCastFailed petCastFailed = new();
			FillSpellCastFailedArgs(petCastFailed, _castId, _spellInfo, result, SpellCustomErrors.None, param1, param2, owner.ToPlayer());
			owner.ToPlayer().SendPacket(petCastFailed);
		}

		public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, int? param1 = null, int? param2 = null)
		{
			if (result == SpellCastResult.SpellCastOk)
				return;

			CastFailed packet = new();
			packet.Visual = spellVisual;
			FillSpellCastFailedArgs(packet, castCount, spellInfo, result, customError, param1, param2, caster);
			caster.SendPacket(packet);
		}

		private void SendMountResult(MountResult result)
		{
			if (result == MountResult.Ok)
				return;

			if (!_caster.IsPlayer())
				return;

			Player caster = _caster.ToPlayer();

			if (caster.IsLoading()) // don't send mount results at loading time
				return;

			MountResultPacket packet = new();
			packet.Result = (uint)result;
			caster.SendPacket(packet);
		}

		private void SendSpellStart()
		{
			if (!IsNeedSendToClient())
				return;

			SpellCastFlags castFlags            = SpellCastFlags.HasTrajectory;
			uint           schoolImmunityMask   = 0;
			ulong          mechanicImmunityMask = 0;
			Unit           unitCaster           = _caster.ToUnit();

			if (unitCaster != null)
			{
				schoolImmunityMask   = _timer != 0 ? unitCaster.GetSchoolImmunityMask() : 0;
				mechanicImmunityMask = _timer != 0 ? _spellInfo.GetMechanicImmunityMask(unitCaster) : 0;
			}

			if (schoolImmunityMask != 0 ||
			    mechanicImmunityMask != 0)
				castFlags |= SpellCastFlags.Immunity;

			if (((IsTriggered() && !_spellInfo.IsAutoRepeatRangedSpell()) || _triggeredByAuraSpell != null) &&
			    !_fromClient)
				castFlags |= SpellCastFlags.Pending;

			if (_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
			    _spellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) ||
			    _spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
				castFlags |= SpellCastFlags.Projectile;

			if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.ToCreature().IsPet())) &&
			    _powerCost.Any(cost => cost.Power != PowerType.Health))
				castFlags |= SpellCastFlags.PowerLeftSelf;

			if (HasPowerTypeCost(PowerType.Runes))
				castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

			SpellStart    packet   = new();
			SpellCastData castData = packet.Cast;

			if (_CastItem)
				castData.CasterGUID = _CastItem.GetGUID();
			else
				castData.CasterGUID = _caster.GetGUID();

			castData.CasterUnit     = _caster.GetGUID();
			castData.CastID         = _castId;
			castData.OriginalCastID = _originalCastId;
			castData.SpellID        = (int)_spellInfo.Id;
			castData.Visual         = _SpellVisual;
			castData.CastFlags      = castFlags;
			castData.CastFlagsEx    = _castFlagsEx;
			castData.CastTime       = (uint)_casttime;

			_targets.Write(castData.Target);

			if (castFlags.HasAnyFlag(SpellCastFlags.PowerLeftSelf))
				foreach (SpellPowerCost cost in _powerCost)
				{
					SpellPowerData powerData;
					powerData.Type = cost.Power;
					powerData.Cost = _caster.ToUnit().GetPower(cost.Power);
					castData.RemainingPower.Add(powerData);
				}

			if (castFlags.HasAnyFlag(SpellCastFlags.RuneList)) // rune cooldowns list
			{
				castData.RemainingRunes = new RuneData();

				RuneData runeData = castData.RemainingRunes;
				//TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
				//The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

				Player player = _caster.ToPlayer();

				if (player)
				{
					runeData.Start = _runesState;            // runes state before
					runeData.Count = player.GetRunesState(); // runes state after

					for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
					{
						// float casts ensure the division is performed on floats as we need float result
						float baseCd = player.GetRuneBaseCooldown();
						runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
					}
				}
				else
				{
					runeData.Start = 0;
					runeData.Count = 0;

					for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
						runeData.Cooldowns.Add(0);
				}
			}

			UpdateSpellCastDataAmmo(castData.Ammo);

			if (castFlags.HasAnyFlag(SpellCastFlags.Immunity))
			{
				castData.Immunities.School = schoolImmunityMask;
				castData.Immunities.Value  = (uint)mechanicImmunityMask;
			}

			/** @todo implement heal prediction packet data
			if (castFlags & CAST_FLAG_HEAL_PREDICTION)
			{
			    castData.Predict.BeconGUID = ??
			    castData.Predict.Points = 0;
			    castData.Predict.Type = 0;
			}**/

			_caster.SendMessageToSet(packet, true);
		}

		private void SendSpellGo()
		{
			// not send invisible spell casting
			if (!IsNeedSendToClient())
				return;

			Log.outDebug(LogFilter.Spells, "Sending SMSG_SPELL_GO id={0}", _spellInfo.Id);

			SpellCastFlags castFlags = SpellCastFlags.Unk9;

			// triggered spells with spell visual != 0
			if (((IsTriggered() && !_spellInfo.IsAutoRepeatRangedSpell()) || _triggeredByAuraSpell != null) &&
			    !_fromClient)
				castFlags |= SpellCastFlags.Pending;

			if (_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
			    _spellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) ||
			    _spellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
				castFlags |= SpellCastFlags.Projectile; // arrows/bullets visual

			if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.ToCreature().IsPet())) &&
			    _powerCost.Any(cost => cost.Power != PowerType.Health))
				castFlags |= SpellCastFlags.PowerLeftSelf;

			if (_caster.IsTypeId(TypeId.Player) &&
			    _caster.ToPlayer().GetClass() == Class.Deathknight &&
			    HasPowerTypeCost(PowerType.Runes) &&
			    !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
			{
				castFlags |= SpellCastFlags.NoGCD;    // same as in SMSG_SPELL_START
				castFlags |= SpellCastFlags.RuneList; // rune cooldowns list
			}

			if (_targets.HasTraj())
				castFlags |= SpellCastFlags.AdjustMissile;

			if (_spellInfo.StartRecoveryTime == 0)
				castFlags |= SpellCastFlags.NoGCD;

			SpellGo       packet   = new();
			SpellCastData castData = packet.Cast;

			if (_CastItem != null)
				castData.CasterGUID = _CastItem.GetGUID();
			else
				castData.CasterGUID = _caster.GetGUID();

			castData.CasterUnit     = _caster.GetGUID();
			castData.CastID         = _castId;
			castData.OriginalCastID = _originalCastId;
			castData.SpellID        = (int)_spellInfo.Id;
			castData.Visual         = _SpellVisual;
			castData.CastFlags      = castFlags;
			castData.CastFlagsEx    = _castFlagsEx;
			castData.CastTime       = Time.GetMSTime();

			castData.HitTargets = new List<ObjectGuid>();
			UpdateSpellCastDataTargets(castData);

			_targets.Write(castData.Target);

			if (Convert.ToBoolean(castFlags & SpellCastFlags.PowerLeftSelf))
			{
				castData.RemainingPower = new List<SpellPowerData>();

				foreach (SpellPowerCost cost in _powerCost)
				{
					SpellPowerData powerData;
					powerData.Type = cost.Power;
					powerData.Cost = _caster.ToUnit().GetPower(cost.Power);
					castData.RemainingPower.Add(powerData);
				}
			}

			if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList)) // rune cooldowns list
			{
				castData.RemainingRunes = new RuneData();
				RuneData runeData = castData.RemainingRunes;

				Player player = _caster.ToPlayer();
				runeData.Start = _runesState;            // runes state before
				runeData.Count = player.GetRunesState(); // runes state after

				for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				{
					// float casts ensure the division is performed on floats as we need float result
					float baseCd = (float)player.GetRuneBaseCooldown();
					runeData.Cooldowns.Add((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
				}
			}

			if (castFlags.HasFlag(SpellCastFlags.AdjustMissile))
			{
				castData.MissileTrajectory.TravelTime = (uint)_delayMoment;
				castData.MissileTrajectory.Pitch      = _targets.GetPitch();
			}

			packet.LogData.Initialize(this);

			_caster.SendCombatLogMessage(packet);
		}

		// Writes miss and hit targets for a SMSG_SPELL_GO packet
		private void UpdateSpellCastDataTargets(SpellCastData data)
		{
			// This function also fill data for channeled spells:
			// _needAliveTargetMask req for stop channelig if one target die
			foreach (var targetInfo in _UniqueTargetInfo)
			{
				if (targetInfo.EffectMask == 0) // No effect apply - all immuned add state
					// possibly SPELL_MISS_IMMUNE2 for this??
					targetInfo.MissCondition = SpellMissInfo.Immune2;

				if (targetInfo.MissCondition == SpellMissInfo.None ||
				    (targetInfo.MissCondition == SpellMissInfo.Block && !_spellInfo.HasAttribute(SpellAttr3.CompletelyBlocked))) // Add only hits and partial blocked
				{
					data.HitTargets.Add(targetInfo.TargetGUID);
					data.HitStatus.Add(new SpellHitStatus(SpellMissInfo.None));

					_channelTargetEffectMask |= targetInfo.EffectMask;
				}
				else // misses
				{
					data.MissTargets.Add(targetInfo.TargetGUID);

					data.MissStatus.Add(new SpellMissStatus(targetInfo.MissCondition, targetInfo.ReflectResult));
				}
			}

			foreach (GOTargetInfo targetInfo in _UniqueGOTargetInfo)
				data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

			foreach (CorpseTargetInfo targetInfo in _UniqueCorpseTargetInfo)
				data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

			// Reset _needAliveTargetMask for non channeled spell
			if (!_spellInfo.IsChanneled())
				_channelTargetEffectMask = 0;
		}

		private void UpdateSpellCastDataAmmo(SpellAmmo ammo)
		{
			InventoryType ammoInventoryType = 0;
			uint          ammoDisplayID     = 0;

			Player playerCaster = _caster.ToPlayer();

			if (playerCaster != null)
			{
				Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

				if (pItem)
				{
					ammoInventoryType = pItem.GetTemplate().GetInventoryType();

					if (ammoInventoryType == InventoryType.Thrown)
					{
						ammoDisplayID = pItem.GetDisplayId(playerCaster);
					}
					else if (playerCaster.HasAura(46699)) // Requires No Ammo
					{
						ammoDisplayID     = 5996; // normal arrow
						ammoInventoryType = InventoryType.Ammo;
					}
				}
			}
			else
			{
				Unit unitCaster = _caster.ToUnit();

				if (unitCaster != null)
				{
					uint          nonRangedAmmoDisplayID     = 0;
					InventoryType nonRangedAmmoInventoryType = 0;

					for (byte i = (int)WeaponAttackType.BaseAttack; i < (int)WeaponAttackType.Max; ++i)
					{
						uint itemId = unitCaster.GetVirtualItemId(i);

						if (itemId != 0)
						{
							ItemRecord itemEntry = CliDB.ItemStorage.LookupByKey(itemId);

							if (itemEntry != null)
								if (itemEntry.ClassID == ItemClass.Weapon)
								{
									switch ((ItemSubClassWeapon)itemEntry.SubclassID)
									{
										case ItemSubClassWeapon.Thrown:
											ammoDisplayID     = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
											ammoInventoryType = (InventoryType)itemEntry.inventoryType;

											break;
										case ItemSubClassWeapon.Bow:
										case ItemSubClassWeapon.Crossbow:
											ammoDisplayID     = 5996; // is this need fixing?
											ammoInventoryType = InventoryType.Ammo;

											break;
										case ItemSubClassWeapon.Gun:
											ammoDisplayID     = 5998; // is this need fixing?
											ammoInventoryType = InventoryType.Ammo;

											break;
										default:
											nonRangedAmmoDisplayID     = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
											nonRangedAmmoInventoryType = itemEntry.inventoryType;

											break;
									}

									if (ammoDisplayID != 0)
										break;
								}
						}
					}

					if (ammoDisplayID == 0 &&
					    ammoInventoryType == 0)
					{
						ammoDisplayID     = nonRangedAmmoDisplayID;
						ammoInventoryType = nonRangedAmmoInventoryType;
					}
				}
			}

			ammo.DisplayID     = (int)ammoDisplayID;
			ammo.InventoryType = (sbyte)ammoInventoryType;
		}

		private void SendSpellExecuteLog()
		{
			if (_executeLogEffects.Empty())
				return;

			SpellExecuteLog spellExecuteLog = new();

			spellExecuteLog.Caster  = _caster.GetGUID();
			spellExecuteLog.SpellID = _spellInfo.Id;
			spellExecuteLog.Effects = _executeLogEffects.Values.ToList();
			spellExecuteLog.LogData.Initialize(this);

			_caster.SendCombatLogMessage(spellExecuteLog);
		}

		public SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
		{
			var spellLogEffect = _executeLogEffects.LookupByKey(effect);

			if (spellLogEffect != null)
				return spellLogEffect;

			SpellLogEffect executeLogEffect = new();
			executeLogEffect.Effect = (int)effect;
			_executeLogEffects.Add(effect, executeLogEffect);

			return executeLogEffect;
		}

		private void ExecuteLogEffectTakeTargetPower(SpellEffectName effect, Unit target, PowerType powerType, uint points, float amplitude)
		{
			SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

			spellLogEffectPowerDrainParams.Victim    = target.GetGUID();
			spellLogEffectPowerDrainParams.Points    = points;
			spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
			spellLogEffectPowerDrainParams.Amplitude = amplitude;

			GetExecuteLogEffect(effect).PowerDrainTargets.Add(spellLogEffectPowerDrainParams);
		}

		private void ExecuteLogEffectExtraAttacks(SpellEffectName effect, Unit victim, uint numAttacks)
		{
			SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
			spellLogEffectExtraAttacksParams.Victim     = victim.GetGUID();
			spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

			GetExecuteLogEffect(effect).ExtraAttacksTargets.Add(spellLogEffectExtraAttacksParams);
		}

		private void SendSpellInterruptLog(Unit victim, uint spellId)
		{
			SpellInterruptLog data = new();
			data.Caster             = _caster.GetGUID();
			data.Victim             = victim.GetGUID();
			data.InterruptedSpellID = _spellInfo.Id;
			data.SpellID            = spellId;

			_caster.SendMessageToSet(data, true);
		}

		private void ExecuteLogEffectDurabilityDamage(SpellEffectName effect, Unit victim, int itemId, int amount)
		{
			SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
			spellLogEffectDurabilityDamageParams.Victim = victim.GetGUID();
			spellLogEffectDurabilityDamageParams.ItemID = itemId;
			spellLogEffectDurabilityDamageParams.Amount = amount;

			GetExecuteLogEffect(effect).DurabilityDamageTargets.Add(spellLogEffectDurabilityDamageParams);
		}

		private void ExecuteLogEffectOpenLock(SpellEffectName effect, WorldObject obj)
		{
			SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
			spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

			GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
		}

		private void ExecuteLogEffectCreateItem(SpellEffectName effect, uint entry)
		{
			SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
			spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

			GetExecuteLogEffect(effect).TradeSkillTargets.Add(spellLogEffectTradeSkillItemParams);
		}

		private void ExecuteLogEffectDestroyItem(SpellEffectName effect, uint entry)
		{
			SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
			spellLogEffectFeedPetParams.ItemID = (int)entry;

			GetExecuteLogEffect(effect).FeedPetTargets.Add(spellLogEffectFeedPetParams);
		}

		private void ExecuteLogEffectSummonObject(SpellEffectName effect, WorldObject obj)
		{
			SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
			spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

			GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
		}

		private void ExecuteLogEffectUnsummonObject(SpellEffectName effect, WorldObject obj)
		{
			SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
			spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

			GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
		}

		private void ExecuteLogEffectResurrect(SpellEffectName effect, Unit target)
		{
			SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
			spellLogEffectGenericVictimParams.Victim = target.GetGUID();

			GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
		}

		private void SendInterrupted(byte result)
		{
			SpellFailure failurePacket = new();
			failurePacket.CasterUnit = _caster.GetGUID();
			failurePacket.CastID     = _castId;
			failurePacket.SpellID    = _spellInfo.Id;
			failurePacket.Visual     = _SpellVisual;
			failurePacket.Reason     = result;
			_caster.SendMessageToSet(failurePacket, true);

			SpellFailedOther failedPacket = new();
			failedPacket.CasterUnit = _caster.GetGUID();
			failedPacket.CastID     = _castId;
			failedPacket.SpellID    = _spellInfo.Id;
			failedPacket.Visual     = _SpellVisual;
			failedPacket.Reason     = result;
			_caster.SendMessageToSet(failedPacket, true);
		}

		public void SendChannelUpdate(uint time)
		{
			// GameObjects don't channel
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			if (time == 0)
			{
				unitCaster.ClearChannelObjects();
				unitCaster.SetChannelSpellId(0);
				unitCaster.SetChannelVisual(new SpellCastVisualField());
			}

			SpellChannelUpdate spellChannelUpdate = new();
			spellChannelUpdate.CasterGUID    = unitCaster.GetGUID();
			spellChannelUpdate.TimeRemaining = (int)time;
			unitCaster.SendMessageToSet(spellChannelUpdate, true);
		}

		private void SendChannelStart(uint duration)
		{
			// GameObjects don't channel
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			SpellChannelStart spellChannelStart = new();
			spellChannelStart.CasterGUID      = unitCaster.GetGUID();
			spellChannelStart.SpellID         = (int)_spellInfo.Id;
			spellChannelStart.Visual          = _SpellVisual;
			spellChannelStart.ChannelDuration = duration;

			uint  schoolImmunityMask   = unitCaster.GetSchoolImmunityMask();
			ulong mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();

			if (schoolImmunityMask != 0 ||
			    mechanicImmunityMask != 0)
			{
				SpellChannelStartInterruptImmunities interruptImmunities = new();
				interruptImmunities.SchoolImmunities = (int)schoolImmunityMask;
				interruptImmunities.Immunities       = (int)mechanicImmunityMask;

				spellChannelStart.InterruptImmunities = interruptImmunities;
			}

			unitCaster.SendMessageToSet(spellChannelStart, true);

			_timer = (int)duration;

			if (!_targets.HasDst())
			{
				uint channelAuraMask          = 0;
				uint explicitTargetEffectMask = 0xFFFFFFFF;

				// if there is an explicit target, only add channel objects from effects that also hit ut
				if (!_targets.GetUnitTargetGUID().IsEmpty())
				{
					var explicitTarget = _UniqueTargetInfo.Find(target => target.TargetGUID == _targets.GetUnitTargetGUID());

					if (explicitTarget != null)
						explicitTargetEffectMask = explicitTarget.EffectMask;
				}

				foreach (var spellEffectInfo in _spellInfo.GetEffects())
					if (spellEffectInfo.Effect == SpellEffectName.ApplyAura &&
					    (explicitTargetEffectMask & (1u << (int)spellEffectInfo.EffectIndex)) != 0)
						channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

				foreach (TargetInfo target in _UniqueTargetInfo)
				{
					if ((target.EffectMask & channelAuraMask) == 0)
						continue;

					SpellAttr1 requiredAttribute = target.TargetGUID != unitCaster.GetGUID() ? SpellAttr1.IsChannelled : SpellAttr1.IsSelfChannelled;

					if (!_spellInfo.HasAttribute(requiredAttribute))
						continue;

					unitCaster.AddChannelObject(target.TargetGUID);
				}

				foreach (GOTargetInfo target in _UniqueGOTargetInfo)
					if ((target.EffectMask & channelAuraMask) != 0)
						unitCaster.AddChannelObject(target.TargetGUID);
			}
			else if (_spellInfo.HasAttribute(SpellAttr1.IsSelfChannelled))
			{
				unitCaster.AddChannelObject(unitCaster.GetGUID());
			}

			Creature creatureCaster = unitCaster.ToCreature();

			if (creatureCaster != null)
				if (unitCaster._unitData.ChannelObjects.Size() == 1 &&
				    unitCaster._unitData.ChannelObjects[0].IsUnit())
					if (!creatureCaster.HasSpellFocus(this))
						creatureCaster.SetSpellFocus(this, Global.ObjAccessor.GetWorldObject(creatureCaster, unitCaster._unitData.ChannelObjects[0]));

			unitCaster.SetChannelSpellId(_spellInfo.Id);
			unitCaster.SetChannelVisual(_SpellVisual);
		}

		private void SendResurrectRequest(Player target)
		{
			// get resurrector name for creature resurrections, otherwise packet will be not accepted
			// for player resurrections the name is looked up by guid
			string sentName = "";

			if (!_caster.IsPlayer())
				sentName = _caster.GetName(target.GetSession().GetSessionDbLocaleIndex());

			ResurrectRequest resurrectRequest = new();
			resurrectRequest.ResurrectOffererGUID                = _caster.GetGUID();
			resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
			resurrectRequest.Name                                = sentName;
			resurrectRequest.Sickness                            = _caster.IsUnit() && !_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"
			resurrectRequest.UseTimer                            = !_spellInfo.HasAttribute(SpellAttr3.NoResTimer);

			Pet pet = target.GetPet();

			if (pet)
			{
				CharmInfo charmInfo = pet.GetCharmInfo();

				if (charmInfo != null)
					resurrectRequest.PetNumber = charmInfo.GetPetNumber();
			}

			resurrectRequest.SpellID = _spellInfo.Id;

			target.SendPacket(resurrectRequest);
		}

		private void TakeCastItem()
		{
			if (_CastItem == null ||
			    !_caster.IsTypeId(TypeId.Player))
				return;

			// not remove cast item at triggered spell (equipping, weapon damage, etc)
			if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem))
				return;

			ItemTemplate proto = _CastItem.GetTemplate();

			if (proto == null)
			{
				// This code is to avoid a crash
				// I'm not sure, if this is really an error, but I guess every item needs a prototype
				Log.outError(LogFilter.Spells, "Cast item has no item prototype {0}", _CastItem.GetGUID().ToString());

				return;
			}

			bool expendable     = false;
			bool withoutCharges = false;

			foreach (ItemEffectRecord itemEffect in _CastItem.GetEffects())
			{
				if (itemEffect.LegacySlotIndex >= _CastItem._itemData.SpellCharges.GetSize())
					continue;

				// item has limited charges
				if (itemEffect.Charges != 0)
				{
					if (itemEffect.Charges < 0)
						expendable = true;

					int charges = _CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

					// item has charges left
					if (charges != 0)
					{
						if (charges > 0)
							--charges;
						else
							++charges;

						if (proto.GetMaxStackSize() == 1)
							_CastItem.SetSpellCharges(itemEffect.LegacySlotIndex, charges);

						_CastItem.SetState(ItemUpdateState.Changed, _caster.ToPlayer());
					}

					// all charges used
					withoutCharges = (charges == 0);
				}
			}

			if (expendable && withoutCharges)
			{
				uint count = 1;
				_caster.ToPlayer().DestroyItemCount(_CastItem, ref count, true);

				// prevent crash at access to deleted _targets.GetItemTarget
				if (_CastItem == _targets.GetItemTarget())
					_targets.SetItemTarget(null);

				_CastItem = null;
				_castItemGUID.Clear();
				_castItemEntry = 0;
			}
		}

		private void TakePower()
		{
			// GameObjects don't use power
			Unit unitCaster = _caster.ToUnit();

			if (!unitCaster)
				return;

			if (_CastItem != null ||
			    _triggeredByAuraSpell != null)
				return;

			//Don't take power if the spell is cast while .cheat power is enabled.
			if (unitCaster.IsTypeId(TypeId.Player))
				if (unitCaster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
					return;

			foreach (SpellPowerCost cost in _powerCost)
			{
				bool hit = true;

				if (unitCaster.IsTypeId(TypeId.Player))
					if (_spellInfo.HasAttribute(SpellAttr1.DiscountPowerOnMiss))
					{
						ObjectGuid targetGUID = _targets.GetUnitTargetGUID();

						if (!targetGUID.IsEmpty())
						{
							var ihit = _UniqueTargetInfo.FirstOrDefault(targetInfo => targetInfo.TargetGUID == targetGUID && targetInfo.MissCondition != SpellMissInfo.None);

							if (ihit != null)
							{
								hit = false;
								//lower spell cost on fail (by talent aura)
								Player modOwner = unitCaster.GetSpellModOwner();

								if (modOwner != null)
									modOwner.ApplySpellMod(_spellInfo, SpellModOp.PowerCostOnMiss, ref cost.Amount);
							}
						}
					}

				if (cost.Power == PowerType.Runes)
				{
					TakeRunePower(hit);

					continue;
				}

				if (cost.Amount == 0)
					continue;

				// health as power used
				if (cost.Power == PowerType.Health)
				{
					unitCaster.ModifyHealth(-cost.Amount);

					continue;
				}

				if (cost.Power >= PowerType.Max)
				{
					Log.outError(LogFilter.Spells, "Spell.TakePower: Unknown power type '{0}'", cost.Power);

					continue;
				}

				unitCaster.ModifyPower(cost.Power, -cost.Amount);
			}
		}

		private SpellCastResult CheckRuneCost()
		{
			int runeCost = _powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

			if (runeCost == 0)
				return SpellCastResult.SpellCastOk;

			Player player = _caster.ToPlayer();

			if (!player)
				return SpellCastResult.SpellCastOk;

			if (player.GetClass() != Class.Deathknight)
				return SpellCastResult.SpellCastOk;

			int readyRunes = 0;

			for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				if (player.GetRuneCooldown(i) == 0)
					++readyRunes;

			if (readyRunes < runeCost)
				return SpellCastResult.NoPower; // not sure if result code is correct

			return SpellCastResult.SpellCastOk;
		}

		private void TakeRunePower(bool didHit)
		{
			if (!_caster.IsTypeId(TypeId.Player) ||
			    _caster.ToPlayer().GetClass() != Class.Deathknight)
				return;

			Player player = _caster.ToPlayer();
			_runesState = player.GetRunesState(); // store previous state

			int runeCost = _powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

			for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				if (player.GetRuneCooldown(i) == 0 &&
				    runeCost > 0)
				{
					player.SetRuneCooldown(i, didHit ? player.GetRuneBaseCooldown() : RuneCooldowns.Miss);
					--runeCost;
				}
		}

		private void TakeReagents()
		{
			if (!_caster.IsTypeId(TypeId.Player))
				return;

			// do not take reagents for these item casts
			if (_CastItem != null &&
			    _CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
				return;

			Player p_caster = _caster.ToPlayer();

			if (p_caster.CanNoReagentCast(_spellInfo))
				return;

			for (int x = 0; x < SpellConst.MaxReagents; ++x)
			{
				if (_spellInfo.Reagent[x] <= 0)
					continue;

				uint itemid    = (uint)_spellInfo.Reagent[x];
				uint itemcount = _spellInfo.ReagentCount[x];

				// if CastItem is also spell reagent
				if (_CastItem != null &&
				    _CastItem.GetEntry() == itemid)
				{
					foreach (ItemEffectRecord itemEffect in _CastItem.GetEffects())
					{
						if (itemEffect.LegacySlotIndex >= _CastItem._itemData.SpellCharges.GetSize())
							continue;

						// CastItem will be used up and does not count as reagent
						int charges = _CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

						if (itemEffect.Charges < 0 &&
						    Math.Abs(charges) < 2)
						{
							++itemcount;

							break;
						}
					}

					_CastItem = null;
					_castItemGUID.Clear();
					_castItemEntry = 0;
				}

				// if GetItemTarget is also spell reagent
				if (_targets.GetItemTargetEntry() == itemid)
					_targets.SetItemTarget(null);

				p_caster.DestroyItemCount(itemid, itemcount, true);
			}

			foreach (var reagentsCurrency in _spellInfo.ReagentsCurrency)
				p_caster.ModifyCurrency(reagentsCurrency.CurrencyTypesID, -reagentsCurrency.CurrencyCount, false, true);
		}

		private void HandleThreatSpells()
		{
			// wild GameObject spells don't cause threat
			Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

			if (unitCaster == null)
				return;

			if (_UniqueTargetInfo.Empty())
				return;

			if (!_spellInfo.HasInitialAggro())
				return;

			float            threat      = 0.0f;
			SpellThreatEntry threatEntry = Global.SpellMgr.GetSpellThreatEntry(_spellInfo.Id);

			if (threatEntry != null)
			{
				if (threatEntry.apPctMod != 0.0f)
					threat += threatEntry.apPctMod * unitCaster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

				threat += threatEntry.flatMod;
			}
			else if (!_spellInfo.HasAttribute(SpellCustomAttributes.NoInitialThreat))
			{
				threat += _spellInfo.SpellLevel;
			}

			// past this point only multiplicative effects occur
			if (threat == 0.0f)
				return;

			// since 2.0.1 threat from positive effects also is distributed among all targets, so the overall caused threat is at most the defined bonus
			threat /= _UniqueTargetInfo.Count;

			foreach (var ihit in _UniqueTargetInfo)
			{
				float threatToAdd = threat;

				if (ihit.MissCondition != SpellMissInfo.None)
					threatToAdd = 0.0f;

				Unit target = Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);

				if (target == null)
					continue;

				// positive spells distribute threat among all units that are in combat with target, like healing
				if (IsPositive())
				{
					target.GetThreatManager().ForwardThreatForAssistingMe(unitCaster, threatToAdd, _spellInfo);
				}
				// for negative spells threat gets distributed among affected targets
				else
				{
					if (!target.CanHaveThreatList())
						continue;

					target.GetThreatManager().AddThreat(unitCaster, threatToAdd, _spellInfo, true);
				}
			}

			Log.outDebug(LogFilter.Spells, "Spell {0}, added an additional {1} threat for {2} {3} target(s)", _spellInfo.Id, threat, IsPositive() ? "assisting" : "harming", _UniqueTargetInfo.Count);
		}

		public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGoTarget, Corpse pCorpseTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
		{
			effectHandleMode = mode;
			unitTarget       = pUnitTarget;
			itemTarget       = pItemTarget;
			gameObjTarget    = pGoTarget;
			corpseTarget     = pCorpseTarget;
			destTarget       = _destTargets[spellEffectInfo.EffectIndex].Position;
			effectInfo       = spellEffectInfo;

			damage = CalculateDamage(spellEffectInfo, unitTarget, out variance);

			bool preventDefault = CallScriptEffectHandlers(spellEffectInfo.EffectIndex, mode);

			if (!preventDefault)
				Global.SpellMgr.GetSpellEffectHandler(spellEffectInfo.Effect).Invoke(this);
		}

		public static Spell ExtractSpellFromEvent(BasicEvent basicEvent)
		{
			SpellEvent spellEvent = (SpellEvent)basicEvent;

			if (spellEvent != null)
				return spellEvent.GetSpell();

			return null;
		}

		public SpellCastResult CheckCast(bool strict)
		{
			int param1 = 0, param2 = 0;

			return CheckCast(strict, ref param1, ref param2);
		}

		public SpellCastResult CheckCast(bool strict, ref int param1, ref int param2)
		{
			SpellCastResult castResult;

			// check death state
			if (_caster.ToUnit() &&
			    !_caster.ToUnit().IsAlive() &&
			    !_spellInfo.IsPassive() &&
			    !(_spellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead) || (IsTriggered() && _triggeredByAuraSpell == null)))
				return SpellCastResult.CasterDead;

			// Prevent cheating in case the player has an immunity effect and tries to interact with a non-allowed gameobject. The error message is handled by the client so we don't report anything here
			if (_caster.IsPlayer() &&
			    _targets.GetGOTarget() != null)
				if (_targets.GetGOTarget().GetGoInfo().GetNoDamageImmune() != 0 &&
				    _caster.ToUnit().HasUnitFlag(UnitFlags.Immune))
					return SpellCastResult.DontReport;

			// check cooldowns to prevent cheating
			if (!_spellInfo.IsPassive())
			{
				Player playerCaster = _caster.ToPlayer();

				if (playerCaster != null)
				{
					//can cast triggered (by aura only?) spells while have this flag
					if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate))
					{
						// These two auras check SpellFamilyName defined by db2 class data instead of current spell SpellFamilyName
						if (playerCaster.HasAuraType(AuraType.DisableCastingExceptAbilities) &&
						    !_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) &&
						    !_spellInfo.HasEffect(SpellEffectName.Attack) &&
						    !_spellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled) &&
						    !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, _spellInfo.SpellFamilyFlags))
							return SpellCastResult.CantDoThatRightNow;

						if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
							if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, _spellInfo.SpellFamilyFlags))
								if (_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
								    _spellInfo.IsNextMeleeSwingSpell() ||
								    _spellInfo.HasAttribute(SpellAttr1.InitiatesCombatEnablesAutoAttack) ||
								    _spellInfo.HasAttribute(SpellAttr2.InitiateCombatPostCastEnablesAutoAttack) ||
								    _spellInfo.HasEffect(SpellEffectName.Attack) ||
								    _spellInfo.HasEffect(SpellEffectName.NormalizedWeaponDmg) ||
								    _spellInfo.HasEffect(SpellEffectName.WeaponDamageNoSchool) ||
								    _spellInfo.HasEffect(SpellEffectName.WeaponPercentDamage) ||
								    _spellInfo.HasEffect(SpellEffectName.WeaponDamage))
									return SpellCastResult.CantDoThatRightNow;
					}

					// check if we are using a potion in combat for the 2nd+ time. Cooldown is added only after caster gets out of combat
					if (!IsIgnoringCooldowns() &&
					    playerCaster.GetLastPotionId() != 0 &&
					    _CastItem &&
					    (_CastItem.IsPotion() || _spellInfo.IsCooldownStartedOnEvent()))
						return SpellCastResult.NotReady;
				}

				if (!IsIgnoringCooldowns() &&
				    _caster.ToUnit() != null)
				{
					if (!_caster.ToUnit().GetSpellHistory().IsReady(_spellInfo, _castItemEntry))
					{
						if (_triggeredByAuraSpell != null)
							return SpellCastResult.DontReport;
						else
							return SpellCastResult.NotReady;
					}

					if ((IsAutoRepeat() || _spellInfo.CategoryId == 76) &&
					    !_caster.ToUnit().IsAttackReady(WeaponAttackType.RangedAttack))
						return SpellCastResult.DontReport;
				}
			}

			if (_spellInfo.HasAttribute(SpellAttr7.IsCheatSpell) &&
			    _caster.IsUnit() &&
			    !_caster.ToUnit().HasUnitFlag2(UnitFlags2.AllowCheatSpells))
			{
				_customError = SpellCustomErrors.GmOnly;

				return SpellCastResult.CustomError;
			}

			// Check global cooldown
			if (strict &&
			    !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) &&
			    HasGlobalCooldown())
				return !_spellInfo.HasAttribute(SpellAttr0.CooldownOnEvent) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

			// only triggered spells can be processed an ended Battleground
			if (!IsTriggered() &&
			    _caster.IsTypeId(TypeId.Player))
			{
				Battleground bg = _caster.ToPlayer().GetBattleground();

				if (bg)
					if (bg.GetStatus() == BattlegroundStatus.WaitLeave)
						return SpellCastResult.DontReport;
			}

			if (_caster.IsTypeId(TypeId.Player) &&
			    Global.VMapMgr.IsLineOfSightCalcEnabled())
			{
				if (_spellInfo.HasAttribute(SpellAttr0.OnlyOutdoors) &&
				    !_caster.IsOutdoors())
					return SpellCastResult.OnlyOutdoors;

				if (_spellInfo.HasAttribute(SpellAttr0.OnlyIndoors) &&
				    _caster.IsOutdoors())
					return SpellCastResult.OnlyIndoors;
			}

			Unit unitCaster = _caster.ToUnit();

			if (unitCaster != null)
			{
				if (_spellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed) &&
				    unitCaster.IsCharmed())
					return SpellCastResult.Charmed;

				// only check at first call, Stealth auras are already removed at second call
				// for now, ignore triggered spells
				if (strict && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreShapeshift))
				{
					bool checkForm = true;
					// Ignore form req aura
					var ignore = unitCaster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);

					foreach (var aurEff in ignore)
					{
						if (!aurEff.IsAffectingSpell(_spellInfo))
							continue;

						checkForm = false;

						break;
					}

					if (checkForm)
					{
						// Cannot be used in this stance/form
						SpellCastResult shapeError = _spellInfo.CheckShapeshift(unitCaster.GetShapeshiftForm());

						if (shapeError != SpellCastResult.SpellCastOk)
							return shapeError;

						if (_spellInfo.HasAttribute(SpellAttr0.OnlyStealthed) &&
						    !unitCaster.HasStealthAura())
							return SpellCastResult.OnlyStealthed;
					}
				}

				bool reqCombat  = true;
				var  stateAuras = unitCaster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);

				foreach (var aura in stateAuras)
					if (aura.IsAffectingSpell(_spellInfo))
					{
						_needComboPoints = false;

						if (aura.GetMiscValue() == 1)
						{
							reqCombat = false;

							break;
						}
					}

				// caster state requirements
				// not for triggered spells (needed by execute)
				if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterAurastate))
				{
					if (_spellInfo.CasterAuraState != 0 &&
					    !unitCaster.HasAuraState(_spellInfo.CasterAuraState, _spellInfo, unitCaster))
						return SpellCastResult.CasterAurastate;

					if (_spellInfo.ExcludeCasterAuraState != 0 &&
					    unitCaster.HasAuraState(_spellInfo.ExcludeCasterAuraState, _spellInfo, unitCaster))
						return SpellCastResult.CasterAurastate;

					// Note: spell 62473 requres casterAuraSpell = triggering spell
					if (_spellInfo.CasterAuraSpell != 0 &&
					    !unitCaster.HasAura(_spellInfo.CasterAuraSpell))
						return SpellCastResult.CasterAurastate;

					if (_spellInfo.ExcludeCasterAuraSpell != 0 &&
					    unitCaster.HasAura(_spellInfo.ExcludeCasterAuraSpell))
						return SpellCastResult.CasterAurastate;

					if (_spellInfo.CasterAuraType != 0 &&
					    !unitCaster.HasAuraType(_spellInfo.CasterAuraType))
						return SpellCastResult.CasterAurastate;

					if (_spellInfo.ExcludeCasterAuraType != 0 &&
					    unitCaster.HasAuraType(_spellInfo.ExcludeCasterAuraType))
						return SpellCastResult.CasterAurastate;

					if (reqCombat &&
					    unitCaster.IsInCombat() &&
					    !_spellInfo.CanBeUsedInCombat())
						return SpellCastResult.AffectingCombat;
				}

				// Check vehicle flags
				if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
				{
					SpellCastResult vehicleCheck = _spellInfo.CheckVehicle(unitCaster);

					if (vehicleCheck != SpellCastResult.SpellCastOk)
						return vehicleCheck;
				}
			}

			// check spell cast conditions from database
			{
				ConditionSourceInfo condInfo = new(_caster, _targets.GetObjectTarget());

				if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Spell, _spellInfo.Id, condInfo))
				{
					// mLastFailedCondition can be NULL if there was an error processing the condition in Condition.Meets (i.e. wrong data for ConditionTarget or others)
					if (condInfo.mLastFailedCondition != null &&
					    condInfo.mLastFailedCondition.ErrorType != 0)
					{
						if (condInfo.mLastFailedCondition.ErrorType == (uint)SpellCastResult.CustomError)
							_customError = (SpellCustomErrors)condInfo.mLastFailedCondition.ErrorTextId;

						return (SpellCastResult)condInfo.mLastFailedCondition.ErrorType;
					}

					if (condInfo.mLastFailedCondition == null ||
					    condInfo.mLastFailedCondition.ConditionTarget == 0)
						return SpellCastResult.CasterAurastate;

					return SpellCastResult.BadTargets;
				}
			}

			// Don't check explicit target for passive spells (workaround) (check should be skipped only for learn case)
			// those spells may have incorrect target entries or not filled at all (for example 15332)
			// such spells when learned are not targeting anyone using targeting system, they should apply directly to caster instead
			// also, such casts shouldn't be sent to client
			if (!(_spellInfo.IsPassive() && (_targets.GetUnitTarget() == null || _targets.GetUnitTarget() == _caster)))
			{
				// Check explicit target for _originalCaster - todo: get rid of such workarounds
				WorldObject caster = _caster;

				// in case of gameobjects like traps, we need the gameobject itself to check target validity
				// otherwise, if originalCaster is far away and cannot detect the target, the trap would not hit the target
				if (_originalCaster != null &&
				    !caster.IsGameObject())
					caster = _originalCaster;

				castResult = _spellInfo.CheckExplicitTarget(caster, _targets.GetObjectTarget(), _targets.GetItemTarget());

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;
			}

			Unit unitTarget = _targets.GetUnitTarget();

			if (unitTarget != null)
			{
				castResult = _spellInfo.CheckTarget(_caster, unitTarget, _caster.IsGameObject()); // skip stealth checks for GO casts

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;

				// If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
				if (_spellInfo.DmgClass != SpellDmgClass.Melee)
				{
					Unit unitCaster1 = _caster.ToUnit();

					if (unitCaster1 != null)
					{
						foreach (var auraEffect in unitCaster1.GetAuraEffectsByType(AuraType.InterfereTargetting))
							if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) &&
							    !unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()))
								return SpellCastResult.VisionObscured;

						foreach (var auraEffect in unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting))
							if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) &&
							    (!unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()) || !unitCaster1.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID())))
								return SpellCastResult.VisionObscured;
					}
				}

				if (unitTarget != _caster)
				{
					// Must be behind the target
					if (_spellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) &&
					    unitTarget.HasInArc(MathFunctions.PI, _caster))
						return SpellCastResult.NotBehind;

					// Target must be facing you
					if (_spellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) &&
					    !unitTarget.HasInArc(MathFunctions.PI, _caster))
						return SpellCastResult.NotInfront;

					// Ignore LOS for gameobjects casts
					if (!_caster.IsGameObject())
					{
						WorldObject losTarget = _caster;

						if (IsTriggered() &&
						    _triggeredByAuraSpell != null)
						{
							DynamicObject dynObj = _caster.ToUnit().GetDynObject(_triggeredByAuraSpell.Id);

							if (dynObj)
								losTarget = dynObj;
						}

						if (!_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) &&
						    !Global.DisableMgr.IsDisabledFor(DisableType.Spell, _spellInfo.Id, null, (byte)DisableFlags.SpellLOS) &&
						    !unitTarget.IsWithinLOSInMap(losTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
							return SpellCastResult.LineOfSight;
					}
				}
			}

			// Check for line of sight for spells with dest
			if (_targets.HasDst())
			{
				float x, y, z;
				_targets.GetDstPos().GetPosition(out x, out y, out z);

				if (!_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) &&
				    !Global.DisableMgr.IsDisabledFor(DisableType.Spell, _spellInfo.Id, null, (byte)DisableFlags.SpellLOS) &&
				    !_caster.IsWithinLOS(x, y, z, LineOfSightChecks.All, ModelIgnoreFlags.M2))
					return SpellCastResult.LineOfSight;
			}

			// check pet presence
			if (unitCaster != null)
			{
				if (_spellInfo.HasAttribute(SpellAttr2.NoActivePets))
					if (!unitCaster.GetPetGUID().IsEmpty())
						return SpellCastResult.AlreadyHavePet;

				foreach (var spellEffectInfo in _spellInfo.GetEffects())
					if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitPet)
					{
						if (unitCaster.GetGuardianPet() == null)
						{
							if (_triggeredByAuraSpell != null) // not report pet not existence for triggered spells
								return SpellCastResult.DontReport;
							else
								return SpellCastResult.NoPet;
						}

						break;
					}
			}

			// Spell casted only on Battleground
			if (_spellInfo.HasAttribute(SpellAttr3.OnlyBattlegrounds))
				if (!_caster.GetMap().IsBattleground())
					return SpellCastResult.OnlyBattlegrounds;

			// do not allow spells to be cast in arenas or rated Battlegrounds
			Player player = _caster.ToPlayer();

			if (player != null)
				if (player.InArena() /* || player.InRatedBattleground() NYI*/)
				{
					castResult = CheckArenaAndRatedBattlegroundCastRules();

					if (castResult != SpellCastResult.SpellCastOk)
						return castResult;
				}

			// zone check
			if (!_caster.IsPlayer() ||
			    !_caster.ToPlayer().IsGameMaster())
			{
				uint zone, area;
				_caster.GetZoneAndAreaId(out zone, out area);

				SpellCastResult locRes = _spellInfo.CheckLocation(_caster.GetMapId(), zone, area, _caster.ToPlayer());

				if (locRes != SpellCastResult.SpellCastOk)
					return locRes;
			}

			// not let players cast spells at mount (and let do it to creatures)
			if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
				if (_caster.IsPlayer() &&
				    _caster.ToPlayer().IsMounted() &&
				    !_spellInfo.IsPassive() &&
				    !_spellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
				{
					if (_caster.ToPlayer().IsInFlight())
						return SpellCastResult.NotOnTaxi;
					else
						return SpellCastResult.NotMounted;
				}

			// check spell focus object
			if (_spellInfo.RequiresSpellFocus != 0)
				if (!_caster.IsUnit() ||
				    !_caster.ToUnit().HasAuraTypeWithMiscvalue(AuraType.ProvideSpellFocus, (int)_spellInfo.RequiresSpellFocus))
				{
					focusObject = SearchSpellFocus();

					if (!focusObject)
						return SpellCastResult.RequiresSpellFocus;
				}

			// always (except passive spells) check items (focus object can be required for any type casts)
			if (!_spellInfo.IsPassive())
			{
				castResult = CheckItems(ref param1, ref param2);

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;
			}

			// Triggered spells also have range check
			// @todo determine if there is some flag to enable/disable the check
			castResult = CheckRange(strict);

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;

			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
			{
				castResult = CheckPower();

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;
			}

			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAuras))
			{
				castResult = CheckCasterAuras(ref param1);

				if (castResult != SpellCastResult.SpellCastOk)
					return castResult;
			}

			// script hook
			castResult = CallScriptCheckCastHandlers();

			if (castResult != SpellCastResult.SpellCastOk)
				return castResult;

			uint approximateAuraEffectMask = 0;
			uint nonAuraEffectMask         = 0;

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				// for effects of spells that have only one target
				switch (spellEffectInfo.Effect)
				{
					case SpellEffectName.Dummy:
					{
						if (_spellInfo.Id == 19938) // Awaken Peon
						{
							Unit unit = _targets.GetUnitTarget();

							if (unit == null ||
							    !unit.HasAura(17743))
								return SpellCastResult.BadTargets;
						}
						else if (_spellInfo.Id == 31789) // Righteous Defense
						{
							if (!_caster.IsTypeId(TypeId.Player))
								return SpellCastResult.DontReport;

							Unit target = _targets.GetUnitTarget();

							if (target == null ||
							    !target.IsFriendlyTo(_caster) ||
							    target.GetAttackers().Empty())
								return SpellCastResult.BadTargets;
						}

						break;
					}
					case SpellEffectName.LearnSpell:
					{
						if (spellEffectInfo.TargetA.GetTarget() != Targets.UnitPet)
							break;

						Pet pet = _caster.ToPlayer().GetPet();

						if (pet == null)
							return SpellCastResult.NoPet;

						SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

						if (learn_spellproto == null)
							return SpellCastResult.NotKnown;

						if (_spellInfo.SpellLevel > pet.GetLevel())
							return SpellCastResult.Lowlevel;

						break;
					}
					case SpellEffectName.UnlockGuildVaultTab:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						var guild = _caster.ToPlayer().GetGuild();

						if (guild != null)
							if (guild.GetLeaderGUID() != _caster.ToPlayer().GetGUID())
								return SpellCastResult.CantDoThatRightNow;

						break;
					}
					case SpellEffectName.LearnPetSpell:
					{
						// check target only for unit target case
						Unit target = _targets.GetUnitTarget();

						if (target != null)
						{
							if (!_caster.IsTypeId(TypeId.Player))
								return SpellCastResult.BadTargets;

							Pet pet = target.ToPet();

							if (pet == null ||
							    pet.GetOwner() != _caster)
								return SpellCastResult.BadTargets;

							SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

							if (learn_spellproto == null)
								return SpellCastResult.NotKnown;

							if (_spellInfo.SpellLevel > pet.GetLevel())
								return SpellCastResult.Lowlevel;
						}

						break;
					}
					case SpellEffectName.ApplyGlyph:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.GlyphNoSpec;

						Player caster = _caster.ToPlayer();

						if (!caster.HasSpell(_misc.SpellId))
							return SpellCastResult.NotKnown;

						uint glyphId = (uint)spellEffectInfo.MiscValue;

						if (glyphId != 0)
						{
							GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);

							if (glyphProperties == null)
								return SpellCastResult.InvalidGlyph;

							List<uint> glyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);

							if (glyphBindableSpells.Empty())
								return SpellCastResult.InvalidGlyph;

							if (!glyphBindableSpells.Contains(_misc.SpellId))
								return SpellCastResult.InvalidGlyph;

							List<uint> glyphRequiredSpecs = Global.DB2Mgr.GetGlyphRequiredSpecs(glyphId);

							if (!glyphRequiredSpecs.Empty())
							{
								if (caster.GetPrimarySpecialization() == 0)
									return SpellCastResult.GlyphNoSpec;

								if (!glyphRequiredSpecs.Contains(caster.GetPrimarySpecialization()))
									return SpellCastResult.GlyphInvalidSpec;
							}

							uint replacedGlyph = 0;

							foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
							{
								List<uint> activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(activeGlyphId);

								if (!activeGlyphBindableSpells.Empty())
									if (activeGlyphBindableSpells.Contains(_misc.SpellId))
									{
										replacedGlyph = activeGlyphId;

										break;
									}
							}

							foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
							{
								if (activeGlyphId == replacedGlyph)
									continue;

								if (activeGlyphId == glyphId)
									return SpellCastResult.UniqueGlyph;

								if (CliDB.GlyphPropertiesStorage.LookupByKey(activeGlyphId).GlyphExclusiveCategoryID == glyphProperties.GlyphExclusiveCategoryID)
									return SpellCastResult.GlyphExclusiveCategory;
							}
						}

						break;
					}
					case SpellEffectName.FeedPet:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						Item foodItem = _targets.GetItemTarget();

						if (!foodItem)
							return SpellCastResult.BadTargets;

						Pet pet = _caster.ToPlayer().GetPet();

						if (!pet)
							return SpellCastResult.NoPet;

						if (!pet.HaveInDiet(foodItem.GetTemplate()))
							return SpellCastResult.WrongPetFood;

						if (foodItem.GetTemplate().GetBaseItemLevel() + 30 <= pet.GetLevel())
							return SpellCastResult.FoodLowlevel;

						if (_caster.ToPlayer().IsInCombat() ||
						    pet.IsInCombat())
							return SpellCastResult.AffectingCombat;

						break;
					}
					case SpellEffectName.Charge:
					{
						if (unitCaster == null)
							return SpellCastResult.BadTargets;

						if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) &&
						    unitCaster.HasUnitState(UnitState.Root))
							return SpellCastResult.Rooted;

						if (GetSpellInfo().NeedsExplicitUnitTarget())
						{
							Unit target = _targets.GetUnitTarget();

							if (target == null)
								return SpellCastResult.DontReport;

							// first we must check to see if the target is in LoS. A path can usually be built but LoS matters for charge spells
							if (!target.IsWithinLOSInMap(unitCaster)) //Do full LoS/Path check. Don't exclude m2
								return SpellCastResult.LineOfSight;

							float objSize = target.GetCombatReach();
							float range   = _spellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

							_preGeneratedPath = new PathGenerator(unitCaster);
							_preGeneratedPath.SetPathLengthLimit(range);

							// first try with raycast, if it fails fall back to normal path
							bool result = _preGeneratedPath.CalculatePath(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), false);

							if (_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
								return SpellCastResult.NoPath;
							else if (!result ||
							         _preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
								return SpellCastResult.NoPath;
							else if (_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
								return SpellCastResult.NoPath;

							_preGeneratedPath.ShortenPathUntilDist(target, objSize); //move back
						}

						break;
					}
					case SpellEffectName.Skinning:
					{
						if (!_caster.IsTypeId(TypeId.Player) ||
						    _targets.GetUnitTarget() == null ||
						    !_targets.GetUnitTarget().IsTypeId(TypeId.Unit))
							return SpellCastResult.BadTargets;

						if (!_targets.GetUnitTarget().HasUnitFlag(UnitFlags.Skinnable))
							return SpellCastResult.TargetUnskinnable;

						Creature creature = _targets.GetUnitTarget().ToCreature();
						Loot     loot     = creature.GetLootForPlayer(_caster.ToPlayer());

						if (loot != null &&
						    (!loot.IsLooted() || loot.loot_type == LootType.Skinning))
							return SpellCastResult.TargetNotLooted;

						SkillType skill = creature.GetCreatureTemplate().GetRequiredLootSkill();

						ushort skillValue  = _caster.ToPlayer().GetSkillValue(skill);
						uint   TargetLevel = _targets.GetUnitTarget().GetLevelForTarget(_caster);
						int    ReqValue    = (int)(skillValue < 100 ? (TargetLevel - 10) * 10 : TargetLevel * 5);

						if (ReqValue > skillValue)
							return SpellCastResult.LowCastlevel;

						break;
					}
					case SpellEffectName.OpenLock:
					{
						if (spellEffectInfo.TargetA.GetTarget() != Targets.GameobjectTarget &&
						    spellEffectInfo.TargetA.GetTarget() != Targets.GameobjectItemTarget)
							break;

						if (!_caster.IsTypeId(TypeId.Player) // only players can open locks, gather etc.
						    // we need a go target in case of TARGET_GAMEOBJECT_TARGET
						    ||
						    (spellEffectInfo.TargetA.GetTarget() == Targets.GameobjectTarget && _targets.GetGOTarget() == null))
							return SpellCastResult.BadTargets;

						Item pTempItem = null;

						if (Convert.ToBoolean(_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
						{
							TradeData pTrade = _caster.ToPlayer().GetTradeData();

							if (pTrade != null)
								pTempItem = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
						}
						else if (Convert.ToBoolean(_targets.GetTargetMask() & SpellCastTargetFlags.Item))
						{
							pTempItem = _caster.ToPlayer().GetItemByGuid(_targets.GetItemTargetGUID());
						}

						// we need a go target, or an openable item target in case of TARGET_GAMEOBJECT_ITEM_TARGET
						if (spellEffectInfo.TargetA.GetTarget() == Targets.GameobjectItemTarget &&
						    _targets.GetGOTarget() == null &&
						    (pTempItem == null || pTempItem.GetTemplate().GetLockID() == 0 || !pTempItem.IsLocked()))
							return SpellCastResult.BadTargets;

						if (_spellInfo.Id != 1842 ||
						    (_targets.GetGOTarget() != null &&
						     _targets.GetGOTarget().GetGoInfo().type != GameObjectTypes.Trap))
							if (_caster.ToPlayer().InBattleground() && // In Battlegroundplayers can use only flags and banners
							    !_caster.ToPlayer().CanUseBattlegroundObject(_targets.GetGOTarget()))
								return SpellCastResult.TryAgain;

						// get the lock entry
						uint       lockId = 0;
						GameObject go     = _targets.GetGOTarget();
						Item       itm    = _targets.GetItemTarget();

						if (go != null)
						{
							lockId = go.GetGoInfo().GetLockId();

							if (lockId == 0)
								return SpellCastResult.BadTargets;

							if (go.GetGoInfo().GetNotInCombat() != 0 &&
							    _caster.ToUnit().IsInCombat())
								return SpellCastResult.AffectingCombat;
						}
						else if (itm != null)
						{
							lockId = itm.GetTemplate().GetLockID();
						}

						SkillType skillId       = SkillType.None;
						int       reqSkillValue = 0;
						int       skillValue    = 0;

						// check lock compatibility
						SpellCastResult res = CanOpenLock(spellEffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

						if (res != SpellCastResult.SpellCastOk)
							return res;

						break;
					}
					case SpellEffectName.ResurrectPet:
					{
						Player playerCaster = _caster.ToPlayer();

						if (playerCaster == null ||
						    playerCaster.GetPetStable() == null)
							return SpellCastResult.BadTargets;

						Pet pet = playerCaster.GetPet();

						if (pet != null &&
						    pet.IsAlive())
							return SpellCastResult.AlreadyHaveSummon;

						PetStable petStable   = playerCaster.GetPetStable();
						var       deadPetInfo = petStable.ActivePets.FirstOrDefault(petInfo => petInfo?.Health == 0);

						if (deadPetInfo == null)
							return SpellCastResult.BadTargets;

						break;
					}
					// This is generic summon effect
					case SpellEffectName.Summon:
					{
						if (unitCaster == null)
							break;

						var SummonProperties = CliDB.SummonPropertiesStorage.LookupByKey(spellEffectInfo.MiscValueB);

						if (SummonProperties == null)
							break;

						switch (SummonProperties.Control)
						{
							case SummonCategory.Pet:
								if (!_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
								    !unitCaster.GetPetGUID().IsEmpty())
									return SpellCastResult.AlreadyHaveSummon;

								goto case SummonCategory.Puppet;
							case SummonCategory.Puppet:
								if (!unitCaster.GetCharmedGUID().IsEmpty())
									return SpellCastResult.AlreadyHaveCharm;

								break;
						}

						break;
					}
					case SpellEffectName.CreateTamedPet:
					{
						if (_targets.GetUnitTarget() != null)
						{
							if (!_targets.GetUnitTarget().IsTypeId(TypeId.Player))
								return SpellCastResult.BadTargets;

							if (!_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
							    !_targets.GetUnitTarget().GetPetGUID().IsEmpty())
								return SpellCastResult.AlreadyHaveSummon;
						}

						break;
					}
					case SpellEffectName.SummonPet:
					{
						if (unitCaster == null)
							return SpellCastResult.BadTargets;

						if (!unitCaster.GetPetGUID().IsEmpty()) //let warlock do a replacement summon
						{
							if (unitCaster.IsTypeId(TypeId.Player))
							{
								if (strict) //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
								{
									Pet pet = unitCaster.ToPlayer().GetPet();

									if (pet != null)
										pet.CastSpell(pet,
										              32752,
										              new CastSpellExtraArgs(TriggerCastFlags.FullMask)
											              .SetOriginalCaster(pet.GetGUID())
											              .SetTriggeringSpell(this));
								}
							}
							else if (!_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
							{
								return SpellCastResult.AlreadyHaveSummon;
							}
						}

						if (!unitCaster.GetCharmedGUID().IsEmpty())
							return SpellCastResult.AlreadyHaveCharm;

						Player playerCaster = unitCaster.ToPlayer();

						if (playerCaster != null &&
						    playerCaster.GetPetStable() != null)
						{
							PetSaveMode? petSlot = null;

							if (spellEffectInfo.MiscValue == 0)
							{
								petSlot = (PetSaveMode)spellEffectInfo.CalcValue();

								// No pet can be summoned if any pet is dead
								foreach (var activePet in playerCaster.GetPetStable().ActivePets)
									if (activePet?.Health == 0)
									{
										playerCaster.SendTameFailure(PetTameResult.Dead);

										return SpellCastResult.DontReport;
									}
							}

							var info = Pet.GetLoadPetInfo(playerCaster.GetPetStable(), (uint)spellEffectInfo.MiscValue, 0, petSlot);

							if (info.Item1 != null)
							{
								if (info.Item1.Type == PetType.Hunter)
								{
									CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(info.Item1.CreatureId);

									if (creatureInfo == null ||
									    !creatureInfo.IsTameable(playerCaster.CanTameExoticPets()))
									{
										// if problem in exotic pet
										if (creatureInfo != null &&
										    creatureInfo.IsTameable(true))
											playerCaster.SendTameFailure(PetTameResult.CantControlExotic);
										else
											playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

										return SpellCastResult.DontReport;
									}
								}
							}
							else if (spellEffectInfo.MiscValue == 0) // when miscvalue is present it is allowed to create new pets
							{
								playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

								return SpellCastResult.DontReport;
							}
						}

						break;
					}
					case SpellEffectName.DismissPet:
					{
						Player playerCaster = _caster.ToPlayer();

						if (playerCaster == null)
							return SpellCastResult.BadTargets;

						Pet pet = playerCaster.GetPet();

						if (pet == null)
							return SpellCastResult.NoPet;

						if (!pet.IsAlive())
							return SpellCastResult.TargetsDead;

						break;
					}
					case SpellEffectName.SummonPlayer:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						if (_caster.ToPlayer().GetTarget().IsEmpty())
							return SpellCastResult.BadTargets;

						Player target = Global.ObjAccessor.FindPlayer(_caster.ToPlayer().GetTarget());

						if (target == null ||
						    _caster.ToPlayer() == target ||
						    (!target.IsInSameRaidWith(_caster.ToPlayer()) && _spellInfo.Id != 48955)) // refer-a-friend spell
							return SpellCastResult.BadTargets;

						if (target.HasSummonPending())
							return SpellCastResult.SummonPending;

						// check if our map is dungeon
						InstanceMap map = _caster.GetMap().ToInstanceMap();

						if (map != null)
						{
							uint         mapId      = map.GetId();
							Difficulty   difficulty = map.GetDifficultyID();
							InstanceLock mapLock    = map.GetInstanceLock();

							if (mapLock != null)
								if (Global.InstanceLockMgr.CanJoinInstanceLock(target.GetGUID(), new MapDb2Entries(mapId, difficulty), mapLock) != TransferAbortReason.None)
									return SpellCastResult.TargetLockedToRaidInstance;

							if (!target.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapId, difficulty), mapId))
								return SpellCastResult.BadTargets;
						}

						break;
					}
					// RETURN HERE
					case SpellEffectName.SummonRafFriend:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.BadTargets;

						Player playerCaster = _caster.ToPlayer();

						//
						if (playerCaster.GetTarget().IsEmpty())
							return SpellCastResult.BadTargets;

						Player target = Global.ObjAccessor.FindPlayer(playerCaster.GetTarget());

						if (target == null ||
						    !(target.GetSession().GetRecruiterId() == playerCaster.GetSession().GetAccountId() || target.GetSession().GetAccountId() == playerCaster.GetSession().GetRecruiterId()))
							return SpellCastResult.BadTargets;

						break;
					}
					case SpellEffectName.Leap:
					case SpellEffectName.TeleportUnitsFaceCaster:
					{
						//Do not allow to cast it before BG starts.
						if (_caster.IsTypeId(TypeId.Player))
						{
							Battleground bg = _caster.ToPlayer().GetBattleground();

							if (bg)
								if (bg.GetStatus() != BattlegroundStatus.InProgress)
									return SpellCastResult.TryAgain;
						}

						break;
					}
					case SpellEffectName.StealBeneficialBuff:
					{
						if (_targets.GetUnitTarget() == null ||
						    _targets.GetUnitTarget() == _caster)
							return SpellCastResult.BadTargets;

						break;
					}
					case SpellEffectName.LeapBack:
					{
						if (unitCaster == null)
							return SpellCastResult.BadTargets;

						if (unitCaster.HasUnitState(UnitState.Root))
						{
							if (unitCaster.IsTypeId(TypeId.Player))
								return SpellCastResult.Rooted;
							else
								return SpellCastResult.DontReport;
						}

						break;
					}
					case SpellEffectName.Jump:
					case SpellEffectName.JumpDest:
					{
						if (unitCaster == null)
							return SpellCastResult.BadTargets;

						if (unitCaster.HasUnitState(UnitState.Root))
							return SpellCastResult.Rooted;

						break;
					}
					case SpellEffectName.TalentSpecSelect:
					{
						ChrSpecializationRecord spec         = CliDB.ChrSpecializationStorage.LookupByKey(_misc.SpecializationId);
						Player                  playerCaster = _caster.ToPlayer();

						if (!playerCaster)
							return SpellCastResult.TargetNotPlayer;

						if (spec == null ||
						    (spec.ClassID != (uint)player.GetClass() && !spec.IsPetSpecialization()))
							return SpellCastResult.NoSpec;

						if (spec.IsPetSpecialization())
						{
							Pet pet = player.GetPet();

							if (!pet ||
							    pet.GetPetType() != PetType.Hunter ||
							    pet.GetCharmInfo() == null)
								return SpellCastResult.NoPet;
						}

						// can't change during already started arena/Battleground
						Battleground bg = player.GetBattleground();

						if (bg)
							if (bg.GetStatus() == BattlegroundStatus.InProgress)
								return SpellCastResult.NotInBattleground;

						break;
					}
					case SpellEffectName.RemoveTalent:
					{
						Player playerCaster = _caster.ToPlayer();

						if (playerCaster == null)
							return SpellCastResult.BadTargets;

						TalentRecord talent = CliDB.TalentStorage.LookupByKey(_misc.TalentId);

						if (talent == null)
							return SpellCastResult.DontReport;

						if (playerCaster.GetSpellHistory().HasCooldown(talent.SpellID))
						{
							param1 = (int)talent.SpellID;

							return SpellCastResult.CantUntalent;
						}

						break;
					}
					case SpellEffectName.GiveArtifactPower:
					case SpellEffectName.GiveArtifactPowerNoBonus:
					{
						Player playerCaster = _caster.ToPlayer();

						if (playerCaster == null)
							return SpellCastResult.BadTargets;

						Aura artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

						if (artifactAura == null)
							return SpellCastResult.NoArtifactEquipped;

						Item artifact = playerCaster.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());

						if (artifact == null)
							return SpellCastResult.NoArtifactEquipped;

						if (spellEffectInfo.Effect == SpellEffectName.GiveArtifactPower)
						{
							ArtifactRecord artifactEntry = CliDB.ArtifactStorage.LookupByKey(artifact.GetTemplate().GetArtifactID());

							if (artifactEntry == null ||
							    artifactEntry.ArtifactCategoryID != spellEffectInfo.MiscValue)
								return SpellCastResult.WrongArtifactEquipped;
						}

						break;
					}
					case SpellEffectName.ChangeBattlepetQuality:
					case SpellEffectName.GrantBattlepetLevel:
					case SpellEffectName.GrantBattlepetExperience:
					{
						Player playerCaster = _caster.ToPlayer();

						if (playerCaster == null ||
						    !_targets.GetUnitTarget() ||
						    !_targets.GetUnitTarget().IsCreature())
							return SpellCastResult.BadTargets;

						var battlePetMgr = playerCaster.GetSession().GetBattlePetMgr();

						if (!battlePetMgr.HasJournalLock())
							return SpellCastResult.CantDoThatRightNow;

						Creature creature = _targets.GetUnitTarget().ToCreature();

						if (creature != null)
						{
							if (playerCaster.GetSummonedBattlePetGUID().IsEmpty() ||
							    creature.GetBattlePetCompanionGUID().IsEmpty())
								return SpellCastResult.NoPet;

							if (playerCaster.GetSummonedBattlePetGUID() != creature.GetBattlePetCompanionGUID())
								return SpellCastResult.BadTargets;

							var battlePet = battlePetMgr.GetPet(creature.GetBattlePetCompanionGUID());

							if (battlePet != null)
							{
								var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePet.PacketInfo.Species);

								if (battlePetSpecies != null)
								{
									uint battlePetType = (uint)spellEffectInfo.MiscValue;

									if (battlePetType != 0)
										if ((battlePetType & (1 << battlePetSpecies.PetTypeEnum)) == 0)
											return SpellCastResult.WrongBattlePetType;

									if (spellEffectInfo.Effect == SpellEffectName.ChangeBattlepetQuality)
									{
										var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < spellEffectInfo.BasePoints);

										BattlePetBreedQuality quality = BattlePetBreedQuality.Poor;

										if (qualityRecord != null)
											quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

										if (battlePet.PacketInfo.Quality >= (byte)quality)
											return SpellCastResult.CantUpgradeBattlePet;
									}

									if (spellEffectInfo.Effect == SpellEffectName.GrantBattlepetLevel ||
									    spellEffectInfo.Effect == SpellEffectName.GrantBattlepetExperience)
										if (battlePet.PacketInfo.Level >= SharedConst.MaxBattlePetLevel)
											return SpellCastResult.GrantPetLevelFail;

									if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
										return SpellCastResult.BadTargets;
								}
							}
						}

						break;
					}
					default:
						break;
				}

				if (spellEffectInfo.IsAura())
					approximateAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
				else if (spellEffectInfo.IsEffect())
					nonAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
			}

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				switch (spellEffectInfo.ApplyAuraName)
				{
					case AuraType.ModPossessPet:
					{
						if (!_caster.IsTypeId(TypeId.Player))
							return SpellCastResult.NoPet;

						Pet pet = _caster.ToPlayer().GetPet();

						if (pet == null)
							return SpellCastResult.NoPet;

						if (!pet.GetCharmerGUID().IsEmpty())
							return SpellCastResult.AlreadyHaveCharm;

						break;
					}
					case AuraType.ModPossess:
					case AuraType.ModCharm:
					case AuraType.AoeCharm:
					{
						Unit unitCaster1 = (_originalCaster ? _originalCaster : _caster.ToUnit());

						if (unitCaster1 == null)
							return SpellCastResult.BadTargets;

						if (!unitCaster1.GetCharmerGUID().IsEmpty())
							return SpellCastResult.AlreadyHaveCharm;

						if (spellEffectInfo.ApplyAuraName == AuraType.ModCharm ||
						    spellEffectInfo.ApplyAuraName == AuraType.ModPossess)
						{
							if (!_spellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
							    !unitCaster1.GetPetGUID().IsEmpty())
								return SpellCastResult.AlreadyHaveSummon;

							if (!unitCaster1.GetCharmedGUID().IsEmpty())
								return SpellCastResult.AlreadyHaveCharm;
						}

						Unit target = _targets.GetUnitTarget();

						if (target != null)
						{
							if (target.IsTypeId(TypeId.Unit) &&
							    target.ToCreature().IsVehicle())
								return SpellCastResult.BadImplicitTargets;

							if (target.IsMounted())
								return SpellCastResult.CantBeCharmed;

							if (!target.GetCharmerGUID().IsEmpty())
								return SpellCastResult.Charmed;

							if (target.GetOwner() != null &&
							    target.GetOwner().IsTypeId(TypeId.Player))
								return SpellCastResult.TargetIsPlayerControlled;

							int damage = CalculateDamage(spellEffectInfo, target);

							if (damage != 0 &&
							    target.GetLevelForTarget(_caster) > damage)
								return SpellCastResult.Highlevel;
						}

						break;
					}
					case AuraType.Mounted:
					{
						if (unitCaster == null)
							return SpellCastResult.BadTargets;

						if (unitCaster.IsInWater() &&
						    _spellInfo.HasAura(AuraType.ModIncreaseMountedFlightSpeed))
							return SpellCastResult.OnlyAbovewater;

						if (unitCaster.IsInDisallowedMountForm())
						{
							SendMountResult(MountResult.Shapeshifted); // mount result gets sent before the cast result

							return SpellCastResult.DontReport;
						}

						break;
					}
					case AuraType.RangedAttackPowerAttackerBonus:
					{
						if (_targets.GetUnitTarget() == null)
							return SpellCastResult.BadImplicitTargets;

						// can be casted at non-friendly unit or own pet/charm
						if (_caster.IsFriendlyTo(_targets.GetUnitTarget()))
							return SpellCastResult.TargetFriendly;

						break;
					}
					case AuraType.Fly:
					case AuraType.ModIncreaseFlightSpeed:
					{
						// not allow cast fly spells if not have req. skills  (all spells is self target)
						// allow always ghost flight spells
						if (_originalCaster != null &&
						    _originalCaster.IsTypeId(TypeId.Player) &&
						    _originalCaster.IsAlive())
						{
							BattleField Bf   = Global.BattleFieldMgr.GetBattlefieldToZoneId(_originalCaster.GetMap(), _originalCaster.GetZoneId());
							var         area = CliDB.AreaTableStorage.LookupByKey(_originalCaster.GetAreaId());

							if (area != null)
								if (area.HasFlag(AreaFlags.NoFlyZone) ||
								    (Bf != null && !Bf.CanFlyIn()))
									return SpellCastResult.NotHere;
						}

						break;
					}
					case AuraType.PeriodicManaLeech:
					{
						if (spellEffectInfo.IsTargetingArea())
							break;

						if (_targets.GetUnitTarget() == null)
							return SpellCastResult.BadImplicitTargets;

						if (!_caster.IsTypeId(TypeId.Player) ||
						    _CastItem != null)
							break;

						if (_targets.GetUnitTarget().GetPowerType() != PowerType.Mana)
							return SpellCastResult.BadTargets;

						break;
					}
					default:
						break;
				}

				// check if target already has the same type, but more powerful aura
				if (!_spellInfo.HasAttribute(SpellAttr4.AuraNeverBounces) &&
				    (nonAuraEffectMask == 0 || _spellInfo.HasAttribute(SpellAttr4.AuraBounceFailsSpell)) &&
				    (approximateAuraEffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
				    !_spellInfo.IsTargetingArea())
				{
					Unit target = _targets.GetUnitTarget();

					if (target != null)
						if (!target.IsHighestExclusiveAuraEffect(_spellInfo, spellEffectInfo.ApplyAuraName, spellEffectInfo.CalcValue(_caster, _spellValue.EffectBasePoints[spellEffectInfo.EffectIndex], null, _castItemEntry, _castItemLevel), approximateAuraEffectMask, false))
							return SpellCastResult.AuraBounced;
				}
			}

			// check trade slot case (last, for allow catch any another cast problems)
			if (Convert.ToBoolean(_targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
			{
				if (_CastItem != null)
					return SpellCastResult.ItemEnchantTradeWindow;

				if (_spellInfo.HasAttribute(SpellAttr2.EnchantOwnItemOnly))
					return SpellCastResult.ItemEnchantTradeWindow;

				if (!_caster.IsTypeId(TypeId.Player))
					return SpellCastResult.NotTrading;

				TradeData my_trade = _caster.ToPlayer().GetTradeData();

				if (my_trade == null)
					return SpellCastResult.NotTrading;

				TradeSlots slot = (TradeSlots)_targets.GetItemTargetGUID().GetLowValue();

				if (slot != TradeSlots.NonTraded)
					return SpellCastResult.BadTargets;

				if (!IsTriggered())
					if (my_trade.GetSpell() != 0)
						return SpellCastResult.ItemAlreadyEnchanted;
			}

			// check if caster has at least 1 combo point for spells that require combo points
			if (_needComboPoints)
			{
				Player plrCaster = _caster.ToPlayer();

				if (plrCaster != null)
					if (plrCaster.GetComboPoints() == 0)
						return SpellCastResult.NoComboPoints;
			}

			// all ok
			return SpellCastResult.SpellCastOk;
		}

		public SpellCastResult CheckPetCast(Unit target)
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster != null &&
			    unitCaster.HasUnitState(UnitState.Casting) &&
			    !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress)) //prevent spellcast interruption by another spellcast
				return SpellCastResult.SpellInProgress;

			// dead owner (pets still alive when owners ressed?)
			Unit owner = _caster.GetCharmerOrOwner();

			if (owner != null)
				if (!owner.IsAlive())
					return SpellCastResult.CasterDead;

			if (target == null &&
			    _targets.GetUnitTarget() != null)
				target = _targets.GetUnitTarget();

			if (_spellInfo.NeedsExplicitUnitTarget())
			{
				if (target == null)
					return SpellCastResult.BadImplicitTargets;

				_targets.SetUnitTarget(target);
			}

			// cooldown
			Creature creatureCaster = _caster.ToCreature();

			if (creatureCaster)
				if (creatureCaster.GetSpellHistory().HasCooldown(_spellInfo.Id))
					return SpellCastResult.NotReady;

			// Check if spell is affected by GCD
			if (_spellInfo.StartRecoveryCategory > 0)
				if (unitCaster.GetCharmInfo() != null &&
				    unitCaster.GetSpellHistory().HasGlobalCooldown(_spellInfo))
					return SpellCastResult.NotReady;

			return CheckCast(true);
		}

		private SpellCastResult CheckCasterAuras(ref int param1)
		{
			Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

			if (unitCaster == null)
				return SpellCastResult.SpellCastOk;

			// these attributes only show the spell as usable on the client when it has related aura applied
			// still they need to be checked against certain mechanics

			// SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
			bool usableWhileStunned = _spellInfo.HasAttribute(SpellAttr5.AllowWhileStunned);

			// SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
			bool usableWhileFeared = _spellInfo.HasAttribute(SpellAttr5.AllowWhileFleeing);

			// SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
			bool usableWhileConfused = _spellInfo.HasAttribute(SpellAttr5.AllowWhileConfused);

			// Check whether the cast should be prevented by any state you might have.
			SpellCastResult result = SpellCastResult.SpellCastOk;
			// Get unit state
			UnitFlags unitflag = (UnitFlags)(uint)unitCaster._unitData.Flags;

			// this check should only be done when player does cast directly
			// (ie not when it's called from a script) Breaks for example PlayerAI when charmed
			/*if (!unitCaster.GetCharmerGUID().IsEmpty())
			{
			    Unit charmer = unitCaster.GetCharmer();
			    if (charmer)
			        if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref param1))
			            result = SpellCastResult.Charmed;
			}*/

			// spell has attribute usable while having a cc state, check if caster has allowed mechanic auras, another mechanic types must prevent cast spell
			SpellCastResult mechanicCheck(AuraType auraType, ref int _param1)
			{
				bool foundNotMechanic = false;
				var  auras            = unitCaster.GetAuraEffectsByType(auraType);

				foreach (AuraEffect aurEff in auras)
				{
					ulong mechanicMask = aurEff.GetSpellInfo().GetAllEffectsMechanicMask();

					if (mechanicMask != 0 &&
					    !Convert.ToBoolean(mechanicMask & GetSpellInfo().GetAllowedMechanicMask()))
					{
						foundNotMechanic = true;

						// fill up aura mechanic info to send client proper error message
						_param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

						if (_param1 == 0)
							_param1 = (int)aurEff.GetSpellInfo().Mechanic;

						break;
					}
				}

				if (foundNotMechanic)
					switch (auraType)
					{
						case AuraType.ModStun:
						case AuraType.ModStunDisableGravity:
							return SpellCastResult.Stunned;
						case AuraType.ModFear:
							return SpellCastResult.Fleeing;
						case AuraType.ModConfuse:
							return SpellCastResult.Confused;
						default:
							//ABORT();
							return SpellCastResult.NotKnown;
					}

				return SpellCastResult.SpellCastOk;
			}

			if (unitflag.HasAnyFlag(UnitFlags.Stunned))
			{
				if (usableWhileStunned)
				{
					SpellCastResult mechanicResult = mechanicCheck(AuraType.ModStun, ref param1);

					if (mechanicResult != SpellCastResult.SpellCastOk)
						result = mechanicResult;
				}
				else if (!CheckSpellCancelsStun(ref param1))
				{
					result = SpellCastResult.Stunned;
				}
				else if ((_spellInfo.Mechanic & Mechanics.ImmuneShield) != 0 &&
				         _caster.IsUnit() &&
				         _caster.ToUnit().HasAuraWithMechanic(1 << (int)Mechanics.Banish))
				{
					result = SpellCastResult.Stunned;
				}
			}
			else if (unitflag.HasAnyFlag(UnitFlags.Silenced) &&
			         _spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) &&
			         !CheckSpellCancelsSilence(ref param1))
			{
				result = SpellCastResult.Silenced;
			}
			else if (unitflag.HasAnyFlag(UnitFlags.Pacified) &&
			         _spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) &&
			         !CheckSpellCancelsPacify(ref param1))
			{
				result = SpellCastResult.Pacified;
			}
			else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
			{
				if (usableWhileFeared)
				{
					SpellCastResult mechanicResult = mechanicCheck(AuraType.ModFear, ref param1);

					if (mechanicResult != SpellCastResult.SpellCastOk)
					{
						result = mechanicResult;
					}
					else
					{
						mechanicResult = mechanicCheck(AuraType.ModStunDisableGravity, ref param1);

						if (mechanicResult != SpellCastResult.SpellCastOk)
							result = mechanicResult;
					}
				}
				else if (!CheckSpellCancelsFear(ref param1))
				{
					result = SpellCastResult.Fleeing;
				}
			}
			else if (unitflag.HasAnyFlag(UnitFlags.Confused))
			{
				if (usableWhileConfused)
				{
					SpellCastResult mechanicResult = mechanicCheck(AuraType.ModConfuse, ref param1);

					if (mechanicResult != SpellCastResult.SpellCastOk)
						result = mechanicResult;
				}
				else if (!CheckSpellCancelsConfuse(ref param1))
				{
					result = SpellCastResult.Confused;
				}
			}
			else if (unitCaster.HasUnitFlag2(UnitFlags2.NoActions) &&
			         _spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions) &&
			         !CheckSpellCancelsNoActions(ref param1))
			{
				result = SpellCastResult.NoActions;
			}

			// Attr must make flag drop spell totally immune from all effects
			if (result != SpellCastResult.SpellCastOk)
				return (param1 != 0) ? SpellCastResult.PreventedByMechanic : result;

			return SpellCastResult.SpellCastOk;
		}

		private bool CheckSpellCancelsAuraEffect(AuraType auraType, ref int param1)
		{
			Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

			if (unitCaster == null)
				return false;

			// Checking auras is needed now, because you are prevented by some state but the spell grants immunity.
			var auraEffects = unitCaster.GetAuraEffectsByType(auraType);

			if (auraEffects.Empty())
				return true;

			foreach (AuraEffect aurEff in auraEffects)
			{
				if (_spellInfo.SpellCancelsAuraEffect(aurEff))
					continue;

				param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

				if (param1 == 0)
					param1 = (int)aurEff.GetSpellInfo().Mechanic;

				return false;
			}

			return true;
		}

		private bool CheckSpellCancelsCharm(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModCharm, ref param1) ||
			       CheckSpellCancelsAuraEffect(AuraType.AoeCharm, ref param1) ||
			       CheckSpellCancelsAuraEffect(AuraType.ModPossess, ref param1);
		}

		private bool CheckSpellCancelsStun(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
			       CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
		}

		private bool CheckSpellCancelsSilence(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
			       CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
		}

		private bool CheckSpellCancelsPacify(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
			       CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
		}

		private bool CheckSpellCancelsFear(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
		}

		private bool CheckSpellCancelsConfuse(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
		}

		private bool CheckSpellCancelsNoActions(ref int param1)
		{
			return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
		}

		private SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
		{
			bool isRatedBattleground = false; // NYI
			bool isArena             = !isRatedBattleground;

			// check USABLE attributes
			// USABLE takes precedence over NOT_USABLE
			if (isRatedBattleground && _spellInfo.HasAttribute(SpellAttr9.UsableInRatedBattlegrounds))
				return SpellCastResult.SpellCastOk;

			if (isArena && _spellInfo.HasAttribute(SpellAttr4.IgnoreDefaultArenaRestrictions))
				return SpellCastResult.SpellCastOk;

			// check NOT_USABLE attributes
			if (_spellInfo.HasAttribute(SpellAttr4.NotInArenaOrRatedBattleground))
				return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

			if (isArena && _spellInfo.HasAttribute(SpellAttr9.NotUsableInArena))
				return SpellCastResult.NotInArena;

			// check cooldowns
			uint spellCooldown = _spellInfo.GetRecoveryTime();

			if (isArena && spellCooldown > 10 * Time.Minute * Time.InMilliseconds) // not sure if still needed
				return SpellCastResult.NotInArena;

			if (isRatedBattleground && spellCooldown > 15 * Time.Minute * Time.InMilliseconds)
				return SpellCastResult.NotInBattleground;

			return SpellCastResult.SpellCastOk;
		}

		public bool CanAutoCast(Unit target)
		{
			if (!target)
				return (CheckPetCast(target) == SpellCastResult.SpellCastOk);

			ObjectGuid targetguid = target.GetGUID();

			// check if target already has the same or a more powerful aura
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				if (!spellEffectInfo.IsAura())
					continue;

				AuraType auraType = spellEffectInfo.ApplyAuraName;
				var      auras    = target.GetAuraEffectsByType(auraType);

				foreach (var eff in auras)
				{
					if (GetSpellInfo().Id == eff.GetSpellInfo().Id)
						return false;

					switch (Global.SpellMgr.CheckSpellGroupStackRules(GetSpellInfo(), eff.GetSpellInfo()))
					{
						case SpellGroupStackRule.Exclusive:
							return false;
						case SpellGroupStackRule.ExclusiveFromSameCaster:
							if (GetCaster() == eff.GetCaster())
								return false;

							break;
						case SpellGroupStackRule.ExclusiveSameEffect: // this one has further checks, but i don't think they're necessary for autocast logic
						case SpellGroupStackRule.ExclusiveHighest:
							if (Math.Abs(spellEffectInfo.BasePoints) <= Math.Abs(eff.GetAmount()))
								return false;

							break;
						case SpellGroupStackRule.Default:
						default:
							break;
					}
				}
			}

			SpellCastResult result = CheckPetCast(target);

			if (result == SpellCastResult.SpellCastOk ||
			    result == SpellCastResult.UnitNotInfront)
			{
				// do not check targets for ground-targeted spells (we target them on top of the intended target anyway)
				if (GetSpellInfo().ExplicitTargetMask.HasAnyFlag((uint)SpellCastTargetFlags.DestLocation))
					return true;

				SelectSpellTargets();

				//check if among target units, our WANTED target is as well (.only self cast spells return false)
				foreach (var ihit in _UniqueTargetInfo)
					if (ihit.TargetGUID == targetguid)
						return true;
			}

			// either the cast failed or the intended target wouldn't be hit
			return false;
		}

		private SpellCastResult CheckRange(bool strict)
		{
			// Don't check for instant cast spells
			if (!strict &&
			    _casttime == 0)
				return SpellCastResult.SpellCastOk;

			(float minRange, float maxRange) = GetMinMaxRange(strict);

			// dont check max_range to strictly after cast
			if (_spellInfo.RangeEntry != null &&
			    _spellInfo.RangeEntry.Flags != SpellRangeFlag.Melee &&
			    !strict)
				maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

			// get square values for sqr distance checks
			minRange *= minRange;
			maxRange *= maxRange;

			Unit target = _targets.GetUnitTarget();

			if (target && target != _caster)
			{
				if (_caster.GetExactDistSq(target) > maxRange)
					return SpellCastResult.OutOfRange;

				if (minRange > 0.0f &&
				    _caster.GetExactDistSq(target) < minRange)
					return SpellCastResult.OutOfRange;

				if (_caster.IsTypeId(TypeId.Player) &&
				    ((_spellInfo.FacingCasterFlags.HasAnyFlag(1u) && !_caster.HasInArc((float)Math.PI, target)) && !_caster.ToPlayer().IsWithinBoundaryRadius(target)))
					return SpellCastResult.UnitNotInfront;
			}

			GameObject goTarget = _targets.GetGOTarget();

			if (goTarget != null)
				if (!goTarget.IsAtInteractDistance(_caster.ToPlayer(), _spellInfo))
					return SpellCastResult.OutOfRange;

			if (_targets.HasDst() &&
			    !_targets.HasTraj())
			{
				if (_caster.GetExactDistSq(_targets.GetDstPos()) > maxRange)
					return SpellCastResult.OutOfRange;

				if (minRange > 0.0f &&
				    _caster.GetExactDistSq(_targets.GetDstPos()) < minRange)
					return SpellCastResult.OutOfRange;
			}

			return SpellCastResult.SpellCastOk;
		}

		private (float minRange, float maxRange) GetMinMaxRange(bool strict)
		{
			float rangeMod = 0.0f;
			float minRange = 0.0f;
			float maxRange = 0.0f;

			if (strict && _spellInfo.IsNextMeleeSwingSpell())
				return (0.0f, 100.0f);

			Unit unitCaster = _caster.ToUnit();

			if (_spellInfo.RangeEntry != null)
			{
				Unit target = _targets.GetUnitTarget();

				if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
				{
					// when the target is not a unit, take the caster's combat reach as the target's combat reach.
					if (unitCaster)
						rangeMod = unitCaster.GetMeleeRange(target ? target : unitCaster);
				}
				else
				{
					float meleeRange = 0.0f;

					if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
						// when the target is not a unit, take the caster's combat reach as the target's combat reach.
						if (unitCaster != null)
							meleeRange = unitCaster.GetMeleeRange(target ? target : unitCaster);

					minRange = _caster.GetSpellMinRangeForTarget(target, _spellInfo) + meleeRange;
					maxRange = _caster.GetSpellMaxRangeForTarget(target, _spellInfo);

					if (target || _targets.GetCorpseTarget())
					{
						rangeMod = _caster.GetCombatReach() + (target ? target.GetCombatReach() : _caster.GetCombatReach());

						if (minRange > 0.0f &&
						    !_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
							minRange += rangeMod;
					}
				}

				if (target != null &&
				    unitCaster != null &&
				    unitCaster.IsMoving() &&
				    target.IsMoving() &&
				    !unitCaster.IsWalking() &&
				    !target.IsWalking() &&
				    (_spellInfo.RangeEntry.Flags.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer()))
					rangeMod += 8.0f / 3.0f;
			}

			if (_spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) &&
			    _caster.IsTypeId(TypeId.Player))
			{
				Item ranged = _caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack, true);

				if (ranged)
					maxRange *= ranged.GetTemplate().GetRangedModRange() * 0.01f;
			}

			Player modOwner = _caster.GetSpellModOwner();

			if (modOwner)
				modOwner.ApplySpellMod(_spellInfo, SpellModOp.Range, ref maxRange, this);

			maxRange += rangeMod;

			return (minRange, maxRange);
		}

		private SpellCastResult CheckPower()
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return SpellCastResult.SpellCastOk;

			// item cast not used power
			if (_CastItem != null)
				return SpellCastResult.SpellCastOk;

			foreach (SpellPowerCost cost in _powerCost)
			{
				// health as power used - need check health amount
				if (cost.Power == PowerType.Health)
				{
					if (unitCaster.GetHealth() <= (ulong)cost.Amount)
						return SpellCastResult.CasterAurastate;

					continue;
				}

				// Check valid power type
				if (cost.Power >= PowerType.Max)
				{
					Log.outError(LogFilter.Spells, "Spell.CheckPower: Unknown power type '{0}'", cost.Power);

					return SpellCastResult.Unknown;
				}

				//check rune cost only if a spell has PowerType == POWER_RUNES
				if (cost.Power == PowerType.Runes)
				{
					SpellCastResult failReason = CheckRuneCost();

					if (failReason != SpellCastResult.SpellCastOk)
						return failReason;
				}

				// Check power amount
				if (unitCaster.GetPower(cost.Power) < cost.Amount)
					return SpellCastResult.NoPower;
			}

			return SpellCastResult.SpellCastOk;
		}

		private SpellCastResult CheckItems(ref int param1, ref int param2)
		{
			Player player = _caster.ToPlayer();

			if (!player)
				return SpellCastResult.SpellCastOk;

			if (_CastItem == null)
			{
				if (!_castItemGUID.IsEmpty())
					return SpellCastResult.ItemNotReady;
			}
			else
			{
				uint itemid = _CastItem.GetEntry();

				if (!player.HasItemCount(itemid))
					return SpellCastResult.ItemNotReady;

				ItemTemplate proto = _CastItem.GetTemplate();

				if (proto == null)
					return SpellCastResult.ItemNotReady;

				foreach (ItemEffectRecord itemEffect in _CastItem.GetEffects())
					if (itemEffect.LegacySlotIndex < _CastItem._itemData.SpellCharges.GetSize() &&
					    itemEffect.Charges != 0)
						if (_CastItem.GetSpellCharges(itemEffect.LegacySlotIndex) == 0)
							return SpellCastResult.NoChargesRemain;

				// consumable cast item checks
				if (proto.GetClass() == ItemClass.Consumable &&
				    _targets.GetUnitTarget() != null)
				{
					// such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
					SpellCastResult failReason = SpellCastResult.SpellCastOk;

					foreach (var spellEffectInfo in _spellInfo.GetEffects())
					{
						// skip check, pet not required like checks, and for TARGET_UNIT_PET _targets.GetUnitTarget() is not the real target but the caster
						if (spellEffectInfo.TargetA.GetTarget() == Targets.UnitPet)
							continue;

						if (spellEffectInfo.Effect == SpellEffectName.Heal)
						{
							if (_targets.GetUnitTarget().IsFullHealth())
							{
								failReason = SpellCastResult.AlreadyAtFullHealth;

								continue;
							}
							else
							{
								failReason = SpellCastResult.SpellCastOk;

								break;
							}
						}

						// Mana Potion, Rage Potion, Thistle Tea(Rogue), ...
						if (spellEffectInfo.Effect == SpellEffectName.Energize)
						{
							if (spellEffectInfo.MiscValue < 0 ||
							    spellEffectInfo.MiscValue >= (int)PowerType.Max)
							{
								failReason = SpellCastResult.AlreadyAtFullPower;

								continue;
							}

							PowerType power = (PowerType)spellEffectInfo.MiscValue;

							if (_targets.GetUnitTarget().GetPower(power) == _targets.GetUnitTarget().GetMaxPower(power))
							{
								failReason = SpellCastResult.AlreadyAtFullPower;

								continue;
							}
							else
							{
								failReason = SpellCastResult.SpellCastOk;

								break;
							}
						}
					}

					if (failReason != SpellCastResult.SpellCastOk)
						return failReason;
				}
			}

			// check target item
			if (!_targets.GetItemTargetGUID().IsEmpty())
			{
				Item item = _targets.GetItemTarget();

				if (item == null)
					return SpellCastResult.ItemGone;

				if (!item.IsFitToSpellRequirements(_spellInfo))
					return SpellCastResult.EquippedItemClass;
			}
			// if not item target then required item must be equipped
			else
			{
				if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement))
					if (_caster.IsTypeId(TypeId.Player) &&
					    !_caster.ToPlayer().HasItemFitToSpellRequirements(_spellInfo))
						return SpellCastResult.EquippedItemClass;
			}

			// do not take reagents for these item casts
			if (!(_CastItem != null && _CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost)))
			{
				bool checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost) && !player.CanNoReagentCast(_spellInfo);

				// Not own traded item (in trader trade slot) requires reagents even if triggered spell
				if (!checkReagents)
				{
					Item targetItem = _targets.GetItemTarget();

					if (targetItem != null)
						if (targetItem.GetOwnerGUID() != player.GetGUID())
							checkReagents = true;
				}

				// check reagents (ignore triggered spells with reagents processed by original spell) and special reagent ignore case.
				if (checkReagents)
				{
					for (byte i = 0; i < SpellConst.MaxReagents; i++)
					{
						if (_spellInfo.Reagent[i] <= 0)
							continue;

						uint itemid    = (uint)_spellInfo.Reagent[i];
						uint itemcount = _spellInfo.ReagentCount[i];

						// if CastItem is also spell reagent
						if (_CastItem != null &&
						    _CastItem.GetEntry() == itemid)
						{
							ItemTemplate proto = _CastItem.GetTemplate();

							if (proto == null)
								return SpellCastResult.ItemNotReady;

							foreach (ItemEffectRecord itemEffect in _CastItem.GetEffects())
							{
								if (itemEffect.LegacySlotIndex >= _CastItem._itemData.SpellCharges.GetSize())
									continue;

								// CastItem will be used up and does not count as reagent
								int charges = _CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

								if (itemEffect.Charges < 0 &&
								    Math.Abs(charges) < 2)
								{
									++itemcount;

									break;
								}
							}
						}

						if (!player.HasItemCount(itemid, itemcount))
						{
							param1 = (int)itemid;

							return SpellCastResult.Reagents;
						}
					}

					foreach (var reagentsCurrency in _spellInfo.ReagentsCurrency)
						if (!player.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
						{
							param1 = -1;
							param2 = reagentsCurrency.CurrencyTypesID;

							return SpellCastResult.Reagents;
						}
				}

				// check totem-item requirements (items presence in inventory)
				uint totems = 2;

				for (int i = 0; i < 2; ++i)
					if (_spellInfo.Totem[i] != 0)
					{
						if (player.HasItemCount(_spellInfo.Totem[i]))
						{
							totems -= 1;

							continue;
						}
					}
					else
					{
						totems -= 1;
					}

				if (totems != 0)
					return SpellCastResult.Totems;

				// Check items for TotemCategory (items presence in inventory)
				uint totemCategory = 2;

				for (byte i = 0; i < 2; ++i)
					if (_spellInfo.TotemCategory[i] != 0)
					{
						if (player.HasItemTotemCategory(_spellInfo.TotemCategory[i]))
						{
							totemCategory -= 1;

							continue;
						}
					}
					else
					{
						totemCategory -= 1;
					}

				if (totemCategory != 0)
					return SpellCastResult.TotemCategory;
			}

			// special checks for spell effects
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
				switch (spellEffectInfo.Effect)
				{
					case SpellEffectName.CreateItem:
					case SpellEffectName.CreateLoot:
					{
						// _targets.GetUnitTarget() means explicit cast, otherwise we dont check for possible equip error
						Unit target = _targets.GetUnitTarget() ?? player;

						if (target.IsPlayer() &&
						    !IsTriggered())
						{
							// SPELL_EFFECT_CREATE_ITEM_2 differs from SPELL_EFFECT_CREATE_ITEM in that it picks the random item to create from a pool of potential items,
							// so we need to make sure there is at least one free space in the player's inventory
							if (spellEffectInfo.Effect == SpellEffectName.CreateLoot)
								if (target.ToPlayer().GetFreeInventorySpace() == 0)
								{
									player.SendEquipError(InventoryResult.InvFull, null, null, spellEffectInfo.ItemType);

									return SpellCastResult.DontReport;
								}

							if (spellEffectInfo.ItemType != 0)
							{
								ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType);

								if (itemTemplate == null)
									return SpellCastResult.ItemNotFound;

								uint createCount = (uint)Math.Clamp(spellEffectInfo.CalcValue(), 1u, itemTemplate.GetMaxStackSize());

								List<ItemPosCount> dest = new();
								InventoryResult    msg  = target.ToPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, createCount);

								if (msg != InventoryResult.Ok)
								{
									/// @todo Needs review
									if (itemTemplate.GetItemLimitCategory() == 0)
									{
										player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

										return SpellCastResult.DontReport;
									}
									else
									{
										// Conjure Food/Water/Refreshment spells
										if (_spellInfo.SpellFamilyName != SpellFamilyNames.Mage ||
										    (!_spellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
										{
											return SpellCastResult.TooManyOfItem;
										}
										else if (!target.ToPlayer().HasItemCount(spellEffectInfo.ItemType))
										{
											player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

											return SpellCastResult.DontReport;
										}
										else if (_spellInfo.GetEffects().Count > 1)
										{
											player.CastSpell(player,
											                 (uint)_spellInfo.GetEffect(1).CalcValue(),
											                 new CastSpellExtraArgs()
												                 .SetTriggeringSpell(this)); // move this to anywhere
										}

										return SpellCastResult.DontReport;
									}
								}
							}
						}

						break;
					}
					case SpellEffectName.EnchantItem:
						if (spellEffectInfo.ItemType != 0 &&
						    _targets.GetItemTarget() != null &&
						    _targets.GetItemTarget().IsVellum())
						{
							// cannot enchant vellum for other player
							if (_targets.GetItemTarget().GetOwner() != player)
								return SpellCastResult.NotTradeable;

							// do not allow to enchant vellum from scroll made by vellum-prevent exploit
							if (_CastItem != null &&
							    _CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
								return SpellCastResult.TotemCategory;

							List<ItemPosCount> dest = new();
							InventoryResult    msg  = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);

							if (msg != InventoryResult.Ok)
							{
								player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

								return SpellCastResult.DontReport;
							}
						}

						goto case SpellEffectName.EnchantItemPrismatic;
					case SpellEffectName.EnchantItemPrismatic:
					{
						Item targetItem = _targets.GetItemTarget();

						if (targetItem == null)
							return SpellCastResult.ItemNotFound;

						// required level has to be checked also! Exploit fix
						if (targetItem.GetItemLevel(targetItem.GetOwner()) < _spellInfo.BaseLevel ||
						    (targetItem.GetRequiredLevel() != 0 && targetItem.GetRequiredLevel() < _spellInfo.BaseLevel))
							return SpellCastResult.Lowlevel;

						bool isItemUsable = false;

						foreach (ItemEffectRecord itemEffect in targetItem.GetEffects())
							if (itemEffect.SpellID != 0 &&
							    itemEffect.TriggerType == ItemSpelltriggerType.OnUse)
							{
								isItemUsable = true;

								break;
							}

						var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellEffectInfo.MiscValue);

						// do not allow adding usable enchantments to items that have use effect already
						if (enchantEntry != null)
							for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
								switch (enchantEntry.Effect[s])
								{
									case ItemEnchantmentType.UseSpell:
										if (isItemUsable)
											return SpellCastResult.OnUseEnchant;

										break;
									case ItemEnchantmentType.PrismaticSocket:
									{
										uint numSockets = 0;

										for (uint socket = 0; socket < ItemConst.MaxGemSockets; ++socket)
											if (targetItem.GetSocketColor(socket) != 0)
												++numSockets;

										if (numSockets == ItemConst.MaxGemSockets ||
										    targetItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
											return SpellCastResult.MaxSockets;

										break;
									}
								}

						// Not allow enchant in trade slot for some enchant type
						if (targetItem.GetOwner() != player)
						{
							if (enchantEntry == null)
								return SpellCastResult.Error;

							if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
								return SpellCastResult.NotTradeable;
						}

						break;
					}
					case SpellEffectName.EnchantItemTemporary:
					{
						Item item = _targets.GetItemTarget();

						if (item == null)
							return SpellCastResult.ItemNotFound;

						// Not allow enchant in trade slot for some enchant type
						if (item.GetOwner() != player)
						{
							int enchant_id   = spellEffectInfo.MiscValue;
							var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

							if (enchantEntry == null)
								return SpellCastResult.Error;

							if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
								return SpellCastResult.NotTradeable;
						}

						// Apply item level restriction if the enchanting spell has max level restrition set
						if (_CastItem != null &&
						    _spellInfo.MaxLevel > 0)
						{
							if (item.GetTemplate().GetBaseItemLevel() < _CastItem.GetTemplate().GetBaseRequiredLevel())
								return SpellCastResult.Lowlevel;

							if (item.GetTemplate().GetBaseItemLevel() > _spellInfo.MaxLevel)
								return SpellCastResult.Highlevel;
						}

						break;
					}
					case SpellEffectName.EnchantHeldItem:
						// check item existence in effect code (not output errors at offhand hold item effect to main hand for example
						break;
					case SpellEffectName.Disenchant:
					{
						Item item = _targets.GetItemTarget();

						if (!item)
							return SpellCastResult.CantBeSalvaged;

						// prevent disenchanting in trade slot
						if (item.GetOwnerGUID() != player.GetGUID())
							return SpellCastResult.CantBeSalvaged;

						ItemTemplate itemProto = item.GetTemplate();

						if (itemProto == null)
							return SpellCastResult.CantBeSalvaged;

						ItemDisenchantLootRecord itemDisenchantLoot = item.GetDisenchantLoot(_caster.ToPlayer());

						if (itemDisenchantLoot == null)
							return SpellCastResult.CantBeSalvaged;

						if (itemDisenchantLoot.SkillRequired > player.GetSkillValue(SkillType.Enchanting))
							return SpellCastResult.CantBeSalvagedSkill;

						break;
					}
					case SpellEffectName.Prospecting:
					{
						Item item = _targets.GetItemTarget();

						if (!item)
							return SpellCastResult.CantBeProspected;

						//ensure item is a prospectable ore
						if (!item.GetTemplate().HasFlag(ItemFlags.IsProspectable))
							return SpellCastResult.CantBeProspected;

						//prevent prospecting in trade slot
						if (item.GetOwnerGUID() != player.GetGUID())
							return SpellCastResult.CantBeProspected;

						//Check for enough skill in jewelcrafting
						uint item_prospectingskilllevel = item.GetTemplate().GetRequiredSkillRank();

						if (item_prospectingskilllevel > player.GetSkillValue(SkillType.Jewelcrafting))
							return SpellCastResult.LowCastlevel;

						//make sure the player has the required ores in inventory
						if (item.GetCount() < 5)
						{
							param1 = (int)item.GetEntry();
							param2 = 5;

							return SpellCastResult.NeedMoreItems;
						}

						if (!LootStorage.Prospecting.HaveLootFor(_targets.GetItemTargetEntry()))
							return SpellCastResult.CantBeProspected;

						break;
					}
					case SpellEffectName.Milling:
					{
						Item item = _targets.GetItemTarget();

						if (!item)
							return SpellCastResult.CantBeMilled;

						//ensure item is a millable herb
						if (!item.GetTemplate().HasFlag(ItemFlags.IsMillable))
							return SpellCastResult.CantBeMilled;

						//prevent milling in trade slot
						if (item.GetOwnerGUID() != player.GetGUID())
							return SpellCastResult.CantBeMilled;

						//Check for enough skill in inscription
						uint item_millingskilllevel = item.GetTemplate().GetRequiredSkillRank();

						if (item_millingskilllevel > player.GetSkillValue(SkillType.Inscription))
							return SpellCastResult.LowCastlevel;

						//make sure the player has the required herbs in inventory
						if (item.GetCount() < 5)
						{
							param1 = (int)item.GetEntry();
							param2 = 5;

							return SpellCastResult.NeedMoreItems;
						}

						if (!LootStorage.Milling.HaveLootFor(_targets.GetItemTargetEntry()))
							return SpellCastResult.CantBeMilled;

						break;
					}
					case SpellEffectName.WeaponDamage:
					case SpellEffectName.WeaponDamageNoSchool:
					{
						if (_attackType != WeaponAttackType.RangedAttack)
							break;

						Item item = player.GetWeaponForAttack(_attackType);

						if (item == null ||
						    item.IsBroken())
							return SpellCastResult.EquippedItem;

						switch ((ItemSubClassWeapon)item.GetTemplate().GetSubClass())
						{
							case ItemSubClassWeapon.Thrown:
							{
								uint ammo = item.GetEntry();

								if (!player.HasItemCount(ammo))
									return SpellCastResult.NoAmmo;

								break;
							}
							case ItemSubClassWeapon.Gun:
							case ItemSubClassWeapon.Bow:
							case ItemSubClassWeapon.Crossbow:
							case ItemSubClassWeapon.Wand:
								break;
							default:
								break;
						}

						break;
					}
					case SpellEffectName.RechargeItem:
					{
						uint itemId = spellEffectInfo.ItemType;

						ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);

						if (proto == null)
							return SpellCastResult.ItemAtMaxCharges;

						Item item = player.GetItemByEntry(itemId);

						if (item != null)
							foreach (ItemEffectRecord itemEffect in item.GetEffects())
								if (itemEffect.LegacySlotIndex <= item._itemData.SpellCharges.GetSize() &&
								    itemEffect.Charges != 0 &&
								    item.GetSpellCharges(itemEffect.LegacySlotIndex) == itemEffect.Charges)
									return SpellCastResult.ItemAtMaxCharges;

						break;
					}
					case SpellEffectName.RespecAzeriteEmpoweredItem:
					{
						Item item = _targets.GetItemTarget();

						if (item == null)
							return SpellCastResult.AzeriteEmpoweredOnly;

						if (item.GetOwnerGUID() != _caster.GetGUID())
							return SpellCastResult.DontReport;

						AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();

						if (azeriteEmpoweredItem == null)
							return SpellCastResult.AzeriteEmpoweredOnly;

						bool hasSelections = false;

						for (int tier = 0; tier < SharedConst.MaxAzeriteEmpoweredTier; ++tier)
							if (azeriteEmpoweredItem.GetSelectedAzeritePower(tier) != 0)
							{
								hasSelections = true;

								break;
							}

						if (!hasSelections)
							return SpellCastResult.AzeriteEmpoweredNoChoicesToUndo;

						if (!_caster.ToPlayer().HasEnoughMoney(azeriteEmpoweredItem.GetRespecCost()))
							return SpellCastResult.DontReport;

						break;
					}
					default:
						break;
				}

			// check weapon presence in slots for main/offhand weapons
			if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement) &&
			    _spellInfo.EquippedItemClass >= 0)
			{
				var weaponCheck = new Func<WeaponAttackType, SpellCastResult>(attackType =>
				                                                              {
					                                                              Item item = player.ToPlayer().GetWeaponForAttack(attackType);

					                                                              // skip spell if no weapon in slot or broken
					                                                              if (!item ||
					                                                                  item.IsBroken())
						                                                              return SpellCastResult.EquippedItemClass;

					                                                              // skip spell if weapon not fit to triggered spell
					                                                              if (!item.IsFitToSpellRequirements(_spellInfo))
						                                                              return SpellCastResult.EquippedItemClass;

					                                                              return SpellCastResult.SpellCastOk;
				                                                              });

				// main hand weapon required
				if (_spellInfo.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
				{
					SpellCastResult mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);

					if (mainHandResult != SpellCastResult.SpellCastOk)
						return mainHandResult;
				}

				// offhand hand weapon required
				if (_spellInfo.HasAttribute(SpellAttr3.RequiresOffHandWeapon))
				{
					SpellCastResult offHandResult = weaponCheck(WeaponAttackType.OffAttack);

					if (offHandResult != SpellCastResult.SpellCastOk)
						return offHandResult;
				}
			}

			return SpellCastResult.SpellCastOk;
		}

		public void Delayed() // only called in DealDamage()
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			if (IsDelayableNoMore()) // Spells may only be delayed twice
				return;

			//check pushback reduce
			int delaytime   = 500; // spellcasting delay is normally 500ms
			int delayReduce = 100; // must be initialized to 100 for percent modifiers

			Player player = unitCaster.GetSpellModOwner();

			if (player != null)
				player.ApplySpellMod(_spellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

			delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

			if (delayReduce >= 100)
				return;

			MathFunctions.AddPct(ref delaytime, -delayReduce);

			if (_timer + delaytime > _casttime)
			{
				delaytime = _casttime - _timer;
				_timer    = _casttime;
			}
			else
			{
				_timer += delaytime;
			}

			SpellDelayed spellDelayed = new();
			spellDelayed.Caster      = unitCaster.GetGUID();
			spellDelayed.ActualDelay = delaytime;

			unitCaster.SendMessageToSet(spellDelayed, true);
		}

		public void DelayedChannel()
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			if (_spellState != SpellState.Casting)
				return;

			if (IsDelayableNoMore()) // Spells may only be delayed twice
				return;

			//check pushback reduce
			// should be affected by modifiers, not take the dbc duration.
			int duration = ((_channeledDuration > 0) ? _channeledDuration : _spellInfo.GetDuration());

			int delaytime   = MathFunctions.CalculatePct(duration, 25); // channeling delay is normally 25% of its time per hit
			int delayReduce = 100;                                      // must be initialized to 100 for percent modifiers

			Player player = unitCaster.GetSpellModOwner();

			if (player != null)
				player.ApplySpellMod(_spellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

			delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

			if (delayReduce >= 100)
				return;

			MathFunctions.AddPct(ref delaytime, -delayReduce);

			if (_timer <= delaytime)
			{
				delaytime = _timer;
				_timer    = 0;
			}
			else
			{
				_timer -= delaytime;
			}

			foreach (var ihit in _UniqueTargetInfo)
				if (ihit.MissCondition == SpellMissInfo.None)
				{
					Unit unit = (unitCaster.GetGUID() == ihit.TargetGUID) ? unitCaster : Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);

					if (unit != null)
						unit.DelayOwnedAuras(_spellInfo.Id, _originalCasterGUID, delaytime);
				}

			// partially interrupt persistent area auras
			DynamicObject dynObj = unitCaster.GetDynObject(_spellInfo.Id);

			if (dynObj != null)
				dynObj.Delay(delaytime);

			SendChannelUpdate((uint)_timer);
		}

		public bool HasPowerTypeCost(PowerType power)
		{
			return GetPowerTypeCostAmount(power).HasValue;
		}

		public int? GetPowerTypeCostAmount(PowerType power)
		{
			var powerCost = _powerCost.Find(cost => cost.Power == power);

			if (powerCost == null)
				return null;

			return powerCost.Amount;
		}

		private bool UpdatePointers()
		{
			if (_originalCasterGUID == _caster.GetGUID())
			{
				_originalCaster = _caster.ToUnit();
			}
			else
			{
				_originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGUID);

				if (_originalCaster != null &&
				    !_originalCaster.IsInWorld)
					_originalCaster = null;
			}

			if (!_castItemGUID.IsEmpty() &&
			    _caster.IsTypeId(TypeId.Player))
			{
				_CastItem      = _caster.ToPlayer().GetItemByGuid(_castItemGUID);
				_castItemLevel = -1;

				// cast item not found, somehow the item is no longer where we expected
				if (!_CastItem)
					return false;

				// check if the item is really the same, in case it has been wrapped for example
				if (_castItemEntry != _CastItem.GetEntry())
					return false;

				_castItemLevel = (int)_CastItem.GetItemLevel(_caster.ToPlayer());
			}

			_targets.Update(_caster);

			// further actions done only for dest targets
			if (!_targets.HasDst())
				return true;

			// cache last transport
			WorldObject transport = null;

			// update effect destinations (in case of moved transport dest target)
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				SpellDestination dest = _destTargets[spellEffectInfo.EffectIndex];

				if (dest.TransportGUID.IsEmpty())
					continue;

				if (transport == null ||
				    transport.GetGUID() != dest.TransportGUID)
					transport = Global.ObjAccessor.GetWorldObject(_caster, dest.TransportGUID);

				if (transport != null)
				{
					dest.Position.Relocate(transport.GetPosition());
					dest.Position.RelocateOffset(dest.TransportOffset);
				}
			}

			return true;
		}

		public CurrentSpellTypes GetCurrentContainer()
		{
			if (_spellInfo.IsNextMeleeSwingSpell())
				return CurrentSpellTypes.Melee;
			else if (IsAutoRepeat())
				return CurrentSpellTypes.AutoRepeat;
			else if (_spellInfo.IsChanneled())
				return CurrentSpellTypes.Channeled;

			return CurrentSpellTypes.Generic;
		}

		public Difficulty GetCastDifficulty()
		{
			return _caster.GetMap().GetDifficultyID();
		}

		private bool CheckEffectTarget(Unit target, SpellEffectInfo spellEffectInfo, Position losPosition)
		{
			if (spellEffectInfo == null ||
			    !spellEffectInfo.IsEffect())
				return false;

			switch (spellEffectInfo.ApplyAuraName)
			{
				case AuraType.ModPossess:
				case AuraType.ModCharm:
				case AuraType.ModPossessPet:
				case AuraType.AoeCharm:
					if (target.GetVehicleKit() &&
					    target.GetVehicleKit().IsControllableVehicle())
						return false;

					if (target.IsMounted())
						return false;

					if (!target.GetCharmerGUID().IsEmpty())
						return false;

					int damage = CalculateDamage(spellEffectInfo, target);

					if (damage != 0)
						if (target.GetLevelForTarget(_caster) > damage)
							return false;

					break;
				default:
					break;
			}

			// check for ignore LOS on the effect itself
			if (_spellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) ||
			    Global.DisableMgr.IsDisabledFor(DisableType.Spell, _spellInfo.Id, null, (byte)DisableFlags.SpellLOS))
				return true;

			// check if gameobject ignores LOS
			GameObject gobCaster = _caster.ToGameObject();

			if (gobCaster != null)
				if (gobCaster.GetGoInfo().GetRequireLOS() == 0)
					return true;

			// if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
			if (!_spellInfo.HasAttribute(SpellAttr5.AlwaysLineOfSight) &&
			    IsTriggered() &&
			    _triggeredByAuraSpell != null &&
			    (_triggeredByAuraSpell.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, _triggeredByAuraSpell.Id, null, (byte)DisableFlags.SpellLOS)))
				return true;

			// @todo shit below shouldn't be here, but it's temporary
			//Check targets for LOS visibility
			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.SkinPlayerCorpse:
				{
					if (_targets.GetCorpseTargetGUID().IsEmpty())
					{
						if (target.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2) &&
						    target.HasUnitFlag(UnitFlags.Skinnable))
							return true;

						return false;
					}

					Corpse corpse = ObjectAccessor.GetCorpse(_caster, _targets.GetCorpseTargetGUID());

					if (!corpse)
						return false;

					if (target.GetGUID() != corpse.GetOwnerGUID())
						return false;

					if (!corpse.HasCorpseDynamicFlag(CorpseDynFlags.Lootable))
						return false;

					if (!corpse.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
						return false;

					break;
				}
				default:
				{
					if (losPosition == null ||
					    _spellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight))
					{
						// Get GO cast coordinates if original caster . GO
						WorldObject caster = null;

						if (_originalCasterGUID.IsGameObject())
							caster = _caster.GetMap().GetGameObject(_originalCasterGUID);

						if (!caster)
							caster = _caster;

						if (target != _caster &&
						    !target.IsWithinLOSInMap(caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
							return false;
					}

					if (losPosition != null)
						if (!target.IsWithinLOS(losPosition.GetPositionX(), losPosition.GetPositionY(), losPosition.GetPositionZ(), LineOfSightChecks.All, ModelIgnoreFlags.M2))
							return false;

					break;
				}
			}

			return true;
		}

		private bool CheckEffectTarget(GameObject target, SpellEffectInfo spellEffectInfo)
		{
			if (spellEffectInfo == null ||
			    !spellEffectInfo.IsEffect())
				return false;

			switch (spellEffectInfo.Effect)
			{
				case SpellEffectName.GameObjectDamage:
				case SpellEffectName.GameobjectRepair:
				case SpellEffectName.GameobjectSetDestructionState:
					if (target.GetGoType() != GameObjectTypes.DestructibleBuilding)
						return false;

					break;
				default:
					break;
			}

			return true;
		}

		private bool CheckEffectTarget(Item target, SpellEffectInfo spellEffectInfo)
		{
			if (spellEffectInfo == null ||
			    !spellEffectInfo.IsEffect())
				return false;

			return true;
		}

		private bool IsAutoActionResetSpell()
		{
			if (IsTriggered())
				return false;

			if (_casttime == 0 &&
			    _spellInfo.HasAttribute(SpellAttr6.DoesntResetSwingTimerIfInstant))
				return false;

			return true;
		}

		public bool IsPositive()
		{
			return _spellInfo.IsPositive() && (_triggeredByAuraSpell == null || _triggeredByAuraSpell.IsPositive());
		}

		private bool IsNeedSendToClient()
		{
			return _SpellVisual.SpellXSpellVisualID != 0 ||
			       _SpellVisual.ScriptVisualID != 0 ||
			       _spellInfo.IsChanneled() ||
			       _spellInfo.HasAttribute(SpellAttr8.AuraSendAmount) ||
			       _spellInfo.HasHitDelay() ||
			       (_triggeredByAuraSpell == null && !IsTriggered());
		}

		public Unit GetUnitCasterForEffectHandlers()
		{
			return _originalCaster != null ? _originalCaster : _caster.ToUnit();
		}

		private bool IsValidDeadOrAliveTarget(Unit target)
		{
			if (target.IsAlive())
				return !_spellInfo.IsRequiringDeadTarget();

			if (_spellInfo.IsAllowingDeadTarget())
				return true;

			return false;
		}

		private void HandleLaunchPhase()
		{
			// handle effects with SPELL_EFFECT_HANDLE_LAUNCH mode
			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				// don't do anything for empty effect
				if (!spellEffectInfo.IsEffect())
					continue;

				HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
			}

			PrepareTargetProcessing();

			foreach (TargetInfo target in _UniqueTargetInfo)
				PreprocessSpellLaunch(target);

			foreach (var spellEffectInfo in _spellInfo.GetEffects())
			{
				float multiplier = 1.0f;

				if ((_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
					multiplier = spellEffectInfo.CalcDamageMultiplier(_originalCaster, this);

				foreach (TargetInfo target in _UniqueTargetInfo)
				{
					uint mask = target.EffectMask;

					if ((mask & (1 << (int)spellEffectInfo.EffectIndex)) == 0)
						continue;

					DoEffectOnLaunchTarget(target, multiplier, spellEffectInfo);
				}
			}

			FinishTargetProcessing();
		}

		private void PreprocessSpellLaunch(TargetInfo targetInfo)
		{
			Unit targetUnit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);

			if (targetUnit == null)
				return;

			// This will only cause combat - the target will engage once the projectile hits (in Spell::TargetInfo::PreprocessTarget)
			if (_originalCaster &&
			    targetInfo.MissCondition != SpellMissInfo.Evade &&
			    !_originalCaster.IsFriendlyTo(targetUnit) &&
			    (!_spellInfo.IsPositive() || _spellInfo.HasEffect(SpellEffectName.Dispel)) &&
			    (_spellInfo.HasInitialAggro() || targetUnit.IsEngaged()))
				_originalCaster.SetInCombatWith(targetUnit, true);

			Unit unit = null;

			// In case spell hit target, do all effect on that target
			if (targetInfo.MissCondition == SpellMissInfo.None)
				unit = targetUnit;
			// In case spell reflect from target, do all effect on caster (if hit)
			else if (targetInfo.MissCondition == SpellMissInfo.Reflect &&
			         targetInfo.ReflectResult == SpellMissInfo.None)
				unit = _caster.ToUnit();

			if (unit == null)
				return;

			float critChance = _spellValue.CriticalChance;

			if (_originalCaster)
			{
				if (critChance == 0)
					critChance = _originalCaster.SpellCritChanceDone(this, null, _spellSchoolMask, _attackType);

				critChance = unit.SpellCritChanceTaken(_originalCaster, this, null, _spellSchoolMask, critChance, _attackType);
			}

			targetInfo.IsCrit = RandomHelper.randChance(critChance);
		}

		private void DoEffectOnLaunchTarget(TargetInfo targetInfo, float multiplier, SpellEffectInfo spellEffectInfo)
		{
			Unit unit = null;

			// In case spell hit target, do all effect on that target
			if (targetInfo.MissCondition == SpellMissInfo.None ||
			    (targetInfo.MissCondition == SpellMissInfo.Block && !_spellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
				unit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);
			// In case spell reflect from target, do all effect on caster (if hit)
			else if (targetInfo.MissCondition == SpellMissInfo.Reflect &&
			         targetInfo.ReflectResult == SpellMissInfo.None)
				unit = _caster.ToUnit();

			if (!unit)
				return;

			_damage  = 0;
			_healing = 0;

			HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

			if (_originalCaster != null &&
			    _damage > 0)
				if (spellEffectInfo.IsTargetingArea() ||
				    spellEffectInfo.IsAreaAuraEffect() ||
				    spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura) ||
				    _spellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
				{
					_damage = unit.CalculateAOEAvoidance(_damage, (uint)_spellInfo.SchoolMask, _originalCaster.GetGUID());

					if (_originalCaster.IsPlayer())
					{
						// cap damage of player AOE
						long targetAmount = GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);

						if (targetAmount > 20)
							_damage = (int)(_damage * 20 / targetAmount);
					}
				}

			if ((_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
			{
				_damage  = (int)(_damage * _damageMultipliers[spellEffectInfo.EffectIndex]);
				_healing = (int)(_healing * _damageMultipliers[spellEffectInfo.EffectIndex]);

				_damageMultipliers[spellEffectInfo.EffectIndex] *= multiplier;
			}

			targetInfo.Damage  += _damage;
			targetInfo.Healing += _healing;
		}

		private SpellCastResult CanOpenLock(SpellEffectInfo effect, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
		{
			if (lockId == 0) // possible case for GO and maybe for items.
				return SpellCastResult.SpellCastOk;

			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return SpellCastResult.BadTargets;

			// Get LockInfo
			var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

			if (lockInfo == null)
				return SpellCastResult.BadTargets;

			bool reqKey = false; // some locks not have reqs

			for (int j = 0; j < SharedConst.MaxLockCase; ++j)
				switch ((LockKeyType)lockInfo.LockType[j])
				{
					// check key item (many fit cases can be)
					case LockKeyType.Item:
						if (lockInfo.Index[j] != 0 &&
						    _CastItem &&
						    _CastItem.GetEntry() == lockInfo.Index[j])
							return SpellCastResult.SpellCastOk;

						reqKey = true;

						break;
					// check key skill (only single first fit case can be)
					case LockKeyType.Skill:
					{
						reqKey = true;

						// wrong locktype, skip
						if (effect.MiscValue != lockInfo.Index[j])
							continue;

						skillId = SharedConst.SkillByLockType((LockType)lockInfo.Index[j]);

						if (skillId != SkillType.None ||
						    lockInfo.Index[j] == (uint)LockType.Lockpicking)
						{
							reqSkillValue = lockInfo.Skill[j];

							// castitem check: rogue using skeleton keys. the skill values should not be added in this case.
							skillValue = 0;

							if (!_CastItem &&
							    unitCaster.IsTypeId(TypeId.Player))
								skillValue = unitCaster.ToPlayer().GetSkillValue(skillId);
							else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
								skillValue = (int)unitCaster.GetLevel() * 5;

							// skill bonus provided by casting spell (mostly item spells)
							// add the effect base points modifier from the spell cast (cheat lock / skeleton key etc.)
							if (effect.TargetA.GetTarget() == Targets.GameobjectItemTarget ||
							    effect.TargetB.GetTarget() == Targets.GameobjectItemTarget)
								skillValue += effect.CalcValue();

							if (skillValue < reqSkillValue)
								return SpellCastResult.LowCastlevel;
						}

						return SpellCastResult.SpellCastOk;
					}
					case LockKeyType.Spell:
						if (_spellInfo.Id == lockInfo.Index[j])
							return SpellCastResult.SpellCastOk;

						reqKey = true;

						break;
				}

			if (reqKey)
				return SpellCastResult.BadTargets;

			return SpellCastResult.SpellCastOk;
		}

		public void SetSpellValue(SpellValueMod mod, int value)
		{
			if (mod < SpellValueMod.End)
			{
				_spellValue.EffectBasePoints[(int)mod] =  value;
				_spellValue.CustomBasePointsMask       |= 1u << (int)mod;

				return;
			}

			switch (mod)
			{
				case SpellValueMod.RadiusMod:
					_spellValue.RadiusMod = (float)value / 10000;

					break;
				case SpellValueMod.MaxTargets:
					_spellValue.MaxAffectedTargets = (uint)value;

					break;
				case SpellValueMod.AuraStack:
					_spellValue.AuraStackAmount = value;

					break;
				case SpellValueMod.CritChance:
					_spellValue.CriticalChance = value / 100.0f; // @todo ugly /100 remove when basepoints are double

					break;
				case SpellValueMod.DurationPct:
					_spellValue.DurationMul = (float)value / 100.0f;

					break;
				case SpellValueMod.Duration:
					_spellValue.Duration = value;

					break;
			}
		}

		private void PrepareTargetProcessing()
		{
		}

		private void FinishTargetProcessing()
		{
			SendSpellExecuteLog();
		}

		private void LoadScripts()
		{
			_loadedScripts = Global.ScriptMgr.CreateSpellScripts(_spellInfo.Id, this);

			foreach (var script in _loadedScripts)
			{
				Log.outDebug(LogFilter.Spells, "Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script._GetScriptName(), _spellInfo.Id);
				script.Register();

				if (script is ISpellScript)
					foreach (var iFace in script.GetType().GetInterfaces())
					{
						if (iFace.Name == nameof(ISpellScript) ||
						    iFace.Name == nameof(ISpellScript))
							continue;

						if (!_spellScriptsByType.TryGetValue(iFace, out var spellScripts))
						{
							spellScripts               = new List<ISpellScript>();
							_spellScriptsByType[iFace] = spellScripts;
						}

						spellScripts.Add((ISpellScript)script);
						RegisterSpellEffectHandler(script);
					}
			}
		}

		private void RegisterSpellEffectHandler(SpellScript script)
		{
			if (script is IHasSpellEffects hse)
				foreach (var effect in hse.SpellEffects)
					if (effect is ISpellEffectHandler se)
					{
						uint mask = 0;

						if (se.EffectIndex == SpellConst.EffectAll ||
						    se.EffectIndex == SpellConst.EffectFirstFound)
						{
							for (byte i = 0; i < SpellConst.MaxEffects; ++i)
							{
								if (se.EffectIndex == SpellConst.EffectFirstFound &&
								    mask != 0)
									break;

								if (CheckSpellEffectHandler(se, i))
									AddSpellEffect(i, script, se);
							}
						}
						else
						{
							if (CheckSpellEffectHandler(se, se.EffectIndex))
								AddSpellEffect(se.EffectIndex, script, se);
						}
					}
					else if (effect is ITargetHookHandler th)
					{
						uint mask = 0;

						if (th.EffectIndex == SpellConst.EffectAll ||
						    th.EffectIndex == SpellConst.EffectFirstFound)
						{
							for (byte i = 0; i < SpellConst.MaxEffects; ++i)
							{
								if (th.EffectIndex == SpellConst.EffectFirstFound &&
								    mask != 0)
									break;

								if (CheckTargetHookEffect(th, i))
									AddSpellEffect(i, script, th);
							}
						}
						else
						{
							if (CheckTargetHookEffect(th, th.EffectIndex))
								AddSpellEffect(th.EffectIndex, script, th);
						}
					}
		}

		private bool CheckSpellEffectHandler(ISpellEffectHandler se, uint effIndex)
		{
			if (_spellInfo.GetEffects().Count <= effIndex)
				return false;

			SpellEffectInfo spellEffectInfo = _spellInfo.GetEffect(effIndex);

			if (spellEffectInfo.Effect == 0 &&
			    se.EffectName == 0)
				return true;

			if (spellEffectInfo.Effect == 0)
				return false;

			return se.EffectName == SpellEffectName.Any || spellEffectInfo.Effect == se.EffectName;
		}

		public bool CheckTargetHookEffect(ITargetHookHandler th, uint effIndexToCheck)
		{
			if (th.TargetType == 0)
				return false;

			if (_spellInfo.GetEffects().Count <= effIndexToCheck)
				return false;

			SpellEffectInfo spellEffectInfo = _spellInfo.GetEffect(effIndexToCheck);

			if (spellEffectInfo.TargetA.GetTarget() != th.TargetType &&
			    spellEffectInfo.TargetB.GetTarget() != th.TargetType)
				return false;

			SpellImplicitTargetInfo targetInfo = new(th.TargetType);

			switch (targetInfo.GetSelectionCategory())
			{
				case SpellTargetSelectionCategories.Channel: // SINGLE
					return !th.Area;
				case SpellTargetSelectionCategories.Nearby: // BOTH
					return true;
				case SpellTargetSelectionCategories.Cone: // AREA
				case SpellTargetSelectionCategories.Line: // AREA
					return th.Area;
				case SpellTargetSelectionCategories.Area: // AREA
					if (targetInfo.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
						return th.Area || th.Dest;

					return th.Area;
				case SpellTargetSelectionCategories.Default:
					switch (targetInfo.GetObjectType())
					{
						case SpellTargetObjectTypes.Src: // EMPTY
							return false;
						case SpellTargetObjectTypes.Dest: // Dest
							return th.Dest;
						default:
							switch (targetInfo.GetReferenceType())
							{
								case SpellTargetReferenceTypes.Caster: // SINGLE
									return !th.Area;
								case SpellTargetReferenceTypes.Target: // BOTH
									return true;
								default:
									break;
							}

							break;
					}

					break;
				default:
					break;
			}

			return false;
		}


		private void CallScriptOnPrecastHandler()
		{
			foreach (ISpellScript script in GetSpellScripts<IOnPrecast>())
			{
				script._PrepareScriptCall(SpellScriptHookType.OnPrecast);
				((IOnPrecast)script).OnPrecast();
				script._FinishScriptCall();
			}
		}

		private void CallScriptBeforeCastHandlers()
		{
			foreach (ISpellScript script in GetSpellScripts<IBeforeCast>())
			{
				script._PrepareScriptCall(SpellScriptHookType.BeforeCast);

				((IBeforeCast)script).BeforeCast();

				script._FinishScriptCall();
			}
		}

		private void CallScriptOnCastHandlers()
		{
			foreach (ISpellScript script in GetSpellScripts<IOnCast>())
			{
				script._PrepareScriptCall(SpellScriptHookType.OnCast);

				((IOnCast)script).OnCast();

				script._FinishScriptCall();
			}
		}

		private void CallScriptAfterCastHandlers()
		{
			foreach (ISpellScript script in GetSpellScripts<IAfterCast>())
			{
				script._PrepareScriptCall(SpellScriptHookType.AfterCast);

				((IAfterCast)script).AfterCast();

				script._FinishScriptCall();
			}
		}

		private SpellCastResult CallScriptCheckCastHandlers()
		{
			SpellCastResult retVal = SpellCastResult.SpellCastOk;

			foreach (ISpellScript script in GetSpellScripts<ICheckCastHander>())
			{
				script._PrepareScriptCall(SpellScriptHookType.CheckCast);

				var tempResult = ((ICheckCastHander)script).CheckCast();

				if (tempResult != SpellCastResult.SpellCastOk)
					retVal = tempResult;

				script._FinishScriptCall();
			}

			return retVal;
		}

		private int CallScriptCalcCastTimeHandlers(int castTime)
		{
			foreach (ISpellScript script in GetSpellScripts<ICalculateCastTime>())
			{
				script._PrepareScriptCall(SpellScriptHookType.CalcCastTime);
				castTime = ((ICalculateCastTime)script).CalcCastTime(castTime);
				script._FinishScriptCall();
			}

			return castTime;
		}

		private bool CallScriptEffectHandlers(uint effIndex, SpellEffectHandleMode mode)
		{
			// execute script effect handler hooks and check if effects was prevented
			bool preventDefault = false;

			switch (mode)
			{
				case SpellEffectHandleMode.Launch:

					foreach (var script in GetEffectScripts(SpellScriptHookType.Launch, effIndex))
						preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Launch);

					break;
				case SpellEffectHandleMode.LaunchTarget:

					foreach (var script in GetEffectScripts(SpellScriptHookType.LaunchTarget, effIndex))
						preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.LaunchTarget);

					break;
				case SpellEffectHandleMode.Hit:

					foreach (var script in GetEffectScripts(SpellScriptHookType.Hit, effIndex))
						preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Hit);

					break;
				case SpellEffectHandleMode.HitTarget:

					foreach (var script in GetEffectScripts(SpellScriptHookType.EffectHitTarget, effIndex))
						preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.EffectHitTarget);

					break;
				default:
					Cypher.Assert(false);

					return false;
			}

			return preventDefault;
		}

		private static bool ProcessScript(uint effIndex, bool preventDefault, ISpellScript script, ISpellEffect effect, SpellScriptHookType hookType)
		{
			script._InitHit();

			script._PrepareScriptCall(hookType);

			if (!script._IsEffectPrevented(effIndex))
				if (effect is ISpellEffectHandler seh)
					seh.CallEffect(effIndex);

			if (!preventDefault)
				preventDefault = script._IsDefaultEffectPrevented(effIndex);

			script._FinishScriptCall();

			return preventDefault;
		}

		private void CallScriptSuccessfulDispel(uint effIndex)
		{
			foreach (var script in GetEffectScripts(SpellScriptHookType.EffectSuccessfulDispel, effIndex))
			{
				script.Item1._PrepareScriptCall(SpellScriptHookType.EffectSuccessfulDispel);

				if (script.Item2 is ISpellEffectHandler seh)
					seh.CallEffect(effIndex);

				script.Item1._FinishScriptCall();
			}
		}

		public void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
		{
			foreach (ISpellScript script in GetSpellScripts<IBeforeHit>())
			{
				script._InitHit();
				script._PrepareScriptCall(SpellScriptHookType.BeforeHit);
				((IBeforeHit)script).BeforeHit(missInfo);
				script._FinishScriptCall();
			}
		}

		public void CallScriptOnHitHandlers()
		{
			foreach (ISpellScript script in GetSpellScripts<IOnHit>())
			{
				script._PrepareScriptCall(SpellScriptHookType.Hit);
				((IOnHit)script).OnHit();
				script._FinishScriptCall();
			}
		}

		public void CallScriptAfterHitHandlers()
		{
			foreach (ISpellScript script in GetSpellScripts<IAfterHit>())
			{
				script._PrepareScriptCall(SpellScriptHookType.AfterHit);
				((IAfterHit)script).AfterHit();
				script._FinishScriptCall();
			}
		}

		public void CallScriptCalcCritChanceHandlers(Unit victim, ref float critChance)
		{
			foreach (ISpellScript loadedScript in GetSpellScripts<ICalcCritChance>())
			{
				loadedScript._PrepareScriptCall(SpellScriptHookType.CalcCritChance);

				((ICalcCritChance)loadedScript).CalcCritChance(victim, ref critChance);

				loadedScript._FinishScriptCall();
			}
		}

		private void CallScriptObjectAreaTargetSelectHandlers(List<WorldObject> targets, uint effIndex, SpellImplicitTargetInfo targetType)
		{
			foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex))
			{
				script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectAreaTargetSelect);

				if (script.Item2 is IObjectAreaTargetSelect oas)
					if (targetType.GetTarget() == oas.TargetType)
						oas.FilterTargets(targets);

				script.Item1._FinishScriptCall();
			}
		}

		private void CallScriptObjectTargetSelectHandlers(ref WorldObject target, uint effIndex, SpellImplicitTargetInfo targetType)
		{
			foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex))
			{
				script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectTargetSelect);

				if (script.Item2 is IObjectTargetSelectHandler ots)
					if (targetType.GetTarget() == ots.TargetType)
						ots.TargetSelect(ref target);

				script.Item1._FinishScriptCall();
			}
		}

		private void CallScriptDestinationTargetSelectHandlers(ref SpellDestination target, uint effIndex, SpellImplicitTargetInfo targetType)
		{
			foreach (var script in GetEffectScripts(SpellScriptHookType.DestinationTargetSelect, effIndex))
			{
				script.Item1._PrepareScriptCall(SpellScriptHookType.DestinationTargetSelect);

				if (script.Item2 is IDestinationTargetSelectHandler dts)
					if (targetType.GetTarget() == dts.TargetType)
						dts.SetDest(ref target);

				script.Item1._FinishScriptCall();
			}
		}

		public void CallScriptOnResistAbsorbCalculateHandlers(DamageInfo damageInfo, ref uint resistAmount, ref int absorbAmount)
		{
			foreach (ISpellScript script in GetSpellScripts<ICheckCastHander>())
			{
				script._PrepareScriptCall(SpellScriptHookType.OnResistAbsorbCalculation);

				((ICalculateResistAbsorb)script).CalculateResistAbsorb(damageInfo, ref resistAmount, ref absorbAmount);

				script._FinishScriptCall();
			}
		}

		private bool CheckScriptEffectImplicitTargets(uint effIndex, uint effIndexToCheck)
		{
			// Skip if there are not any script
			if (_loadedScripts.Empty())
				return true;

			var otsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex).Count > 0;
			var otsEffIndexCheck  = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndexToCheck).Count > 0;

			var oatsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex).Count > 0;
			var oatsEffIndexCheck  = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndexToCheck).Count > 0;

			if ((otsTargetEffIndex && !otsEffIndexCheck) ||
			    (!otsTargetEffIndex && otsEffIndexCheck))
				return false;

			if ((oatsTargetEffIndex && !oatsEffIndexCheck) ||
			    (!oatsTargetEffIndex && oatsEffIndexCheck))
				return false;

			return true;
		}

		public bool CanExecuteTriggersOnHit(Unit unit, SpellInfo triggeredByAura = null)
		{
			bool onlyOnTarget = triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ClassTriggerOnlyOnTarget);

			if (!onlyOnTarget)
				return true;

			// If triggeredByAura has SPELL_ATTR4_CLASS_TRIGGER_ONLY_ON_TARGET then it can only proc on either noncaster units...
			if (unit != _caster)
				return true;

			// ... or caster if it is the only target
			if (_UniqueTargetInfo.Count == 1)
				return true;

			return false;
		}

		private void PrepareTriggersExecutedOnHit()
		{
			Unit unitCaster = _caster.ToUnit();

			if (unitCaster == null)
				return;

			// handle SPELL_AURA_ADD_TARGET_TRIGGER auras:
			// save auras which were present on spell caster on cast, to prevent triggered auras from affecting caster
			// and to correctly calculate proc chance when combopoints are present
			var targetTriggers = unitCaster.GetAuraEffectsByType(AuraType.AddTargetTrigger);

			foreach (var aurEff in targetTriggers)
			{
				if (!aurEff.IsAffectingSpell(_spellInfo))
					continue;

				SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(aurEff.GetSpellEffectInfo().TriggerSpell, GetCastDifficulty());

				if (spellInfo != null)
				{
					// calculate the chance using spell base amount, because aura amount is not updated on combo-points change
					// this possibly needs fixing
					int auraBaseAmount = aurEff.GetBaseAmount();
					// proc chance is stored in effect amount
					int chance = unitCaster.CalculateSpellDamage(null, aurEff.GetSpellEffectInfo(), auraBaseAmount);
					chance *= aurEff.GetBase().GetStackAmount();

					// build trigger and add to the list
					_hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.GetSpellInfo(), chance));
				}
			}
		}

		private bool CanHaveGlobalCooldown(WorldObject caster)
		{
			// Only players or controlled units have global cooldown
			if (!caster.IsPlayer() &&
			    (!caster.IsCreature() || caster.ToCreature().GetCharmInfo() == null))
				return false;

			return true;
		}

		private bool HasGlobalCooldown()
		{
			if (!CanHaveGlobalCooldown(_caster))
				return false;

			return _caster.ToUnit().GetSpellHistory().HasGlobalCooldown(_spellInfo);
		}

		private void TriggerGlobalCooldown()
		{
			if (!CanHaveGlobalCooldown(_caster))
				return;

			TimeSpan gcd = TimeSpan.FromMilliseconds(_spellInfo.StartRecoveryTime);

			if (gcd == TimeSpan.Zero ||
			    _spellInfo.StartRecoveryCategory == 0)
				return;

			if (_caster.IsTypeId(TypeId.Player))
				if (_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Cooldown))
					return;

			TimeSpan MinGCD = TimeSpan.FromMilliseconds(750);
			TimeSpan MaxGCD = TimeSpan.FromMilliseconds(1500);

			// Global cooldown can't leave range 1..1.5 secs
			// There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
			// but as tests show are not affected by any spell mods.
			if (gcd >= MinGCD &&
			    gcd <= MaxGCD)
			{
				// gcd modifier auras are applied only to own spells and only players have such mods
				Player modOwner = _caster.GetSpellModOwner();

				if (modOwner)
				{
					int intGcd = (int)gcd.TotalMilliseconds;
					modOwner.ApplySpellMod(_spellInfo, SpellModOp.StartCooldown, ref intGcd, this);
					gcd = TimeSpan.FromMilliseconds(intGcd);
				}

				bool isMeleeOrRangedSpell = _spellInfo.DmgClass == SpellDmgClass.Melee ||
				                            _spellInfo.DmgClass == SpellDmgClass.Ranged ||
				                            _spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
				                            _spellInfo.HasAttribute(SpellAttr0.IsAbility);

				// Apply haste rating
				if (gcd > MinGCD &&
				    (_spellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell))
				{
					gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.ToUnit()._unitData.ModSpellHaste);
					int intGcd = (int)gcd.TotalMilliseconds;
					MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
					gcd = TimeSpan.FromMilliseconds(intGcd);
				}

				if (gcd > MinGCD &&
				    _caster.ToUnit().HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, _spellInfo))
				{
					gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.ToUnit()._unitData.ModHasteRegen);
					int intGcd = (int)gcd.TotalMilliseconds;
					MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
					gcd = TimeSpan.FromMilliseconds(intGcd);
				}
			}

			_caster.ToUnit().GetSpellHistory().AddGlobalCooldown(_spellInfo, gcd);
		}

		private void CancelGlobalCooldown()
		{
			if (!CanHaveGlobalCooldown(_caster))
				return;

			if (_spellInfo.StartRecoveryTime == 0)
				return;

			// Cancel global cooldown when interrupting current cast
			if (_caster.ToUnit().GetCurrentSpell(CurrentSpellTypes.Generic) != this)
				return;

			_caster.ToUnit().GetSpellHistory().CancelGlobalCooldown(_spellInfo);
		}

		private string GetDebugInfo()
		{
			return $"Id: {GetSpellInfo().Id} Name: '{GetSpellInfo().SpellName[Global.WorldMgr.GetDefaultDbcLocale()]}' OriginalCaster: {_originalCasterGUID} State: {GetState()}";
		}

		public List<ISpellScript> GetSpellScripts<T>() where T : ISpellScript
		{
			if (_spellScriptsByType.TryGetValue(typeof(T), out List<ISpellScript> scripts))
				return scripts;

			return _dummy;
		}

		public List<(ISpellScript, ISpellEffect)> GetEffectScripts(SpellScriptHookType h, uint index)
		{
			if (_effectHandlers.TryGetValue(index, out var effDict) &&
			    effDict.TryGetValue(h, out List<(ISpellScript, ISpellEffect)> scripts))
				return scripts;

			return _dummySpellEffects;
		}


		private void AddSpellEffect(uint index, ISpellScript script, ISpellEffect effect)
		{
			if (!_effectHandlers.TryGetValue(index, out var effecTypes))
			{
				effecTypes = new Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>();
				_effectHandlers.Add(index, effecTypes);
			}

			if (!effecTypes.TryGetValue(effect.HookType, out var effects))
			{
				effects = new List<(ISpellScript, ISpellEffect)>();
				effecTypes.Add(effect.HookType, effects);
			}

			effects.Add((script, effect));
		}


		public SpellCastResult CheckMovement()
		{
			if (IsTriggered())
				return SpellCastResult.SpellCastOk;

			Unit unitCaster = _caster.ToUnit();

			if (unitCaster != null)
				if (!unitCaster.CanCastSpellWhileMoving(_spellInfo))
				{
					if (GetState() == SpellState.Preparing)
					{
						if (_casttime > 0 &&
						    _spellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement))
							return SpellCastResult.Moving;
					}
					else if (GetState() == SpellState.Casting &&
					         !_spellInfo.IsMoveAllowedChannel())
					{
						return SpellCastResult.Moving;
					}
				}

			return SpellCastResult.SpellCastOk;
		}

		private int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target)
		{
			return CalculateDamage(spellEffectInfo, target, out _);
		}

		private int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target, out float variance)
		{
			bool needRecalculateBasePoints = (_spellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) == 0;

			return _caster.CalculateSpellDamage(out variance, target, spellEffectInfo, needRecalculateBasePoints ? null : _spellValue.EffectBasePoints[spellEffectInfo.EffectIndex], _castItemEntry, _castItemLevel);
		}

		public SpellState GetState()
		{
			return _spellState;
		}

		public void SetState(SpellState state)
		{
			_spellState = state;
		}

		private void CheckSrc()
		{
			if (!_targets.HasSrc()) _targets.SetSrc(_caster);
		}

		private void CheckDst()
		{
			if (!_targets.HasDst()) _targets.SetDst(_caster);
		}

		public int GetCastTime()
		{
			return _casttime;
		}

		private bool IsAutoRepeat()
		{
			return _autoRepeat;
		}

		private void SetAutoRepeat(bool rep)
		{
			_autoRepeat = rep;
		}

		private void ReSetTimer()
		{
			_timer = _casttime > 0 ? _casttime : 0;
		}

		public bool IsTriggered()
		{
			return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask);
		}

		public bool IsTriggeredByAura(SpellInfo auraSpellInfo)
		{
			return (auraSpellInfo == _triggeredByAuraSpell);
		}

		public bool IsIgnoringCooldowns()
		{
			return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD);
		}

		public bool IsFocusDisabled()
		{
			return _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (_spellInfo.IsChanneled() && !_spellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel));
		}

		public bool IsProcDisabled()
		{
			return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents);
		}

		public bool IsChannelActive()
		{
			return _caster.IsUnit() && _caster.ToUnit().GetChannelSpellId() != 0;
		}

		public bool IsDeletable()
		{
			return !_referencedFromCurrentSpell && !_executedCurrently;
		}

		public void SetReferencedFromCurrent(bool yes)
		{
			_referencedFromCurrentSpell = yes;
		}

		public bool IsInterruptable()
		{
			return !_executedCurrently;
		}

		private void SetExecutedCurrently(bool yes)
		{
			_executedCurrently = yes;
		}

		public ulong GetDelayStart()
		{
			return _delayStart;
		}

		public void SetDelayStart(ulong _time)
		{
			_delayStart = _time;
		}

		public ulong GetDelayMoment()
		{
			return _delayMoment;
		}

		public WorldObject GetCaster()
		{
			return _caster;
		}

		public ObjectGuid GetOriginalCasterGUID()
		{
			return _originalCasterGUID;
		}

		public Unit GetOriginalCaster()
		{
			return _originalCaster;
		}

		public SpellInfo GetSpellInfo()
		{
			return _spellInfo;
		}

		public List<SpellPowerCost> GetPowerCost()
		{
			return _powerCost;
		}

		private bool IsDelayableNoMore()
		{
			if (_delayAtDamageCount >= 2)
				return true;

			++_delayAtDamageCount;

			return false;
		}

		private bool DontReport()
		{
			return Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.DontReportCastError);
		}

		public SpellInfo GetTriggeredByAuraSpell()
		{
			return _triggeredByAuraSpell;
		}

		public static implicit operator bool(Spell spell)
		{
			return spell != null;
		}

		#region Fields

		private Dictionary<SpellEffectName, SpellLogEffect> _executeLogEffects = new();
		private PathGenerator _preGeneratedPath;

		public SpellInfo _spellInfo;
		public Item _CastItem;
		public ObjectGuid _castItemGUID;
		public uint _castItemEntry;
		public int _castItemLevel;
		public ObjectGuid _castId;
		public ObjectGuid _originalCastId;
		public bool _fromClient;
		public SpellCastFlagsEx _castFlagsEx;
		public SpellMisc _misc;
		public object _customArg;
		public SpellCastVisual _SpellVisual;
		public SpellCastTargets _targets;
		public sbyte _comboPointGain;
		public SpellCustomErrors _customError;

		public List<Aura> _appliedMods;

		private WorldObject _caster;
		public SpellValue _spellValue;
		private ObjectGuid _originalCasterGUID;
		private Unit _originalCaster;
		public Spell _selfContainer;

		//Spell data
		internal SpellSchoolMask _spellSchoolMask; // Spell school (can be overwrite for some spells (wand shoot for example)
		internal WeaponAttackType _attackType;     // For weapon based attack

		private List<SpellPowerCost> _powerCost = new();
		private int _casttime;          // Calculated spell cast time initialized only in Spell.prepare
		private int _channeledDuration; // Calculated channeled spell duration in order to calculate correct pushback.
		private bool _canReflect;       // can reflect this spell?
		private bool _autoRepeat;
		private byte _runesState;
		private byte _delayAtDamageCount;

		// Delayed spells system
		private ulong _delayStart;      // time of spell delay start, filled by event handler, zero = just started
		private ulong _delayMoment;     // moment of next delay call, used internally
		private bool _launchHandled;    // were launch actions handled
		private bool _immediateHandled; // were immediate actions handled? (used by delayed spells only)

		// These vars are used in both delayed spell system and modified immediate spell system
		private bool _referencedFromCurrentSpell;
		private bool _executedCurrently;
		internal bool _needComboPoints;
		private uint _applyMultiplierMask;
		private float[] _damageMultipliers = new float[SpellConst.MaxEffects];

		// Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
		public Unit unitTarget;
		public Item itemTarget;
		public GameObject gameObjTarget;
		public Corpse corpseTarget;
		public WorldLocation destTarget;
		public int damage;
		public SpellMissInfo targetMissInfo;
		public float variance;
		private SpellEffectHandleMode effectHandleMode;

		public SpellEffectInfo effectInfo;

		// used in effects handlers
		internal UnitAura spellAura;
		internal DynObjAura dynObjAura;

		// -------------------------------------------
		private GameObject focusObject;

		// Damage and healing in effects need just calculate
		public int _damage;  // Damge   in effects count here
		public int _healing; // Healing in effects count here

		// ******************************************
		// Spell trigger system
		// ******************************************
		internal ProcFlagsInit _procAttacker; // Attacker trigger flags
		internal ProcFlagsInit _procVictim;   // Victim   trigger flags
		internal ProcFlagsHit _hitMask;

		// *****************************************
		// Spell target subsystem
		// *****************************************
		// Targets store structures and data
		public List<TargetInfo> _UniqueTargetInfo = new();
		private uint _channelTargetEffectMask; // Mask req. alive targets

		private List<GOTargetInfo> _UniqueGOTargetInfo = new();
		private List<ItemTargetInfo> _UniqueItemInfo = new();
		private List<CorpseTargetInfo> _UniqueCorpseTargetInfo = new();

		private SpellDestination[] _destTargets = new SpellDestination[SpellConst.MaxEffects];

		private List<HitTriggerSpell> _hitTriggerSpells = new();

		private SpellState _spellState;
		private int _timer;

		private SpellEvent _spellEvent;
		private TriggerCastFlags _triggeredCastFlags;

		// if need this can be replaced by Aura copy
		// we can't store original aura link to prevent access to deleted auras
		// and in same time need aura data and after aura deleting.
		public SpellInfo _triggeredByAuraSpell;

		#endregion
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SpellMisc
	{
		// Alternate names for this value 
		[FieldOffset(0)] public uint TalentId;

		[FieldOffset(0)] public uint SpellId;

		[FieldOffset(0)] public uint SpecializationId;

		// SPELL_EFFECT_SET_FOLLOWER_QUALITY
		// SPELL_EFFECT_INCREASE_FOLLOWER_ITEM_LEVEL
		// SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
		// SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
		// SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
		[FieldOffset(0)] public uint FollowerId;

		[FieldOffset(4)] public uint FollowerAbilityId; // only SPELL_EFFECT_LEARN_FOLLOWER_ABILITY

		// SPELL_EFFECT_FINISH_GARRISON_MISSION
		[FieldOffset(0)] public uint GarrMissionId;

		// SPELL_EFFECT_UPGRADE_HEIRLOOM
		[FieldOffset(0)] public uint ItemId;

		[FieldOffset(0)] public uint Data0;

		[FieldOffset(4)] public uint Data1;

		public uint[] GetRawData()
		{
			return new uint[]
			       {
				       Data0, Data1
			       };
		}
	}

	public struct HitTriggerSpell
	{
		public HitTriggerSpell(SpellInfo spellInfo, SpellInfo auraSpellInfo, int procChance)
		{
			triggeredSpell  = spellInfo;
			triggeredByAura = auraSpellInfo;
			chance          = procChance;
		}

		public SpellInfo triggeredSpell;

		public SpellInfo triggeredByAura;

		// ubyte triggeredByEffIdx          This might be needed at a later stage - No need known for now
		public int chance;
	}

	public enum SpellEffectHandleMode
	{
		Launch,
		LaunchTarget,
		Hit,
		HitTarget
	}

	public class SkillStatusData
	{
		public byte Pos;
		public SkillState State;

		public SkillStatusData(uint _pos, SkillState state)
		{
			Pos   = (byte)_pos;
			State = state;
		}
	}

	public class SpellChainNode
	{
		public SpellInfo first;
		public SpellInfo last;
		public SpellInfo next;
		public SpellInfo prev;
		public byte rank;
	}

	public class SpellLearnSkillNode
	{
		public ushort maxvalue; // 0  - max skill value for player level
		public SkillType skill;
		public ushort step;
		public ushort value; // 0  - max skill value for player level
	}

	public class SpellLearnSpellNode
	{
		public bool Active;      // show in spellbook or not
		public bool AutoLearned; // This marks the spell as automatically learned from another source that - will only be used for unlearning
		public uint OverridesSpell;
		public uint Spell;
	}

	public class SpellDestination
	{
		public WorldLocation Position;
		public ObjectGuid TransportGUID;
		public Position TransportOffset;

		public SpellDestination()
		{
			Position        = new WorldLocation();
			TransportGUID   = ObjectGuid.Empty;
			TransportOffset = new Position();
		}

		public SpellDestination(float x, float y, float z, float orientation = 0.0f, uint mapId = 0xFFFFFFFF) : this()
		{
			Position.Relocate(x, y, z, orientation);
			TransportGUID = ObjectGuid.Empty;
			Position.SetMapId(mapId);
		}

		public SpellDestination(Position pos) : this()
		{
			Position.Relocate(pos);
			TransportGUID = ObjectGuid.Empty;
		}

		public SpellDestination(WorldLocation loc) : this()
		{
			Position.WorldRelocate(loc);
			TransportGUID.Clear();
			TransportOffset.Relocate(0, 0, 0, 0);
		}

		public SpellDestination(WorldObject wObj) : this()
		{
			TransportGUID = wObj.GetTransGUID();
			TransportOffset.Relocate(wObj.GetTransOffsetX(), wObj.GetTransOffsetY(), wObj.GetTransOffsetZ(), wObj.GetTransOffsetO());
			Position.Relocate(wObj.GetPosition());
		}

		public void Relocate(Position pos)
		{
			if (!TransportGUID.IsEmpty())
			{
				Position offset;
				Position.GetPositionOffsetTo(pos, out offset);
				TransportOffset.RelocateOffset(offset);
			}

			Position.Relocate(pos);
		}

		public void RelocateOffset(Position offset)
		{
			if (!TransportGUID.IsEmpty())
				TransportOffset.RelocateOffset(offset);

			Position.RelocateOffset(offset);
		}
	}

	public class TargetInfoBase
	{
		public uint EffectMask;

		public virtual void PreprocessTarget(Spell spell)
		{
		}

		public virtual void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
		{
		}

		public virtual void DoDamageAndTriggers(Spell spell)
		{
		}
	}

	public class TargetInfo : TargetInfoBase
	{
		private bool _enablePVP; // need to enable PVP at DoDamageAndTriggers?

		private Unit _spellHitTarget; // changed for example by reflect
		public int[] AuraBasePoints = new int[SpellConst.MaxEffects];
		public int AuraDuration;
		public int Damage;

		// info set at PreprocessTarget, used by DoTargetSpellHit
		public DiminishingGroup DRGroup;
		public int Healing;
		public UnitAura HitAura;

		public bool IsAlive;
		public bool IsCrit;

		public SpellMissInfo MissCondition;
		public bool Positive = true;
		public SpellMissInfo ReflectResult;
		public ObjectGuid TargetGUID;
		public ulong TimeDelay;

		public override void PreprocessTarget(Spell spell)
		{
			Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

			if (unit == null)
				return;

			// Need init unitTarget by default unit (can changed in code on reflect)
			spell.unitTarget = unit;

			// Reset damage/healing counter
			spell._damage  = Damage;
			spell._healing = Healing;

			_spellHitTarget = null;

			if (MissCondition == SpellMissInfo.None ||
			    (MissCondition == SpellMissInfo.Block && !spell.GetSpellInfo().HasAttribute(SpellAttr3.CompletelyBlocked)))
				_spellHitTarget = unit;
			else if (MissCondition == SpellMissInfo.Reflect &&
			         ReflectResult == SpellMissInfo.None)
				_spellHitTarget = spell.GetCaster().ToUnit();

			if (spell.GetOriginalCaster() &&
			    MissCondition != SpellMissInfo.Evade &&
			    !spell.GetOriginalCaster().IsFriendlyTo(unit) &&
			    (!spell._spellInfo.IsPositive() || spell._spellInfo.HasEffect(SpellEffectName.Dispel)) &&
			    (spell._spellInfo.HasInitialAggro() || unit.IsEngaged()))
				unit.SetInCombatWith(spell.GetOriginalCaster());

			// if target is flagged for pvp also flag caster if a player
			// but respect current pvp rules (buffing/healing npcs flagged for pvp only flags you if they are in combat)
			_enablePVP = (MissCondition == SpellMissInfo.None || spell._spellInfo.HasAttribute(SpellAttr3.PvpEnabling)) && unit.IsPvP() && (unit.IsInCombat() || unit.IsCharmedOwnedByPlayerOrPlayer()) && spell.GetCaster().IsPlayer(); // need to check PvP state before spell effects, but act on it afterwards

			if (_spellHitTarget)
			{
				SpellMissInfo missInfo = spell.PreprocessSpellHit(_spellHitTarget, this);

				if (missInfo != SpellMissInfo.None)
				{
					if (missInfo != SpellMissInfo.Miss)
						spell.GetCaster().SendSpellMiss(unit, spell._spellInfo.Id, missInfo);

					spell._damage   = 0;
					spell._healing  = 0;
					_spellHitTarget = null;
				}
			}

			spell.CallScriptOnHitHandlers();

			// scripts can modify damage/healing for current target, save them
			Damage  = spell._damage;
			Healing = spell._healing;
		}

		public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
		{
			Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

			if (unit == null)
				return;

			// Need init unitTarget by default unit (can changed in code on reflect)
			// Or on missInfo != SPELL_MISS_NONE unitTarget undefined (but need in trigger subsystem)
			spell.unitTarget     = unit;
			spell.targetMissInfo = MissCondition;

			// Reset damage/healing counter
			spell._damage  = Damage;
			spell._healing = Healing;

			if (unit.IsAlive() != IsAlive)
				return;

			if (spell.GetState() == SpellState.Delayed &&
			    !spell.IsPositive() &&
			    (GameTime.GetGameTimeMS() - TimeDelay) <= unit.LastSanctuaryTime)
				return; // No missinfo in that case

			if (_spellHitTarget)
				spell.DoSpellEffectHit(_spellHitTarget, spellEffectInfo, this);

			// scripts can modify damage/healing for current target, save them
			Damage  = spell._damage;
			Healing = spell._healing;
		}

		public override void DoDamageAndTriggers(Spell spell)
		{
			Unit unit = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToUnit() : Global.ObjAccessor.GetUnit(spell.GetCaster(), TargetGUID);

			if (unit == null)
				return;

			// other targets executed before this one changed pointer
			spell.unitTarget = unit;

			if (_spellHitTarget)
				spell.unitTarget = _spellHitTarget;

			// Reset damage/healing counter
			spell._damage  = Damage;
			spell._healing = Healing;

			// Get original caster (if exist) and calculate damage/healing from him data
			// Skip if _originalCaster not available
			Unit caster = spell.GetOriginalCaster() ? spell.GetOriginalCaster() : spell.GetCaster().ToUnit();

			if (caster != null)
			{
				// Fill base trigger info
				ProcFlagsInit      procAttacker  = spell._procAttacker;
				ProcFlagsInit      procVictim    = spell._procVictim;
				ProcFlagsSpellType procSpellType = ProcFlagsSpellType.None;
				ProcFlagsHit       hitMask       = ProcFlagsHit.None;

				// Spells with this flag cannot trigger if effect is cast on self
				bool canEffectTrigger = (!spell._spellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs) || !spell._spellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs)) && spell.unitTarget.CanProc();

				// Trigger info was not filled in Spell::prepareDataForTriggerSystem - we do it now
				if (canEffectTrigger &&
				    !procAttacker &&
				    !procVictim)
				{
					bool positive = true;

					if (spell._damage > 0)
						positive = false;
					else if (spell._healing == 0)
						for (uint i = 0; i < spell._spellInfo.GetEffects().Count; ++i)
						{
							// in case of immunity, check all effects to choose correct procFlags, as none has technically hit
							if (EffectMask != 0 &&
							    (EffectMask & (1 << (int)i)) == 0)
								continue;

							if (!spell._spellInfo.IsPositiveEffect(i))
							{
								positive = false;

								break;
							}
						}

					switch (spell._spellInfo.DmgClass)
					{
						case SpellDmgClass.None:
						case SpellDmgClass.Magic:
							if (spell._spellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
							{
								if (positive)
								{
									procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
									procVictim.Or(ProcFlags.TakeHelpfulPeriodic);
								}
								else
								{
									procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
									procVictim.Or(ProcFlags.TakeHarmfulPeriodic);
								}
							}
							else if (spell._spellInfo.HasAttribute(SpellAttr0.IsAbility))
							{
								if (positive)
								{
									procAttacker.Or(ProcFlags.DealHelpfulAbility);
									procVictim.Or(ProcFlags.TakeHelpfulAbility);
								}
								else
								{
									procAttacker.Or(ProcFlags.DealHarmfulAbility);
									procVictim.Or(ProcFlags.TakeHarmfulAbility);
								}
							}
							else
							{
								if (positive)
								{
									procAttacker.Or(ProcFlags.DealHelpfulSpell);
									procVictim.Or(ProcFlags.TakeHelpfulSpell);
								}
								else
								{
									procAttacker.Or(ProcFlags.DealHarmfulSpell);
									procVictim.Or(ProcFlags.TakeHarmfulSpell);
								}
							}

							break;
					}
				}

				// All calculated do it!
				// Do healing
				bool       hasHealing      = false;
				DamageInfo spellDamageInfo = null;
				HealInfo   healInfo        = null;

				if (spell._healing > 0)
				{
					hasHealing = true;
					int addhealth = spell._healing;

					if (IsCrit)
					{
						hitMask   |= ProcFlagsHit.Critical;
						addhealth =  Unit.SpellCriticalHealingBonus(caster, spell._spellInfo, addhealth, null);
					}
					else
					{
						hitMask |= ProcFlagsHit.Normal;
					}

					healInfo = new HealInfo(caster, spell.unitTarget, (uint)addhealth, spell._spellInfo, spell._spellInfo.GetSchoolMask());
					caster.HealBySpell(healInfo, IsCrit);
					spell.unitTarget.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, spell._spellInfo);
					spell._healing = (int)healInfo.GetEffectiveHeal();

					procSpellType |= ProcFlagsSpellType.Heal;
				}

				// Do damage
				bool hasDamage = false;

				if (spell._damage > 0)
				{
					hasDamage = true;
					// Fill base damage struct (unitTarget - is real spell target)
					SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell._spellInfo, spell._SpellVisual, spell._spellSchoolMask, spell._castId);

					// Check damage immunity
					if (spell.unitTarget.IsImmunedToDamage(spell._spellInfo))
					{
						hitMask       = ProcFlagsHit.Immune;
						spell._damage = 0;

						// no packet found in sniffs
					}
					else
					{
						caster.SetLastDamagedTargetGuid(spell.unitTarget.GetGUID());

						// Add bonuses and fill damageInfo struct
						caster.CalculateSpellDamageTaken(damageInfo, spell._damage, spell._spellInfo, spell._attackType, IsCrit, MissCondition == SpellMissInfo.Block, spell);
						Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

						hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
						procVictim.Or(ProcFlags.TakeAnyDamage);

						spell._damage = (int)damageInfo.damage;

						caster.DealSpellDamage(damageInfo, true);

						// Send log damage message to client
						caster.SendSpellNonMeleeDamageLog(damageInfo);
					}

					// Do triggers for unit
					if (canEffectTrigger)
					{
						spellDamageInfo =  new DamageInfo(damageInfo, DamageEffectType.SpellDirect, spell._attackType, hitMask);
						procSpellType   |= ProcFlagsSpellType.Damage;
					}
				}

				// Passive spell hits/misses or active spells only misses (only triggers)
				if (!hasHealing &&
				    !hasDamage)
				{
					// Fill base damage struct (unitTarget - is real spell target)
					SpellNonMeleeDamage damageInfo = new(caster, spell.unitTarget, spell._spellInfo, spell._SpellVisual, spell._spellSchoolMask);
					hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);

					// Do triggers for unit
					if (canEffectTrigger)
					{
						spellDamageInfo =  new DamageInfo(damageInfo, DamageEffectType.NoDamage, spell._attackType, hitMask);
						procSpellType   |= ProcFlagsSpellType.NoDmgHeal;
					}

					// Failed Pickpocket, reveal rogue
					if (MissCondition == SpellMissInfo.Resist &&
					    spell._spellInfo.HasAttribute(SpellCustomAttributes.PickPocket) &&
					    spell.unitTarget.IsCreature())
					{
						Unit unitCaster = spell.GetCaster().ToUnit();
						unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);
						spell.unitTarget.ToCreature().EngageWithTarget(unitCaster);
					}
				}

				// Do triggers for unit
				if (canEffectTrigger)
				{
					if (spell._spellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
						procAttacker = new ProcFlagsInit();

					if (spell._spellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs))
						procVictim = new ProcFlagsInit();

					Unit.ProcSkillsAndAuras(caster, spell.unitTarget, procAttacker, procVictim, procSpellType, ProcFlagsSpellPhase.Hit, hitMask, spell, spellDamageInfo, healInfo);

					// item spells (spell hit of non-damage spell may also activate items, for example seal of corruption hidden hit)
					if (caster.IsPlayer() &&
					    procSpellType.HasAnyFlag(ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal))
						if (spell._spellInfo.DmgClass == SpellDmgClass.Melee ||
						    spell._spellInfo.DmgClass == SpellDmgClass.Ranged)
							if (!spell._spellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat) &&
							    !spell._spellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs))
								caster.ToPlayer().CastItemCombatSpell(spellDamageInfo);
				}

				// set hitmask for finish procs
				spell._hitMask |= hitMask;

				// Do not take combo points on dodge and miss
				if (MissCondition != SpellMissInfo.None &&
				    spell._needComboPoints &&
				    spell._targets.GetUnitTargetGUID() == TargetGUID)
					spell._needComboPoints = false;

				// _spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
				if (MissCondition != SpellMissInfo.Evade &&
				    _spellHitTarget &&
				    !spell.GetCaster().IsFriendlyTo(unit) &&
				    (!spell.IsPositive() || spell._spellInfo.HasEffect(SpellEffectName.Dispel)))
				{
					Unit unitCaster = spell.GetCaster().ToUnit();

					if (unitCaster != null)
					{
						unitCaster.AtTargetAttacked(unit, spell._spellInfo.HasInitialAggro());

						if (spell._spellInfo.HasAttribute(SpellAttr6.TapsImmediately))
						{
							Creature targetCreature = unit.ToCreature();

							if (targetCreature != null)
								if (unitCaster.IsPlayer())
									targetCreature.SetTappedBy(unitCaster);
						}
					}

					if (!spell._spellInfo.HasAttribute(SpellAttr3.DoNotTriggerTargetStand) &&
					    !unit.IsStandState())
						unit.SetStandState(UnitStandStateType.Stand);
				}

				// Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
				if (MissCondition == SpellMissInfo.None &&
				    spell._spellInfo.HasAttribute(SpellAttr7.InterruptOnlyNonplayer) &&
				    !unit.IsPlayer())
					caster.CastSpell(unit, 32747, new CastSpellExtraArgs(spell));
			}

			if (_spellHitTarget)
			{
				//AI functions
				Creature cHitTarget = _spellHitTarget.ToCreature();

				if (cHitTarget != null)
				{
					CreatureAI hitTargetAI = cHitTarget.GetAI();

					if (hitTargetAI != null)
						hitTargetAI.SpellHit(spell.GetCaster(), spell._spellInfo);
				}

				if (spell.GetCaster().IsCreature() &&
				    spell.GetCaster().ToCreature().IsAIEnabled())
					spell.GetCaster().ToCreature().GetAI().SpellHitTarget(_spellHitTarget, spell._spellInfo);
				else if (spell.GetCaster().IsGameObject() &&
				         spell.GetCaster().ToGameObject().GetAI() != null)
					spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(_spellHitTarget, spell._spellInfo);

				if (HitAura != null)
				{
					AuraApplication aurApp = HitAura.GetApplicationOfTarget(_spellHitTarget.GetGUID());

					if (aurApp != null)
					{
						// only apply unapplied effects (for reapply case)
						uint effMask = EffectMask & aurApp.GetEffectsToApply();

						for (uint i = 0; i < spell._spellInfo.GetEffects().Count; ++i)
							if ((effMask & (1 << (int)i)) != 0 &&
							    aurApp.HasEffect(i))
								effMask &= ~(1u << (int)i);

						if (effMask != 0)
							_spellHitTarget._ApplyAura(aurApp, effMask);
					}
				}

				// Needs to be called after dealing damage/healing to not remove breaking on damage auras
				spell.DoTriggersOnSpellHit(_spellHitTarget);
			}

			if (_enablePVP)
				spell.GetCaster().ToPlayer().UpdatePvP(true);

			spell.spellAura = HitAura;
			spell.CallScriptAfterHitHandlers();
			spell.spellAura = null;
		}
	}

	public class GOTargetInfo : TargetInfoBase
	{
		public ObjectGuid TargetGUID;
		public ulong TimeDelay;

		public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
		{
			GameObject go = spell.GetCaster().GetGUID() == TargetGUID ? spell.GetCaster().ToGameObject() : ObjectAccessor.GetGameObject(spell.GetCaster(), TargetGUID);

			if (go == null)
				return;

			spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

			spell.HandleEffects(null, null, go, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

			//AI functions
			if (go.GetAI() != null)
				go.GetAI().SpellHit(spell.GetCaster(), spell._spellInfo);

			if (spell.GetCaster().IsCreature() &&
			    spell.GetCaster().ToCreature().IsAIEnabled())
				spell.GetCaster().ToCreature().GetAI().SpellHitTarget(go, spell._spellInfo);
			else if (spell.GetCaster().IsGameObject() &&
			         spell.GetCaster().ToGameObject().GetAI() != null)
				spell.GetCaster().ToGameObject().GetAI().SpellHitTarget(go, spell._spellInfo);

			spell.CallScriptOnHitHandlers();
			spell.CallScriptAfterHitHandlers();
		}
	}

	public class ItemTargetInfo : TargetInfoBase
	{
		public Item TargetItem;

		public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
		{
			spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

			spell.HandleEffects(null, TargetItem, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

			spell.CallScriptOnHitHandlers();
			spell.CallScriptAfterHitHandlers();
		}
	}

	public class CorpseTargetInfo : TargetInfoBase
	{
		public ObjectGuid TargetGUID;
		public ulong TimeDelay;

		public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
		{
			Corpse corpse = ObjectAccessor.GetCorpse(spell.GetCaster(), TargetGUID);

			if (corpse == null)
				return;

			spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

			spell.HandleEffects(null, null, null, corpse, spellEffectInfo, SpellEffectHandleMode.HitTarget);

			spell.CallScriptOnHitHandlers();
			spell.CallScriptAfterHitHandlers();
		}
	}

	public class SpellValue
	{
		public int AuraStackAmount;
		public float CriticalChance;
		public uint CustomBasePointsMask;
		public int? Duration;
		public float DurationMul;

		public int[] EffectBasePoints = new int[SpellConst.MaxEffects];
		public uint MaxAffectedTargets;
		public float RadiusMod;

		public SpellValue(SpellInfo proto, WorldObject caster)
		{
			foreach (var spellEffectInfo in proto.GetEffects())
				EffectBasePoints[spellEffectInfo.EffectIndex] = spellEffectInfo.CalcBaseValue(caster, null, 0, -1);

			CustomBasePointsMask = 0;
			MaxAffectedTargets   = proto.MaxAffectedTargets;
			RadiusMod            = 1.0f;
			AuraStackAmount      = 1;
			CriticalChance       = 0.0f;
			DurationMul          = 1;
		}
	}

	// Spell modifier (used for modify other spells)
	public class SpellModifier
	{
		public SpellModifier(Aura _ownerAura)
		{
			op        = SpellModOp.HealingAndDamage;
			type      = SpellModType.Flat;
			spellId   = 0;
			ownerAura = _ownerAura;
		}

		public SpellModOp op { get; set; }
		public SpellModType type { get; set; }
		public uint spellId { get; set; }
		public Aura ownerAura { get; set; }
	}

	public class SpellModifierByClassMask : SpellModifier
	{
		public FlagArray128 mask;

		public int value;

		public SpellModifierByClassMask(Aura _ownerAura) : base(_ownerAura)
		{
			value = 0;
			mask  = new FlagArray128();
		}
	}

	public class SpellFlatModifierByLabel : SpellModifier
	{
		public SpellFlatModByLabel value = new();

		public SpellFlatModifierByLabel(Aura _ownerAura) : base(_ownerAura)
		{
		}
	}

	internal class SpellPctModifierByLabel : SpellModifier
	{
		public SpellPctModByLabel value = new();

		public SpellPctModifierByLabel(Aura _ownerAura) : base(_ownerAura)
		{
		}
	}

	public class WorldObjectSpellTargetCheck : ICheck<WorldObject>
	{
		internal WorldObject _caster;
		private List<Condition> _condList;
		private ConditionSourceInfo _condSrcInfo;
		private SpellTargetObjectTypes _objectType;
		private WorldObject _referer;
		internal SpellInfo _spellInfo;
		private SpellTargetCheckTypes _targetSelectionType;

		public WorldObjectSpellTargetCheck(WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
		{
			_caster              = caster;
			_referer             = referer;
			_spellInfo           = spellInfo;
			_targetSelectionType = selectionType;
			_condList            = condList;
			_objectType          = objectType;

			if (condList != null)
				_condSrcInfo = new ConditionSourceInfo(null, caster);
		}

		public virtual bool Invoke(WorldObject target)
		{
			if (_spellInfo.CheckTarget(_caster, target, true) != SpellCastResult.SpellCastOk)
				return false;

			Unit   unitTarget   = target.ToUnit();
			Corpse corpseTarget = target.ToCorpse();

			if (corpseTarget != null)
			{
				// use owner for party/assistance checks
				Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());

				if (owner != null)
					unitTarget = owner;
				else
					return false;
			}

			Unit refUnit = _referer.ToUnit();

			if (unitTarget != null)
			{
				// do only faction checks here
				switch (_targetSelectionType)
				{
					case SpellTargetCheckTypes.Enemy:
						if (unitTarget.IsTotem())
							return false;

						// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
						if (!target.IsCorpse() &&
						    !_caster.IsValidAttackTarget(unitTarget, _spellInfo))
							return false;

						break;
					case SpellTargetCheckTypes.Ally:
						if (unitTarget.IsTotem())
							return false;

						// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
						if (!target.IsCorpse() &&
						    !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
							return false;

						break;
					case SpellTargetCheckTypes.Party:
						if (refUnit == null)
							return false;

						if (unitTarget.IsTotem())
							return false;

						// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
						if (!target.IsCorpse() &&
						    !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
							return false;

						if (!refUnit.IsInPartyWith(unitTarget))
							return false;

						break;
					case SpellTargetCheckTypes.RaidClass:
						if (!refUnit)
							return false;

						if (refUnit.GetClass() != unitTarget.GetClass())
							return false;

						goto case SpellTargetCheckTypes.Raid;
					case SpellTargetCheckTypes.Raid:
						if (refUnit == null)
							return false;

						if (unitTarget.IsTotem())
							return false;

						// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
						if (!target.IsCorpse() &&
						    !_caster.IsValidAssistTarget(unitTarget, _spellInfo))
							return false;

						if (!refUnit.IsInRaidWith(unitTarget))
							return false;

						break;
					case SpellTargetCheckTypes.Summoned:
						if (!unitTarget.IsSummon())
							return false;

						if (unitTarget.ToTempSummon().GetSummonerGUID() != _caster.GetGUID())
							return false;

						break;
					case SpellTargetCheckTypes.Threat:
						if (!_referer.IsUnit() ||
						    _referer.ToUnit().GetThreatManager().GetThreat(unitTarget, true) <= 0.0f)
							return false;

						break;
					case SpellTargetCheckTypes.Tap:
						if (_referer.GetTypeId() != TypeId.Unit ||
						    unitTarget.GetTypeId() != TypeId.Player)
							return false;

						if (!_referer.ToCreature().IsTappedBy(unitTarget.ToPlayer()))
							return false;

						break;
					default:
						break;
				}

				switch (_objectType)
				{
					case SpellTargetObjectTypes.Corpse:
					case SpellTargetObjectTypes.CorpseAlly:
					case SpellTargetObjectTypes.CorpseEnemy:
						if (unitTarget.IsAlive())
							return false;

						break;
					default:
						break;
				}
			}

			if (_condSrcInfo == null)
				return true;

			_condSrcInfo.mConditionTargets[0] = target;

			return Global.ConditionMgr.IsObjectMeetToConditions(_condSrcInfo, _condList);
		}
	}

	public class WorldObjectSpellNearbyTargetCheck : WorldObjectSpellTargetCheck
	{
		private Position _position;
		private float _range;

		public WorldObjectSpellNearbyTargetCheck(float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
			: base(caster, caster, spellInfo, selectionType, condList, objectType)
		{
			_range    = range;
			_position = caster.GetPosition();
		}

		public override bool Invoke(WorldObject target)
		{
			float dist = target.GetDistance(_position);

			if (dist < _range &&
			    base.Invoke(target))
			{
				_range = dist;

				return true;
			}

			return false;
		}
	}

	public class WorldObjectSpellAreaTargetCheck : WorldObjectSpellTargetCheck
	{
		private Position _position;
		private float _range;

		public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
			: base(caster, referer, spellInfo, selectionType, condList, objectType)
		{
			_range    = range;
			_position = position;
		}

		public override bool Invoke(WorldObject target)
		{
			if (target.ToGameObject())
			{
				// isInRange including the dimension of the GO
				bool isInRange = target.ToGameObject().IsInRange(_position.GetPositionX(), _position.GetPositionY(), _position.GetPositionZ(), _range);

				if (!isInRange)
					return false;
			}
			else
			{
				bool isInsideCylinder = target.IsWithinDist2d(_position, _range) && Math.Abs(target.GetPositionZ() - _position.GetPositionZ()) <= _range;

				if (!isInsideCylinder)
					return false;
			}

			return base.Invoke(target);
		}
	}

	public class WorldObjectSpellConeTargetCheck : WorldObjectSpellAreaTargetCheck
	{
		private float _coneAngle;
		private Position _coneSrc;
		private float _lineWidth;

		public WorldObjectSpellConeTargetCheck(Position coneSrc, float coneAngle, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
			: base(range, caster.GetPosition(), caster, caster, spellInfo, selectionType, condList, objectType)
		{
			_coneSrc   = coneSrc;
			_coneAngle = coneAngle;
			_lineWidth = lineWidth;
		}

		public override bool Invoke(WorldObject target)
		{
			if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeBack))
			{
				if (_coneSrc.HasInArc(-Math.Abs(_coneAngle), target))
					return false;
			}
			else if (_spellInfo.HasAttribute(SpellCustomAttributes.ConeLine))
			{
				if (!_coneSrc.HasInLine(target, target.GetCombatReach(), _lineWidth))
					return false;
			}
			else
			{
				if (!_caster.IsUnit() ||
				    !_caster.ToUnit().IsWithinBoundaryRadius(target.ToUnit()))
					// ConeAngle > 0 . select targets in front
					// ConeAngle < 0 . select targets in back
					if (_coneSrc.HasInArc(_coneAngle, target) != MathFunctions.fuzzyGe(_coneAngle, 0.0f))
						return false;
			}

			return base.Invoke(target);
		}
	}

	public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellTargetCheck
	{
		private Position _position;
		private float _range;

		public WorldObjectSpellTrajTargetCheck(float range, Position position, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
			: base(caster, caster, spellInfo, selectionType, condList, objectType)
		{
			_range    = range;
			_position = position;
		}

		public override bool Invoke(WorldObject target)
		{
			// return all targets on missile trajectory (0 - size of a missile)
			if (!_caster.HasInLine(target, target.GetCombatReach(), SpellConst.TrajectoryMissileSize))
				return false;

			if (target.GetExactDist2d(_position) > _range)
				return false;

			return base.Invoke(target);
		}
	}

	public class WorldObjectSpellLineTargetCheck : WorldObjectSpellAreaTargetCheck
	{
		private float _lineWidth;
		private Position _position;

		public WorldObjectSpellLineTargetCheck(Position srcPosition, Position dstPosition, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
			: base(range, caster, caster, caster, spellInfo, selectionType, condList, objectType)
		{
			_position  = srcPosition;
			_lineWidth = lineWidth;

			if (dstPosition != null &&
			    srcPosition != dstPosition)
				_position.SetOrientation(srcPosition.GetAbsoluteAngle(dstPosition));
		}

		public override bool Invoke(WorldObject target)
		{
			if (!_position.HasInLine(target, target.GetCombatReach(), _lineWidth))
				return false;

			return base.Invoke(target);
		}
	}

	public class SpellEvent : BasicEvent
	{
		private Spell _Spell;

		public SpellEvent(Spell spell)
		{
			_Spell = spell;
		}

		public override bool Execute(ulong e_time, uint p_time)
		{
			// update spell if it is not finished
			if (_Spell.GetState() != SpellState.Finished)
				_Spell.Update(p_time);

			// check spell state to process
			switch (_Spell.GetState())
			{
				case SpellState.Finished:
				{
					// spell was finished, check deletable state
					if (_Spell.IsDeletable())
						// check, if we do have unfinished triggered spells
						return true; // spell is deletable, finish event

					// event will be re-added automatically at the end of routine)
					break;
				}
				case SpellState.Delayed:
				{
					// first, check, if we have just started
					if (_Spell.GetDelayStart() != 0)
					{
						// run the spell handler and think about what we can do next
						ulong t_offset = e_time - _Spell.GetDelayStart();
						ulong n_offset = _Spell.HandleDelayed(t_offset);

						if (n_offset != 0)
						{
							// re-add us to the queue
							_Spell.GetCaster()._Events.AddEvent(this, TimeSpan.FromMilliseconds(_Spell.GetDelayStart() + n_offset), false);

							return false; // event not complete
						}
						// event complete
						// finish update event will be re-added automatically at the end of routine)
					}
					else
					{
						// delaying had just started, record the moment
						_Spell.SetDelayStart(e_time);
						// handle effects on caster if the spell has travel time but also affects the caster in some way
						ulong n_offset = _Spell.HandleDelayed(0);

						if (_Spell._spellInfo.LaunchDelay != 0)
							Cypher.Assert(n_offset == (ulong)Math.Floor(_Spell._spellInfo.LaunchDelay * 1000.0f));
						else
							Cypher.Assert(n_offset == _Spell.GetDelayMoment());

						// re-plan the event for the delay moment
						_Spell.GetCaster()._Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + n_offset), false);

						return false; // event not complete
					}

					break;
				}
				default:
				{
					// all other states
					// event will be re-added automatically at the end of routine)
					break;
				}
			}

			// spell processing not complete, plan event on the next update interval
			_Spell.GetCaster()._Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + 1), false);

			return false; // event not complete
		}

		public override void Abort(ulong e_time)
		{
			// oops, the spell we try to do is aborted
			if (_Spell.GetState() != SpellState.Finished)
				_Spell.Cancel();
		}

		public override bool IsDeletable()
		{
			return _Spell.IsDeletable();
		}

		public Spell GetSpell()
		{
			return _Spell;
		}
	}

	internal class ProcReflectDelayed : BasicEvent
	{
		private ObjectGuid _casterGuid;

		private Unit _victim;

		public ProcReflectDelayed(Unit owner, ObjectGuid casterGuid)
		{
			_victim     = owner;
			_casterGuid = casterGuid;
		}

		public override bool Execute(ulong e_time, uint p_time)
		{
			Unit caster = Global.ObjAccessor.GetUnit(_victim, _casterGuid);

			if (!caster)
				return true;

			ProcFlags           typeMaskActor        = ProcFlags.None;
			ProcFlags           typeMaskActionTarget = ProcFlags.TakeHarmfulSpell | ProcFlags.TakeHarmfulAbility;
			ProcFlagsSpellType  spellTypeMask        = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
			ProcFlagsSpellPhase spellPhaseMask       = ProcFlagsSpellPhase.None;
			ProcFlagsHit        hitMask              = ProcFlagsHit.Reflect;

			Unit.ProcSkillsAndAuras(caster, _victim, new ProcFlagsInit(typeMaskActor), new ProcFlagsInit(typeMaskActionTarget), spellTypeMask, spellPhaseMask, hitMask, null, null, null);

			return true;
		}
	}

	public class CastSpellTargetArg
	{
		public SpellCastTargets Targets;

		public CastSpellTargetArg()
		{
			Targets = new SpellCastTargets();
		}

		public CastSpellTargetArg(WorldObject target)
		{
			if (target != null)
			{
				Unit unitTarget = target.ToUnit();

				if (unitTarget != null)
				{
					Targets = new SpellCastTargets();
					Targets.SetUnitTarget(unitTarget);
				}
				else
				{
					GameObject goTarget = target.ToGameObject();

					if (goTarget != null)
					{
						Targets = new SpellCastTargets();
						Targets.SetGOTarget(goTarget);
					}
					// error when targeting anything other than units and gameobjects
				}
			}
			else
			{
				Targets = new SpellCastTargets(); // nullptr is allowed
			}
		}

		public CastSpellTargetArg(Item itemTarget)
		{
			Targets = new SpellCastTargets();
			Targets.SetItemTarget(itemTarget);
		}

		public CastSpellTargetArg(Position dest)
		{
			Targets = new SpellCastTargets();
			Targets.SetDst(dest);
		}

		public CastSpellTargetArg(SpellCastTargets targets)
		{
			Targets = new SpellCastTargets();
			Targets = targets;
		}
	}

	public class CastSpellExtraArgs
	{
		public Difficulty CastDifficulty;
		public Item CastItem;
		public object CustomArg;
		public ObjectGuid OriginalCaster = ObjectGuid.Empty;
		public ObjectGuid OriginalCastId = ObjectGuid.Empty;
		public int? OriginalCastItemLevel;
		public Dictionary<SpellValueMod, int> SpellValueOverrides = new();
		public TriggerCastFlags TriggerFlags;
		public AuraEffect TriggeringAura;
		public Spell TriggeringSpell;

		public CastSpellExtraArgs()
		{
		}

		public CastSpellExtraArgs(bool triggered)
		{
			TriggerFlags = triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None;
		}

		public CastSpellExtraArgs(TriggerCastFlags trigger)
		{
			TriggerFlags = trigger;
		}

		public CastSpellExtraArgs(Item item)
		{
			TriggerFlags = TriggerCastFlags.FullMask;
			CastItem     = item;
		}

		public CastSpellExtraArgs(Spell triggeringSpell)
		{
			TriggerFlags = TriggerCastFlags.FullMask;
			SetTriggeringSpell(triggeringSpell);
		}

		public CastSpellExtraArgs(AuraEffect eff)
		{
			TriggerFlags = TriggerCastFlags.FullMask;
			SetTriggeringAura(eff);
		}

		public CastSpellExtraArgs(Difficulty castDifficulty)
		{
			CastDifficulty = castDifficulty;
		}

		public CastSpellExtraArgs(SpellValueMod mod, int val)
		{
			SpellValueOverrides.Add(mod, val);
		}

		public CastSpellExtraArgs SetTriggerFlags(TriggerCastFlags flag)
		{
			TriggerFlags = flag;

			return this;
		}

		public CastSpellExtraArgs SetCastItem(Item item)
		{
			CastItem = item;

			return this;
		}

		public CastSpellExtraArgs SetTriggeringSpell(Spell triggeringSpell)
		{
			TriggeringSpell = triggeringSpell;

			if (triggeringSpell != null)
			{
				OriginalCastItemLevel = triggeringSpell._castItemLevel;
				OriginalCastId        = triggeringSpell._castId;
			}

			return this;
		}

		public CastSpellExtraArgs SetTriggeringAura(AuraEffect triggeringAura)
		{
			TriggeringAura = triggeringAura;

			if (triggeringAura != null)
				OriginalCastId = triggeringAura.GetBase().GetCastId();

			return this;
		}

		public CastSpellExtraArgs SetOriginalCaster(ObjectGuid guid)
		{
			OriginalCaster = guid;

			return this;
		}

		public CastSpellExtraArgs SetCastDifficulty(Difficulty castDifficulty)
		{
			CastDifficulty = castDifficulty;

			return this;
		}

		public CastSpellExtraArgs SetOriginalCastId(ObjectGuid castId)
		{
			OriginalCastId = castId;

			return this;
		}

		public CastSpellExtraArgs AddSpellMod(SpellValueMod mod, int val)
		{
			SpellValueOverrides.Add(mod, val);

			return this;
		}

		public CastSpellExtraArgs SetCustomArg(object customArg)
		{
			CustomArg = customArg;

			return this;
		}
	}

	public class SpellLogEffect
	{
		public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets = new();
		public int Effect;
		public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets = new();
		public List<SpellLogEffectFeedPetParams> FeedPetTargets = new();
		public List<SpellLogEffectGenericVictimParams> GenericVictimTargets = new();

		public List<SpellLogEffectPowerDrainParams> PowerDrainTargets = new();
		public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets = new();
	}

	public class ProcFlagsInit : FlagsArray<int>
	{
		public ProcFlagsInit(ProcFlags procFlags = 0, ProcFlags2 procFlags2 = 0) : base(2)
		{
			_storage[0] = (int)procFlags;
			_storage[1] = (int)procFlags2;
		}

		public ProcFlagsInit(params int[] flags) : base(flags)
		{
		}

		public ProcFlagsInit Or(ProcFlags procFlags)
		{
			_storage[0] |= (int)procFlags;

			return this;
		}

		public ProcFlagsInit Or(ProcFlags2 procFlags2)
		{
			_storage[1] |= (int)procFlags2;

			return this;
		}

		public bool HasFlag(ProcFlags procFlags)
		{
			return (_storage[0] & (int)procFlags) != 0;
		}

		public bool HasFlag(ProcFlags2 procFlags)
		{
			return (_storage[1] & (int)procFlags) != 0;
		}
	}
}