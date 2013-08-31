using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmazonScrape
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {        
        static App()
        {
            // Ensure that displayed text uses ClearType globally
            TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(Window),
                new FrameworkPropertyMetadata(TextFormattingMode.Display, 
                    FrameworkPropertyMetadataOptions.AffectsMeasure | 
                    FrameworkPropertyMetadataOptions.AffectsRender | 
                    FrameworkPropertyMetadataOptions.Inherits));
            
        }

    }
}
