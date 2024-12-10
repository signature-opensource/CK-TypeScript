namespace CK.Transform.Core;

/// <summary>
/// A parser functions must return null when it doesn't recognize its pattern (and let the head untouched).
/// On error it can return a <see cref="TokenErrorNode"/>. Error-tolerant parser return a
/// <see cref="ErrorTolerant.IErrorTolerantNode"/> with the head positionned on the recovery point: it is
/// its responsibility to determine this recovery point.
/// </summary>
/// <param name="head">The parser head.</param>
/// <returns>A node (can be an error node) or null if not recognized.</returns>
public delegate IAbstractNode? NodeParser( ref ParserHead head );

