using GlassMaking.Items;
using GlassMaking.Items.Behavior;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking
{
	public class GlassBlowingRecipe : RecipeBase
	{
		private static readonly SmoothRadialShape EmptyShape = new() { Segments = 1, Outer = [new() { Vertices = [[-1.5f, 0]] }], Inner = [new() { Vertices = [[-1.5f, 0]] }] };

		[JsonProperty]
		public AssetLocation? Code { get => Name; set => Name = value; }

		[JsonProperty]
		public JsonItemStack Output = default!;

		[JsonProperty]
		public GlassBlowingRecipeStep[] Steps = default!;

		public override IEnumerable<IRecipeIngredient> RecipeIngredients => Steps;
		public override IRecipeOutput RecipeOutput => Output;

		protected GlassMakingMod? mod = null;

		public override void OnParsed(IWorldAccessor world)
		{
			base.OnParsed(world);
			mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
		}

		protected override Dictionary<string, HashSet<string>> GetNameToCodeMapping(IWorldAccessor world)
		{
			mod ??= world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			var map = new Dictionary<string, HashSet<string>>();
			for(int i = 0; i < Steps.Length; i++)
			{
				var step = Steps[i];
				var matchingType = IRecipeIngredient.GetMatchType(step.Code?.ToString(), step.Name != null);
				switch(matchingType)
				{
					case EnumRecipeMatchType.NamedWildcard:
					case EnumRecipeMatchType.AdvancedWildcard:
						step.MatchingType = matchingType;
						var descriptor = mod.GetPipeToolDescriptor(step.Tool);
						descriptor?.GetWildcardMapping(world, this, i, map);
						break;
				}
			}
			return map;
		}

		protected override void FillPlaceHolder(string variantCode, string currentVariant)
		{
			Code = Code!.CopyWithPath(Code!.Path.Replace("{" + variantCode + "}", currentVariant).DeDuplicate());

			for(int i = 0; i < Steps.Length; i++)
			{
				var descriptor = mod!.GetPipeToolDescriptor(Steps[i].Tool);
				descriptor?.FillWildcardPlaceholder(this, i, variantCode, currentVariant);
				Steps[i].SkipVariants = null;
				Steps[i].AllowedVariants = null;
			}

			Output.FillPlaceHolder(variantCode, currentVariant);
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

		public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			if(Code == null || string.IsNullOrEmpty(Code.ToShortString()))
			{
				world.Logger.Error("Glassblowing recipe with output {0} has no recipe code. Ignoring recipe.", Output?.Code);
				return false;
			}
			if(Steps == null || Steps.Length == 0 || Output == null)
			{
				world.Logger.Error("Glassblowing recipe with output {0} has no steps or missing output. Ignoring recipe.", Output);
				return false;
			}
			var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i].Tool = Steps[i].Tool.ToLowerInvariant();
				var descriptor = mod.GetPipeToolDescriptor(Steps[i].Tool);
				if(descriptor == null)
				{
					world.Logger.Error("Glassblowing recipe with output {0} uses unknown tool '{1}'. Ignoring recipe.", Output?.Code, Steps[i].Tool);
					return false;
				}
				if(!descriptor.ResolveIngredient(world, this, i, sourceForErrorLogging))
				{
					return false;
				}
			}
			if(!Output.Resolve(world, sourceForErrorLogging))
			{
				world.Logger.Error("Glassblowing recipe '{0}': failed to resolve output {1} '{2}'. Check that the block/item code is correct and the mod providing it is loaded. Recipe will be skipped.", Code, Output.Type, Output.Code);
				return false;
			}
			return true;
		}

		public void GetRecipeInfo(ItemStack item, ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			dsc.AppendLine(Lang.Get("glassmaking:Recipe: {0}", Output.ResolvedItemStack!.Collectible.GetHeldItemName(Output.ResolvedItemStack)));
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

		public void GetInteractionHelp(ItemStack item, ITreeAttribute recipeAttribute, List<WorldInteraction> interactions,
			IWorldAccessor world, GlassMakingMod glassMaking)
		{
			int step = recipeAttribute.GetInt("step", 0);
			var descriptor = glassMaking.GetPipeToolDescriptor(Steps[step].Tool);
			descriptor?.GetInteractionHelp(world, item, this, step, interactions);
		}

		public void GetBreakDrops(ItemStack itemStack, ITreeAttribute recipeAttribute, IWorldAccessor world, List<ItemStack> outList)
		{
			int step = recipeAttribute.GetInt("step", 0);
			var tools = new HashSet<string>();
			for(int i = 0; i <= step; i++)
			{
				tools.Add(Steps[i].Tool);
			}
			mod ??= world.Api.ModLoader.GetModSystem<GlassMakingMod>();
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

			mod ??= world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			float maxTemperature = 0f;
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
			int current = slot.Itemstack!.TempAttributes.GetInt("glassmaking:blowingStep", 0);
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
			return slot.Itemstack!.TempAttributes.GetInt("glassmaking:blowingStep", 0) == index;
		}

		public void OnStepProgress(ItemSlot slot, float progress)
		{
			var beh = GetRecipeBehavior(slot.Itemstack!.Collectible);
			if(beh == null) return;
			if(beh.TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
			{
				recipeAttribute.SetFloat("progress", GameMath.Clamp(progress, 0, 1));
				beh.OnRecipeUpdated(slot, false);
				slot.MarkDirty();
			}
		}

		public void OnStepComplete(ItemSlot slot, EntityAgent byEntity)
		{
			if(byEntity.Api.Side != EnumAppSide.Server) return;
			var beh = GetRecipeBehavior(slot.Itemstack!.Collectible);
			if(beh == null) return;
			if(beh.TryGetRecipeAttribute(slot.Itemstack, out var recipeAttribute))
			{
				int step = recipeAttribute.GetInt("step", 0) + 1;
				if(step >= Steps.Length)
				{
					var item = Output.ResolvedItemStack!.Clone();
					var pipe = (ItemGlassworkPipe)slot.Itemstack.Collectible;
					item.Collectible.SetTemperature(byEntity.World, item, pipe.GetGlassTemperature(byEntity.World, slot.Itemstack));
					if(!byEntity.TryGiveItemStack(item))
					{
						byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
					}
					slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:blowingStep");
					beh.OnRecipeUpdated(slot, true);
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

			SmoothRadialShape? prevShape = null;
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
			SmoothRadialShape.BuildLerpedMesh(container.Mesh, prevShape, Steps[step].Shape!, EmptyShape, t,
				(m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
			container.EndMeshChange();
		}

		public override void ToBytes(BinaryWriter writer)
		{
			base.ToBytes(writer);

			writer.Write(Steps.Length);
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i].ToBytes(writer);
			}

			Output.ToBytes(writer);
		}

		public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
		{
			base.FromBytes(reader, resolver);

			Steps = new GlassBlowingRecipeStep[reader.ReadInt32()];
			for(int i = 0; i < Steps.Length; i++)
			{
				Steps[i] = new GlassBlowingRecipeStep();
				Steps[i].FromBytes(reader, resolver);
			}

			Output = new JsonItemStack();
			Output.FromBytes(reader, resolver.ClassRegistry);
			Output.Resolve(resolver, "[FromBytes]");
		}

		protected override void CloneTo(object cloneTo)
		{
			base.CloneTo(cloneTo);
			if(cloneTo is GlassBlowingRecipe recipe)
			{
				recipe.mod = mod;
				recipe.Output = Output.Clone();
				recipe.Steps = Array.ConvertAll(Steps, s => s.Clone());
			}
		}

		public override RecipeBase Clone()
		{
			var recipe = new GlassBlowingRecipe();
			CloneTo(recipe);
			return recipe;
		}

		private static GlasspipeRecipeBehavior? GetRecipeBehavior(CollectibleObject collectible)
		{
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if(beh is GlasspipeRecipeBehavior grb) return grb;
			}
			return null;
		}
	}

	public sealed class GlassBlowingRecipeStep : CraftingRecipeIngredient, IConcreteCloneable<GlassBlowingRecipeStep>
	{
		[JsonProperty(Required = Required.Always)]
		public string Tool = default!;

		[JsonProperty]
		public SmoothRadialShape? Shape = null;

		[JsonProperty]
		public int Amount { get => Quantity; set => Quantity = value; }

		public GlassBlowingRecipeStep()
		{
			MatchingType = EnumRecipeMatchType.Exact;
		}

		public override void ToBytes(BinaryWriter writer)
		{
			base.ToBytes(writer);

			writer.Write(Tool);

			writer.Write(Shape != null);
			Shape?.ToBytes(writer);
		}

		public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
		{
			base.FromBytes(reader, resolver);

			Tool = reader.ReadString().ToLowerInvariant();
			if(reader.ReadBoolean())
			{
				Shape = new SmoothRadialShape();
				Shape.FromBytes(reader);
			}
		}

		protected override void CloneTo(object cloneTo)
		{
			base.CloneTo(cloneTo);
			if(cloneTo is GlassBlowingRecipeStep ingredient)
			{
				ingredient.Tool = Tool;
				ingredient.Shape = Shape?.Clone();
			}
		}

		public new GlassBlowingRecipeStep Clone()
		{
			var ingredient = new GlassBlowingRecipeStep();
			CloneTo(ingredient);
			return ingredient;
		}
	}
}