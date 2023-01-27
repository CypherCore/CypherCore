using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
	public interface IObjectTargetSelectHandler : ITargetHookHandler
	{
		void TargetSelect(ref WorldObject targets);
	}

	public class ObjectTargetSelectHandler : TargetHookHandler, IObjectTargetSelectHandler
	{
		public delegate void SpellObjectTargetSelectFnType(ref WorldObject targets);

		private SpellObjectTargetSelectFnType _func;


		public ObjectTargetSelectHandler(SpellObjectTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectTargetSelect) : base(effectIndex, targetType, false, hookType)
		{
			_func = func;
		}

		public void TargetSelect(ref WorldObject targets)
		{
			_func(ref targets);
		}
	}
}