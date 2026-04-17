using SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            BindingContext = new HomeViewModel();
        }

  
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is HomeViewModel vm)
                await vm.ReloadAsync();
        }
    }
}