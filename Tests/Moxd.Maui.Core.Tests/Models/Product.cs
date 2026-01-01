namespace Moxd.Maui.Core.Tests.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public string Category { get; set; } = "";
}