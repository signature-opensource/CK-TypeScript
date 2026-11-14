using CK.AppIdentity;

namespace CK.TypeScript;

/// <summary>
/// The TypeScript framework of a client: the client feature of a framework into which the TypeScript code of the client
/// is generated ("NgApp" for an Angular application, CK.TS.Angular). A client has at most one.
/// </summary>
public interface ITypeScriptFramework : IPartyComponentAbstraction
{
    /// <summary>
    /// Gets what the build says about the client application (its project, its build output, its facts).
    /// </summary>
    ClientInfo Info { get; }
}
