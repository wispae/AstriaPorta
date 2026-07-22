using AstriaPorta.Content;
using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

#nullable enable

namespace AstriaPorta.Gui;

public class KinoRemoteState
{
    public string LocalGateAddress = string.Empty;
    public string RemoteGateAddress = string.Empty;
    public string InputGateAddress = string.Empty;
    public string LocalStatusString = string.Empty;
    public EnumStargateState LocalStatus = EnumStargateState.Idle;
    public int CurrentAddressBookIndex = 0;

    public LabeledAddress[] Addresses = new LabeledAddress[10];
    public List<TabNavigation> NavigationStack = [
        new() {
            TabIndex = 0,
            ElementId = 0
        }
    ];

    public int CurrentTabIndex => NavigationStack[NavigationStack.Count - 1].TabIndex;
}

public class TabNavigation
{
    public int TabIndex;
    public int ElementId;
}

public struct LabeledAddress
{
    public string Label = string.Empty;
    public string Address = string.Empty;

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(Label))
                return Label;

            return Lang.Get("astriaporta:gui-kino-label-empty");
        }
    }

    public string GetDisplayLabel(int index)
    {
        if (!string.IsNullOrEmpty(Label))
            return Label;

        return Lang.Get("astriaporta:gui-kino-label-empty-index", index + 1);
    }

    public LabeledAddress() { }
}

public class GuiKinoRemote : GuiDialogGeneric
{
    public GuiKinoRemote(ICoreClientAPI capi, ItemKinoRemote kinoRemote) : base("kinointerface", capi)
    {
        _kinoItem = kinoRemote;
        _state = new();

        Compose();
    }

    private bool _addressBookTextDirty = false;
    private string _detectedGateAddress = Lang.Get("astriaporta:gui-kino-no-gate-detected");
    private bool _dialButtonDirty = false;
    private bool _editingAddress = false;
    private string _gateStatus = StateToText(EnumStargateState.Idle);
    private ItemKinoRemote _kinoItem;
    private bool _setCaretToEnd = false;
    private KinoRemoteState _state;

    private const int _height = 200;
    private const int _padding = 10;
    private const int _width = _height * 2;

    public LoadedTexture? GuiTexture;

    public KinoRemoteState State => _state;

    public event Action<LoadedTexture?>? TextureIdUpdated;

    private string TopAddressDisplay => _state.CurrentAddressBookIndex == 0 ? _state.Addresses[_state.Addresses.Length - 1].GetDisplayLabel(_state.Addresses.Length - 1) : _state.Addresses[_state.CurrentAddressBookIndex - 1].GetDisplayLabel(_state.CurrentAddressBookIndex - 1);
    private string MiddleAddressDisplay => _state.Addresses[_state.CurrentAddressBookIndex].GetDisplayLabel(_state.CurrentAddressBookIndex);
    private string BottomAddressDisplay => _state.CurrentAddressBookIndex == _state.Addresses.Length - 1 ? _state.Addresses[0].GetDisplayLabel(0) : _state.Addresses[_state.CurrentAddressBookIndex + 1].GetDisplayLabel(_state.CurrentAddressBookIndex + 1);

    private LabeledAddress TopAddress => _state.CurrentAddressBookIndex == 0 ? _state.Addresses[_state.Addresses.Length - 1] : _state.Addresses[_state.CurrentAddressBookIndex - 1];
    private LabeledAddress MiddleAddress => _state.Addresses[_state.CurrentAddressBookIndex];
    private LabeledAddress BottomAddress => _state.CurrentAddressBookIndex == _state.Addresses.Length - 1 ? _state.Addresses[0] : _state.Addresses[_state.CurrentAddressBookIndex + 1];

    public override bool PrefersUngrabbedMouse => true;

    public override double DrawOrder => 1.0;

    public override EnumDialogType DialogType => EnumDialogType.HUD;

    public void Compose()
    {

        var dialogBounds = ElementBounds.FixedSize(_width, _height);
        if (SingleComposer == null)
        {
            SingleComposer = capi.Gui.CreateCompo("kinointerfacedialog", dialogBounds);
            SingleComposer.OnComposed += OnComposed;
        }
        else
        {
            SingleComposer.Clear(dialogBounds);
        }

        var bgBounds = ElementBounds.FixedSize(_width, _height);

        SingleComposer.AddShadedDialogBG(bgBounds, false)
            .BeginChildElements(bgBounds);

        switch (_state.CurrentTabIndex)
        {
            case 0:
                ComposeMainMenu(SingleComposer);
                break;
            case 1:
                ComposeAddressBook(SingleComposer);
                break;
            default:
                ComposeMainMenu(SingleComposer);
                break;
        }

        SingleComposer.EndChildElements();

        SingleComposer.Compose();

        SingleComposer.FocusElement(_state.NavigationStack[_state.NavigationStack.Count - 1].ElementId);

        if (_state.CurrentTabIndex == 1 && !_editingAddress)
            SingleComposer.UnfocusOwnElements();
    }

