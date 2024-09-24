using CK.CrisLike;
using System.Collections.Generic;

namespace CK.StObj.TypeScript.Tests.CrisLike;

/// <summary>
/// Tests the SecondaryPoco use. SecondaryPoco is erased in TS: the primary <see cref="IWithObjectCommand"/> is used
/// everywhere a secondary definition appears.
/// </summary>
[TypeScript( Folder = "Cmd/WithObject" )]
public interface IWithSecondaryCommand : ICommand
{
    /// <summary>
    /// A mutable field. Uninitialized since nullable.
    /// </summary>
    IWithObjectSpecializedAsSuperPocoCommand? NullableCmdWithSetter { get; set; }

    /// <summary>
    /// A mutable field. Initialized to an initial command since non nullable.
    /// </summary>
    IWithObjectSpecializedAsPocoCommand CmdWithSetter { get; set; }

    /// <summary>
    /// Read-only field. Initialized to an initial command.
    /// </summary>
    IWithObjectSpecializedAsSuperPocoCommand CmdAuto { get; }

    /// <summary>
    /// Concrete list must have getter and setter.
    /// </summary>
    List<IWithObjectSpecializedAsSuperPocoCommand> ListSecondary { get; set; }

    /// <summary>
    /// "Standard" Poco covariant auto implementation.
    /// </summary>
    IList<IWithObjectSpecializedAsPocoCommand> ListSecondaryAuto { get; }
}
