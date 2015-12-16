﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.DataModelGenerator
{
    internal static class CodeGenerator
    {
        public const string CommonInterfaceName = "ISyntax";

        /// <summary>Writes an XML doc comment node with the supplied content.</summary>
        /// <param name="sourceCode">Code writer into which the doc comment shall be written.</param>
        /// <param name="commentType">Type of the comment.</param>
        /// <param name="comments">The comment text.</param>
        public static void WriteXmlDocComment(CodeWriter sourceCode, string commentType, string comments)
        {
            if (String.IsNullOrWhiteSpace(comments))
            {
                return;
            }

            comments = CommentSanitizer.Sanitize(comments);
            var xml = new XElement(commentType, comments);
            string xmlStr = xml.ToString();
            foreach (string xmlStrLine in CommentSanitizer.SplitByNewlines(xmlStr))
            {
                sourceCode.WriteLine("/// " + xmlStrLine);
            }
        }

        public static void WriteXmlDocCommentsFor(CodeWriter sourceCode, DataModelType type)
        {
            WriteXmlDocComment(sourceCode, "summary", type.SummaryText);
            WriteXmlDocComment(sourceCode, "remarks", type.RemarksText);
        }

        public static void WriteXmlDocCommentsFor(CodeWriter sourceCode, DataModelMember member)
        {
            WriteXmlDocComment(sourceCode, "summary", member.SummaryText);
        }

        /// <summary>Writes the header to a C# file generated from a .g4 file.</summary>
        /// <param name="sourceCode">Code writer into which the header shall be written.</param>
        /// <param name="sourcePath">Full pathname of the source g4 file.</param>
        /// <param name="metaData">Metadata describing the whole data model.</param>
        public static void WriteHeader(CodeWriter sourceCode, string sourcePath, DataModelMetadata metaData)
        {
            sourceCode.WriteLine("// *********************************************************");
            sourceCode.WriteLine("// *                                                       *");
            sourceCode.WriteLine("// *   Copyright (C) Microsoft. All rights reserved.       *");
            sourceCode.WriteLine("// *                                                       *");
            sourceCode.WriteLine("// *********************************************************");
            sourceCode.WriteLine();
            sourceCode.WriteLine("//----------------------------------------------------------");
            sourceCode.WriteLine("// <auto-generated>");
            sourceCode.WriteLine("//     This code was generated by a tool.");
            sourceCode.WriteLine("//     Input Grammar      : " + metaData.Name);
            sourceCode.WriteLine("//     Input Grammar file : " + sourcePath);
            sourceCode.WriteLine("//     ");
            sourceCode.WriteLine("//     Changes to this file may cause incorrect behavior and ");
            sourceCode.WriteLine("//     will be lost when the code is regenerated.");
            sourceCode.WriteLine("// </auto-generated>");
            sourceCode.WriteLine("//----------------------------------------------------------");
            sourceCode.WriteLine();
            sourceCode.WriteLine("using System;");
            sourceCode.WriteLine("using System.Collections.Generic;");
            sourceCode.WriteLine("using System.Globalization;");
            sourceCode.WriteLine("using System.Linq;");
            sourceCode.WriteLine("using System.Runtime.CompilerServices;");
            sourceCode.WriteLine("using System.Runtime.Serialization;");
            sourceCode.WriteLine("using System.Text;");
            sourceCode.WriteLine();
            sourceCode.OpenBrace("namespace " + metaData.Namespace);
        }

        /// <summary>Writes a footer for a C# file generated from a .g4 file.</summary>
        /// <param name="sourceCode">Code writer into which the footer shall be written.</param>
        public static void WriteFooter(CodeWriter sourceCode)
        {
            sourceCode.CloseBrace(); // namespace
            sourceCode.WriteLine("// End of generated code.");
            sourceCode.WriteLine();
        }

        /// <summary>Writes code document comments for all overrides of <see cref="Object.GetHashCode"/>
        /// on generated C# code.</summary>
        /// <param name="sourceCode">Code writer into which the doc comments shall be written.</param>
        public static void WriteGetHashCodeDocComments(CodeWriter sourceCode)
        {
            sourceCode.WriteLine("/// <summary>Gets a hash code for this instance.</summary>");
        }

        /// <summary>Writes code document comments for all overrides of
        /// <see cref="Object.Equals(object)"/> on generated C# code.</summary>
        /// <param name="sourceCode">Code writer into which the doc comments shall be written.</param>
        public static void WriteEqualsDocComments(CodeWriter sourceCode)
        {
            sourceCode.WriteLine("/// <summary>Compares this instance with another object, and returns whether or not they contain equivalent data.</summary>");
            sourceCode.WriteLine("/// <param name=\"o\">The object with which this instance shall be compared.</param>");
        }

        /// <summary>Writes a data model as a C# file.</summary>
        /// <param name="codeWriter">The code writer into which the C# is written.</param>
        /// <param name="model">The data model to write.</param>
        public static void WriteDataModel(CodeWriter codeWriter, DataModel model)
        {
            WriteHeader(codeWriter, model.SourceFilePath, model.MetaData);

            WriteKindEnumeration(codeWriter, model);
            WriteCommonInterface(codeWriter, model);
            WriteTypes(codeWriter, model);
            VisitorGenerator.Generate(codeWriter, model);
            RewritingVisitorGenerator.Generate(codeWriter, model);

            WriteFooter(codeWriter);
        }

        /// <summary>Writes a "kind" enumeration for a data model.</summary>
        /// <param name="codeWriter">The code writer into which the C# is written.</param>
        /// <param name="model">The data model for which a kind enum shall be written.</param>
        public static void WriteKindEnumeration(CodeWriter codeWriter, DataModel model)
        {
            codeWriter.WriteLine("/// <summary>An enumeration containing all the types which implement <see cref=\"" + CommonInterfaceName + "\" />.</summary>");
            codeWriter.OpenBrace("public enum " + model.Name + "Kind");
            codeWriter.WriteLine("/// <summary>An uninitialized kind.</summary>");
            codeWriter.WriteLine("None,");

            foreach (DataModelType type in model.Types)
            {
                if (type.Kind == DataModelTypeKind.Leaf)
                {
                    string enumName = type.CSharpName;
                    codeWriter.WriteLine();
                    codeWriter.WriteLine("/// <summary>An entry indicating that the <see cref=\"" + CommonInterfaceName + "\" /> object is of type " + enumName + ".</summary>");
                    codeWriter.WriteLine(enumName + ",");
                }
            }

            codeWriter.CloseBrace(); // enum
            codeWriter.WriteLine();
        }

        /// <summary>Writes the common interface for a data model.</summary>
        /// <param name="codeWriter">The code writer into which the C# is written.</param>
        /// <param name="model">The data model for which an common interface shall be written.</param>
        public static void WriteCommonInterface(CodeWriter codeWriter, DataModel model)
        {
            //WriteKnownTypes(codeWriter, model.Types.Where(x => x.Kind != DataModelTypeKind.BuiltInString).Select(x => x.CSharpName));
            codeWriter.WriteLine("/// <summary>An interface for all types generated from the grammar " + model.Name + ".</summary>");
            codeWriter.OpenBrace("public interface " + CommonInterfaceName);

            WriteKindXmlComments(codeWriter);
            codeWriter.WriteLine(model.Name + "Kind SyntaxKind { get; }");

            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Makes a deep copy of this instance.</summary>");
            codeWriter.WriteLine(CommonInterfaceName + " DeepClone();");

            if (model.MetaData.GenerateLocations)
            {
                WriteLocationProperties(codeWriter, String.Empty);
            }

            codeWriter.CloseBrace(); // interface
            codeWriter.WriteLine();
        }

        private static void WriteLocationProperties(CodeWriter codeWriter, string prefix)
        {
            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Gets or sets the location of this object in a text stream.</summary>");
            codeWriter.WriteLine("/// <value>The location of this object in a text stream.</summary>");
            codeWriter.WriteLine(prefix + "int Offset { get; set; }");

            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Gets or sets the length of this object in a text stream.</summary>");
            codeWriter.WriteLine("/// <value>The length of this object in a text stream.</summary>");
            codeWriter.WriteLine(prefix + "int Length { get; set; }");
        }

        public static void WriteTypes(CodeWriter codeWriter, DataModel model)
        {
            foreach (DataModelType type in model.Types)
            {
                switch (type.Kind)
                {
                    case DataModelTypeKind.Base:
                    case DataModelTypeKind.Leaf:
                        WriteType(codeWriter, model, type);
                        break;
                    case DataModelTypeKind.Enum:
                        WriteEnum(codeWriter, model, type);
                        break;
                    case DataModelTypeKind.BuiltInNumber:
                    case DataModelTypeKind.BuiltInString:
                    case DataModelTypeKind.BuiltInDictionary:
                    case DataModelTypeKind.BuiltInBoolean:
                    case DataModelTypeKind.BuiltInVersion:
                    case DataModelTypeKind.BuiltInUri:
                        // Don't write builtin types
                        break;
                    case DataModelTypeKind.Default:
                    default:
                        Debug.Fail("Unexpected data model type kind in a data model " + type.Kind);
                        break;
                }
            }
        }

        private static void WriteEnum(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            WriteXmlDocCommentsFor(codeWriter, type);
            WriteCommonAttributes(codeWriter);
            codeWriter.WriteLine("public enum " + type.CSharpName);
            codeWriter.OpenBrace();

            codeWriter.WriteLine("Unknown = 0,");

            foreach (string member in type.SerializedValues)
            {
                codeWriter.WriteLine(member.Trim() + ",");
            }

            codeWriter.CloseBrace(); // enum
            codeWriter.WriteLine();
        }

        private static void WriteType(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            WriteXmlDocCommentsFor(codeWriter, type);
            WriteCommonAttributes(codeWriter);
            WriteClassDeclaration(codeWriter, model, type);

            WriteKindProperty(codeWriter, model, type);
            if (model.MetaData.GenerateLocations && !type.HasBase)
            {
                // (if the type has a base; these properties come from the base)
                WriteLocationProperties(codeWriter, "public ");
            }

            WriteMembers(codeWriter, model, type);
            WriteConstructors(codeWriter, model, type);
            WriteVirtualConstructor(codeWriter, type);
            ToStringGenerator.Generate(codeWriter, model, type);
            EqualsGenerator.Generate(codeWriter, model, type);

            codeWriter.CloseBrace(); // class
            codeWriter.WriteLine();
        }

        private static void WriteKindProperty(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            if (type.HasBase && type.Kind == DataModelTypeKind.Base)
            {
                // It came from the base
                return;
            }

            WriteKindXmlComments(codeWriter);

            var sb = new StringBuilder("public ");
            if (type.Kind == DataModelTypeKind.Base)
            {
                sb.Append("abstract ");
            }
            else if (type.HasBase)
            {
                sb.Append("override ");
            }

            sb.Append(model.Name);
            sb.Append("Kind SyntaxKind ");

            switch (type.Kind)
            {
                case DataModelTypeKind.Leaf:
                    sb.Append("{ get { return " + model.Name + "Kind." + type.CSharpName + "; } }");
                    break;
                case DataModelTypeKind.Base:
                    sb.Append("{ get; }");
                    break;
                default:
                    Debug.Fail("Tried to write kind for class not implementing common interface");
                    break;
            }

            codeWriter.WriteLineConsume(sb);
        }

        private static void WriteMembers(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            foreach (DataModelMember member in type.Members)
            {
                if (model.MetaData.GenerateLocations && (member.CSharpName == "Length" || member.CSharpName == "Offset"))
                {
                    continue;
                }

                codeWriter.WriteLine();
                WriteProperty(codeWriter, model, member);
            }
        }

        private static void WriteConstructors(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            if (type.Members.Length == 0)
            {
                // No need for the constructor when there are no members; the implicit one is fine.
                return;
            }

            WriteInitFunction(codeWriter, model, type);
            WriteNoArgConstructor(codeWriter, type.CSharpName);
            WriteMemberConstructor(codeWriter, model, type);
            WriteCopyConstructor(codeWriter, type);
        }

        private static void WriteVirtualConstructor(CodeWriter codeWriter, DataModelType type)
        {
            if (!type.HasBase)
            {
                // If the type has a base, this got emitted in the base.
                codeWriter.WriteLine();
                codeWriter.OpenBrace(CommonInterfaceName + " " + CommonInterfaceName + ".DeepClone()");
                codeWriter.WriteLine("return this.DeepCloneCore();");
                codeWriter.CloseBrace();
            }

            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Creates a deep copy of this instance.</summary>");
            var sb = new StringBuilder("public ");
            if (type.HasBase)
            {
                sb.Append("new ");
            }

            sb.Append(type.CSharpName).Append(" DeepClone()");
            codeWriter.OpenBraceConsume(sb);
            codeWriter.WriteLine("return ({0})this.DeepCloneCore();", type.CSharpName);
            codeWriter.CloseBrace();

            if (type.Kind == DataModelTypeKind.Base)
            {
                if (type.HasBase)
                {
                    // Already got emitted in the base class
                    return;
                }

                codeWriter.WriteLine();
                codeWriter.WriteLine("protected abstract " + CommonInterfaceName + " DeepCloneCore();");
                return;
            }

            codeWriter.WriteLine();

            if (type.HasBase)
            {
                sb.Append("protected override ");
            }
            else if (type.Kind == DataModelTypeKind.Base)
            {
                sb.Append("protected ");
            }
            else
            {
                sb.Append("private ");
            }

            sb.Append(CommonInterfaceName).Append(" DeepCloneCore()");
            codeWriter.OpenBraceConsume(sb);
            string copyType;
            if (type.Members.Length == 0)
            {
                copyType = String.Empty;
            }
            else
            {
                copyType = "this";
            }

            codeWriter.WriteLine("return new {0}({1});", type.CSharpName, copyType);
            codeWriter.CloseBrace();
        }

        private static void WriteCopyConstructor(CodeWriter codeWriter, DataModelType type)
        {
            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Initializes a new instance of the <see cref=\"" + type.CSharpName + "\" /> class as a copy of another instance.</summary>");
            codeWriter.WriteLine("/// <exception cref=\"ArgumentNullException\">Thrown if <paramref name=\"other\" /> is null.</exception>");
            codeWriter.WriteLine("/// <param name=\"other\">The instance to copy.</param>");
            codeWriter.OpenBrace("public {0}({0} other)", type.CSharpName);
            codeWriter.OpenBrace("if (other == null)");
            codeWriter.WriteLine("throw new ArgumentNullException(\"other\");");
            codeWriter.CloseBrace();
            codeWriter.WriteLine();
            WriteCallToInit(codeWriter, type.Members.Select(member => "other." + member.CSharpName));
            codeWriter.CloseBrace();
        }

        private static void WriteMemberConstructor(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Initializes a new instance of the <see cref=\"" + type.CSharpName + "\" /> class with the supplied data.</summary>");
            foreach (DataModelMember member in type.Members)
            {
                codeWriter.WriteLine("/// <param name=\"" + member.ArgumentName + "\">An initialization value for the <see cref=\"P:" + member.CSharpName + "\" /> member.</param>");
            }

            WriteConstructorDeclaration(codeWriter, model, type);
            WriteCallToInit(codeWriter, type.Members.Select(member => member.ArgumentName));
            codeWriter.CloseBrace();
        }

        private static void WriteNoArgConstructor(CodeWriter codeWriter, string typeName)
        {
            codeWriter.WriteLine();
            codeWriter.WriteLine("/// <summary>Initializes a new instance of the <see cref=\"" + typeName + "\" /> class.</summary>");
            codeWriter.OpenBrace("public " + typeName + "()");
            codeWriter.WriteLine("// Blank on purpose");
            codeWriter.CloseBrace();
        }

        private static void WriteInitFunction(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            codeWriter.WriteLine();

            var sb = new StringBuilder();
            sb.Append("private void Init(");
            WriteParameters(sb, model, type);
            sb.Append(')');
            codeWriter.OpenBraceConsume(sb);

            var valueNamer = new LocalVariableNamer("value");
            var destNamer = new LocalVariableNamer("destination");
            foreach (DataModelMember member in type.Members)
            {
                WriteInitForMember(codeWriter, model, member, destNamer, valueNamer);
            }

            codeWriter.CloseBrace(); // Init
        }

        private static void WriteCallToInit(CodeWriter codeWriter, IEnumerable<string> argNames)
        {
            codeWriter.WriteLine("this.Init(");
            codeWriter.IncrementIndentLevel();
            using (IEnumerator<string> enumerator = argNames.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    codeWriter.Write(enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        codeWriter.WriteLineRaw(", ");
                        codeWriter.Write(enumerator.Current);
                    }
                }
            }

            codeWriter.WriteLineRaw();
            codeWriter.DecrementIndentLevel();
            codeWriter.WriteLine(");");
        }

        private static void WriteInitForMember(CodeWriter codeWriter, DataModel model, DataModelMember member, LocalVariableNamer destNamer, LocalVariableNamer valueNamer)
        {
            DataModelType memberType = model.GetTypeForMember(member);
            if (memberType.IsNullable)
            {
                codeWriter.OpenBrace("if ({0} != null)", member.ArgumentName);
            }

            if (member.Rank == 0)
            {
                codeWriter.WriteLine("this." + member.CSharpName + " = " + GetCopyCreationExpression(memberType, member.ArgumentName) + ";");
            }
            else
            {
                string[] destinations = new string[member.Rank];
                string source = member.ArgumentName;
                for (int idx = 0; idx < destinations.Length; ++idx)
                {
                    string currentDest = destNamer.MakeName();
                    destinations[idx] = currentDest;

                    StringBuilder sb = new StringBuilder();
                    sb.Append("var ").Append(currentDest).Append(" = new List<");
                    AppendRankedTypeName(sb, "IList", memberType.CSharpName, member.Rank - idx - 1);
                    sb.Append(">();");
                    codeWriter.WriteLineConsume(sb);
                    codeWriter.OpenBrace("if ({0} != null)", source);
                    string value = valueNamer.MakeName();
                    codeWriter.OpenBrace("foreach (var {0} in {1})", value, source);
                    source = value;
                }

                string lastDestination = destinations[destinations.Length - 1];
                if (memberType.IsNullable)
                {
                    codeWriter.OpenBrace("if ({0} == null)", source);
                    codeWriter.WriteLine(lastDestination + ".Add(null);");
                    codeWriter.CloseBrace();
                    codeWriter.OpenBrace("else");
                }

                codeWriter.WriteLine(lastDestination + ".Add(" + GetCopyCreationExpression(memberType, source) + ");");
                if (memberType.IsNullable)
                {
                    codeWriter.CloseBrace();
                }

                codeWriter.CloseBrace(); // foreach
                codeWriter.CloseBrace(); // if (source != null)
                codeWriter.WriteLine();

                for (int idx = 1; idx < destinations.Length; ++idx)
                {
                    codeWriter.WriteLine("{0}.Add({1});", destinations[member.Rank - idx - 1], destinations[member.Rank - idx]);
                    codeWriter.CloseBrace(); // foreach
                    codeWriter.CloseBrace(); // if (source != null)
                    codeWriter.WriteLine();
                }

                codeWriter.WriteLine("this.{0} = {1};", member.CSharpName, destinations[0]);
            }

            if (memberType.IsNullable)
            {
                codeWriter.CloseBrace();
            }
        }

        private static string GetCopyCreationExpression(DataModelType memberType, string sourceVariable)
        {
            switch (memberType.Kind)
            {
                case DataModelTypeKind.Leaf:
                case DataModelTypeKind.BuiltInDictionary:
                    return "new " + memberType.CSharpName + "(" + sourceVariable + ")";
                case DataModelTypeKind.Base:
                    return sourceVariable + ".DeepClone()";
                case DataModelTypeKind.BuiltInNumber:
                case DataModelTypeKind.BuiltInString:
                case DataModelTypeKind.BuiltInBoolean:
                    return sourceVariable;
                case DataModelTypeKind.BuiltInVersion:
                    return "(global::System.Version)" + sourceVariable + ".Clone()";
                case DataModelTypeKind.Enum:
                    return sourceVariable;
                case DataModelTypeKind.BuiltInUri:
                    return "new global::System.Uri(" + sourceVariable + ".OriginalString, " + sourceVariable + ".IsAbsoluteUri ? global::System.UriKind.Absolute : global::System.UriKind.Relative)";
                default:
                    Debug.Fail("Unexpected DataModelTypeKind");
                    throw new InvalidOperationException();
            }
        }

        private static void WriteConstructorDeclaration(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(type.CSharpName);
            sb.Append('(');
            WriteParameters(sb, model, type);
            sb.Append(')');
            codeWriter.OpenBraceConsume(sb);
        }

        private static void WriteParameters(StringBuilder sb, DataModel model, DataModelType type)
        {
            WriteParameterForMember(sb, model, type.Members[0]);
            for (int idx = 1; idx < type.Members.Length; ++idx)
            {
                sb.Append(", ");
                WriteParameterForMember(sb, model, type.Members[idx]);
            }
        }

        private static void WriteParameterForMember(StringBuilder sb, DataModel model, DataModelMember member)
        {
            CodeGenerator.AppendRankedTypeName(sb, "IEnumerable", model.GetTypeForMember(member).CSharpName, member.Rank);
            sb.Append(' ');
            sb.Append(member.ArgumentName);
        }

        private static void AppendRankedTypeName(StringBuilder sb, string collectionType, string tType, int rank)
        {
            for (int idx = 0; idx < rank; ++idx)
            {
                sb.Append(collectionType);
                sb.Append('<');
            }

            sb.Append(tType);
            sb.Append('>', rank);
        }

        private static void WriteCommonAttributes(CodeWriter codeWriter)
        {
            codeWriter.WriteLine("[DataContract]");
            codeWriter.WriteLine("[CompilerGenerated]");
        }

        private static void WriteClassDeclaration(CodeWriter codeWriter, DataModel model, DataModelType type)
        {
            var decl = new StringBuilder("public ");
            switch (type.Kind)
            {
                case DataModelTypeKind.Base:
                    decl.Append("abstract ");
                    break;
                case DataModelTypeKind.Leaf:
                    decl.Append("sealed ");
                    break;
                default:
                    throw new InvalidOperationException("Unexpected data model kind");
            }

            decl.Append("class ");
            decl.Append(type.CSharpName);
            decl.Append(" : ");

            if (!string.IsNullOrEmpty(type.InterfaceName))
            {
                decl.Append(type.InterfaceName);
                decl.Append(", ");
            }

            if (type.HasBase)
            {
                decl.Append(type.Base);
                decl.Append(", ");
            }

            decl.Append(CommonInterfaceName);

            if (model.MetaData.GenerateEquals && type.Kind == DataModelTypeKind.Leaf)
            {
                decl.Append(", IEquatable<");
                decl.Append(type.CSharpName);
                decl.Append('>');
            }

            codeWriter.OpenBraceConsume(decl);
        }

        private static void WriteProperty(CodeWriter codeWriter, DataModel model, DataModelMember member)
        {
            WriteXmlDocCommentsFor(codeWriter, member);
            WritePropertyDataMemberAttribute(codeWriter, member);
            WritePropertyDeclaration(codeWriter, member, model.GetTypeByG4Name(member.DeclaredName).CSharpName);
        }

        private static void WritePropertyDeclaration(CodeWriter codeWriter, DataModelMember member, string memberTypeName)
        {
            var decl = new StringBuilder();
            decl.Append("public ");
            AppendRankedTypeName(decl, "IList", memberTypeName, member.Rank);
            decl.Append(' ');
            decl.Append(member.CSharpName);
            decl.Append(" { get; set; }");
            codeWriter.WriteLineConsume(decl);
        }

        private static void WritePropertyDataMemberAttribute(CodeWriter codeWriter, DataModelMember member)
        {
            var dataMemberAttribute = new StringBuilder();
            dataMemberAttribute.Append("[DataMember(Name=\"");
            dataMemberAttribute.Append(member.SerializedName);
            dataMemberAttribute.Append("\", IsRequired = ");
            if (member.Required)
            {
                dataMemberAttribute.Append("true)]");
            }
            else
            {
                dataMemberAttribute.Append("false, EmitDefaultValue = false)]");
            }

            codeWriter.WriteLine(dataMemberAttribute.ToString());
        }

        private static void WriteKindXmlComments(CodeWriter codeWriter)
        {
            codeWriter.WriteLine("/// <summary>Gets the kind of type implementing <see cref=\"" + CommonInterfaceName + "\" />.</summary>");
            codeWriter.WriteLine("/// <value>The enumeration value for the kind of type implementing <see cref=\"" + CommonInterfaceName + "\" />.</value>");
        }
    }
}
