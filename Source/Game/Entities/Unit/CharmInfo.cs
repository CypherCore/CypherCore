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

using Framework.Collections;
using Framework.Constants;
using Framework.GameMath;
using Game.Network;
using Game.Spells;
using System;

namespace Game.Entities
{
    public class CharmInfo
    {
        public CharmInfo(Unit unit)
        {
            _unit = unit;
            _CommandState = CommandStates.Follow;
            _petnumber = 0;
            _oldReactState = ReactStates.Passive;
            for (byte i = 0; i < SharedConst.MaxSpellCharm; ++i)
            {
                _charmspells[i] = new UnitActionBarEntry();
                _charmspells[i].SetActionAndType(0, ActiveStates.Disabled);
            }

            for (var i = 0; i < SharedConst.ActionBarIndexMax; ++i)
                PetActionBar[i] = new UnitActionBarEntry();

            if (_unit.IsTypeId(TypeId.Unit))
            {
                _oldReactState = _unit.ToCreature().GetReactState();
                _unit.ToCreature().SetReactState(ReactStates.Passive);
            }
        }

        public void RestoreState()
        {
            if (_unit.IsTypeId(TypeId.Unit))
            {
                Creature creature = _unit.ToCreature();
                if (creature)
                    creature.SetReactState(_oldReactState);
            }
        }

        public void InitPetActionBar()
        {

            // the first 3 SpellOrActions are attack, follow and stay
            for (byte i = 0; i < SharedConst.ActionBarIndexPetSpellStart - SharedConst.ActionBarIndexStart; ++i)
                SetActionBar((byte)(SharedConst.ActionBarIndexStart + i), (uint)CommandStates.Attack - i, ActiveStates.Command);

            // middle 4 SpellOrActions are spells/special attacks/abilities
            for (byte i = 0; i < SharedConst.ActionBarIndexPetSpellEnd - SharedConst.ActionBarIndexPetSpellStart; ++i)
                SetActionBar((byte)(SharedConst.ActionBarIndexPetSpellStart + i), 0, ActiveStates.Passive);

            // last 3 SpellOrActions are reactions
            for (byte i = 0; i < SharedConst.ActionBarIndexEnd - SharedConst.ActionBarIndexPetSpellEnd; ++i)
                SetActionBar((byte)(SharedConst.ActionBarIndexPetSpellEnd + i), (uint)CommandStates.Attack - i, ActiveStates.Reaction);
        }

        public void InitEmptyActionBar(bool withAttack = true)
        {
            if (withAttack)
                SetActionBar(SharedConst.ActionBarIndexStart, (uint)CommandStates.Attack, ActiveStates.Command);
            else
                SetActionBar(SharedConst.ActionBarIndexStart, 0, ActiveStates.Passive);
            for (byte x = SharedConst.ActionBarIndexStart + 1; x < SharedConst.ActionBarIndexEnd; ++x)
                SetActionBar(x, 0, ActiveStates.Passive);
        }

        public void InitPossessCreateSpells()
        {
            if (_unit.IsTypeId(TypeId.Unit))
            {
                // Adding switch until better way is found. Malcrom
                // Adding entrys to this switch will prevent COMMAND_ATTACK being added to pet bar.
                switch (_unit.GetEntry())
                {
                    case 23575: // Mindless Abomination
                    case 24783: // Trained Rock Falcon
                    case 27664: // Crashin' Thrashin' Racer
                    case 40281: // Crashin' Thrashin' Racer
                    case 28511: // Eye of Acherus
                        break;
                    default:
                        InitEmptyActionBar();
                        break;
                }

                for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
                {
                    uint spellId = _unit.ToCreature().m_spells[i];
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);
                    if (spellInfo != null)
                    {
                        if (spellInfo.IsPassive())
                            _unit.CastSpell(_unit, spellInfo, true);
                        else
                            AddSpellToActionBar(spellInfo, ActiveStates.Passive, i % SharedConst.ActionBarIndexMax);
                    }
                }
            }
            else
                InitEmptyActionBar();
        }

