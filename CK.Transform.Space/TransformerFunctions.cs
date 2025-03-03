using CK.Core;
using CK.EmbeddedResources;
using CK.Transform.Core;
using System.Collections.Generic;

namespace CK.Transform.Space;

public class TransformerFunctions : TransformableSource
{
    readonly string _transformTargetBase;
    readonly TransformerHost.Language? _monoTargetLanguage;
    List<TransformerFunction>? _transformers;

    public TransformerFunctions( TransformPackage package,
                                 ResourceLocator origin,
                                 string? localFilePath,
                                 string transformTargetBase,
                                 TransformerHost.Language? monoTargetLanguage )
        : base( package, origin, localFilePath )
    {
        _transformTargetBase = transformTargetBase;
        _monoTargetLanguage = monoTargetLanguage;
    }

    public TransformerHost.Language? MonoTargetLanguage => _monoTargetLanguage;

    /// <summary>
    /// Gets the target name if <see cref="MonoTargetLanguage"/> is known (this resource must
    /// then contain a single transformer function) or only the base name when the target
    /// language is not known: there can be more than one transformer function and their actual
    /// targets must be resolved based on their transform language and their <see cref="TransformerFunction.Target"/>
    /// optional specifier.
    /// </summary>
    public string TransformTargetBase => _transformTargetBase;

    private protected override bool OnTextAvailable( ApplyChangesContext c, string text )
    {
        _transformers = c.Host.TryParseFunctions( c.Monitor, text );
        if( _transformers == null )
        {
            c.AddError( $"Unable to parse transformers from {Origin}." );
            return false;
        }
        bool success = true;
        if( _transformers.Count == 0 )
        {
            c.Monitor.Warn( $"No transformers found in {Origin}." );
        }
        else
        {
            foreach( var t in _transformers )
            {

            }
        }
        return success;
    }
}
