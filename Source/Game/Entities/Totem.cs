// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Groups;
using Game.Networking.Packets;
using Game.Spells;
using System;

namespace Game.Entities
{
    public class Totem : Minion
    {
        public Totem(SummonPropertiesRecord properties, Unit owner) : base(properties, owner, false)
        {
            UnitTypeMask |= UnitTypeMask.Totem;
            m_type = TotemType.Passive;
        }

        public override void Update(uint diff)
        {
            if (!GetOwner().IsAlive() || !IsAlive())
            {
                UnSummon();                                         // remove self
                return;
            }

            if (m_duration <= TimeSpan.FromMilliseconds(diff))
            {
                UnSummon();                                         // remove self
                return;
            }

            m_duration -= TimeSpan.FromMilliseconds(diff);

            base.Update(diff);

        }

        public override void InitStats(WorldObject summoner, TimeSpan duration)
        {
            // client requires SMSG_TOTEM_CREATED to be sent before adding to world and before removing old totem
            Player owner = GetOwner().ToPlayer();
            if (owner != null)
            {
                int slot = m_Properties.Slot;
                if (slot == (int)SummonSlot.Any)
                    slot = FindUsableTotemSlot(owner);

                if (slot >= (int)SummonSlot.Totem && slot < SharedConst.MaxTotemSlot)
                {
                    TotemCreated packet = new();
                    packet.Totem = GetGUID();
                    packet.Slot = (byte)(slot - (int)SummonSlot.Totem);
                    packet.Duration = duration;
                    packet.SpellID = m_unitData.CreatedBySpell;
                    owner.ToPlayer().SendPacket(packet);
                }

                // set display id depending on caster's race
                uint totemDisplayId = Global.SpellMgr.GetModelForTotem(m_unitData.CreatedBySpell, owner.GetRace());
                if (totemDisplayId != 0)
                    SetDisplayId(totemDisplayId);
                else
                    Log.outDebug(LogFilter.Misc, $"Totem with entry {GetEntry()}, does not have a specialized model for spell {m_unitData.CreatedBySpell} and race {owner.GetRace()}. Set to default.");
            }

            base.InitStats(summoner, duration);

            // Get spell cast by totem
            SpellInfo totemSpell = Global.SpellMgr.GetSpellInfo(GetSpell(), GetMap().GetDifficultyID());
            if (totemSpell != null)
                if (totemSpell.CalcCastTime() != 0)   // If spell has cast time -> its an active totem
                    m_type = TotemType.Active;

            m_duration = duration;
        }

        public override void InitSummon(WorldObject summoner)
        {
            if (m_type == TotemType.Passive && GetSpell() != 0)
                CastSpell(this, GetSpell(), true);

            // Some totems can have both instant effect and passive spell
            if (GetSpell(1) != 0)
                CastSpell(this, GetSpell(1), true);
        }

        public override void UnSummon(uint msTime = 0)
        {
            if (msTime != 0)
            {
                m_Events.AddEventAtOffset(new ForcedDespawnDelayEvent(this), TimeSpan.FromMilliseconds(msTime));
                return;
            }

            CombatStop();
            RemoveAurasDueToSpell(GetSpell(), GetGUID());

            // clear owner's totem slot
            for (byte i = (int)SummonSlot.Totem; i < SharedConst.MaxTotemSlot; ++i)
            {
                if (GetOwner().m_SummonSlot[i] == GetGUID())
                {
                    GetOwner().m_SummonSlot[i].Clear();
                    break;
                }
            }

            GetOwner().RemoveAurasDueToSpell(GetSpell(), GetGUID());

            // remove aura all party members too
            Player owner = GetOwner().ToPlayer();
            if (owner != null)
            {
                owner.SendAutoRepeatCancel(this);

                SpellInfo spell = Global.SpellMgr.GetSpellInfo(m_unitData.CreatedBySpell, GetMap().GetDifficultyID());
                if (spell != null)
                    GetSpellHistory().SendCooldownEvent(spell, 0, null, false);

                Group group = owner.GetGroup();
                if (group != null)
                {
                    foreach (GroupReference groupRef in group.GetMembers())
                    {
                        Player target = groupRef.GetSource();
                        if (target.IsInMap(owner) && group.SameSubGroup(owner, target))
                            target.RemoveAurasDueToSpell(GetSpell(), GetGUID());
                    }
                }
            }

            AddObjectToRemoveList();
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
        {
            // immune to all positive spells, except of stoneclaw totem absorb and sentry totem bind sight
            // totems positive spells have unit_caster target
            if (spellEffectInfo.Effect != SpellEffectName.Dummy &&
                spellEffectInfo.Effect != SpellEffectName.ScriptEffect &&
                spellInfo.IsPositive() && spellEffectInfo.TargetA.GetTarget() != Targets.UnitCaster &&
                spellEffectInfo.TargetA.GetCheckType() != SpellTargetCheckTypes.Entry)
                return true;

            switch (spellEffectInfo.ApplyAuraName)
            {
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicLeech:
                case AuraType.ModFear:
                case AuraType.Transform:
                    return true;
                default:
                    break;
            }

            return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
        }

        public uint GetSpell(byte slot = 0) { return m_spells[slot]; }

        public TimeSpan GetTotemDuration() { return m_duration; }

        public void SetTotemDuration(TimeSpan duration) { m_duration = duration; }

        public TotemType GetTotemType() { return m_type; }

        public override bool UpdateStats(Stats stat) { return true; }

        public override bool UpdateAllStats() { return true; }

        public override void UpdateResistances(SpellSchools school) { }
        public override void UpdateArmor() { }
        public override void UpdateMaxHealth() { }
        public override void UpdateMaxPower(PowerType power) { }
        public override void UpdateAttackPowerAndDamage(bool ranged = false) { }
        public override void UpdateDamagePhysical(WeaponAttackType attType) { }

        TotemType m_type;
        TimeSpan m_duration;
    }

    public enum TotemType
    {
        Passive = 0,
        Active = 1,
        Statue = 2 // copied straight from MaNGOS, may need more implementation to work
    }
}
