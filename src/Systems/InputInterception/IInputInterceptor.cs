using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AstriaPorta.Systems;

public interface IInputInterceptor
{
    void HandleKeyInput(ICoreClientAPI capi, ItemStack stack, KeyEvent e);

    void HandleScrollInput(ICoreClientAPI capi, ItemStack stack, MouseWheelEventArgs e);
}
