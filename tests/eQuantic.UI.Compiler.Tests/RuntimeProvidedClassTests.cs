using eQuantic.UI.Compiler.Services;
using FluentAssertions;
using Xunit;

namespace eQuantic.UI.Compiler.Tests;

/// <summary>
/// <c>[RuntimeProvided]</c> types are supplied by <c>@equantic/runtime</c>, including when eqc has
/// no project semantic model and must route imports from the dependency scan alone.
/// </summary>
public class RuntimeProvidedClassTests
{
    [Fact]
    public void RuntimeProvidedStaticHelper_FallbackImportsRuntime_NotLocalModule()
    {
        var dir = Path.Combine(Path.GetTempPath(), "eq-runtime-provided-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "ButtonStyles.cs"), """
                [RuntimeProvided]
                public static class ButtonStyles
                {
                    public const int MinWidth = 64;
                }
                """);
            var probePath = Path.Combine(dir, "Probe.cs");
            File.WriteAllText(probePath, """
                public class Probe
                {
                    public int Build() => ButtonStyles.MinWidth;
                }
                """);

            var resolver = new ComponentDependencyResolver();
            resolver.ScanSourceDirectories([dir]);
            resolver.GetAllStaticHelpers().Should().NotContain("ButtonStyles");

            var compiler = new ComponentCompiler { SymbolsAreAuthoritative = false };
            compiler.SetDependencyResolver(resolver);
            var probe = compiler.CompileFile(probePath).Single(result => result.ComponentName == "Probe");

            probe.Success.Should().BeTrue(string.Join("; ", probe.Errors.Select(error => error.Message)));
            probe.TypeScript.Should().Contain("ButtonStyles");
            probe.TypeScript.Should().Contain("from \"@equantic/runtime\"");
            probe.TypeScript.Should().NotContain("from \"./ButtonStyles\"");

            File.WriteAllText(Path.Combine(dir, "ButtonStyles.cs"), """
                public static class ButtonStyles
                {
                    public const int MinWidth = 64;
                }
                """);
            var withoutAttribute = new ComponentDependencyResolver();
            withoutAttribute.ScanSourceDirectories([dir]);
            withoutAttribute.GetAllStaticHelpers().Should().Contain("ButtonStyles");

            var fallbackCompiler = new ComponentCompiler { SymbolsAreAuthoritative = false };
            fallbackCompiler.SetDependencyResolver(withoutAttribute);
            var fallbackProbe = fallbackCompiler.CompileFile(probePath)
                .Single(result => result.ComponentName == "Probe");
            fallbackProbe.TypeScript.Should().Contain("from \"./ButtonStyles\"");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
