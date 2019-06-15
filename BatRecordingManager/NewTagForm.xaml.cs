using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for NewTagForm.xaml
    /// </summary>
    public partial class NewTagForm : Window
    {
        /// <summary>
        ///     The tag text
        /// </summary>
        public String TagText;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NewTagForm"/> class.
        /// </summary>
        public NewTagForm()
        {
            InitializeComponent();
            TagText = "";
            TagTextBox.Focus();
        }

        /// <summary>
        ///     Handles the Click event of the Button control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(TagTextBox.Text))
            {
                TagText = TagTextBox.Text;
                BatTag tag = DBAccess.GetTag(TagText);
                if (tag != null)
                {
                    MessageBox.Show("Tag Already defined for " + tag.Bat.Name, "Tag <" + TagText + "> In Use");
                    return;
                }
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}