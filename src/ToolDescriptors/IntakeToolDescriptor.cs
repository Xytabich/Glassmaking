using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.ToolDescriptors
{
	public class IntakeToolDescriptor : ToolBehaviorDescriptor<GlassIntakeTool>
	{
		private readonly HashSet<string> toolCodes = new();
		private ItemStack[] items = default!;

		public IntakeToolDescriptor(GlassMakingMod mod) : base(mod)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			var tools = ToolCollection.Create(api);
			foreach(var item in api.World.BlockItemEnumerator())
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						var code = ((GlassblowingToolBehavior)beh).ToolCode;
						toolCodes.Add(code);
						mod.AddPipeToolDescriptor(code, this);
						tools?.AddItem(code, item);
					}
				}
			}
			handbookItemsByType = tools?.Collect()!;
			if(api.Side == EnumAppSide.Client)
			{
				items = Utils.GetGlassmeltSources(api);
			}
		}

		public override bool ResolveIngredient(IWorldAccessor world, GlassBlowingRecipe recipe, int stepIndex, string sourceForErrorLogging)
		{
			var code = recipe.Steps[stepIndex].Code;
			if(code == null || !world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes().ContainsKey(code))
			{
				world.Logger.Warning("Failed resolving a glass type with code '{0}' in {1}", code, sourceForErrorLogging);
				return false;
			}
			return true;
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			outComponents.Add(new SlideshowItemstackTextComponent(capi, items, 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));

			var step = recipe.Steps[stepIndex];
			if(Utils.GetBlendStacks(capi).TryGetValue(step.Code!, out var blends))
			{
				outComponents.Add(new SlideshowItemstackTextComponent(capi, blends, 40.0, EnumFloat.Inline,
					cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:{0} units", step.Quantity), CairoFont.WhiteSmallText()));
			}
			else
			{
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:{0} glass {1} units", Lang.Get(GlassBlend.GetBlendNameCode(step.Code!)), step.Quantity), CairoFont.WhiteSmallText()));
			}
		}

		public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
		{
			var step = recipe.Steps[stepIndex];
			dsc.AppendLine("• " + Lang.Get("glassmaking:Take {0} units of {1} glass", step.Quantity,
				Lang.Get(GlassBlend.GetBlendNameCode(step.Code!))));
		}

		public override void GetInteractionHelp(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, List<WorldInteraction> interactions)
		{
			var sources = Utils.GetGlassmeltSources(world.Api);
			interactions.Add(new WorldInteraction() {
				ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = sources
			});
			interactions.Add(new WorldInteraction() {
				ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "sneak",
				Itemstacks = sources
			});
		}

		public override bool TryGetWorkingTemperature(IWorldAccessor world, ItemStack itemStack, GlassBlowingRecipe recipe, int currentStepIndex, out float temperature)
		{
			var steps = recipe.Steps;
			int lastIndex = currentStepIndex - 1;
			if(toolCodes.Contains(steps[currentStepIndex].Tool) && itemStack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0) > 0)
			{
				lastIndex++;
			}

			temperature = 0f;
			for(int i = 0; i <= lastIndex; i++)
			{
				if(toolCodes.Contains(steps[i].Tool))
				{
					var info = mod.GetGlassTypeInfo(steps[i].Code!);
					temperature = Math.Max((info?.MeltingPoint ?? 0) * 0.8f, temperature);
				}
			}
			return temperature > 0f;
		}

		public override void GetBreakDrops(IWorldAccessor world, ItemStack itemStack, GlassBlowingRecipe recipe, int currentStepIndex, List<ItemStack> outList)
		{
			var steps = recipe.Steps;
			var amountByCode = new Dictionary<string, int>();
			if(toolCodes.Contains(steps[currentStepIndex].Tool))
			{
				int intake = itemStack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
				if(intake > 0)
				{
					amountByCode[steps[currentStepIndex].Code!.ToShortString()] = intake;
				}
			}

			for(int i = 0; i < currentStepIndex; i++)
			{
				if(toolCodes.Contains(steps[i].Tool))
				{
					var code = steps[i].Code!.ToShortString();
					if(!amountByCode.TryGetValue(code, out var amount)) amount = 0;
					amountByCode[code] = amount + steps[i].Quantity;
				}
			}
			if(amountByCode.Count == 0) return;

			foreach(var item in mod.GetShardsList(world, amountByCode))
			{
				outList.Add(item);
			}
		}

		public override void GetWildcardMapping(IWorldAccessor world, GlassBlowingRecipe recipe, int stepIndex, Dictionary<string, HashSet<string>> outMap)
		{
			var step = recipe.Steps[stepIndex];
			if(step.MatchingType != EnumRecipeMatchType.NamedWildcard) return;

			var types = world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes();
			var list = Utils.WildcardMatches(step.Code!, types.Keys, step.AllowedVariants);
			if(list.Count != 0)
			{
				outMap[step.Name!] = list;
			}
		}

		public override void FillWildcardPlaceholder(GlassBlowingRecipe recipe, int stepIndex, string variantCode, string currentVariant)
		{
			var step = recipe.Steps[stepIndex];
			if(step.MatchingType == EnumRecipeMatchType.NamedWildcard && step.Name == variantCode)
			{
				step.Code = step.Code!.CopyWithPath(step.Code.Path.Replace("*", currentVariant));
			}
			recipe.Steps[stepIndex].FillPlaceHolder(variantCode, currentVariant);
		}
	}
}