using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Shared symbol identity and type relationship checks.
/// Missing metadata types never match unresolved symbols.
/// </summary>
internal static class SymbolExtensions
{
    public static bool IsAttribute(this AttributeData attribute, INamedTypeSymbol? attributeType) =>
        attributeType != null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType);

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        if (attributeType == null)
        {
            return false;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.IsAttribute(attributeType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool InheritsFrom(this INamedTypeSymbol type, INamedTypeSymbol? baseType)
    {
        if (baseType == null)
        {
            return false;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ImplementsInterface(this INamedTypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        if (interfaceType == null)
        {
            return false;
        }

        foreach (var implementedInterface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implementedInterface.OriginalDefinition, interfaceType))
            {
                return true;
            }
        }

        return false;
    }
}