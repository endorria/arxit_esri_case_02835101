using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1.Models
{
    public class SdeRelationshipClassEventHistorization
    {
        public const string FieldDateDebut = "DATE_DEBUT";
        public const string FieldEventIdDebut = "EVENT_ID_DEBUT";
        public const string FieldDateFin = "DATE_FIN";
        public const string FieldEventIdFin = "EVENT_ID_FIN";

        public static readonly string FieldDescriptor = $"{FieldDateDebut} DATE 'Date début' # #;" +
                                               $"{FieldEventIdDebut} LONG 'Evènement début' # #;" +
                                               $"{FieldDateFin} DATE 'Date fin' # #;" +
                                               $"{FieldEventIdFin} LONG 'Evènement fin' # #;";
    }
}
