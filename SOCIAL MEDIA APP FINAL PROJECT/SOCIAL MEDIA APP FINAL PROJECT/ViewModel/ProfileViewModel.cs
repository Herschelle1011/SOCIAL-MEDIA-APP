using Microsoft.Maui.Graphics.Platform;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private const string BaseUrl = "https://69dbab37560857310a07e486.mockapi.io/api";

    

        // ─── Backing fields ──────────────────────────────────────────────────
        private bool _isEditing;
        private bool _isBusy;
        private bool _passwordHidden = true;
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _avatarUrl = string.Empty;
        private string _avatarEmoji = "👤";
        private string _avatarImageSource = string.Empty;

        private string _statusMessage = string.Empty;
        private string _statusColor = "#06C167";
        private string _userId = string.Empty;

       
        // ─── Properties ──────────────────────────────────────────────────────
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotEditing));
                OnPropertyChanged(nameof(EditButtonText));
            }
        }

        public bool IsNotEditing => !_isEditing;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }

        public bool IsNotBusy => !_isBusy;

        public bool PasswordHidden
        {
            get => _passwordHidden;
            set { _passwordHidden = value; OnPropertyChanged(); OnPropertyChanged(nameof(PasswordToggleIcon)); }
        }

        public string PasswordToggleIcon => _passwordHidden ? "👁" : "🙈";

        public string EditButtonText => _isEditing ? "Cancel" : "Edit";

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayEmail)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string AvatarUrl
        {
            get => _avatarUrl;
            set
            {
                _avatarUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAvatarImage));
                OnPropertyChanged(nameof(HasNoAvatarImage));
                OnPropertyChanged(nameof(AvatarImageSource));
            }
        }

        private void LoadUser()
        {
            AvatarUrl = Preferences.Get("user_avatar", "");
        }


        public string AvatarEmoji
        {
            get => _avatarEmoji;
            set { _avatarEmoji = value; OnPropertyChanged(); }
        }

        // Add this property to ProfileViewModel.cs
        public ImageSource? AvatarImageSource
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_avatarUrl))
                    return null;

                // Handle Base64 data URLs
                if (_avatarUrl.StartsWith("data:image"))
                {
                    try
                    {
                        var base64 = _avatarUrl.Substring(_avatarUrl.IndexOf(",") + 1);
                        var bytes = Convert.FromBase64String(base64);
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch { return null; }
                }

                // Handle normal http URLs
                if (_avatarUrl.StartsWith("http"))
                    return ImageSource.FromUri(new Uri(_avatarUrl));

                return null;
            }
        }
        public bool HasAvatarImage =>
        !string.IsNullOrWhiteSpace(_avatarUrl) &&
        (_avatarUrl.StartsWith("http") || _avatarUrl.StartsWith("data:image"));

        public bool HasNoAvatarImage => !HasAvatarImage;
        public string DisplayName => string.IsNullOrWhiteSpace(_username) ? "Your Name" : _username;
        public string DisplayEmail => string.IsNullOrWhiteSpace(_email) ? "your@email.com" : _email;

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatusMessage)); }
        }

        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }



        public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

        // ─── Commands ────────────────────────────────────────────────────────
        public ICommand ToggleEditCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand TogglePasswordCommand { get; }
        public ICommand PickAvatarCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoHomeCommand { get; }

        // ─── Constructor ─────────────────────────────────────────────────────
        public ProfileViewModel()
        {
            ToggleEditCommand = new Command(OnToggleEdit);
            SaveProfileCommand = new Command(async () => await SaveProfileAsync());
            TogglePasswordCommand = new Command(() => PasswordHidden = !PasswordHidden);
            PickAvatarCommand = new Command(async () => await PickAvatarAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            GoHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//HomePage"));
            LoadUser();


            LoadCurrentUser();
        }

        // ─── Load saved user from Preferences ────────────────────────────────
        public void LoadCurrentUser()
        {
            _userId = Preferences.Get("user_id", string.Empty);
            Username = Preferences.Get("user_username", string.Empty);
            Email = Preferences.Get("user_email", string.Empty);
            AvatarUrl = Preferences.Get("user_avatar", string.Empty);

            System.Diagnostics.Debug.WriteLine($"[Profile] avatar url = {AvatarUrl}");
        }

        // ─── Toggle edit/cancel ───────────────────────────────────────────────
        private void OnToggleEdit()
        {
            IsEditing = !IsEditing;
            StatusMessage = string.Empty;

            // Reload original values if cancelling
            if (!IsEditing)
            {
                LoadCurrentUser();
                Password = string.Empty;
            }
        }

        // ─── Save Profile to MockAPI ──────────────────────────────────────────
        private async Task SaveProfileAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email))
            {
                StatusMessage = "Username and email are required.";
                StatusColor = "#FF4444";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                // DEBUG — check what ID we have
                System.Diagnostics.Debug.WriteLine($"[Profile] user_id = '{_userId}'");
                System.Diagnostics.Debug.WriteLine($"[Profile] BaseUrl = '{BaseUrl}'");

                if (string.IsNullOrWhiteSpace(_userId))
                {
                    StatusMessage = "Session expired. Please log out and log in again.";
                    StatusColor = "#FF4444";
                    return;
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // SaveProfileAsync — send correct field names to MockAPI
                var updateData = new Dictionary<string, string>
                {
                    ["name"] = Username,              // display name (optional: add a Name field to profile)
                    ["email"] = Email,                 // ✅ real email field
                    ["username"] = Username,              // ✅ real username field
                    ["avatar"] = AvatarUrl ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(Password))
                    updateData["password"] = Password;

                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{BaseUrl}/users/{_userId}";
                System.Diagnostics.Debug.WriteLine($"[Profile] PUT {url}");
                System.Diagnostics.Debug.WriteLine($"[Profile] Body: {json}");

                var response = await client.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[Profile] Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[Profile] Response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Save failed ({(int)response.StatusCode}). Check your MockAPI URL.";
                    StatusColor = "#FF4444";
                    return;
                }

                // Persist locally
                Preferences.Set("user_username", Username);
                Preferences.Set("user_email", Email);
                Preferences.Set("user_avatar", AvatarUrl ?? string.Empty);
                Password = string.Empty;
                StatusMessage = "✓ Profile updated successfully!";
                StatusColor = "#06C167";
                IsEditing = false;
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = "Network error. Check your internet connection.";
                StatusColor = "#FF4444";
                System.Diagnostics.Debug.WriteLine($"[Profile] HttpRequestException: {ex.Message}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unexpected error: {ex.Message}";
                StatusColor = "#FF4444";
                System.Diagnostics.Debug.WriteLine($"[Profile] Exception: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ImageSource? _pickedAvatarSource;
        public ImageSource? PickedAvatarSource
        {
            get => _pickedAvatarSource;
            set { _pickedAvatarSource = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPickedAvatar)); }
        }
        public bool HasPickedAvatar => _pickedAvatarSource != null;

        // ─── Pick avatar from device gallery ─────────────────────────────────
        private async Task PickAvatarAsync()
        {
            try
            {
                var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Pick a profile photo"
                });

                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // ✅ Resize/compress before Base64 encoding
                var compressedBytes = await CompressImageAsync(imageBytes, maxWidthHeight: 300, quality: 60);

                var base64String = Convert.ToBase64String(compressedBytes);
                var mimeType = "image/jpeg"; // always jpeg after compression
                var dataUrl = $"data:{mimeType};base64,{base64String}";

                AvatarUrl = dataUrl;

                var previewStream = new MemoryStream(compressedBytes);
                PickedAvatarSource = ImageSource.FromStream(() => previewStream);

                StatusMessage = "Avatar selected! Tap Save to apply.";
                StatusColor = "#06C167";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not pick photo: {ex.Message}";
                StatusColor = "#FF4444";
            }
        }

        // ─── Logout ───────────────────────────────────────────────────────────
        private async Task LogoutAsync()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Log Out",
                "Are you sure you want to log out?",
                "Log Out", "Cancel");

            if (!confirmed) return;

            // Clear all saved session data
            Preferences.Remove("user_id");
            Preferences.Remove("user_name");
            Preferences.Remove("user_username");   
            Preferences.Remove("user_email");
            Preferences.Remove("user_avatar");



            Application.Current.MainPage = new AppShell();
            // Navigate back to login
            await Shell.Current.GoToAsync("//MainPage");
        }

        // ─── INotifyPropertyChanged ───────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

  

        // ✅ Compress using MAUI's built-in encoder
        private async Task<byte[]> CompressImageAsync(byte[] imageBytes, int maxWidthHeight, int quality)
        {
            using var inputStream = new MemoryStream(imageBytes);

            // Decode original
            var originalImage = PlatformImage.FromStream(inputStream);

            // Scale down if needed
            float scale = Math.Min(
                maxWidthHeight / (float)originalImage.Width,
                maxWidthHeight / (float)originalImage.Height
            );

            // Only downscale, never upscale
            if (scale >= 1f) scale = 1f;

            int newWidth = (int)(originalImage.Width * scale);
            int newHeight = (int)(originalImage.Height * scale);

            var resized = originalImage.Resize(newWidth, newHeight, ResizeMode.Fit);

            using var outputStream = new MemoryStream();
            await resized.SaveAsync(outputStream, ImageFormat.Jpeg, quality / 100f);
            return outputStream.ToArray();
        }

    }
}