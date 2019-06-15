using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatListControl.xaml
    /// </summary>
    public partial class BatListControl : UserControl
    {
        #region SortedBatList

        /// <summary>
        ///     SortedBatList Dependency Property
        /// </summary>
        public static readonly DependencyProperty SortedBatListProperty =
            DependencyProperty.Register("SortedBatList", typeof(BulkObservableCollection<Bat>), typeof(BatListControl),
                new FrameworkPropertyMetadata((BulkObservableCollection<Bat>)new BulkObservableCollection<Bat>()));

        /// <summary>
        ///     Gets or sets the SortedBatList property. This dependency property indicates ....
        /// </summary>
        public BulkObservableCollection<Bat> SortedBatList
        {
            get { return (BulkObservableCollection<Bat>)GetValue(SortedBatListProperty); }
            //set { SetValue(SortedBatListProperty, value); }
        }

        #endregion SortedBatList

        //private BatSummary batSummary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatListControl"/> class.
        /// </summary>
        public BatListControl()
        {
            SetValue(SortedBatListProperty, new BulkObservableCollection<Bat>());
            InitializeComponent();
            this.DataContext = this;
            BatsDataGrid.EnableColumnVirtualization = true;
            BatsDataGrid.EnableRowVirtualization = true;

            //batSummary = new BatSummary();

            batDetailControl.ListChanged += BatDetailControl_ListChanged;
            //RefreshData();

            //batDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;
        }

        /// <summary>
        /// Returns the currently selected bat or null if none has been selected
        /// </summary>
        /// <returns></returns>
        internal Bat GetSelectedBat()
        {
            Bat result = null;

            if (batDetailControl != null)
            {
                if (batDetailControl.selectedBat != null)
                {
                    result = batDetailControl.selectedBat;
                }
            }

            return (result);
        }

        internal void RefreshData()
        {
            using (new WaitCursor("Refreshing Bat List"))
            {
                int index = BatsDataGrid.SelectedIndex;
                SortedBatList.Clear();
                SortedBatList.AddRange(DBAccess.GetSortedBatList());
                BatsDataGrid.SelectedIndex = index < SortedBatList.Count() ? index : SortedBatList.Count() - 1;

                batDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;
            }
        }

        private void AddBatButton_Click(object sender, RoutedEventArgs e)
        {
            EditBatForm batEditingForm = new EditBatForm();
            batEditingForm.NewBat = new Bat();
            batEditingForm.NewBat.Id = -1;
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult != null && batEditingForm.DialogResult.Value)
            {
                //DBAccess.InsertBat(batEditingForm.newBat);
                RefreshData();
            }
            batDetailControl.selectedBat = BatsDataGrid.SelectedItem as Bat;
        }

        private void BatDetailControl_ListChanged(object sender, EventArgs e)
        {
            BatDetailControl bdc = sender as BatDetailControl;

            int tagIndex = bdc.BatTagsListView.SelectedIndex;

            RefreshData();
            bdc.BatTagsListView.SelectedIndex = tagIndex;
        }

        /// Double-click on the DataGrid listing the bats sends all the images for the selected bats to the comparison window
        private void BatsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (new WaitCursor("Selecting all images for Comparison"))
            {
                BulkObservableCollection<StoredImage> images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        Bat bat = item as Bat;
                        BulkObservableCollection<StoredImage> thisBatsImages = DBAccess.GetAllImagesForBat(bat);
                        if (thisBatsImages != null)
                        {
                            images.AddRange(thisBatsImages);
                        }
                    }
                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty())
                    {
                        ComparisonHost.Instance.AddImageRange(images);
                    }
                }
            }
        }

        private void BatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //DataGrid bsdg = sender as DataGrid;
            if (e.AddedItems == null || e.AddedItems.Count <= 0) return;
            Bat selected = e.AddedItems[0] as Bat;
            if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                Bat previous = e.RemovedItems[0] as Bat;
                if (previous == selected) return;
            }
            // therefore we have a selected item which is different from the previously selected item
            using (new WaitCursor("Bat selection changed"))
            {
                batDetailControl.selectedBat = selected;
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Add image to Comparison Window"))
            {
                BulkObservableCollection<StoredImage> images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        Bat bat = item as Bat;
                        BulkObservableCollection<StoredImage> thisBatsImages = DBAccess.GetBatAndCallImagesForBat(bat);
                        if (thisBatsImages != null)
                        {
                            images.AddRange(thisBatsImages);
                        }
                    }
                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty())
                    {
                        ComparisonHost.Instance.AddImageRange(images);
                    }
                }
            }
        }

        private void DelBatButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatsDataGrid.SelectedItem != null)
            {
                Bat selectedBat = BatsDataGrid.SelectedItem as Bat;

                DBAccess.DeleteBat(selectedBat);
                RefreshData();
            }
        }

        private void EditBatButton_Click(object sender, RoutedEventArgs e)
        {
            batDetailControl.Reset();
            EditBatForm batEditingForm = new EditBatForm();
            if (BatsDataGrid.SelectedItem == null)
            {
                batEditingForm.NewBat = new Bat();
                batEditingForm.NewBat.Id = -1;
            }
            else
            {
                batEditingForm.NewBat = BatsDataGrid.SelectedItem as Bat;
            }
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult ?? false)
            {
                RefreshData();
            }
        }

        private void CompareImagesButton_Click(object sender, RoutedEventArgs e)
        {
            using (new WaitCursor("Selecting all images for Comparison"))
            {
                BulkObservableCollection<StoredImage> images = new BulkObservableCollection<StoredImage>();
                if (BatsDataGrid.SelectedItems != null)
                {
                    //var selectedBat = BatsDataGrid.SelectedItem as Bat;

                    foreach (var item in BatsDataGrid.SelectedItems)
                    {
                        Bat bat = item as Bat;
                        BulkObservableCollection<StoredImage> thisBatsImages = DBAccess.GetAllImagesForBat(bat);
                        if (thisBatsImages != null)
                        {
                            images.AddRange(thisBatsImages);
                        }
                    }
                    //var images = DBAccess.GetImagesForBat(selectedBat, Tools.BlobType.PNG);
                    //var images = selectedBat.GetImageList();
                    if (!images.IsNullOrEmpty())
                    {
                        ComparisonHost.Instance.AddImageRange(images);
                    }
                }
            }
        }
    }
}