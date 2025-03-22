using miCompressor.core;
using miCompressor.ui.viewmodel;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class CompressionProgress : UserControl
    {
        public CompressionViewModel vm => App.CurrentState.CompressionViewModel;
        public WarningHelper warningAndError => WarningHelper.Instance;
        public MasterState CurrentState => App.CurrentState;

        [AutoNotify] private bool canShowError = false;
        [AutoNotify] private bool canShowWarning = false;
        public bool CanShowGeneralInfo => !vm.CompressionInProgress && vm.TotalFilesFailedToCompress == 0 && vm.TotalFilesCancelled == 0 && vm.TotalFilesCompressed > 1;

        private bool HasPreCompressionWarnings => warningAndError.HasPreCompressionWarnings;
        
        private bool _showErrorView = false;
        public bool ShowErrorView
        {
            get => _showErrorView;
            set
            {
                if(_showErrorView != value)
                {
                    _showErrorView = value;
                    OnPropertyChanged(nameof(ShowErrorView));
                    OnPropertyChanged(nameof(ShowErrorOrWarningView));
                }
            }
        }

        private bool _showWarningView = false;
        public bool ShowWarningView
        {
            get => _showWarningView;
            set
            {
                if (_showWarningView != value)
                {
                    _showWarningView = value;
                    OnPropertyChanged(nameof(ShowWarningView));
                    OnPropertyChanged(nameof(ShowErrorOrWarningView));
                }
            }
        }

        public bool ShowErrorOrWarningView => ShowErrorView || ShowWarningView || warningAndError.HasPreCompressionWarnings;

        private string ErrorText => $"Errors ({warningAndError.ErrorCount})";
        private string WarningText => $"Warnings ({warningAndError.WarningCount})";


        private bool ShowBackButton => _showErrorView || _showWarningView || !vm.CompressionInProgress;

        [AutoNotify] public List<PieChartData> compressionProgressVisuals = new();

        public bool ShowOutputFolderLink
        {
            get
            {
                return App.OutputSettingsInstance.OutputLocationSettings == OutputLocationSetting.UserSpecificFolder
                    && Directory.Exists(App.OutputSettingsInstance.OutputFolder) && vm.CompressionInProgress == false;
            }
        }

        public CompressionProgress()
        {
            this.InitializeComponent();

            vm.PropertyChanged += CompressionVM_PropertyChanged;
            warningAndError.PropertyChanged += WarningAndError_PropertyChanged;
            this.PropertyChanged += CompressionProgress_PropertyChanged;
        }

        private void WarningAndError_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WarningHelper.HasWarnings))
                CanShowWarning = WarningHelper.Instance.HasWarnings;
            if (e.PropertyName == nameof(WarningHelper.HasErrors))
                CanShowError = WarningHelper.Instance.HasErrors;

            if (e.PropertyName == nameof(WarningHelper.WarningCount))
                OnPropertyChanged(nameof(WarningText));
            if (e.PropertyName == nameof(WarningHelper.ErrorCount))
                OnPropertyChanged(nameof(ErrorText));

            OnPropertyChanged(nameof(ShowBackButton));
            OnPropertyChanged(nameof(ShowErrorOrWarningView));
            OnPropertyChanged(nameof(HasPreCompressionWarnings));
        }

        private void CompressionProgress_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompressionProgress.ShowErrorView) || e.PropertyName == nameof(CompressionProgress.ShowWarningView))
            {
                OnPropertyChanged(nameof(ShowBackButton));
                OnPropertyChanged(nameof(ShowErrorOrWarningView));
                OnPropertyChanged(nameof(HasPreCompressionWarnings));
                OnPropertyChanged(nameof(CanShowGeneralInfo));
            }
        }

        private void CompressionVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName != nameof(CompressionViewModel.TimeToShow))
            {
                var remaining = vm.TotalFilesToCompress - vm.TotalFilesCompressed - vm.TotalFilesFailedToCompress - vm.TotalFilesCancelled;

                CompressionProgressVisuals = new()
                {
                    new() { Label="Remaining", Value=remaining, Color = Color.FromArgb(255, 217, 217, 92) },
                    new() { Label="Processed", Value=vm.TotalFilesCompressed, Color = Color.FromArgb(255, 92, 184, 92) },
                    new() { Label="Failed", Value=vm.TotalFilesFailedToCompress, Color = Color.FromArgb(255,  217, 92, 108) },
                    new() { Label="Cancelled", Value= vm.TotalFilesCancelled, Color = Color.FromArgb(255,  92, 128, 217) },
                };

                OnPropertyChanged(nameof(ShowOutputFolderLink));
                OnPropertyChanged(nameof(ShowBackButton));
                OnPropertyChanged(nameof(ShowErrorOrWarningView));
                OnPropertyChanged(nameof(HasPreCompressionWarnings));
                OnPropertyChanged(nameof(CanShowError));
                OnPropertyChanged(nameof(CanShowWarning));
                OnPropertyChanged(nameof(CanShowGeneralInfo));
            }         
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowErrorView)
                ShowErrorView = false;
            else if (ShowWarningView)
                ShowWarningView = false;
            else
                vm.CancelCompression();
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if(ShowWarningView || ShowErrorView)
            {
                ShowWarningView = false;
                ShowErrorView = false;
            }
            else 
                App.CurrentState.ShowCompressionProgress = false;
        }


        private async void CompressDespiteWarnings_Click(object sender, RoutedEventArgs e)
        {
            vm.OverridePreCompressionWarningsAndStartCompression();
            OnPropertyChanged(nameof(HasPreCompressionWarnings));
            OnPropertyChanged(nameof(ShowErrorOrWarningView));            
        }

        private async void CancelCompression_Click(object sender, RoutedEventArgs e)
        {
            vm.CancelAsPreCompressionWarnings();
            App.CurrentState.ShowCompressionProgress = false;
            OnPropertyChanged(nameof(HasPreCompressionWarnings));
        }

        private async void OpenOutputFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (App.OutputSettingsInstance.OutputFolder != null && Directory.Exists(App.OutputSettingsInstance.OutputFolder))
            {
                await Launcher.LaunchFolderPathAsync(App.OutputSettingsInstance.OutputFolder);
            }
        }

        protected ICommand ShowWarningCommand => new RelayCommand<object>(param =>
        {
            ShowWarningView = true;
            OnPropertyChanged(nameof(ShowBackButton));
        });

        protected ICommand ShowErrorCommand => new RelayCommand<object>(param =>
        {
            ShowErrorView = true;
            OnPropertyChanged(nameof(ShowBackButton));
        });
    }
}
