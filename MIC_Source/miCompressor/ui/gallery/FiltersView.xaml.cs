using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using miCompressor.core;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class FiltersView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            UIThreadHelper.RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        // Bind to the shared filter instance.
        public Filter SelectionFilter => App.CurrentState.SelectionFilter;

        public FiltersView()
        {
            this.InitializeComponent();
        }

        #region String values For UI (mostly dropdown buttons)
        public string LargerThan => FileSizeComparator.GreaterThan.GetDescription();
        public string SmallerThan => FileSizeComparator.LessThan.GetDescription();

        public string Bytes => FileSizeUnit.B.GetDescription();
        public string KB => FileSizeUnit.KB.GetDescription();
        public string MB => FileSizeUnit.MB.GetDescription();

        public string Contains = "Contains";
        public string StartsWith = "Starts With";
        public string EndsWith = "Ends With";
        #endregion

        public bool _applyFileNameSelectionFilter;
        public bool ApplyNameFileSelectionFilter
        {
            get 
            {
                return _applyFileNameSelectionFilter;
            }
            set
            {
                if (_applyFileNameSelectionFilter == value) return;
                _applyFileNameSelectionFilter = value;

                SelectionFilter.ApplyNameContainsFilter = false;
                SelectionFilter.ApplyNameStartsWithFilter = false;
                SelectionFilter.ApplyNameEndsWithFilter = false;

                if (_applyFileNameSelectionFilter)
                    ApplyFileSelectionFilterAndUpdateUI(FileNameFilterModeDropdown.Content.ToString());

                OnPropertyChanged(nameof(ApplyNameFileSelectionFilter));
            }
}

        // File Name Filter Mode dropdown click event handler.
        private void FileNameFilterMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                string mode = item.Text;
                ApplyFileSelectionFilterAndUpdateUI(mode);
            }
        }

        private void ApplyFileSelectionFilterAndUpdateUI(string mode)
        {
            FileNameFilterModeDropdown.Content = mode;

            // Reset all file name filter flags.
            SelectionFilter.ApplyNameContainsFilter = false;
            SelectionFilter.ApplyNameStartsWithFilter = false;
            SelectionFilter.ApplyNameEndsWithFilter = false;

            // Update the active property based on the selected mode and update the text box.
            if (mode == Contains)
            {
                SelectionFilter.ApplyNameContainsFilter = true;
                FileNameFilterTextBox.Text = SelectionFilter.NameContains;
            }
            else if (mode == StartsWith)
            {
                SelectionFilter.ApplyNameStartsWithFilter = true;
                FileNameFilterTextBox.Text = SelectionFilter.NameStartsWith;
            }
            else
            {
                SelectionFilter.ApplyNameEndsWithFilter = true;
                FileNameFilterTextBox.Text = SelectionFilter.NameEndsWith;
            }
        }

        // Method to update all controls based on the current values of SelectionFilter.
        private void UpdateControlsFromFilter()
        {
            // --- File Name Filter Section ---
            // Set the dropdown text and textbox value based on which file name filter is enabled.
            if (SelectionFilter.ApplyNameContainsFilter)
            {
                FileNameFilterModeDropdown.Content = Contains;
                FileNameFilterTextBox.Text = SelectionFilter.NameContains;
            }
            else if (SelectionFilter.ApplyNameStartsWithFilter)
            {
                FileNameFilterModeDropdown.Content = StartsWith;
                FileNameFilterTextBox.Text = SelectionFilter.NameStartsWith;
            }
            else if (SelectionFilter.ApplyNameEndsWithFilter)
            {
                FileNameFilterModeDropdown.Content = EndsWith;
                FileNameFilterTextBox.Text = SelectionFilter.NameEndsWith;
            }
            else
            {
                // Default to "Contains" if none are set.
                FileNameFilterModeDropdown.Content = Contains;
                FileNameFilterTextBox.Text = SelectionFilter.NameContains;
            }

            FileSizeComparatorDropdown.Content = SelectionFilter.SizeComparator.GetDescription();
            
            FileSizeUnitDropdown.Content = SelectionFilter.FileSizeUnit.GetDescription();
        }

        // Update the file name filter text property as the user types.
        private void FileNameFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FileNameFilterModeDropdown.Content is string mode)
            {
                string text = FileNameFilterTextBox.Text;
                if (mode == Contains) SelectionFilter.NameContains = text;
                if (mode == StartsWith) SelectionFilter.NameStartsWith = text;
                if (mode == EndsWith) SelectionFilter.NameEndsWith = text;
            }
        }

        // File Size Comparator dropdown event handler.
        private void FileSizeComparator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                string comp = item.Text;
                FileSizeComparatorDropdown.Content = comp;

                // Update the filter's comparator based on the selection.
                if (comp == LargerThan)
                {
                    SelectionFilter.SizeComparator = FileSizeComparator.GreaterThan;
                }
                else if (comp == SmallerThan)
                {
                    SelectionFilter.SizeComparator = FileSizeComparator.LessThan;
                }
            }
        }

        // File Size Unit dropdown event handler.
        private void FileSizeUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                string unit = item.Text;
                FileSizeUnitDropdown.Content = unit;

                // Update the filter's FileSizeUnit property.
                if (unit == Bytes) SelectionFilter.FileSizeUnit = FileSizeUnit.B;
                if (unit == KB) SelectionFilter.FileSizeUnit = FileSizeUnit.KB;
                if (unit == MB) SelectionFilter.FileSizeUnit = FileSizeUnit.MB;
            }
        }

        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilterButton.IsEnabled = false;
            //TODO: Run everytime FileStore Instance changes
            UIThreadHelper.RunOnUIThread(() => {
                foreach(var file in App.FileStoreInstance.GetAllFiles)
                   SelectionFilter.Apply(file);
                ApplyFilterButton.IsEnabled = true;
            });            
        }
    }
}
