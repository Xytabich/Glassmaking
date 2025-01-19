using GlassMaking.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace GlassMaking
{
	[JsonObject(MemberSerialization.OptIn)]
	public class WorkbenchRecipe : IRecipeBase, IByteSerializable, IRecipeBase<WorkbenchRecipe>
	{
		public int RecipeId;

		[JsonProperty]
		public AssetLocation Code = default!;

		[JsonProperty]
		public CraftingRecipeIngredient Input = default!;

		[JsonProperty]
		public JsonItemStack Output = default!;

		[JsonProperty]
		public WorkbenchRecipeStep[] Steps = default!;

		public AssetLocation Name { get; set; } = default!;

		public bool Enabled { get; set; } = true;

		public CraftingRecipeIngredient[] Ingredients => ingredients ?? (ingredients = new CraftingRecipeIngredient[] { Input });

		IRecipeIngredient[] IRecipeBase<WorkbenchRecipe>.Ingredients => Ingredients;

		IRecipeOutput IRecipeBase<WorkbenchRecipe>.Output => filler;

		AssetLocation IRecipeBase.Code => Code;

		private CraftingRecipeIngredient[]? ingredients = null;

		private readonly PlaceholderFiller filler;

		public WorkbenchRecipe()
		{
			filler = new PlaceholderFiller(this);
		}

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

			foreach(var item in Ingredients)
			{
				if(string.IsNullOrEmpty(item.Name)) continue;
				if(!item.Code.Path.Contains("*")) continue;

				int wildcardStartLen = item.Code.Path.IndexOf("*");
				int wildcardEndLen = item.Code.Path.Length - wildcardStartLen - 1;

				List<string> codes = new List<string>();
				if(item.Type == EnumItemClass.Block)
				{
					for(int i = 0; i < world.Blocks.Count; i++)
					{
						if(world.Blocks[i] == null || world.Blocks[i].IsMissing) continue;

						if(WildcardUtil.Match(item.Code, world.Blocks[i].Code, item.AllowedVariants))
						{
							string code = world.Blocks[i].Code.Path.Substring(wildcardStartLen);
							string codepart = code.Substring(0, code.Length - wildcardEndLen);
							codes.Add(codepart);
						}
					}
				}
				else
				{
					for(int i = 0; i < world.Items.Count; i++)
					{
						if(world.Items[i] == null || world.Items[i].IsMissing) continue;

						if(WildcardUtil.Match(item.Code, world.Items[i].Code, item.AllowedVariants))
						{
							string code = world.Items[i].Code.Path.Substring(wildcardStartLen);
							string codepart = code.Substring(0, code.Length - wildcardEndLen);
							codes.Add(codepart);
						}
					}
				}

				mappings[item.Name] = codes.ToArray();
			}

			return mappings;
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			if(Code == null || string.IsNullOrEmpty(Code.ToShortString()))
			{
				world.Logger.Error("Workbench recipe with output {0} has no recipe code. Ignoring recipe.", Output?.Code);
				return false;
			}
			if(Steps == null || Steps.Length == 0 || Input == null || Output == null)
			{
				world.Logger.Error("Workbench recipe {0} has no steps or missing output. Ignoring recipe.", Code);
				return false;
			}
			if(!Input.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			if(!Output.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			foreach(var step in Steps)
			{
				step.Initialize();
			}
			return true;
		}

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(RecipeId);
			writer.Write(Code);
			writer.Write(Steps.Length);
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i].ToBytes(writer);
			}
			Input.ToBytes(writer);
			Output.ToBytes(writer);
		}

		public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
		{
			RecipeId = reader.ReadInt32();
			Code = reader.ReadAssetLocation();
			Steps = new WorkbenchRecipeStep[reader.ReadInt32()];
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i] = new WorkbenchRecipeStep();
				Steps[i].FromBytes(reader);
			}
			Input = new CraftingRecipeIngredient();
			Input.FromBytes(reader, resolver);
			Input.Resolve(resolver, "[FromBytes]");
			Output = new JsonItemStack();
			Output.FromBytes(reader, resolver.ClassRegistry);
			Output.Resolve(resolver, "[FromBytes]");
		}

		public WorkbenchRecipe Clone()
		{
			var input = Input.Clone();
			return new WorkbenchRecipe() {
				RecipeId = RecipeId,
				Code = Code.Clone(),
				Input = input,
				Output = Output.Clone(),
				Steps = Array.ConvertAll(Steps, CloneStep),
				Name = Name.Clone(),
				Enabled = Enabled
			};
		}

		private static WorkbenchRecipeStep CloneStep(WorkbenchRecipeStep other)
		{
			return other.Clone();
		}

		private class PlaceholderFiller : IRecipeOutput
		{
			private WorkbenchRecipe recipe;

			public PlaceholderFiller(WorkbenchRecipe recipe)
			{
				this.recipe = recipe;
			}

			public void FillPlaceHolder(string key, string value)
			{
				recipe.Output.FillPlaceHolder(key, value);
				var wkey = "{" + key + "}";
				recipe.Code = recipe.Code.CopyWithPath(recipe.Code.Path.Replace(wkey, value));
				foreach(var step in recipe.Steps)
				{
					if(step.Shape != null)
					{
						step.Shape.Base = step.Shape.Base.CopyWithPath(step.Shape.Base.Path.Replace(wkey, value));
					}
					if(step.Textures != null)
					{
						foreach(var texture in step.Textures)
						{
							if(texture.Value.Base != null)
							{
								texture.Value.Base = texture.Value.Base.CopyWithPath(texture.Value.Base.Path.Replace(wkey, value));
							}
						}
					}
					foreach(var tool in step.Tools)
					{
						tool.Value?.FillPlaceHolder(key, value);
					}
				}
			}
		}
	}

	[JsonObject]
	public sealed class WorkbenchRecipeStep
	{
		[JsonProperty]
		public CompositeShape? Shape = null;

		[JsonProperty]
		public Dictionary<string, CompositeTexture>? Textures = null;

		[JsonProperty(Required = Required.Always, ItemConverterType = typeof(JsonAttributesConverter))]
		public Dictionary<string, JsonObject?> Tools = default!;

		[JsonProperty]
		public ModelTransform? WorkpieceTransform = null;

		[JsonProperty]
		public float? UseTime = null;

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(Shape != null);
			if(Shape != null)
			{
				writer.Write(Shape.Base);
				writer.Write(Shape.InsertBakedTextures);
				writer.Write((short)(Shape.rotateX % 360f * 64f));
				writer.Write((short)(Shape.rotateY % 360f * 64f));
				writer.Write((short)(Shape.rotateZ % 360f * 64f));
				writer.Write(Shape.offsetX);
				writer.Write(Shape.offsetY);
				writer.Write(Shape.offsetZ);
				writer.Write((short)(Shape.Scale * 64f));
				writer.Write((byte)Shape.Format);
				writer.Write(Shape.VoxelizeTexture);
				writer.Write(Shape.QuantityElements ?? 0);
			}

			writer.Write(Textures != null);
			if(Textures != null)
			{
				writer.Write(Textures.Count);
				foreach(var texture in Textures)
				{
					writer.Write(texture.Key);
					writer.Write(texture.Value?.Base ?? new AssetLocation(""));
				}
			}

			writer.Write(Tools.Count);
			foreach(var pair in Tools)
			{
				writer.Write(pair.Key);
				writer.Write(pair.Value != null);
				if(pair.Value != null) writer.Write(pair.Value.Token.ToString());
			}

			writer.Write(WorkpieceTransform != null);
			if(WorkpieceTransform != null)
			{
				writer.Write(WorkpieceTransform.Origin);
				writer.Write(WorkpieceTransform.Translation);
				writer.Write(WorkpieceTransform.Rotation);
				writer.Write(WorkpieceTransform.ScaleXYZ);
			}

			writer.Write(UseTime.HasValue);
			if(UseTime.HasValue) writer.Write(UseTime.Value);
		}

		public void FromBytes(BinaryReader reader)
		{
			if(reader.ReadBoolean())
			{
				Shape = new CompositeShape();
				Shape.Base = reader.ReadAssetLocation();
				Shape.InsertBakedTextures = reader.ReadBoolean();
				Shape.rotateX = reader.ReadInt16() / 64f;
				Shape.rotateY = reader.ReadInt16() / 64f;
				Shape.rotateZ = reader.ReadInt16() / 64f;
				Shape.offsetX = reader.ReadSingle();
				Shape.offsetY = reader.ReadSingle();
				Shape.offsetZ = reader.ReadSingle();
				Shape.Scale = reader.ReadInt16() / 64f + 1f;
				Shape.Format = (EnumShapeFormat)reader.ReadByte();
				Shape.VoxelizeTexture = reader.ReadBoolean();
				Shape.QuantityElements = reader.ReadInt32();
			}

			int count;
			if(reader.ReadBoolean())
			{
				count = reader.ReadInt32();
				Textures = new Dictionary<string, CompositeTexture>(count);
				for(int i = 0; i < count; i++)
				{
					var key = reader.ReadString();
					Textures[key] = new CompositeTexture(reader.ReadAssetLocation());
				}
			}

			count = reader.ReadInt32();
			Tools = new Dictionary<string, JsonObject?>(count);
			for(int i = 0; i < count; i++)
			{
				var tool = reader.ReadString().ToLowerInvariant();
				JsonObject? attribs = null;
				if(reader.ReadBoolean())
				{
					attribs = new JsonObject(JToken.Parse(reader.ReadString()));
				}
				Tools[tool] = attribs;
			}

			if(reader.ReadBoolean())
			{
				WorkpieceTransform = new ModelTransform();
				WorkpieceTransform.Origin = reader.ReadVec3f();
				WorkpieceTransform.Translation = reader.ReadVec3f();
				WorkpieceTransform.Rotation = reader.ReadVec3f();
				WorkpieceTransform.ScaleXYZ = reader.ReadVec3f();
			}

			if(reader.ReadBoolean())
			{
				UseTime = reader.ReadSingle();
			}
			else
			{
				UseTime = null;
			}
		}

		public WorkbenchRecipeStep Clone()
		{
			return new WorkbenchRecipeStep() {
				Shape = Shape?.Clone(),
				Textures = Textures?.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
				Tools = Tools.Select(pair => new KeyValuePair<string, JsonObject?>(pair.Key, pair.Value?.Clone())).ToDictionary(pair => pair.Key, pair => pair.Value),
				WorkpieceTransform = WorkpieceTransform?.Clone(),
				UseTime = UseTime
			};
		}

		public void Initialize()
		{
			Tools = Tools.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
			WorkpieceTransform?.EnsureDefaultValues();
		}
	}
}