using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class NodeTypeTests
{
    [Test]
    public void TokenType_class_reservation_throws()
    {
        FluentActions.Invoking( () => NodeTypeExtensions.ReserveTokenClass( 0, "This is the error!" ) )
            .Should().Throw<InvalidOperationException>()
                     .WithMessage( "The class 'This is the error!' cannot use n°0, this number is already reserved by 'Error'." );
    }

}
