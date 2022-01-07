using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.GlassblowingTools
{
    public class ShearsTool : IGlassBlowingTool
    {
        private bool initComplete = false;
        private WorldInteraction[] interactions;

        public GlassBlowingToolStep GetStepInstance()
        {
            return new ToolStep(this);
        }

        private void OnInitClient(ICoreClientAPI api)
        {
            if(initComplete) return;
            initComplete = true;
            List<ItemStack> list = new List<ItemStack>();
            foreach(Item item in api.World.Items)
            {
                if(item is ItemShears tool && tool.ToolTier >= 4)
                {
                    list.Add(new ItemStack(tool));
                }
            }
            interactions = new WorldInteraction[1] {
                new WorldInteraction() {
                    ActionLangCode = "glassmaking:heldhelp-gbtool-shears",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = list.ToArray()
                }
            };
        }

        private class ToolStep : GlassBlowingToolStep
        {
            private float time;
            private int damage = 1;

            private ShearsTool toolInstance;

            public ToolStep(ShearsTool toolInstance)
            {
                this.toolInstance = toolInstance;
            }

            public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                base.FromBytes(reader, resolver);
                time = reader.ReadSingle();
                damage = reader.ReadInt32();
                if(resolver.Api.Side == EnumAppSide.Client)
                {
                    toolInstance.OnInitClient(resolver.Api as ICoreClientAPI);
                }
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
                return new ToolStep(toolInstance) {
                    tool = tool,
                    shape = shape == null ? null : shape.Clone(),
                    time = time,
                    damage = damage
                };
            }

            public override WorldInteraction[] GetHeldInteractionHelp(ItemStack item, IAttribute data)
            {
                return toolInstance.interactions;
            }

            public override float GetMeshTransitionValue(ItemStack item, IAttribute data)
            {
                return Math.Max(item.TempAttributes.GetFloat("toolUseTime", 0f) / time, 0f);
            }

            public override void OnHeldInteractStart(ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
            {
                isComplete = false;
                if(firstEvent)
                {
                    var tool = byEntity.RightHandItemSlot?.Itemstack?.Item as ItemShears;
                    if(tool != null && tool.ToolTier >= 4)
                    {
                        if(byEntity.Api.Side == EnumAppSide.Client)
                        {
                            slot.Itemstack.TempAttributes.SetFloat("toolUseTime", 0f);
                        }
                        handling = EnumHandHandling.PreventDefault;
                    }
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