﻿using GlassMaking.Items;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GlassblowingTools
{
    public abstract class GlassblowingToolBehavior : CollectibleBehavior
    {
        public string toolCode;

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
            toolCode = properties?["tool"].AsString();
        }

        protected bool TryGetRecipeStep(ItemSlot slot, EntityAgent byEntity, out ToolRecipeStep stepInfo)
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
                if(((ItemGlassworkPipe)pipeSlot.Itemstack.Collectible).GetGlassTemperature(byEntity.World, pipeSlot.Itemstack) >= ItemGlassworkPipe.WORK_TEMPERATURE)
                {
                    var recipeAttribute = pipeSlot.Itemstack.Attributes.GetTreeAttribute("recipe");
                    if(recipeAttribute != null)
                    {
                        var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                        if(recipe != null)
                        {
                            var step = recipe.GetStepIndex(recipeAttribute);
                            if(step >= 0 && recipe.steps[step].tool == toolCode)
                            {
                                stepInfo = new ToolRecipeStep(step, pipeSlot, recipe, recipe.steps[step].attributes);
                                return true;
                            }
                        }
                    }
                }
            }
            stepInfo = null;
            return false;
        }

        protected sealed class ToolRecipeStep
        {
            public int index;
            public ItemSlot pipeSlot;
            public GlassBlowingRecipe recipe;
            public JsonObject stepAttributes;

            private bool isComplete = false;

            public ToolRecipeStep(int index, ItemSlot pipeSlot, GlassBlowingRecipe recipe, JsonObject stepAttributes)
            {
                this.index = index;
                this.pipeSlot = pipeSlot;
                this.recipe = recipe;
                this.stepAttributes = stepAttributes;
            }

            public bool BeginStep()
            {
                return recipe.TryBeginStep(pipeSlot, index);
            }

            public bool ContinueStep()
            {
                return recipe.IsCurrentStep(pipeSlot, index);
            }

            /// <param name="progress">0.0-1.0</param>
            public void SetProgress(float progress)
            {
                if(pipeSlot.Itemstack == null) return;
                if(isComplete) return;
                recipe.OnStepProgress(pipeSlot, progress);
            }

            public void CompleteStep(EntityAgent byEntity)
            {
                if(isComplete) return;
                isComplete = true;
                recipe.OnStepComplete(pipeSlot, byEntity);
            }
        }
    }
}