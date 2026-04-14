using SOCIAL_MEDIA_APP_FINAL_PROJECT.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace SOCIAL_MEDIA_APP_FINAL_PROJECT.ViewModel
{

    public class AuthViewModel : INotifyPropertyChanged
    {
        // ─── MockAPI endpoint ────────────────────────────────────────────────
        // Replace with your actual MockAPI.io project URL
        private const string BaseUrl = "https://69dbab37560857310a07e486.mockapi.io/api";

        // ─── Backing fields ──────────────────────────────────────────────────
        private bool _isLoginMode = true;
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _rememberMe;
        private bool _isBusy;
        private string _errorMessage = string.Empty;

        public string AvatarUrl { get; set; }

        private void LoadUser()
        {
            AvatarUrl = Preferences.Get("user_avatar", "");
        }

        // ─── Properties ──────────────────────────────────────────────────────
        public bool IsLoginMode
        {
            get => _isLoginMode;
            set { _isLoginMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRegisterMode)); UpdateDynamicLabels(); }
        }

        public bool IsRegisterMode => !_isLoginMode;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set { _rememberMe = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
        }

        public bool IsNotBusy => !_isBusy;

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // Dynamic UI labels
        private string _actionButtonText = "Login";
        public string ActionButtonText
        {
            get => _actionButtonText;
            set { _actionButtonText = value; OnPropertyChanged(); }
        }

        private string _bottomLinkText = "Don't have an account?";
        public string BottomLinkText
        {
            get => _bottomLinkText;
            set { _bottomLinkText = value; OnPropertyChanged(); }
        }

        private string _bottomLinkAction = "Sign up";
        public string BottomLinkAction
        {
            get => _bottomLinkAction;
            set { _bottomLinkAction = value; OnPropertyChanged(); }
        }

        // ─── Commands ────────────────────────────────────────────────────────
        public ICommand SwitchToLoginCommand { get; }
        public ICommand SwitchToRegisterCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand AuthCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand AppleSignInCommand { get; }
        public ICommand GoogleSignInCommand { get; }

        // ─── Constructor ─────────────────────────────────────────────────────
        public AuthViewModel()
        {
            SwitchToLoginCommand = new Command(() => IsLoginMode = true);
            SwitchToRegisterCommand = new Command(() => IsLoginMode = false);
            ToggleModeCommand = new Command(() => IsLoginMode = !IsLoginMode);
            AuthCommand = new Command(async () => await ExecuteAuthAsync());
            ForgotPasswordCommand = new Command(async () => await OnForgotPasswordAsync());
            AppleSignInCommand = new Command(async () => await OnAppleSignInAsync());
            GoogleSignInCommand = new Command(async () => await OnGoogleSignInAsync());
        }

        // ─── Auth Logic ──────────────────────────────────────────────────────
        private async Task ExecuteAuthAsync()
        {
            ErrorMessage = string.Empty;

            if (IsLoginMode)
                await LoginAsync();
            else
                await RegisterAsync();
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please fill in all fields.";
                return;
            }

            IsBusy = true;
            try
            {
                using var client = new HttpClient();
                // GET users and filter by email + password (MockAPI doesn't have auth endpoint)
                var response = await client.GetAsync($"{BaseUrl}/users");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Server error. Please try again.";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (users == null)
                {
                    ErrorMessage = "No users found or failed to load data.";
                    return;
                }
                var matched = users.FirstOrDefault(u =>
             string.Equals(u.Email, Email, StringComparison.OrdinalIgnoreCase) &&
              u.Password == Password);

                if (matched == null)
                {
                    ErrorMessage = "Invalid email or password.";
                    return;
                }

                // Clear old session first
                Preferences.Remove("user_id");
                Preferences.Remove("user_name");
                Preferences.Remove("user_email");
                Preferences.Remove("user_username");
                Preferences.Remove("user_avatar");



                // Save Preferences correctly after login
                Preferences.Set("user_id", matched.Id ?? "");
                Preferences.Set("user_name", matched.Name ?? "");
                Preferences.Set("user_email", matched.Email ?? "");   
                Preferences.Set("user_username", matched.Username ?? "");   
                Preferences.Set("user_avatar", matched.Avatar ?? "");
                // TODO: Navigate to home page
                await Shell.Current.GoToAsync("//HomePage");

     
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please fill in all fields.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            IsBusy = true;
            try
            {
                using var client = new HttpClient();

                var newUser = new UserModel
                {
                    Name = Username,  
                    Email = Email,      
                    Username = Username,   
                    Password = Password,
                    Avatar = ""
                };

                var body = JsonSerializer.Serialize(newUser);
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{BaseUrl}/users", content);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Registration failed. Please try again.";
                    return;
                }

                await Shell.Current.DisplayAlert("Success", "Account created! You can now log in.", "OK");
                IsLoginMode = true;
                ClearFields();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnForgotPasswordAsync()
        {
            await Shell.Current.DisplayAlert("Forgot Password", "A reset link will be sent to your email.", "OK");
        }

        private async Task OnAppleSignInAsync()
        {
            await Shell.Current.DisplayAlert("Apple Sign In", "Apple Sign In coming soon.", "OK");
        }

        private async Task OnGoogleSignInAsync()
        {
            await Shell.Current.DisplayAlert("Google Sign In", "Google Sign In coming soon.", "OK");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────
        private void UpdateDynamicLabels()
        {
            if (IsLoginMode)
            {
                ActionButtonText = "Login";
                BottomLinkText = "Don't have an account?";
                BottomLinkAction = "Sign up";
            }
            else
            {
                ActionButtonText = "Register";
                BottomLinkText = "Already have an account?";
                BottomLinkAction = "Log in";
            }
        }

        private void ClearFields()
        {
            Username = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        // ─── INotifyPropertyChanged ──────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}