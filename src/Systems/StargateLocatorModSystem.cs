using AstriaPorta.Config;
using AstriaPorta.Content;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Systems
{
    public class GateGenerationRequest
    {
        public ItemSlot forSlot;
        public ItemStack forStack;
        public string schematiccode;
        public int startX;
        public int startZ;
        public byte attempts;
        public bool currentlySearching;
    }

    /// <summary>
    /// ModSystem for generating random stargates. Based largely on the base game ItemLocatorMap item
    /// </summary>
    public class StargateLocatorModSystem : ModSystem
    {
        ICoreServerAPI sapi;

        Queue<GateGenerationRequest> requestQueue;
        GateGenerationRequest currentRequest = null;

        private int tickId = -1;

        public const byte MAX_RETRIES = 25;
        // TODO: move these ranges to a config file
        public const int MIN_TELEPORTER_RANGE_BLOCKS = 1000;
        public const int MAX_TELEPORTER_RANGE_BLOCKS = 8000;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            requestQueue = new Queue<GateGenerationRequest>();
        }

        /// <summary>
        /// Attempts to find a naturally generated structure with a stargate
        /// </summary>
        /// <param name="stack">The itemstack the request originated from</param>
        /// <param name="startX">The x-coordinate of the entity initiating the request</param>
        /// <param name="startZ">The z-coordinate of the entity initiating the request</param>
        /// <param name="inventoryId">The id of the inventory the stack belongs to</param>
        /// <param name="schematiccode">The class of schematics to try and generate</param>
        /// <returns>Whether the request could be executed or not</returns>
        public bool AddGenerationRequest(ItemSlot slot, int startX, int startZ, string schematiccode)
        {
            if (!StargateConfig.Loaded.EnableCartoucheGates)
            {
                return false;
            }

            // take generation details, create a new generation request
            // add it to the queue, and start the callback if queue is empty
            if (slot.Itemstack.TempAttributes.HasAttribute("gatelocatorworking") || slot.Itemstack.TempAttributes.HasAttribute("gategenerationfinished"))
            {
                return false;
            }
            slot.Itemstack.Attributes.SetString("position", startX.ToString() + startZ.ToString());
            slot.Itemstack.TempAttributes.SetBool("gatelocatorworking", true);

            GateGenerationRequest request = new GateGenerationRequest
            {
                forSlot = slot,
                forStack = slot.Itemstack,
                startX = startX,
                startZ = startZ,
                schematiccode = schematiccode,
                attempts = 0,
                currentlySearching = false
            };
            requestQueue.Enqueue(request);

            if (tickId == -1)
            {
                sapi.World.RegisterGameTickListener(onServerGameTick, 250);
            }

            slot.MarkDirty();

            return true;
        }

        private void onServerGameTick(float dt)
        {
            // TODO: put everything into a queue, and only process one at a time
            if (currentRequest == null)
            {
                if (requestQueue.Count == 0)
                {
                    sapi.World.UnregisterGameTickListener(tickId);
                    tickId = -1;
                    return;
                }

                currentRequest = requestQueue.Dequeue();
            }

            if (currentRequest.currentlySearching) return;
            if (currentRequest.attempts > MAX_RETRIES)
            {
                ItemSlot stackSlot = TryFindItemSlotForRequest(currentRequest);
                if (stackSlot != null)
                {
                    stackSlot.Itemstack.Attributes.RemoveAttribute("position");
                    stackSlot.Itemstack.TempAttributes.RemoveAttribute("gatelocatorworking");
                    stackSlot.Itemstack.Attributes.SetBool("generationfinished", true);
                    stackSlot.Itemstack.Attributes.SetBool("generationfailed", true);
                    stackSlot.MarkDirty();
                }
                currentRequest = null;
                return;
            }

            int addrange = MAX_TELEPORTER_RANGE_BLOCKS - MIN_TELEPORTER_RANGE_BLOCKS;
            int dx = (int)(MIN_TELEPORTER_RANGE_BLOCKS + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);
            int dz = (int)(MIN_TELEPORTER_RANGE_BLOCKS + sapi.World.Rand.NextDouble() * addrange) * (2 * sapi.World.Rand.Next(2) - 1);

            int chunkX = (currentRequest.startX + dx) / GlobalConstants.ChunkSize;
            int chunkZ = (currentRequest.startZ + dz) / GlobalConstants.ChunkSize;

            if (!sapi.World.BlockAccessor.IsValidPos(new BlockPos(currentRequest.startX + dx, 1, currentRequest.startZ, 0)))
            {
                currentRequest.currentlySearching = false;
                return;
            }

            currentRequest.currentlySearching = true;
            ChunkPeekOptions opts = new ChunkPeekOptions()
            {
                OnGenerated = (chunks) => testForExitPoint(chunks, chunkX, chunkZ, currentRequest),
                UntilPass = EnumWorldGenPass.TerrainFeatures,
                ChunkGenParams = chunkGenParams(currentRequest.schematiccode)
            };

            sapi.WorldManager.PeekChunkColumn(chunkX, chunkZ, opts);
        }

        ITreeAttribute chunkGenParams(string schematicCode)
        {
            TreeAttribute tree = new TreeAttribute();
            TreeAttribute subtree;
            tree["structureChanceModifier"] = subtree = new TreeAttribute();
            subtree.SetFloat(schematicCode, 50);

            tree["structureMaxCount"] = subtree = new TreeAttribute();
            subtree.SetInt(schematicCode, 1);

            return tree;
        }

        private void testForExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerX, int centerZ, GateGenerationRequest request)
        {
            BlockPos pos = HasExitPoint(columnsByChunkCoordinate, centerX, centerZ, request.schematiccode);

            if (pos == null)
            {
                request.currentlySearching = false;
            }
            else
            {
                sapi.WorldManager.LoadChunkColumnPriority(centerX, centerZ, new ChunkLoadOptions()
                {
                    ChunkGenParams = chunkGenParams(request.schematiccode),
                    OnLoaded = () => exitChunkLoaded(request, pos)
                });
            }
        }

        private void exitChunkLoaded(GateGenerationRequest request, BlockPos pos)
        {
            BlockEntity be = sapi.World.BlockAccessor.GetBlockEntity(pos);
            BlockEntityStargate gateEntity = be as BlockEntityStargate;
            if (gateEntity == null)
            {
                request.currentlySearching = false;
                return;
            }

            StargateAddress addr = gateEntity.GateAddress;

            request.forStack.Attributes.SetString("gateAddressS", addr.ToString());

            ItemSlot stackSlot = TryFindItemSlotForRequest(request);
            if (stackSlot == null)
            {
                sapi.Logger.Debug("Did not find the itemstack in any other slot of the inventory");
            }

            stackSlot.Itemstack.Attributes.SetString("gateAddressS", addr.ToString());
            stackSlot.Itemstack.Attributes.SetBool("generationfinished", true);
            stackSlot.Itemstack.Attributes.RemoveAttribute("position");
            stackSlot.Itemstack.TempAttributes.RemoveAttribute("gatelocatorworking");
            stackSlot.MarkDirty();

            currentRequest = null;
        }

        private ItemSlot TryFindItemSlotForRequest(GateGenerationRequest request)
        {
            if (request.forSlot.Itemstack == request.forStack) return request.forSlot;

            var slotPos = request.forStack.Attributes.GetString("position");
            string invPos;
            foreach (ItemSlot slot in request.forSlot.Inventory)
            {
                invPos = slot.Itemstack?.Attributes.GetString("position");
                if (slot.Itemstack == null || invPos == null) continue;

                if (invPos == slotPos)
                {
                    return slot;
                }
            }

            return null;
        }

        private BlockPos HasExitPoint(Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int nearX, int nearZ, string structureName)
        {
            IMapRegion mapregion = columnsByChunkCoordinate[new Vec2i(nearX, nearZ)][0].MapChunk.MapRegion;

            List<GeneratedStructure> structures = mapregion.GeneratedStructures;
            foreach (var structure in structures)
            {
                if (structure.Code.Contains(structureName))
                {
                    BlockPos pos = findStargate(structure.Location, columnsByChunkCoordinate, nearX, nearZ);
                    return pos;
                }
            }

            return null;
        }

        private BlockPos findStargate(Cuboidi location, Dictionary<Vec2i, IServerChunk[]> columnsByChunkCoordinate, int centerX, int centerZ)
        {
            const int chunksize = GlobalConstants.ChunkSize;

            for (int x = location.X1; x < location.X2; x++)
            {
                for (int y = location.Y1; y < location.Y2; y++)
                {
                    for (int z = location.Z1; z < location.Z2; z++)
                    {
                        int cx = x / chunksize;
                        int cz = z / chunksize;

                        IServerChunk[] chunks;
                        if (!columnsByChunkCoordinate.TryGetValue(new Vec2i(cx, cz), out chunks))
                        {
                            continue;
                        }

                        IServerChunk chunk = chunks[y / chunksize];

                        int lx = x % chunksize;
                        int ly = y % chunksize;
                        int lz = z % chunksize;

                        int index3d = (ly * chunksize + lz) * chunksize + lx;
                        Block block = sapi.World.Blocks[chunk.Data[index3d]];

                        BlockStargate gateBlock = block as BlockStargate;
                        if (gateBlock != null)
                        {
                            return new BlockPos(x, y, z);
                        }
                    }
                }
            }

            return null;
        }
    }
}
