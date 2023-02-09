using Game.Entities;

namespace Scripts.Spells.DemonHunter;

public class auraData
{
	public auraData(uint id, ObjectGuid casterGUID)
	{
		m_id         = id;
		m_casterGuid = casterGUID;
	}

	public uint m_id;
	public ObjectGuid m_casterGuid = new();
}