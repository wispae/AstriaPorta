using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content;

public class BEBehaviorSlidingDoor : BEBehaviorAnimatable, IRotatable
{
    private BlockBehaviorSlidingDoor _doorBehavior;
    private bool _invertSide;

    private Vec3i leftDoorOffset;
    private Vec3i rightDoorOffset;

    public float RotateYRad;

    public bool InvertSide
    {
        get
        {
            return _invertSide;
        }
    }

    public BlockBehaviorSlidingDoor DoorBehavior
    {
        get
        {
            return _doorBehavior;
        }
    }

    public BlockFacing Facing
    {
        get
        {
            return BlockFacing.HorizontalFromYaw(this.RotateYRad);
        }
    }

    public BEBehaviorSlidingDoor LeftDoor
    {
        get
        {
            if (leftDoorOffset == null) return null;

            var doorAt = BlockBehaviorSlidingDoor.GetDoorAt(Api.World, Pos.AddCopy(leftDoorOffset));
            if (doorAt == null) leftDoorOffset = null;

            return doorAt;
        }
        protected set
        {
            leftDoorOffset = ((value == null) ? null : value.Pos.SubCopy(Pos).ToVec3i());
        }
    }

    public BEBehaviorSlidingDoor RightDoor
    {
        get
        {
            if (rightDoorOffset == null) return null;

            var doorAt = BlockBehaviorSlidingDoor.GetDoorAt(Api.World, Pos.AddCopy(rightDoorOffset));
            if (doorAt == null) rightDoorOffset = null;

            return doorAt;
        }
        set
        {
            rightDoorOffset = ((value == null) ? null : value.Pos.SubCopy(Pos).ToVec3i());
        }
    }

    public BEBehaviorSlidingDoor(BlockEntity be) : base(be)
    {
        _doorBehavior = Block.GetBehavior<BlockBehaviorSlidingDoor>();
    }

    public virtual void OnBlockPlaced(ItemStack byItemstack, IPlayer byPlayer, BlockSelection blockSel)
    {
        throw new NotImplementedException();
    }

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        RotateYRad = tree.GetFloat("rotateYRad", 0f);
        RotateYRad = (RotateYRad - (float)degreeRotation * GameMath.DEG2RAD) % GameMath.TWOPI;
        tree.SetFloat("rotateYRad", RotateYRad);
    }

    protected void SetupRotation(bool initialSetup)
    {
        if (initialSetup)
        {
            BEBehaviorSlidingDoor otherDoor;
            int offset;

            if (BlockBehaviorSlidingDoor.HasCombinableLeftDoor(Api.World, RotateYRad, Pos, _doorBehavior.Width, out otherDoor, out offset)
                && otherDoor.LeftDoor == null
                && otherDoor.RightDoor == null
                && otherDoor.Facing == Facing)
            {
                if (otherDoor.InvertSide)
                {
                    if (otherDoor.)
                }
            }
        }
    }
}
