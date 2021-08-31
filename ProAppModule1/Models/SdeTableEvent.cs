using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProAppModule1.Models
{
    /// <summary>
    /// Représente la table événement EVENEMENT
    /// </summary>
    public class SdeTableEvent
    {
        public const string LayerName = "EVENEMENT";

        public const string FieldId = "ID_EVENEMENT";
        public const string FieldDate = "DATE_EVENEMENT";
        public const string FieldAuthor = "AUTEUR";
        public const string FieldType = "TYPE";
        public const string FieldDescription = "DESCRIPTION";
        public const string FieldNumber = "NOMBRE_MAJ_EVENEMENT";

        public static EventElement Insert(EventElement element, Table table)
        {
            using (var rowBuffer = table.CreateRowBuffer())
            {
                rowBuffer[FieldId] = GetMaxId(table);
                rowBuffer[FieldDate] = element.Date;
                rowBuffer[FieldAuthor] = element.Author;
                rowBuffer[FieldType] = element.Type;
                rowBuffer[FieldDescription] = element.Description;
                rowBuffer[FieldNumber] = element.Number;

                using (var insertedRow = table.CreateRow(rowBuffer))
                {
                    element.Id = (int)insertedRow[FieldId];
                    element.Date = (DateTime)insertedRow[FieldDate];
                    element.Author = (string)insertedRow[FieldAuthor];
                    element.Type = (int)insertedRow[FieldType];
                    element.Description = (string)insertedRow[FieldDescription];
                    element.Number = (int)insertedRow[FieldNumber];
                }
            }

            return element;
        }

        public static int GetMaxId(Table table)
        {
            var id = 1;

            using (var def = table.GetDefinition())
            {
                var indexOfFieldId = table.GetDefinition().FindField(FieldId);
                var field = def.GetFields().ElementAt(indexOfFieldId);
                var stats = new StatisticsDescription(field, new List<StatisticsFunction>() { StatisticsFunction.Max });
                var result = table.CalculateStatistics(new TableStatisticsDescription(new List<StatisticsDescription>() { stats }));

                if (result.Count > 0)
                {
                    id = result[0].StatisticsResults.Count > 0 ?
                        (int)result[0].StatisticsResults[0].Max + 1 :
                        1;
                }
            }

            return id;
        }
    }
}
