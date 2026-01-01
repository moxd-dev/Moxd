using CommunityToolkit.Mvvm.ComponentModel;

namespace Moxd.Maui.Sample.Models;

/// <summary>
/// A sample item used for performance testing.
/// </summary>
public partial class TestItem : ObservableObject
{
    #region Bindable Properties
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal Value { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial DateTime LastUpdated { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;
    #endregion

    #region Static Methods
    public static TestItem Create(int id)
    {
        return new TestItem
        {
            Id = id,
            Name = $"Item {id}",
            Description = $"Description for item {id}",
            Category = $"Category {id % 10}",
            Value = id * 1.5m,
            IsActive = id % 2 == 0,
            LastUpdated = DateTime.Now,
            Status = "Active"
        };
    }

    public void UpdateAllProperties(string suffix)
    {
        Name = $"Updated Item {Id} {suffix}";
        Description = $"Updated description {suffix}";
        Category = $"Updated Category {suffix}";
        Value = Value + 100;
        IsActive = !IsActive;
        LastUpdated = DateTime.Now;
        Status = $"Updated {suffix}";
    }
    #endregion
}