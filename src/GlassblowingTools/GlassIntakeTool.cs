using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.Items;
using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
    public class GlassIntakeTool : IGlassBlowingTool
    {
        public GlassBlowingToolStep GetStepInstance()
        {
            return new ToolStep();
        }

        private class ToolStep : GlassBlowingToolStep
        {
            private AssetLocation code;
            private int amount;

            public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
            {
                base.FromBytes(reader, resolver);
                code = new AssetLocation(reader.ReadString());
                amount = reader.ReadInt32();
            }

            public override void ToBytes(BinaryWriter writer)
            {
                base.ToBytes(writer);
                writer.Write(code.ToShortString());
                writer.Write(amount);
            }

            public override bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging)
            {
                if(attributes.KeyExists("amount"))
                {
                    amount = attributes["amount"].AsInt(0);
                    if(amount > 0)
                    {
                        if(attributes.KeyExists("code"))
                        {
                            string str = attributes["code"].AsString(null);
                            if(!string.IsNullOrEmpty(str))
                            {
                                code = new AssetLocation(str);
                                return true;//TODO: world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassInfo(code) != null
                            }
                        }
                    }
                }
                world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}: Wrong glass code or amount", nameof(GlassIntakeTool), sourceForErrorLogging);
                return false;
            }

            public override GlassBlowingToolStep Clone()
            {
                return new ToolStep() {
                    tool = tool,
                    shape = shape == null ? null : shape.Clone(),
                    code = code.Clone(),
                    amount = amount
                };
            }

            public override void GetStepInfo(ItemStack item, IAttribute data, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
            {
                IntAttribute amountAttribute = data as IntAttribute;
                dsc.AppendLine("  Required: " + Lang.Get(GlassBlend.GetBlendNameCode(code)));
                dsc.AppendLine("  Remaining amount: " + (amount - (amountAttribute == null ? 0 : amountAttribute.value)));
            }

            public override float GetStepProgress(ItemStack item, IAttribute data)
            {
                IntAttribute amountAttribute = data as IntAttribute;
                if(amountAttribute != null) return (float)amountAttribute.value / amount;
                return 0f;
            }

            public override void OnHeldInteractStart(ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
            {
                isComplete = false;
                if(firstEvent && blockSel != null && slot.Itemstack.Item is ItemGlassworkPipe)
                {
                    var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                    if(be != null)
                    {
                        var source = be as BlockEntityGlassSmeltery;
                        if(source != null)
                        {
                            int sourceAmount = source.GetGlassAmount();
                            if(sourceAmount > 0 && ((data as IntAttribute)?.value ?? 0) < amount)
                            {
                                if(byEntity.World.Side == EnumAppSide.Server)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("lastAddGlassTime", 0f);
                                }
                                handling = EnumHandHandling.PreventDefault;
                            }
                        }
                    }
                }
            }

            public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isComplete)
            {
                isComplete = false;
                if(blockSel == null) return false;
                var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
                if(source != null)
                {
                    IntAttribute amountAttribute = data as IntAttribute;
                    if(amountAttribute == null)
                    {
                        data = amountAttribute = new IntAttribute(0);
                    }

                    int sourceAmount = source.GetGlassAmount();
                    if(sourceAmount > 0 && amountAttribute.value < amount)
                    {
                        float speed = 1.5f;
                        if(byEntity.Api.Side == EnumAppSide.Client)
                        {
                            ModelTransform modelTransform = new ModelTransform();
                            modelTransform.EnsureDefaultValues();
                            modelTransform.Origin.Set(0.5f, 0.2f, 0.5f);
                            modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.5f, speed * secondsUsed), Math.Min(0.5f, speed * secondsUsed));
                            modelTransform.Scale = 1f - Math.Min(0.1f, speed * secondsUsed / 4f);
                            modelTransform.Rotation.X = -Math.Min(30f, secondsUsed * 180f * speed) + GameMath.FastSin(secondsUsed * 3f) * 5f;
                            modelTransform.Rotation.Z = GameMath.FastCos(secondsUsed * 3f) * 5f;
                            byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
                        }
                        float useTime = 2f;
                        if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= useTime)
                        {
                            if(slot.Itemstack.TempAttributes.GetFloat("lastAddGlassTime") + useTime <= secondsUsed)
                            {
                                slot.Itemstack.TempAttributes.SetFloat("lastAddGlassTime", (float)Math.Floor(secondsUsed));
                                int consumed = Math.Min(Math.Min(amount - amountAttribute.value, sourceAmount), (byEntity.Controls.Sneak ? 5 : 1) * (5 + (int)(amountAttribute.value * 0.01f)));
                                amountAttribute.value += consumed;
                                source.RemoveGlass(consumed);
                                slot.MarkDirty();
                                if(amountAttribute.value >= amount)
                                {
                                    isComplete = true;
                                    return false;
                                }
                            }
                        }
                        if(secondsUsed > 1f / speed)
                        {
                            IPlayer byPlayer = null;
                            if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                            source.SpawnGlassUseParticles(byEntity.World, blockSel, byPlayer);
                        }
                        return true;
                    }
                }
                return false;
            }

            public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
            }

            public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
                return true;
            }
        }
    }
}