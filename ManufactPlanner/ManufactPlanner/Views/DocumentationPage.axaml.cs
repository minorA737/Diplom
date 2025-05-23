using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views
{
    public partial class DocumentationPage : UserControl
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        public DocumentationPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            InitializeComponent();
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // ������������� �� ������� �������� ��������
            this.Loaded += DocumentationPage_Loaded;
        }

        private void DocumentationPage_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // �������� ���� ������ ����� ������ �������� ��������
            var window = TopLevel.GetTopLevel(this) as Window;
            DataContext = new DocumentationViewModel(_mainWindowViewModel, _dbContext, window);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}