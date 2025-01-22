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

            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GEN001", "Source Generator starts", $"Found {receiver.Fields.Count} properties", "SourceGenerator",
                    DiagnosticSeverity.Info, true), Location.None));

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
                    var propertyName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                    var fieldType = symbol.Type.ToDisplayString();

                    // Report diagnostic when generating a property
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GEN002", "Generating Property",
                        $"🔧 Generating property {propertyName} in {className}",
                        "SourceGenerator", DiagnosticSeverity.Info, true), Location.None));

                    var source = $@"
#nullable enable

namespace {namespaceName}
{{
    public partial class {className}
    {{
        public {fieldType} {propertyName}
        {{
            get => GetProperty<{fieldType}>();
            set => SetProperty(value);
        }}
    }}
}}";

                    context.AddSource($"{className}_{propertyName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }
    }

    class FieldSyntaxReceiver : ISyntaxReceiver
    {
        public List<FieldDeclarationSyntax> Fields { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is FieldDeclarationSyntax fieldDecl)
            {
                foreach (var attributeList in fieldDecl.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        Console.WriteLine($"Detected attribute: {attribute.Name.ToFullString()}");
                    }
                }

                if (fieldDecl.AttributeLists.Any(a => a.Attributes.Any(attr => attr.Name.ToFullString() == "AutoNotify")))
                {                 
                    Console.WriteLine($"✅ Found AutoNotify on: {fieldDecl.Declaration.Variables.First().Identifier}");
                    Fields.Add(fieldDecl);
                }
            }
        }
    }

}
