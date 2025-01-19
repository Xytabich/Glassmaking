using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	public class GlassTypeRegistry : RecipeRegistryBase
	{
		public Dictionary<AssetLocation, GlassTypeVariant> GlassTypes = new Dictionary<AssetLocation, GlassTypeVariant>();

		public override void FromBytes(IWorldAccessor resolver, int quantity, byte[] data)
		{
			GlassTypes.Clear();
			using(var stream = new MemoryStream(data))
			{
				var reader = new BinaryReader(stream, Encoding.UTF8);
				for(int i = 0; i < quantity; i++)
				{
					var type = new GlassTypeVariant();
					type.ReadFrom(reader);
					GlassTypes[type.Code] = type;
				}
			}
		}

		public override void ToBytes(IWorldAccessor resolver, out byte[] data, out int quantity)
		{
			quantity = GlassTypes.Count;
			using(var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				foreach(var pair in GlassTypes)
				{
					pair.Value.WriteTo(writer);
				}
				data = stream.ToArray();
			}
		}
	}

	public class GlassTypeProperty : WorldProperty<GlassTypeVariant>
	{
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class GlassTypeVariant : WorldPropertyVariant
	{
		[JsonProperty(Required = Required.Always)]
		public float MeltingPoint;

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(Code.ToShortString());
			writer.Write(MeltingPoint);
		}

		public void ReadFrom(BinaryReader reader)
		{
			Code = new AssetLocation(reader.ReadString());
			MeltingPoint = reader.ReadSingle();
		}
	}
}