    private void ComposeAddressBook(GuiComposer composer)
    {
        var x = 2;
        var a = 3;
        var b = 2;

        var itemHeight = (_height - (2 + 2 * a) * _padding) / 3;
        var itemWidth = (_width - (1 + b + x) * _padding) / 2;

        var topAddressBounds = ElementBounds.Fixed(_padding, 2 * _padding, itemWidth, itemHeight).WithAlignment(EnumDialogArea.FixedTop);
        var middleAddressBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(topAddressBounds, a * _padding).WithFixedX(3 * _padding);
        var bottomAddressBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(middleAddressBounds, a * _padding).WithFixedX(_padding);

        itemHeight = (_height - (2 + 2 * a) * _padding) / 4;
        var xOffset = (b + x) * _padding + itemWidth;
        var addressInputBounds = ElementBounds.Fixed(xOffset, _padding, itemWidth, itemHeight).WithAlignment(EnumDialogArea.FixedTop);
        var descriptionInputBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(addressInputBounds, _padding).WithFixedX(xOffset);
        var dialButtonBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(descriptionInputBounds, _padding).WithFixedX(xOffset);
        var copyButtonBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(dialButtonBounds, _padding).WithFixedX(xOffset);

        composer.AddDynamicText(TopAddressDisplay, CairoFont.WhiteSmallishText().WithColor(GuiStyle.DisabledTextColor), topAddressBounds, "addressmenu.topaddress")
                .AddDynamicText(MiddleAddressDisplay, CairoFont.WhiteSmallishText().WithColor(GuiStyle.ActiveButtonTextColor), middleAddressBounds, "addressmenu.middleaddress")
                .AddDynamicText(BottomAddressDisplay, CairoFont.WhiteSmallishText().WithColor(GuiStyle.DisabledTextColor), bottomAddressBounds, "addressmenu.bottomaddress")
                .AddPhysicalTextInput(addressInputBounds, (string _) => { }, CairoFont.TextInput(), "addressmenu.addressinput")
                .AddPhysicalTextInput(descriptionInputBounds, (string _) => { }, CairoFont.TextInput(), "addressmenu.descriptioninput")
                .AddButton(Lang.Get("astriaporta:gui-kino-button-dial"), OnClickDialAddressBook, dialButtonBounds, EnumButtonStyle.Normal, "addressmenu.dialbutton")
                .AddButton(Lang.Get("astriaporta:gui-kino-button-copy"), OnClickCopy, copyButtonBounds, EnumButtonStyle.Normal, "addressmenu.copybutton");

        var addressInput = composer.GetPhysicalTextInput("addressmenu.addressinput");
        var descriptionInput = composer.GetPhysicalTextInput("addressmenu.descriptioninput");

        addressInput.SetPlaceHolderText(Lang.Get("astriaporta:gui-kino-placeholder-address"));
        if (!string.IsNullOrEmpty(_state.Addresses[_state.CurrentAddressBookIndex].Address))
            addressInput.SetValue(_state.Addresses[_state.CurrentAddressBookIndex].Address);
        addressInput.OnTryTextChangeText = OnTryChangeAddressBookText;
        addressInput.OnTextChanged = OnAddressBookTextChanged;

        descriptionInput.SetPlaceHolderText(Lang.Get("astriaporta:gui-kino-placeholder-description"));
        if (!string.IsNullOrEmpty(_state.Addresses[_state.CurrentAddressBookIndex].Label))
            descriptionInput.SetValue(_state.Addresses[_state.CurrentAddressBookIndex].Label);
        descriptionInput.OnTryTextChangeText = OnTryChangeAddressBookLabel;

        addressInput.TabIndex = 0;
        descriptionInput.TabIndex = 1;
        composer.GetButton("addressmenu.dialbutton").TabIndex = 2;
        composer.GetButton("addressmenu.copybutton").TabIndex = 3;

        _addressBookTextDirty = true;
    }

