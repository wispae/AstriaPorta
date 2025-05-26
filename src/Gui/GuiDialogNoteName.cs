using AstriaPorta.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
    public class GuiDialogNoteName : GuiDialogGeneric
    {
        private ItemSlot forSlot;
        private GuiElementTextInput nameInput;

        private string addressName = string.Empty;
        private int maxNameLength = 16;

        public GuiDialogNoteName(string DialogTitle, string text, ICoreClientAPI capi, TextAreaConfig signConfig, ItemSlot forSlot) : base(DialogTitle, capi)
        {
            this.forSlot = forSlot;
            this.capi = capi;

            ElementBounds bgBounds = ElementBounds.FixedSize(signConfig.MaxWidth + 32, 120).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds inputBounds = ElementBounds.FixedSize(signConfig.MaxWidth + 4, 24).WithAlignment(EnumDialogArea.CenterMiddle).WithFixedOffset(0, 2 * GuiStyle.ElementToDialogPadding);

            ElementBounds cancelBounds = ElementBounds.FixedSize(0, 0).FixedUnder(inputBounds, 10).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8, 2).WithFixedAlignmentOffset(GuiStyle.ElementToDialogPadding, 0);
            ElementBounds saveBounds = ElementBounds.FixedSize(0, 0).FixedUnder(inputBounds, 10).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(8, 2).WithFixedAlignmentOffset(-GuiStyle.ElementToDialogPadding, 0);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            CairoFont font = CairoFont.TextInput().WithFontSize(18);

            SingleComposer = capi.Gui.CreateCompo("stargateaddressnamedialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, onTitleBarClosed)
                .BeginChildElements()
                    .AddTextInput(inputBounds, (s) => { }, font, "nameinput")
                    .AddSmallButton(Lang.Get("Cancel"), onButtonCancel, cancelBounds)
                    .AddSmallButton(Lang.Get("Save"), onButtonSave, saveBounds)
                .EndChildElements()
                .Compose();

            nameInput = SingleComposer.GetTextInput("nameinput");
            nameInput.OnTryTextChangeText = onTextChanged;
            nameInput.LoadValue(new List<string> { text });
            nameInput.SetCaretPos(text.Length);
        }

        private bool onTextChanged(List<string> sl)
        {
            if (sl.Count == 0) return false;
            string newText = sl[0];

            if (newText.Length > maxNameLength)
            {
                newText = newText.Substring(0, maxNameLength);
            }

            sl.Clear();
            sl.Add(newText);
            addressName = newText;

            return true;
        }

        private void onSave(string text)
        {
            if (forSlot.Itemstack == null) return;

            forSlot.Itemstack.Attributes.SetString("addresscustomname", text);

            var syncSystem = capi.ModLoader.GetModSystem<InventoryAttributeSyncSystem>();
            IPlayer player = capi.World.Player;
            TreeAttribute attr = new TreeAttribute();
            attr["addresscustomname"] = forSlot.Itemstack.Attributes["addresscustomname"];
            syncSystem.ModifyAttribute(player, forSlot, attr);
        }

        private bool onButtonCancel()
        {
            TryClose();
            return true;
        }

        private bool onButtonSave()
        {
            onSave(addressName);

            TryClose();

            return true;
        }

        private void onTitleBarClosed()
        {
            onButtonCancel();
        }
    }
}