        public void InitCharmCreateSpells()
        {
            if (_unit.IsTypeId(TypeId.Player))                // charmed players don't have spells
            {
                InitEmptyActionBar();
                return;
            }

            InitPetActionBar();

            for (uint x = 0; x < SharedConst.MaxSpellCharm; ++x)
            {
                uint spellId = _unit.ToCreature().m_spells[x];
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId);

                if (spellInfo == null)
                {
                    _charmspells[x].SetActionAndType(spellId, ActiveStates.Disabled);
                    continue;
                }

                if (spellInfo.IsPassive())
                {
                    _unit.CastSpell(_unit, spellInfo, true);
                    _charmspells[x].SetActionAndType(spellId, ActiveStates.Passive);
                }
                else
                {
                    _charmspells[x].SetActionAndType(spellId, ActiveStates.Disabled);

                    ActiveStates newstate = ActiveStates.Passive;

                    if (!spellInfo.IsAutocastable())
                        newstate = ActiveStates.Passive;
                    else
                    {
                        if (spellInfo.NeedsExplicitUnitTarget())
                        {
                            newstate = ActiveStates.Enabled;
                            ToggleCreatureAutocast(spellInfo, true);
                        }
                        else
                            newstate = ActiveStates.Disabled;
                    }

                    AddSpellToActionBar(spellInfo, newstate);
                }
            }
        }

        public bool AddSpellToActionBar(SpellInfo spellInfo, ActiveStates newstate = ActiveStates.Decide, int preferredSlot = 0)
        {
            uint spell_id = spellInfo.Id;
            uint first_id = spellInfo.GetFirstRankSpell().Id;

            // new spell rank can be already listed
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                uint action = PetActionBar[i].GetAction();
                if (action != 0)
                {
                    if (PetActionBar[i].IsActionBarForSpell() && Global.SpellMgr.GetFirstSpellInChain(action) == first_id)
                    {
                        PetActionBar[i].SetAction(spell_id);
                        return true;
                    }
                }
            }

            // or use empty slot in other case
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                byte j = (byte)((preferredSlot + i) % SharedConst.ActionBarIndexMax);
                if (PetActionBar[j].GetAction() == 0 && PetActionBar[j].IsActionBarForSpell())
                {
                    SetActionBar(j, spell_id, newstate == ActiveStates.Decide ? spellInfo.IsAutocastable() ? ActiveStates.Disabled : ActiveStates.Passive : newstate);
                    return true;
                }
            }
            return false;
        }

        public bool RemoveSpellFromActionBar(uint spell_id)
        {
            uint first_id = Global.SpellMgr.GetFirstSpellInChain(spell_id);

            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                uint action = PetActionBar[i].GetAction();
                if (action != 0)
                {
                    if (PetActionBar[i].IsActionBarForSpell() && Global.SpellMgr.GetFirstSpellInChain(action) == first_id)
                    {
                        SetActionBar(i, 0, ActiveStates.Passive);
                        return true;
                    }
                }
            }

            return false;
        }

        public void ToggleCreatureAutocast(SpellInfo spellInfo, bool apply)
        {
            if (spellInfo.IsPassive())
                return;

            for (uint x = 0; x < SharedConst.MaxSpellCharm; ++x)
                if (spellInfo.Id == _charmspells[x].GetAction())
                    _charmspells[x].SetType(apply ? ActiveStates.Enabled : ActiveStates.Disabled);
        }

        public void SetPetNumber(uint petnumber, bool statwindow)
        {
            _petnumber = petnumber;
            if (statwindow)
                _unit.SetUInt32Value(UnitFields.PetNumber, _petnumber);
            else
                _unit.SetUInt32Value(UnitFields.PetNumber, 0);
        }

        public void LoadPetActionBar(string data)
        {
            InitPetActionBar();

            var tokens = new StringArray(data, ' ');
            if (tokens.Length != (SharedConst.ActionBarIndexEnd - SharedConst.ActionBarIndexStart) * 2)
                return;                                             // non critical, will reset to default

            byte index = 0;
            for (byte i = 0; i < tokens.Length && index < SharedConst.ActionBarIndexEnd; ++i, ++index)
            {
                ActiveStates type = tokens[i++].ToEnum<ActiveStates>();
                uint.TryParse(tokens[i], out uint action);

                PetActionBar[index].SetActionAndType(action, type);

                // check correctness
                if (PetActionBar[index].IsActionBarForSpell())
                {
                    SpellInfo spelInfo = Global.SpellMgr.GetSpellInfo(PetActionBar[index].GetAction());
                    if (spelInfo == null)
                        SetActionBar(index, 0, ActiveStates.Passive);
                    else if (!spelInfo.IsAutocastable())
                        SetActionBar(index, PetActionBar[index].GetAction(), ActiveStates.Passive);
                }
            }
        }

        public void BuildActionBar(WorldPacket data)
        {
            for (int i = 0; i < SharedConst.ActionBarIndexMax; ++i)
                data.WriteUInt32(PetActionBar[i].packedData);
        }

        public void SetSpellAutocast(SpellInfo spellInfo, bool state)
        {
            for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
            {
                if (spellInfo.Id == PetActionBar[i].GetAction() && PetActionBar[i].IsActionBarForSpell())
                {
                    PetActionBar[i].SetType(state ? ActiveStates.Enabled : ActiveStates.Disabled);
                    break;
                }
            }
        }

        public void SetIsCommandAttack(bool val)
        {
            _isCommandAttack = val;
        }

        public bool IsCommandAttack()
        {
            return _isCommandAttack;
        }

        public void SetIsCommandFollow(bool val)
        {
            _isCommandFollow = val;
        }

        public bool IsCommandFollow()
        {
            return _isCommandFollow;
        }

        public void SaveStayPosition()
        {
            //! At this point a new spline destination is enabled because of Unit.StopMoving()
            Vector3 stayPos = _unit.moveSpline.FinalDestination();

            if (_unit.moveSpline.onTransport)
            {
                float o = 0;
                ITransport transport = _unit.GetDirectTransport();
                if (transport != null)
                    transport.CalculatePassengerPosition(ref stayPos.X, ref stayPos.Y, ref stayPos.Z, ref o);
            }

            _stayX = stayPos.X;
            _stayY = stayPos.Y;
            _stayZ = stayPos.Z;
        }

        public void GetStayPosition(out float x, out float y, out float z)
        {
            x = _stayX;
            y = _stayY;
            z = _stayZ;
        }

        public void SetIsAtStay(bool val)
        {
            _isAtStay = val;
        }

        public bool IsAtStay()
        {
            return _isAtStay;
        }

        public void SetIsFollowing(bool val)
        {
            _isFollowing = val;
        }

        public bool IsFollowing()
        {
            return _isFollowing;
        }

        public void SetIsReturning(bool val)
        {
            _isReturning = val;
        }

        public bool IsReturning()
        {
            return _isReturning;
        }

        public uint GetPetNumber() { return _petnumber; }
        public void SetCommandState(CommandStates st) { _CommandState = st; }
        public CommandStates GetCommandState() { return _CommandState; }
        public bool HasCommandState(CommandStates state) { return (_CommandState == state); }

        public void SetActionBar(byte index, uint spellOrAction, ActiveStates type)
        {
            PetActionBar[index].SetActionAndType(spellOrAction, type);
        }
        public UnitActionBarEntry GetActionBarEntry(byte index) { return PetActionBar[index]; }

        public UnitActionBarEntry GetCharmSpell(byte index) { return _charmspells[index]; }

        Unit _unit;
        UnitActionBarEntry[] PetActionBar = new UnitActionBarEntry[SharedConst.ActionBarIndexMax];
        UnitActionBarEntry[] _charmspells = new UnitActionBarEntry[4];
        CommandStates _CommandState;
        uint _petnumber;

        ReactStates _oldReactState;

        bool _isCommandAttack;
        bool _isCommandFollow;
        bool _isAtStay;
        bool _isFollowing;
        bool _isReturning;
        float _stayX;
        float _stayY;
        float _stayZ;
    }

    public class UnitActionBarEntry
    {
        public UnitActionBarEntry()
        {
            packedData = (uint)ActiveStates.Disabled << 24;
        }

        public ActiveStates GetActiveState() { return (ActiveStates)UNIT_ACTION_BUTTON_TYPE(packedData); }

        public uint GetAction() { return UNIT_ACTION_BUTTON_ACTION(packedData); }

        public bool IsActionBarForSpell()
        {
            ActiveStates Type = GetActiveState();
            return Type == ActiveStates.Disabled || Type == ActiveStates.Enabled || Type == ActiveStates.Passive;
        }

        public void SetActionAndType(uint action, ActiveStates type)
        {
            packedData = MAKE_UNIT_ACTION_BUTTON(action, (uint)type);
        }

        public void SetType(ActiveStates type)
        {
            packedData = MAKE_UNIT_ACTION_BUTTON(UNIT_ACTION_BUTTON_ACTION(packedData), (uint)type);
        }

        public void SetAction(uint action)
        {
            packedData = (packedData & 0xFF000000) | UNIT_ACTION_BUTTON_ACTION(action);
        }

        public uint packedData;

        public static uint MAKE_UNIT_ACTION_BUTTON(uint action, uint type)
        {
            return (action | (type << 24));
        }
        public static uint UNIT_ACTION_BUTTON_ACTION(uint packedData)
        {
            return (packedData & 0x00FFFFFF);
        }
        public static uint UNIT_ACTION_BUTTON_TYPE(uint packedData)
        {
            return ((packedData & 0xFF000000) >> 24);
        }
    }

}
