namespace SOCIAL_MEDIA_APP_FINAL_PROJECT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("CreatePostPage", typeof(CreatePostPage));
            Routing.RegisterRoute("HomePage", typeof(HomePage));
            Routing.RegisterRoute("profile", typeof(profile));
            Routing.RegisterRoute("CreatePostPage", typeof(CreatePostPage));

        }
    }
}
