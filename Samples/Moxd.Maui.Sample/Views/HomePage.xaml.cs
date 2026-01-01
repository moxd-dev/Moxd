using Moxd.Maui.Sample.ViewModels;

namespace Moxd.Maui.Sample.Views;

public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
}