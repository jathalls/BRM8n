using Microsoft.Maps.MapControl.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        private Location _coordinates;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapControl"/> class.
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            mapControl.Focus();
            lastInsertedPinLocation = null;
        }

        /// <summary>
        ///     Gets or sets the coordinates.
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location coordinates
        {
            get
            {
                return (_coordinates);
            }
            set
            {
                _coordinates = value;
                mapControl.Center = value;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Location lastInsertedPinLocation { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        ///     Adds the push pin.
        /// </summary>
        /// <param name="PinCoordinates">
        ///     The pin coordinates.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        public void AddPushPin(Location PinCoordinates, String text)
        {
            Pushpin pin = new Pushpin();
            pin.Location = PinCoordinates;
            pin.Content = text;
            mapControl.Children.Add(pin);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void AddPushPin(Location location)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Pushpin pin = new Pushpin();
            pin.Location = location;
            mapControl.Children.Add(pin);
        }

        private void mapControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            mapControl.Children.Clear();
            Point mousePosition = e.GetPosition(this);
            Location pinLocation = mapControl.ViewportPointToLocation(mousePosition);

            Pushpin pin = new Pushpin();
            pin.Location = pinLocation;
            lastInsertedPinLocation = pinLocation;
            mapControl.Children.Add(pin);
        }
    }
}