using GlassMaking.Blocks;
using GlassMaking.Common;
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
    public class ItemGlassworkPipe : Item
    {
        private int MAX_GLASS_AMOUNT = 1000;

        private Item shardsItem;
        private GlassMakingMod mod;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            mod = api.ModLoader.GetModSystem<GlassMakingMod>();
            shardsItem = api.World.GetItem(new AssetLocation("glassmaking", "glassshards"));
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
                var codes = (glasslayers["code"] as StringArrayAttribute).value;
                var amounts = (glasslayers["amount"] as IntArrayAttribute).value;
                for(int i = 0; i < codes.Length; i++)
                {
                    dsc.AppendLine("  " + amounts[i] + "x " + GlassBlend.GetBlendNameCode(new AssetLocation(codes[i])));
                }
            }

            var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
            if(glassmelt != null)
            {
                dsc.AppendLine();
                dsc.AppendLine("Break down to receive:");
                foreach(var pair in glassmelt)
                {
                    int amount = (pair.Value as IntAttribute).value * 5 / 5;
                    if(amount > 0) dsc.AppendLine("  " + amount + "x " + GlassBlend.GetBlendNameCode(new AssetLocation(pair.Key)));
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            var itemstack = inSlot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    return recipe.GetHeldInteractionHelp(recipeAttribute);
                }
            }
            return base.GetHeldInteractionHelp(inSlot);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if(firstEvent)
            {
                var itemstack = slot.Itemstack;
                var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
                if(recipeAttribute != null)
                {
                    var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                    if(recipe != null)
                    {
                        //TODO: передать renderer в котором можно будет изменить внешний вид
                        recipe.OnHeldInteractStart(slot, ref recipeAttribute, byEntity, blockSel, entitySel, firstEvent, ref handling);
                        if(recipeAttribute == null)
                        {
                            handling = EnumHandHandling.PreventDefault;
                            itemstack.Attributes.RemoveAttribute("recipe");
                            slot.MarkDirty();
                        }
                        return;
                    }
                }
                else
                {
                    var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                    if(be != null)
                    {
                        var mold = be as BlockEntityGlassBlowingMold;
                        if(mold != null)
                        {

                        }
                        var source = be as BlockEntityGlassSmeltery;
                        if(source != null)
                        {
                            int amount = source.GetGlassAmount();
                            if(amount > 0 && CanAddGlass(byEntity, slot, amount, source.GetGlassCode(), (!byEntity.Controls.Sneak) ? 1 : 5))
                            {
                                if(byEntity.World.Side == EnumAppSide.Server)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("lastAddGlassTime", 0f);
                                }
                                handling = EnumHandHandling.PreventDefault;
                                return;
                            }
                        }
                    }
                }
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(blockSel == null) return false;
            var itemstack = slot.Itemstack;
            var recipeAttribute = itemstack.Attributes.GetTreeAttribute("recipe");
            if(recipeAttribute != null)
            {
                var recipe = mod.GetGlassBlowingRecipe(recipeAttribute.GetString("code"));
                if(recipe != null)
                {
                    //TODO: передать renderer в котором можно будет изменить внешний вид
                    bool result = recipe.OnHeldInteractStep(secondsUsed, slot, ref recipeAttribute, byEntity, blockSel, entitySel);
                    if(recipeAttribute == null)
                    {
                        itemstack.Attributes.RemoveAttribute("recipe");
                        slot.MarkDirty();
                        return false;
                    }
                    return result;
                }
            }
            else
            {
                var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                if(be != null)
                {
                    var mold = be as BlockEntityGlassBlowingMold;
                    if(mold != null)
                    {

                    }
                    var source = be as BlockEntityGlassSmeltery;
                    if(source != null)
                    {
                        int amount = source.GetGlassAmount();
                        if(amount > 0 && CanAddGlass(byEntity, slot, amount, source.GetGlassCode(), (!byEntity.Controls.Sneak) ? 1 : 5))
                        {
                            float speed = 1.5f;
                            if(api.Side == EnumAppSide.Client)
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
                            if(api.Side == EnumAppSide.Server && secondsUsed >= useTime)
                            {
                                if(slot.Itemstack.TempAttributes.GetFloat("lastAddGlassTime") + useTime <= secondsUsed)
                                {
                                    slot.Itemstack.TempAttributes.SetFloat("lastAddGlassTime", (float)Math.Floor(secondsUsed));
                                    if(amount > 0 && AddGlass(byEntity, slot, amount, source.GetGlassCode(), (!byEntity.Controls.Sneak) ? 1 : 5, out int consumed))
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
            slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            slot.Itemstack.TempAttributes.RemoveAttribute("lastAddGlassTime");
            return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
            if(api.Side == EnumAppSide.Client)
            {
                if(slot.Itemstack.Attributes.HasAttribute("glasslayers"))
                {
                    SetMeshDirty(slot.Itemstack);
                }
            }
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if(itemstack.Attributes.HasAttribute("glasslayers"))
            {
                var container = MeshContainer.Get(capi, itemstack);
                if(container.isDirty || !container.hasMesh)
                {
                    var glassmelt = itemstack.Attributes.GetTreeAttribute("glassmelt");
                    if(glassmelt != null) UpdateGlassmeltMesh(itemstack, glassmelt);
                }

                container.UpdateMeshRef(capi, Shape, capi.Tesselator.GetTextureSource(this));
                renderinfo.ModelRef = container.meshRef;
                renderinfo.CullFaces = true;
                return;
            }
            else
            {
                //TODO: if has no render attributes - remove & dispose container
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        private bool CanAddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier)
        {
            var glassmelt = slot.Itemstack.Attributes.GetTreeAttribute("glassmelt");
            if(glassmelt != null)
            {
                int count = 0;
                foreach(var pair in glassmelt)
                {
                    count += (pair.Value as IntAttribute).value;
                }
                if(count >= MAX_GLASS_AMOUNT) return false;
            }
            return true;
        }

        private bool AddGlass(EntityAgent byEntity, ItemSlot slot, int amount, AssetLocation code, int multiplier, out int consumed)
        {
            int maxAmount = MAX_GLASS_AMOUNT;
            var glassmelt = slot.Itemstack.Attributes.GetOrAddTreeAttribute("glassmelt");
            foreach(var pair in glassmelt)
            {
                maxAmount -= (pair.Value as IntAttribute).value;
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
            consumed = Math.Min(maxAmount, Math.Min(amount, multiplier * 5));
            if(codesAttrib.value.Length > 0 && codesAttrib.value[codesAttrib.value.Length - 1] == glassCode)
            {
                amountsAttrib.value[amountsAttrib.value.Length - 1] += consumed;
            }
            else
            {
                Array.Resize(ref codesAttrib.value, codesAttrib.value.Length + 1);
                Array.Resize(ref amountsAttrib.value, amountsAttrib.value.Length + 1);
                codesAttrib.value[codesAttrib.value.Length - 1] = glassCode;
                amountsAttrib.value[amountsAttrib.value.Length - 1] = consumed;
            }

            glassmelt.SetInt(glassCode, glassmelt.GetInt(glassCode) + consumed);

            return true;
        }

        private void SetMeshDirty(ItemStack item)
        {
            MeshContainer.Get(api, item).isDirty = true;
        }

        private void UpdateGlassmeltMesh(ItemStack item, ITreeAttribute glassmelt)
        {
            int count = 0;
            foreach(var pair in glassmelt)
            {
                count += (pair.Value as IntAttribute).value;
            }
            var container = MeshContainer.Get(api, item);
            if(container.data == null || (int)container.data != count)
            {
                const double invPI = 1.0 / Math.PI;
                container.data = count;
                var root = Math.Pow(count * invPI, 1.0 / 3.0);
                int length = GameMath.Max(1, (int)Math.Floor(root));
                float radius = (float)Math.Sqrt(count * invPI - length) * 0.5f * 1.5f;
                length *= 2;
                var shape = new SmoothRadialShape();
                shape.segments = length + 3;
                shape.outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() {
                    vertices = new float[][] {
                       new float[] { -3, 0 },
                       new float[] { length * 0.1f, radius  },
                       new float[] { length, radius },
                       new float[] { length, 0 }
                    }
                } };
                container.BeginMeshChange();
                SmoothRadialShape.BuildMesh(container.mesh, shape, GlasspipeRenderUtil.GenerateRadialVertices, GlasspipeRenderUtil.GenerateRadialFaces);
                container.EndMeshChange();
            }
        }

        private class MeshContainer
        {
            private static int counter = 0;

            public MeshData mesh;
            public MeshRef meshRef = null;
            public object data;

            public bool isDirty = false;

            public bool hasMesh => meshRef != null || updateMesh.HasValue;

            private bool? updateMesh = null;
            private int prevVertices, prevIndices;

            private MeshContainer() { }

            public void BeginMeshChange()
            {
                mesh.Clear();
            }

            public void EndMeshChange()
            {
                isDirty = false;
                if(meshRef == null || mesh.VerticesCount != prevVertices || mesh.IndicesCount != prevIndices)
                {
                    prevVertices = mesh.VerticesCount;
                    prevIndices = mesh.IndicesCount;
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
                    container.mesh = new MeshData(16, 16, true, true, true, true).WithColorMaps();
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

            public void UpdateMeshRef(ICoreClientAPI capi, CompositeShape shape, ITexPositionSource tex)
            {
                if(updateMesh.HasValue)
                {
                    mesh.SetTexPos(tex["glass"]);
                    GlasspipeMeshCache.TryGet(capi, out var cache);
                    var baseMesh = cache.GetMesh(capi, shape, tex);
                    var toUpload = new MeshData(baseMesh.VerticesCount + mesh.VerticesCount, baseMesh.IndicesCount + mesh.IndicesCount, false, true, true, true);
                    toUpload.AddMeshData(baseMesh);
                    toUpload.AddMeshData(mesh);
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
                    foreach(var pair in cache.containers)
                    {
                        pair.Value.Dispose();
                    }
                }
            }
        }
    }
}