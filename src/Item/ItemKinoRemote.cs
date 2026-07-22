using AstriaPorta.Gui;
using AstriaPorta.Util;
using AstriaPorta.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

#nullable enable

namespace AstriaPorta.Content;

public class ItemKinoRemote : Item, IInputInterceptor
{
    private ICoreClientAPI _capi = null!;
    private string _closestGateText = string.Empty;
    private MeshData? _displayMesh = null;
    private MultiTextureMeshRef? _displayMeshRef = null;
    private Cuboidi? _gateSearchArea = null;
    private bool _guiStale = false;
    private GuiKinoRemote? _kinoGui = null;
    private long _lastCheckTime = 0;
    private DynamicTextureSource? _kinoTextureSource = null;
    private bool _meshIsStale = true;

    private WorldInteraction[] _interactions = [
        new() {
            ActionLangCode = "astriaporta:heldhelp-kino",
            MouseButton = EnumMouseButton.None
        },
    ];

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        if (inSlot.Itemstack == null || !inSlot.Itemstack.TempAttributes.GetAsBool("gui_active") || _kinoGui == null)
            return base.GetHeldInteractionHelp(inSlot);

        return base.GetHeldInteractionHelp(inSlot).Append(_interactions);
    }

    public void HandleKeyInput(ICoreClientAPI capi, ItemStack stack, KeyEvent e)
    {
        if (!stack.TempAttributes.GetAsBool("gui_active") || _kinoGui == null)
            return;

        if (Enum.IsDefined(typeof(GlKeys), e.KeyCode))
        {
            // forward to UI first, check if that handled it or not
            _kinoGui.OnKeyDown(e);

            if (!e.Handled)
            {
                var key = (GlKeys)e.KeyCode;
                if (key == GlKeys.Escape)
                {
                    e.Handled = true;
                    _kinoGui?.TryClose();
                    return;
                }
            }
        }

        return;
    }

    public void HandleScrollInput(ICoreClientAPI capi, ItemStack stack, MouseWheelEventArgs e)
    {
        if (!stack.TempAttributes.GetAsBool("gui_active") || _kinoGui == null)
            return;

        _kinoGui.OnMouseWheel(e);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

        if (target == EnumItemRenderTarget.Ground || target == EnumItemRenderTarget.Gui)
            return;

        if (!itemstack.TempAttributes.GetAsBool("gui_active"))
            return;

        if (_meshIsStale)
        {
            _displayMesh?.Dispose();
            if (_displayMeshRef != null)
                _displayMeshRef.Dispose();

            _displayMesh = CreateUIEnabledMesh(capi, itemstack);

            _displayMeshRef = capi.Render.UploadMultiTextureMesh(_displayMesh);
            _meshIsStale = false;
        }

        renderinfo.ModelRef = _displayMeshRef;
    }

    private void OnGuiClosed()
    {
        var activeSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        if (activeSlot == null) return;

        if (activeSlot.Itemstack?.Collectible == this)
        {
            activeSlot.Itemstack.TempAttributes.SetBool("gui_active", false);
        }
    }

    internal void OnGuiStateUpdated(KinoRemoteState state)
    {
        var heldStackSlot = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
        var heldStack = heldStackSlot?.Itemstack;
        if (heldStack == null || heldStack.Class != EnumItemClass.Item || heldStack.Item is not ItemKinoRemote)
            return;

        ITreeAttribute addressesTree;
        try
        {
            addressesTree = heldStack.Attributes.GetOrAddTreeAttribute("addresses");
        }
        catch
        {
            heldStack.Attributes.RemoveAttribute("addresses");
            addressesTree = heldStack.Attributes.GetOrAddTreeAttribute("addresses");
        }

        for (int i = 0; i < state.Addresses.Length; i++)
        {
            if (string.IsNullOrEmpty(state.Addresses[i].Label) || string.IsNullOrEmpty(state.Addresses[i].Address))
                continue;

            addressesTree.SetString($"l{i}", state.Addresses[i].Label);
            addressesTree.SetString($"a{i}", state.Addresses[i].Address);
        }

        api.ModLoader.GetModSystem<InventoryAttributeSyncSystem>().ModifyAttribute(_capi.World.Player, heldStackSlot, heldStack.Attributes);
    }

    public void OnGuiTabChanged(int tabIndex)
    {
        var capi = api as ICoreClientAPI;
        var activeSlot = capi?.World.Player.InventoryManager.ActiveHotbarSlot;
        if (activeSlot == null)
            return;

        var hotbar = capi.LoadedGuis.First(o => o is HudHotbar) as HudHotbar;
        if (hotbar == null)
            return;

        var wiUtil = GetHotbarDrawUtil(hotbar);
        wiUtil.ComposeBlockWorldInteractionHelp(GetHeldInteractionHelp(activeSlot));
        ref long activeMs = ref GetHotbarTextActiveMs(hotbar);
        // default is 3500ms, but I'd like help to show for 5000ms
        activeMs = capi.ElapsedMilliseconds + 1500;
    }

    internal void OnGuiTempStateUpdate(KinoRemoteState state)
    {
        var heldStack = _capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (heldStack == null || heldStack.Class != EnumItemClass.Item || heldStack.Item is not ItemKinoRemote)
            return;

        heldStack.TempAttributes.SetInt("addressindex", state.CurrentAddressBookIndex);
        heldStack.TempAttributes.SetInt("tabindex", state.CurrentTabIndex);
    }

    private void OnGuiTextureUpdated(LoadedTexture? texture)
    {
        if (_kinoTextureSource == null)
            return;

        _kinoTextureSource.UpdateTexture(texture);
        _meshIsStale = true;
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        if (slot.Itemstack?.TempAttributes.GetAsBool("gui_active") ?? false)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            var mouseEvent = new MouseEvent(0, 0, EnumMouseButton.Left, 0);
            _kinoGui?.OnMouseDown(mouseEvent);
        }
        else
        {
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);

        if (_capi == null)
            return;
        if (_capi.ElapsedMilliseconds - _lastCheckTime < 3000)
            return;
        _lastCheckTime = _capi.ElapsedMilliseconds;

        if (byEntity != _capi.World.Player.Entity)
            return;

        if (!(slot.Itemstack?.TempAttributes.GetAsBool("gui_active") ?? false))
            return;

        if (_kinoGui == null)
            return;

        var nearestGate = GateUtils.FindClosestGate(_capi.World.BlockAccessor, byEntity.Pos.AsBlockPos, _gateSearchArea);
        if (nearestGate == null)
        {
            _closestGateText = Lang.Get("astriaporta:gui-kino-no-gate-detected");
            _kinoGui?.UpdateGateAddress(_closestGateText);
            _kinoGui?.UpdateLocalGateState(EnumStargateState.Idle, string.Empty);
        }
        else
        {
            _closestGateText = nearestGate.Address.ToString();
            _kinoGui?.UpdateGateAddress(_closestGateText);
            _kinoGui?.UpdateLocalGateState(nearestGate.State, nearestGate.DialingAddress?.ToString());
        }
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        if (handling != EnumHandHandling.NotHandled)
            return;

        var stack = slot.Itemstack;
        if (stack == null)
            return;

        if (stack.TempAttributes.GetAsBool("gui_active"))
        {
            stack.TempAttributes.SetBool("gui_active", false);
            _kinoGui?.TryClose();
        }
        else
        {
            stack.TempAttributes.SetBool("gui_active", true);

            if (_kinoGui != null)
            {
                var addressesElement = stack.Attributes.GetTreeAttribute("addresses");
                _kinoGui.UpdateAddressBook(addressesElement);

                var closestGate = GateUtils.FindClosestGate(_capi.World.BlockAccessor, byEntity.Pos.AsBlockPos, _gateSearchArea);
                if (closestGate != null)
                {
                    _kinoGui.UpdateGateAddress(closestGate.Address.ToString() ?? string.Empty);
                    _kinoGui.UpdateLocalGateState(closestGate.State, closestGate.DialingAddress?.ToString() ?? string.Empty);
                    _closestGateText = closestGate?.Address.ToString() ?? string.Empty;
                } else
                {
                    _kinoGui.UpdateGateAddress(Lang.Get("astriaporta:gui-kino-no-gate-detected"));
                    _kinoGui.UpdateLocalGateState(EnumStargateState.Idle, string.Empty);
                    _closestGateText = Lang.Get("astriaporta:gui-kino-no-gate-detected");
                }
                UpdateStateFromAttributes(stack);
                _kinoGui.TryOpen();
                _guiStale = true;
                _lastCheckTime = _capi.ElapsedMilliseconds - 2990;

                OnGuiTabChanged(_kinoGui.State.CurrentTabIndex);
            }
        }

        handling = EnumHandHandling.PreventDefault;
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        _gateSearchArea = new Cuboidi(-10, -2, -10, 10, 3, 10);
        if (api.Side == EnumAppSide.Client)
        {
            _capi = api as ICoreClientAPI;
        }
    }

    public void TryCancelDial()
    {
        var closestGate = GateUtils.FindClosestGate(_capi.World.BlockAccessor, _capi.World.Player.Entity.Pos.AsBlockPos, _gateSearchArea);
        if (closestGate == null) return;

        closestGate.TryDisconnect();

        _closestGateText = closestGate.Address.ToString();
        _kinoGui?.UpdateGateAddress(_closestGateText);
        _kinoGui?.UpdateLocalGateState(closestGate.State, closestGate.DialingAddress?.ToString() ?? string.Empty);
    }

    public void TryDial(IStargateAddress address)
    {
        var closestGate = GateUtils.FindClosestGate(_capi.World.BlockAccessor, _capi.World.Player.Entity.Pos.AsBlockPos, _gateSearchArea);
        if (closestGate == null) return;

        var success = closestGate.TryDial(address);

        _closestGateText = closestGate.Address.ToString();
        _kinoGui?.UpdateGateAddress(_closestGateText);
        _kinoGui?.UpdateLocalGateState(success ? EnumStargateState.DialingOutgoing : closestGate.State, closestGate.DialingAddress?.ToString() ?? string.Empty);
    }

    private void UpdateStateFromAttributes(ItemStack kinoStack)
    {
        if (_kinoGui == null || kinoStack == null)
            return;

        ITreeAttribute addressesTree;
        try
        {
            addressesTree = kinoStack.Attributes.GetOrAddTreeAttribute("addresses");
        }
        catch
        {
            kinoStack.Attributes.RemoveAttribute("addresses");
            addressesTree = kinoStack.Attributes.GetOrAddTreeAttribute("addresses");
        }

        for (int i = 0; i < _kinoGui.State.Addresses.Length; i++)
        {
            _kinoGui.State.Addresses[i].Label = addressesTree.GetString($"l{i}", string.Empty);
            _kinoGui.State.Addresses[i].Address = addressesTree.GetString($"a{i}", string.Empty);
        }

        _kinoGui.StateChanged();
    }

    private MeshData CreateUIEnabledMesh(ICoreClientAPI capi, ItemStack? stack)
    {
        if (_kinoTextureSource == null)
        {
            _kinoTextureSource = new(capi);
        }

        if (_kinoGui == null)
        {
            _kinoGui = new GuiKinoRemote(capi, this);
            _kinoGui.TextureIdUpdated += OnGuiTextureUpdated;
            _kinoGui.OnClosed += OnGuiClosed;

            capi.Gui.RegisterDialog(_kinoGui);

            _kinoGui.UpdateGateAddress(_closestGateText);
            if (stack != null)
            {
                UpdateStateFromAttributes(stack);
                _kinoGui.TryOpen();
            }
        }

        // Assume that "gui" is in the IgnoredElements via the itemtype json
        var baseShape = Shape;

        string[] selectiveElements;

        var guiShape = Shape.CloneWithoutAlternates();
        if (guiShape.SelectiveElements != null)
        {
            selectiveElements = new string[guiShape.SelectiveElements.Length + 1];
            guiShape.SelectiveElements.CopyTo(selectiveElements, 0);
        }
        else
        {
            selectiveElements = new string[1];
        }

        if (guiShape.IgnoreElements.Contains("gui"))
        {
            guiShape.IgnoreElements = guiShape.IgnoreElements.Remove("gui");
        }

        selectiveElements[selectiveElements.Length - 1] = "kino_root/gui";
        guiShape.SelectiveElements = selectiveElements;

        MeshData baseMesh;
        capi.Tesselator.TesselateItem(this, baseShape, out baseMesh);

        MeshData uiMesh;
        capi.Tesselator.TesselateShape("astriaporta.gui", "astriaporta.codeinternal", guiShape, out uiMesh, _kinoTextureSource);

        baseMesh.AddMeshData(uiMesh);
        uiMesh.Dispose();

        return baseMesh;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "wiUtil")]
    private extern static ref DrawWorldInteractionUtil GetHotbarDrawUtil(HudHotbar hotbar);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "itemInfoTextActiveMs")]
    private extern static ref long GetHotbarTextActiveMs(HudHotbar hotbar);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RecomposeActiveSlotHoverText")]
    private extern static void RecomposeHelpText(HudHotbar hotbar, int newSlotIndex);
}
