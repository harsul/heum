using System.Reflection;
using Heum.Application;
using Heum.Contracts.Events;
using Heum.Data;
using Heum.Infrastructure.Messaging;
using Heum.Server.Common;
using NetArchTest.Rules;

namespace Heum.Server.xUnit;

/// <summary>
/// Enforces the layering described in CLAUDE.md:
///   Contracts → no dependencies
///   Application → no dependencies on Data/Infrastructure/Server
///   Data → no dependencies on Server/Infrastructure
///   Infrastructure → no dependencies on Server
///   Server endpoints → depend only on service interfaces, not concrete implementations
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly ContractsAssembly = typeof(IDomainEvent).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICurrentUserService).Assembly;
    private static readonly Assembly DataAssembly = typeof(HeumDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(EventTopicRegistry).Assembly;
    private static readonly Assembly ServerAssembly = typeof(PagedResponse<>).Assembly;

    [Fact]
    public void Contracts_HasNoDependencyOnOtherHeumProjects()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny("Heum.Data", "Heum.Infrastructure", "Heum.Server", "Heum.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_HasNoDependencyOnDataInfrastructureOrServer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("Heum.Data", "Heum.Infrastructure", "Heum.Server")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Data_HasNoDependencyOnServerOrInfrastructure()
    {
        var result = Types.InAssembly(DataAssembly)
            .Should()
            .NotHaveDependencyOnAny("Heum.Server", "Heum.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_HasNoDependencyOnServer()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("Heum.Server")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ServerEndpoints_DoNotDependOnConcreteServiceImplementations()
    {
        // Concrete service types: name ends with "Service", not an interface (doesn't start with "I")
        var concreteServiceTypes = Types.InAssembly(ServerAssembly)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotInterfaces()
            .GetTypes()
            .Select(t => t.FullName!)
            .ToArray();

        if (concreteServiceTypes.Length == 0)
            return; // nothing to check

        var result = Types.InAssembly(ServerAssembly)
            .That()
            .ResideInNamespaceEndingWith("Endpoints")
            .Should()
            .NotHaveDependencyOnAny(concreteServiceTypes)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }
}
