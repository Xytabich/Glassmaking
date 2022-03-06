using GlassMaking.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GlassblowingTools
{
	public abstract class GlassblowingToolBehavior : CollectibleBehavior
	{
		public string ToolCode;

		protected GlassMakingMod mod;
		protected ICoreAPI api;

		public GlassblowingToolBehavior(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			this.api = api;
			base.OnLoaded(api);
			mod = api.ModLoader.GetModSystem<GlassMakingMod>();
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			ToolCode = properties?["tool"].AsString();
		}

		protected bool TryGetRecipeStep(ItemSlot slot, EntityAgent byEntity, out ToolRecipeStep stepInfo, bool workingTemperatureRequired = true, bool showWarning = false)
		{
			ItemSlot pipeSlot = null;
			if(slot.Itemstack.Collectible is ItemGlassworkPipe)
			{
				pipeSlot = slot;
			}
			else
			{
				var leftStack = byEntity.LeftHandItemSlot?.Itemstack;
				if(leftStack != null && leftStack.Collectible is ItemGlassworkPipe)
				{
					pipeSlot = byEntity.LeftHandItemSlot;
				}
			}
			if(pipeSlot != null)
			{
				var recipeAttribute = pipeSlot.Itemstack.Attributes.GetTreeAttribute("glassmaking:recipe");
				if(recipeAttribute != null)
				{
					if(!workingTemperatureRequired || ((ItemGlassworkPipe)pipeSlot.Itemstack.Collectible).IsWorkingTemperature(byEntity.World, pipeSlot.Itemstack))
					{
						var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
						if(recipe != null)
						{
							var step = recipe.GetStepIndex(recipeAttribute);
							if(step >= 0 && recipe.Steps[step].Tool == ToolCode)
							{
								stepInfo = new ToolRecipeStep(step, pipeSlot, recipe, recipe.Steps[step].Attributes);
								return true;
							}
						}
					}
					else if(showWarning && api.Side == EnumAppSide.Client)
					{
						((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:The workpiece is not hot enough to work"));
					}
				}
			}
			stepInfo = null;
			return false;
		}

		protected sealed class ToolRecipeStep
		{
			public int Index;
			public ItemSlot PipeSlot;
			public GlassBlowingRecipe Recipe;
			public JsonObject StepAttributes;

			private bool isComplete = false;

			public ToolRecipeStep(int index, ItemSlot pipeSlot, GlassBlowingRecipe recipe, JsonObject stepAttributes)
			{
				Index = index;
				PipeSlot = pipeSlot;
				Recipe = recipe;
				StepAttributes = stepAttributes;
			}

			public bool BeginStep()
			{
				return Recipe.TryBeginStep(PipeSlot, Index);
			}

			public bool ContinueStep()
			{
				return Recipe.IsCurrentStep(PipeSlot, Index);
			}

			/// <param name="progress">0.0-1.0</param>
			public void SetProgress(float progress)
			{
				if(PipeSlot.Itemstack == null) return;
				if(isComplete) return;
				Recipe.OnStepProgress(PipeSlot, progress);
			}

			public void CompleteStep(EntityAgent byEntity)
			{
				if(isComplete) return;
				isComplete = true;
				Recipe.OnStepComplete(PipeSlot, byEntity);
			}
		}
	}
}