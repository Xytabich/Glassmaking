using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Items
{
	public class StrictLiquidContainer : ItemLiquidContainer
	{
		public ItemStack[] allowedLiquids;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			var items = Attributes?["allowedLiquids"].AsObject<JsonItemStack[]>(null, Code.Domain);
			List<ItemStack> list = null;
			if(items != null)
			{
				foreach(var item in items)
				{
					if(item.Resolve(api.World, "allowed liquid"))
					{
						(list ?? (list = new List<ItemStack>())).Add(item.ResolvedItemstack);
					}
				}
			}
			allowedLiquids = list?.ToArray() ?? new ItemStack[0];
			if(allowedLiquids.Length == 0)
			{
				api.Logger.Log(EnumLogType.Warning, "The list of allowed liquids of the item {0} is empty, the item will not be able to take any liquids", Code);
			}
		}

		public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
		{
			if(liquidStack == null) return 0;

			var props = GetContainableProps(liquidStack);
			if(props == null) return 0;

			int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
			int availItems = liquidStack.StackSize;

			ItemStack stack = GetContent(containerStack);
			ILiquidSink sink = containerStack.Collectible as ILiquidSink;

			if(stack == null)
			{
				if(!props.Containable) return 0;
				if(!CanTakeLiquid(liquidStack)) return 0;

				int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre);

				ItemStack placedstack = liquidStack.Clone();
				placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
				SetContent(containerStack, placedstack);

				return Math.Min(desiredItems, placeableItems);
			}
			else
			{
				if(!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

				float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
				int placeableItems = (int)(maxItems - (float)stack.StackSize);

				stack.StackSize += Math.Min(placeableItems, desiredItems);

				return Math.Min(placeableItems, desiredItems);
			}
		}

		public virtual bool CanTakeLiquid(ItemStack liquidStack)
		{
			foreach(var item in allowedLiquids)
			{
				if(item.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
				{
					return true;
				}
			}
			return false;
		}
	}
}