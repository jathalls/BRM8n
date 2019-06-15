using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Diagnostics;

namespace BatRecordingManager
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public sealed class ComparisonHost
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private static readonly ComparisonHost comparisonHostInstance = new ComparisonHost();

        private ComparisonWindow comparisonWindow = null;

        static ComparisonHost()
        {
        }

        private ComparisonHost()
        {
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static ComparisonHost Instance
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get
            {
                return comparisonHostInstance;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void AddImage(StoredImage image)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (image != null)
            {
                Debug.WriteLine("Added Image <" + image.caption + ">...<" + image.description + ">");
                if (comparisonWindow == null)
                {
                    comparisonWindow = new ComparisonWindow();
                    comparisonWindow.Show();
                }
                comparisonWindow.AddImage(image);
            }
        }

        /// <summary>
        /// Returns the currently selected image from the list of images in the comparison window,
        /// or the first image in the list if none are specifically selected.  If there is no current instance
        /// of the comparison window, or if the list of images is empty, then returns null.
        /// </summary>
        /// <returns></returns>
        public StoredImage GetSelectedImage()
        {
            StoredImage result = null;
            if (comparisonWindow != null)
            {
                result = comparisonWindow.GetSelectedImage();
            }

            return (result);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public void AddImageRange(BulkObservableCollection<StoredImage> images)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (!images.IsNullOrEmpty())
            {
                Debug.WriteLine("Added Image <" + images[0].caption + ">...<" + images[0].description + ">");
                if (comparisonWindow == null)
                {
                    comparisonWindow = new ComparisonWindow();
                    comparisonWindow.Show();
                }
                comparisonWindow.AddImageRange(images);
            }
        }

        internal void Close()
        {
            if (comparisonWindow != null)
            {
                comparisonWindow.Close();
                comparisonWindow = null;
            }
        }
    }
}