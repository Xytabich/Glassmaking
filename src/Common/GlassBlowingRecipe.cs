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
    public class GlassBlowingRecipe : IRecipeBase, IByteSerializable, IRecipeBase<GlassBlowingRecipe>
    {
        public int recipeId;

        public AssetLocation code;

        public JsonItemStack output;

        public JsonGlassBlowingToolStep[] steps;

        public GlassBlowingToolStep[] resolvedSteps;

        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; } = true;

        public IRecipeIngredient[] Ingredients { get; } = new IRecipeIngredient[0];

        public IRecipeOutput Output => output;

        AssetLocation IRecipeBase.code => code;

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

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            return new Dictionary<string, string[]>();
        }

        public void GetRecipeInfo(ItemStack item, ITreeAttribute recipeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine("Recipe: " + recipeAttribute.GetString("code"));
            int step = recipeAttribute.GetInt("step");
            dsc.AppendLine("Step: " + (step + 1));
            resolvedSteps[step].GetStepInfo(item, recipeAttribute.GetTreeAttribute("data"), dsc, world, withDebugInfo);
        }

        public WorldInteraction[] GetHeldInteractionHelp(ItemStack item, ITreeAttribute recipeAttribute)
        {
            int step = recipeAttribute.GetInt("step");
            return resolvedSteps[step].GetHeldInteractionHelp(item, recipeAttribute.GetTreeAttribute("data"));
        }

        public void OnHeldInteractStart(ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            resolvedSteps[step].OnHeldInteractStart(slot, ref data, byEntity, blockSel, entitySel, firstEvent, ref handling, out bool isComplete);
            if(ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete))
            {
                slot.MarkDirty();
            }
        }

        public bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            bool result = resolvedSteps[step].OnHeldInteractStep(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, out bool isComplete);
            if(ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete))
            {
                slot.MarkDirty();
                return false;
            }
            return result;
        }

        public void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            resolvedSteps[step].OnHeldInteractStop(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, out bool isComplete);
            if(ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete))
            {
                slot.MarkDirty();
            }
        }

        public bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref ITreeAttribute recipeAttribute, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            int step = recipeAttribute.GetInt("step");
            var data = recipeAttribute.GetTreeAttribute("data");
            var prevData = data;
            bool result = resolvedSteps[step].OnHeldInteractCancel(secondsUsed, slot, ref data, byEntity, blockSel, entitySel, cancelReason, out bool isComplete);
            if(ApplyStepProperties(ref recipeAttribute, byEntity, step, prevData, data, isComplete))
            {
                slot.MarkDirty();
                return true;
            }
            return result;
        }

        public void UpdateMesh(ItemStack item, MeshData mesh, ITreeAttribute recipeAttribute)
        {
            int step = recipeAttribute.GetInt("step");
            float progress = resolvedSteps[step].GetStepProgress(item, recipeAttribute.GetTreeAttribute("data"));
            SmoothRadialShape prevShape = null;
            for(int i = 0; i < step; i++)
            {
                if(resolvedSteps[i].shape != null)
                {
                    prevShape = resolvedSteps[i].shape;
                    break;
                }
            }
            if(resolvedSteps[step].shape == null)
            {
                if(prevShape == null) return;
                SmoothRadialShape.BuildMesh(mesh, prevShape, GlasspipeRenderUtil.GenerateRadialVertices, GlasspipeRenderUtil.GenerateRadialFaces);
            }
            if(prevShape == null) prevShape = SmoothRadialShape.Empty;
            SmoothRadialShape.BuildLerpedMesh(mesh, prevShape, resolvedSteps[step].shape, GameMath.Clamp(progress, 0, 1), GlasspipeRenderUtil.GenerateRadialVertices, GlasspipeRenderUtil.GenerateRadialFaces);
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

        private bool ApplyStepProperties(ref ITreeAttribute recipeAttribute, EntityAgent byEntity, int step, ITreeAttribute prevData, ITreeAttribute data, bool isComplete)
        {
            if(byEntity.Api.Side == EnumAppSide.Client) return false;
            if(isComplete)
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
                return true;
            }
            else
            {
                if(prevData != data)
                {
                    if(data == null) recipeAttribute.RemoveAttribute("data");
                    else recipeAttribute["data"] = data;
                }
                return false;
            }
        }

        private static GlassBlowingToolStep CloneStep(GlassBlowingToolStep other)
        {
            return other.Clone();
        }
    }

    public sealed class JsonGlassBlowingToolStep
    {
        public string tool;

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

        public virtual void GetStepInfo(ItemStack item, ITreeAttribute treeAttribute, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { }

        public abstract WorldInteraction[] GetHeldInteractionHelp(ItemStack item, ITreeAttribute treeAttribute);

        public virtual float GetStepProgress(ItemStack item, ITreeAttribute treeAttribute)
        {
            return 0f;
        }

        public virtual void OnHeldInteractStart(ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling, out bool isComplete)
        {
            isComplete = true;
        }

        public virtual bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isComplete)
        {
            isComplete = true;
            return true;
        }

        public virtual void OnHeldInteractStop(float secondsUsed, ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, out bool isComplete)
        {
            isComplete = false;
        }

        public virtual bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, ref ITreeAttribute data, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, out bool isComplete)
        {
            isComplete = false;
            return true;
        }
    }
}