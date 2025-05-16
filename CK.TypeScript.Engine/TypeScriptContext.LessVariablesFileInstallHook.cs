using CK.Core;
using System;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class LessVariablesFileInstallHook : ITransformableFileInstallHook
    {
        public void Install( IActivityMonitor monitor,
                             IInstallableItem item,
                             string finalText,
                             IResourceSpaceItemInstaller installer,
                             ITransformableFileInstallHook.NextHook next )
        {
            throw new NotImplementedException();
        }
    }
}
