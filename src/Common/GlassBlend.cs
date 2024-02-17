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
	}

	public static class GlassBlendExt
	{
		public static IEnumerable<ItemStack> GetShardsList(this GlassMakingMod mod, IWorldAccessor world, IReadOnlyDictionary<string, int> amountByCode, bool limitStackSize = false)
		{
			foreach(var pair in amountByCode)
			{
				foreach(var itemStack in GetShardsList(mod, world, new AssetLocation(pair.Key), pair.Value, limitStackSize))
				{
					yield return itemStack;
				}
			}
		}

		public static IEnumerable<ItemStack> GetShardsList(this GlassMakingMod mod, IWorldAccessor world, AssetLocation glassCode, int glassAmount, bool limitStackSize = false)
		{
			var shards = mod.Config.Shards;
			int len = shards.Length;
			for(int i = 0; i < len; i++)
			{
				var shardInfo = shards[i];
				if(glassAmount >= shardInfo.amount)
				{
					int count = glassAmount / shardInfo.amount;
					glassAmount -= count * shardInfo.amount;

					CollectibleObject collectible = shardInfo.type == EnumItemClass.Item ? world.GetItem(shardInfo.code) : world.GetBlock(shardInfo.code);
					var blendInfo = new GlassBlend(glassCode, shardInfo.amount);
					if(limitStackSize)
					{
						while(count > 0)
						{
							var itemStack = new ItemStack(collectible, Math.Min(count, collectible.MaxStackSize));
							blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
							yield return itemStack;

							count -= collectible.MaxStackSize;
						}
					}
					else
					{
						var itemStack = new ItemStack(collectible, count);
						blendInfo.ToTreeAttributes(itemStack.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
						yield return itemStack;
					}
				}
			}
		}
	}
}