using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	public class RecipeRegistryDictionary<T> : RecipeRegistryBase where T : IByteSerializable, IRecipeBase, new()
	{
		public List<T> Recipes;
		public Dictionary<string, T> Pairs;

		public RecipeRegistryDictionary()
		{
			Recipes = new List<T>();
			Pairs = new Dictionary<string, T>();
		}

		public RecipeRegistryDictionary(List<T> recipes, Dictionary<string, T> pairs)
		{
			this.Recipes = recipes;
			this.Pairs = pairs;
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

		public void AddRecipe(T rec)
		{
			Recipes.Add(rec);
			Pairs[rec.code.ToShortString()] = rec;
		}
	}

	public interface IRecipeBase
	{
		AssetLocation code { get; }
	}
}