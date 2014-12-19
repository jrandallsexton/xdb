
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Enumerations;

namespace XDB.DataObjects
{

    public class PropertySelect
    {

        public Guid Id { get; set; }

        public Guid PropertyId { get; set; }

        public Guid? SubPropertyId { get; set; }

        public string PropertyName { get; set; }

        public string SubPropertyName { get; set; }

        public ECustomReportFieldType CustomReportFieldType { get; set; }

        public ETimeElapsedType TimeElapsedType { get; set; }

        public ETimeElapsedFormat TimeElapsedFormat { get; set; }

        public DateTime? TimeElapsedSpecificDate { get; set; }

        public string Label { get; set; }

        public string Logic { get; set; }

        public List<Guid> PropertyIds { get; set; }

        public int Index { get; set; }

        public bool IsGrouping { get; set; }

        public PropertySelect()
        {
            this.Id = Guid.NewGuid();
            this.PropertyIds = new List<Guid>();
            this.CustomReportFieldType = ECustomReportFieldType.NotApplicable;
            this.TimeElapsedFormat = ETimeElapsedFormat.NotApplicable;
            this.TimeElapsedType = ETimeElapsedType.AlwaysCurrentDate;
        }

        public PropertySelect(Guid id, Guid propertyId, string propertyName)
            : this()
        {
            this.Id = id;
            this.PropertyId = propertyId;
            this.PropertyName = propertyName;
        }

        public PropertySelect(Guid id, Guid propertyId, string propertyName, int index)
            : this()
        {
            this.Id = id;
            this.PropertyId = propertyId;
            this.PropertyName = propertyName;
            this.Index = index;
        }

        public PropertySelect(Guid id, Guid propertyId, string propertyName, Guid subPropertyId, string subPropertyName)
            : this()
        {
            this.Id = id;
            this.PropertyId = propertyId;
            this.PropertyName = propertyName;
            this.SubPropertyId = subPropertyId;
            this.SubPropertyName = subPropertyName;
        }

        //public PropertySelect Clone()
        //{
        //    ReportProperty value = new ReportProperty();
        //    value.Id = Guid.NewGuid();
        //    value.CustomReportFieldType = this.CustomReportFieldType;
        //    value.Index = this.Index;
        //    value.Label = this.Label;
        //    value.Logic = this.Logic;
        //    value.PropertyId = this.PropertyId;

        //    foreach (Guid id in this.PropertyIds)
        //    {
        //        value.PropertyIds.Add(id);
        //    }

        //    value.PropertyName = this.PropertyName;
        //    value.SubPropertyId = this.SubPropertyId;
        //    value.SubPropertyName = this.SubPropertyName;
        //    value.TimeElapsedSpecificDate = this.TimeElapsedSpecificDate;
        //    value.TimeElapsedType = this.TimeElapsedType;

        //    return value;
        //}

    }

}