﻿using GlassMaking.Blocks;
using GlassMaking.Items;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
    public class GlassIntakeTool : GlassblowingToolBehavior
    {
        //private WorldInteraction[] interactions;

        public GlassIntakeTool(CollectibleObject collObj) : base(collObj)
        {
        }

        //public override void OnLoaded(ICoreAPI api)
        //{
        //    List<ItemStack> list = new List<ItemStack>();
        //    foreach(Block block in api.World.Blocks)
        //    {
        //        if(block is BlockGlassSmeltery)
        //        {
        //            List<ItemStack> stacks = block.GetHandBookStacks(api);
        //            if(stacks != null) list.AddRange(stacks);
        //        }
        //    }
        //    interactions = new WorldInteraction[] {
        //        new WorldInteraction() {
        //            ActionLangCode = "glassmaking:heldhelp-gbtool-intake",
        //            MouseButton = EnumMouseButton.Right,
        //            Itemstacks = list.ToArray()
        //        },
        //        new WorldInteraction() {
        //            ActionLangCode = "glassmaking:heldhelp-gbtool-intake",
        //            MouseButton = EnumMouseButton.Right,
        //            HotKeyCode = "sneak",
        //            Itemstacks = list.ToArray()
        //        }
        //    };
        //}

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if(firstEvent && blockSel != null && TryGetRecipeStep(slot, byEntity, out var step))
            {
                var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
                if(source != null && source.CanInteract(byEntity, blockSel))
                {
                    if(step.BeginStep())
                    {
                        int sourceAmount = source.GetGlassAmount();
                        int intake = slot.Itemstack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
                        if(sourceAmount > 0 && intake < step.stepAttributes["amount"].AsInt())
                        {
                            if(byEntity.World.Side == EnumAppSide.Server)
                            {
                                slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", 0f);
                            }
                            handHandling = EnumHandHandling.PreventDefault;
                            handling = EnumHandling.PreventSubsequent;
                        }
                    }
                }
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if(blockSel != null && TryGetRecipeStep(slot, byEntity, out var step))
            {
                if(step.ContinueStep())
                {
                    var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
                    if(source != null && source.CanInteract(byEntity, blockSel))
                    {
                        int intake = slot.Itemstack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
                        int sourceAmount = source.GetGlassAmount();
                        int amount = step.stepAttributes["amount"].AsInt();
                        if(sourceAmount > 0 && intake < amount)
                        {
                            const float speed = 1.5f;
                            if(byEntity.Api.Side == EnumAppSide.Client)
                            {
                                ModelTransform modelTransform = new ModelTransform();
                                modelTransform.EnsureDefaultValues();
                                modelTransform.Origin.Set(0f, 0f, 0f);
                                modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.5f, speed * secondsUsed), Math.Min(0.5f, speed * secondsUsed));
                                modelTransform.Scale = 1f - Math.Min(0.1f, speed * secondsUsed / 4f);
                                modelTransform.Rotation.X = -Math.Min(10f, secondsUsed * 45f * speed);
                                modelTransform.Rotation.Y = -Math.Min(15f, secondsUsed * 45f * speed) + GameMath.FastSin(secondsUsed * 1.5f);
                                modelTransform.Rotation.Z = secondsUsed * 90f % 360f;
                                byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
                            }
                            const float useTime = 2f;
                            if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= useTime)
                            {
                                if(slot.Itemstack.TempAttributes.GetFloat("glassmaking:lastAddGlassTime") + useTime <= secondsUsed)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", (float)Math.Floor(secondsUsed));
                                    int consumed = Math.Min(Math.Min(amount - intake, sourceAmount), (byEntity.Controls.Sneak ? 5 : 1) * (5 + (int)(intake * 0.01f)));

                                    var code = AssetLocation.Create(step.stepAttributes["code"].AsString(), step.recipe.code.Domain);
                                    ((ItemGlassworkPipe)slot.Itemstack.Item).AddGlassmelt(byEntity.World, slot.Itemstack, code, consumed, source.GetTemperature());

                                    intake += consumed;
                                    source.RemoveGlass(consumed);

                                    if(intake >= amount)
                                    {
                                        slot.Itemstack.Attributes.RemoveAttribute("glassmaking:toolIntakeAmount");
                                        slot.MarkDirty();

                                        step.CompleteStep(byEntity);
                                        handling = EnumHandling.PreventSubsequent;
                                        return false;
                                    }

                                    slot.Itemstack.Attributes.SetInt("glassmaking:toolIntakeAmount", intake);
                                    step.SetProgress((float)intake / amount);
                                    slot.MarkDirty();
                                }
                            }
                            if(secondsUsed > 1f / speed)
                            {
                                IPlayer byPlayer = null;
                                if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                                source.SpawnGlassUseParticles(byEntity.World, blockSel, byPlayer);
                            }
                            handling = EnumHandling.PreventSubsequent;
                            return true;
                        }
                    }
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
            slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}