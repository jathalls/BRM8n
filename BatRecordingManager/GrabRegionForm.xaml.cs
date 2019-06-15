using System;
using System.Windows;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for GrabRegionForm.xaml
    /// </summary>
    public partial class GrabRegionForm : Window
    {
        public GrabRegionForm()
        {
            InitializeComponent();
        }

        public System.Drawing.Rectangle rect { get; set; } = new System.Drawing.Rectangle();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.ChangedButton == MouseButton.Left && (Mouse.LeftButton == MouseButtonState.Pressed))
                {
                    try
                    {
                        this.DragMove();
                        e.Handled = true;
                    }
                    catch (Exception) { }
                }
            }
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                if (e.ChangedButton == MouseButton.Right)
                {
                    rect = new System.Drawing.Rectangle((int)this.Left, (int)this.Top, (int)this.Width, (int)this.Height);

                    this.Close();
                }
            }
        }
    }
}