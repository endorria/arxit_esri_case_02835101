using System;
using System.Collections.Generic;

namespace ProAppModule1.Models
{
    public class EventElement
    {
        public int Id { get; set; } = -1;
        public DateTime Date { get; set; }
        public int Type { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public int Number { get; set; }

        public Dictionary<string, object> ToAttributes()
        {
            var attributes = new Dictionary<string, object>();
            attributes.Add("ID_EVENEMENT", Id);
            attributes.Add("DATE_EVENEMENT", Date);
            attributes.Add("AUTEUR", Author);
            attributes.Add("TYPE", Type);
            attributes.Add("DESCRIPTION", Description);
            attributes.Add("NOMBRE_MAJ_EVENEMENT", Number);

            return attributes;
        }
    }
}
