using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

/// <summary>
/// A low-level tokenizer recognizes simple patterns, typically identifiers, keywords or isolated characters. Depending on the
/// language, more complex tokens can be isolated. 
/// </summary>
/// <param name="head">The start of the text to categorize. Leading trivias have already been handled.</param
public delegate LowLevelToken LowLevelTokenizer( ReadOnlySpan<char> head );

/// <summary>
/// Low-level token is a candidate token.
/// </summary>
/// <param name="NodeType">The detected candidate node type. Defaults to <see cref="NodeType.None"/>.</param>
/// <param name="Length">The candidate token length. Defaults to 0.</param>
public readonly record struct LowLevelToken( NodeType NodeType, int Length );
