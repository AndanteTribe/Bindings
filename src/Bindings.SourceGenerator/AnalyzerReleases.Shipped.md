; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BND001  | Bindings | Error    | ViewModelTypeNameMustEndWithViewModel. [Documentation](../../docs/diagnostics/BND001.md)
BND002  | Bindings | Error    | InvalidSchemaIdValue. [Documentation](../../docs/diagnostics/BND002.md)
BND003  | Bindings | Error    | ConflictingTooltipValues. [Documentation](../../docs/diagnostics/BND003.md)
BND004  | Bindings | Error    | UnsupportedTypeAccessibility. [Documentation](../../docs/diagnostics/BND004.md)
BND005  | Bindings | Warning  | ViewModelSerializedByUnity. [Documentation](../../docs/diagnostics/BND005.md)
BND006  | Bindings | Error    | ViewMustNotDeriveFromMonoBehaviour. [Documentation](../../docs/diagnostics/BND006.md)
BND007  | Bindings | Error    | SchemaFieldMustBeAccessedThroughProperty. [Documentation](../../docs/diagnostics/BND007.md)
BNDP001 | Bindings.Practices | Error | UseGeneratedSelfRebindHelper. [Documentation](../../docs/diagnostics/BNDP001.md)
BNDP002 | Bindings.Practices | Error | ViewMustNotConstructViewModel. [Documentation](../../docs/diagnostics/BNDP002.md)
BNDP003 | Bindings.Practices | Error | ViewModelMustNotMerelyAggregateViewModels. [Documentation](../../docs/diagnostics/BNDP003.md)
BNDP004 | Bindings.Practices | Error | DoNotWrapSchemaPropertyAssignments. [Documentation](../../docs/diagnostics/BNDP004.md)
