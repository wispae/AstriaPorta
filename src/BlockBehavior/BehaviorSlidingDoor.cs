using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content;

public class BlockBehaviorSlidingDoor : StrongBlockBehavior, IMultiBlockColSelBoxes
{
    public int Height;
    public int Width;
    public bool SingleDoor;

    public BlockBehaviorSlidingDoor(Block b) : base(b)
    {
    }

    public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        var pos = blockSel.Position.Copy();
        float rotRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
        var facing = BlockFacing.HorizontalFromYaw(rotRad);
        bool blocked = false;
        BEBehaviorSlidingDoor otherDoor;
        int offset;
        bool opposite = false;

        if (!SingleDoor)
        {
            opposite = HasCombinableLeftDoor(world, rotRad, blockSel.Position, Width, out otherDoor, out offset);
            if (opposite && Width > 1 && offset != 0)
            {
                pos.Add(facing.GetCCW(), offset);
            }
            
            if (!opposite && HasCombinableRightDoor(world, rotRad, blockSel.Position, Width, out otherDoor, out offset) && Width > 1 && offset != 0)
            {
                pos.Add(facing.GetCW(), offset);
            }
        }

        IterateOverEach(pos, rotRad, opposite, delegate (BlockPos mpos)
        {
            if (!world.BlockAccessor.GetBlock(mpos, 1).IsReplacableBy(block))
            {
                blocked = true;
                return false;
            }
            return true;
        });

        if (blocked)
        {
            handling = EnumHandling.PreventDefault;
            failureCode = "notenoughspace";
            return false;
        }

        return base.CanPlaceBlock(world, byPlayer, blockSel, ref handling, ref failureCode);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PreventDefault;
        var pos = blockSel.Position.Copy();
        IBlockAccessor ba = world.BlockAccessor;

        if (!block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

        if (!SingleDoor)
        {
            float rotRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
            var facing = BlockFacing.HorizontalFromYaw(rotRad);
            BEBehaviorSlidingDoor otherDoor;
            int offset;

            if (HasCombinableLeftDoor(world, rotRad, blockSel.Position, Width, out otherDoor, out offset))
            {
                if (Width > 1 && offset != 0)
                {
                    pos.Add(facing.GetCCW(), offset);
                }
            }
            else if (HasCombinableRightDoor(world, rotRad, blockSel.Position, Width, out otherDoor, out offset) && Width > 1 && offset != 0)
            {
                pos.Add(facing.GetCW(), offset);
            }
        }

        return PlaceDoor(world, byPlayer, itemstack, blockSel, pos, ba);
    }

    protected bool PlaceDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack fromStack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
    {
        ba.SetBlock(block.BlockId, pos);
        var be = ba.GetBlockEntity(pos);
        var beh = (be != null) ? be.GetBehavior<BEBehaviorSlidingDoor>() : null;

        if (beh == null) return false;

        beh.OnBlockPlaced(fromStack, byPlayer, blockSel);
        if (world.Side == EnumAppSide.Server)
        {
            PlaceMultiblockParts(world, pos);
        }

        return true;
    }

    public void PlaceMultiblockParts(IWorldAccessor world, BlockPos pos)
    {
        var be = world.BlockAccessor.GetBlockEntity(pos);
        var beh = ((be != null) ? be.GetBehavior<BEBehaviorSlidingDoor>() : null);
        float rotRad = ((beh != null) ? beh.RotateYRad : 0f);
        this.IterateOverEach(pos, rotRad, beh != null && beh.InvertSide, delegate (BlockPos mpos)
        {
            if (mpos == pos) return true;

            int dx = mpos.X - pos.X;
            int dy = mpos.Y - pos.Y;
            int dz = mpos.Z - pos.Z;
            string sdx = ((dx < 0) ? "n" : ((dx > 0) ? "p" : "")) + Math.Abs(dx).ToString();
            string sdy = ((dy < 0) ? "n" : ((dy > 0) ? "p" : "")) + Math.Abs(dy).ToString();
            string sdz = ((dz < 0) ? "n" : ((dz > 0) ? "p" : "")) + Math.Abs(dz).ToString();

            var loc = new AssetLocation(string.Concat("multiblock-monolithic-", sdx, "-", sdy, "-", sdz));
            var block = world.GetBlock(loc);
            world.BlockAccessor.SetBlock(block.Id, mpos);
            if (world.Side == EnumAppSide.Server)
            {
                world.BlockAccessor.TriggerNeighbourBlockUpdate(mpos);
            }

            return true;
        });
    }