    private void ComposeMainMenu(GuiComposer composer)
    {
        var itemHeight = (_height - 4 * _padding) / 4;
        var itemWidth = (_width - 3 * _padding) / 2;

        var gateInputBounds = ElementBounds.Fixed(_padding, _padding, _width - 2 * _padding, itemHeight).WithAlignment(EnumDialogArea.FixedTop);

        var dialButtonBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(gateInputBounds, _padding).WithFixedX(_padding);
        var addressBookButtonBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(dialButtonBounds, _padding / 2).WithFixedX(_padding);
        var statusButtonBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(addressBookButtonBounds, _padding / 2).WithFixedX(_padding);

        var xOffset = 2 * _padding + itemWidth;
        var localAddressBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(gateInputBounds, _padding).WithFixedX(xOffset);
        var gateStatusBounds = ElementBounds.FixedSize(itemWidth, itemHeight).FixedUnder(localAddressBounds, _padding).WithFixedX(xOffset);

        var showDisconnect = _state.LocalStatus != EnumStargateState.Idle;
        composer.AddPhysicalTextInput(gateInputBounds, (string _) => { }, CairoFont.TextInput(), "mainmenu.gateinputaddress")
                .AddButton(showDisconnect ? Lang.Get("astriaporta:gui-kino-button-disconnect") : Lang.Get("astriaporta:gui-kino-button-dial"), OnClickConnect, dialButtonBounds, EnumButtonStyle.Normal, "mainmenu.dialbutton")
                .AddButton(Lang.Get("astriaporta:gui-kino-button-addresses"), OnClickAddressBook, addressBookButtonBounds, EnumButtonStyle.Normal, "mainmenu.addressbookbutton")
                .AddButton(Lang.Get("astriaporta:gui-kino-button-status"), () => { return true; }, statusButtonBounds, EnumButtonStyle.Normal, "mainmenu.statusbutton")
                .AddDynamicText(_detectedGateAddress, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), localAddressBounds, "mainmenu.localaddress")
                .AddDynamicText(_gateStatus, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), gateStatusBounds, "mainmenu.gatestatus");

        var gateAddressInput = composer.GetPhysicalTextInput("mainmenu.gateinputaddress");
        gateAddressInput.SetValue(_state.InputGateAddress);
        gateAddressInput.Enabled = _state.LocalStatus == EnumStargateState.Idle;
        gateAddressInput.OnTryTextChangeText = OnTryChangeAddressText;
        gateAddressInput.OnTextChanged = OnAddressTextChanged;

        var statusButton = composer.GetButton("mainmenu.statusbutton");
        statusButton.Enabled = false;

        gateAddressInput.TabIndex = 3;
        composer.GetButton("mainmenu.dialbutton").TabIndex = 0;
        composer.GetButton("mainmenu.addressbookbutton").TabIndex = 1;
        statusButton.TabIndex = 2;

    }

    private void CreateGuiTexture()
    {
        var staticTexture = SingleComposer.GetStaticTexture();

        RawTexture rawTexture = new()
        {
            Height = staticTexture.Height,
            Width = staticTexture.Width,
            PixelFormat = EnumTexturePixelFormat.Rgba
        };

        capi.Render.GenTexture(rawTexture);

        GuiTexture?.Dispose();
        GuiTexture = new(capi, rawTexture.TextureId, rawTexture.Width, rawTexture.Height);

        TextureIdUpdated?.Invoke(GuiTexture);
    }

    private void HandleKeyDownAddressBook(KeyEvent args)
    {
        var key = (GlKeys)args.KeyCode;
        if (key == GlKeys.Escape)
        {
            if (_editingAddress)
            {
                _addressBookTextDirty = true;
                _editingAddress = false;
                SingleComposer.UnfocusOwnElements();
                args.Handled = true;
            }
            else
            {
                PopNavigation();
                args.Handled = true;
            }
        }
        else if (key == GlKeys.Enter || key == GlKeys.KeypadEnter)
        {
            if (!_editingAddress)
            {
                _editingAddress = true;
                SingleComposer.FocusElement(0);
                args.Handled = true;
            }
        }
    }

    private void HandleKeyDownMainMenu(KeyEvent args)
    {
        return;
    }

    private bool OnClickAddressBook()
    {
        PushNavigation(1, SingleComposer.CurrentTabIndexElement.TabIndex);
        Recompose();

        return true;
    }

    private bool OnClickConnect()
    {
        if (_state.LocalStatus != EnumStargateState.Idle)
        {
            _kinoItem.TryCancelDial();
            return true;
        }

        string s = AddressUtils.SanitizeAddressString(_state.InputGateAddress);
        if (!AddressUtils.IsValidAddressString(s))
            return false;

        StargateAddress a = new StargateAddress();
        a.FromGlyphs(AddressUtils.StringAddressToBytes(s), capi);
        _kinoItem.TryDial(a);

        return true;
    }

    private bool OnClickCopy()
    {
        // todo: verify it's a valid stargate address before doing this
        var cleanAddress = _detectedGateAddress.Replace("-", string.Empty).Replace(" ", string.Empty);
        SingleComposer.GetPhysicalTextInput("addressmenu.addressinput").SetValue(cleanAddress);

        return true;
    }

    private bool OnClickDialAddressBook()
    {
        string s = AddressUtils.SanitizeAddressString(_state.Addresses[_state.CurrentAddressBookIndex].Address);

        if (!AddressUtils.IsValidAddressString(s))
            return false;

        _state.InputGateAddress = AddressUtils.FormatAddressString(s);

        StargateAddress a = new StargateAddress();
        a.FromGlyphs(AddressUtils.StringAddressToBytes(s), capi);
        _kinoItem.TryDial(a);

        PopNavigation();

        return true;
    }

    private void OnComposed()
    {
        // check if the dimensions of the static texture ID have changed / are different from our current GUI texture
        // then enqueue disposal / creation of a new texture to the main thread
        var staticTexture = SingleComposer.GetStaticTexture();
        if (GuiTexture == null || GuiTexture.Width != staticTexture.Width || GuiTexture.Height != staticTexture.Height)
        {
            var clientMain = capi.World as ClientMain;
            clientMain?.EnqueueMainThreadTask(CreateGuiTexture, "GuiKinoRemote.CreateGuiTexture");
        }
    }

    public override void OnKeyDown(KeyEvent args)
    {
        if ((GlKeys)args.KeyCode != GlKeys.Enter || SingleComposer.CurrentTabIndexElement is not GuiElementTextInput)
            base.OnKeyDown(args);

        if (args.Handled)
            return;

        switch (_state.CurrentTabIndex)
        {
            case 0:
                HandleKeyDownMainMenu(args);
                break;
            case 1:
                HandleKeyDownAddressBook(args);
                break;
        }
    }

    public override void OnMouseDown(MouseEvent args)
    {
        // Left-click emulates activating an element via the "Enter" key
        if (args.Button == EnumMouseButton.Left)
        {
            KeyEvent keyArgs = new()
            {
                KeyCode = 49,
            };

            OnKeyDown(keyArgs);
        }
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        if (args.delta == 0)
            return;

        if (_state.CurrentTabIndex == 1 && !_editingAddress)
        {
            ScrollAddress(args);
            args.SetHandled(true);
            return;
        }

        // use scrollwheel to navigate between elements
        // TODO: loop scroll
        KeyEvent keyArgs = new()
        {
            KeyCode = (int)GlKeys.Tab,
            ShiftPressed = args.delta > 0
        };

        base.OnKeyDown(keyArgs);
        args.SetHandled(true);
    }

    // This will run on the main thread, guaranteed (rendering)
    public override void OnRenderGUI(float deltaTime)
    {
        // debug render the GUI to main framebuffer as usual
        // base.OnRenderGUI(deltaTime);

        if (_addressBookTextDirty)
        {
            if (_state.CurrentTabIndex == 1)
            {
                SingleComposer.GetDynamicText("addressmenu.topaddress").RecomposeText();
                SingleComposer.GetDynamicText("addressmenu.middleaddress").RecomposeText();
                SingleComposer.GetDynamicText("addressmenu.bottomaddress").RecomposeText();
            }

            _addressBookTextDirty = false;
        }

        RenderGuiToTexture(deltaTime);
    }

    private bool OnTryChangeAddressText(List<string> sl)
    {
        if (sl.Count == 0) return false;

        var input = SingleComposer.GetTextInput("mainmenu.gateinputaddress");

        string s = sl[0];

        s = AddressUtils.FormatAddressString(s);
        
        int lengthDelta = s.Length - input.Text.Length;
        input.Text = s;
        var caretPos = input.CaretPosInLine;

        if (lengthDelta > 1)
        {
            _setCaretToEnd = true;
        }

        sl.Clear();
        sl.Add(s);

        return true;
    }

    private void OnAddressTextChanged(string s)
    {
        if (!_setCaretToEnd)
            return;

        SingleComposer.GetTextInput("mainmenu.gateinputaddress").SetCaretPos(s.Length);
        _setCaretToEnd = false;
    }

    private bool OnTryChangeAddressBookLabel(List<string> sl)
    {
        if (sl.Count == 0) return false;

        _state.Addresses[_state.CurrentAddressBookIndex].Label = sl[0];
        SingleComposer.GetDynamicText("addressmenu.middleaddress").Text = MiddleAddressDisplay;
        _addressBookTextDirty = true;
        _kinoItem.OnGuiStateUpdated(_state);

        return true;
    }

    private bool OnTryChangeAddressBookText(List<string> sl)
    {
        if (sl.Count == 0) return false;

        var input = SingleComposer.GetTextInput("addressmenu.addressinput");

        string s = sl[0];
        s = AddressUtils.FormatAddressString(s);

        int lengthDelta = s.Length - input.Text.Length;
        input.Text = s;

        var caretPos = input.CaretPosInLine;

        if (lengthDelta > 1)
        {
            _setCaretToEnd = true;
        }

        sl.Clear();
        sl.Add(s);

        _state.Addresses[_state.CurrentAddressBookIndex].Address = s;
        _kinoItem.OnGuiStateUpdated(_state);

        return true;
    }

    private void OnAddressBookTextChanged(string s)
    {
        if (!_setCaretToEnd)
            return;

        SingleComposer.GetTextInput("addressmenu.addressinput").SetCaretPos(s.Length);
        _setCaretToEnd = false;
    }

    /// <summary>
    /// Returns to the previous navigation entry
    /// </summary>
    /// <returns>The current navigation entry, or null if at the bottom of the stack</returns>
    public TabNavigation? PopNavigation(bool recompose = true)
    {
        if (_state.NavigationStack.Count == 1)
            return null;

        _state.NavigationStack.PopLast();
        _kinoItem.OnGuiTabChanged(_state.NavigationStack[_state.NavigationStack.Count - 1].TabIndex);

        if (recompose)
            Recompose();

        return _state.NavigationStack[_state.NavigationStack.Count - 1];
    }

    /// <summary>
    /// Adds a tabindex and the element that pushed the navigation to the navigation stack
    /// </summary>
    /// <param name="tabIndex"></param>
    /// <param name="elementTabIndex"></param>
    public void PushNavigation(int tabIndex, int elementTabIndex)
    {
        _state.NavigationStack[_state.NavigationStack.Count - 1].ElementId = elementTabIndex;

        _state.NavigationStack.Add(new()
        {
            TabIndex = tabIndex,
            ElementId = -1
        });

        _kinoItem.OnGuiTabChanged(tabIndex);
    }

    public override void Recompose()
    {
        Compose();
    }

    private bool RenderGuiToTexture(float deltaTime)
    {
        if (GuiTexture == null) return false;

        var clientMain = capi.World as ClientMain;
        if (clientMain == null) return false;

        var platform = clientMain.Platform;

        clientMain.GlPushMatrix();
        float clientWidth = clientMain.Width;
        float clientHeight = clientMain.Height;

        clientMain.GlScale(clientWidth / GuiTexture.Width, -clientHeight / GuiTexture.Height, 1f);
        clientMain.GlTranslate(0f, -GuiTexture.Height, 0f);

        var guiFrameBuffer = capi.Render.CreateFrameBuffer(GuiTexture);
        var originalFrameBuffer = capi.Render.CurrentFrameBuffer;
        capi.Render.CurrentFrameBuffer = guiFrameBuffer;

        base.OnRenderGUI(deltaTime);

        clientMain.GlPopMatrix();
        capi.Render.CurrentFrameBuffer = originalFrameBuffer;
        capi.Render.DestroyFrameBuffer(guiFrameBuffer);

        return true;
    }

    private void ScrollAddress(MouseWheelEventArgs args)
    {
        _state.CurrentAddressBookIndex += (args.delta > 0 ? -1 : 1);
        if (_state.CurrentAddressBookIndex < 0)
            _state.CurrentAddressBookIndex = _state.Addresses.Length - 1;
        else if (_state.CurrentAddressBookIndex >= _state.Addresses.Length)
            _state.CurrentAddressBookIndex = 0;

        SingleComposer.GetDynamicText("addressmenu.topaddress").Text = TopAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.middleaddress").Text = MiddleAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.bottomaddress").Text = BottomAddressDisplay;

        SingleComposer.GetTextInput("addressmenu.addressinput").SetValue(MiddleAddress.Address);
        SingleComposer.GetTextInput("addressmenu.descriptioninput").SetValue(MiddleAddress.Label);

        _addressBookTextDirty = true;
    }

    public override bool ShouldReceiveMouseEvents()
    {
        return false;
    }

    public void StateChanged()
    {
        _addressBookTextDirty = true;
    }

    private static string StateToText(EnumStargateState state) => state switch
    {
        EnumStargateState.Idle => Lang.Get("astriaporta:gui-kino-gate-status-idle"),
        EnumStargateState.DialingIncoming => Lang.Get("astriaporta:gui-kino-gate-status-dialing"),
        EnumStargateState.DialingOutgoing => Lang.Get("astriaporta:gui-kino-gate-status-dialing"),
        EnumStargateState.ConnectedIncoming => Lang.Get("astriaporta:gui-kino-gate-status-connected"),
        EnumStargateState.ConnectedOutgoing => Lang.Get("astriaporta:gui-kino-gate-status-connected"),
        _ => Lang.Get("astriaporta:gui-kino-gate-state-idle")
    };

    public override bool TryOpen()
    {
        Focus();
        return base.TryOpen();
    }

    public void UpdateActiveAddressIndex(int index)
    {
        _state.CurrentAddressBookIndex = index;
        if (_state.CurrentTabIndex != 1)
            return;

        SingleComposer.GetDynamicText("addressmenu.topaddress").Text = TopAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.middleaddress").Text = MiddleAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.bottomaddress").Text = BottomAddressDisplay;

        SingleComposer.GetTextInput("addressmenu.addressinput").SetValue(MiddleAddress.Address);
        SingleComposer.GetTextInput("addressmenu.descriptioninput").SetValue(MiddleAddress.Label);

        _addressBookTextDirty = true;
    }

    public void UpdateAddressBook(ITreeAttribute? addressAttributeTree)
    {
        for (int i = 0; i < _state.Addresses.Length; i++)
        {
            _state.Addresses[i].Label = addressAttributeTree?.GetString($"l{i}") ?? string.Empty;
            _state.Addresses[i].Address = addressAttributeTree?.GetString($"a{i}") ?? string.Empty;
        }

        if (_state.CurrentTabIndex != 1)
            return;

        SingleComposer.GetDynamicText("addressmenu.topaddress").Text = TopAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.middleaddress").Text = MiddleAddressDisplay;
        SingleComposer.GetDynamicText("addressmenu.bottomaddress").Text = BottomAddressDisplay;

        SingleComposer.GetTextInput("addressmenu.addressinput").SetValue(MiddleAddress.Address);
        SingleComposer.GetTextInput("addressmenu.descriptioninput").SetValue(MiddleAddress.Label);

        _addressBookTextDirty = true;
    }

    public void UpdateGateAddress(string displayAddress)
    {
        _detectedGateAddress = displayAddress;
        if (_state.CurrentTabIndex != 0)
            return;
        var gateAddressElement = SingleComposer.GetDynamicText("mainmenu.localaddress");
        _state.LocalGateAddress = displayAddress;
        gateAddressElement.Text = displayAddress;
        gateAddressElement.RecomposeText(true);
    }

    public void UpdateLocalGateState(EnumStargateState state, string remoteAddress)
    {
        var stateText = StateToText(state);
        _gateStatus = stateText;

        if (_state.CurrentTabIndex == 0)
        {
            var gateStateElement = SingleComposer.GetDynamicText("mainmenu.gatestatus");
            gateStateElement.Text = stateText;
            gateStateElement.RecomposeText(true);

            var addressInputElement = SingleComposer.GetPhysicalTextInput("mainmenu.gateinputaddress");
            addressInputElement.Enabled = state == EnumStargateState.Idle;
            if (state != EnumStargateState.Idle)
            {
                _state.RemoteGateAddress = remoteAddress;
                addressInputElement.Text = remoteAddress;
                addressInputElement.SetValue(remoteAddress);
            }
            else
            {
                _state.RemoteGateAddress = string.Empty;
            }

            var dialButton = SingleComposer.GetButton("mainmenu.dialbutton");
            dialButton.Enabled = state != EnumStargateState.DialingIncoming;
            if (state == EnumStargateState.Idle)
            {
                dialButton.Text = Lang.Get("astriaporta:gui-kino-button-connect");
            }
            else
            {
                dialButton.Text = Lang.Get("astriaporta:gui-kino-button-disconnect");
            }
        }

        if (_state.LocalStatus != state)
        {
            _state.LocalStatus = state;
            Recompose();
        }
    }
}
