using GlassMaking.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
    public class ItemGlassworkPipe : Item
    {
        private const int RADIAL_SECTIONS_VERTICES = 8;
        private const int RADIAL_SECTION_INDICES = (RADIAL_SECTIONS_VERTICES + 1) * 2;
        private const int CAP_SECTION_INDICES = RADIAL_SECTIONS_VERTICES + 2;

        private static readonly int[] radialSectionTriangles = {
            0, 9,  1, 1, 9,  10,
            1, 10, 2, 2, 10, 11,
            2, 11, 3, 3, 11, 12,
            3, 12, 4, 4, 12, 13,
            4, 13, 5, 5, 13, 14,
            5, 14, 6, 6, 14, 15,
            6, 15, 7, 7, 15, 16,
            7, 16, 8, 8, 16, 17,
        };

        private static readonly int[] radialSectionTrianglesInverted = {
            0, 1, 9,  9,  1, 10,
            1, 2, 10, 10, 2, 11,
            2, 3, 11, 11, 3, 12,
            3, 4, 12, 12, 4, 13,
            4, 5, 13, 13, 5, 14,
            5, 6, 14, 14, 6, 15,
            6, 7, 15, 15, 7, 16,
            7, 8, 16, 16, 8, 17,
        };

        private static readonly int[] capTrianglesUp = {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 7,
            0, 7, 8,
            0, 8, 9
        };

        private static readonly int[] capTrianglesDown = {
            9, 0, 1,
            9, 1, 2,
            9, 2, 3,
            9, 3, 4,
            9, 4, 5,
            9, 5, 6,
            9, 6, 7,
            9, 7, 8
        };

        private static readonly int[] capTrianglesUpInverted = {
            1, 0, 2,
            2, 0, 3,
            3, 0, 4,
            4, 0, 5,
            5, 0, 6,
            6, 0, 7,
            7, 0, 8,
            8, 0, 9
        };

        private static readonly int[] capTrianglesDownInverted = {
            0, 9, 1,
            1, 9, 2,
            2, 9, 3,
            3, 9, 4,
            4, 9, 5,
            5, 9, 6,
            6, 9, 7,
            7, 9, 8
        };

        private const int MAX_RADIUS = 16;

        private static int nextMeshRefId = 0;

        private Item shardsItem;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            shardsItem = api.World.GetItem(new AssetLocation("glassmaking", "glassshards"));
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            var meshes = ObjectCacheUtil.TryGet<Dictionary<int, CachedWorkItem>>(api, "glassmaking:pipemesh");
            if(meshes != null)
            {
                ObjectCacheUtil.Delete(api, "glassmaking:pipemesh");
                foreach(var mesh in meshes)
                {
                    mesh.Value.meshref.Dispose();
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            var bytes = inSlot.Itemstack.Attributes.GetBytes("radii");
            if(bytes != null)
            {
                bool appendSeparator = false;
                for(int i = 0; i < bytes.Length; i++)
                {
                    if(appendSeparator) dsc.Append('|');
                    appendSeparator = true;
                    int v = bytes[i] & 15;
                    dsc.Append(v > 9 ? v.ToString() : ("0" + v));
                }
                dsc.AppendLine();
                appendSeparator = false;
                for(int i = 0; i < bytes.Length; i++)
                {
                    if(appendSeparator) dsc.Append('|');
                    appendSeparator = true;
                    int v = (bytes[i] >> 4) & 15;
                    dsc.Append(v > 9 ? v.ToString() : ("0" + v));
                }
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            var itemstack = slot.Itemstack;
            if(firstEvent)
            {
                if((blockSel == null || byEntity.World.BlockAccessor.GetBlock(blockSel.Position).Id == 0))
                {
                    //var bytes = itemstack.Attributes.GetBytes("radii");
                    //int minCost = int.MaxValue;
                    //int minIndex = -1;
                    //int baseIndex = bytes.Length - 1;
                    //for(int i = 0; i < bytes.Length - 1; i++)
                    //{
                    //    if(TryGetExpansionCost(bytes, i, out int cost))
                    //    {
                    //        if(cost + i < minCost)
                    //        {
                    //            minCost = cost + i;
                    //            minIndex = i;
                    //        }
                    //    }
                    //    if((bytes[i] & 15) == 0)
                    //    {
                    //        baseIndex = i;
                    //        break;
                    //    }
                    //}

                    //int length = 0;
                    //bool displaceBase = true;
                    //int lastHeight = (bytes[baseIndex] >> 4) & 15;
                    //for(int i = baseIndex; i < bytes.Length; i++)
                    //{
                    //    int height = (bytes[i] >> 4) & 15;
                    //    if((bytes[i] & 15) != 0 || height > lastHeight)
                    //    {
                    //        displaceBase = false;
                    //        break;
                    //    }
                    //    length++;
                    //    lastHeight = height;
                    //}
                    //if(displaceBase && (length + baseIndex) < minCost && lastHeight > 0)
                    //{
                    //    int height = lastHeight - 1;
                    //    int index = baseIndex;
                    //    for(int i = baseIndex - 1; i >= 0; i--)
                    //    {
                    //        if((bytes[i] & 15) > height)
                    //        {
                    //            break;
                    //        }
                    //        index--;
                    //    }
                    //    for(int i = index; i < bytes.Length; i++)
                    //    {
                    //        bytes[i] = (byte)((bytes[i] & 240) | (height + 1));
                    //    }
                    //    Array.Resize(ref bytes, bytes.Length + 1);
                    //    bytes[bytes.Length - 1] = (byte)(height << 4);
                    //}
                    //else if(minIndex >= 0)
                    //{
                    //    bytes[minIndex] = (byte)(((bytes[minIndex] & 15) + 2) | ((((bytes[minIndex] >> 4) & 15) + 1) << 4));
                    //}
                    //itemstack.Attributes.SetBytes("radii", bytes);
                    //slot.MarkDirty();
                    //handling = EnumHandHandling.PreventDefault;
                    //return;
                }
                else
                {
                    if(itemstack.Attributes.HasAttribute("radii") && itemstack.Attributes.HasAttribute("glasscode"))
                    {
                        var form = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassBlowingMold;
                        if(form != null)
                        {
                            int count = 0;
                            var bytes = itemstack.Attributes.GetBytes("radii");
                            for(int i = 0; i < bytes.Length; i++)
                            {
                                int inner = bytes[i] & 15;
                                int outer = ((bytes[i] >> 4) & 15) + 1;
                                count += (outer * outer) - (inner * inner);
                            }
                            if(form.CanReceiveGlass(count, new AssetLocation(itemstack.Attributes.GetString("glasscode"))))
                            {
                                byEntity.World.RegisterCallback(delegate (IWorldAccessor world, BlockPos pos, float dt) {
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
                    var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
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
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(blockSel == null) return false;
            var itemstack = slot.Itemstack;
            if(itemstack.Attributes.HasAttribute("radii") && itemstack.Attributes.HasAttribute("glasscode"))
            {
                var form = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassBlowingMold;
                if(form != null)
                {
                    int count = 0;
                    var bytes = itemstack.Attributes.GetBytes("radii");
                    for(int i = 0; i < bytes.Length; i++)
                    {
                        int inner = bytes[i] & 15;
                        int outer = ((bytes[i] >> 4) & 15) + 1;
                        count += (outer * outer) - (inner * inner);
                    }
                    if(form.CanReceiveGlass(count, new AssetLocation(itemstack.Attributes.GetString("glasscode"))))
                    {
                        float speed = 1.5f;
                        if(api.Side == EnumAppSide.Client)
                        {
                            ModelTransform modelTransform = new ModelTransform();
                            modelTransform.EnsureDefaultValues();
                            modelTransform.Origin.Set(0.5f, 0.2f, 0.5f);
                            modelTransform.Translation.Set(-Math.Min(0.3f, speed * secondsUsed), -Math.Min(0.75f, speed * secondsUsed), Math.Min(0.75f, speed * secondsUsed));
                            modelTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4f);
                            modelTransform.Rotation.X = Math.Max(-50f, -secondsUsed * 180f * speed);
                            byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
                        }
                        if(api.Side == EnumAppSide.Server && secondsUsed >= 1f + form.GetRequiredAmount() * 0.01f)
                        {
                            var item = form.GetOutputItem();
                            if(!byEntity.TryGiveItemStack(item))
                            {
                                byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                            }
                            int quantity = (count - form.GetRequiredAmount()) / 5;
                            while(quantity > 0)
                            {
                                item = new ItemStack(shardsItem, Math.Min(quantity, shardsItem.MaxStackSize));
                                if(!byEntity.TryGiveItemStack(item))
                                {
                                    byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
                                }
                                quantity -= shardsItem.MaxStackSize;
                            }
                            itemstack.Attributes.RemoveAttribute("radii");
                            itemstack.Attributes.RemoveAttribute("glasscode");
                            slot.MarkDirty();
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
            var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
            if(source != null)
            {
                int amount = source.GetGlassAmount();
                if(CanAddGlass(byEntity, slot, amount, source.GetGlassCode(), (!byEntity.Controls.Sneak) ? 1 : 5))
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

        protected virtual bool CanAddGlass(EntityAgent byEntity, ItemSlot slot, int available, AssetLocation code, int factor)
        {
            string contentCode = slot.Itemstack.Attributes.GetString("glasscode");
            if(string.IsNullOrEmpty(contentCode) || contentCode == code.ToShortString())
            {
                return true;
            }
            return false;
        }

        protected virtual bool AddGlass(EntityAgent byEntity, ItemSlot slot, int available, AssetLocation code, int factor, out int consumed)
        {
            consumed = factor;
            var itemstack = slot.Itemstack;
            var bytes = itemstack.Attributes.GetBytes("radii");
            if(bytes == null)
            {
                bytes = new byte[] { 1 << 4, 1 << 4, 1 << 4, 1 << 4 };
                //TODO: consumed count
            }
            else
            {
                Array.Resize(ref bytes, bytes.Length + 1);
                int prevOuter = (bytes[bytes.Length - 2] >> 4) & 15;
                for(int i = bytes.Length - 2; i >= 0; i--)
                {
                    int outer = (bytes[i] >> 4) & 15;
                    if(outer + 1 < MAX_RADIUS)
                    {
                        bytes[i] = (byte)((bytes[i] & 15) | ((outer + 1) << 4));
                    }
                }
                if(prevOuter + 1 < MAX_RADIUS) prevOuter++;
                bytes[bytes.Length - 1] = (byte)(prevOuter << 4);
                //TODO: consumed count
            }
            itemstack.Attributes.SetBytes("radii", bytes);
            itemstack.Attributes.SetString("glasscode", code.ToShortString());
            return true;
        }

        private bool TryGetExpansionCost(byte[] radii, int index, out int cost)
        {
            cost = default;
            int outerRadius = (radii[index] >> 4) & 15;
            if(outerRadius + 1 < MAX_RADIUS)
            {
                int innerRadius = radii[index] & 15;
                if(outerRadius - innerRadius > 0)
                {
                    if(index > 0 && ((radii[index - 1] >> 4) & 15) < (innerRadius + 1)) return false;
                    if(((radii[index + 1] >> 4) & 15) < (innerRadius + 1)) return false;
                    cost = outerRadius + 1;
                    return true;
                }
            }
            return false;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
            if(itemstack.Attributes.HasAttribute("radii"))
            {
                CachedWorkItem mesh = CreateOrUpdateMesh(capi, itemstack);

                renderinfo.ModelRef = mesh.meshref;
                renderinfo.CullFaces = true;
            }
        }

        private CachedWorkItem CreateOrUpdateMesh(ICoreClientAPI capi, ItemStack itemstack)
        {
            int value = itemstack.Attributes.GetInt("meshRefId");
            if(!itemstack.Attributes.HasAttribute("meshRefId"))
            {
                value = ++nextMeshRefId;
                itemstack.Attributes.SetInt("meshRefId", value);
            }
            var meshes = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:pipemesh", () => new Dictionary<int, CachedWorkItem>());
            var bytes = itemstack.Attributes.GetBytes("radii");
            if(!meshes.TryGetValue(value, out var mesh) || mesh.isDirty && !Enumerable.SequenceEqual(bytes, mesh.radii))
            {
                if(mesh == null)
                {
                    mesh = new CachedWorkItem();
                    meshes.Add(value, mesh);
                }
                else
                {
                    mesh.meshref.Dispose();
                }
                Shape shapeBase = capi.Assets.TryGet(new AssetLocation(Shape.Base.Domain, "shapes/" + Shape.Base.Path + ".json")).ToObject<Shape>();
                capi.Tesselator.TesselateShape("pipemesh", shapeBase, out var modeldata, capi.Tesselator.GetTextureSource(this), new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), 0, 0, 0);
                modeldata.AddMeshData(GenMesh(capi, bytes));
                mesh.meshref = capi.Render.UploadMesh(modeldata);
                mesh.radii = bytes;
            }

            return mesh;
        }

        private MeshData GenMesh(ICoreClientAPI capi, byte[] radii)
        {
            var mesh = new MeshData(24, 36).WithColorMaps().WithRenderpasses().WithNormals();
            var texture = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("glass-plain")))["material"];

            if(radii.Length > 0)
            {
                bool addCap = true;
                int innerRadius, outerRadius;
                AddVertice(mesh, 0, 0, -2.5f, 0, 0);
                for(int i = 0; i < radii.Length; i++)
                {
                    innerRadius = radii[i] & 15;
                    if(innerRadius == 0)
                    {
                        AddVertice(mesh, 0, 0, i + 0.5f, i / 32f, 0);
                        if(!addCap) GenearateCapFaces(mesh, true, true);
                        addCap = true;
                    }
                    else
                    {
                        GenerateRadialVertices(mesh, i, innerRadius, true);
                        if(addCap)
                        {
                            GenearateCapFaces(mesh, false, true);
                            addCap = false;
                        }
                        else GenerateRadialFaces(mesh, true);
                    }
                }
                AddVertice(mesh, 0, 0, -3.5f, 0, 0);
                for(int i = 0; i < radii.Length; i++)
                {
                    outerRadius = ((radii[i] >> 4) & 15) + 1;
                    GenerateRadialVertices(mesh, i, outerRadius, false);
                    if(i == 0) GenearateCapFaces(mesh, false, false);
                    else GenerateRadialFaces(mesh, false);
                }
                AddVertice(mesh, 0, 0, radii.Length + 0.5f, radii.Length / 32f, 0);
                GenearateCapFaces(mesh, true, false);
            }
            mesh.SetTexPos(texture);
            return mesh;
        }

        private void GenerateRadialVertices(MeshData mesh, int offset, int radius, bool invertNormal)
        {
            float u = 1f / 32f;
            float v = radius / (8f * RADIAL_SECTIONS_VERTICES);
            float step = GameMath.PI * 2f / RADIAL_SECTIONS_VERTICES;
            for(int i = 0; i <= RADIAL_SECTIONS_VERTICES; i++)
            {
                AddVertice(mesh, GameMath.FastSin(step * i) * radius, GameMath.FastCos(step * i) * radius, offset, offset * u, i * v);
            }
        }

        private void AddVertice(MeshData mesh, float x, float y, float z, float u, float v)
        {
            float scale = 1f / 16f;
            mesh.AddVertexWithFlags(x * scale, y * scale, z * scale, u, v, int.MaxValue, 255);
            var vec = new Vec3f(x, y, 0).Normalize();
            mesh.AddNormal(vec.X, vec.Y, vec.Z);
        }

        private void GenerateRadialFaces(MeshData mesh, bool invert)
        {
            int index = mesh.VerticesCount - RADIAL_SECTION_INDICES;
            var indices = invert ? radialSectionTrianglesInverted : radialSectionTriangles;
            for(int i = 0; i < indices.Length; i++)
            {
                mesh.AddIndex(indices[i] + index);
            }
        }

        private void GenearateCapFaces(MeshData mesh, bool isDown, bool invert)
        {
            int index = mesh.VerticesCount - CAP_SECTION_INDICES;
            var indices = isDown ? (invert ? capTrianglesDownInverted : capTrianglesDown) : (invert ? capTrianglesUpInverted : capTrianglesUp);
            for(int i = 0; i < indices.Length; i++)
            {
                mesh.AddIndex(indices[i] + index);
            }
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
            if(slot.Itemstack != null && slot.Itemstack.Attributes.HasAttribute("meshRefId"))
            {
                int value = slot.Itemstack.Attributes.GetInt("meshRefId");
                var meshes = ObjectCacheUtil.TryGet<Dictionary<int, CachedWorkItem>>(world.Api, "glassmaking:pipemesh");
                if(meshes != null && meshes.TryGetValue(value, out var mesh))
                {
                    mesh.isDirty = true;
                }
            }
        }

        private void GenerateSegment(int radius, out int[] minCoords, out int[] maxCoords)
        {
            if(radius == 0)
            {
                minCoords = new int[1] { 0 };
                maxCoords = new int[1] { 0 };
                return;
            }
            if(radius == 1)
            {
                minCoords = new int[2] { 1, 0 };
                maxCoords = new int[2] { 1, 0 };
                return;
            }

            minCoords = new int[radius + 1];
            maxCoords = new int[radius + 1];

            int x = 0;
            int y = radius;
            int delta = 1 - 2 * radius;
            int error;
            while(y >= x)
            {
                maxCoords[y] = x;
                minCoords[x] = y;
                maxCoords[x] = Math.Max(maxCoords[x], y);
                minCoords[y] = Math.Min(minCoords[y], x);

                error = 2 * (delta + y) - 1;
                if(delta < 0 && error <= 0)
                {
                    x++;
                    delta += 2 * x + 1;
                    continue;
                }
                error = 2 * (delta + y) - 1;
                if((delta < 0) && (error <= 0))
                {
                    x++;
                    delta += 2 * x + 1;
                    continue;
                }
                if((delta > 0) && (error > 0))
                {
                    y--;
                    delta -= 2 * y + 1;
                    continue;
                }
                x++;
                delta += 2 * (x - y);
                y--;
            }
        }

        private class CachedWorkItem
        {
            public MeshRef meshref;
            public int TextureId;
            public byte[] radii;
            public bool isDirty = false;
        }

        private class SegmentInfo
        {
            public int[] minCoords;
            public int[] maxCoords;
        }
    }
}