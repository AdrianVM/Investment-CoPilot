using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace InvestmentCoPilot.ArchitectureTests;

/// <summary>
/// User data scoping relies on EF Core global query filters. Bypassing them (or querying with raw SQL, which they
/// do not apply to) is allowed only in SharedKernel and Identity, which own the scoping logic.
/// </summary>
public class DataAccessTests
{
    private static readonly HashSet<string> FilterBypassingMethods =
    [
        "IgnoreQueryFilters",
        "FromSql",
        "FromSqlRaw",
        "FromSqlInterpolated",
        "SqlQuery",
        "SqlQueryRaw",
        "ExecuteSql",
        "ExecuteSqlRaw",
        "ExecuteSqlAsync",
        "ExecuteSqlRawAsync",
        "ExecuteSqlInterpolated",
        "ExecuteSqlInterpolatedAsync",
    ];

    public static TheoryData<string> RestrictedAssemblies()
    {
        var data = new TheoryData<string>();
        foreach (var module in Modules.All.Where(m => m != Modules.Identity))
        {
            data.Add(module.Implementation.GetName().Name!);
            data.Add(module.Contracts.GetName().Name!);
        }

        data.Add(Modules.Identity.Contracts.GetName().Name!);
        data.Add(Modules.Host.GetName().Name!);
        return data;
    }

    [Theory]
    [MemberData(nameof(RestrictedAssemblies))]
    public void Assembly_does_not_bypass_query_filters_or_use_raw_sql(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        using var definition = AssemblyDefinition.ReadAssembly(assembly.Location);

        var violations = (
            from type in definition.MainModule.GetTypes()
            from method in type.Methods
            where method.HasBody
            from instruction in method.Body.Instructions
            where instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt
            let called = instruction.Operand as MethodReference
            where called is not null
                && called.DeclaringType.Namespace.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                && FilterBypassingMethods.Contains(called.Name)
            select $"{type.FullName}.{method.Name} calls {called.Name}").ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
