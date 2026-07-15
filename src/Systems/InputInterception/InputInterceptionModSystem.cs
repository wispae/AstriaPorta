using AstriaPorta.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AstriaPorta.Systems;
#nullable enable

public class InputInterceptionModSystem : ModSystem
{
    private ICoreClientAPI _capi = null!;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override double ExecuteOrder()
    {
        return 0.37d;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _capi = api;
        api.Event.KeyDown += OnKeyDown;
        api.Event.MouseWheelMove += OnScrollEvent;
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_capi != null)
        {
            _capi.Event.KeyDown -= OnKeyDown;
            _capi.Event.MouseWheelMove -= OnScrollEvent;
        }
    }

    private void OnKeyDown(KeyEvent e)
    {
        var interceptorStack = GetHeldInterceptor();
        if (interceptorStack.Interceptor == null)
            return;

        interceptorStack.Interceptor.HandleKeyInput(_capi, interceptorStack.Stack, e);
    }

    private void OnScrollEvent(MouseWheelEventArgs e)
    {
        var interceptorStack = GetHeldInterceptor();
        if (interceptorStack.Interceptor == null)
            return;

        interceptorStack.Interceptor.HandleScrollInput(_capi, interceptorStack.Stack, e);
    }

    private InterceptorStack GetHeldInterceptor()
    {
        InterceptorStack stack = new();

        var player = _capi.World.Player;
        var hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot == null)
            return stack;

        stack.Stack = hotbarSlot.Itemstack;
        stack.Interceptor = hotbarSlot.Itemstack?.Collectible as IInputInterceptor;

        return stack;
    }

    private struct InterceptorStack
    {
        public ItemStack? Stack;
        public IInputInterceptor? Interceptor;
    }
}