    public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i origin)
    {
        return block.CollisionBoxes;
    }

    public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset)
    {
        return block.SelectionBoxes;
    }

    protected void IterateOverEach(BlockPos pos, float rotRad, bool invert, ActionConsumable<BlockPos> onBlock)
    {
        int xMult = 0, zMult = 0;
        if (rotRad == 0)
        {
            xMult = -1;
        }
        else if (rotRad == 180)
        {
            xMult = 1;
        }

        if (rotRad == 90)
        {
            zMult = -1;
        }
        else if (rotRad == 270)
        {
            zMult = 1;
        }

        if (invert)
        {
            xMult *= -1;
            zMult *= -1;
        }

        BlockPos tmpPos = new BlockPos(pos.dimension);
        // sliding doors only go sideways + phase through solid matter
        for (int dy = 0; dy < Height; dy++)
        {
            for (int dx = 0; dx < Width; dx++)
            {
                tmpPos.Set(pos.X + dx * xMult, pos.Y + dy, pos.Z + dx * zMult);
                if (!onBlock(tmpPos))
                {
                    return;
                }
            }
        }
    }

    public static bool HasCombinableLeftDoor(IWorldAccessor world, float rotRad, BlockPos pos, int width, out BEBehaviorSlidingDoor leftDoor, out int leftOffset)
    {
        leftOffset = 0;
        leftDoor = null;

        BlockFacing leftFacing = BlockFacing.HorizontalFromYaw(rotRad).GetCW();
        BlockPos leftPos = pos.AddCopy(leftFacing);
        leftDoor = GetDoorAt(world, leftPos);
        if (width > 1)
        {
            if (leftDoor == null)
            {
                for (int i = 2; i <= width; i++)
                {
                    leftPos = pos.AddCopy(leftFacing, i);
                    leftDoor = GetDoorAt(world, leftPos);
                    if (leftDoor != null)
                    {
                        break;
                    }
                }
            }
            if (leftDoor != null)
            {
                BlockPos offsetPos = leftDoor.Pos.AddCopy(leftFacing.Opposite, leftDoor.InvertSide ? width : (width + leftDoor.DoorBehavior.Width - 1));
                leftOffset = (int)pos.DistanceTo(offsetPos);
                if ((leftDoor.Facing.Axis == EnumAxis.X && leftPos.X != leftDoor.Pos.X) || (leftDoor.Facing.Axis == EnumAxis.Z && leftPos.Z != leftDoor.Pos.Z))
                {
                    leftDoor = null;
                    leftOffset = 0;
                }
            }
        }

        return leftDoor != null && leftDoor.LeftDoor == null && leftDoor.RightDoor == null && leftDoor.Facing == BlockFacing.HorizontalFromYaw(rotRad);
    }

    public static bool HasCombinableRightDoor(IWorldAccessor world, float rotRad, BlockPos pos, int width, out BEBehaviorSlidingDoor rightDoor, out int rightOffset)
    {
        rightOffset = 0;
        rightDoor = null;

        BlockFacing rightFacing = BlockFacing.HorizontalFromYaw(rotRad).GetCCW();
        BlockPos rightPos = pos.AddCopy(rightFacing);
        rightDoor = GetDoorAt(world, rightPos);
        if (width > 1)
        {
            if (rightDoor == null)
            {
                for (int i = 2; i <= width; i++)
                {
                    rightPos = pos.AddCopy(rightFacing, i);
                    rightDoor = GetDoorAt(world, rightPos);
                    if (rightDoor != null)
                    {
                        break;
                    }
                }
            }

            if (rightDoor != null)
            {
                BlockPos offsetPos = rightDoor.Pos.AddCopy(rightFacing.Opposite, rightDoor.InvertSide ? width : (width + rightDoor.DoorBehavior.Width - 1));
                rightOffset = (int)pos.DistanceTo(offsetPos);
                if ((rightDoor.Facing.Axis == EnumAxis.X && rightPos.X != rightDoor.Pos.X) || (rightDoor.Facing.Axis == EnumAxis.Z && rightPos.Z != rightDoor.Pos.Z))
                {
                    rightDoor = null;
                    rightOffset = 0;
                }
            }
        }

        return rightDoor != null && rightDoor.RightDoor == null && rightDoor.LeftDoor == null && rightDoor.Facing == BlockFacing.HorizontalFromYaw(rotRad);
    }

    public static BEBehaviorSlidingDoor GetDoorAt(IWorldAccessor world, BlockPos pos)
    {
        var be = world.BlockAccessor.GetBlockEntity(pos);
        var beh = ((be != null) ? be.GetBehavior<BEBehaviorSlidingDoor>() : null);

        if (beh != null)
        {
            return beh;
        }

        var blockMb = world.BlockAccessor.GetBlock(pos) as BlockMultiblock;
        if (blockMb != null)
        {
            var be2 = world.BlockAccessor.GetBlockEntity(pos.AddCopy(blockMb.OffsetInv));
            return (be2 != null) ? be2.GetBehavior<BEBehaviorSlidingDoor>() : null;
        }

        return null;
    }
}
