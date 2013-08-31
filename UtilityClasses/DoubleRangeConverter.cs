using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;

namespace AmazonScrape
{
    class DoubleRangeConverter : global::System.ComponentModel.TypeConverter
    {
        // True if source is string
        public override bool CanConvertFrom(
         System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        //should return true when destinationtype if 
        public override bool CanConvertTo(
             System.ComponentModel.ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(DoubleRange);

        }

        // Convert string to DoubleRange
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    return DoubleRange.Parse(value as string);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format(
                    "Cannot convert '{0}' ({1}) because {2}", value, value.GetType(), ex.Message), ex);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        
        public override object ConvertTo(
         System.ComponentModel.ITypeDescriptorContext context,
          System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            DoubleRange range = value as DoubleRange;

            if (range != null)
                if (this.CanConvertTo(context, destinationType))
                    return range.ToString();

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
