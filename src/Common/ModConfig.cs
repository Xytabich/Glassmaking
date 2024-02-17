using ProtoBuf;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	[ProtoContract]
	public class ModConfig
	{
		/// <summary>
		/// Provides a list of shards, the key is the item code, the value is the number of glass units in the shard. List sorted in descending order.
		/// </summary>
		public (AssetLocation code, EnumItemClass type, int amount)[] Shards => shards;

		[ProtoMember(1)]
		private (AssetLocation, EnumItemClass, int)[] shards;

		public ModConfig() { }

		public ModConfig((AssetLocation, EnumItemClass, int)[] shards)
		{
			this.shards = shards;
		}

		public static ModConfig CreateEmpty()
		{
			return new ModConfig(new (AssetLocation, EnumItemClass, int)[0]);
		}
	}

	internal class ModConfigJson
	{
		/// <summary>
		/// Provides a list of shards, the key is the item code, the value is the number of glass units in the shard
		/// </summary>
		public ShardInfo[] Shards { get; set; } = {
			new(new("glassmaking", "glassshards"), EnumItemClass.Item, 5),
			new(new("glassmaking", "glasspiece"), EnumItemClass.Item, 100),
			new(new("glassmaking", "glasschunk"), EnumItemClass.Item, 500)
		};

		internal class ShardInfo
		{
			public AssetLocation Code { get; set; }
			public EnumItemClass Type { get; set; }
			public int Amount { get; set; }

			public ShardInfo() { }

			public ShardInfo(AssetLocation code, EnumItemClass type, int amount)
			{
				Code = code;
				Type = type;
				Amount = amount;
			}
		}
	}
}