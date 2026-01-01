using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Moxd.Maui.Sample.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [RelayCommand]
    private async Task NavigateToCollectionDemoAsync()
    {
        await Shell.Current.GoToAsync("//CollectionDemoPage");
    }

    [RelayCommand]
    private async Task NavigateToUtilitiesDemoAsync()
    {
        await Shell.Current.GoToAsync("//UtilitiesDemoPage");
    }
}