using FluentValidation;
using Karasu.ERP.Api.Controllers;
using Karasu.ERP.Application.Features.Products.Commands.CreateProduct;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Identity.Entities;
using NetArchTest.Rules;
using Xunit;

namespace Karasu.ERP.ArchitectureTests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_should_not_depend_on_other_layers()
    {
        var result = Types.InAssembly(typeof(Product).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Karasu.ERP.Application",
                "Karasu.ERP.Persistence",
                "Karasu.ERP.Infrastructure",
                "Karasu.ERP.Identity",
                "Karasu.ERP.Api",
                "Karasu.ERP.Shared")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_layers()
    {
        var result = Types.InAssembly(typeof(CreateProductCommandHandler).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Karasu.ERP.Persistence",
                "Karasu.ERP.Infrastructure",
                "Karasu.ERP.Identity",
                "Karasu.ERP.Api")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_persistence_or_api()
    {
        var result = Types.InAssembly(typeof(global::Karasu.ERP.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Karasu.ERP.Persistence",
                "Karasu.ERP.Api",
                "Karasu.ERP.Identity")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Persistence_should_not_depend_on_api_or_infrastructure()
    {
        var result = Types.InAssembly(typeof(global::Karasu.ERP.Persistence.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Karasu.ERP.Api",
                "Karasu.ERP.Infrastructure")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Identity_should_not_depend_on_persistence_or_api()
    {
        var result = Types.InAssembly(typeof(Permission).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Karasu.ERP.Persistence",
                "Karasu.ERP.Api",
                "Karasu.ERP.Infrastructure")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Controllers_should_reside_in_controllers_namespace()
    {
        var result = Types.InAssembly(typeof(ProductsController).Assembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespace("Karasu.ERP.Api.Controllers")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Handlers_should_have_handler_suffix()
    {
        var result = Types.InAssembly(typeof(CreateProductCommandHandler).Assembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Validators_should_inherit_abstract_validator()
    {
        var result = Types.InAssembly(typeof(CreateProductCommandHandler).Assembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        AssertArchRule(result);
    }

    [Fact]
    public void Aggregate_roots_should_reside_in_entities_namespace()
    {
        var result = Types.InAssembly(typeof(Product).Assembly)
            .That()
            .ImplementInterface(typeof(Domain.Common.IAggregateRoot))
            .Should()
            .ResideInNamespace("Karasu.ERP.Domain.Entities")
            .GetResult();

        AssertArchRule(result);
    }

    private static void AssertArchRule(TestResult result)
    {
        var failing = result.FailingTypes?.Select(t => t.FullName) ?? [];
        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, failing));
    }
}
