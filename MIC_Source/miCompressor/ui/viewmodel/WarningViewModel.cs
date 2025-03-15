using miCompressor.core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace miCompressor.ui.viewmodel
{
    public class WarningViewModel : INotifyPropertyChanged
    {
        private readonly WarningHelper warningHelper = WarningHelper.Instance;
        public ObservableCollection<WarningGroup> ErrorsOrWarnings { get; private set; } = new ObservableCollection<WarningGroup>();

        private string _currentWarningType = "Post Compression ErrorsOrWarnings";
        public string CurrentWarningType
        {
            get => _currentWarningType;
            set
            {
                _currentWarningType = value;
                OnPropertyChanged(nameof(CurrentWarningType));
                RefreshWarnings();
            }
        }

        public WarningViewModel()
        {
            warningHelper.PropertyChanged += WarningHelper_PropertyChanged;
            RefreshWarnings(); // Initial load
        }

        // Get the errors/warning in textual form.
        public string GetText()
        {
            StringBuilder text = new StringBuilder().Append("");

            foreach (var group in ErrorsOrWarnings)
            {
                text.AppendLine($"** {group.GroupName} ({group.Items.Count} files) **");
                foreach (var file in group.Items)
                    text.AppendLine($"    - {file.FilePath}");
            }
            return text.ToString();
        }

        private void WarningHelper_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WarningHelper.PostCompressionWarnings) ||
                e.PropertyName == nameof(WarningHelper.PreCompressionWarnings) ||
                e.PropertyName == nameof(WarningHelper.CompressionErrors))
            {
                RefreshWarnings();
            }
        }

        private void RefreshWarnings()
        {
            ErrorsOrWarnings.Clear();
            Dictionary<Enum, List<MediaFileInfo>> source = CurrentWarningType switch
            {
                "Pre Compression Warnings" => warningHelper.PreCompressionWarnings.ToDictionary(kvp => (Enum)kvp.Key, kvp => kvp.Value),
                "Post Compression Warnings" => warningHelper.PostCompressionWarnings.ToDictionary(kvp => (Enum)kvp.Key, kvp => kvp.Value),
                "Compression Errors" => warningHelper.CompressionErrors.ToDictionary(kvp => (Enum)kvp.Key, kvp => kvp.Value),
                _ => new Dictionary<Enum, List<MediaFileInfo>>()
            };

            foreach (var group in source)
            {
                string description = group.Key switch
                {
                    PostCompressionWarningType post => EnumDescriptionProvider<PostCompressionWarningType>.Description(post),
                    PreCompressionWarningType pre => EnumDescriptionProvider<PreCompressionWarningType>.Description(pre),
                    CompressionErrorType error => EnumDescriptionProvider<CompressionErrorType>.Description(error),
                    _ => group.Key.ToString() // Fallback to enum name if no match
                };

                ErrorsOrWarnings.Add(new WarningGroup(description, group.Value));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            UIThreadHelper.RunOnUIThread(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    public class WarningGroup
    {
        public string GroupName { get; }
        public List<MediaFileInfo> Items { get; }

        public WarningGroup(string groupName, List<MediaFileInfo> items)
        {
            GroupName = groupName;
            Items = items;
        }
    }
}
