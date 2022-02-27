using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace GlassMaking.GenericItemAction
{
	public class GenericItemActionSystem : ModSystem
	{
		private IClientNetworkChannel clientChannel = null;

		public override void Start(ICoreAPI api)
		{
			base.Start(api);
			api.Network.RegisterChannel("genitemdlg:action").RegisterMessageType<HeldItemActionMessage>();
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
			base.StartServerSide(api);
			var serverChannel = api.Network.GetChannel("genitemdlg:action");
			serverChannel.SetMessageHandler<HeldItemActionMessage>(OnHeldAction);
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			base.StartClientSide(api);
			clientChannel = api.Network.GetChannel("genitemdlg:action");
		}

		public void SendActionMessage(CollectibleObject item, string action, ITreeAttribute attributes)
		{
			var msg = new HeldItemActionMessage();
			msg.itemId = item.Id;
			msg.itemClass = item.ItemClass;
			msg.action = action;
			msg.attributes = null;
			if(attributes != null)
			{
				using(var stream = new MemoryStream())
				{
					attributes.ToBytes(new BinaryWriter(stream));
					msg.attributes = stream.ToArray();
				}
			}

			clientChannel.SendPacket(msg);
		}

		private void OnHeldAction(IServerPlayer fromPlayer, HeldItemActionMessage msg)
		{
			var itemstack = fromPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if(itemstack != null && itemstack.Class == msg.itemClass && itemstack.Id == msg.itemId)
			{
				ITreeAttribute attributes = null;
				if(msg.attributes != null)
				{
					using(var stream = new MemoryStream(msg.attributes))
					{
						attributes = new TreeAttribute();
						attributes.FromBytes(new BinaryReader(stream));
					}
				}
				IGenericHeldItemAction heldAction;
				if((heldAction = itemstack.Collectible as IGenericHeldItemAction) != null)
				{
					if(heldAction.GenericHeldItemAction(fromPlayer, msg.action, attributes)) return;
				}
				foreach(var behavior in itemstack.Collectible.CollectibleBehaviors)
				{
					if((heldAction = behavior as IGenericHeldItemAction) != null)
					{
						if(heldAction.GenericHeldItemAction(fromPlayer, msg.action, attributes)) return;
					}
				}
			}
		}
	}
}