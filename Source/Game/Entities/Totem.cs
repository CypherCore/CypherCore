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
using Game.DataStorage;
using Game.Groups;
using Game.Network.Packets;
using Game.Spells;
using System.Linq;

namespace Game.Entities
{
    public class Totem : Minion
    {
        public Totem(SummonPropertiesRecord properties, Unit owner) : base(properties, owner, false)
        {
            m_unitTypeMask |= UnitTypeMask.Totem;
            m_type = TotemType.Passive;
        }

        public override void Update(uint diff)
        {
            if (!GetOwner().IsAlive() || !IsAlive())
            {
                UnSummon();                                         // remove self
                return;
            }

            if (m_duration <= diff)
            {
                UnSummon();                                         // remove self
                return;
            }
            else
                m_duration -= diff;
            base.Update(diff);

        }

        public override void InitStats(uint duration)
        {
            // client requires SMSG_TOTEM_CREATED to be sent before adding to world and before removing old totem
            Player owner = GetOwner().ToPlayer();
            if (owner)
            {
                if (m_Properties.Slot >= (int)SummonSlot.Totem && m_Properties.Slot < SharedConst.MaxTotemSlot)
                {
                    TotemCreated packet = new TotemCreated();
                    packet.Totem = GetGUID();
                    packet.Slot = (byte)(m_Properties.Slot - (int)SummonSlot.Totem);
                    packet.Duration = duration;
                    packet.SpellID = GetUInt32Value(UnitFields.CreatedBySpell);
                    owner.ToPlayer().SendPacket(packet);
                }

                // set display id depending on caster's race
                uint totemDisplayId = Global.SpellMgr.GetModelForTotem(GetUInt32Value(UnitFields.CreatedBySpell), owner.GetRace());
                if (totemDisplayId == 0)
                    Log.outError(LogFilter.Spells, $"Spell {GetUInt32Value(UnitFields.CreatedBySpell)} with RaceID ({owner.GetRace()}) have no totem model data defined, set to default model.");
                else
                    SetDisplayId(totemDisplayId);
            }

            base.InitStats(duration);

            // Get spell cast by totem
            SpellInfo totemSpell = Global.SpellMgr.GetSpellInfo(GetSpell());
            if (totemSpell != null)
                if (totemSpell.CalcCastTime(getLevel()) != 0)   // If spell has cast time . its an active totem
                    m_type = TotemType.Active;

            m_duration = duration;

            SetLevel(GetOwner().getLevel());
        }

        public override void InitSummon()
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
                m_Events.AddEvent(new ForcedUnsummonDelayEvent(this), m_Events.CalculateTime(msTime));
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

                SpellInfo spell = Global.SpellMgr.GetSpellInfo(GetUInt32Value(UnitFields.CreatedBySpell));
                if (spell != null)
                    GetSpellHistory().SendCooldownEvent(spell, 0, null, false);

                Group group = owner.GetGroup();
                if (group)
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                    {
                        Player target = refe.GetSource();
                        if (target && group.SameSubGroup(owner, target))
                            target.RemoveAurasDueToSpell(GetSpell(), GetGUID());
                    }
                }
            }

            AddObjectToRemoveList();
        }

        public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, uint index, Unit caster)
        {
            // @todo possibly all negative auras immune?
            if (GetEntry() == 5925)
                return false;

            SpellEffectInfo effect = spellInfo.GetEffect(GetMap().GetDifficultyID(), index);
            if (effect == null)
                return true;

            switch (effect.ApplyAuraName)
            {
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicLeech:
                case AuraType.ModFear:
                case AuraType.Transform:
                    return true;
                default:
                    break;
            }

            return base.IsImmunedToSpellEffect(spellInfo, index, caster);
        }

        public uint GetSpell(byte slot = 0) { return m_spells[slot]; }

        public uint GetTotemDuration() { return m_duration; }

        public void SetTotemDuration(uint duration) { m_duration = duration; }

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
        uint m_duration;
    }

    public enum TotemType
    {
        Passive = 0,
        Active = 1,
        Statue = 2 // copied straight from MaNGOS, may need more implementation to work
    }
}
