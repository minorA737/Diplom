using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using System.Threading.Tasks;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class MessageBoxDialog : Window
    {
        public MessageBoxDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<bool> ShowDialog(Window parent, string title, string message, string okButtonText = "OK", string cancelButtonText = "Отмена")
        {
            var viewModel = new MessageBoxDialogViewModel
            {
                Title = title,
                Message = message,
                OkButtonText = okButtonText,
                CancelButtonText = cancelButtonText
            };

            var dialog = new MessageBoxDialog
            {
                DataContext = viewModel
            };

            viewModel.SetDialogWindow(dialog);

            return await dialog.ShowDialog<bool>(parent);
        }
    }
}