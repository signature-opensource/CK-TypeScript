using CK.Core;
using System;
using System.Text;

namespace CK.Setup;

public sealed partial class TypeScriptContext
{
    sealed class LessVariablesFileInstallHook : ITransformableFileInstallHook
    {
        StringBuilder _stableVariables;
        StringBuilder _localVariables;

        public LessVariablesFileInstallHook()
        {
            _stableVariables = new StringBuilder( """
                // This file contains the lifted content of all the "variables.less" resources
                // (following their topological order) for the stable packages.
                """ );
            _localVariables = new StringBuilder( """"
                """
                // This file contains the lifted content of all the "variables.less" resources
                // (following their topological order) for the local packages.
                """
                """" );
        }

        public void StartInstall( IActivityMonitor monitor )
        {
        }

        public bool HandleInstall( IActivityMonitor monitor, TransformInstallableItem item, string finalText, IResourceSpaceItemInstaller installer )
        {
            Throw.DebugAssert( "variables.less".Length == 14 );
            var fName = item.TargetPath.LastPart;
            if( fName.EndsWith( "variables.less" )
                && (fName.Length == 14 || fName[^14] == '.') )
            {
                Capture( item, finalText, item.IsLocalItem ? _localVariables : _stableVariables );
                return true;
            }
            return false;
        }

        static void Capture( TransformInstallableItem item, string finalText, StringBuilder b )
        {
            b.Clear();
            b.Append( "// " ).Append( item.TargetPath ).AppendLine()
             .Append( finalText ).AppendLine();
        }

        public void StopInstall( IActivityMonitor monitor, IResourceSpaceItemInstaller installer )
        {
            installer.Write( "ck-gen/styles/stable.variables.less", _stableVariables.ToString() );
            installer.Write( "ck-gen/styles/local.variables.less", _localVariables.ToString() );
        }
    }
}
