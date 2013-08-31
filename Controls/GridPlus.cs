using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

/*
namespace AmazonScrape
{
    class GridPlus : Grid
    {
        public enum GridComponent { Row, Column };

        public GridPlus() 
        {
            // Dynamically grab the control style
            Style = ResourceLoader.GetControlStyle("GridStyle");
        }

        public int GetRowDefinitionCount()
        {
            return this.RowDefinitions.Count;
        }

        public int GetColumnDefinitionCount()
        {
            return this.ColumnDefinitions.Count;
        }

        public void AddContent(UIElement content, int row, int col,int span = 1)
        {
            if (row > GetRowDefinitionCount() - 1)
            {
                string message = "The supplied row index (" + row.ToString() + ") is " + 
                    "greater than the max row index " + GetRowDefinitionCount().ToString();
                throw new ArgumentOutOfRangeException(message);
            }

            if (col > GetColumnDefinitionCount() - 1)
            {
                string message = "The supplied column index (" + col.ToString() + ") is " +
                    "greater than the max column index " + GetColumnDefinitionCount().ToString();
                throw new ArgumentOutOfRangeException(message);
            }

            this.Children.Add(content);
            Grid.SetRow(content, row);
            Grid.SetColumn(content, col);
            Grid.SetColumnSpan(content, span);
        }

        public void AddRow(Int32 val, GridUnitType type)
        {
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(val, type);;
            this.RowDefinitions.Add(row);
        }

        public void AddColumn(Int32 val,GridUnitType type)
        {
            ColumnDefinition col = new ColumnDefinition();
            col.Width = new GridLength(val, type);
            this.ColumnDefinitions.Add(col);
        }
    }
}
*/