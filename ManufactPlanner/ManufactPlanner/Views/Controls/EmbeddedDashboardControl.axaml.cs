using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ManufactPlanner.Services;
using System;
using System.Threading.Tasks;
using ProgressBar = Avalonia.Controls.ProgressBar;

namespace ManufactPlanner.Views.Components;

public partial class EmbeddedDashboardControl : UserControl
{
    private ProgressBar _loadingIndicator;
    private Border _errorPanel;
    private TextBlock _errorMessage;

    public string DashboardUrl { get; set; }

    public EmbeddedDashboardControl()
    {
        InitializeComponent();

        //_dashboardWebView = this.FindControl<Xamarin.Forms.WebView>("DashboardWebView");
        _loadingIndicator = this.FindControl<ProgressBar>("LoadingIndicator");
        _errorPanel = this.FindControl<Border>("ErrorPanel");
        _errorMessage = this.FindControl<TextBlock>("ErrorMessage");


        // При загрузке компонента загружаем дашборд
        this.Loaded += (s, e) => LoadDashboard();

        // При изменении URL перезагружаем дашборд
        this.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(DashboardUrl))
            {
                LoadDashboard();
            }
        };
    }

    private void LoadDashboard()
    {
        if (string.IsNullOrEmpty(DashboardUrl))
        {
            ShowError("URL дашборда не указан");
            return;
        }

        _loadingIndicator.IsVisible = true;
        _errorPanel.IsVisible = false;

        try
        {
            //_dashboardWebView.Address = DashboardUrl;

            // Обработка событий WebView
            //_dashboardWebView.NavigationCompleted += (s, e) =>
            //{
            //    _loadingIndicator.IsVisible = false;

            //    if (!e.IsSuccess)
            //    {
            //        ShowError($"Ошибка загрузки: {e.HttpStatusCode}");
            //    }
            //};

            //_dashboardWebView.NavigationFailed += (s, e) =>
            //{
            //    _loadingIndicator.IsVisible = false;
            //    ShowError($"Ошибка навигации: {e.ErrorCode} - {e.ErrorMessage}");
            //};
        }
        catch (Exception ex)
        {
            _loadingIndicator.IsVisible = false;
            ShowError($"Исключение: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _errorMessage.Text = message;
            _errorPanel.IsVisible = true;
        });
    }

    private void RetryButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LoadDashboard();
    }
}