using System.Collections.Generic;
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

    /// <summary>
    /// Checks whether a method implements an interface method in its declaring type.
    /// Explicit/partial implementations, inherited interfaces, and constructed generic interfaces are supported.
    /// Interfaces introduced only by a derived type are outside this declaring-type check.
    /// </summary>
    public static bool IsInterfaceImplementation(this IMethodSymbol method)
    {
        var definition = method.PartialDefinitionPart ?? method;
        if (!definition.ExplicitInterfaceImplementations.IsEmpty)
        {
            return true;
        }

        foreach (var contract in definition.ContainingType.AllInterfaces)
        {
            foreach (var member in contract.GetMembers(definition.Name))
            {
                if (definition.ContainingType.FindImplementationForInterfaceMember(member) is IMethodSymbol implementation
                    && SymbolEqualityComparer.Default.Equals(implementation.PartialDefinitionPart ?? implementation, definition))
                {
                    return true;
                }
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

    public static bool IsOrInheritsFrom(this INamedTypeSymbol type, INamedTypeSymbol? baseType) =>
        baseType != null && (SymbolEqualityComparer.Default.Equals(type, baseType) || type.InheritsFrom(baseType));

    /// <summary>
    /// Finds the specified base type or a derived type in a type reference,
    /// including array elements, generic arguments, and type-parameter constraints.
    /// </summary>
    public static INamedTypeSymbol? FindReferencedTypeDerivingFrom(this ITypeSymbol? type, INamedTypeSymbol baseType) =>
        FindReferencedTypeDerivingFrom(type, baseType, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));

    private static INamedTypeSymbol? FindReferencedTypeDerivingFrom(ITypeSymbol? type, INamedTypeSymbol baseType, ISet<ITypeSymbol> visited)
    {
        if (type == null || type.TypeKind == TypeKind.Error || !visited.Add(type))
        {
            return null;
        }

        switch (type)
        {
            case IArrayTypeSymbol array:
                return FindReferencedTypeDerivingFrom(array.ElementType, baseType, visited);
            case ITypeParameterSymbol parameter:
                foreach (var constraint in parameter.ConstraintTypes)
                {
                    if (FindReferencedTypeDerivingFrom(constraint, baseType, visited) is { } match)
                    {
                        return match;
                    }
                }
                break;
            case INamedTypeSymbol named when named.IsOrInheritsFrom(baseType):
                return named;
            case INamedTypeSymbol named:
                for (var current = named; current != null; current = current.ContainingType)
                {
                    foreach (var argument in current.TypeArguments)
                    {
                        if (FindReferencedTypeDerivingFrom(argument, baseType, visited) is { } match)
                        {
                            return match;
                        }
                    }
                }
                break;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a type is the specified interface itself or implements it.
    /// </summary>
    public static bool IsOrImplementsInterface(this ITypeSymbol type, INamedTypeSymbol? interfaceType) =>
        interfaceType != null && (SymbolEqualityComparer.Default.Equals(type, interfaceType) || type.ImplementsInterface(interfaceType));

    /// <summary>
    /// Checks interface membership, resolving type-parameter constraints when necessary.
    /// </summary>
    public static bool ImplementsInterface(this ITypeSymbol type, INamedTypeSymbol? interfaceType)
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

        // Resolve type-parameter constraints explicitly; AllInterfaces can be empty for them.
        return type is ITypeParameterSymbol parameter &&
            HasInterfaceConstraint(parameter, interfaceType, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    }

    private static bool HasInterfaceConstraint(ITypeParameterSymbol parameter, INamedTypeSymbol interfaceType, ISet<ITypeSymbol> visited)
    {
        if (!visited.Add(parameter))
        {
            return false;
        }

        foreach (var constraint in parameter.ConstraintTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(constraint.OriginalDefinition, interfaceType))
            {
                return true;
            }

            if (constraint is ITypeParameterSymbol nested)
            {
                if (HasInterfaceConstraint(nested, interfaceType, visited))
                {
                    return true;
                }
            }
            else if (constraint.ImplementsInterface(interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the nearest containing type implementing an interface, including lexical outer types.
    /// </summary>
    public static INamedTypeSymbol? GetContainingTypeImplementingInterface(this ISymbol symbol, INamedTypeSymbol? interfaceType)
    {
        for (var type = symbol.ContainingType; type != null; type = type.ContainingType)
        {
            if (type.ImplementsInterface(interfaceType))
            {
                return type;
            }
        }

        return null;
    }
}