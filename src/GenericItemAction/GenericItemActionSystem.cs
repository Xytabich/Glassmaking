using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace GlassMaking.GenericItemAction
{
	public class GenericItemActionSystem : ModSystem
	{
		private IClientNetworkChannel clientChannel = default!;

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
			msg.ItemId = item.Id;
			msg.ItemClass = item.ItemClass;
			msg.Action = action;
			msg.Attributes = null;
			if(attributes != null)
			{
				using(var stream = new MemoryStream())
				{
					attributes.ToBytes(new BinaryWriter(stream));
					msg.Attributes = stream.ToArray();
				}
			}

			clientChannel.SendPacket(msg);
		}

		private void OnHeldAction(IServerPlayer fromPlayer, HeldItemActionMessage msg)
		{
			var itemstack = fromPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if(itemstack != null && itemstack.Class == msg.ItemClass && itemstack.Id == msg.ItemId)
			{
				ITreeAttribute? attributes = null;
				if(msg.Attributes != null)
				{
					using(var stream = new MemoryStream(msg.Attributes))
					{
						attributes = new TreeAttribute();
						attributes.FromBytes(new BinaryReader(stream));
					}
				}
				{
					if(itemstack.Collectible is IGenericHeldItemAction heldAction)
					{
						if(heldAction.GenericHeldItemAction(fromPlayer, msg.Action, attributes)) return;
					}
				}
				foreach(var behavior in itemstack.Collectible.CollectibleBehaviors)
				{
					if(behavior is IGenericHeldItemAction heldAction)
					{
						if(heldAction.GenericHeldItemAction(fromPlayer, msg.Action, attributes)) return;
					}
				}
			}
		}
	}
}