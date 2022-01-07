using GlassMaking.Common;
using Newtonsoft.Json;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassSmeltery : BlockEntity, IBlockEntityContainer, ITimeBasedHeatReceiver
    {
        private const float TEMPERATURE_MODIFIER = 1.1f;
        private const float BUBBLING_TEMPERATURE = 1450 / TEMPERATURE_MODIFIER;
        private const float MELTING_TEMPERATURE = 1300 / TEMPERATURE_MODIFIER;
        private const float WORKING_TEMPERATURE = 1100 / TEMPERATURE_MODIFIER;

        private const double PROCESS_HOURS_PER_UNIT = 0.001;
        private const double BUBBLING_PROCESS_MULTIPLIER = 3;

        private static SimpleParticleProperties smokeParticles;

        protected virtual int maxGlassAmount => 1000;

        IInventory IBlockEntityContainer.Inventory => inventory;
        string IBlockEntityContainer.InventoryClassName => inventory.ClassName;

        private BlockRendererGlassSmeltery renderer = null;

        private ITimeBasedHeatSource heatSource = null;

        private MeshData coverMesh;

        private SmelteryState state;
        private int glassAmount;
        private AssetLocation glassCode;
        private double processProgress;

        private GlassSmelteryInventory inventory = new GlassSmelteryInventory(null, null);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("glassmaking:glasssmeltery-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            if(api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = (ICoreClientAPI)api;
                var asset = capi.Assets.TryGet(new AssetLocation(Block.Code.Domain, "shapes/block/glass-smeltery/cover.json"));
                capi.Tesselator.TesselateShape(Block, asset.ToObject<Shape>(), out coverMesh, new Vec3f(0f, GetRotation(), 0f));
                var bathSource = capi.Tesselator.GetTexSource(Block);
                var bathMesh = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:glass-smeltery-" + Block.Variant["side"], () => {
                    asset = capi.Assets.TryGet(new AssetLocation(Block.Code.Domain, "shapes/block/glass-smeltery/bath.json"));
                    capi.Tesselator.TesselateShape("glassmaking:glass-smeltery-shape", asset.ToObject<Shape>(), out var bath, bathSource, new Vec3f(0f, GetRotation(), 0f));
                    return capi.Render.UploadMesh(bath);
                });
                renderer = new BlockRendererGlassSmeltery(Pos, capi.Tesselator.GetTexSource(Block), capi, bathMesh, bathSource["inside"].atlasTextureId);
                capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "glassmaking:firebox");
                UpdateRendererFull();
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if(heatSource != null)
            {
                switch(state)
                {
                    case SmelteryState.ContainsMix:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        if(heatSource.IsHeatedUp()) dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                    case SmelteryState.Melting:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Melting progress: " + (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT) * 100).ToString("0") + "%");
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                    case SmelteryState.Bubbling:
                        dsc.AppendLine("Contains: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Bubbling progress: " + (processProgress / (glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER) * 100).ToString("0") + "%");
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                    case SmelteryState.ContainsGlass:
                        dsc.AppendLine("Contains melt: " + glassAmount + "x " + Lang.Get(GlassBlend.GetBlendNameCode(glassCode)));
                        dsc.AppendLine("Temperature: " + heatSource.CalcCurrentTemperature().ToString("0"));
                        break;
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("state", (int)state);
            if(state != SmelteryState.Empty)
            {
                tree.SetInt("glassamount", glassAmount);
                tree.SetString("glasscode", glassCode.ToShortString());
                if(state == SmelteryState.ContainsMix)
                {
                    inventory.ToTreeAttributes(tree.GetOrAddTreeAttribute("inventory"));
                }
                else if(state != SmelteryState.ContainsGlass)
                {
                    tree.SetDouble("progress", processProgress);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            state = (SmelteryState)tree.GetInt("state");
            if(state != SmelteryState.Empty)
            {
                glassAmount = tree.GetInt("glassamount");
                glassCode = new AssetLocation(tree.GetString("glasscode"));
                if(state == SmelteryState.ContainsMix)
                {
                    inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
                }
                else if(state != SmelteryState.ContainsGlass)
                {
                    processProgress = tree.GetDouble("progress");
                }
            }
            UpdateRendererFull();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(coverMesh);
            base.OnTesselation(mesher, tessThreadTesselator);
            return true;
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            renderer?.Dispose();
            base.OnBlockRemoved();
        }

        public void GetGlassFillState(out int canAddAmount, out AssetLocation code)
        {
            code = null;
            canAddAmount = maxGlassAmount;
            if(glassCode != null)
            {
                code = glassCode;
                canAddAmount = maxGlassAmount - glassAmount;
            }
        }

        public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int multiplier)
        {
            if(heatSource == null) return false;
            if(state == SmelteryState.Bubbling)
            {
                var reducer = slot.Itemstack.ItemAttributes?["glassmaking:glassBubblingReducer"].AsObject<GlassBubblingReducer>();
                if(reducer != null)
                {
                    if(Api.Side == EnumAppSide.Server)
                    {
                        var collectible = slot.Itemstack.Collectible;
                        slot.TakeOut(1);
                        processProgress = Math.Min(processProgress + reducer.amount, glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER);
                        slot.MarkDirty();
                        MarkDirty(true);
                        if(reducer.replacement != null)
                        {
                            if(reducer.replacement.Resolve(Api.World, "glassBubblingReducer.replacement from " + collectible.Code))
                            {
                                var item = reducer.replacement.ResolvedItemstack;
                                if(!byPlayer.Entity.TryGiveItemStack(item))
                                {
                                    byPlayer.Entity.World.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                                }
                            }
                        }
                    }
                    byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/sizzle"), byPlayer, byPlayer);
                    SpawnGlassUseParticles(byPlayer.Entity.World, null, byPlayer, 10);
                    return true;
                }
            }

            if(glassAmount >= maxGlassAmount) return false;
            GlassBlend blend = GlassBlend.FromJson(slot.Itemstack);
            if(blend == null) blend = GlassBlend.FromTreeAttributes(slot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
            if(blend != null && blend.amount > 0 && (glassCode == null || glassCode.Equals(blend.code)) && (blend.amount + glassAmount) <= maxGlassAmount)
            {
                if(Api.Side == EnumAppSide.Server)
                {
                    if(glassCode == null) glassCode = blend.code.Clone();
                    if(state == SmelteryState.Bubbling || state == SmelteryState.ContainsGlass)
                    {
                        if(heatSource.CalcCurrentTemperature() >= MELTING_TEMPERATURE)
                        {
                            state = SmelteryState.Melting;
                            processProgress = PROCESS_HOURS_PER_UNIT * glassAmount;
                        }
                        else
                        {
                            state = SmelteryState.ContainsMix;
                        }
                    }
                    int consume = Math.Min(Math.Min(multiplier, slot.Itemstack.StackSize), (maxGlassAmount - glassAmount) / blend.amount);
                    var item = slot.TakeOut(consume);
                    if(state == SmelteryState.Empty || state == SmelteryState.ContainsMix)
                    {
                        inventory.AddItem(item);
                        state = SmelteryState.ContainsMix;
                    }
                    glassAmount += blend.amount * consume;
                    slot.MarkDirty();
                    MarkDirty(true);
                }
                return true;
            }
            return false;
        }

        public ItemStack[] GetDropItems()
        {
            var items = inventory.GetItems();
            if(glassAmount >= 5)
            {
                var shards = new ItemStack(Api.World.GetItem(new AssetLocation("glassmaking", "glassshards")), glassAmount / 5);
                new GlassBlend(glassCode, 5).ToTreeAttributes(shards.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
                items.Add(shards);
            }
            return items.ToArray();
        }

        public bool CanInteract(EntityAgent byEntity, BlockSelection blockSel)
        {
            return blockSel.Face.Opposite.Code.ToLower() == Block.Variant["side"].ToLower();
        }

        public int GetGlassAmount()
        {
            if(heatSource != null)
            {
                if(state == SmelteryState.ContainsGlass && heatSource.CalcCurrentTemperature() >= WORKING_TEMPERATURE)
                {
                    return glassAmount;
                }
            }
            return 0;
        }

        public AssetLocation GetGlassCode()
        {
            return glassCode;
        }

        public void RemoveGlass(int amount)
        {
            glassAmount -= amount;
            if(glassAmount <= 0 && Api.Side == EnumAppSide.Server)
            {
                state = SmelteryState.Empty;
                glassCode = null;
            }
            MarkDirty(true);
        }

        public void SpawnGlassUseParticles(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer, float quantity = 1f)
        {
            // Smoke on the mold
            Vec3d blockpos = Pos.ToVec3d().Add(0.5, 0, 0.5);
            world.SpawnParticles(
                quantity,
                ColorUtil.ToRgba(50, 220, 220, 220),
                blockpos.AddCopy(-0.25, -0.1, -0.25),
                blockpos.Add(0.25, 0.1, 0.25),
                new Vec3f(-0.5f, 0f, -0.5f),
                new Vec3f(0.5f, 0f, 0.5f),
                0.25f,
                -0.05f,
                0.5f,
                EnumParticleModel.Quad,
                byPlayer
            );
        }

        void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSource heatSource)
        {
            this.heatSource = heatSource;
        }

        void ITimeBasedHeatReceiver.OnHeatSourceTick(float dt)
        {
            if(state != SmelteryState.Empty && heatSource.IsHeatedUp())
            {
                double timeOffset = 0;
                if(state == SmelteryState.ContainsMix)
                {
                    if(Api.Side == EnumAppSide.Server && heatSource.CalcTempElapsedTime(timeOffset, MELTING_TEMPERATURE) > 0)
                    {
                        state = SmelteryState.Melting;
                        processProgress = 0;
                        inventory.Clear();
                        UpdateRendererFull();
                        MarkDirty(true);
                    }
                }
                if(state == SmelteryState.Melting)
                {
                    double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT - processProgress;
                    double time = heatSource.CalcTempElapsedTime(timeOffset, MELTING_TEMPERATURE);
                    if(time >= timeLeft)
                    {
                        timeOffset += timeLeft;
                        if(Api.Side == EnumAppSide.Server)
                        {
                            processProgress = 0;
                            state = SmelteryState.Bubbling;
                            MarkDirty(true);
                        }
                    }
                    else
                    {
                        processProgress += time;
                    }
                }
                if(state == SmelteryState.Bubbling)
                {
                    double timeLeft = glassAmount * PROCESS_HOURS_PER_UNIT * BUBBLING_PROCESS_MULTIPLIER - processProgress;
                    double time = heatSource.CalcTempElapsedTime(timeOffset, BUBBLING_TEMPERATURE);
                    if(time >= timeLeft)
                    {
                        if(Api.Side == EnumAppSide.Server)
                        {
                            state = SmelteryState.ContainsGlass;
                            MarkDirty(true);
                        }
                    }
                    else
                    {
                        processProgress += time;
                    }
                }
            }

            if(Api.Side == EnumAppSide.Client)
            {
                UpdateRendererParameters();
                if(heatSource.IsBurning()) EmitParticles();
            }
        }

        private void EmitParticles()
        {
            if(Api.World.Rand.Next(5) > 0)
            {
                GetSmokeParameters(out var x, out var z, out var wx, out var wz);
                smokeParticles.MinPos.Set(Pos.X + 0.5 + x, Pos.Y + 0.75f, Pos.Z + 0.5 + z);
                smokeParticles.AddPos.Set(wx, 0.0, wz);
                Api.World.SpawnParticles(smokeParticles);
            }
        }

        private void UpdateRendererFull()
        {
            if(Api != null && Api.Side == EnumAppSide.Client && renderer != null)
            {
                renderer.SetHeight((float)glassAmount / maxGlassAmount);
                UpdateRendererParameters();
            }
        }

        private void UpdateRendererParameters()
        {
            renderer.SetParameters(state == SmelteryState.ContainsMix, Math.Min(223, (int)((heatSource == null ? 0 : heatSource.GetTemperature() / 1500f) * 223)));
        }

        private int GetRotation()
        {
            int result = 0;
            switch(Block.Variant["side"])
            {
                case "north":
                    result = 0;
                    break;
                case "east":
                    result = 270;
                    break;
                case "south":
                    result = 180;
                    break;
                case "west":
                    result = 90;
                    break;
            }
            return result;
        }

        private void GetSmokeParameters(out double x, out double z, out double wx, out double wz)
        {
            x = 0;
            z = 0;
            wx = 0.25;
            wz = 0.25;
            switch(Block.Variant["side"])
            {
                case "north":
                    x = -0.3125;
                    z = -0.3125;
                    wx = 0.625;
                    wz = 0.1875;
                    break;
                case "east":
                    x = 0.125;
                    z = -0.3125;
                    wx = 0.1875;
                    wz = 0.625;
                    break;
                case "south":
                    x = -0.3125;
                    z = 0.125;
                    wx = 0.625;
                    wz = 0.1875;
                    break;
                case "west":
                    x = -0.3125;
                    z = -0.3125;
                    wx = 0.1875;
                    wz = 0.625;
                    break;
            }
        }

        static BlockEntityGlassSmeltery()
        {
            smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
            smokeParticles.SelfPropelled = true;
            smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
        }

        private enum SmelteryState
        {
            Empty,
            ContainsMix,
            Melting,
            Bubbling,
            ContainsGlass
        }
    }

    [JsonObject]
    internal class GlassBubblingReducer
    {
        [JsonProperty]
        public JsonItemStack replacement = null;
        [JsonProperty(Required = Required.Always)]
        public double amount;
    }
}