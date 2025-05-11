// ViewModels/Dialogs/MessageBoxDialogViewModel.cs
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;

namespace ManufactPlanner.ViewModels.Dialogs
{
    public class MessageBoxDialogViewModel : ViewModelBase
    {
        private string _title;
        private string _message;
        private string _okButtonText = "OK";
        private string _cancelButtonText = "Отмена";
        private Window _dialogWindow;

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => this.RaiseAndSetIfChanged(ref _message, value);
        }

        public string OkButtonText
        {
            get => _okButtonText;
            set => this.RaiseAndSetIfChanged(ref _okButtonText, value);
        }

        public string CancelButtonText
        {
            get => _cancelButtonText;
            set => this.RaiseAndSetIfChanged(ref _cancelButtonText, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public MessageBoxDialogViewModel()
        {
            OkCommand = ReactiveCommand.Create(() =>
            {
                _dialogWindow?.Close(true);
            });

            CancelCommand = ReactiveCommand.Create(() =>
            {
                _dialogWindow?.Close(false);
            });
        }

        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }
    }
}