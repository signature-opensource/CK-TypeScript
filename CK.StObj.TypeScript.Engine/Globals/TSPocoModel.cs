using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Exposes the IPoco TypeScript model. It is the <see cref="IPocoType"/> for the <see cref="IPoco"/>
    /// and parts that enable extending the model.
    /// </summary>
    public sealed class TSPocoModel
    {
        readonly ITSFileCSharpType _tsPocoType;
        readonly ITSCodePart _pocoTypeInterfacePart;
        readonly ITSCodePart _pocoInterfacePart;
        readonly ITSCodePart _namedRecordInterfacePart;
        readonly ITSCodePart _modelFieldModel;
        readonly ITSCodePart _modelPocoTypePart;

        /// <summary>
        /// Gets the TypeScript <see cref="IPoco"/> type.
        /// The <see cref="ITSFileCSharpType.File"/> contains the IPoco and its IPocoModel implementations.
        /// </summary>
        /// <returns>The IPoco generated type.</returns>
        public ITSFileCSharpType IPocoType => _tsPocoType;

        /// <summary>
        /// Gets a part that can extend the TypeScript IPocoType interface that generalizes
        /// INamedRecord and IPoco interfaces.
        /// </summary>
        public ITSCodePart PocoTypeInterfacePart => _pocoTypeInterfacePart;

        /// <summary>
        /// Gets a part that can extend the TypeScript IPoco interface. This is the base interface of
        /// all IPoco types (AbstractPoco that are TypeScript interfaces and PrimaryPoco that are classes).
        /// <para>
        /// SecondaryPoco are backend implementation details, they are mapped to their PrimaryPoco and don't surface in TypeScript.
        /// </para>
        /// </summary>
        public ITSCodePart PocoInterfacePart => _pocoInterfacePart;

        /// <summary>
        /// Gets a part that can extend the TypeScript the INamedRecord type (C# struct).
        /// </summary>
        public ITSCodePart NamedRecordInterfacePart => _namedRecordInterfacePart;

        /// <summary>
        /// Gets a part that can extend the TypeScript FieldModel.
        /// </summary>
        public ITSCodePart ModelFieldModel => _modelFieldModel;

        /// <summary>
        /// Gets a part that can extend the TypeScript IPocoTypeModel.
        /// </summary>
        public ITSCodePart ModelPocoTypePart => _modelPocoTypePart;


        internal TSPocoModel( IActivityMonitor monitor, TypeScriptContext typeScriptContext )
        {
            _tsPocoType = (ITSFileCSharpType)typeScriptContext.Root.TSTypes.ResolveTSType( monitor, typeof( IPoco ) );
            _tsPocoType.TypePart.Append( """
                /**
                * Base interface for all IPoco types.
                * IPocoType can be INamedRecord or IPoco: they have a name and fields.
                * Anonymous records (C# value tuples) have no name: they have no model.
                **/
                export interface IPocoType {
                    /**
                    * Gets the Poco Type description. 
                    **/
                    readonly pocoTypeModel: IPocoTypeModel;
                    
                    readonly _brand: {};
                """ )
                .CreatePart( out _pocoTypeInterfacePart, closer: "}\n" )
                .Append( """

                /**
                * Base interface for all IPoco types (AbstractPoco that are TypeScript interfaces
                * or PrimaryPoco that are TypeScript classes).
                * SecondaryPoco are backend implementation details, they are mapped to their PrimaryPoco.
                **/
                export interface IPoco extends IPocoType {
                    readonly _brand: {"IPoco": any};
                """ )
                .CreatePart( out _pocoInterfacePart, closer: "}\n" )
                .Append( """

                /**
                * Base interface for all INamedRecord types (C# struct).
                **/
                export interface INamedRecord extends IPocoType {
                    readonly _brand: {"INamedRecord": any};
                """ )
                .CreatePart( out _namedRecordInterfacePart, closer: "}\n" )
                .Append( """

                /**
                * Models a field in a IPocoType.
                **/
                export type FieldModel = {

                    /**
                    * Gets the field name.
                    **/
                    readonly name: string,

                    /**
                    * Gets the field's type name.
                    **/
                    readonly type: string,

                    /**
                    * Gets whether the field can be "undefined".
                    **/
                    readonly isOptional: boolean;

                    /**
                    * Gets field index in its IPocoType.
                    **/
                    readonly index: number;
                """ )
                .CreatePart( out _modelFieldModel, closer: "}\n" )
                .Append( """

                /**
                * Describes a IPoco type. 
                **/
                export interface IPocoTypeModel {
                    /**
                    * Gets whether this is a INamedRecord or a IPoco. 
                    **/
                    readonly isNamedRecord: boolean;

                    /**
                    * Gets the type name.
                    **/
                    readonly type: string;

                    /**
                    * Gets a unique index for this type in the Poco Type System. 
                    **/
                    readonly index: number;
                    
                    /**
                    * Gets the model for each field. 
                    **/
                    readonly fields: ReadonlyArray<FieldModel>;
                """ )
                .CreatePart( out _modelPocoTypePart, closer: "}\n" );
        }
    }
}
