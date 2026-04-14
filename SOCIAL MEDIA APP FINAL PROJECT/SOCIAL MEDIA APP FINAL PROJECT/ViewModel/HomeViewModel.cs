using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel
{
    public class PostModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("authorName")]
        public string AuthorName { get; set; } = string.Empty;

        [JsonPropertyName("authorAvatar")]
        public string AuthorAvatar { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("likesCount")]
        public string LikesCountRaw { get; set; } = "0";

        [JsonPropertyName("commentsCount")]
        public string CommentsCountRaw { get; set; } = "0";

        [JsonPropertyName("isLiked")]
        public bool IsLiked { get; set; }

        public int LikesCount
        {
            get => (int)(double.TryParse(LikesCountRaw, out var n) ? n : 0);
            set => LikesCountRaw = value.ToString();
        }

        public int CommentsCount
        {
            get => (int)(double.TryParse(CommentsCountRaw, out var n) ? n : 0);
            set => CommentsCountRaw = value.ToString();
        }

        public bool HasImage =>
            !string.IsNullOrWhiteSpace(ImageUrl) &&
            Uri.TryCreate(ImageUrl, UriKind.Absolute, out _);

        public string TimeAgo
        {
            get
            {
                if (!DateTime.TryParse(CreatedAt, out var dt)) return "Recently";
                var diff = DateTime.UtcNow - dt.ToUniversalTime();
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }
    }

    public class HomeViewModel : INotifyPropertyChanged
    {
        // ✅ Correct URL — no /v1
        private const string BaseUrl = "https://69dbab37560857310a07e486.mockapi.io/api";

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }

        public bool IsNotBusy => !_isBusy;

        public string AvatarUrl => Preferences.Get("user_avatar", string.Empty);
        public string CurrentUserName => Preferences.Get("user_name", "You");

        public ObservableCollection<PostModel> Posts { get; } = new();

        // ✅ Only ONE definition of each command
        public ICommand RefreshCommand { get; }
        public ICommand LikePostCommand { get; }
        public ICommand CreatePostCommand { get; }
        public ICommand GoToProfileCommand { get; }

        public HomeViewModel()
        {
            RefreshCommand = new Command(async () => await LoadPostsAsync());
            LikePostCommand = new Command<PostModel>(OnLikePost);

            // ✅ Navigates to CreatePostPage (registered route, no // prefix)
            CreatePostCommand = new Command(async () =>
                await Shell.Current.GoToAsync("CreatePostPage"));

            // ✅ Correct profile route
            GoToProfileCommand = new Command(async () =>
                await Shell.Current.GoToAsync("//ProfilePage"));

            // Initial load — will be called again by OnAppearing anyway
            Task.Run(async () => await LoadPostsAsync());
        }

        // ✅ Called by HomePage.xaml.cs OnAppearing every time page is shown
        public async Task ReloadAsync()
        {
            await LoadPostsAsync();
        }

        private async Task LoadPostsAsync()
        {
            IsBusy = true;
            try
            {
                using var client = new HttpClient();

                // ✅ Full correct URL
                var url = $"{BaseUrl}/posts";
                var response = await client.GetAsync(url);

                System.Diagnostics.Debug.WriteLine($"[Home] GET {url} → {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("[Home] Non-success, loading mock posts");
                    MainThread.BeginInvokeOnMainThread(LoadMockPosts);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Home] JSON: {json[..Math.Min(200, json.Length)]}");

                var posts = JsonSerializer.Deserialize<List<PostModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Posts.Clear();
                    if (posts == null || posts.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[Home] No posts from API, loading mock");
                        LoadMockPosts();
                        return;
                    }
                    foreach (var post in posts.OrderByDescending(p => p.CreatedAt))
                        Posts.Add(post);

                    System.Diagnostics.Debug.WriteLine($"[Home] Loaded {Posts.Count} posts");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Home] LoadPosts ERROR: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(LoadMockPosts);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadMockPosts()
        {
            Posts.Clear();
            Posts.Add(new PostModel
            {
                Id = "mock1",
                AuthorName = "James Rivera",
                AuthorAvatar = "https://i.pravatar.cc/150?img=1",
                Content = "Just shipped a new feature! The dark theme is looking 🔥",
                LikesCountRaw = "128",
                CommentsCountRaw = "24",
                CreatedAt = DateTime.UtcNow.AddHours(-2).ToString("o")
            });
            Posts.Add(new PostModel
            {
                Id = "mock2",
                AuthorName = "Sofia Chen",
                AuthorAvatar = "https://i.pravatar.cc/150?img=5",
                Content = "Exploring .NET MAUI for cross-platform development! 🚀 #dotnet #maui",
                LikesCountRaw = "87",
                CommentsCountRaw = "11",
                CreatedAt = DateTime.UtcNow.AddDays(-1).ToString("o")
            });
            Posts.Add(new PostModel
            {
                Id = "mock3",
                AuthorName = "Marco Diaz",
                AuthorAvatar = "https://i.pravatar.cc/150?img=8",
                Content = "Code is poetry. Keep building! 💚",
                LikesCountRaw = "213",
                CommentsCountRaw = "45",
                CreatedAt = DateTime.UtcNow.AddDays(-3).ToString("o")
            });
        }

        private void OnLikePost(PostModel? post)
        {
            if (post == null) return;
            post.IsLiked = !post.IsLiked;
            post.LikesCount += post.IsLiked ? 1 : -1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}