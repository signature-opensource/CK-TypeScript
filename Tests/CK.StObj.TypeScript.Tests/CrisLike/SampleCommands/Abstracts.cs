using CK.Core;
using CK.CrisLike;
using System;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    /// <summary>
    /// Abstract command is a part with an object key and
    /// a list, a set and a dictionary of objects.
    /// <para>
    /// The properties here can only be "Abstract Read Only" properties, if they were
    /// "concrete properties", they would impose the type.
    /// </para>
    /// </summary>
    [TypeScript( Folder = "Cmd/Abstract" )]
    public interface ICommandAbs : IAbstractCommand
    {
        /// <summary>
        /// Gets an object key.
        /// </summary>
        object Key { get; }

        /// <summary>
        /// Gets a list of key as objects.
        /// </summary>
        IReadOnlyList<object> KeyList { get; }

        /// <summary>
        /// Gets a set of key as objects.
        /// </summary>
        IReadOnlySet<object> KeySet { get; }

        /// <summary>
        /// Gets a dictionary of key as objects.
        /// </summary>
        IReadOnlyDictionary<string,object> KeyDictionary { get; }
    }

    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with integers.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface IIntCommand: ICommandAbs
    {
        /// <summary>
        /// Gets or sets the integer key.
        /// </summary>
        new int Key { get; set; }

        /// <summary>
        /// Gets the mutable list of integer.
        /// </summary>
        new IList<int> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of integer.
        /// </summary>
        new ISet<int> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of integer.
        /// </summary>
        new IDictionary<string, int> KeyDictionary { get; }
    }

    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with strings.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface IStringCommand : ICommandAbs
    {
        /// <summary>
        /// Gets or sets the string key.
        /// </summary>
        new string Key { get; set; }

        /// <summary>
        /// Gets the mutable list of string.
        /// </summary>
        new IList<string> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of integer.
        /// </summary>
        new ISet<string> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of integer.
        /// </summary>
        new IDictionary<string, string> KeyDictionary { get; }
    }

    /// <summary>
    /// Simple data record. Compatible with a IPoco field (no mutable reference).
    /// </summary>
    /// <param name="Value">The data value.</param>
    /// <param name="Name">The data name.</param>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public record struct NamedRecord( int Value, string Name );

    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with <see cref="NamedRecord"/>.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface INamedRecordCommand : ICommandAbs
    {
        /// <summary>
        /// Gets the record.
        /// </summary>
        new ref NamedRecord Key { get; }

        /// <summary>
        /// Gets the mutable list of record.
        /// </summary>
        new IList<NamedRecord> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of record.
        /// </summary>
        new ISet<NamedRecord> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of record.
        /// </summary>
        new IDictionary<string, NamedRecord> KeyDictionary { get; }
    }

    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with an anonymous record.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface IAnonymousRecordCommand : ICommandAbs
    {
        /// <summary>
        /// Gets the anonymous record key.
        /// </summary>
        new ref (int Value, Guid Id) Key { get; }

        /// <summary>
        /// Gets the mutable list of anonymous record.
        /// </summary>
        new IList<(int Value, Guid Id)> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of anonymous record.
        /// </summary>
        new ISet<(int Value, Guid Id)> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of anonymous record.
        /// </summary>
        new IDictionary<string, (int Value, Guid Id)> KeyDictionary { get; }
    }

    /// <summary>
    /// <see cref="ICommandAbs"/> has a non nullable object key: this can
    /// be used only with types that has a devault value. An Abstract IPoco has no
    /// default value: to reference an Abstract IPoco, the property MUST be nullable.
    /// <para>
    /// This is not the case for the collections since non null objects can be added and
    /// the empty collection is the default value.
    /// </para>
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface ICommandAbsWithNullableKey : IAbstractCommand
    {
        /// <summary>
        /// Gets a nullable object key.
        /// </summary>
        object? Key { get; }

        /// <summary>
        /// Gets a list of key as objects.
        /// </summary>
        IReadOnlyList<object> KeyList { get; }

        /// <summary>
        /// Gets a set of key as objects.
        /// </summary>
        IReadOnlySet<object> KeySet { get; }

        /// <summary>
        /// Gets a dictionary of key as objects.
        /// </summary>
        IReadOnlyDictionary<string, object> KeyDictionary { get; }
    }

    /// <summary>
    /// Concrete command where <see cref="ICommandAbs"/> works with any command.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface ICommandCommand : ICommandAbsWithNullableKey
    {
        /// <summary>
        /// Gets or sets the command key.
        /// Here, the command must be nullable otherwise we would be
        /// unable to have a default value for it. 
        /// </summary>
        new ICommand? Key { get; set; }

        /// <summary>
        /// Gets the mutable list of command.
        /// </summary>
        new IList<ICommand> KeyList { get; }

        /// <summary>
        /// The mutable set of command is not possible: a set must have read-only compliant key and a poco is
        /// everything but read-only compliant.
        /// </summary>
        new ISet<ExtendedCultureInfo> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of command.
        /// </summary>
        new IDictionary<string, ICommand> KeyDictionary { get; }
    }

    /// <summary>
    /// Specializes the <see cref="ICommandAbs"/> to return an object.
    /// </summary>
    [CKTypeDefiner]
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface ICommandAbsWithResult : ICommandAbs, ICommand<object>
    {
    }

    /// <summary>
    /// Concrete command where <see cref="ICommandAbsWithResult"/> works with <see cref="NamedRecord"/>.
    /// </summary>
    [TypeScript( SameFolderAs = typeof( ICommandAbs ) )]
    public interface INamedRecordCommandWithResult : ICommandAbsWithResult
    {
        /// <summary>
        /// Gets the record.
        /// </summary>
        new ref NamedRecord Key { get; }

        /// <summary>
        /// Gets the mutable list of record.
        /// </summary>
        new IList<NamedRecord> KeyList { get; }

        /// <summary>
        /// Gets the mutable set of record.
        /// </summary>
        new ISet<NamedRecord> KeySet { get; }

        /// <summary>
        /// Gets the mutable dictionary of record.
        /// </summary>
        new IDictionary<string, NamedRecord> KeyDictionary { get; }
    }


}
