using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AmazonScrape

{
    class ValueRangeControl : Grid
    {
        DoubleRange _range;
        
        public ValueRangeControl(string name, DoubleRange range)
        {
            this._range = range;

            // Set up grid with four coulmns, one row
            ColumnDefinition col = new ColumnDefinition();
            col.Width = new GridLength(20, GridUnitType.Star);
            this.ColumnDefinitions.Add(col);

            col = new ColumnDefinition();
            col.Width = new GridLength(20, GridUnitType.Star);
            this.ColumnDefinitions.Add(col);

            col = new ColumnDefinition();
            col.Width = new GridLength(20, GridUnitType.Star);
            this.ColumnDefinitions.Add(col);

            col = new ColumnDefinition();
            col.Width = new GridLength(20, GridUnitType.Star);
            this.ColumnDefinitions.Add(col);

            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(100, GridUnitType.Star);

            // TODO: remove the set row if it's not necessary
            TextBlock from = new TextBlock();
            from.Text = name + " from :";
            this.Children.Add(from);
            Grid.SetColumn(from,0);
            Grid.SetRow(from,0);

            TextBoxPlus low = new TextBoxPlus();
            low.Text = range.Low.ToString();
            this.Children.Add(low);
            Grid.SetColumn(low, 1);
            Grid.SetRow(low,0);

            TextBlock to = new TextBlock();
            to.Text = " to : ";
            this.Children.Add(to);
            Grid.SetColumn(to, 2);
            Grid.SetRow(to, 0);

            TextBoxPlus high = new TextBoxPlus();            
            if (range.High < double.MaxValue)
            { high.Text = range.High.ToString(); }
            Grid.SetColumn(high, 3);
            Grid.SetRow(high, 0);


        }
    
}
}
