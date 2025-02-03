using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoNotifyGenerator
{
    [Generator]
    public class AutoNotifyPropertyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Uncomment to debug the generator.
            // System.Diagnostics.Debugger.Launch();

            // Register a syntax receiver that collects candidate fields.
            context.RegisterForSyntaxNotifications(() => new FieldSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is FieldSyntaxReceiver receiver))
            {
                return;
            }

            // Retrieve the symbol for AutoNotifyAttribute.
            INamedTypeSymbol? autoNotifyAttributeSymbol = context.Compilation
                .GetTypeByMetadataName("miCompressor.core.AutoNotifyAttribute");

            // Group candidate fields by their containing type.
            var fieldsByClass = new Dictionary<INamedTypeSymbol, List<IFieldSymbol>>(SymbolEqualityComparer.Default);

            foreach (var fieldDecl in receiver.CandidateFields)
            {
                var semanticModel = context.Compilation.GetSemanticModel(fieldDecl.SyntaxTree);
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
                        continue;

                    // Only consider fields explicitly marked with [AutoNotify].
                    if (autoNotifyAttributeSymbol == null ||
                        !fieldSymbol.GetAttributes().Any(ad =>
                            ad.AttributeClass?.Equals(autoNotifyAttributeSymbol, SymbolEqualityComparer.Default) == true))
                    {
                        continue;
                    }

                    // Skip static or constant fields.
                    if (fieldSymbol.IsStatic || fieldSymbol.IsConst)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN005",
                                "Invalid Field",
                                $"Field '{fieldSymbol.Name}' in class '{fieldSymbol.ContainingType.Name}' is static or const and cannot be used with [AutoNotify].",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            fieldSymbol.Locations.FirstOrDefault()));
                        continue;
                    }

                    // Skip readonly fields.
                    if (fieldSymbol.IsReadOnly)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN006",
                                "Readonly Field",
                                $"Field '{fieldSymbol.Name}' in class '{fieldSymbol.ContainingType.Name}' is readonly. [AutoNotify] requires a mutable field.",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            fieldSymbol.Locations.FirstOrDefault()));
                        continue;
                    }

                    // Ensure the containing type is declared as partial.
                    var containingType = fieldSymbol.ContainingType;
                    bool isPartial = containingType.DeclaringSyntaxReferences
                        .Select(r => r.GetSyntax())
                        .OfType<ClassDeclarationSyntax>()
                        .Any(c => c.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));
                    if (!isPartial)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN007",
                                "Class Not Partial",
                                $"Class '{containingType.Name}' must be declared partial to use [AutoNotify].",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            fieldSymbol.Locations.FirstOrDefault()));
                        continue;
                    }

                    // Add the field to its containing type.
                    if (!fieldsByClass.TryGetValue(containingType, out var list))
                    {
                        list = new List<IFieldSymbol>();
                        fieldsByClass.Add(containingType, list);
                    }
                    list.Add(fieldSymbol);
                }
            }

            // Validate each group for duplicate or invalid generated property names.
            foreach (var kvp in fieldsByClass)
            {
                var classSymbol = kvp.Key;
                var fields = kvp.Value;
                var propertyNames = new Dictionary<string, IFieldSymbol>(StringComparer.Ordinal);

                // Work on a copy since we might remove fields that fail the check.
                foreach (var field in fields.ToList())
                {
                    string propertyName = ToPascalCase(field.Name);

                    // Enforce naming conventions: reject names that are "E" or "S".
                    if (propertyName == "E")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN003",
                                "Invalid Property Name",
                                $"The generated property name for field '{field.Name}' in class '{classSymbol.Name}' is too short ('E').",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            field.Locations.FirstOrDefault()));
                        fields.Remove(field);
                        continue;
                    }
                    if (propertyName == "S")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN004",
                                "Invalid Property Name",
                                $"The generated property name for field '{field.Name}' in class '{classSymbol.Name}' should not be Pascal cased already.",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            field.Locations.FirstOrDefault()));
                        fields.Remove(field);
                        continue;
                    }

                    // Check for duplicates.
                    if (propertyNames.TryGetValue(propertyName, out var existingField))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "GEN008",
                                "Duplicate Property Name",
                                $"Duplicate generated property name '{propertyName}' found in class '{classSymbol.Name}' from fields '{existingField.Name}' and '{field.Name}'.",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            field.Locations.FirstOrDefault()));
                        fields.Remove(existingField);
                        fields.Remove(field);
                        continue;
                    }
                    else
                    {
                        propertyNames[propertyName] = field;
                    }
                }
            }

            // Only generate code for classes that have at least one valid candidate field.
            foreach (var kvp in fieldsByClass)
            {
                var classSymbol = kvp.Key;
                var fields = kvp.Value;
                if (fields.Count == 0)
                    continue;

                string namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
                    ? null
                    : classSymbol.ContainingNamespace.ToDisplayString();
                string source = GeneratePartialClass(namespaceName, classSymbol, fields);
                context.AddSource($"{classSymbol.Name}_AutoNotify.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Generates the partial class code that implements INotifyPropertyChanged and the properties.
        /// </summary>
        private string GeneratePartialClass(string? namespaceName, INamedTypeSymbol classSymbol, List<IFieldSymbol> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using miCompressor.core;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // Generate a partial class that implements INotifyPropertyChanged.
            sb.AppendLine($"partial class {classSymbol.Name} : INotifyPropertyChanged");
            sb.AppendLine("{");
            sb.AppendLine("    public event PropertyChangedEventHandler? PropertyChanged;");
            sb.AppendLine();
            sb.AppendLine("    protected void OnPropertyChanged(string propertyName) =>");
            sb.AppendLine("        UIThreadHelper.RunOnUIThread(() =>");
            sb.AppendLine("        {");
            sb.AppendLine("            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine("        });");
            sb.AppendLine();

            // Generate a property for each candidate field.
            foreach (var field in fields)
            {
                string fieldName = field.Name;
                string propertyName = ToPascalCase(field.Name);
                string fieldType = field.Type.ToDisplayString();

                // Preserve the setter accessibility from the field.
                string setterAccessibility = field.DeclaredAccessibility switch
                {
                    Accessibility.Private => "private ",
                    Accessibility.Protected => "protected ",
                    Accessibility.Internal => "internal ",
                    Accessibility.ProtectedAndInternal => "protected internal ",
                    Accessibility.ProtectedOrInternal => "protected internal ",
                    _ => ""
                };

                sb.AppendLine($"    public {fieldType} {propertyName}");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => {fieldName};");
                sb.AppendLine("        set");
                sb.AppendLine("        {");
                sb.AppendLine($"            if (!EqualityComparer<{fieldType}>.Default.Equals({fieldName}, value))");
                sb.AppendLine("            {");
                sb.AppendLine($"                {fieldName} = value;");
                sb.AppendLine($"                OnPropertyChanged(nameof({propertyName}));");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a field name (e.g. _width or width) to a PascalCase property name (e.g. Width).
        /// </summary>
        private string ToPascalCase(string fieldName)
        {
            // Remove any underscore prefix.
            string name = fieldName.TrimStart('_');
            if (string.IsNullOrEmpty(name))
                return fieldName;
            return char.ToUpper(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// A syntax receiver that collects all field declarations with attributes.
        /// </summary>
        private class FieldSyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Only consider field declarations that have at least one attribute.
                if (syntaxNode is FieldDeclarationSyntax fieldDeclaration &&
                    fieldDeclaration.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclaration);
                }
            }
        }
    }
}
