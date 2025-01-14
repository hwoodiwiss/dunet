﻿using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Dunet.Generator.UnionGeneration;

namespace Dunet.Benchmark;

[MemoryDiagnoser]
[InProcess]
public class SourceGeneratorBenchmarks
{
    const string sourceText =
        """
        using Dunet;

        [Union]
        public partial record Expression
        {
            partial record Number(int Value);

            partial record Add(Expression Left, Expression Right);

            partial record Multiply(Expression Left, Expression Right);

            partial record Variable(string Value);
        }

        [Union]
        partial record Shape
        {
            partial record Circle(double Radius);

            partial record Rectangle(double Length, double Width);

            partial record Triangle(double Base, double Height);
        }

        [Union]
        public partial record Option<T>
        {
            public static implicit operator Option<T>(T value) => new Some(value);

            partial record Some(T Value);

            partial record None();
        }
        """;
        
    private GeneratorDriver? _driver;
    private Compilation? _compilation;

    private (Compilation, CSharpGeneratorDriver) Setup(string source)
    {
        var compilation = CreateCompilation(source);
        if (compilation == null)
            throw new InvalidOperationException("Compilation returned null");

        var unionGenerator = new UnionGenerator();

        var driver = CSharpGeneratorDriver.Create(unionGenerator);
        
        return (compilation, driver);
    }

    [GlobalSetup(Target = nameof(Compile))]
    public void SetupCompile() => (_compilation, _driver) = Setup(sourceText);
    
    [GlobalSetup(Target = nameof(Cached))]
    public void SetupCached()
    {
        (_compilation, var driver) = Setup(sourceText);
        _driver = driver.RunGenerators(_compilation);
    }

    [Benchmark]
    public GeneratorDriver Compile() => _driver!.RunGeneratorsAndUpdateCompilation(_compilation!, out _, out _);
    
    [Benchmark]
    public GeneratorDriver Cached() => _driver!.RunGeneratorsAndUpdateCompilation(_compilation!, out _, out _);

    private static Compilation CreateCompilation(params string[] sources) =>
        CSharpCompilation.Create(
            "compilation",
            sources.Select(static source => CSharpSyntaxTree.ParseText(source)),
            new[]
            {
                // Resolves to System.Private.CoreLib.dll
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                // Resolves to System.Runtime.dll, which is needed for the Attribute type
                // Can't use typeof(Attribute).GetTypeInfo().Assembly.Location because it resolves to System.Private.CoreLib.dll
                MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().First(f => f.FullName?.Contains("System.Runtime") == true).Location),
                MetadataReference.CreateFromFile(typeof(UnionAttribute).GetTypeInfo().Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );
}