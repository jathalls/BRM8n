using System.Windows;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     </summary>
    public class CommandExtensions : DependencyObject
    {
        /// <summary>
        ///     The command property
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(CommandExtensions),
                new UIPropertyMetadata(null));

        /// <summary>
        ///     Gets the command.
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <returns>
        ///     </returns>
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        /// <summary>
        ///     Sets the command.
        /// </summary>
        /// <param name="obj">
        ///     The object.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }
    }
}