using System.Text;

namespace Dunet.Generator.UnionGeneration;

internal static class UnionSourceBuilder
{
    public static string Build(UnionDeclaration union)
    {
        var builder = new StringBuilder();

        builder.AppendLine("#pragma warning disable 1591");

        foreach (var import in union.Imports)
        {
            builder.AppendLine(import);
        }

        if (union.Namespace is not null)
        {
            builder.AppendLine($"namespace {union.Namespace};");
        }

        var parentTypes = union.ParentTypes;

        foreach (var type in parentTypes)
        {
            builder.AppendLine($"partial {(type.IsRecord ? "record" : "class")} {type.Identifier}");
            builder.AppendLine("{");
        }

        builder.Append($"abstract partial record {union.Name}");
        builder.AppendTypeParams(union.TypeParameters);
        builder.AppendLine();
        builder.AppendLine("{");
        builder.AppendLine($"    private {union.Name}() {{}}");
        builder.AppendLine();

        builder.AppendAbstractMatchMethods(union);
        builder.AppendAbstractSpecificMatchMethods(union);
        builder.AppendAbstractUnwrapMethods(union);

        if (union.SupportsImplicitConversions())
        {
            foreach (var variant in union.Variants)
            {
                builder.Append($"    public static implicit operator {union.Name}");
                builder.AppendTypeParams(union.TypeParameters);
                builder.AppendLine(
                    $"({variant.Parameters[0].Type.Identifier} value) => new {variant.Identifier}(value);"
                );
            }
            builder.AppendLine();
        }

        foreach (var variant in union.Variants)
        {
            builder.Append($"    public sealed partial record {variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.Append($" : {union.Name}");
            builder.AppendTypeParams(union.TypeParameters);
            builder.AppendLine();
            builder.AppendLine("    {");

            builder.AppendVariantMatchMethodImplementations(union, variant);
            builder.AppendVariantSpecificMatchMethodImplementations(union, variant);
            builder.AppendUnwrapMethodImplementations(union, variant);
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        foreach (var _ in parentTypes)
        {
            builder.AppendLine("}");
        }

        builder.AppendLine("#pragma warning restore 1591");

        return builder.ToString();
    }

    private static StringBuilder AppendAbstractMatchMethods(
        this StringBuilder builder,
        UnionDeclaration union
    )
    {
        // public abstract TMatchOutput Match<TMatchOutput>(
        //     System.Func<UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariant1,
        //     System.Func<UnionVariant2<T1, T2, ...>, TMatchOutput> @unionVariant2,
        //     ...
        // );
        builder.AppendLine("    public abstract TMatchOutput Match<TMatchOutput>(");
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variant = union.Variants[i];
            builder.Append($"        System.Func<{variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.Append($", TMatchOutput> {variant.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("    );");

        // public abstract void Match(
        //     System.Action<UnionVariant1<T1, T2, ...>> @unionVariant1,
        //     System.Action<UnionVariant2<T1, T2, ...>> @unionVariant2,
        //     ...
        // );
        builder.AppendLine("    public abstract void Match(");
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variant = union.Variants[i];
            builder.Append($"        System.Action<{variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.Append($"> {variant.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("    );");
        builder.AppendLine();

        // public abstract TMatchOutput Match<TState, TMatchOutput>(
        //     TState state,
        //     System.Func<TState, UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariant1,
        //     System.Func<TState, UnionVariant2<T1, T2, ...>, TMatchOutput> @unionVariant2,
        //     ...
        // );
        builder.AppendLine("    public abstract TMatchOutput Match<TState, TMatchOutput>(");
        builder.Append($"        TState state");
        builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variant = union.Variants[i];
            builder.Append($"        System.Func<TState, {variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.Append($", TMatchOutput> {variant.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("    );");

        // public abstract void Match<TState>(
        //     TState state,
        //     System.Action<TState, UnionVariant1<T1, T2, ...>> @unionVariant1,
        //     System.Action<TState, UnionVariant2<T1, T2, ...>> @unionVariant2,
        //     ...
        // );
        builder.AppendLine("    public abstract void Match<TState>(");
        builder.Append($"        TState state");
        builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variant = union.Variants[i];
            builder.Append($"        System.Action<TState, {variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.Append($"> {variant.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("    );");
        builder.AppendLine();

        return builder;
    }

    private static StringBuilder AppendAbstractSpecificMatchMethods(
        this StringBuilder builder,
        UnionDeclaration union
    )
    {
        foreach (var variant in union.Variants)
        {
            // public abstract TMatchOutput MatchSpecific<TMatchOutput>(
            //     System.Func<Specific<T1, T2, ...>, TMatchOutput> @specific,
            //     System.Func<TMatchOutput> @else
            // );
            builder.AppendLine(
                $"    public abstract TMatchOutput Match{variant.Identifier}<TMatchOutput>("
            );
            builder.Append($"        System.Func<{variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.AppendLine($", TMatchOutput> {variant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"        System.Func<TMatchOutput> @else");
            builder.AppendLine("    );");
        }

        builder.AppendLine();

        foreach (var variant in union.Variants)
        {
            // public abstract void MatchSpecific(
            //     System.Action<Specific<T1, T2, ...>> @specific,
            //     System.Action @else
            // );
            builder.AppendLine($"    public abstract void Match{variant.Identifier}(");
            builder.Append($"        System.Action<{variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.AppendLine($"> {variant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"        System.Action @else");
            builder.AppendLine("    );");
        }

        builder.AppendLine();

        foreach (var variant in union.Variants)
        {
            // public abstract TMatchOutput MatchSpecific<TState, TMatchOutput>(
            //     TState state,
            //     System.Func<TState, Specific<T1, T2, ...>, TMatchOutput> @specific,
            //     System.Func<TState, TMatchOutput> @else
            // );
            builder.AppendLine(
                $"    public abstract TMatchOutput Match{variant.Identifier}<TState, TMatchOutput>("
            );
            builder.Append($"        TState state");
            builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
            builder.Append($"        System.Func<TState, {variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.AppendLine($", TMatchOutput> {variant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"        System.Func<TState, TMatchOutput> @else");
            builder.AppendLine("    );");
        }

        builder.AppendLine();

        foreach (var variant in union.Variants)
        {
            // public abstract void MatchSpecific<TState>(
            //     TState state,
            //     System.Action<TState, Specific<T1, T2, ...>> @specific,
            //     System.Action<TState> @else
            // );
            builder.AppendLine($"    public abstract void Match{variant.Identifier}<TState>(");
            builder.Append($"        TState state");
            builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
            builder.Append($"        System.Action<TState, {variant.Identifier}");
            builder.AppendTypeParams(variant.TypeParameters);
            builder.AppendLine($"> {variant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"        System.Action<TState> @else");
            builder.AppendLine("    );");
        }

        builder.AppendLine();

        return builder;
    }

    private static StringBuilder AppendVariantMatchMethodImplementations(
        this StringBuilder builder,
        UnionDeclaration union,
        VariantDeclaration variant
    )
    {
        // public override TMatchOutput Match<TMatchOutput>(
        //     System.Func<UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariant1,
        //     System.Func<UnionVariant2<T1, T2, ...>, TMatchOutput> @unionVariant2,
        //     ...
        // ) => unionVariantX(this);
        builder.AppendLine("        public override TMatchOutput Match<TMatchOutput>(");
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variantParam = union.Variants[i];
            builder.Append($"            System.Func<{variantParam.Identifier}");
            builder.AppendTypeParams(variantParam.TypeParameters);
            builder.Append($", TMatchOutput> {variantParam.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine($"        ) => {variant.Identifier.ToMethodParameterCase()}(this);");

        // public override void Match(
        //     System.Action<UnionVariant1<T1, T2, ...>> @unionVariant1,
        //     System.Action<UnionVariant2<T1, T2, ...>> @unionVariant2,
        //     ...
        // ) => unionVariantX(this);
        builder.AppendLine("        public override void Match(");
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variantParam = union.Variants[i];
            builder.Append($"            System.Action<{variantParam.Identifier}");
            builder.AppendTypeParams(variantParam.TypeParameters);
            builder.Append($"> {variantParam.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine($"        ) => {variant.Identifier.ToMethodParameterCase()}(this);");

        // public override TMatchOutput Match<TState, TMatchOutput>(
        //     TState state,
        //     System.Func<TState, UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariant1,
        //     System.Func<TState, UnionVariant2<T1, T2, ...>, TMatchOutput> @unionVariant2,
        //     ...
        // ) => unionVariantX(state, this);
        builder.AppendLine("        public override TMatchOutput Match<TState, TMatchOutput>(");
        builder.Append($"        TState state");
        builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variantParam = union.Variants[i];
            builder.Append($"            System.Func<TState, {variantParam.Identifier}");
            builder.AppendTypeParams(variantParam.TypeParameters);
            builder.Append($", TMatchOutput> {variantParam.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine(
            $"        ) => {variant.Identifier.ToMethodParameterCase()}(state, this);"
        );

        // public override void Match<TState>(
        //     TState state,
        //     System.Action<TState, UnionVariant1<T1, T2, ...>> @unionVariant1,
        //     System.Action<TState, UnionVariant2<T1, T2, ...>> @unionVariant2,
        //     ...
        // ) => unionVariantX(state, this);
        builder.AppendLine("        public override void Match<TState>(");
        builder.Append($"        TState state");
        builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
        for (int i = 0; i < union.Variants.Count; ++i)
        {
            var variantParam = union.Variants[i];
            builder.Append($"            System.Action<TState, {variantParam.Identifier}");
            builder.AppendTypeParams(variantParam.TypeParameters);
            builder.Append($"> {variantParam.Identifier.ToMethodParameterCase()}");
            if (i < union.Variants.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine(
            $"        ) => {variant.Identifier.ToMethodParameterCase()}(state, this);"
        );

        return builder;
    }

    private static StringBuilder AppendVariantSpecificMatchMethodImplementations(
        this StringBuilder builder,
        UnionDeclaration union,
        VariantDeclaration variant
    )
    {
        // public override TMatchOutput MatchVariantX<TMatchOutput>(
        //     System.Func<UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariantX,
        //     System.Func<TMatchOutput> @else,
        //     ...
        // ) => unionVariantX(this);
        foreach (var specificVariant in union.Variants)
        {
            builder.AppendLine(
                $"        public override TMatchOutput Match{specificVariant.Identifier}<TMatchOutput>("
            );
            builder.Append($"            System.Func<{specificVariant.Identifier}");
            builder.AppendTypeParams(specificVariant.TypeParameters);
            builder.AppendLine(
                $", TMatchOutput> {specificVariant.Identifier.ToMethodParameterCase()},"
            );
            builder.AppendLine($"            System.Func<TMatchOutput> @else");
            builder.Append("        ) => ");
            if (specificVariant.Identifier == variant.Identifier)
            {
                builder.AppendLine($"{specificVariant.Identifier.ToMethodParameterCase()}(this);");
            }
            else
            {
                builder.AppendLine("@else();");
            }
        }

        // public override void MatchVariantX(
        //     System.Action<UnionVariant1<T1, T2, ...>> @unionVariantX,
        //     System.Action @else,
        //     ...
        // ) => unionVariantX(this);
        foreach (var specificVariant in union.Variants)
        {
            builder.AppendLine($"        public override void Match{specificVariant.Identifier}(");
            builder.Append($"            System.Action<{specificVariant.Identifier}");
            builder.AppendTypeParams(specificVariant.TypeParameters);
            builder.AppendLine($"> {specificVariant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"            System.Action @else");
            builder.Append("        ) => ");
            if (specificVariant.Identifier == variant.Identifier)
            {
                builder.AppendLine($"{specificVariant.Identifier.ToMethodParameterCase()}(this);");
            }
            else
            {
                builder.AppendLine("@else();");
            }
        }

        // public override TMatchOutput MatchVariantX<TState, TMatchOutput>(
        //     TState state,
        //     System.Func<TState, UnionVariant1<T1, T2, ...>, TMatchOutput> @unionVariantX,
        //     System.Func<TState, TMatchOutput> @else,
        //     ...
        // ) => unionVariantX(state, this);
        foreach (var specificVariant in union.Variants)
        {
            builder.AppendLine(
                $"        public override TMatchOutput Match{specificVariant.Identifier}<TState, TMatchOutput>("
            );
            builder.Append($"        TState state");
            builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
            builder.Append($"            System.Func<TState, {specificVariant.Identifier}");
            builder.AppendTypeParams(specificVariant.TypeParameters);
            builder.AppendLine(
                $", TMatchOutput> {specificVariant.Identifier.ToMethodParameterCase()},"
            );
            builder.AppendLine($"            System.Func<TState, TMatchOutput> @else");
            builder.Append("        ) => ");
            if (specificVariant.Identifier == variant.Identifier)
            {
                builder.AppendLine(
                    $"{specificVariant.Identifier.ToMethodParameterCase()}(state, this);"
                );
            }
            else
            {
                builder.AppendLine("@else(state);");
            }
        }

        // public override void MatchVariantX<TState>(
        //     TState state,
        //     System.Action<TState, UnionVariant1<T1, T2, ...>> @unionVariantX,
        //     System.Action<TState> @else,
        //     ...
        // ) => unionVariantX(state, this);
        foreach (var specificVariant in union.Variants)
        {
            builder.AppendLine(
                $"        public override void Match{specificVariant.Identifier}<TState>("
            );
            builder.Append($"        TState state");
            builder.AppendLine(union.Variants.Count > 0 ? "," : string.Empty);
            builder.Append($"            System.Action<TState, {specificVariant.Identifier}");
            builder.AppendTypeParams(specificVariant.TypeParameters);
            builder.AppendLine($"> {specificVariant.Identifier.ToMethodParameterCase()},");
            builder.AppendLine($"            System.Action<TState> @else");
            builder.Append("        ) => ");
            if (specificVariant.Identifier == variant.Identifier)
            {
                builder.AppendLine(
                    $"{specificVariant.Identifier.ToMethodParameterCase()}(state, this);"
                );
            }
            else
            {
                builder.AppendLine("@else(state);");
            }
        }

        builder.AppendLine();

        return builder;
    }

    private static StringBuilder AppendAbstractUnwrapMethods(
        this StringBuilder builder,
        UnionDeclaration union
    )
    {
        foreach (var variant in union.Variants)
        {
            // public abstract Variant UnwrapVariant();
            builder.AppendLine(
                $"    public abstract {variant.Identifier} Unwrap{variant.Identifier}();"
            );
        }

        builder.AppendLine();

        return builder;
    }

    private static StringBuilder AppendUnwrapMethodImplementations(
        this StringBuilder builder,
        UnionDeclaration union,
        VariantDeclaration variant
    )
    {
        foreach (var specificVariant in union.Variants)
        {
            builder.Append(
                $"        public override {specificVariant.Identifier} Unwrap{specificVariant.Identifier}() => "
            );

            if (specificVariant.Identifier == variant.Identifier)
            {
                builder.AppendLine("this;");
            }
            else
            {
                builder.AppendLine(
                    $"throw new System.InvalidOperationException($\"Called `{union.Name}.Unwrap{specificVariant.Identifier}()` on `{variant.Identifier}` value.\");"
                );
            }
        }

        return builder;
    }
}
