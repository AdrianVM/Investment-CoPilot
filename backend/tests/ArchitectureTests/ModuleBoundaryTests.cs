using NetArchTest.Rules;

namespace InvestmentCoPilot.ArchitectureTests;

/// <summary>
/// Modules may talk to each other only through their <c>*.Contracts</c> projects, and never through each other's data.
/// </summary>
public class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules.Pairs), MemberType = typeof(Modules))]
    public void Module_does_not_reference_another_modules_implementation(string moduleName, string otherName)
    {
        var module = Modules.Named(moduleName);
        var other = Modules.Named(otherName);

        var referenced = module.Implementation.GetReferencedAssemblies().Select(a => a.Name);

        Assert.DoesNotContain(other.Implementation.GetName().Name, referenced);
    }

    [Theory]
    [MemberData(nameof(Modules.Names), MemberType = typeof(Modules))]
    public void Contracts_do_not_reference_any_module_implementation(string moduleName)
    {
        var contracts = Modules.Named(moduleName).Contracts;
        var referenced = contracts.GetReferencedAssemblies().Select(a => a.Name).ToList();

        foreach (var module in Modules.All)
        {
            Assert.DoesNotContain(module.Implementation.GetName().Name, referenced);
        }
    }

    [Theory]
    [MemberData(nameof(Modules.Pairs), MemberType = typeof(Modules))]
    public void Module_does_not_use_another_modules_persistence(string moduleName, string otherName)
    {
        var module = Modules.Named(moduleName);
        var other = Modules.Named(otherName);

        var result = Types.InAssembly(module.Implementation)
            .ShouldNot()
            .HaveDependencyOn(other.PersistenceNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{module} uses {other}'s persistence types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Shared_kernel_does_not_depend_on_any_module()
    {
        var referenced = Modules.SharedKernel.GetReferencedAssemblies().Select(a => a.Name).ToList();

        foreach (var module in Modules.All)
        {
            Assert.DoesNotContain(module.Implementation.GetName().Name, referenced);
            Assert.DoesNotContain(module.Contracts.GetName().Name, referenced);
        }
    }
}
