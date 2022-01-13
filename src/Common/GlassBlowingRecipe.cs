using GlassMaking.Common;
using GlassMaking.Items;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GlassBlowingRecipe : IRecipeBase, IByteSerializable, IRecipeBase<GlassBlowingRecipe>
    {
        private static SmoothRadialShape EmptyShape = new SmoothRadialShape() { segments = 1, outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() { vertices = new float[][] { new float[] { -1.5f, 0 } } } } };

        public int recipeId;

        [JsonProperty]
        public AssetLocation code;

        [JsonProperty]
        public JsonItemStack output;

        [JsonProperty]
        public JsonGlassBlowingToolStep[] steps;

        public GlassBlowingToolStep[] resolvedSteps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        AssetLocation IRecipeBase.code => code;

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public GlassBlowingToolStep GetStep(ITreeAttribute recipeAttribute)
        {
            int step = recipeAttribute.GetInt("step", 0);
            if(step < 0 || step >= resolvedSteps.Length) return null;
            return resolvedSteps[step];
        }

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if(steps == null || steps.Length == 0 || output == null)
            {
                world.Logger.Error("Glassblowing recipe with output {0} has no steps or missing output. Ignoring recipe.", Output);
                return false;
            }
            if(!output.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }
            var system = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
            resolvedSteps = new GlassBlowingToolStep[steps.Length];
            for(int i = 0; i < steps.Length; i++)
            {
                var tool = system.GetGlassBlowingTool(steps[i].tool);
                if(tool == null)
                {
                    world.Logger.Error("Failed resolving a glassblowing tool with code {0} in {1}", steps[i].tool, sourceForErrorLogging);
                    return false;
                }
                var step = tool.GetStepInstance();
                step.tool = steps[i].tool;
                step.shape = steps[i].shape;
                if(!step.Resolve(steps[i].attributes, world, sourceForErrorLogging))
                {
                    return false;
                }
                resolvedSteps[i] = step;
            }
            return true;
        }

        public void GetRecipeInfo(ItemStack item, ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.Append("Recipe: ").AppendLine(recipeAttribute.GetString("code"));
            int step = recipeAttribute.GetInt("step", 0);
            dsc.Append("Step: ").Append(step + 1).AppendLine();
            resolvedSteps[step].GetStepInfo(item, recipeAttribute["data"], dsc, world, withDebugInfo);
        }

        public WorldInteraction[] GetHeldInteractionHelp(ItemStack item, ITreeAttribute recipeAttribute)
        {
            return GetStep(recipeAttribute).GetHeldInteractionHelp(item, recipeAttribute["data"]);
        }

        public void OnHeldInteractStart(ItemSlot slot, ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isRecipeComplete)
        {
            int step = recipeAttribute.GetInt("step", 0);
            slot.Itemstack.TempAttributes.SetInt("glassblowingRecipeStep", step);

            var data = recipeAttribute["data"];
            var prevData = data;
            resolvedSteps[step].OnHeldInteractStart(slot, ref data, byEntity, blockSel, entitySel, firstEvent, ref handling, out bool isComplete);
            ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete);
            isRecipeComplete = recipeAttribute == null;
            if(isComplete) slot.MarkDirty();
        }

        public bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isRecipeComplete)
        {
            int step = recipeAttribute.GetInt("step", 0);
            if(slot.Itemstack.TempAttributes.GetInt("glassblowingRecipeStep", -1) != step)
            {
                isRecipeComplete = false;
                return false;
            }

            var data = recipeAttribute["data"];
            var prevData = data;
            bool result = resolvedSteps[step].OnHeldInteractStep(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, out bool isComplete);
            ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete);
            isRecipeComplete = recipeAttribute == null;
            if(isComplete)
            {
                slot.MarkDirty();
                return false;
            }
            return result;
        }

        public void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            slot.Itemstack.TempAttributes.RemoveAttribute("glassblowingRecipeStep");
            int step = recipeAttribute.GetInt("step", 0);
            var data = recipeAttribute["data"];
            var prevData = data;
            resolvedSteps[step].OnHeldInteractStop(secondsUsed, slot, ref data, byEntity, blockSel, entitySel);
            ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, false);
        }

        public bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            slot.Itemstack.TempAttributes.RemoveAttribute("glassblowingRecipeStep");
            int step = recipeAttribute.GetInt("step", 0);
            var data = recipeAttribute["data"];
            var prevData = data;
            bool result = resolvedSteps[step].OnHeldInteractCancel(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, cancelReason);
            ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, false);
            return result;
        }

        public void UpdateMesh(ItemStack item, ItemGlassworkPipe.IMeshContainer container, int glow, ITreeAttribute recipeAttribute)
        {
            string code = recipeAttribute.GetString("code");
            int step = recipeAttribute.GetInt("step", 0);
            float t = resolvedSteps[step].GetMeshTransitionValue(item, recipeAttribute["data"]);
            t = GameMath.Clamp(t, 0, 1);

            if(container.data == null || !(container.data is MeshInfo data))
            {
                container.data = new MeshInfo(code, step, t);
            }
            else if(!data.Equals(code, step, t))
            {
                data.Set(code, step, t);
            }
            else
            {
                return;
            }

            SmoothRadialShape prevShape = null;
            for(int i = step - 1; i >= 0; i--)
            {
                if(resolvedSteps[i].shape != null)
                {
                    prevShape = resolvedSteps[i].shape;
                    break;
                }
            }
            if(resolvedSteps[step].shape == null)
            {
                container.BeginMeshChange();
                if(prevShape != null)
                {
                    SmoothRadialShape.BuildMesh(container.mesh, prevShape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
                }
                container.EndMeshChange();
                return;
            }

            if(prevShape == null) prevShape = EmptyShape;
            container.BeginMeshChange();
            SmoothRadialShape.BuildLerpedMesh(container.mesh, prevShape, resolvedSteps[step].shape, t,
                (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
            container.EndMeshChange();
        }

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(code);
            writer.Write(resolvedSteps.Length);
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                writer.Write(resolvedSteps[i].tool);
                resolvedSteps[i].ToBytes(writer);
            }
            output.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            code = reader.ReadAssetLocation();
            resolvedSteps = new GlassBlowingToolStep[reader.ReadInt32()];
            var system = resolver.Api.ModLoader.GetModSystem<GlassMakingMod>();
            for(int i = 0; i < resolvedSteps.Length; i++)
            {
                var code = reader.ReadString();
                var tool = system.GetGlassBlowingTool(code);
                resolvedSteps[i] = tool.GetStepInstance();
                resolvedSteps[i].tool = code;
                resolvedSteps[i].FromBytes(reader, resolver);
            }
            output = new JsonItemStack();
            output.FromBytes(reader, resolver.ClassRegistry);
            output.Resolve(resolver, "[FromBytes]");
        }

        public GlassBlowingRecipe Clone()
        {
            return new GlassBlowingRecipe() {
                recipeId = recipeId,
                code = code.Clone(),
                output = output.Clone(),
                resolvedSteps = Array.ConvertAll(resolvedSteps, CloneStep),
                Name = Name.Clone(),
                Enabled = Enabled
            };
        }

        private void ApplyStepProperties(ref ITreeAttribute recipeAttribute, EntityAgent byEntity, int step, IAttribute prevData, IAttribute data, bool isComplete)
        {
            if(isComplete && byEntity.Api.Side == EnumAppSide.Server)
            {
                step++;
                if(step >= steps.Length)
                {
                    recipeAttribute = null;
                    var item = output.ResolvedItemstack.Clone();
                    if(!byEntity.TryGiveItemStack(item))
                    {
                        byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                    }
                }
                else
                {
                    if(prevData != null) recipeAttribute.RemoveAttribute("data");
                    recipeAttribute.SetInt("step", step);
                }
            }
            else
            {
                if(prevData != data)
                {
                    if(data == null) recipeAttribute.RemoveAttribute("data");
                    else recipeAttribute["data"] = data;
                }
            }
        }

        private static GlassBlowingToolStep CloneStep(GlassBlowingToolStep other)
        {
            return other.Clone();
        }

        private class MeshInfo
        {
            public string code;
            public int step;
            public float progress;

            public MeshInfo(string code, int step, float progress)
            {
                this.code = code;
                this.step = step;
                this.progress = progress;
            }

            public void Set(string code, int step, float progress)
            {
                this.code = code;
                this.step = step;
                this.progress = progress;
            }

            public bool Equals(string code, int step, float progress)
            {
                return this.code == code && this.step == step && this.progress == progress;
            }
        }
    }

    [JsonObject]
    public sealed class JsonGlassBlowingToolStep
    {
        [JsonProperty]
        public string tool;

        [JsonProperty]
        public SmoothRadialShape shape;

        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject attributes;
    }

    public abstract class GlassBlowingToolStep
    {
        public string tool;

        public SmoothRadialShape shape;

        public abstract bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging);

        public virtual void ToBytes(BinaryWriter writer)
        {
            writer.Write(shape != null);
            if(shape != null) shape.ToBytes(writer);
        }

        public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            if(reader.ReadBoolean())
            {
                shape = new SmoothRadialShape();
                shape.FromBytes(reader);
            }
        }

        public abstract GlassBlowingToolStep Clone();

        public virtual void GetStepInfo(ItemStack item, IAttribute data, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { }

        public virtual WorldInteraction[] GetHeldInteractionHelp(ItemStack item, IAttribute data) { return new WorldInteraction[0]; }

        public virtual float GetMeshTransitionValue(ItemStack item, IAttribute data)
        {
            return 0f;
        }

        public virtual void OnHeldInteractStart(ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
        {
            isComplete = true;
        }

        public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isComplete)
        {
            isComplete = true;
            return true;
        }

        public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {

        }

        public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref IAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }
    }
}