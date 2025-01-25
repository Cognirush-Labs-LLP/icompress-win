using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AutoNotifyGenerator
{
    [Generator]
    public class AutoNotifyPropertyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the syntax receiver
            context.RegisterForSyntaxNotifications(() => new FieldSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not FieldSyntaxReceiver receiver)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GEN001", "Source Generator Error", "FieldSyntaxReceiver not found", "SourceGenerator",
                    DiagnosticSeverity.Warning, true), Location.None));
                return;
            }

            var compilation = context.Compilation;

            foreach (var fieldDecl in receiver.Fields)
            {
                var semanticModel = compilation.GetSemanticModel(fieldDecl.SyntaxTree);

                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (symbol == null) continue;

                    var className = symbol.ContainingType.Name;
                    var namespaceName = symbol.ContainingNamespace.ToDisplayString();
                    var fieldName = symbol.Name;
                    var propertyName = ToPascalCase(fieldName);
                    var fieldType = symbol.Type.ToDisplayString();

                    if (propertyName == "E") 
                        // Report diagnostic when generating a property
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            "GEN003", "Generating Property",
                            $"🔧 Generating property {propertyName} in {className}, the name is too small.",
                            "SourceGenerator", DiagnosticSeverity.Error, true), Location.None));
                    else if (propertyName == "S")
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            "GEN004", "Generating Property",
                            $"🔧 Generating property {propertyName} in {className}, field name shouldn't be pascal cased.",
                            "SourceGenerator", DiagnosticSeverity.Error, true), Location.None));

                    // Determine accessibility for setter
                    string setterAccessibility = symbol.DeclaredAccessibility switch
                    {
                        Accessibility.Private => "private ",
                        Accessibility.Protected => "protected ",
                        Accessibility.Internal => "internal ",
                        Accessibility.ProtectedAndInternal => "protected internal ",
                        Accessibility.ProtectedOrInternal => "protected internal ",
                        _ => "",
                    };

                    // Report diagnostic when generating a property
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GEN002", "Generating Property",
                        $"🔧 Generating property {propertyName} in {className}",
                        "SourceGenerator", DiagnosticSeverity.Info, true), Location.None));

                    // Generate the property code
                    var source = $@"
                        #nullable enable

                        namespace {namespaceName}
                        {{
                            using System.Collections.Generic;
                            public partial class {className}
                            {{
                                public {fieldType} {propertyName}
                                {{
                                    get => {fieldName};
                                    {setterAccessibility}set
                                    {{
                                        if (!EqualityComparer<{fieldType}>.Default.Equals({fieldName}, value))
                                        {{
                                            {fieldName} = value;
                                            raisePropertyChanged(nameof({propertyName}));
                                        }}
                                    }}
                                }}
                            }}
                        }}";
                    context.AddSource($"{className}_{propertyName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }


        /// <summary>
        /// Creates Pascal Cased Property name. Eg. from `prop` to 'Prop'
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns>E if fieldName is empty. S if field name is already pascal cased.</returns>
        private string ToPascalCase(string fieldName)
        {
            var valueToReturn = "";
            var name = fieldName; //.TrimStart('_', 'm', 'M');
            if (string.IsNullOrEmpty(name) || name.Length < 2)
                return "E"; 

            valueToReturn = char.ToUpper(name[0]) + name.Substring(1);
            if (valueToReturn == fieldName) {
                return "S";
            }
            return valueToReturn;
        }
    }

    class FieldSyntaxReceiver : ISyntaxReceiver
    {
        public List<FieldDeclarationSyntax> Fields { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Look for field declarations with [AutoNotify] attribute
            if (syntaxNode is FieldDeclarationSyntax fieldDecl)
            {
                foreach (var attributeList in fieldDecl.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (name == "AutoNotify" || name.EndsWith(".AutoNotify"))
                        {
                            Fields.Add(fieldDecl);
                            break;
                        }
                    }
                }
            }
        }
    }

}
