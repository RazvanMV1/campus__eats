using CampusEats.Backend.Common;
using FluentAssertions;

namespace CampusEats.Tests.Common;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_CreatesWithCorrectProperties()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var result = new PagedResult<string>
        {
            Items = items,
            CurrentPage = 2,
            PageSize = 10,
            TotalCount = 25
        };

        // Assert
        result.Items.Should().HaveCount(3);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public void PagedResult_EmptyItems_Works()
    {
        // Act
        var result = new PagedResult<int>
        {
            Items = new List<int>(),
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 0
        };

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
