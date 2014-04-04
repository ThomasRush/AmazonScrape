using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AmazonScrape
{
    /// <summary>
    /// Extends DataGrid to allow columns to be easily added through code-behind
    /// Avoids overly-cluttered XAML
    /// </summary>
    class DataGridPlus : DataGrid
    {
        // Create dependency properties so we can change the formatting through xaml ( header font size vs. result font size)
        public DataGridPlus()        
        {
            RowHeight = Double.NaN;
            IsReadOnly = true;
            CanUserAddRows = false;
            AutoGenerateColumns = true;
            CanUserResizeColumns = false;
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;            
            CanUserResizeColumns = true;
            CanUserReorderColumns = false;
            CanUserResizeRows = true;
        }

        public void AddImageColumn(string bindingName,
            string headerText,
            int widthPercent,
            BindingMode bindingMode,
            string sortOn="",Style style=null)
        {
            FrameworkElementFactory ef = new FrameworkElementFactory(typeof(Image));
            Binding binding = new Binding(bindingName);
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Mode = bindingMode;
            ef.SetValue(Image.SourceProperty, binding);
            AddColumn(ef, widthPercent,headerText,sortOn,style);
        }

        public void AddButtonColumn(string buttonText, int widthPercent, RoutedEventHandler clickHandler,Style style = null)
        {
            FrameworkElementFactory ef = new FrameworkElementFactory(typeof(Button));
            ef.SetValue(Button.StyleProperty, ResourceLoader.GetControlStyle("ButtonStyle"));
            ef.SetValue(Button.ContentProperty, buttonText);
            ef.AddHandler(Button.ClickEvent, clickHandler, true);
            AddColumn(ef, widthPercent, "","",style);
        }

        public void AddTextColumn(string bindingName,string headerText,int widthPercent,Style style = null)
        {
            FrameworkElementFactory ef = new FrameworkElementFactory(typeof(TextBlock));            
            ef.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            ef.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            ef.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            ef.SetValue(TextBlock.TextProperty, new Binding(bindingName));
            AddColumn(ef, widthPercent, headerText, bindingName, style);
        }

        private void AddColumn(FrameworkElementFactory ef,int widthPercent,string headerText,string sortOn = "",Style style=null)
        {
            // If overriding the default style
            // TODO: test this
            if (!(style == null)) ef.SetValue(StyleProperty, style);

            DataGridTemplateColumn newCol = new DataGridTemplateColumn();

            if (sortOn.Length > 0)
            {
                newCol.CanUserSort = true;
                newCol.SortMemberPath = sortOn;
            }

            newCol.Header = headerText;
            newCol.Width = new DataGridLength(widthPercent, DataGridLengthUnitType.Star);
            
            DataTemplate template = new DataTemplate();
            template.VisualTree = ef;
            newCol.CellTemplate = template;
            this.Columns.Add(newCol);
        }

    }
}
