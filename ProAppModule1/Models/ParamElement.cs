using System.Collections.Generic;

namespace ProAppModule1.Models
{
    public class ParamElement
    {
        public string TableName { get; set; }
        public string IdField { get; set; }
        public string LabelField { get; set; }
        public string Label2Field { get; set; }
        public bool IsFeatureClass { get; set; }

        public ParamElement()
        {
        }

        public ParamElement(string tableName, string idField, string labelField, string label2Field, bool isFeatureClass)
        {
            this.TableName = tableName;
            this.IdField = idField;
            this.LabelField = labelField;
            this.Label2Field = label2Field;
            this.IsFeatureClass = isFeatureClass;
        }

        public Dictionary<string, object> ToAttributes()
        {
            var attributes = new Dictionary<string, object>();
            attributes.Add("COUCHE", TableName);
            attributes.Add("CHAMP_ID", IdField);
            attributes.Add("CHAMP_LIBELLE", LabelField);
            if (!string.IsNullOrEmpty(Label2Field))
            {
                attributes.Add("CHAMP_LIBELLE_2", Label2Field);
            }
            attributes.Add("TYPE", IsFeatureClass ? "1" : "0");

            return attributes;
        }
    }
}
