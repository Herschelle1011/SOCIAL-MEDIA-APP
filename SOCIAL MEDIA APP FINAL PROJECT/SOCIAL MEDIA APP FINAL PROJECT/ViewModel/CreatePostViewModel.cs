using Microsoft.Maui.Graphics.Platform;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel
{
    public class CreatePostViewModel : INotifyPropertyChanged
    {
        private const string BaseUrl = "https://69dbab37560857310a07e486.mockapi.io/api";

        private string _postContent = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isBusy;
        private bool _hasImage;
        private ImageSource? _pickedImageSource;
        private string _pickedImageBase64 = string.Empty; // ✅ store base64, not path

        // ✅ Fixed: use "user_username" to match what LoginAsync saves
        public string AuthorName => Preferences.Get("user_username", "Anonymous");
        public string AvatarUrl => Preferences.Get("user_avatar", string.Empty);

        public string PostContent
        {
            get => _postContent;
            set
            {
                _postContent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CharacterCount));
                OnPropertyChanged(nameof(CanPost));
            }
        }

        public int CharacterCount => _postContent.Length;
        public bool CanPost => !string.IsNullOrWhiteSpace(_postContent) && !_isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanPost)); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public bool HasImage
        {
            get => _hasImage;
            set { _hasImage = value; OnPropertyChanged(); }
        }

        public ImageSource? PickedImageSource
        {
            get => _pickedImageSource;
            set { _pickedImageSource = value; OnPropertyChanged(); }
        }

        public ICommand SubmitPostCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PickImageCommand { get; }
        public ICommand RemoveImageCommand { get; }

        public CreatePostViewModel()
        {
            SubmitPostCommand = new Command(async () => await SubmitPostAsync());
            CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            PickImageCommand = new Command(async () => await PickImageAsync());
            RemoveImageCommand = new Command(() =>
            {
                PickedImageSource = null;
                _pickedImageBase64 = string.Empty;
                HasImage = false;
            });
        }

        // ─── Submit Post ─────────────────────────────────────────────────────
        private async Task SubmitPostAsync()
        {
            if (string.IsNullOrWhiteSpace(PostContent))
            {
                ErrorMessage = "Please write something before posting.";
                return;
            }

            if (PostContent.Length > 500)
            {
                ErrorMessage = "Post content cannot exceed 500 characters.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                using var client = new HttpClient();

                var newPost = new
                {
                    authorName = AuthorName,
                    authorAvatar = AvatarUrl,
                    content = PostContent,
                    imageUrl = _pickedImageBase64, // ✅ base64 data URL or empty string
                    likesCount = "0",
                    commentsCount = "0",
                    isLiked = false,
                    createdAt = DateTime.UtcNow.ToString("o")
                };

                var json = JsonSerializer.Serialize(newPost);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{BaseUrl}/posts";

                System.Diagnostics.Debug.WriteLine($"[CreatePost] POST {url}");

                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[CreatePost] Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[CreatePost] Response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Failed to post ({(int)response.StatusCode}). Try again.";
                    return;
                }

                await Shell.Current.DisplayAlert("Posted!", "Your post was shared! 🎉", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CreatePost] Exception: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ─── Pick & Compress Image ────────────────────────────────────────────
        private async Task PickImageAsync()
        {
            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Choose a photo for your post"
                });

                if (result == null) return;

                // Read bytes
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // ✅ Compress before storing (keeps MockAPI happy)
                var compressedBytes = await CompressImageAsync(imageBytes, maxWidthHeight: 600, quality: 65);

                // ✅ Store as base64 data URL
                var base64 = Convert.ToBase64String(compressedBytes);
                var mimeType = result.ContentType ?? "image/jpeg";
                _pickedImageBase64 = $"data:{mimeType};base64,{base64}";

                // Show preview
                PickedImageSource = ImageSource.FromStream(() => new MemoryStream(compressedBytes));
                HasImage = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Could not pick image: {ex.Message}";
            }
        }

        // ─── Compress helper ─────────────────────────────────────────────────
        private async Task<byte[]> CompressImageAsync(byte[] imageBytes, int maxWidthHeight, int quality)
        {
            using var inputStream = new MemoryStream(imageBytes);
            var originalImage = PlatformImage.FromStream(inputStream);

            float scale = Math.Min(
                maxWidthHeight / (float)originalImage.Width,
                maxWidthHeight / (float)originalImage.Height);

            if (scale >= 1f) scale = 1f; // never upscale

            int newWidth = (int)(originalImage.Width * scale);
            int newHeight = (int)(originalImage.Height * scale);

            var resized = originalImage.Resize(newWidth, newHeight, ResizeMode.Fit);

            using var outputStream = new MemoryStream();
            await resized.SaveAsync(outputStream, ImageFormat.Jpeg, quality / 100f);
            return outputStream.ToArray();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}