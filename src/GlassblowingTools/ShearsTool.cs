using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GlassblowingTools
{
    public class ShearsTool : IGlassBlowingTool
    {
        public GlassBlowingToolStep GetStepInstance()
        {
            return new ToolStep();
        }

        private class ToolStep : GlassBlowingToolStep
        {
            private float time;
            private int damage = 1;

            public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                base.FromBytes(reader, resolver);
                time = reader.ReadSingle();
                damage = reader.ReadInt32();
            }

            public override void ToBytes(BinaryWriter writer)
            {
                base.ToBytes(writer);
                writer.Write(time);
                writer.Write(damage);
            }

            public override bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging)
            {
                if(attributes.KeyExists("time"))
                {
                    time = attributes["time"].AsFloat(0);
                    if(time > 0f)
                    {
                        if(attributes.KeyExists("damage"))
                        {
                            damage = attributes["damage"].AsInt(0);
                            if(damage > 0) return true;
                        }
                        else return true;
                    }
                }
                world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}: Invalid time or damage values", nameof(BlowingTool), sourceForErrorLogging);
                return false;
            }

            public override GlassBlowingToolStep Clone()
            {
                return new ToolStep() {
                    tool = tool,
                    shape = shape == null ? null : shape.Clone(),
                    time = time,
                    damage = damage
                };
            }

            public override float GetStepProgress(ItemStack item, IAttribute data)
            {
                return Math.Max(item.TempAttributes.GetFloat("toolUseTime", 0f) / time, 0f);
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
                    modelTransform.Origin.Set(0.5f, 0.2f, 0.5f);
                    modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.15f, speed * secondsUsed * 1.5f), Math.Min(1f, speed * secondsUsed * 1.5f));
                    modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                    modelTransform.Rotation.X = Math.Max(-50f, -secondsUsed * 180f * speed);
                    byEntity.Controls.LeftUsingHeldItemTransformBefore = modelTransform;

                    slot.Itemstack.TempAttributes.SetFloat("toolUseTime", Math.Max(secondsUsed - 1f, 0f));
                }
                if(secondsUsed - 1f >= time)
                {
                    if(byEntity.Api.Side == EnumAppSide.Server)
                    {
                        var toolSlot = byEntity.RightHandItemSlot;
                        toolSlot.Itemstack.Item.DamageItem(byEntity.World, byEntity, toolSlot, damage);
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