using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Common
{
	[JsonObject]
	public class GlassBlend
	{
		public const string PROPERTY_NAME = "glassmaking:glassblend";

		private static AssetLocation shardsItemCode = new AssetLocation("glassmaking", "glassshards");
		private static AssetLocation pieceItemCode = new AssetLocation("glassmaking", "glasspiece");
		private static AssetLocation chunkItemCode = new AssetLocation("glassmaking", "glasschunk");

		[JsonProperty(Required = Required.DisallowNull)]
		public AssetLocation Code;
		[JsonProperty(Required = Required.Always)]
		public int Amount;

		public GlassBlend()
		{
		}

		public GlassBlend(AssetLocation code, int amount)
		{
			Code = code;
			Amount = amount;
		}

		public void ToTreeAttributes(ITreeAttribute tree)
		{
			tree.SetString("code", Code.ToShortString());
			tree.SetInt("amount", Amount);
		}

		public static string GetBlendNameCode(AssetLocation code)
		{
			return code.Clone().WithPathPrefixOnce("glassblend-").ToString();
		}

		public static GlassBlend FromTreeAttributes(ITreeAttribute tree)
		{
			if(tree == null) return null;
			return new GlassBlend(new AssetLocation(tree.GetString("code")), tree.GetInt("amount"));
		}

		public static GlassBlend FromJson(CollectibleObject collectible)
		{
			if(collectible.Attributes == null) return null;
			return collectible.Attributes[PROPERTY_NAME].AsObject<GlassBlend>(null, collectible.Code.Domain);
		}

		public static GlassBlend FromJson(ItemStack stack)
		{
			return FromJson(stack.Collectible);
		}

		public static IEnumerable<ItemStack> GetShardsList(IWorldAccessor world, IReadOnlyDictionary<string, int> amountByCode, bool limitStackSize = false)
		{
			foreach(var pair in amountByCode)
			{
				foreach(var itemStack in GetShardsList(world, new AssetLocation(pair.Key), pair.Value, limitStackSize))
				{
					yield return itemStack;
				}
			}
		}

		public static IEnumerable<ItemStack> GetShardsList(IWorldAccessor world, AssetLocation glassCode, int glassAmount, bool limitStackSize = false)
		{
			if(glassAmount >= 500)
			{
				int count = glassAmount / 500;
				glassAmount -= count * 500;

				var item = world.GetItem(chunkItemCode);
				var blendInfo = new GlassBlend(glassCode, 500);
				if(limitStackSize)
				{
					while(count > 0)
					{
						var itemStack = new ItemStack(item, Math.Min(count, item.MaxStackSize));
						blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
						yield return itemStack;

						count -= item.MaxStackSize;
					}
				}
				else
				{
					var itemStack = new ItemStack(item, count);
					blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
					yield return itemStack;
				}
			}
			if(glassAmount >= 100)
			{
				int count = glassAmount / 100;
				glassAmount -= count * 100;

				var item = world.GetItem(pieceItemCode);
				var blendInfo = new GlassBlend(glassCode, 100);
				if(limitStackSize)
				{
					while(count > 0)
					{
						var itemStack = new ItemStack(item, Math.Min(count, item.MaxStackSize));
						blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
						yield return itemStack;

						count -= item.MaxStackSize;
					}
				}
				else
				{
					var itemStack = new ItemStack(item, count);
					blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
					yield return itemStack;
				}
			}
			if(glassAmount >= 5)
			{
				int count = glassAmount / 5;

				var item = world.GetItem(shardsItemCode);
				var blendInfo = new GlassBlend(glassCode, 5);
				if(limitStackSize)
				{
					while(count > 0)
					{
						var itemStack = new ItemStack(item, Math.Min(count, item.MaxStackSize));
						blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
						yield return itemStack;

						count -= item.MaxStackSize;
					}
				}
				else
				{
					var itemStack = new ItemStack(item, count);
					blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
					yield return itemStack;
				}
			}
		}
	}
}