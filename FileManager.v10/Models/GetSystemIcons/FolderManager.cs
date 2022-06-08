using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static FileManager.v10.ShellManager;

namespace FileManager.v10
{
    public static class FolderManager
    {
        public static ImageSource GetImageSource(string directory, ItemState folderType)
        {
            try
            {
                return FolderManager.GetImageSource(directory, new Size(16, 16), folderType);
            }
            catch
            {

                return GetDummyImage();
            }
        }

        public static ImageSource GetDummyImage()
        {
            string psAssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            Uri oUri = new Uri("pack://application:,,,/" + psAssemblyName + ";component/" + "Icons/question.png", UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }

        public static ImageSource GetImageSource(string directory, Size size, ItemState folderType)
        {
            try
            {
                using (var icon = ShellManager.GetIcon(directory, ItemType.Folder, IconSize.Large, folderType))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)size.Width, (int)size.Height));
                }
            }
            catch
            {
                throw;
            }
        }
    }

}
