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


        // ��� �������� ���������� ��������� �������
        this.Loaded += (s, e) => LoadDashboard();

        // ��� ��������� URL ������������� �������
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
            ShowError("URL �������� �� ������");
            return;
        }

        _loadingIndicator.IsVisible = true;
        _errorPanel.IsVisible = false;

        try
        {
            //_dashboardWebView.Address = DashboardUrl;

            // ��������� ������� WebView
            //_dashboardWebView.NavigationCompleted += (s, e) =>
            //{
            //    _loadingIndicator.IsVisible = false;

            //    if (!e.IsSuccess)
            //    {
            //        ShowError($"������ ��������: {e.HttpStatusCode}");
            //    }
            //};

            //_dashboardWebView.NavigationFailed += (s, e) =>
            //{
            //    _loadingIndicator.IsVisible = false;
            //    ShowError($"������ ���������: {e.ErrorCode} - {e.ErrorMessage}");
            //};
        }
        catch (Exception ex)
        {
            _loadingIndicator.IsVisible = false;
            ShowError($"����������: {ex.Message}");
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