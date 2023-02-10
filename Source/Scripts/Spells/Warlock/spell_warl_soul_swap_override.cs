using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warlock
{
	[Script] // 86211 - Soul Swap - Also acts as a dot container
	public class spell_warl_soul_swap_override : AuraScript
	{
		private readonly List<uint> _dotList = new();

		private Unit _swapCaster;

		//! Forced to, pure virtual functions must have a body when linking
		public override void Register()
		{
		}

		public void AddDot(uint id)
		{
			_dotList.Add(id);
		}

		public List<uint> GetDotList()
		{
			return _dotList;
		}

		public Unit GetOriginalSwapSource()
		{
			return _swapCaster;
		}

		public void SetOriginalSwapSource(Unit victim)
		{
			_swapCaster = victim;
		}
	}
}