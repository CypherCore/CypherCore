// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Framework.Dynamic;

namespace Game.Spells
{
    public class SpellEvent : BasicEvent
    {
        private readonly Spell _spell;

        public SpellEvent(Spell spell)
        {
            _spell = spell;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            // update spell if it is not finished
            if (_spell.GetState() != SpellState.Finished)
                _spell.Update(p_time);

            // check spell State to process
            switch (_spell.GetState())
            {
                case SpellState.Finished:
                    {
                        // spell was finished, check deletable State
                        if (_spell.IsDeletable())
                            // check, if we do have unfinished triggered spells
                            return true; // spell is deletable, finish event

                        // event will be re-added automatically at the end of routine)
                        break;
                    }
                case SpellState.Delayed:
                    {
                        // first, check, if we have just started
                        if (_spell.GetDelayStart() != 0)
                        {
                            // run the spell handler and think about what we can do next
                            ulong t_offset = e_time - _spell.GetDelayStart();
                            ulong n_offset = _spell.HandleDelayed(t_offset);

                            if (n_offset != 0)
                            {
                                // re-add us to the queue
                                _spell.GetCaster().Events.AddEvent(this, TimeSpan.FromMilliseconds(_spell.GetDelayStart() + n_offset), false);

                                return false; // event not complete
                            }
                            // event complete
                            // finish update event will be re-added automatically at the end of routine)
                        }
                        else
                        {
                            // delaying had just started, record the moment
                            _spell.SetDelayStart(e_time);
                            // handle effects on caster if the spell has travel Time but also affects the caster in some way
                            ulong n_offset = _spell.HandleDelayed(0);

                            if (_spell.SpellInfo.LaunchDelay != 0)
                                Cypher.Assert(n_offset == (ulong)Math.Floor(_spell.SpellInfo.LaunchDelay * 1000.0f));
                            else
                                Cypher.Assert(n_offset == _spell.GetDelayMoment());

                            // re-plan the event for the delay moment
                            _spell.GetCaster().Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + n_offset), false);

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
            _spell.GetCaster().Events.AddEvent(this, TimeSpan.FromMilliseconds(e_time + 1), false);

            return false; // event not complete
        }

        public override void Abort(ulong e_time)
        {
            // oops, the spell we try to do is aborted
            if (_spell.GetState() != SpellState.Finished)
                _spell.Cancel();
        }

        public override bool IsDeletable()
        {
            return _spell.IsDeletable();
        }

        public Spell GetSpell()
        {
            return _spell;
        }
    }
}