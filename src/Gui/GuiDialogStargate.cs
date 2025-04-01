using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
	public class GuiDialogStargate : GuiDialogBlockEntity
	{
		EnumPosFlag screenPos;

		InventoryBase inv;

		public override double DrawOrder => 0.2;

		public GuiDialogStargate(string dialogTitle,
								 string displayAddress,
								 InventoryBase Inventory,
								 BlockPos BlockEntityPosition,
								 ICoreClientAPI capi)
			: base(dialogTitle, Inventory, BlockEntityPosition, capi)
		{
			if (IsDuplicate) return;

			inv = Inventory;
			capi.World.Player.InventoryManager.OpenInventory(Inventory);

			SetupDialog(displayAddress);
		}

		private void SetupDialog(string displayAddress)
		{
			ElementBounds txtBounds = ElementBounds.Fixed(0, 20, 200, 20);
			ElementBounds camoSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 75, 5, 1);

			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(ElementBounds.Fixed(0, 0, 210, 250));

			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
				.WithFixedAlignmentOffset(IsRight(screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
				.WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);

			ClearComposers();
			SingleComposer = capi.Gui
				.CreateCompo("blockentitystargate" + BlockEntityPosition, dialogBounds)
				.AddShadedDialogBG(bgBounds)
				.AddDialogTitleBar(DialogTitle, OnTitleBarClose)
				.BeginChildElements(bgBounds)
					.AddStaticText(displayAddress, CairoFont.WhiteSmallishText(), txtBounds)
					.AddItemSlotGrid(Inventory, SendInvPacket, 5, camoSlotBounds, "camoInventoryGrid")
				.EndChildElements()
				.Compose();

			SingleComposer.GetSlotGrid("camoInventoryGrid").CanClickSlot = OnCanClickSlot;
		}

		private bool OnCanClickSlot(int slotID)
		{
			ItemStack mousestack = capi.World.Player.InventoryManager.MouseItemSlot.Itemstack;

			if (mousestack == null || mousestack.Block == null)
			{
				inv[slotID].Itemstack = null;
			}
			else
			{
				inv[slotID].Itemstack = mousestack.Clone();
			}

			inv[slotID].MarkDirty();

			return false;
		}

		private void SendInvPacket(object packet)
		{
			capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
		}

		private void OnInventorySlotModified(int slotid)
		{
			inv.MarkSlotDirty(slotid);
		}

		private void OnTitleBarClose()
		{
			TryClose();
		}

		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			inv.SlotModified += OnInventorySlotModified;
		}

		public override void OnGuiClosed()
		{
			inv.SlotModified -= OnInventorySlotModified;
			TreeAttribute tree = new TreeAttribute();
			for (int i = 0; i < inv.Count; i++)
			{
				if (inv[i].Itemstack == null) continue;
				tree.SetItemstack("stack" + i, inv[i].Itemstack.Clone());
			}

			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter writer = new BinaryWriter(ms);
				tree.ToBytes(writer);
				capi.Network.SendBlockEntityPacket(BlockEntityPosition, (int)EnumStargatePacketType.CamoUpdate, ms.ToArray());
			}

			SingleComposer.GetSlotGrid("camoInventoryGrid").OnGuiClosed(capi);

			base.OnGuiClosed();

			FreePos("smallblockgui", screenPos);
		}
	}
}
