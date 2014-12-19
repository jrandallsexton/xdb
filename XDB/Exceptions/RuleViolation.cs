
using System;

namespace XDB.Exceptions
{

    public class RuleViolation
    {

        public string ErrorMessage { get; set; }

        public string PropertyName { get; set; }

        public RuleViolation() { }

        public RuleViolation(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }

        public RuleViolation(string errorMessage, string propertyName)
        {
            this.ErrorMessage = errorMessage;
            this.PropertyName = propertyName;
        }

    }

}