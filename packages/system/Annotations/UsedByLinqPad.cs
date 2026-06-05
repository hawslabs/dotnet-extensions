namespace System.Annotations;

using JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse(ImplicitUseKindFlags.Access)]
internal sealed class UsedByLinqPadAttribute : Attribute;