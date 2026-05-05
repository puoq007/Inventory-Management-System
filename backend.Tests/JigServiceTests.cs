using System;
using System.Threading.Tasks;
using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using shared.Models;
using Xunit;

namespace backend.Tests;

public class JigServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly JigService _service;

    public JigServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new AppDbContext(options);
        _service = new JigService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GenerateNextJigId_WithNewToolNo_Returns01Suffix()
    {
        // Act
        var result = await _service.GenerateNextJigId("T100");

        // Assert
        Assert.Equal("T100-01", result);
    }

    [Fact]
    public async Task GenerateNextJigId_WithExistingToolNo_IncrementsCorrectly()
    {
        // Arrange
        _context.Jigs.Add(new Jig { Uid = Guid.NewGuid().ToString(), Id = "T100-01", ToolNo = "T100" });
        _context.Jigs.Add(new Jig { Uid = Guid.NewGuid().ToString(), Id = "T100-05", ToolNo = "T100" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateNextJigId("T100");

        // Assert
        Assert.Equal("T100-06", result); // Next max suffix is 05 + 1 = 06
    }

    [Fact]
    public async Task GenerateNextJigId_WithNullToolNo_GeneratesRandomJigId()
    {
        // Act
        var result = await _service.GenerateNextJigId(null);

        // Assert
        Assert.StartsWith("JIG-", result);
        Assert.True(result.Length > 4); // Random part length
    }

    [Fact]
    public void CleanAllSpaces_RemovesAllWhitespace()
    {
        // Act
        var result = _service.CleanAllSpaces("  A B \t C \n D  ");

        // Assert
        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void NormalizeSpaces_ReplacesMultipleSpacesWithSingle()
    {
        // Act
        var result = _service.NormalizeSpaces("  Hello    World  \n\t Test  ");

        // Assert
        Assert.Equal("Hello World Test", result);
    }
}
