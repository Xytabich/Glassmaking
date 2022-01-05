using GlassMaking.Items;
using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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

            public override float GetStepProgress(ItemStack item, IAttribute data)
            {
                return item.TempAttributes.GetFloat("blowingTime", 0f) / time;
            }

            public override void OnHeldInteractStart(ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
            {
                isComplete = false;
                if(firstEvent && slot.Itemstack.Item is ItemGlassworkPipe)
                {
                    slot.Itemstack.TempAttributes.SetFloat("blowingTime", 0f);
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
                    modelTransform.Origin.Set(0.5f, 0.2f, 0.5f);
                    modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.15f, speed * secondsUsed * 1.5f), Math.Min(1f, speed * secondsUsed * 1.5f));
                    modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                    modelTransform.Rotation.X = Math.Max(-50f, -secondsUsed * 180f * speed);
                    byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;

                    slot.Itemstack.TempAttributes.SetFloat("blowingTime", Math.Max(secondsUsed - 1f, 0f));
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
                slot.Itemstack.TempAttributes.RemoveAttribute("blowingTime");
            }

            public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("blowingTime");
                return true;
            }
        }
    }
}