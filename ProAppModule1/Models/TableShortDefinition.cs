using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;

namespace ProAppModule1.Models
{
    public interface IText
    {
        string Text { get; set; }
    }

    public class TableShortDefinition : IText
    {
        public string Text { get; set; }

        private string _tableName;
        public string TableName
        {
            get => _tableName;
            set
            {
                _tableName = value;
                Text = value;
            }
        }
        public string GeoField { get; set; }

        public IReadOnlyList<Field> AllFields { get; set; }

        public IReadOnlyList<Field> Fields
        {
            get
            {
                return AllFields
                    .Where(field => !field.Name.Equals(GeoField, StringComparison.OrdinalIgnoreCase) && !field.Name.StartsWith(GeoField + ".", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

    }
}
