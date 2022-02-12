using GlassMaking.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
	[JsonObject(MemberSerialization.OptIn)]
	public class WorkbenchRecipe : IRecipeBase, IByteSerializable, IRecipeBase<WorkbenchRecipe>
	{
		public int RecipeId;

		[JsonProperty]
		public AssetLocation Code;

		[JsonProperty]
		public JsonItemStack Input;

		[JsonProperty]
		public JsonItemStack Output;

		[JsonProperty]
		public WorkbenchRecipeStep[] Steps;

		public AssetLocation Name { get; set; }

		public bool Enabled { get; set; } = true;

		public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

		IRecipeOutput IRecipeBase<WorkbenchRecipe>.Output => Output;

		AssetLocation IRecipeBase.Code => Code;

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			return new Dictionary<string, string[]>();
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			if(Steps == null || Steps.Length == 0 || Input == null || Output == null)
			{
				world.Logger.Error("Workbench recipe {0} has no steps or missing output. Ignoring recipe.", Code);
				return false;
			}
			foreach(var step in Steps)
			{
				step.Tool = step.Tool.ToLowerInvariant();
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

		public void ToBytes(BinaryWriter writer)
		{
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
			Code = reader.ReadAssetLocation();
			Steps = new WorkbenchRecipeStep[reader.ReadInt32()];
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i] = new WorkbenchRecipeStep();
				Steps[i].FromBytes(reader);
			}
			Input = new JsonItemStack();
			Input.FromBytes(reader, resolver.ClassRegistry);
			Input.Resolve(resolver, "[FromBytes]");
			Output = new JsonItemStack();
			Output.FromBytes(reader, resolver.ClassRegistry);
			Output.Resolve(resolver, "[FromBytes]");
		}

		public WorkbenchRecipe Clone()
		{
			return new WorkbenchRecipe() {
				RecipeId = RecipeId,
				Code = Code.Clone(),
				Input = Input.Clone(),
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
	}

	[JsonObject]
	public sealed class WorkbenchRecipeStep
	{
		[JsonProperty(Required = Required.Always)]
		public string Tool;

		[JsonProperty]
		public CompositeShape Shape;

		[JsonProperty]
		[JsonConverter(typeof(JsonAttributesConverter))]
		public JsonObject Attributes;

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(Tool);
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
			writer.Write(Attributes != null);
			if(Attributes != null) writer.Write(Attributes.Token.ToString());
		}

		public void FromBytes(BinaryReader reader)
		{
			Tool = reader.ReadString().ToLowerInvariant();
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
			if(reader.ReadBoolean())
			{
				Attributes = new JsonObject(JToken.Parse(reader.ReadString()));
			}
		}

		public WorkbenchRecipeStep Clone()
		{
			return new WorkbenchRecipeStep() {
				Tool = Tool,
				Shape = Shape.Clone(),
				Attributes = Attributes?.Clone()
			};
		}
	}
}