using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// TypeScript builder for a type. See <see cref="TSTypeFile"/> that implements its.
    /// <para>
    /// This is provided to the ConfigureTypeScriptAttribute method of all global <see cref="ITSCodeGenerator"/>
    /// and to the <see cref="ITSCodeGeneratorType"/> bound to the type for any type that must be generated.
    /// </para>
    /// <para>
    /// Note that the actual <see cref="CK.TypeScript.CodeGen.TypeScriptFile"/> that will define this type may contain
    /// other types.
    /// </para>
    /// </summary>
    public interface ITSTypeFileBuilder
    {
        /// <summary>
        /// Gets the central <see cref="TypeScriptContext"/>.
        /// </summary>
        TypeScriptContext Context { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> for which a TypeScript file must be generated.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets whether this type should not be generated.
        /// This is false by default and can only transition to true.
        /// Setting CancelGeneration to true is a strong action: the type can no more have an associated <see cref="TSTypeFile"/>
        /// and <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type)"/> will throw when called with the type that
        /// has been canceled.
        /// </summary>
        bool CancelGeneration { get; set; }

        /// <summary>
        /// Gets a mutable list of the generators bound to this <see cref="Type"/>.
        /// </summary>
        IList<ITSCodeGeneratorType> Generators { get; }

        /// <summary>
        /// Gets or sets a finalizer function that will be called after the <see cref="Generators"/>.
        /// To compose a function with this one, use the <see cref="AddFinalizer(Func{IActivityMonitor, TSTypeFile, bool}, bool)"/> helper.
        /// </summary>
        Func<IActivityMonitor, TSTypeFile, bool>? Finalizer { get; set; }

        /// <summary>
        /// Combines a function with the current <see cref="Finalizer"/>.
        /// </summary>
        /// <param name="newFinalizer">The finalizer to call before or after the current one.</param>
        /// <param name="prepend">
        /// True to first call <paramref name="newFinalizer"/> before <see cref="Finalizer"/>, false by default (if <see cref="Finalizer"/>
        /// is not null, it is called before <paramref name="newFinalizer"/>).
        /// </param>
        void AddFinalizer( Func<IActivityMonitor, TSTypeFile, bool> newFinalizer, bool prepend = false );
    }
}
