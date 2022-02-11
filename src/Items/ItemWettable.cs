using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Items
{
    public class ItemWettable : Item, IWettable
    {
        private AssetLocation waterCode = new AssetLocation("waterportion");

        private float capacity;
        private float evaporation;

        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capacity = Attributes["maxHumidity"].AsFloat();
            evaporation = Attributes["evaporation"].AsFloat();
            if(api.Side == EnumAppSide.Client)
            {
                interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:heldhelp-wettable", () => {
                    var capi = (ICoreClientAPI)api;
                    var containers = api.World.GetItem(waterCode).GetHandBookStacks(capi) ?? new List<ItemStack>();
                    return new WorldInteraction[] {
                        new WorldInteraction() {
                            ActionLangCode = "glassmaking:heldhelp-wettable-wet",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = containers.ToArray()
                        }
                    };
                });
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if(firstEvent && handling != EnumHandHandling.PreventDefault)
            {
                if(blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockLiquidContainerBase container)
                {
                    if(container.IsTopOpened)
                    {
                        var stack = container.GetContent(blockSel.Position);
                        if(stack != null && stack.Collectible.Code.Equals(waterCode))
                        {
                            var props = BlockLiquidContainerBase.GetContainableProps(stack);
                            if(props != null)
                            {
                                var value = GetHumidity(slot.Itemstack, byEntity.World);
                                int takeAmount = (int)Math.Ceiling((capacity - value) * props.ItemsPerLitre);
                                if(takeAmount > 0)
                                {
                                    stack = container.TryTakeContent(blockSel.Position, takeAmount);
                                    if(stack != null)
                                    {
                                        SetHumidity(slot.Itemstack, Math.Min(capacity, value + stack.StackSize / props.ItemsPerLitre));
                                        slot.MarkDirty();
                                        handling = EnumHandHandling.PreventDefault;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual float GetHumidity(ItemStack itemStack, IWorldAccessor world)
        {
            var state = UpdateAndGetTransitionState(world, new DummySlot(itemStack), EnumTransitionType.Dry);
            return state == null ? 0 : (capacity - state.TransitionedHours * evaporation);
        }

        public virtual void SetHumidity(ItemStack itemStack, float value)
        {
            SetTransitionState(itemStack, EnumTransitionType.Dry, (capacity - value) / evaporation);
        }

        public virtual void ConsumeHumidity(ItemStack itemStack, float value, IWorldAccessor world)
        {
            SetHumidity(itemStack, GetHumidity(itemStack, world) - value);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
            SetHumidity(outputSlot.Itemstack, 0);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            if(GetHumidity(itemStack, api.World) >= 0.01)
            {
                return Lang.Get("{0} (Wet)", base.GetHeldItemName(itemStack));
            }
            return base.GetHeldItemName(itemStack);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var value = GetHumidity(inSlot.Itemstack, world);
            if(value >= 0.01) dsc.AppendLine(Lang.Get("Humidity: {0}", value));
        }

        public override void SetTransitionState(ItemStack stack, EnumTransitionType type, float transitionedHours)
        {
            if(type == EnumTransitionType.Dry)
            {
                ITreeAttribute attr = (ITreeAttribute)stack.Attributes["transitionstate"];

                if(attr == null)
                {
                    stack.Attributes["transitionstate"] = new TreeAttribute();
                    UpdateAndGetTransitionState(api.World, new DummySlot(stack), type);
                    attr = (ITreeAttribute)stack.Attributes["transitionstate"];
                }

                ((FloatArrayAttribute)attr["transitionedHours"]).value[0] = transitionedHours;
            }
        }

        public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {
            ItemStack itemstack = inslot.Itemstack;

            if(itemstack.Attributes == null)
            {
                itemstack.Attributes = new TreeAttribute();
            }

            if(!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
            {
                return null;
            }

            ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["transitionstate"];

            float[] transitionedHours;
            float[] freshHours;
            float[] transitionHours;
            TransitionState[] states = new TransitionState[1];

            if(!attr.HasAttribute("createdTotalHours"))
            {
                attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
                attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);

                freshHours = new float[1];
                transitionHours = new float[1];
                transitionedHours = new float[1];

                transitionedHours[0] = 0;
                freshHours[0] = capacity / evaporation;
                transitionHours[0] = capacity / evaporation;

                attr["freshHours"] = new FloatArrayAttribute(freshHours);
                attr["transitionHours"] = new FloatArrayAttribute(transitionHours);
                attr["transitionedHours"] = new FloatArrayAttribute(transitionedHours);
            }
            else
            {
                freshHours = (attr["freshHours"] as FloatArrayAttribute).value;
                transitionHours = (attr["transitionHours"] as FloatArrayAttribute).value;
                transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute).value;
            }

            if(transitionHours[0] <= 0)
            {
                return null;
            }

            double lastUpdatedTotalHours = attr.GetDouble("lastUpdatedTotalHours");
            double nowTotalHours = world.Calendar.TotalHours;

            float hoursPassed = (float)(nowTotalHours - lastUpdatedTotalHours);

            float transitionRateMul = GetTransitionRateMul(world, inslot, EnumTransitionType.Dry);

            if(hoursPassed > 0.05f)
            {
                float hoursPassedAdjusted = hoursPassed * transitionRateMul;
                transitionedHours[0] += hoursPassedAdjusted;
            }

            float freshHoursLeft = Math.Max(0, freshHours[0] - transitionedHours[0]);
            if(freshHoursLeft <= 0)
            {
                itemstack.Attributes.RemoveAttribute("transitionstate");
                if(!(inslot is DummySlot)) inslot.MarkDirty();
                return null;
            }

            states[0] = new TransitionState() {
                FreshHoursLeft = freshHoursLeft,
                TransitionLevel = 0,
                TransitionedHours = transitionedHours[0],
                TransitionHours = transitionHours[0],
                FreshHours = freshHours[0],
                Props = new TransitionableProperties() { TransitionRatio = 1, Type = EnumTransitionType.Dry }
            };

            if(hoursPassed > 0.05f)
            {
                attr.SetDouble("lastUpdatedTotalHours", nowTotalHours);
            }

            return states;
        }
    }
}