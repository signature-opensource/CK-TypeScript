using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class RecordTests
    {
        [TypeScript( Folder = "" )]
        public interface IValueTuplePoco1 : IPoco
        {
            // Anonymous record with no name at all: TypeScript [tuple] syntax.
            ref (int, string, string?, Guid?) Power { get; }
            // Note that the default is optimized.
            // => public power: [Number, String, String?, Guid?] = [0, ""]
        }

        [TypeScript( Folder = "" )]
        public interface IValueTuplePoco2 : IPoco
        {
            // TypeScript [tuple] syntax with nullables:
            //
            // [Number, String?, String, NormalizedCultureInfo?, Number?]
            //                 ^ Error TS1257 A required element cannot follow an optional element.
            //
            // This has to be:
            // [Number, String|undefined, String, NormalizedCultureInfo?, Number?]
            //
            ref (int, string?, string, NormalizedCultureInfo?, double?) Power { get; }
            // => public readonly power: [Number, String|undefined, String, NormalizedCultureInfo?, Number?] = [0, undefined, ""]
        }

        [TypeScript( Folder = "" )]
        public interface IValueTuplePoco3 : IPoco
        {
            ref (int, string, NormalizedCultureInfo, double) Power { get; }
            // => public readonly power: [Number, String, NormalizedCultureInfo, Number] = [0, "", NormalizedCultureInfo.codeDefault, 0]
        }

        [TypeScript( Folder = "" )]
        public interface IValueTupleWithNamePoco1 : IPoco
        {
            // When fields have name: {object} syntax.
            ref (int Age, string UserId, string? FirstName, Guid? LastName) Power { get; }
            // => public readonly power: {age: Number, userId: String, firstName?: String, lastName?: Guid} = {age: 0, userId: ""}
        }

        [TypeScript( Folder = "" )]
        public interface IValueTupleWithNamePoco2 : IPoco
        {
            // When at least one field has name: {object} syntax.
            // Missing names are item1, item2, etc.
            ref (int, string Name, string?, Guid? AnotherName) Power { get; }
            // => public readonly power: {item1: Number, name: String, item3?: String, anotherName?: Guid} = {item1: 0, name: ""}
        }

        [TypeScript( Folder = "" )]
        public interface IValueTupleWithNamePoco3 : IPoco
        {
            // {object} syntax with leading nullable (optional) fields is fine.
            ref (int?, string Name, string, Guid? AnotherName) Power { get; }
            // => public readonly power: {item1?: Number, name: String, item3: String, anotherName?: Guid} = {name: "", item3: ""}
        }

        [Test]
        public void anonymous_records_use_TypeScript_Tuple_or_Object_Syntax()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IValueTuplePoco1 ),
                typeof( IValueTuplePoco2 ),
                typeof( IValueTuplePoco3 ),
                typeof( IValueTupleWithNamePoco1 ),
                typeof( IValueTupleWithNamePoco2 ),
                typeof( IValueTupleWithNamePoco3 ),
            };
            TestHelper.GenerateTypeScript( targetProjectPath, tsTypes, tsTypes );

            CheckFile( targetProjectPath,
                "ValueTuplePoco1.ts",
                """public readonly power: [Number, String, String?, Guid?] = [0, ""]""" );

            CheckFile( targetProjectPath,
                "ValueTuplePoco2.ts",
                """public readonly power: [Number, String|undefined, String, NormalizedCultureInfo?, Number?] = [0, undefined, ""]""" );

            CheckFile( targetProjectPath,
                "ValueTuplePoco3.ts",
                """public readonly power: [Number, String, NormalizedCultureInfo, Number] = [0, "", NormalizedCultureInfo.codeDefault, 0]""" );

            CheckFile( targetProjectPath,
                "ValueTupleWithNamePoco1.ts",
                """public readonly power: {age: Number, userId: String, firstName?: String, lastName?: Guid} = {age: 0, userId: ""}""" );

            CheckFile( targetProjectPath,
                "ValueTupleWithNamePoco2.ts",
                """public readonly power: {item1: Number, name: String, item3?: String, anotherName?: Guid} = {item1: 0, name: ""}""" );

            CheckFile( targetProjectPath,
                "ValueTupleWithNamePoco3.ts",
                """public readonly power: {item1?: Number, name: String, item3: String, anotherName?: Guid} = {name: "", item3: ""}""" );
        }

        [TypeScript( SameFolderAs = typeof( IRecordPoco1 ) )]
        public record struct Rec1( int Age,
                                   string Name,
                                   string? AltName,
                                   Guid? Key );

        [TypeScript( Folder = "" )]
        public interface IRecordPoco1 : IPoco
        {
            ref Rec1 R1 { get; }
        }

        [TypeScript( SameFileAs = typeof( IRecordPoco2 ) )]
        public record struct Rec2( int Age,
                                   string Name = "Aurélien",
                                   string? AltName = "Barrau",
                                   Guid? Key = null );

        [TypeScript( Folder = "" )]
        public interface IRecordPoco2 : IPoco
        {
            ref Rec2 R2 { get; }
        }


        [TypeScript( SameFolderAs = typeof( IRecordPoco3 ) )]
        public record struct Rec3( Guid? Key = null,
                                   string? AltName = "Barrau",
                                   string Name = "Aurélien",
                                   int Age = 40 );

        [TypeScript( Folder = "" )]
        public interface IRecordPoco3 : IPoco
        {
            ref Rec3 R3 { get; }
        }


        [Test]
        public void named_records_are_TypeScript_classes()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IRecordPoco1 ),
                typeof( IRecordPoco2 ),
                typeof( IRecordPoco3 ),
            };
            var registeredTypes = tsTypes.Append( typeof( Rec1 ) ).Append( typeof( Rec2 ) ).Append( typeof( Rec3 ) );
            TestHelper.GenerateTypeScript( targetProjectPath, registeredTypes, tsTypes );

            CheckFile( targetProjectPath,
                "Rec1.ts",
                """
                public age: Number = 0, 
                public name: String = "", 
                public altName?: String, 
                public key?: Guid
                """ );

            CheckFile( targetProjectPath,
                "RecordPoco1.ts",
                """
                public readonly r1: Rec1 = new Rec1()
                """ );

            CheckFile( targetProjectPath,
                "RecordPoco2.ts",
                """
                public age: Number = 0, 
                public name: String = "Aurélien", 
                public altName: String|undefined = "Barrau", 
                public key?: Guid
                """ );

            CheckFile( targetProjectPath,
                "Rec3.ts",
                """
                public name: String = "Aurélien", 
                public age: Number = 40, 
                public altName: String|undefined = "Barrau", 
                public key?: Guid
                """ );

        }

        [TypeScript( SameFolderAs = typeof( IRecTryPoco ) )]
        public record struct RecTry( string Name, List<RecTry> Others );

        [TypeScript( Folder = "" )]
        public interface IRecTryPoco : IPoco
        {
            IList<RecTry> R1 { get; }
        }

        [Test]
        public void recurse_via_record_is_handled()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IRecTryPoco )
            };
            var registeredTypes = tsTypes.Append( typeof( RecTry ) );
            TestHelper.GenerateTypeScript( targetProjectPath, registeredTypes, tsTypes );
            CheckFile( targetProjectPath,
               "RecTry.ts",
               """
               public name: String = "", 
               public others: Array<RecTry> = []
               """ );

        }

        // The only way to have a non defaultable field in the Poco world is a reference
        // to an abstract type. And the only abstract type is a IAbstractPoco.
        [TypeScript( SameFolderAs = typeof( IRecWithNonNullDefaultPoco ) )]
        public record struct RecWithNonNullDefault( double? Nullable, IPoco IMustExist, string Name, int Age = 42 );

        [TypeScript( Folder = "" )]
        public interface IRecWithNonNullDefaultPoco : IPoco
        {
            IList<RecWithNonNullDefault> R1 { get; }
        }

        [Test]
        public void record_with_fields_without_default_is_handled()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IRecWithNonNullDefaultPoco )
            };
            var registeredTypes = tsTypes.Append( typeof( RecWithNonNullDefault ) );
            TestHelper.GenerateTypeScript( targetProjectPath, registeredTypes, tsTypes );
            CheckFile( targetProjectPath,
              "RecWithNonNullDefault.ts",
              """
              public iMustExist: IPoco, 
              public name: String = "", 
              public age: Number = 42, 
              public nullable?: Number
              """ );
        }

        static void CheckFile( string targetProjectPath, string name, string expected )
        {
            File.ReadAllText( Path.Combine( targetProjectPath, "ck-gen", "src", name ) )
                .ReplaceLineEndings()
                .Should().Contain( expected.ReplaceLineEndings() );
        }
    }
}
