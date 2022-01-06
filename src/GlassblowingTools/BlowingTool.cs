using GlassMaking.Items;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
    public class BlowingTool : IGlassBlowingTool
    {
        public GlassBlowingToolStep GetStepInstance()
        {
            return new ToolStep();
        }

        private class ToolStep : GlassBlowingToolStep
        {
            private float time;

            public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                base.FromBytes(reader, resolver);
                time = reader.ReadSingle();
            }

            public override void ToBytes(BinaryWriter writer)
            {
                base.ToBytes(writer);
                writer.Write(time);
            }

            public override bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging)
            {
                if(attributes.KeyExists("time"))
                {
                    time = attributes["time"].AsFloat(0);
                    if(time > 0f) return true;
                }
                world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}: Invalid time", nameof(BlowingTool), sourceForErrorLogging);
                return false;
            }

            public override GlassBlowingToolStep Clone()
            {
                return new ToolStep() {
                    tool = tool,
                    shape = shape == null ? null : shape.Clone(),
                    time = time
                };
            }

            public override float GetMeshTransitionValue(ItemStack item, IAttribute data)
            {
                return item.TempAttributes.GetFloat("toolUseTime", 0f) / time;
            }

            public override void OnHeldInteractStart(ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
            {
                isComplete = false;
                if(firstEvent)
                {
                    if(byEntity.Api.Side == EnumAppSide.Client)
                    {
                        slot.Itemstack.TempAttributes.SetFloat("toolUseTime", 0f);
                    }
                    handling = EnumHandHandling.PreventDefault;
                }
            }

            public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isComplete)
            {
                const float speed = 1.5f;
                if(byEntity.Api.Side == EnumAppSide.Client)
                {
                    ModelTransform modelTransform = new ModelTransform();
                    modelTransform.EnsureDefaultValues();
                    modelTransform.Translation.Set(-Math.Min(1.275f, speed * secondsUsed * 1.5f), -Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.25f, speed * Math.Max(0, secondsUsed - 0.5f) * 0.5f));
                    modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                    byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;

                    slot.Itemstack.TempAttributes.SetFloat("toolUseTime", Math.Max(secondsUsed - 1f, 0f));
                }
                if(secondsUsed >= time)
                {
                    if(byEntity.Api.Side == EnumAppSide.Server)
                    {
                        isComplete = true;
                        return false;
                    }
                }
                isComplete = false;
                return true;
            }

            public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
            {
                if(byEntity.Api.Side == EnumAppSide.Client)
                {
                    slot.Itemstack.TempAttributes.RemoveAttribute("toolUseTime");
                }
            }

            public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
            {
                if(byEntity.Api.Side == EnumAppSide.Client)
                {
                    slot.Itemstack.TempAttributes.RemoveAttribute("toolUseTime");
                }
                return true;
            }
        }
    }
}