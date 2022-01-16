using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GenericItemAction;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
    public class ItemGlassworkPipe : Item, IItemCrafter, IGenericHeldItemAction
    {
        public const float WORK_TEMPERATURE = 1100;

        private GlassMakingMod mod;
        private int maxGlassAmount;
        private ModelTransform glassTransform;
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            mod = api.ModLoader.GetModSystem<GlassMakingMod>();
            maxGlassAmount = Attributes["maxGlass"].AsInt();
            glassTransform = Attributes["glassTransform"].AsObject<ModelTransform>();
            if(api.Side == EnumAppSide.Client)
            {
                interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:heldhelp-glasspipe", delegate {
                    List<ItemStack> list = new List<ItemStack>();
                    var capi = api as ICoreClientAPI;
                    foreach(Block block in api.World.Blocks)
                    {
                        if(block is BlockGlassSmeltery)
                        {
                            List<ItemStack> stacks = block.GetHandBookStacks(capi);
                            if(stacks != null) list.AddRange(stacks);
                        }
                    }
                    return new WorldInteraction[] {
                        new WorldInteraction() {
                            ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = list.ToArray()
                        },
                        new WorldInteraction() {
                            ActionLangCode = "glassmaking:heldhelp-glasspipe-intake",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = list.ToArray()
                        }
                    };
                });
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            GlasspipeMeshCache.Dispose(api);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var itemstack = inSlot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    recipe.GetRecipeInfo(recipeAttribute, dsc, world, withDebugInfo);
                }
            }
            var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
            if(glasslayers != null)
            {
                dsc.AppendLine("Layers:");
                var codes = ((StringArrayAttribute)glasslayers["code"]).value;
                var amounts = ((IntArrayAttribute)glasslayers["amount"]).value;
                for(int i = 0; i < codes.Length; i++)
                {
                    dsc.Append("  ").Append(amounts[i]).Append("x ").AppendLine(GlassBlend.GetBlendNameCode(new AssetLocation(codes[i])));
                }
            }

            var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
            if(glassmelt != null)
            {
                dsc.AppendLine("Temperature: " + GetGlassTemperature(world, inSlot.Itemstack).ToString("0"));
                dsc.AppendLine();
                dsc.AppendLine("Break down to receive:");
                foreach(var pair in glassmelt)
                {
                    int amount = ((IntAttribute)pair.Value).value * 5 / 5;
                    if(amount > 0) dsc.Append("  ").Append(amount).Append("x ").AppendLine(GlassBlend.GetBlendNameCode(new AssetLocation(pair.Key)));
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            var itemstack = inSlot.Itemstack;
            var list = interactions.Append(base.GetHeldInteractionHelp(inSlot));
            if(!itemstack.Attributes.HasAttribute("glasslayers") && !itemstack.Attributes.HasAttribute("recipe"))
            {
                list = new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "glassmaking:heldhelp-glasspipe-recipe",
                        HotKeyCode = "itemrecipeselect",
                        MouseButton = EnumMouseButton.None
                    }
                }.Append(list);
            }
            return list;
        }

        public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
        {
            if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe && gridRecipe.Attributes?.IsTrue("breakglass") == true)
            {
                var glassmelt = stackInSlot.Itemstack.Attributes.GetTreeAttribute("glassmelt");
                if(glassmelt != null)
                {
                    var shardsItem = api.World.GetItem(new AssetLocation("glassmaking", "glassshards"));
                    foreach(var pair in glassmelt)
                    {
                        int count = ((IntAttribute)pair.Value).value * quantity / 5;
                        if(count > 0)
                        {
                            var item = new ItemStack(shardsItem, count);
                            new GlassBlend(new AssetLocation(pair.Key), 5).ToTreeAttributes(item.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
                            if(!byPlayer.Entity.TryGiveItemStack(item))
                            {
                                byPlayer.Entity.World.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                            }
                        }
                    }
                }
            }
            base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
        }

        public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
        {
            if(gridRecipe.Output.ResolvedItemstack?.Item is ItemGlassworkPipe && ingredient.ResolvedItemstack?.Item is ItemGlassworkPipe &&
                gridRecipe.Attributes?.IsTrue("breakglass") == true)
            {
                return inputStack.Attributes.HasAttribute("glassmelt");
            }
            return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            bool preventDefault = false;

            foreach(CollectibleBehavior behavior in CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling, ref handled);

                if(handled != EnumHandling.PassThrough) preventDefault = true;
                if(handled == EnumHandling.PreventSubsequent) return;
            }

            if(preventDefault) return;

            var itemstack = slot.Itemstack;
            if(itemstack.Attributes.HasAttribute("recipe")) return;
            if(firstEvent && blockSel != null)
            {
                var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                if(be != null)
                {
                    var mold = be as IGlassBlowingMold;
                    if(mold != null)
                    {
                        var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
                        if(glasslayers != null)
                        {
                            if(GetGlassTemperature(byEntity.World, itemstack) >= WORK_TEMPERATURE)
                            {
                                var codesAttrib = glasslayers["code"] as StringArrayAttribute;
                                var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
                                if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out _))
                                {
                                    byEntity.World.RegisterCallback((world, pos, dt) => {
                                        if(byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                                        {
                                            IPlayer dualCallByPlayer = null;
                                            if(byEntity is EntityPlayer)
                                            {
                                                dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                                            }
                                            world.PlaySoundAt(new AssetLocation("sounds/sizzle"), byEntity, dualCallByPlayer);
                                        }
                                    }, blockSel.Position, 666);
                                    handling = EnumHandHandling.PreventDefault;
                                    return;
                                }
                            }
                        }
                    }
                    var source = be as BlockEntityGlassSmeltery;
                    if(source != null && source.CanInteract(byEntity, blockSel))
                    {
                        if(GetGlassTemperature(byEntity.World, itemstack) >= WORK_TEMPERATURE)
                        {
                            int amount = source.GetGlassAmount();
                            if(amount > 0 && CanAddGlass(byEntity, slot, amount, source.GetGlassCode(), byEntity.Controls.Sneak ? 5 : 1))
                            {
                                if(byEntity.World.Side == EnumAppSide.Server)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", 0f);
                                }
                                handling = EnumHandHandling.PreventDefault;
                                return;
                            }
                        }
                    }
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            bool result = true;
            bool preventDefault = false;

            foreach(CollectibleBehavior behavior in CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);

                if(handled != EnumHandling.PassThrough)
                {
                    result &= behaviorResult;
                    preventDefault = true;
                }
                if(handled == EnumHandling.PreventSubsequent) return result;
            }
            if(preventDefault) return result;

            var itemstack = slot.Itemstack;
            if(itemstack.Attributes.HasAttribute("recipe") || blockSel == null) return false;
            var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if(be != null)
            {
                var mold = be as IGlassBlowingMold;
                if(mold != null)
                {
                    var glasslayers = itemstack.Attributes.GetTreeAttribute("glasslayers");
                    if(glasslayers != null)
                    {
                        if(GetGlassTemperature(byEntity.World, itemstack) >= WORK_TEMPERATURE)
                        {
                            var codesAttrib = glasslayers["code"] as StringArrayAttribute;
                            var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
                            if(mold.CanReceiveGlass(codesAttrib.value, amountsAttrib.value, out float fillTime))
                            {
                                const float speed = 1.5f;
                                if(api.Side == EnumAppSide.Client)
                                {
                                    ModelTransform modelTransform = new ModelTransform();
                                    modelTransform.EnsureDefaultValues();
                                    modelTransform.Origin.Z = 0;
                                    modelTransform.Translation.Set(-Math.Min(1.275f, speed * secondsUsed * 1.5f), -Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.25f, speed * Math.Max(0, secondsUsed - 0.5f) * 0.5f));
                                    modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                                    modelTransform.Rotation.X = -Math.Min(25f, secondsUsed * 45f * speed);
                                    byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
                                }
                                if(api.Side == EnumAppSide.Server && secondsUsed >= 1f + fillTime)
                                {
                                    mold.TakeGlass(byEntity, codesAttrib.value, amountsAttrib.value);
                                    Dictionary<string, int> shards = new Dictionary<string, int>();
                                    for(int i = 0; i < codesAttrib.value.Length; i++)
                                    {
                                        if(amountsAttrib.value[i] > 0)
                                        {
                                            int count;
                                            if(!shards.TryGetValue(codesAttrib.value[i], out count)) count = 0;
                                            shards[codesAttrib.value[i]] = count + amountsAttrib.value[i];
                                        }
                                    }
                                    itemstack.Attributes.RemoveAttribute("glassmelt");
                                    itemstack.Attributes.RemoveAttribute("glasslayers");
                                    slot.MarkDirty();

                                    var shardsItem = api.World.GetItem(new AssetLocation("glassmaking", "glassshards"));
                                    foreach(var pair in shards)
                                    {
                                        int quantity = pair.Value / 5;
                                        if(quantity > 0)
                                        {
                                            var item = new ItemStack(shardsItem, quantity);
                                            new GlassBlend(new AssetLocation(pair.Key), 5).ToTreeAttributes(item.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
                                            if(!byEntity.TryGiveItemStack(item))
                                            {
                                                byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                                            }
                                        }
                                    }
                                    return false;
                                }
                                if(secondsUsed > 1f / speed)
                                {
                                    IPlayer byPlayer = null;
                                    if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                                    // Smoke on the mold
                                    Vec3d blockpos = blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5);
                                    float y2 = 0;
                                    Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                                    Cuboidf[] collboxs = block.GetCollisionBoxes(byEntity.World.BlockAccessor, blockSel.Position);
                                    for(int i = 0; collboxs != null && i < collboxs.Length; i++)
                                    {
                                        y2 = Math.Max(y2, collboxs[i].Y2);
                                    }
                                    byEntity.World.SpawnParticles(
                                        Math.Max(1, 12 - (secondsUsed - 1) * 6),
                                        ColorUtil.ToRgba(50, 220, 220, 220),
                                        blockpos.AddCopy(-0.5, y2 - 2 / 16f, -0.5),
                                        blockpos.Add(0.5, y2 - 2 / 16f + 0.15, 0.5),
                                        new Vec3f(-0.5f, 0f, -0.5f),
                                        new Vec3f(0.5f, 0f, 0.5f),
                                        1.5f,
                                        -0.05f,
                                        0.75f,
                                        EnumParticleModel.Quad,
                                        byPlayer
                                    );
                                }
                                return true;
                            }
                        }
                    }
                }
                var source = be as BlockEntityGlassSmeltery;
                if(source != null && source.CanInteract(byEntity, blockSel))
                {
                    if(GetGlassTemperature(byEntity.World, itemstack) >= WORK_TEMPERATURE)
                    {
                        int amount = source.GetGlassAmount();
                        if(amount > 0 && CanAddGlass(byEntity, slot, amount, source.GetGlassCode(), byEntity.Controls.Sneak ? 5 : 1))
                        {
                            const float speed = 1.5f;
                            if(api.Side == EnumAppSide.Client)
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
                            if(api.Side == EnumAppSide.Server && secondsUsed >= useTime)
                            {
                                if(slot.Itemstack.TempAttributes.GetFloat("glassmaking:lastAddGlassTime") + useTime <= secondsUsed)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", (float)Math.Floor(secondsUsed));
                                    if(amount > 0 && AddGlass(byEntity, slot, amount, source.GetGlassCode(), byEntity.Controls.Sneak ? 5 : 1, source.GetTemperature(), out int consumed))
                                    {
                                        source.RemoveGlass(consumed);
                                        slot.MarkDirty();
                                        return true;
                                    }
                                    else
                                    {
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
                }
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
            if(api.Side == EnumAppSide.Client)
            {
                if(slot.Itemstack.Attributes.HasAttribute("glasslayers") || slot.Itemstack.Attributes.HasAttribute("recipe"))
                {
                    SetMeshDirty(slot.Itemstack);
                }
                else
                {
                    MeshContainer.Remove(api, slot.Itemstack);
                }
            }
        }

        public virtual float GetGlassTemperature(IWorldAccessor world, ItemStack itemstack)
        {
            return WORK_TEMPERATURE;
            if(itemstack == null || itemstack.Attributes == null || itemstack.Attributes["glassTemperature"] == null || !(itemstack.Attributes["glassTemperature"] is ITreeAttribute))
            {
                return 20f;
            }
            ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
            double totalHours = world.Calendar.TotalHours;
            double lastUpdate = attr.GetDouble("temperatureLastUpdate");
            if(totalHours - lastUpdate > 1.0 / 85)
            {
                float temperature = Math.Max(0f, attr.GetFloat("temperature", 20f) - Math.Max(0f, (float)(totalHours - lastUpdate) * 180f));
                SetGlassTemperature(world, itemstack, temperature);
                return temperature;
            }
            return attr.GetFloat("temperature", 20f);
        }

        public virtual void SetGlassTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
        {
            if(itemstack != null)
            {
                ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["glassTemperature"];
                if(attr == null)
                {
                    attr = new TreeAttribute();
                    itemstack.Attributes["glassTemperature"] = attr;
                }
                double totalHours = world.Calendar.TotalHours;
                if(delayCooldown && attr.GetFloat("temperature") < temperature)
                {
                    totalHours += 0.5;
                }
                attr.SetDouble("temperatureLastUpdate", totalHours);
                attr.SetFloat("temperature", temperature);
            }
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if(itemstack.Attributes.HasAttribute("glasslayers"))
            {
                var container = MeshContainer.Get(capi, itemstack);
                var temperature = MeshContainer.TemperatureToState(itemstack.Attributes.GetFloat("glassTemperature", 20));
                if(container.isDirty || !container.hasMesh || container.temperature != temperature)
                {
                    container.temperature = temperature;
                    var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
                    if(glassmelt != null) UpdateGlassmeltMesh(itemstack, glassmelt, MeshContainer.StateToGlow(temperature));
                }

                container.UpdateMeshRef(capi, Shape, capi.Tesselator.GetTextureSource(this), glassTransform);
                renderinfo.ModelRef = container.meshRef;
                renderinfo.CullFaces = true;
                return;
            }
            else
            {
                var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
                if(recipeAttribute != null)
                {
                    var container = MeshContainer.Get(capi, itemstack);
                    var temperature = MeshContainer.TemperatureToState(GetGlassTemperature(capi.World, itemstack));
                    if(container.isDirty || !container.hasMesh || container.temperature != temperature)
                    {
                        container.temperature = temperature;
                        UpdateRecipeMesh(itemstack, recipeAttribute, MeshContainer.StateToGlow(temperature));
                    }

                    container.UpdateMeshRef(capi, Shape, capi.Tesselator.GetTextureSource(this), glassTransform);
                    renderinfo.ModelRef = container.meshRef;
                    renderinfo.CullFaces = true;
                    return;
                }
                else
                {
                    MeshContainer.Remove(capi, itemstack);
                }
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        public bool TryGetRecipeAttribute(ItemStack itemstack, out ITreeAttribute recipeAttribute)
        {
            recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            return recipeAttribute != null;
        }

        public void OnRecipeUpdated(ItemSlot slot, bool isComplete)
        {
            if(isComplete)
            {
                slot.Itemstack.Attributes.RemoveAttribute("recipe");
                slot.Itemstack.Attributes.RemoveAttribute("glassmelt");
                slot.MarkDirty();
            }
            else if(api.Side == EnumAppSide.Client)
            {
                SetMeshDirty(slot.Itemstack);
            }
        }

        public void AddGlassmelt(IWorldAccessor world, ItemStack item, AssetLocation code, int amount, float temperature)
        {
            string glassCode = code.ToShortString();
            var glassmelt = item.Attributes.GetOrAddTreeAttribute("glassmelt");
            glassmelt.SetInt(glassCode, glassmelt.GetInt(glassCode) + amount);
            int currentAmount = 0;
            foreach(var pair in glassmelt)
            {
                currentAmount += ((IntAttribute)pair.Value).value;
            }
            SetGlassTemperature(world, item, GameMath.Lerp(GetTemperature(world, item), temperature, (float)amount / currentAmount));
        }

        bool IItemCrafter.PreventRecipeAssignment(IClientPlayer player, ItemStack item)
        {
            return item.Attributes.HasAttribute("recipe") || item.Attributes.HasAttribute("glasslayers");
        }

        bool IItemCrafter.TryGetRecipeOutputs(IClientPlayer player, ItemStack item, out KeyValuePair<IAttribute, ItemStack>[] recipeOutputs)
        {
            var recipes = mod.GetGlassBlowingRecipes();
            recipeOutputs = default;
            if(recipes.Count == 0) return false;

            recipeOutputs = new KeyValuePair<IAttribute, ItemStack>[recipes.Count];
            int index = 0;
            foreach(var pair in recipes)
            {
                recipeOutputs[index++] = new KeyValuePair<IAttribute, ItemStack>(new StringAttribute(pair.Key), pair.Value.output.ResolvedItemstack);
            }
            return index > 0;
        }

        bool IGenericHeldItemAction.GenericHeldItemAction(IPlayer player, string action, ITreeAttribute attributes)
        {
            if(action == "recipe")
            {
                var code = attributes.GetString("key");
                if(!string.IsNullOrEmpty(code))
                {
                    var recipe = mod.GetGlassBlowingRecipe(code);
                    if(recipe != null)
                    {
                        var slot = player.Entity.RightHandItemSlot;
                        slot.Itemstack.Attributes.GetOrAddTreeAttribute("recipe").SetString("code", code);
                        slot.MarkDirty();
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanAddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier)
        {
            var glassmelt = slot.Itemstack.Attributes.GetTreeAttribute("glassmelt");
            if(glassmelt != null)
            {
                int count = 0;
                foreach(var pair in glassmelt)
                {
                    count += ((IntAttribute)pair.Value).value;
                }
                if(count >= maxGlassAmount) return false;
            }
            return true;
        }

        private bool AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier, float temperature, out int consumed)
        {
            int currentAmount = 0;
            var glassmelt = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glassmelt");
            foreach(var pair in glassmelt)
            {
                currentAmount += ((IntAttribute)pair.Value).value;
            }

            var glasslayers = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glasslayers");
            var codesAttrib = glasslayers["code"] as StringArrayAttribute;
            var amountsAttrib = glasslayers["amount"] as IntArrayAttribute;
            if(codesAttrib == null)
            {
                codesAttrib = new StringArrayAttribute(new string[0]);
                glasslayers["code"] = codesAttrib;
                amountsAttrib = new IntArrayAttribute(new int[0]);
                glasslayers["amount"] = amountsAttrib;
            }

            string glassCode = code.ToShortString();
            consumed = Math.Min(maxGlassAmount - currentAmount, Math.Min(amount, multiplier * (5 + (int)(currentAmount * 0.01f))));
            if(codesAttrib.value.Length > 0 && codesAttrib.value[codesAttrib.value.Length - 1] == glassCode)
            {
                amountsAttrib.value[amountsAttrib.value.Length - 1] += consumed;
            }
            else
            {
                codesAttrib.value = codesAttrib.value.Append(glassCode);
                amountsAttrib.value = amountsAttrib.value.Append(consumed);
            }

            AddGlassmelt(byEntity.World, slot.Itemstack, code, consumed, temperature);

            return true;
        }

        private void SetMeshDirty(ItemStack item)
        {
            MeshContainer.Get(api, item).isDirty = true;
        }

        private void UpdateGlassmeltMesh(ItemStack item, ITreeAttribute glassmelt, int glow)
        {
            int count = 0;
            foreach(var pair in glassmelt)
            {
                count += ((IntAttribute)pair.Value).value;
            }
            var container = MeshContainer.Get(api, item);
            if(container._data == null || (int)container._data != count)
            {
                const double invPI = 1.0 / Math.PI;
                container._data = count;

                var root = Math.Pow(count * invPI, 1.0 / 3.0);
                var shape = new SmoothRadialShape();
                shape.segments = GameMath.Max(1, (int)Math.Floor(root)) * 2 + 3;

                float radius = (float)Math.Sqrt(count * invPI / root);
                float length = (float)(root * 1.5);
                shape.outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() {
                    vertices = new float[][] {
                       new float[] { -3, 0 },
                       new float[] { length * 0.1f, radius  },
                       new float[] { length, radius },
                       new float[] { length, 0 }
                    }
                } };
                container.BeginMeshChange();
                SmoothRadialShape.BuildMesh(container._mesh, shape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
                container.EndMeshChange();
            }
        }

        private void UpdateRecipeMesh(ItemStack item, ITreeAttribute recipeAttribute, int glow)
        {
            var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
            if(recipe != null)
            {
                recipe.UpdateMesh(recipeAttribute, MeshContainer.Get(api, item), glow);
            }
        }

        private class MeshContainer : IMeshContainer
        {
            private static int counter = 0;

            public object _data;
            public MeshData _mesh;
            public MeshRef meshRef = null;

            public bool isDirty = false;
            public TemperatureState temperature;

            public bool hasMesh => meshRef != null || updateMesh.HasValue;

            object IMeshContainer.data { get { return _data; } set { _data = value; } }
            MeshData IMeshContainer.mesh { get { return _mesh; } }

            private bool? updateMesh = null;
            private int prevVertices, prevIndices;

            private MeshContainer() { }

            public void BeginMeshChange()
            {
                _mesh.Clear();
            }

            public void EndMeshChange()
            {
                isDirty = false;
                if(meshRef == null || _mesh.VerticesCount != prevVertices || _mesh.IndicesCount != prevIndices)
                {
                    prevVertices = _mesh.VerticesCount;
                    prevIndices = _mesh.IndicesCount;
                    updateMesh = true;
                }
                else
                {
                    updateMesh = false;
                }
            }

            public static MeshContainer Get(ICoreAPI api, ItemStack item)
            {
                var id = item.TempAttributes.TryGetInt("tmpMeshId");
                var cache = GlasspipeMeshCache.Get(api);
                if(id.HasValue && cache.containers.TryGetValue(id.Value, out var container))
                {
                    return container;
                }
                else
                {
                    container = new MeshContainer();
                    container._mesh = new MeshData(16, 16, true, true, true, true).WithColorMaps();
                    item.TempAttributes.SetInt("tmpMeshId", counter);
                    cache.containers[counter] = container;
                    counter++;
                    return container;
                }
            }

            public static bool TryGet(ICoreAPI api, ItemStack item, out MeshContainer container)
            {
                var id = item.TempAttributes.TryGetInt("tmpMeshId");
                if(id.HasValue)
                {
                    if(GlasspipeMeshCache.TryGet(api, out var cache) && cache.containers.TryGetValue(id.Value, out container))
                    {
                        return true;
                    }
                }
                container = default;
                return false;
            }

            public static void Remove(ICoreAPI api, ItemStack item)
            {
                var id = item.TempAttributes.TryGetInt("tmpMeshId");
                if(id.HasValue)
                {
                    item.TempAttributes.RemoveAttribute("tmpMeshId");
                    if(GlasspipeMeshCache.TryGet(api, out var cache) && cache.containers.TryGetValue(id.Value, out var container))
                    {
                        cache.containers.Remove(id.Value);
                        container.Dispose();
                    }
                }
            }

            public void Dispose()
            {
                if(meshRef != null)
                {
                    meshRef.Dispose();
                    meshRef = null;
                }
            }

            public void UpdateMeshRef(ICoreClientAPI capi, CompositeShape shape, ITexPositionSource tex, ModelTransform meshTransform)
            {
                if(updateMesh.HasValue)
                {
                    _mesh.SetTexPos(tex["glass"]);
                    GlasspipeMeshCache.TryGet(capi, out var cache);
                    var baseMesh = cache.GetMesh(capi, shape, tex);
                    var toUpload = new MeshData(baseMesh.VerticesCount + _mesh.VerticesCount, baseMesh.IndicesCount + _mesh.IndicesCount, false, true, true, true);
                    toUpload.AddMeshData(baseMesh);
                    _mesh.ModelTransform(meshTransform);
                    toUpload.AddMeshData(_mesh);
                    if(updateMesh.Value)
                    {
                        if(meshRef != null) meshRef.Dispose();
                        meshRef = capi.Render.UploadMesh(toUpload);
                    }
                    else
                    {
                        capi.Render.UpdateMesh(meshRef, toUpload);
                    }
                    updateMesh = null;
                }
            }

            public static TemperatureState TemperatureToState(float temperature)
            {
                if(temperature < 500) return TemperatureState.Cold;
                if(temperature < 1100) return TemperatureState.Heated;
                return TemperatureState.Working;
            }

            public static int StateToGlow(TemperatureState state)
            {
                switch(state)
                {
                    case TemperatureState.Heated: return 127;
                    case TemperatureState.Working: return 255;
                    default: return 0;
                }
            }
        }

        private class GlasspipeMeshCache
        {
            public const string KEY = "glassmaking:glasspipemesh";

            public Dictionary<int, MeshContainer> containers = new Dictionary<int, MeshContainer>();

            private MeshData mesh = null;

            public MeshData GetMesh(ICoreClientAPI capi, CompositeShape shape, ITexPositionSource tex)
            {
                if(mesh == null)
                {
                    Shape shapeBase = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
                    capi.Tesselator.TesselateShape("pipemesh", shapeBase, out mesh, tex, new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), 0, 0, 0);
                }
                return mesh;
            }

            public static GlasspipeMeshCache Get(ICoreAPI api)
            {
                return ObjectCacheUtil.GetOrCreate(api, KEY, () => new GlasspipeMeshCache());
            }

            public static bool TryGet(ICoreAPI api, out GlasspipeMeshCache cache)
            {
                cache = ObjectCacheUtil.TryGet<GlasspipeMeshCache>(api, KEY);
                return cache != null;
            }

            public static void Dispose(ICoreAPI api)
            {
                if(TryGet(api, out var cache))
                {
                    ObjectCacheUtil.Delete(api, KEY);
                    foreach(var pair in cache.containers)
                    {
                        pair.Value.Dispose();
                    }
                }
            }
        }

        private enum TemperatureState
        {
            Cold,
            Heated,
            Working
        }

        public interface IMeshContainer
        {
            object data { get; set; }
            MeshData mesh { get; }

            void BeginMeshChange();

            void EndMeshChange();
        }
    }
}