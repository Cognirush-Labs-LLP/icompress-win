using miCompressor.core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class FiltersView : UserControl
    {

        #region dependency properties
        /// <summary>
        /// Indicates whether filter options should be shown.
        /// The control uses this to update visuals (Visibility/VisualStates).
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        [AutoNotify] private bool isEmptyViewVisible = true;


        /// <summary>
        /// DP backing field for <see cref="IsOpen"/>.
        /// Default: false (collapsed).
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(FiltersView),
                new PropertyMetadata(false, OnIsOpenChanged));


        /// <summary>
        /// Updates visual state when IsOpen changes.
        /// </summary>
        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FiltersView view)
                view.UpdateVisualState(useTransitions: true);
        }

        /// <summary>
        /// Applies the appropriate visual state or basic Visibility change.
        /// </summary>
        private void UpdateVisualState(bool useTransitions)
        {
            OnPropertyChanged(nameof(IsOpen));
            Debug.WriteLine($"IsOpen: {IsOpen}");
        }
        #endregion

        // Bind to the shared filter instance.
        public Filter SelectionFilter => App.CurrentState.SelectionFilter;

        public FiltersView()
        {
            this.InitializeComponent();
            App.CurrentState.SelectionFilter.PropertyChanged -= SelectionFilter_PropertyChanged;
            App.CurrentState.SelectionFilter.PropertyChanged += SelectionFilter_PropertyChanged;
            this.Unloaded += FiltersView_Unloaded;

            App.FileStoreInstance.PropertyChanged -= FileStore_PropertyChanged;
            App.FileStoreInstance.PropertyChanged += FileStore_PropertyChanged;
            IsEmptyViewVisible = !App.FileStoreInstance.SelectedPaths.Any();
        }

        private void FileStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileStore.SelectedPaths))
                IsEmptyViewVisible = !App.FileStoreInstance.SelectedPaths.Any();
        }

        private void SelectionFilter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AutoApplyFilter();
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
        public string NameGlob = "Glob";
        public string NameRegex = "Regex";

        public string FilterText = "";

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
                SelectionFilter.ApplyNameGlobFilter = false;
                SelectionFilter.ApplyNameRegexFilter = false;


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
            SelectionFilter.ApplyNameGlobFilter = false;
            SelectionFilter.ApplyNameRegexFilter = false;

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
            else if (mode == EndsWith)
            {
                SelectionFilter.ApplyNameEndsWithFilter = true;
                FileNameFilterTextBox.Text = SelectionFilter.NameEndsWith;
            }
            else if (mode == NameGlob)
            {
                SelectionFilter.ApplyNameGlobFilter = true;
                FileNameFilterTextBox.Text = SelectionFilter.NameGlob;
            }
            else if (mode == NameRegex)
            {
                SelectionFilter.ApplyNameRegexFilter = true;

                if (RegexHelper.IsValidPattern(SelectionFilter.NameRegex))
                    FileNameFilterTextBox.Text = SelectionFilter.NameRegex;
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
            else if (SelectionFilter.applyNameGlobFilter)
            {
                FileNameFilterModeDropdown.Content = NameGlob;
                FileNameFilterTextBox.Text = SelectionFilter.NameGlob;
            }
            else if (SelectionFilter.applyNameRegexFilter)
            {
                FileNameFilterModeDropdown.Content = NameRegex;
                FileNameFilterTextBox.Text = SelectionFilter.NameRegex;
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

        public bool RegexError = false; 
        // Update the file name filter text property as the user types.
        private void FileNameFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegexError = false;
            if (FileNameFilterModeDropdown.Content is string mode)
            {
                string text = FileNameFilterTextBox.Text;
                if (mode == Contains) SelectionFilter.NameContains = text;
                if (mode == StartsWith) SelectionFilter.NameStartsWith = text;
                if (mode == EndsWith) SelectionFilter.NameEndsWith = text;
                if (mode == NameGlob) SelectionFilter.NameGlob = text;
                if (mode == NameRegex)
                {
                    //FileNameFilterTextBox's font color green (of theme) or red (of theme) based on text valid or not
                    if (RegexHelper.IsValidPattern(text))
                        SelectionFilter.NameRegex = text;
                    else
                    {
                        RegexError = true;
                        SelectionFilter.NameRegex = "";
                    }
                }
            }
            OnPropertyChanged(nameof(RegexError));
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

        private void AutoApplyFilter(int throttleMs = 1000)
        {
            ThrottleTask.Add(
                throttleTimeInMs: throttleMs,    // 1 second
                key: "ApplySelectionFilter",
                action: () =>
                {
                    foreach (var file in App.FileStoreInstance.GetAllFiles)
                        SelectionFilter.Apply(file);
                    FilterText = SelectionFilter.BuildFilterSummary();
                    OnPropertyChanged(nameof(FilterText));
                },
                shouldRunInUI: true
            );
        }

        /*
        private void ApplyFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilterButton.IsEnabled = false;
            //TODO: Run everytime FileStore Instance changes
            UIThreadHelper.RunOnUIThread(() =>
            {
                foreach (var file in App.FileStoreInstance.GetAllFiles)
                    SelectionFilter.Apply(file);
                ApplyFilterButton.IsEnabled = true;
            });
        }*/

        private void FiltersView_Unloaded(object sender, RoutedEventArgs e)
        {
            App.CurrentState.SelectionFilter.PropertyChanged -= SelectionFilter_PropertyChanged;
        }
    }
}
