using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.ToolDescriptors
{
	public class IntakeToolDescriptor : ToolBehaviorDescriptor<GlassIntakeTool>
	{
		private readonly HashSet<string> toolCodes = new HashSet<string>();
		private ItemStack[] items = default!;

		public IntakeToolDescriptor(GlassMakingMod mod) : base(mod)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			foreach(var item in api.World.Collectibles)
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						var code = ((GlassblowingToolBehavior)beh).ToolCode;
						toolCodes.Add(code);
						mod.AddPipeToolDescriptor(code, this);
					}
				}
			}
			if(api.Side == EnumAppSide.Client)
			{
				items = Utils.GetGlassmeltSources(api);
			}
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var step = recipe.Steps[stepIndex];
			outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}: {1}", stepIndex + 1,
				Lang.Get("glassmaking:Take {0} units of {1} glass", step.Attributes!["amount"].AsInt(),
				Lang.Get(GlassBlend.GetBlendNameCode(new AssetLocation(step.Attributes["code"].AsString()))))) + "\n", CairoFont.WhiteSmallText()));

			outComponents.Add(new SlideshowItemstackTextComponent(capi, items, 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
			outComponents.Add(new ClearFloatTextComponent(capi));
		}

		public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
		{
			var step = recipe.Steps[stepIndex];
			dsc.AppendLine("• " + Lang.Get("glassmaking:Take {0} units of {1} glass", step.Attributes!["amount"].AsInt(),
				Lang.Get(GlassBlend.GetBlendNameCode(new AssetLocation(step.Attributes["code"].AsString())))));
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
					var info = mod.GetGlassTypeInfo(new AssetLocation(steps[i].Attributes!["code"].AsString()));
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
					amountByCode[steps[currentStepIndex].Attributes!["code"].AsString()] = intake;
				}
			}

			for(int i = 0; i < currentStepIndex; i++)
			{
				if(toolCodes.Contains(steps[i].Tool))
				{
					var code = steps[i].Attributes!["code"].AsString();
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
	}
}