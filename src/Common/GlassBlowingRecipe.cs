using GlassMaking.Common;
using GlassMaking.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
	[JsonObject(MemberSerialization.OptIn)]
	public class GlassBlowingRecipe : IRecipeBase, IByteSerializable, IRecipeBase<GlassBlowingRecipe>
	{
		private static SmoothRadialShape EmptyShape = new SmoothRadialShape() { Segments = 1, Outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() { Vertices = new float[][] { new float[] { -1.5f, 0 } } } }, Inner = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() { Vertices = new float[][] { new float[] { -1.5f, 0 } } } } };

		public int RecipeId;

		[JsonProperty]
		public AssetLocation Code;

		[JsonProperty]
		public JsonItemStack Output;

		[JsonProperty]
		public GlassBlowingRecipeStep[] Steps;

		public AssetLocation Name { get; set; }

		public bool Enabled { get; set; } = true;

		public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

		IRecipeOutput IRecipeBase<GlassBlowingRecipe>.Output => Output;

		AssetLocation IRecipeBase.Code => Code;

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			return new Dictionary<string, string[]>();
		}

		public int GetStepIndex(ITreeAttribute recipeAttribute)
		{
			int step = recipeAttribute.GetInt("step", 0);
			return step < 0 || step >= Steps.Length ? -1 : step;
		}

		public void GetStepAndProgress(ITreeAttribute recipeAttribute, out int step, out float progress)
		{
			step = recipeAttribute.GetInt("step", 0);
			if(step < 0 || step >= Steps.Length)
			{
				step = -1;
				progress = 0;
				return;
			}

			progress = GameMath.Clamp(recipeAttribute.GetFloat("progress", 0), 0, 1);
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			if(Steps == null || Steps.Length == 0 || Output == null)
			{
				world.Logger.Error("Glassblowing recipe with output {0} has no steps or missing output. Ignoring recipe.", Output);
				return false;
			}
			foreach(var step in Steps)
			{
				step.Tool = step.Tool.ToLowerInvariant();
			}
			if(!Output.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			return true;
		}

		public void GetRecipeInfo(ItemStack item, ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			dsc.AppendLine(Lang.Get("glassmaking:Recipe: {0}", Output.ResolvedItemstack.Collectible.GetHeldItemName(Output.ResolvedItemstack)));
			int step = recipeAttribute.GetInt("step", 0);
			dsc.AppendLine(Lang.Get("glassmaking:Step {0}/{1}", step + 1, Steps.Length));
			var descriptor = world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetPipeToolDescriptor(Steps[step].Tool);
			if(descriptor == null)
			{
				dsc.AppendLine(Lang.Get("glassmaking:Tool: {0}", Lang.Get("glassmaking:glassblowingtool-" + Steps[step].Tool)));
			}
			else
			{
				descriptor.GetStepInfoForHeldItem(world, item, this, step, dsc, withDebugInfo);
			}
		}

		public void GetBreakDrops(ItemStack itemStack, ITreeAttribute recipeAttribute, IWorldAccessor world, List<ItemStack> outList)
		{
			int step = recipeAttribute.GetInt("step", 0);
			var tools = new HashSet<string>();
			for(int i = 0; i <= step; i++)
			{
				tools.Add(Steps[i].Tool);
			}
			var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			foreach(var tool in tools)
			{
				var descriptor = mod.GetPipeToolDescriptor(tool);
				if(descriptor != null)
				{
					descriptor.GetBreakDrops(world, itemStack, this, step, outList);
				}
			}
		}

		public float GetWorkingTemperature(ItemStack itemStack, ITreeAttribute recipeAttribute, IWorldAccessor world)
		{
			int step = recipeAttribute.GetInt("step", 0);
			var tools = new HashSet<string>();
			for(int i = 0; i <= step; i++)
			{
				tools.Add(Steps[i].Tool);
			}

			float maxTemperature = 0f;
			var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			foreach(var tool in tools)
			{
				var descriptor = mod.GetPipeToolDescriptor(tool);
				if(descriptor != null)
				{
					if(descriptor.TryGetWorkingTemperature(world, itemStack, this, step, out float temp))
					{
						maxTemperature = Math.Max(maxTemperature, temp);
					}
				}
			}
			return maxTemperature;
		}

		public bool TryBeginStep(ItemSlot slot, int index)
		{
			int current = slot.Itemstack.TempAttributes.GetInt("glassmaking:blowingStep", 0);
			if(current <= index)
			{
				if(current < index)
				{
					slot.Itemstack.TempAttributes.SetInt("glassmaking:blowingStep", index);
					slot.MarkDirty();
				}
				return true;
			}
			return false;
		}

		public bool IsCurrentStep(ItemSlot slot, int index)
		{
			return slot.Itemstack.TempAttributes.GetInt("glassmaking:blowingStep", 0) == index;
		}

		public void OnStepProgress(ItemSlot slot, float progress)
		{
			if(((ItemGlassworkPipe)slot.Itemstack.Collectible).TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
			{
				recipeAttribute.SetFloat("progress", GameMath.Clamp(progress, 0, 1));
				((ItemGlassworkPipe)slot.Itemstack.Collectible).OnRecipeUpdated(slot, false);
				slot.MarkDirty();
			}
		}

		public void OnStepComplete(ItemSlot slot, EntityAgent byEntity)
		{
			if(byEntity.Api.Side != EnumAppSide.Server) return;
			var pipe = (ItemGlassworkPipe)slot.Itemstack.Collectible;
			if(pipe.TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
			{
				int step = recipeAttribute.GetInt("step", 0) + 1;
				if(step >= Steps.Length)
				{
					var item = Output.ResolvedItemstack.Clone();
					item.Collectible.SetTemperature(byEntity.World, item, pipe.GetGlassTemperature(byEntity.World, slot.Itemstack));
					if(!byEntity.TryGiveItemStack(item))
					{
						byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
					}
					slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:blowingStep");
					pipe.OnRecipeUpdated(slot, true);
				}
				else
				{
					recipeAttribute.SetInt("step", step);
					recipeAttribute.RemoveAttribute("progress");
				}
				slot.MarkDirty();
			}
		}

		public void UpdateMesh(ITreeAttribute recipeAttribute, ItemGlassworkPipe.IMeshContainer container, int glow)
		{
			string code = recipeAttribute.GetString("code");
			GetStepAndProgress(recipeAttribute, out int step, out float t);

			SmoothRadialShape prevShape = null;
			for(int i = step - 1; i >= 0; i--)
			{
				if(Steps[i].Shape != null)
				{
					prevShape = Steps[i].Shape;
					break;
				}
			}
			if(Steps[step].Shape == null)
			{
				container.BeginMeshChange();
				if(prevShape != null)
				{
					SmoothRadialShape.BuildMesh(container.Mesh, prevShape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
				}
				container.EndMeshChange();
				return;
			}

			if(prevShape == null) prevShape = EmptyShape;
			container.BeginMeshChange();
			SmoothRadialShape.BuildLerpedMesh(container.Mesh, prevShape, Steps[step].Shape, EmptyShape, t,
				(m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
			container.EndMeshChange();
		}

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(Code);
			writer.Write(Steps.Length);
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i].ToBytes(writer);
			}
			Output.ToBytes(writer);
		}

		public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
		{
			Code = reader.ReadAssetLocation();
			Steps = new GlassBlowingRecipeStep[reader.ReadInt32()];
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i] = new GlassBlowingRecipeStep();
				Steps[i].FromBytes(reader);
			}
			Output = new JsonItemStack();
			Output.FromBytes(reader, resolver.ClassRegistry);
			Output.Resolve(resolver, "[FromBytes]");
		}

		public GlassBlowingRecipe Clone()
		{
			return new GlassBlowingRecipe() {
				RecipeId = RecipeId,
				Code = Code.Clone(),
				Output = Output.Clone(),
				Steps = Array.ConvertAll(Steps, CloneStep),
				Name = Name.Clone(),
				Enabled = Enabled
			};
		}

		private static GlassBlowingRecipeStep CloneStep(GlassBlowingRecipeStep other)
		{
			return other.Clone();
		}
	}

	[JsonObject]
	public sealed class GlassBlowingRecipeStep
	{
		[JsonProperty(Required = Required.Always)]
		public string Tool;

		[JsonProperty]
		public SmoothRadialShape Shape;

		[JsonProperty]
		[JsonConverter(typeof(JsonAttributesConverter))]
		public JsonObject Attributes;

		public void ToBytes(BinaryWriter writer)
		{
			writer.Write(Tool);
			writer.Write(Shape != null);
			if(Shape != null) Shape.ToBytes(writer);
			writer.Write(Attributes != null);
			if(Attributes != null) writer.Write(Attributes.Token.ToString());
		}

		public void FromBytes(BinaryReader reader)
		{
			Tool = reader.ReadString().ToLowerInvariant();
			if(reader.ReadBoolean())
			{
				Shape = new SmoothRadialShape();
				Shape.FromBytes(reader);
			}
			if(reader.ReadBoolean())
			{
				Attributes = new JsonObject(JToken.Parse(reader.ReadString()));
			}
		}

		public GlassBlowingRecipeStep Clone()
		{
			return new GlassBlowingRecipeStep() {
				Tool = Tool,
				Shape = Shape.Clone(),
				Attributes = Attributes?.Clone()
			};
		}
	}
}