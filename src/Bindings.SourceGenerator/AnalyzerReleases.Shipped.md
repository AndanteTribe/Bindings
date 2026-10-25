; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
BND001  | Bindings | Error    | ViewModelTypeNameMustContainViewModel
BND002  | Bindings | Error    | InvalidSchemaIdValue
BND003  | Bindings | Error    | ConflictingTooltipValues
BND004  | Bindings | Error    | UnsupportedTypeAccessibility
BND005  | Bindings | Warning  | ViewModelSerializedByUnity
BND006  | Bindings | Error    | ViewMustNotDeriveFromMonoBehaviour
BND007  | Bindings | Error    | SchemaFieldMustBeAccessedThroughProperty
BNDP001 | Bindings.Practices | Error | UseGeneratedSelfRebindHelper
BNDP002 | Bindings.Practices | Error | ViewMustNotConstructViewModel
BNDP003 | Bindings.Practices | Error | ViewModelMustNotMerelyAggregateViewModels
BNDP004 | Bindings.Practices | Error | DoNotWrapSchemaPropertyAssignments
