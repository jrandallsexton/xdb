
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;

namespace XDB.Models
{

    public class XFilter
    {

        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string Property { get; set; }
        public EFilterOperator OperatorId { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string DisplayValue { get; set; }
        public int Order { get; set; }

        public bool FromInstanceOfAsset { get; set; }

        public bool IsIncludesFilter { get; set; }

        public Guid? LogicalOperatorId { get; set; }

        public XFilter()
        {
            this.Id = Guid.NewGuid();
            this.Value = string.Empty;
        }

        public XFilter(Guid id, Guid propertyId, string property, EFilterOperator operatorId, string op, string value, int order)
        {
            this.Id = id;
            this.PropertyId = propertyId;
            this.Property = property;
            this.OperatorId = operatorId;
            this.Operator = op;
            this.Value = value;
            this.Order = order;
        }

        public XFilter(Guid id, Guid propertyId, string property, EFilterOperator operatorId, string op, string value, int order, bool isIncludes, Guid? logOpId)
        {
            this.Id = id;
            this.PropertyId = propertyId;
            this.Property = property;
            this.OperatorId = operatorId;
            this.Operator = op;
            this.Value = value;
            this.Order = order;
            this.IsIncludesFilter = isIncludes;
            this.LogicalOperatorId = logOpId;
        }

        public XFilter Clone()
        {
            XFilter filter = new XFilter();
            filter.Id = System.Guid.NewGuid();
            filter.DisplayValue = this.DisplayValue;
            filter.Operator = this.Operator;
            filter.OperatorId = this.OperatorId;
            filter.Order = this.Order;
            filter.Property = this.Property;
            filter.PropertyId = this.PropertyId;
            filter.Value = this.Value;
            return filter;
        }

    }

}