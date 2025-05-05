using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AstriaPorta.Systems
{
    public enum EnumAttributeSyncOperation
    {
        Add,
        Modify,
        Remove
    }

    [ProtoContract]
    public struct PlayerInventoryAttributeSyncMessage
    {
        [ProtoMember(1)]
        public EnumAttributeSyncOperation SyncOperation;
        [ProtoMember(2)]
        public long EntityId;
        [ProtoMember(3)]
        public string InventoryId;
        [ProtoMember(4)]
        public int SlotId;
        [ProtoMember(5)]
        public byte[] Attribute;
    }

    [ProtoContract]
    public struct BlockInventoryAttributeSyncMessage
    {
        [ProtoMember(1)]
        public EnumAttributeSyncOperation SyncOperation;
        [ProtoMember(2)]
        public int BlockX;
        [ProtoMember(3)]
        public int BlockY;
        [ProtoMember(4)]
        public int BlockZ;
        [ProtoMember(5)]
        public string InventoryId;
        [ProtoMember(6)]
        public int SlotId;
        [ProtoMember(7)]
        public byte[] Attribute;
    }

    public class InventoryAttributeSyncSystem : ModSystem
    {
        public string AttributeSyncChannelName = "attributeSyncChannel";

        private IServerNetworkChannel serverChannel;
        private IClientNetworkChannel clientChannel;

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;

            clientChannel = api.Network.GetChannel(AttributeSyncChannelName);
            if (clientChannel == null )
            {
                clientChannel = api.Network.RegisterChannel(AttributeSyncChannelName);
            }
            clientChannel.RegisterMessageType<PlayerInventoryAttributeSyncMessage>();
            clientChannel.RegisterMessageType<BlockInventoryAttributeSyncMessage>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;

            serverChannel = api.Network.GetChannel(AttributeSyncChannelName);
            if (serverChannel == null)
            {
                serverChannel = api.Network.RegisterChannel(AttributeSyncChannelName);
            }
            serverChannel.RegisterMessageType<PlayerInventoryAttributeSyncMessage>();
            serverChannel.RegisterMessageType<BlockInventoryAttributeSyncMessage>();
            serverChannel.SetMessageHandler<PlayerInventoryAttributeSyncMessage>(OnReceivedClientPlayerOperation);
            serverChannel.SetMessageHandler<BlockInventoryAttributeSyncMessage>(OnReceivedClientBlockOperation);
        }

        public void ModifyAttribute(IPlayer forPlayer, IInventory inventory, int slotNumber, ITreeAttribute attribute)
        {
            SendPlayerAttributeOperation(forPlayer, inventory, slotNumber, attribute, EnumAttributeSyncOperation.Modify);
        }

        public void ModifyAttribute(IPlayer forPlayer, ItemSlot slot, ITreeAttribute attribute)
        {
            SendPlayerAttributeOperation(forPlayer, slot.Inventory, slot.Inventory.GetSlotId(slot), attribute, EnumAttributeSyncOperation.Modify);
        }

        public void ModifyAttribute(BlockPos pos, IInventory inventory, int slotNumber, ITreeAttribute attribute)
        {
            SendBlockAttributeOperation(pos, inventory, slotNumber, attribute, EnumAttributeSyncOperation.Modify);
        }

        public void DeleteAttribute(IPlayer player, IInventory inventory, int slotNumber, ITreeAttribute attribute)
        {
            SendPlayerAttributeOperation(player, inventory, slotNumber, attribute, EnumAttributeSyncOperation.Remove);
        }

        public void DeleteAttribute(IPlayer player, ItemSlot slot, ITreeAttribute attribute)
        {
            SendPlayerAttributeOperation(player, slot.Inventory, slot.Inventory.GetSlotId(slot), attribute, EnumAttributeSyncOperation.Remove);
        }

        public void DeleteAttribute(BlockPos pos, IInventory inventory, int slotNumber, ITreeAttribute attribute)
        {
            SendBlockAttributeOperation(pos, inventory, slotNumber, attribute, EnumAttributeSyncOperation.Remove);
        }

        private void SendPlayerAttributeOperation(IPlayer player, IInventory inventory, int slotNumber, ITreeAttribute attribute, EnumAttributeSyncOperation op)
        {
            byte[] attrBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                attribute.ToBytes(writer);
                attrBytes = ms.ToArray();
            }

            PlayerInventoryAttributeSyncMessage message = new PlayerInventoryAttributeSyncMessage
            {
                SyncOperation = op,
                EntityId = player.Entity.EntityId,
                InventoryId = inventory.InventoryID,
                SlotId = slotNumber,
                Attribute = attrBytes
            };

            clientChannel.SendPacket(message);
        }

        private void SendBlockAttributeOperation(BlockPos pos, IInventory inventory, int slotNumber, ITreeAttribute attribute, EnumAttributeSyncOperation operation)
        {
            byte[] attrBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                attribute.ToBytes(writer);
                attrBytes = ms.ToArray();
            }

            BlockInventoryAttributeSyncMessage message = new BlockInventoryAttributeSyncMessage
            {
                SyncOperation = operation,
                InventoryId = inventory.InventoryID,
                SlotId = slotNumber,
                Attribute = attrBytes,
                BlockX = pos.X,
                BlockY = pos.Y,
                BlockZ = pos.Z
            };

            clientChannel.SendPacket(message);
        }

        private void OnReceivedClientPlayerOperation(IServerPlayer fromPlayer, PlayerInventoryAttributeSyncMessage message)
        {
            IInventory playerInventory;
            if (fromPlayer.Entity.EntityId == message.EntityId)
            {
                playerInventory = fromPlayer.InventoryManager.GetInventory(message.InventoryId);
            } else
            {
                playerInventory = TryGetPlayerInventory(message.EntityId, message.InventoryId);
            }

            if (playerInventory == null) return;
            if (playerInventory.Count <= message.SlotId) return;

            ItemSlot slot = playerInventory[message.SlotId];
            if (slot == null) return;

            TreeAttribute tree = TreeAttribute.CreateFromBytes(message.Attribute);

            foreach (string key in tree.Keys)
            {
                ApplySlotOperation(slot, key, tree[key], message.SyncOperation);
            }
            slot.MarkDirty();
        }

        private void OnReceivedClientBlockOperation(IServerPlayer fromPlayer, BlockInventoryAttributeSyncMessage message)
        {
            BlockPos pos = new BlockPos(message.BlockX, message.BlockY, message.BlockZ);
            IInventory blockInventory = TryGetBlockInventory(pos, message.InventoryId);

            if (blockInventory == null) return;
            if (blockInventory.Count <= message.SlotId) return;

            ItemSlot slot = blockInventory[message.SlotId];
            if (slot == null) return;

            TreeAttribute tree = TreeAttribute.CreateFromBytes(message.Attribute);

            foreach (string key in tree.Keys)
            {
                ApplySlotOperation(slot, key, tree[key], message.SyncOperation);
            }
            slot.MarkDirty();
        }

        private void ApplySlotOperation(ItemSlot slot, string attributeName, IAttribute attribute, EnumAttributeSyncOperation op)
        {
            switch (op)
            {
                case EnumAttributeSyncOperation.Add:
                    ModifySlotAttribute(slot, attributeName, attribute);
                    break;
                case EnumAttributeSyncOperation.Modify:
                    ModifySlotAttribute(slot, attributeName, attribute);
                    break;
                case EnumAttributeSyncOperation.Remove:
                    RemoveSlotAttribute(slot, attributeName);
                    break;
            }
        }

        /// <summary>
        /// Modifies (add / modify) an attribute from the itemstack in the provided slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="attributeName"></param>
        /// <param name="attribute"></param>
        private void ModifySlotAttribute(ItemSlot slot, string attributeName, IAttribute attribute)
        {
            if (slot.Itemstack == null) return;

            slot.Itemstack.Attributes[attributeName] = attribute;
        }

        /// <summary>
        /// Removes an attribute from the itemstack in the provided slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="attributeName"></param>
        private void RemoveSlotAttribute(ItemSlot slot, string attributeName)
        {
            if (slot.Itemstack == null) return;

            slot.Itemstack.Attributes.RemoveAttribute(attributeName);
        }

        /// <summary>
        /// Attempts to get the inventory attached to the player with the provided id.<br/>
        /// Requires the player to be online
        /// </summary>
        /// <param name="playerEntityId"></param>
        /// <param name="inventoryId"></param>
        /// <returns>The requested inventory or null on failure</returns>
        private IInventory TryGetPlayerInventory(long playerEntityId, string inventoryId)
        {
            IServerPlayer p = sapi.World.AllOnlinePlayers.First(p => p.Entity.EntityId == playerEntityId) as IServerPlayer;
            if (p == null) return null;

            if (p.ConnectionState == EnumClientState.Queued || p.ConnectionState == EnumClientState.Offline || p.ConnectionState == EnumClientState.Connecting) return null;

            return p.InventoryManager.GetInventory(inventoryId);
        }

        /// <summary>
        /// Attempts to get the inventory located at the specified BlockPos.<br/>
        /// Requires a BlockEntity at pos that implements IBlockEntityContainer
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="inventoryId"></param>
        /// <returns>The requested inventory or null on failure</returns>
        private IInventory TryGetBlockInventory(BlockPos pos, string inventoryId)
        {
            BlockEntity be = sapi.World.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return null;

            IBlockEntityContainer bc = be as IBlockEntityContainer;
            if (bc == null) return null;
            if (bc.Inventory.InventoryID != inventoryId) return null;

            return bc.Inventory;
        }
    }
}
