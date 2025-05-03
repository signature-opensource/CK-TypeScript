namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    int _editorCount;

    /// <summary>
    /// Gets a disposable <see cref="Editor"/> that enables code modification.
    /// </summary>
    /// <returns></returns>
    public Editor OpenEditor()
    {
        _editorCount++;
        return new Editor( this );
    }

    void CloseEditor()
    {
        if( --_editorCount == 0 )
        {

        }
    }

    public readonly struct Editor
    {
        readonly SourceCodeEditor _e;

        public Editor( SourceCodeEditor e )
        {
            _e = e;
        }

        public void Dispose() => _e.CloseEditor();
    }
}
