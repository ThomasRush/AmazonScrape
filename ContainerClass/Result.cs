using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonScrape
{
    /// <summary>
    /// Container class for a method result, including any status/error messages and
    /// a generic return type.
    /// Used in validation and to return results to the main Window.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        public T Value { get { return _returnValue; } set { _returnValue = value; } }
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; } }
        public string ErrorMessage { get { return _errorMessage; } set { _errorMessage = value; } }
        public bool HasReturnValue { get { return ( ! Equals(_returnValue, default(T))); } }
        public bool HasError { get { return (_errorMessage.Length > 0); } }

        private T _returnValue;
        private string _statusMessage;
        private string _errorMessage;

        public Result()
        {
            _returnValue = default(T);
            ErrorMessage = "";
            StatusMessage = "";
        }

    }
}
