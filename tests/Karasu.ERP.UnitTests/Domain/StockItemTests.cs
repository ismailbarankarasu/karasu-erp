using FluentAssertions;
using Karasu.ERP.Domain.Entities;
using Xunit;

namespace Karasu.ERP.UnitTests.Domain;

public class StockItemTests
{
    [Fact]
    public void Deduct_should_reduce_quantity_when_sufficient_stock()
    {
        var item = new StockItem { Quantity = 10, ReservedQuantity = 0 };

        item.Deduct(3);

        item.Quantity.Should().Be(7);
    }

    [Fact]
    public void Deduct_should_throw_when_insufficient_stock()
    {
        var item = new StockItem { Quantity = 2, ReservedQuantity = 0 };

        var act = () => item.Deduct(3);

        act.Should().Throw<InvalidOperationException>().WithMessage("Yetersiz stok.");
    }

    [Fact]
    public void Restore_should_increase_quantity()
    {
        var item = new StockItem { Quantity = 5, ReservedQuantity = 0 };

        item.Restore(4);

        item.Quantity.Should().Be(9);
    }

    [Fact]
    public void Adjust_should_not_allow_negative_result()
    {
        var item = new StockItem { Quantity = 2, ReservedQuantity = 0 };

        var act = () => item.Adjust(-5);

        act.Should().Throw<InvalidOperationException>();
    }
}
