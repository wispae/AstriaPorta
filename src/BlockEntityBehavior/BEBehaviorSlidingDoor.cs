using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
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

    protected MeshData Mesh;

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

    public static float GetRotateYRad(IPlayer player, BlockSelection blockSel)
    {
        BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
        double num = player.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
        double dz = (double)((float)player.Entity.Pos.Z) - ((double)targetPos.Z + blockSel.HitPosition.Z);
        double num2 = (double)((float)Math.Atan2(num, dz));
        return (float)((int)Math.Round(num2 / (double)GameMath.PIHALF)) * GameMath.PIHALF;
    }

    public virtual void OnBlockPlaced(ItemStack byItemstack, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (byItemstack == null)
        {
            return;
        }

        RotateYRad = BEBehaviorSlidingDoor.GetRotateYRad(byPlayer, blockSel);
        SetupRotation(true);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (!base.OnTesselation(mesher, tessThreadTesselator))
        {
            mesher.AddMeshData(Mesh, 1);
        }

        return true;
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
            SetupCombinableLeftDoor();
            SetupCombinableRightDoor();
        }

        if (Api.Side == EnumAppSide.Client)
        {
            SetupRotationClient(initialSetup);
        }
    }

    protected virtual void SetupCombinableLeftDoor()
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
                if (otherDoor._doorBehavior.Width > 1)
                {
                    Api.World.BlockAccessor.SetBlock(0, otherDoor.Pos);
                    var leftDoorPos = Pos.AddCopy(Facing.GetCW(), otherDoor._doorBehavior.Width + _doorBehavior.Width - 1);
                    Api.World.BlockAccessor.SetBlock(otherDoor.Block.Id, leftDoorPos);

                    otherDoor = Block.GetBEBehavior<BEBehaviorSlidingDoor>(leftDoorPos);
                    otherDoor.RotateYRad = RotateYRad;
                    otherDoor._doorBehavior.PlaceMultiblockParts(Api.World, leftDoorPos);
                    LeftDoor = otherDoor;
                    LeftDoor.RightDoor = this;
                    LeftDoor.SetupRotation(true);
                }
                else
                {
                    otherDoor._invertSide = false;
                    LeftDoor = otherDoor;
                    LeftDoor.RightDoor = this;
                    LeftDoor.Blockentity.MarkDirty(true, null);
                    LeftDoor.SetupRotation(false);
                }
            }
            else
            {
                LeftDoor = otherDoor;
                LeftDoor.RightDoor = this;
            }

            _invertSide = true;
            Blockentity.MarkDirty();
        }
    }

    protected virtual void SetupCombinableRightDoor()
    {
        BEBehaviorSlidingDoor otherDoor;
        int offset;

        if (BlockBehaviorSlidingDoor.HasCombinableRightDoor(Api.World, RotateYRad, Pos, _doorBehavior.Width, out otherDoor, out offset) &&
                otherDoor.LeftDoor == null && otherDoor.RightDoor == null &&
                (otherDoor.Facing == Facing || otherDoor.Facing == Facing.Opposite) &&
                Api.Side == EnumAppSide.Server)
        {
            if (!otherDoor.InvertSide)
            {
                if (otherDoor._doorBehavior.Width > 1)
                {
                    Api.World.BlockAccessor.SetBlock(0, otherDoor.Pos);
                    var rightDoorPos = Pos.AddCopy(Facing.GetCCW(), otherDoor._doorBehavior.Width + _doorBehavior.Width - 1);
                    Api.World.BlockAccessor.SetBlock(otherDoor.Block.Id, rightDoorPos);
                    otherDoor = Block.GetBEBehavior<BEBehaviorSlidingDoor>(rightDoorPos);
                    otherDoor.RotateYRad = RotateYRad;
                    otherDoor._invertSide = true;
                    otherDoor._doorBehavior.PlaceMultiblockParts(Api.World, rightDoorPos);
                    RightDoor = otherDoor;
                    RightDoor.LeftDoor = this;
                    RightDoor.SetupRotation(true);
                }
                else
                {
                    otherDoor._invertSide = true;
                    RightDoor = otherDoor;
                    RightDoor.LeftDoor = this;
                    RightDoor.Blockentity.MarkDirty(true, null);
                    RightDoor.SetupRotation(false);
                }
            }
            else
            {
                RightDoor = otherDoor;
                RightDoor.LeftDoor = this;
            }
        }
    }

    protected virtual void SetupRotationClient(bool initialSetup)
    {
        if (_doorBehavior.AnimatableOriginMesh == null)
        {
            string animKey = Block.Shape.ToString();
            Shape shape;
            _doorBehavior.AnimatableOriginMesh = animUtil.CreateMesh(animKey, null, out shape, null);
            _doorBehavior.AnimatableShape = shape;
            _doorBehavior.AnimatableDictKey = animKey;
        }

        if (_doorBehavior.AnimatableOriginMesh != null)
        {
            animUtil.InitializeAnimator(_doorBehavior.AnimatableDictKey, _doorBehavior.AnimatableOriginMesh, _doorBehavior.AnimatableShape, null, EnumRenderStage.Opaque);
            UpdateMeshAndAnimations();
        }
    }

    protected virtual void UpdateMeshAndAnimations()
    {
        Mesh = _doorBehavior.AnimatableOriginMesh.Clone();
        if (RotateYRad != 0f)
        {
            float rot = (this.InvertSide ? (-RotateYRad) : RotateYRad);
            Mesh = Mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, rot, 0f);
            animUtil.renderer.rotationDeg.Y = rot * GameMath.RAD2DEG;
        }

        if (InvertSide)
        {
            Matrixf matf = new();
            matf.Translate(0.5f, 0.5f, 0.5f).Scale(-1f, 1f, 1f).Translate(-0.5f, -0.5f, -0.5f);
            Mesh.MatrixTransform(matf.Values);
            animUtil.renderer.backfaceCulling = false;
            animUtil.renderer.ScaleX = -1f;
        }
    }
}
