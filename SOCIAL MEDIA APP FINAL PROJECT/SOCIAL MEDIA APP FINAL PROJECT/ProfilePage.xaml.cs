using SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT;

public partial class profile : ContentPage
{
	public profile()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProfileViewModel vm)
        {
            vm.LoadCurrentUser();
        }
    }
}