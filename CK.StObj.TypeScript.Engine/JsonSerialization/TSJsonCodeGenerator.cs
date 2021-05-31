using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation ofJSon serialization.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>.
    /// </remarks>
    public partial class TSJsonCodeGenerator : ITSCodeGenerator
    {
        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                  ITSTypeFileBuilder builder,
                                                  TypeScriptAttribute attr )
        {

            return true;
        }

        /// <summary>
        /// Does nothing (it is the <see cref="ConfigureTypeScriptAttribute"/> method that sets a <see cref="ITSTypeFileBuilder.Finalizer"/>). 
        /// </summary>
        /// <param name="monitor">Unused.</param>
        /// <param name="context">Unused.</param>
        /// <returns>Always true.</returns>
        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;



    }
}
