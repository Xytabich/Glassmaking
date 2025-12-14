using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			outComponents.Add(new SlideshowItemstackTextComponent(capi, items, 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));

			var step = recipe.Steps[stepIndex];
			var code = step.Code ?? new AssetLocation(step.Attributes!["code"].AsString());
			if(Utils.GetGlassBlends(capi).TryGetValue(code, out var blends))
			{
				outComponents.Add(new SlideshowItemstackTextComponent(capi, blends, 40.0, EnumFloat.Inline,
					cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:{0} units", step.Attributes!["amount"].AsInt()), CairoFont.WhiteSmallText()));
			}
		}

		public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
		{
			var step = recipe.Steps[stepIndex];
			var code = step.Code ?? new AssetLocation(step.Attributes!["code"].AsString());
			dsc.AppendLine("• " + Lang.Get("glassmaking:Take {0} units of {1} glass", step.Attributes!["amount"].AsInt(),
				Lang.Get(GlassBlend.GetBlendNameCode(code))));
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
					var code = steps[i].Code ?? new AssetLocation(steps[i].Attributes!["code"].AsString());
					var info = mod.GetGlassTypeInfo(code);
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
					amountByCode[GetCodeString(steps[currentStepIndex])] = intake;
				}
			}

			for(int i = 0; i < currentStepIndex; i++)
			{
				if(toolCodes.Contains(steps[i].Tool))
				{
					var code = GetCodeString(steps[i]);
					if(!amountByCode.TryGetValue(code, out var amount)) amount = 0;
					amountByCode[code] = amount + steps[i].Attributes!["amount"].AsInt();
				}
			}
			if(amountByCode.Count == 0) return;

			foreach(var item in mod.GetShardsList(world, amountByCode))
			{
				outList.Add(item);
			}
		}

		public override void GetWildcardMapping(IWorldAccessor world, GlassBlowingRecipe recipe, int stepIndex, Dictionary<string, string[]> outMap)
		{
			var name = recipe.Steps[stepIndex].Name;
			if(string.IsNullOrEmpty(name)) return;

			var attributes = recipe.Steps[stepIndex].Attributes!;
			var code = new AssetLocation(attributes["code"].AsString());
			int wildcardPos = code.Path.IndexOf('*');
			if(wildcardPos >= 0)
			{
				recipe.Steps[stepIndex].Code = code;

				var types = world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes();
				var allowedVariants = attributes["allowedVariants"].AsArray<string>();
				int wildcardOffset = code.Path.Length - (wildcardPos + 1);
				var list = new List<string>();
				foreach(var glass in types.Keys)
				{
					if(WildcardUtil.Match(code, glass, allowedVariants))
					{
						list.Add(glass.Path[wildcardPos..^wildcardOffset]);
					}
				}
				if(list.Count != 0)
				{
					outMap[name] = list.ToArray();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetCodeString(GlassBlowingRecipeStep step)
		{
			return step.Code?.ToShortString() ?? step.Attributes!["name"].AsString();
		}
	}
}