using AstriaPorta.Content;
using AstriaPorta.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace AstriaPorta.Systems
{
    public class StargateManagerSystem : ModSystem
    {
        private ICoreServerAPI sapi;
        private static long tickListenerId = -1;

        public static StargateManagerSystem GetInstance(ICoreAPI api) => api.ModLoader.GetModSystem<StargateManagerSystem>();

        private ConcurrentDictionary<ulong, AddressCoordinates> knownGates;
        private ConcurrentDictionary<ulong, BlockPos> loadedGates;
        private ConcurrentDictionary<Vec2i, int> activeBlockChunks;
        private List<Vec2i> releasedChunks;
        private object releasedListLock = new object();

        public StargateManagerSystem() : base()
        {
            knownGates = GetExistingGateList();
            loadedGates = new ConcurrentDictionary<ulong, BlockPos>();
            activeBlockChunks = new ConcurrentDictionary<Vec2i, int>();
            releasedChunks = new List<Vec2i>();
        }

        public override double ExecuteOrder()
        {
            return 0.37d;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            FlushRegisteredGates();
            tickListenerId = sapi.Event.RegisterGameTickListener(TryUnloadReleasedChunks, 10000);
        }

        /// <summary>
        /// Unregisters all known gates from the system
        /// <br/>
        /// Use when leaving the world to start with a clean slate
        /// </summary>
        public void FlushRegisteredGates()
        {
            Mod.Logger.Debug("Flushing all gates from GateManager...");
            knownGates.Clear();
            loadedGates.Clear();
            activeBlockChunks.Clear();
            releasedChunks.Clear();
            if (tickListenerId != -1)
            {
                sapi.Event.UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
            }
            Mod.Logger.Debug("All gates flushed, lists emptied");
        }

        /// <summary>
        /// Periodically scans released chunks and unloads when possible
        /// </summary>
        /// <param name="t"></param>
        public void TryUnloadReleasedChunks(float t)
        {
            if (releasedChunks.Count == 0) return;

            int releasedBefore = releasedChunks.Count;

            lock (releasedListLock)
            {
                releasedChunks.RemoveAll((pos) =>
                {
                    if (CanUnload(pos.X, pos.Y))
                    {
                        sapi.WorldManager.UnloadChunkColumn(pos.X / GlobalConstants.ChunkSize, pos.Y / GlobalConstants.ChunkSize);
                        if (activeBlockChunks.ContainsKey(pos / GlobalConstants.ChunkSize)) activeBlockChunks.Remove(pos / GlobalConstants.ChunkSize);
                        return true;
                    }
                    return false;
                });
            }

#if DEBUG
            if (releasedBefore != releasedChunks.Count)
            {
                Mod.Logger.Debug("Unloaded " + (releasedBefore - releasedChunks.Count) + " released chunks");
            }
#endif
        }

        /// <summary>
        /// Tests wether any players are inside the chunk render range
        /// </summary>
        /// <param name="pos">A blockpos within the chunk to unload</param>
        /// <returns></returns>
        private bool CanUnload(BlockPos pos)
        {
            // still need to test if unloading a chunk with a player in it will just reload it again
            return CanUnload(pos.X, pos.Z);
        }

        /// <summary>
        /// Tests wether any players are inside the chunk render range
        /// </summary>
        /// <param name="X">Block X</param>
        /// <param name="Z">Block Z</param>
        /// <returns></returns>
        private bool CanUnload(int X, int Z)
        {
            IServerWorldAccessor wa = sapi.World;
            int serverRange = sapi.Server.Config.MaxChunkRadius + 1;
            int worldHeight = sapi.WorldManager.MapSizeY;
            IPlayer[] inrangeplayers = wa.GetPlayersAround(new Vec3d(X, 0, Z), serverRange * GlobalConstants.ChunkSize * (serverRange * GlobalConstants.ChunkSize), worldHeight);

#if DEBUG
			BlockPos pbp;
			Mod.Logger.Debug("Testing for players around blockpos (" + X + "; " + Z + ")");
			foreach (IPlayer p in inrangeplayers)
			{
				pbp = p.Entity.Pos.AsBlockPos;
				Mod.Logger.Debug("Found player " + p.PlayerName + " at position (" + pbp.X + "; " + pbp.Z + ") and distance (" + (X - pbp.X) + "; " + (Z - pbp.Z) + ")");
			}
#endif

            return inrangeplayers.Length == 0;
        }

        /// <summary>
        /// Remove StargateAddress bits metadata
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        private ulong UnMetaBits(ulong bits)
        {
            return bits & 0x00_FF_FF_FF__FF_FF_FF_FF;
        }

        /// <summary>
        /// Registers a gate so that it is easily known to be loaded
        /// </summary>
        /// <param name="gate"></param>
        /// <returns>Whether the gate was registered or not</returns>
        public bool RegisterLoadedGate(BlockEntityStargate gate)
        {
#if DEBUG
			Mod.Logger.Debug($"Registered gate with address {gate.GateAddress} to manager");
#endif
            loadedGates.TryAdd(UnMetaBits(gate.GateAddress.AddressBits), gate.Pos);

            return true;
        }

        /// <summary>
        /// Removes provided gate from known loaded gates
        /// </summary>
        /// <param name="gate"></param>
        public void UnregisterLoadedGate(BlockEntityStargate gate)
        {
#if DEBUG
			Mod.Logger.Debug($"Unregistered gate with address {gate.GateAddress} to manager");
#endif
            loadedGates.Remove(gate.GateAddress.AddressBits);
        }

        /// <summary>
        /// Force loads the gate at the provided address if it exists.<br/>
        /// Unloads the chunks if no gate exists, otherwise the counter for that chunk
        /// will increase<br/>
        /// Passes the loaded gate to the requester when done
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requester"></param>
        /// <param name="shouldHaveLoaded"></param>
        public void LoadRemoteGate(StargateAddress address, BlockEntityStargate requester, bool shouldHaveLoaded = false)
        {
            if (!address.IsValid)
            {
                Mod.Logger.Debug($"Address {address} was invalid, returning null");
                requester.RemotePosition = null;
                return;
            }

            AddressCoordinates gateCoordinates = address.AddressCoordinates;
            BlockPos gatePos = new BlockPos(0);

            ulong bitKey = UnMetaBits(address.AddressBits);

            bool keyExists = false;
            BlockEntityStargate remoteGate;

            keyExists = loadedGates.ContainsKey(bitKey);
            if (keyExists) gatePos = loadedGates[bitKey];

            if (!keyExists)
            {
                TryForceLoadUnloadedGate(address, gatePos, gateCoordinates, requester, shouldHaveLoaded);
                return;
            }
            else
            {
                if (!IsForceLoaded(gatePos.X / GlobalConstants.ChunkSize, gatePos.Z / GlobalConstants.ChunkSize))
                {
                    ForceLoadChunk(gatePos);
                }

                remoteGate = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityStargate>(gatePos);
                if (remoteGate != null)
                {
                    remoteGate.IsForceLoaded = true;
                }
                else
                {
                    Mod.Logger.Debug($"The requested gate ({address}) was null");
                }
            }
            requester.RemotePosition = remoteGate?.Pos;
            return;
        }

        private void TryForceLoadUnloadedGate(StargateAddress address, BlockPos gatePos, AddressCoordinates gateCoordinates, BlockEntityStargate requester, bool shouldHaveLoaded = false)
        {
            gatePos.X = gateCoordinates.X;
            gatePos.Y = gateCoordinates.Y;
            gatePos.Z = gateCoordinates.Z;
            gatePos.dimension = gateCoordinates.Dimension;

            IServerChunk gateChunk = sapi.WorldManager.GetChunk(gatePos);

            if (gateChunk != null)
            {
                // target chunk loaded and no gate there
                requester.RemotePosition = null;

                if (shouldHaveLoaded) ReleaseChunk(gatePos);

                return;
            }
            else if (shouldHaveLoaded)
            {
                ReleaseChunk(gatePos);
                return;
            }

            ChunkLoadOptions co = new ChunkLoadOptions
            {
                // Remote gate should force load itself if it exists
                KeepLoaded = true,
                OnLoaded = () =>
                {
                    LoadRemoteGate(address, requester, true);
                }
            };

            ForceLoadChunk(gatePos, co);
        }

        /// <summary>
        /// Keeps the chunk the pos is in loaded
        /// </summary>
        /// <param name="blockPos">The block position to keep loaded</param>
        public void ForceLoadChunk(BlockPos blockPos, ChunkLoadOptions co = null)
        {
            Vec2i dictKey = new Vec2i(blockPos.X / GlobalConstants.ChunkSize, blockPos.Z / GlobalConstants.ChunkSize);

            if (!activeBlockChunks.ContainsKey(dictKey)) activeBlockChunks[dictKey] = 0;
            activeBlockChunks[dictKey]++;

            // > 1 means already loaded
            if (activeBlockChunks[dictKey] > 1)
            {
                return;
            }

            if (co == null)
            {
                co = new ChunkLoadOptions
                {
                    KeepLoaded = true,
                };
            }
#if DEBUG
			Mod.Logger.Debug($"Chunkloaded chunk at pos {blockPos}");
#endif
            sapi.WorldManager.LoadChunkColumnPriority(dictKey.X, dictKey.Y, co);
        }

        /// <summary>
        /// Releases a lock for a force loaded chunk<br/>
        /// Decrements that chunks counter and queues the chunk
        /// for unloading when counter reaches 0
        /// </summary>
        /// <param name="blockPos"></param>
        public void ReleaseChunk(BlockPos blockPos)
        {
            Vec2i dictKey = new Vec2i(blockPos.X / GlobalConstants.ChunkSize, blockPos.Z / GlobalConstants.ChunkSize);

            if (!activeBlockChunks.ContainsKey(dictKey)) return;

            activeBlockChunks[dictKey]--;

            if (activeBlockChunks[dictKey] <= 0)
            {
                long chunkIndex = sapi.WorldManager.MapChunkIndex2D(dictKey.X, dictKey.Y);
                ((ServerMain)sapi.World).RemoveChunkColumnFromForceLoadedList(chunkIndex);
                activeBlockChunks.Remove(dictKey);
                return;
            }

            Mod.Logger.Debug("Chunk (" + dictKey.X + " ; " + dictKey.Y + ") is seemingly still in use");
        }

        /// <summary>
        /// Checks if chunk at coordinates is force loaded by the WorldGateManager
        /// </summary>
        /// <param name="X">Chunk X</param>
        /// <param name="Z">Chunk Z</param>
        /// <returns></returns>
        public bool IsForceLoaded(int X, int Z)
        {
            return activeBlockChunks.ContainsKey(new Vec2i(X, Z));
        }

        public BlockPos GetClosestGatePos(int x, int z, int dim)
        {
            if (loadedGates.IsEmpty)
            {
                return new BlockPos(-1, -1, -1, -1);
            }

            BlockPos origin = new BlockPos(x, 0, z, dim);

            return loadedGates.Values.Aggregate((closest, next) =>
            {
                return origin.DistanceTo(closest) < origin.DistanceTo(next) ? closest : next;
            });
        }

        private ConcurrentDictionary<ulong, AddressCoordinates> GetExistingGateList()
        {
            // ?????? why did I return an empty Dictionary????
            return new ConcurrentDictionary<ulong, AddressCoordinates>();
        }
    }
}
