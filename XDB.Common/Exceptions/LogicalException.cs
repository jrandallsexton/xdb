
using System;
using System.Collections.Generic;

namespace XDB.Common.Exceptions
{

    public class LogicalException : Exception
    {

        public List<RuleViolation> RuleViolations { get; set; }

        public LogicalException() { }

        public LogicalException(string errorMessage)
        {
            if (this.RuleViolations == null) { this.RuleViolations = new List<RuleViolation>(); }
            this.RuleViolations.Add(new RuleViolation(errorMessage));
        }

        public LogicalException(string errorMessage, string propertyName)
        {
            if (this.RuleViolations == null) { this.RuleViolations = new List<RuleViolation>(); }
            this.RuleViolations.Add(new RuleViolation(errorMessage, propertyName));
        }

    }

}