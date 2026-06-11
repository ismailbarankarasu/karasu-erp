using FluentAssertions;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Domain.Events;
using Xunit;

namespace Karasu.ERP.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void AddLine_should_recalculate_totals()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), null, "ORD-001");
        order.AddLine(Guid.NewGuid(), quantity: 2, unitPrice: 100, taxRate: 20);

        order.SubTotal.Should().Be(200);
        order.TaxTotal.Should().Be(40);
        order.GrandTotal.Should().Be(240);
        order.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void Confirm_should_raise_OrderConfirmedEvent()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), null, "ORD-002");
        order.AddLine(Guid.NewGuid(), 1, 50, 20);

        order.Confirm(Guid.NewGuid());

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void AddLine_should_throw_when_order_not_draft()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), null, "ORD-003");
        order.AddLine(Guid.NewGuid(), 1, 50, 20);
        order.Confirm(Guid.NewGuid());

        var act = () => order.AddLine(Guid.NewGuid(), 1, 10, 20);

        act.Should().Throw<InvalidOperationException>();
    }
}
