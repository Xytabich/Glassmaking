using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	public class RecipeRegistryDictionary<T> : RecipeRegistryBase where T : IByteSerializable, IRecipeBase, new()
	{
		public readonly List<T> Recipes;
		public readonly Dictionary<string, T> Pairs;

		public RecipeRegistryDictionary()
		{
			Recipes = new List<T>();
			Pairs = new Dictionary<string, T>();
		}

		public RecipeRegistryDictionary(List<T> recipes, Dictionary<string, T> pairs)
		{
			Recipes = recipes;
			Pairs = pairs;
		}

		public override void FromBytes(IWorldAccessor resolver, int quantity, byte[] data)
		{
			using(MemoryStream ms = new MemoryStream(data))
			{
				BinaryReader reader = new BinaryReader(ms);

				for(int j = 0; j < quantity; j++)
				{
					T rec = new T();
					rec.FromBytes(reader, resolver);
					AddRecipe(rec);
				}
			}
		}

		public override void ToBytes(IWorldAccessor resolver, out byte[] data, out int quantity)
		{
			quantity = Recipes.Count;

			using(MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);

				foreach(T recipe in Recipes)
				{
					recipe.ToBytes(writer);
				}

				data = ms.ToArray();
			}
		}

		public bool AddRecipe(T rec)
		{
			var code = rec.Code.ToShortString();
			if(Pairs.ContainsKey(code)) return false;

			Recipes.Add(rec);
			Pairs[code] = rec;
			return true;
		}
	}

	public interface IRecipeBase
	{
		AssetLocation Code { get; }
	}
}