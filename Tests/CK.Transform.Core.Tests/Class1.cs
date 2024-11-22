using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class Class1
{

    [Test]
    public void TokenType_class_reservation_throws()
    {
        FluentActions.Invoking( () => TokenTypeExtensions.ReserveTokenClass( 0, "This is the error!" ) )
            .Should().Throw<InvalidOperationException>()
                     .WithMessage( "The class 'This is the error!' cannot use n°0, this number is already reserved by 'Error'." );
    }

}
