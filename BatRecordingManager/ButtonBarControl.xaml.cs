using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ButtonBarControl.xaml
    /// </summary>
    public partial class ButtonBarControl : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ButtonBarControl"/> class.
        /// </summary>
        public ButtonBarControl()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Adds the custom button.
        /// </summary>
        /// <param name="label">
        ///     The label.
        /// </param>
        /// <param name="index">
        ///     The index.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     </returns>
        public Button AddCustomButton(string label, int index, string name)
        {
            Button newButton = new Button();
            newButton.Name = name;
            newButton.Style = (Style)FindResource("SimpleButton");
            newButton.Content = label;
            if (index < 0) index = 0;
            if (index > ButtonPanel.Children.Count) index = ButtonPanel.Children.Count;
            ButtonPanel.Children.Insert(index, newButton);
            //ButtonPanel.Children.Add(newButton);
            return (newButton);
        }
    }
}