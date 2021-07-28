using CK.Text;
using CK.TypeScript.CodeGen;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class CommentTests
    {
        /// <summary>
        /// Commented enumeration.
        /// </summary>
        [TypeScript( Folder = "" )]
        public enum CommentedEnum
        {
            /// <summary>
            /// The Zero is 0.
            /// <para>
            /// Para elements are transparent.
            /// <para></para>
            /// <para>
            ///
            /// Even nested (consecutive empty lines are collapsed).
            /// 
            /// <para></para>
            /// </para>
            /// </para>
            /// </summary>
            Zero,

            /// <summary>
            /// One is 1 (1 &gt; 0 &amp;&amp; 0 &lt; 1).
            /// Code <c>can be 
            ///   coded (will use
            ///     markdown backticks)
            ///     Note: spaces/newlines are ignored both in C# &amp; TS: they are left as-is.</c>
            /// </summary>
            One,

            /// <summary>
            /// <!-- An xml comment appears in the Xml documentation file. It will be removed from the TS doc. -->
            /// A <code>
            /// Code block will use triple backticks,
            ///    so it can be
            ///        on multiple
            ///            lines:
            ///     Spaces/newlines are ignored in C# tooltip, but preserved in TS (C# is wrong).
            ///     They must be preserved.
            /// </code>
            /// (This is just for fun.)
            /// </summary>
            /// <remarks>
            /// A remark is appended.
            /// </remarks>
            /// <remarks>
            /// Another remark is ignored in C# tooltip, but just as any other Xml elements, they appear
            /// in the Xml documentation file. For TS comment, we concatenate all the remarks.
            /// </remarks>
            Two,

            Three, Four
        }

        [Test]
        public void comments_on_enum_values_are_supported()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( comments_on_enum_values_are_supported ), typeof( CommentedEnum ) );
            var s = File.ReadAllText( output.AppendPart( "CommentedEnum.ts" ) );

            s.Should().Contain( "Commented enumeration." );

            var newLine = Environment.NewLine + " * ";

            s.Should().Contain( "The Zero is 0." + newLine + newLine + "Para elements are transparent." + newLine + newLine + "Even nested (consecutive empty lines are collapsed)." )
                      .And.Contain( "Zero = 0," );

            s.Should().Contain( "One is 1 (1 > 0 && 0 < 1)." + newLine + "Code `can be" )
                      .And.Contain( "Note: spaces/newlines are ignored both in C# & TS: they are left as-is.`" )
                      .And.Contain( "One = 1," );


            s.Should().NotContain( "An xml comment appears in the Xml documentation file." )
                      .And.Contain( "A "+ newLine + "```" + newLine + "Code block will use triple backticks," + newLine + "   so it can be" )
                      .And.Contain( newLine + "```" + newLine + "(This is just for fun.)" + newLine + newLine + "A remark is appended." + newLine + newLine + "Another remark is" )
                      .And.Contain( "Two = 2" );

            s.Should().Contain( "Two = 2," + Environment.NewLine + "Three = 3," + Environment.NewLine + "Four = 4" );
        }

        /// <summary>
        /// An interface with comment.
        /// </summary>
        [TypeScript( Folder = "" )]
        public interface ICommented
        {
            /// <summary>
            /// Gets the power.
            /// </summary>
            int Power { get; }

            /// <summary>
            /// Method comment.
            /// </summary>
            /// <param name="b">The boolean.</param>
            /// <param name="i">The integer.</param>
            /// <param name="s">
            /// The string.
            /// <para>Just like in summary.</para>
            /// </param>
            /// <returns>The returned value.</returns>
            int DoIt( bool b, int i, string s );
        }

        [Test]
        public void comments_for_parameters_and_returns_are_handled()
        {
            var output = LocalTestHelper.GetOutputFolder();

            var ctx = new TypeScriptRoot( new[] { (output, new XElement( "TypeScript" )) }, false, true );

            var f = ctx.Root.FindOrCreateFile( "ICommented.ts" );
            GenerateMembersDocumentation( f, typeof( ICommented ), "interface ICommented" );
            ctx.Save( TestHelper.Monitor );

            var s = f.Body.ToString();
            File.ReadAllText( output.AppendPart( f.Name ) ).Should().Be( s );

            s.Should().Contain( "An interface with comment." );

            var newLine = Environment.NewLine + " * ";

            s.Should().Contain( newLine + "Gets the power." );

            s.Should().Contain( newLine + "Method comment."
                                + newLine + "@param b The boolean."
                                + newLine + "@param i The integer."
                                + newLine + "@param s The string."
                                + newLine
                                + newLine + "Just like in summary."
                                + newLine + "@returns The returned value." );
        }

        /// <summary>
        /// Generic interface.
        /// </summary>
        /// <typeparam name="T1">The FIRST generic type!</typeparam>
        /// <typeparam name="T2">The SECOND generic type!</typeparam>
        [TypeScript( Folder = "" )]
        public interface IGeneric<T1,T2>
        {
            /// <summary>
            /// Generic method comment.
            /// </summary>
            /// <typeparam name="U1">The FIRST generic method parameter.</typeparam>
            /// <typeparam name="U2">The SECOND generic method parameter.</typeparam>
            /// <param name="u">FIRST generic method.</param>
            /// <param name="eU">FIRST generic method DEPENDENT.</param>
            /// <param name="uBis">SECOND generic method.</param>
            /// <param name="eUBis">SECOND generic method DEPENDENT.</param>
            /// <param name="t">FIRST generic type.</param>
            /// <param name="eT">FIRST generic type DEPENDENT.</param>
            /// <param name="tBis">SECOND generic type.</param>
            /// <param name="eTBis">SECOND generic type DEPENDENT.</param>
            /// <returns>The returned generic tuple.</returns>
            (U1,T1,U2,T2) GenericMethodSample<U1,U2>( U1 u, IList<U1> eU, U2 uBis, IList<U2> eUBis, T1 t, IEnumerable<T1> eT, T2 tBis, IEnumerable<T2> eTBis );
        }

        [Test]
        public void comments_with_Type_or_Method_generic_parameters_are_handled()
        {
            var output = LocalTestHelper.GetOutputFolder();
            var ctx = new TypeScriptRoot( new[] { (output, new XElement( "TypeScript" )) }, false, true );

            var f = ctx.Root.FindOrCreateFile( "IGeneric.ts" );
            GenerateMembersDocumentation( f, typeof( IGeneric<,> ), "interface IGeneric<T1,T2>" );
            ctx.Save( TestHelper.Monitor );

            var s = f.Body.ToString();
            File.ReadAllText( output.AppendPart( f.Name ) ).Should().Be( s );

            s.Should().Contain( "Generic interface." )
                      .And.Contain( "@typeParam T1 The FIRST generic type!" )
                      .And.Contain( "@typeParam T2 The SECOND generic type!" );

            var newLine = Environment.NewLine + " * ";

            s.Should().Contain( newLine + "Generic method comment."
                                + newLine + "@typeParam U1 The FIRST generic method parameter."
                                + newLine + "@typeParam U2 The SECOND generic method parameter."
                                + newLine + "@param u FIRST generic method."
                                + newLine + "@param eU FIRST generic method DEPENDENT."
                                + newLine + "@param uBis SECOND generic method."
                                + newLine + "@param eUBis SECOND generic method DEPENDENT."
                                + newLine + "@param t FIRST generic type."
                                + newLine + "@param eT FIRST generic type DEPENDENT."
                                + newLine + "@param tBis SECOND generic type."
                                + newLine + "@param eTBis SECOND generic type DEPENDENT."
                                + newLine + "@returns The returned generic tuple." );
        }

        /// <summary>
        /// Class doc.
        /// </summary>
        [TypeScript( Folder = "" )]
        public class FullClass
        {
            /// <summary>
            /// Constructor doc.
            /// </summary>
            public FullClass() { }

            /// <summary>
            /// Method doc.
            /// </summary>
            public void M() { }

            /// <summary>
            /// Property doc.
            /// </summary>
            public int P { get; set; }

            /// <summary>
            /// Field doc.
            /// </summary>
            public int F;

            /// <summary>
            /// Event doc.
            /// </summary>
            public event EventHandler? E;
        }

        [Test]
        public void comments_for_constructors_properties_fields_events_and_methods_are_handled()
        {
            var output = LocalTestHelper.GetOutputFolder();
            var ctx = new TypeScriptRoot( new[] { (output, new XElement( "TypeScript" )) }, false, true );

            var f = ctx.Root.FindOrCreateFile( "FullClass.ts" );
            GenerateMembersDocumentation( f, typeof( FullClass ), "class FullClass" );
            ctx.Save( TestHelper.Monitor );

            var s = f.Body.ToString();
            File.ReadAllText( output.AppendPart( f.Name ) ).Should().Be( s );

            s.Should().Contain( "Class doc." )
                      .And.Contain( "Constructor doc." )
                      .And.Contain( "Method doc." )
                      .And.Contain( "Property doc." )
                      .And.Contain( "Field doc." )
                      .And.Contain( "Event doc." );
        }

        /// <summary>
        /// WithCodeReference doc.
        /// </summary>
        [TypeScript( Folder = "" )]
        public class WithCodeReference
        {
            /// <summary>
            /// Initializes a new <seealso cref="WithCodeReference"/> instance (seealso is treated like see).
            /// </summary>
            /// <param name="a">A parameter.</param>
            public WithCodeReference( int a ) { }

            /// <summary>
            /// The constructor is <see cref="WithCodeReference(int)">very simple</see> - hey!
            /// The parameter is <paramref name="a"/>.
            /// </summary>
            /// <param name="a">The A.</param>
            public void M( int a ) { }

            /// <summary>
            /// The field is <see cref="F"/>.
            /// </summary>
            public int P { get; set; }

            /// <summary>
            /// Field doc.
            /// </summary>
            public int F;

            /// <summary>
            /// Event doc.
            /// <para>
            /// The type is <see cref="WithCodeReference"/>.
            /// The constructor is <see cref="WithCodeReference(int)"/>.
            /// The method is <see cref="M"/>.
            /// The field is <see cref="F"/>.
            /// The property is <see cref="P"/>.
            /// The event is <see cref="E"/>.
            /// </para>
            /// </summary>
            public event EventHandler? E;
        }

        [Test]
        public void comments_textual_code_references_by_DocumentationCodeRef_TextOnly()
        {
            var output = LocalTestHelper.GetOutputFolder();
            var ctx = new TypeScriptRoot( new[] { (output, new XElement( "TypeScript" )) }, false, true );

            var f = ctx.Root.FindOrCreateFile( "WithCodeReference.ts" );
            GenerateMembersDocumentation( f, typeof( WithCodeReference ), "class WithCodeReference" );
            ctx.Save( TestHelper.Monitor );

            var s = f.Body.ToString();
            File.ReadAllText( output.AppendPart( f.Name ) ).Should().Be( s );

            s.Should().Contain( "WithCodeReference doc." )
                      .And.Contain( "Initializes a new WithCodeReference instance (seealso is treated like see)." )
                      .And.Contain( "The constructor is very simple - hey!" )
                      .And.Contain( "The parameter is a." )
                      .And.Contain( "The type is WithCodeReference." )
                      .And.Contain( "The constructor is WithCodeReference.constructor." )
                      .And.Contain( "The method is WithCodeReference.m." )
                      .And.Contain( "The property is WithCodeReference.p." )
                      .And.Contain( "The field is WithCodeReference.f." )
                      .And.Contain( "The event is WithCodeReference.e." );
        }

        /// <summary>
        /// A buggy reference: <see cref="TypeNotFound"/>.
        /// </summary>
        [TypeScript( Folder = "" )]
        public class BuggyReference
        {
        }

        [Test]
        public void comments_code_reference_error_displays_a_Strikethrough_buggy_ref()
        {
            var output = LocalTestHelper.GetOutputFolder();
            var ctx = new TypeScriptRoot( new[] { (output, new XElement( "TypeScript" )) }, false, true );

            var f = ctx.Root.FindOrCreateFile( "BuggyReference.ts" );
            GenerateMembersDocumentation( f, typeof( BuggyReference ), "class BuggyReference" );
            ctx.Save( TestHelper.Monitor );

            var s = f.Body.ToString();
            File.ReadAllText( output.AppendPart( f.Name ) ).Should().Be( s );

            s.Should().Contain( "A buggy reference: ~~!:TypeNotFound~~." );
        }

        static void GenerateMembersDocumentation( TypeScriptFile f, Type t, string header )
        {
            f.Body.AppendDocumentation( TestHelper.Monitor, t )
                  .Append( header )
                  .OpenBlock()
                  .Append( w =>
                  {
                      foreach( var m in t.GetMembers( System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance ) )
                      {
                          if( m is System.Reflection.MethodInfo method && method.IsSpecialName )
                          {
                              // Skip methods with "Special Name": they are get, set, add or remove.
                              continue;
                          }
                          w.AppendDocumentation( TestHelper.Monitor, m );

                          if( m.Name == ".ctor" ) w.Append( "constructor() {}" );
                          else w.AppendIdentifier( m.Name ).Append( ": any; // " ).Append( m.ToString() );
                      }
                  } )
                  .CloseBlock();
        }


    }
}
