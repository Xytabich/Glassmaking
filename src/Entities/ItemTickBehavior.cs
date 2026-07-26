using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace GlassMaking.Entities.Behavior
{
	public class ItemTickBehavior : EntityBehavior
	{
		private readonly EntityItem entityItem;
		private readonly List<IItemEntityTickListener> listeners = new();
		private CollectibleObject? collectible = null;

		public ItemTickBehavior(Entity entity) : base(entity)
		{
			entityItem = (EntityItem)entity;
		}

		public override string PropertyName()
		{
			return "glassmaking:itemtick";
		}

		public override void AfterInitialized(bool onFirstSpawn)
		{
			base.AfterInitialized(onFirstSpawn);
			collectible = entityItem.Slot.Itemstack?.Collectible;
			RebuildListenerList();
		}

		public override void OnGameTick(float deltaTime)
		{
			var newCollectible = entityItem.Slot.Itemstack?.Collectible;
			if(newCollectible != collectible)
			{
				collectible = newCollectible;
				RebuildListenerList();
			}
			for(int i = 0; i < listeners.Count; i++)
			{
				listeners[i].OnGameTick(entityItem, deltaTime);
			}
		}

		private void RebuildListenerList()
		{
			listeners.Clear();
			if(collectible == null) return;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if(beh is IItemEntityTickListener listener)
				{
					listeners.Add(listener);
				}
			}
		}
	}

	public interface IItemEntityTickListener
	{
		void OnGameTick(EntityItem entity, float deltaTime);
	}
}