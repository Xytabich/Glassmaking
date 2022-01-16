using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
    public class ShearsTool : GlassblowingToolBehavior
    {
        public ShearsTool(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if(firstEvent && TryGetRecipeStep(slot, byEntity, out var step))
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
            if(TryGetRecipeStep(slot, byEntity, out var step))
            {
                if(step.ContinueStep())
                {
                    if(byEntity.Api.Side == EnumAppSide.Client)
                    {
                        const float speed = 2f;
                        ModelTransform leftTransform = new ModelTransform();
                        leftTransform.EnsureDefaultValues();
                        leftTransform.Origin.Set(0f, 0f, 0f);
                        leftTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 3f);
                        leftTransform.Rotation.Y = Math.Min(25f, secondsUsed * 45f * speed);
                        leftTransform.Rotation.X = GameMath.Lerp(0f, 0.5f * GameMath.Clamp(byEntity.Pos.Pitch - (float)Math.PI, -0.2f, 1.0995574f) * GameMath.RAD2DEG, Math.Min(1, secondsUsed * speed * 4f));
                        leftTransform.Rotation.Z = secondsUsed * 90f % 360f;
                        byEntity.Controls.LeftUsingHeldItemTransformBefore = leftTransform;

                        const float speed2 = 1.5f;
                        ModelTransform rightTransform = new ModelTransform();
                        rightTransform.EnsureDefaultValues();
                        rightTransform.Origin.Set(-0.5f, -0.5f, -0.5f);
                        rightTransform.Translation.Set(0.24f, 0.1f, -0.5f);
                        rightTransform.Rotation.Y = Math.Min(25f, secondsUsed * 45f * speed) + GameMath.FastSin(secondsUsed * 4f * speed2) * 3f;
                        rightTransform.Rotation.Z = Math.Min(25f, secondsUsed * 45f * speed);
                        byEntity.Controls.UsingHeldItemTransformBefore = rightTransform;

                        byEntity.Controls.HandUse = EnumHandInteract.None;
                    }

                    float time = step.stepAttributes["time"].AsFloat(1);
                    if(api.Side == EnumAppSide.Client)
                    {
                        step.SetProgress(Math.Max(secondsUsed - 1f, 0f) / time);
                    }
                    if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= time)
                    {
                        int damage = step.stepAttributes["damage"].AsInt(1);
                        if(damage > 0)
                        {
                            slot.Itemstack.Item.DamageItem(byEntity.World, byEntity, slot, damage);
                            slot.MarkDirty();
                        }
                        step.CompleteStep(byEntity);
                        handling = EnumHandling.PreventSubsequent;
                        return false;
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