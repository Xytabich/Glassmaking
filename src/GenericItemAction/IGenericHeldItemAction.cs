using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GenericItemAction
{
	public interface IGenericHeldItemAction
	{
		bool GenericHeldItemAction(IPlayer player, string action, ITreeAttribute attributes);
	}
}