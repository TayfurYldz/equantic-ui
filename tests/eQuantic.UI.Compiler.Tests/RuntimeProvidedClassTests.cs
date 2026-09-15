using eQuantic.UI.Compiler.Services;
using FluentAssertions;
using Xunit;

namespace eQuantic.UI.Compiler.Tests;

/// <summary>
/// <c>[RuntimeProvided]</c> static helpers are excluded from generated local modules, including
/// when eqc has no project semantic model and must fall back to the dependency scan.
/// </summary>
public class RuntimeProvidedClassTests
{
    [Fact]
    public void RuntimeProvidedStaticHelper_FallbackResolverDoesNotRegisterLocalHelper()
    {
        var dir = Path.Combine(Path.GetTempPath(), "eq-runtime-provided-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var helperPath = Path.Combine(dir, "ButtonStyles.cs");
            File.WriteAllText(helperPath, """
                [RuntimeProvided]
                public static class ButtonStyles
                {
                    public const int MinWidth = 64;
                }
                """);

            var resolver = new ComponentDependencyResolver();
            resolver.ScanSourceDirectories([dir]);
            resolver.GetAllStaticHelpers().Should().NotContain("ButtonStyles");

            File.WriteAllText(helperPath, """
                public static class ButtonStyles
                {
                    public const int MinWidth = 64;
                }
                """);

            var withoutAttribute = new ComponentDependencyResolver();
            withoutAttribute.ScanSourceDirectories([dir]);
            withoutAttribute.GetAllStaticHelpers().Should().Contain("ButtonStyles");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

}
