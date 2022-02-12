using System;
using Vintagestory.API.Common;

namespace GlassMaking.GlassblowingTools
{
    public class BlowingTool : GlassblowingToolBehavior
    {
        public BlowingTool(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if(firstEvent && byEntity.Controls.Sneak && TryGetRecipeStep(slot, byEntity, out var step, true, true))
            {
                if(step.BeginStep())
                {
                    if(api.Side == EnumAppSide.Client) step.SetProgress(0);
                    handHandling = EnumHandHandling.PreventDefault;
                    handling = EnumHandling.PreventSubsequent;
                    return;
                }
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if(byEntity.Controls.Sneak && TryGetRecipeStep(slot, byEntity, out var step))
            {
                if(step.ContinueStep())
                {
                    if(byEntity.Api.Side == EnumAppSide.Client)
                    {
                        const float speed = 1.5f;
                        ModelTransform modelTransform = new ModelTransform();
                        modelTransform.EnsureDefaultValues();
                        modelTransform.Translation.Set(-Math.Min(1.275f, speed * secondsUsed * 1.5f), -Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.25f, speed * Math.Max(0, secondsUsed - 0.5f) * 0.5f));
                        modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                        byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
                    }

                    float time = step.stepAttributes["time"].AsFloat(1);
                    if(api.Side == EnumAppSide.Client)
                    {
                        step.SetProgress(Math.Max(secondsUsed - 1f, 0f) / time);
                    }
                    if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= time)
                    {
                        if(byEntity.Api.Side == EnumAppSide.Server)
                        {
                            step.CompleteStep(byEntity);
                            handling = EnumHandling.PreventSubsequent;
                            return false;
                        }
                    }
                    handling = EnumHandling.PreventSubsequent;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if(api.Side == EnumAppSide.Client && TryGetRecipeStep(slot, byEntity, out var step))
            {
                if(step.ContinueStep())
                {
                    step.SetProgress(0);
                }
            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}