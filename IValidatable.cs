using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    /// <summary>
    /// Defines controls that can be validated.
    /// Result contains status/error information and an optional generic return value type.
    /// </summary>
    interface IValidatable
    {
        Result<T> Validate<T>();
    }
}
