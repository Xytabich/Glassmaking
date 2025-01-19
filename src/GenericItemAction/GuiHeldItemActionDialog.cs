using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GenericItemAction
{
	public abstract class GuiHeldItemActionDialog : GuiDialog
	{
		private GenericItemActionSystem system;

		public GuiHeldItemActionDialog(ICoreClientAPI capi) : base(capi)
		{
			system = capi.ModLoader.GetModSystem<GenericItemActionSystem>();
		}

		protected void DoItemAction(IPlayer player, CollectibleObject item, string action, ITreeAttribute attributes)
		{
			{
				if(item is IGenericHeldItemAction heldAction)
				{
					if(heldAction.GenericHeldItemAction(player, action, attributes))
					{
						system.SendActionMessage(item, action, attributes);
						return;
					}
				}
			}
			foreach(var behavior in item.CollectibleBehaviors)
			{
				if(behavior is IGenericHeldItemAction heldAction)
				{
					if(heldAction.GenericHeldItemAction(player, action, attributes))
					{
						system.SendActionMessage(item, action, attributes);
						return;
					}
				}
			}
		}
	}
}