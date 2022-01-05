﻿using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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
                            if(damage >= 0) return true;
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
                if(firstEvent && byEntity.RightHandItemSlot?.Itemstack?.Item is ItemShears)
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
                    System.Diagnostics.Debug.WriteLine(leftTransform.Rotation.X);
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

                    slot.Itemstack.TempAttributes.SetFloat("toolUseTime", Math.Max(secondsUsed - 1f, 0f));
                }
                if(secondsUsed - 1f >= time)
                {
                    if(byEntity.Api.Side == EnumAppSide.Server)
                    {
                        var toolSlot = byEntity.RightHandItemSlot;
                        if(damage > 0) toolSlot.Itemstack.Item.DamageItem(byEntity.World, byEntity, toolSlot, damage);
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