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

        [JsonPropertyName("isLiked")]
        public bool IsLiked { get; set; }

        [JsonPropertyName("likesCount")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int LikesCount { get; set; }

        [JsonPropertyName("commentsCount")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int CommentsCount { get; set; }

        public bool HasImage =>
            !string.IsNullOrWhiteSpace(ImageUrl) &&
            (ImageUrl.StartsWith("http") || ImageUrl.StartsWith("data:image"));

        public ImageSource? PostImageSource
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageUrl)) return null;

                if (ImageUrl.StartsWith("data:image"))
                {
                    try
                    {
                        var base64 = ImageUrl.Substring(ImageUrl.IndexOf(",") + 1);
                        var bytes = Convert.FromBase64String(base64);
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch { return null; }
                }

                if (ImageUrl.StartsWith("http"))
                    return ImageSource.FromUri(new Uri(ImageUrl));

                return null;
            }
        }

        public ImageSource? AuthorAvatarSource
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AuthorAvatar))
                    return null;

                try
                {
                    // base64 image
                    if (AuthorAvatar.StartsWith("data:image"))
                    {
                        var base64 = AuthorAvatar.Substring(AuthorAvatar.IndexOf(",") + 1);
                        var bytes = Convert.FromBase64String(base64);
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }

                    // valid URL
                    if (Uri.TryCreate(AuthorAvatar, UriKind.Absolute, out var uri))
                    {
                        return ImageSource.FromUri(uri);
                    }
                }
                catch
                {
                    // ignore bad images
                }

                return null;
            }
        }

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

    // ✅ Handles int, string, and decimal safely
    public class FlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var intVal))
                    return intVal;

                if (reader.TryGetDouble(out var doubleVal))
                    return (int)doubleVal;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();

                if (int.TryParse(str, out var intVal))
                    return intVal;

                if (double.TryParse(str, out var doubleVal))
                    return (int)doubleVal;
            }

            return 0;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public class HomeViewModel : INotifyPropertyChanged
    {
        private const string BaseUrl = "https://69dbab37560857310a07e486.mockapi.io/api";

        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }

        public bool IsNotBusy => !_isBusy;

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PostModel> Posts { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand LikePostCommand { get; }
        public ICommand CreatePostCommand { get; }
        public ICommand GoToProfileCommand { get; }

        public HomeViewModel()
        {
            RefreshCommand = new Command(async () => await LoadPostsAsync());
            LikePostCommand = new Command<PostModel>(OnLikePost);
            CreatePostCommand = new Command(async () =>
                await Shell.Current.GoToAsync("CreatePostPage"));

            GoToProfileCommand = new Command(async () =>
            {
                await Shell.Current.GoToAsync("//profile");
            });
        }

        public async Task ReloadAsync() => await LoadPostsAsync();

        private async Task LoadPostsAsync()
        {
            IsBusy = true;

            try
            {
                using var client = new HttpClient();

                var response = await client.GetAsync($"{BaseUrl}/posts");

                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();

                var posts = JsonSerializer.Deserialize<List<PostModel>>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Posts.Clear();

                    if (posts == null) return;

                    foreach (var post in posts.OrderByDescending(p =>
                        DateTime.TryParse(p.CreatedAt, out var dt) ? dt : DateTime.MinValue))
                    {
                        Posts.Add(post);
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading posts";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
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