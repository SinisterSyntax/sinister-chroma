using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pixel_Magic
{
    /// <summary>
    /// Interaction logic for PaletteSelectorWindow.xaml
    /// </summary>
    public partial class PaletteSelectorWindow : Window
    {
        public static List<ColorEntry> colors = new List<ColorEntry>();


        public PaletteSelectorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var color = colorSelector.SelectedColor;
            

            colors.Add(new ColorEntry() { Title = color.ToString(), Thumbnail = color.ToString() });
            


            lstColors.ItemsSource = null;
            lstColors.ItemsSource = colors;
            //lstColors.Bind
        }
    }


    public class ColorEntry
    {
        public string Title { get; set; }
        public string Thumbnail { get; set; }
    }
}
