using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Moxd.Maui.Sample.Views;
using Moxd.Maui.Sample.ViewModels;

namespace Moxd.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<CollectionDemoPage>();
        builder.Services.AddTransient<CollectionDemoViewModel>();
        builder.Services.AddTransient<UtilitiesDemoPage>();
        builder.Services.AddTransient<UtilitiesDemoViewModel>();

		return builder.Build();
	}
}