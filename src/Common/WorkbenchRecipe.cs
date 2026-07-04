using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
	public class WorkbenchRecipe : RecipeBase
	{
		[JsonProperty]
		public AssetLocation Code { get => Name!; set => Name = value; }

		[JsonProperty]
		public CraftingRecipeIngredient Input = default!;

		[JsonProperty]
		public JsonItemStack Output = default!;

		[JsonProperty]
		public WorkbenchRecipeStep[] Steps = default!;

		public override IEnumerable<IRecipeIngredient> RecipeIngredients => ingredients ??=
			Enumerable.Concat([Input], Steps.SelectMany(s => s.Tools.Values.Where(i => i != null))).ToArray()!;
		public override IRecipeOutput RecipeOutput => Output;

		private CraftingRecipeIngredient[]? ingredients = null;

		public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			if(Name == null || string.IsNullOrEmpty(Name.ToShortString()))
			{
				world.Logger.Error("Workbench recipe with output {0} has no recipe code. Ignoring recipe.", Output?.Code);
				return false;
			}
			if(Steps == null || Steps.Length == 0 || Input == null || Output == null)
			{
				world.Logger.Error("Workbench recipe {0} has no steps or missing output. Ignoring recipe.", Code);
				return false;
			}
			var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			for(int i = 0; i < Steps.Length; i++)
			{
				if(!Steps[i].Resolve(world, this, mod, sourceForErrorLogging))
				{
					world.Logger.Error($"Unable to resolve step #{i + 1} for workbench recipe {Code} (see details earlier). Ignoring recipe.");
					return false;
				}
			}
			if(!Input.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			if(!Output.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			return true;
		}

		public override void ToBytes(BinaryWriter writer)
		{
			base.ToBytes(writer);

			writer.Write(Steps.Length);
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i].ToBytes(writer);
			}

			Input.ToBytes(writer);

			Output.ToBytes(writer);
		}

		public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
		{
			base.FromBytes(reader, resolver);

			Steps = new WorkbenchRecipeStep[reader.ReadInt32()];
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i] = new WorkbenchRecipeStep();
				Steps[i].FromBytes(reader, resolver);
			}

			Input = new CraftingRecipeIngredient();
			Input.FromBytes(reader, resolver);
			Input.Resolve(resolver, "[FromBytes]");

			Output = new JsonItemStack();
			Output.FromBytes(reader, resolver.ClassRegistry);
			Output.Resolve(resolver, "[FromBytes]");
		}

		protected override void CloneTo(object cloneTo)
		{
			base.CloneTo(cloneTo);
			if(cloneTo is WorkbenchRecipe recipe)
			{
				recipe.Input = Input.Clone();
				recipe.Output = Output.Clone();
				recipe.Steps = Array.ConvertAll(Steps, s => s.Clone());
			}
		}

		public override RecipeBase Clone()
		{
			var recipe = new WorkbenchRecipe();
			CloneTo(recipe);
			return recipe;
		}
	}

	[JsonObject]
	public sealed class WorkbenchRecipeStep
	{
		[JsonProperty]
		public CompositeShape? Shape = null;

		[JsonProperty]
		public Dictionary<string, CompositeTexture>? Textures = null;

		[JsonProperty(Required = Required.Always)]
		public Dictionary<string, CraftingRecipeIngredient?> Tools = default!;

		[JsonProperty]
		public ModelTransform? WorkpieceTransform = null;

		[JsonProperty]
		public float? UseTime = null;

		public bool Resolve(IWorldAccessor world, WorkbenchRecipe recipe, GlassMakingMod mod, string sourceForErrorLogging)
		{
			Tools = Tools.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
			foreach(var (name, ingred) in Tools)
			{
				var tool = mod.GetWorkbenchToolDescriptor(name);
				if(tool == null)
				{
					world.Logger.Error($"Unable to find workbench tool {name}");
					return false;
				}
				if(!tool.ResolveIngredient(world, recipe, ingred, sourceForErrorLogging))
				{
					return false;
				}
			}
			WorkpieceTransform?.EnsureDefaultValues();
			return true;
		}

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
				pair.Value?.ToBytes(writer);
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

		public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
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
			Tools = new Dictionary<string, CraftingRecipeIngredient?>(count);
			for(int i = 0; i < count; i++)
			{
				var tool = reader.ReadString().ToLowerInvariant();
				CraftingRecipeIngredient? ingred = null;
				if(reader.ReadBoolean())
				{
					ingred = new CraftingRecipeIngredient();
					ingred.FromBytes(reader, resolver);
				}
				Tools[tool] = ingred;
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
				Tools = Tools.ToDictionary(pair => pair.Key, pair => pair.Value?.Clone()),
				WorkpieceTransform = WorkpieceTransform?.Clone(),
				UseTime = UseTime
			};
		}
	}
}