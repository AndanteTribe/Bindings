using System.Collections.Generic;
using Bindings.GeneratorCore;
using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Shared symbol identity, type relationship, and Schema property mapping checks.
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

    /// <summary>
    /// Checks whether a property matches a Schema field by generated name and type.
    /// This Bindings-specific mapping does not establish accessibility or source provenance.
    /// </summary>
    public static bool IsSchemaProperty(this IPropertySymbol property, INamedTypeSymbol? schemaAttribute)
    {
        foreach (var member in property.ContainingType.GetMembers())
        {
            if (member is IFieldSymbol field
                && field.HasAttribute(schemaAttribute)
                && TemplateHelpers.ToPropertyName(field.Name) == property.Name
                && SymbolEqualityComparer.IncludeNullability.Equals(field.Type, property.Type))
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

    private static bool HasInterfaceConstraint(ITypeParameterSymbol parameter, INamedTypeSymbol interfaceType, HashSet<ITypeSymbol> visited)
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