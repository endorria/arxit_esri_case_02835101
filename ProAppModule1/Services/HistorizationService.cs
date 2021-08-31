using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProAppModule1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1.Services
{
    internal class HistorizationException : Exception
    {

        private readonly IGPResult _result;
        public HistorizationException(string step, IGPResult result) : base(step)
        {
            _result = result;
        }

        public override string ToString()
        {
            var errorMessage = Message + Environment.NewLine;

            if (_result == null) return errorMessage;

            if (_result.ErrorMessages.Any())
            {
                errorMessage += string.Join(Environment.NewLine, _result.ErrorMessages.Select(x => $"{x.ErrorCode} : {x.Text}"));
            }
            else if (_result.Messages.Any())
            {
                errorMessage += string.Join(Environment.NewLine, _result.Messages.Select(x => $"{x.ErrorCode} : {x.Text}"));
            }
            else if (!string.IsNullOrEmpty(_result.ReturnValue))
            {
                errorMessage += $"Valeur de retour : {_result.ReturnValue}";
            }

            return errorMessage;
        }
    }

    internal class HistorizationService
    {
        public static int NewEventId = -1;
        private static string _objectIdField = "objectid";
        public const string SdeTableParamLayerName = "PARAM_TEMPORALITE";

        public static async Task HistorizeTable(
            string sourceTableName,
            string description,
            DateTime startDate,
            string author,
            string idField,
            string labelField,
            string label2Field)
        {

            var sourceTableData = await CreateHistorizedTable(sourceTableName);

            await CreateRelationshipTablesOnHistorizedDatabase(sourceTableData.HistorizeTableName);
            var eventElement = await CreateEventRow(sourceTableData.Count, description, startDate, author);
            await CopyDataFromSourceToHistorize(
                sourceTableName,
                sourceTableData.HistorizeTableName,
                eventElement,
                sourceTableData.Count);
            await CreateIndexes(sourceTableData.HistorizeTableName, idField);
            await CreateParamTemporaliteRow(sourceTableName,
                sourceTableData.HistorizeTableName,
                idField,
                labelField,
                label2Field);
        }

        private static ProgressorSource CreatePs(string message, uint maxSteps)
        {
            var steps = maxSteps == 0 ? 1 : maxSteps;
            var ps = new ProgressDialog(message, steps);
            return new ProgressorSource(ps) { Max = steps };
        }

        private static void UpdatePs(ProgressorSource ps, uint value, string message)
        {
            ps.Progressor.Value = value;
            var max = ps.Progressor.Max < 1 ? 1 : ps.Progressor.Max;
            ps.Progressor.Status = (ps.Progressor.Value * 100 / max) + @" % Complété";
            if (!string.IsNullOrEmpty(message))
            {
                ps.Progressor.Message = message;
            }
        }

        public static Task<TableShortDefinition> GetShortDefinition(string tableName)
        {
            return QueuedTask.Run(() =>
            {
                TableShortDefinition shortDefinition = null;

                try
                {
                    using (var gdb = ConfigManager.ConnectToGeoDatabase(true))
                    using (var table = gdb.OpenDataset<Table>(tableName))
                    using (var def = table.GetDefinition())
                    {

                        shortDefinition = new TableShortDefinition()
                        {
                            TableName = def.GetName(),
                            GeoField = (def is FeatureClassDefinition fDef) ?
                                fDef.GetShapeField().ToUpper() :
                                "",
                            AllFields = def.GetFields(),
                        };
                    }
                }
                catch (Exception ex)
                {
                    
                }

                return shortDefinition;
            });
        }

        private static Task<SourceTableData> CreateHistorizedTable(string sourceTableName)
        {
            var cps = CreatePs("Etape 1/6 : création de la table historisée", (uint)2);

            return QueuedTask.Run(async () =>
            {
                using (var originGdb = ConfigManager.ConnectToGeoDatabase(true))
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var sourceTable = originGdb.OpenDataset<Table>(sourceTableName))
                {
                    var historizedTableName = Utils.GetTempoLayerNameFromFClass(sourceTable);
                    var result = await Utils.CreateFeatureClassAsync(
                        historizedGdb,
                        historizedTableName,
                        sourceTable,
                        SdeRelationshipClassEventHistorization.FieldDescriptor);

                    if (result.IsFailed)
                    {
                        throw new HistorizationException("Etape 1/6", result);
                    }

                    UpdatePs(cps, (uint)2, "Etape 1/6 : table historisée créée");

                    return new SourceTableData
                    {
                        Count = sourceTable.GetCount(),
                        HistorizeTableName = historizedTableName
                    };
                }
            }, cps.Progressor);
        }

        private static Task CreateRelationshipTablesOnHistorizedDatabase(string historizedTableName)
        {
            var cps = CreatePs(
                "Etape 2/6 : Création des tables relationnelles", (uint)3);
            return QueuedTask.Run(async () =>
            {
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var historizedTable = historizedGdb.OpenDataset<Table>(historizedTableName))
                using (var eventTable = historizedGdb.OpenDataset<Table>(SdeTableEvent.LayerName))
                {
                    // Création d'une relation dans BDD histo entre 
                    // <sourceTableName>_H et EVENEMENT
                    // relation pour Début
                    var result = await Utils.CreateRelationShipClass(
                        Utils.GetTableFullPath(historizedTable),
                        Utils.GetTableFullPath(eventTable),
                        "REL_EVT_DEBUT_" + historizedTableName,
                        "SIMPLE",
                        "Evènement début",
                        historizedTableName + " début",
                        "NONE",
                        "ONE_TO_ONE",
                        false,
                        SdeRelationshipClassEventHistorization.FieldEventIdDebut,
                        SdeTableEvent.FieldId,
                        "",
                        ""
                    );
                    if (result.IsFailed)
                    {
                        if (result.IsCanceled)
                        {
                            throw new OperationCanceledException();
                        }

                        throw new HistorizationException("Etape 2/6 : création de la relation REL_EVT_DEBUT", result);
                    }
                    UpdatePs(cps, (uint)2, "Etape 2/6 : Création de la relation 'fin'");
                    // Création d'une relation dans BDD histo entre 
                    // <sourceTableName>_H et EVENEMENT
                    // relation pour Fin
                    result = await Utils.CreateRelationShipClass(
                        Utils.GetTableFullPath(historizedTable),
                        Utils.GetTableFullPath(eventTable),
                        "REL_EVT_FIN_" + historizedTableName,
                        "SIMPLE",
                        "Evenement fin",
                        historizedTableName + " fin",
                        "NONE",
                        "ONE_TO_ONE",
                        false,
                        SdeRelationshipClassEventHistorization.FieldEventIdFin,
                        SdeTableEvent.FieldId,
                        "",
                        ""
                    );
                    if (result.IsFailed)
                    {
                        if (result.IsCanceled)
                        {
                            throw new OperationCanceledException();
                        }

                        throw new HistorizationException("Etape 2/6 : création de la relation REL_EVT_FIN", result);
                    }
                    UpdatePs(cps, (uint)3, "Tables de relations crées");
                }
            }, cps.Progressor);
        }

        private static Task<EventElement> CreateEventRow(int numberOfItemFromSourceTable,
            string description,
            DateTime startDate,
            string author
            )
        {
            var cps = CreatePs(
                "Etape 3/6 : ajout d'un évènement de type initialisation dans EVENEMENT",
                (uint)2);
            return QueuedTask.Run(async () =>
            {
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var eventTable = historizedGdb.OpenDataset<Table>(SdeTableEvent.LayerName))
                {
                    var newId = new Random().Next(0, 589);

                    var eventElement = new EventElement
                    {
                        Author = author,
                        Date = startDate,
                        Description = description,
                        Id = newId,
                        Number = numberOfItemFromSourceTable,
                        Type = 0
                    };
                    var isCreated = await Utils.CreateRowAsync(eventTable, eventElement.ToAttributes());
                    if (!isCreated)
                    {
                        throw new HistorizationException("Ajout de l'évènement", null);
                    }
                    UpdatePs(cps, (uint)2, $"Etape 3/6 : Evènement {newId} crée");
                    NewEventId = newId;
                    return eventElement;
                }
            });
        }

        private static Task CopyDataFromSourceToHistorize(
            string sourceTableName,
            string historizedTableName,
            EventElement eventElement,
            int numberOfItemInSource)
        {
            // Etape 4 : on copie tous les éléments de la nouvelle table (source) dans la table histo
            var ps = CreatePs("Etape 4/6 : copie de tous les éléments dans la table historisée ", (uint)numberOfItemInSource);
            return QueuedTask.Run(async () =>
            {
                using (var originGdb = ConfigManager.ConnectToGeoDatabase(true))
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var historizedTable = historizedGdb.OpenDataset<Table>(historizedTableName))
                using (var sourceTable = originGdb.OpenDataset<Table>(sourceTableName))
                {
                    var taskCollection = await CopyAllElements(
                        sourceTable,
                        historizedTable,
                        eventElement,
                        ps);

                    UpdatePs(ps, (uint)taskCollection.Count, $"Etape 4/6 : copie terminée");
                }
            }, ps.Progressor);
        }

        private static async Task<TaskCollection> CopyAllElements(
            Table sourceTable,
            Table targetTable,
            EventElement eventElement,
            ProgressorSource ps)
        {
            using (var def = sourceTable.GetDefinition())
            {
                var fields = def.GetFields();
                var listOfAttributes = new List<Dictionary<string, object>>();

                using (var cursor = sourceTable.Search(null, true))
                {
                    while (cursor.MoveNext())
                    {
                        using (var row = cursor.Current)
                        {
                            var dico = new Dictionary<string, object>();
                            foreach (var field in fields)
                            {
                                string domainNameValue;
                                var domain = field.GetDomain();
                                if (domain == null)
                                {
                                    dico.Add(field.Name, row[field.Name]);
                                }
                                else if (domain is CodedValueDomain codedValueDomain &&
                                         row[field.Name] != DBNull.Value &&
                                         row[field.Name] != null)
                                {
                                    try
                                    {
                                        var codedValuePairs = codedValueDomain.GetCodedValuePairs();
                                        if (codedValuePairs.TryGetValue(row[field.Name], out domainNameValue))
                                        {
                                            dico.Add(field.Name, domainNameValue);
                                        }
                                    }
                                    catch
                                    {
                                        dico.Add(field.Name, null);
                                    }
                                }
                                else
                                {
                                    dico.Add(field.Name, null);
                                }
                            }

                            dico.Add(SdeRelationshipClassEventHistorization.FieldDateDebut, eventElement.Date);
                            dico.Add(SdeRelationshipClassEventHistorization.FieldEventIdDebut, eventElement.Id);

                            listOfAttributes.Add(dico);
                        }
                    }
                }

                return new TaskCollection
                {
                    Count = listOfAttributes.Count,
                    IsSuccess = await CreateRowsAsync(
                        targetTable,
                        listOfAttributes,
                        ps)
                };
            }
        }

        public static async Task<bool> CreateRowsAsync(
            Table enterpriseTable,
            List<Dictionary<string, object>> listOfAttributes,
            ProgressorSource ps)
        {
            var result = false;
            var editOperation = new EditOperation();
            var geoField = enterpriseTable is FeatureClass fClass ?
                fClass.GetDefinition().GetShapeField() :
                "";

            var number = 1;
            var numberOfItemToCopy = listOfAttributes.Count;
            ps.Progressor.Max = (uint)numberOfItemToCopy;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            TimeSpan estimatedTimeFor100Items = default(TimeSpan);

            using (var rowBuffer = enterpriseTable.CreateRowBuffer())
            {
                editOperation.Callback(context =>
                {
                    foreach (var attributes in listOfAttributes)
                    {
                        foreach (var attribute in attributes)
                        {
                            if (attribute.Key.Equals(_objectIdField, StringComparison.OrdinalIgnoreCase) ||
                                attribute.Key.StartsWith(geoField + ".", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            rowBuffer[attribute.Key] = attribute.Value;
                        }

                        var row = enterpriseTable.CreateRow(rowBuffer);
                        context.Invalidate(row);
                        row.Dispose();

                        if (number == 1)
                        {
                            UpdatePs(ps, (uint)number, null);
                        }
                        else if (number % 100 == 0)
                        {
                            if (estimatedTimeFor100Items == default(TimeSpan))
                            {
                                stopWatch.Stop();
                                estimatedTimeFor100Items = new TimeSpan(stopWatch.ElapsedTicks);
                            }

                            var timeToGo = TimeSpan.FromMilliseconds(
                                (estimatedTimeFor100Items.TotalMilliseconds * numberOfItemToCopy / 10) -
                                (estimatedTimeFor100Items.TotalMilliseconds * number / 10));

                            var message = $"Etape 4/6 : temps restant approximatif : {timeToGo.ToString(@"hh\:mm\:ss", new CultureInfo("ch-FR"))}";
                            UpdatePs(ps, (uint)number, message);
                        }

                        number++;
                    }
                }, enterpriseTable);

                result = await editOperation.ExecuteAsync();
            }

            return result;
        }


        private static Task CreateIndexes(string historizedTableName, string idField)
        {
            // Etape 5 : création des index

            var ps = CreatePs("Etape 5/6 : création des index", (uint)2);
            return QueuedTask.Run(async () =>
            {
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var historizedTable = historizedGdb.OpenDataset<Table>(historizedTableName))
                {
                    var isHistorizedTableAFeatureClass = historizedTable is FeatureClass;

                    string indexTableName = historizedTable.GetName()
                        .Substring(historizedTable.GetName()
                            .LastIndexOf(".", StringComparison.Ordinal) + 1);
                    string indexName = $"I_{indexTableName}";
                    var result = await Utils.AddAttributeIndex(
                        Utils.GetTableFullPath(historizedTable),
                        new[]
                        {
                            SdeRelationshipClassEventHistorization.FieldDateDebut,
                            SdeRelationshipClassEventHistorization.FieldDateFin,
                            idField
                        },
                        indexName.Length > 30 ? indexName.Substring(0, 30) : indexName);
                    if (result.IsFailed)
                    {
                        throw new HistorizationException("Etape 5/6 : création de l'index attributaire", result);
                    }
                    UpdatePs(ps, (uint)1, "Etape 5/6 : création de l'index spatiale");
                    if (isHistorizedTableAFeatureClass)
                    {
                        result = await Utils.AddOrRebuildSpatialIndex(Utils.GetTableFullPath(historizedTable));

                        if (result.IsFailed)
                        {
                            if (result.IsCanceled)
                            {
                                throw new OperationCanceledException();
                            }

                            throw new HistorizationException("Etape 5/6 : création de l'index spatiale", result);
                        }
                    }

                    UpdatePs(ps, (uint)2, "Etape 5/6 : indexes créés avec succès");
                }
            }, ps.Progressor);
        }

        private static Task CreateParamTemporaliteRow(
            string sourceTableName,
            string historizedTableName,
            string idField,
            string labelField,
            string label2Field)
        {
            // Etape 6 : Ajout dans la table paramétrage
            var ps = CreatePs("Etape 6/6 : ajout dans la table PARAM_TEMPORALITE", (uint)2);
            return QueuedTask.Run(async () =>
            {
                using (var historizedGdb = ConfigManager.ConnectToGeoDatabase(false))
                using (var historizedTable = historizedGdb.OpenDataset<Table>(historizedTableName))
                using (var paramTable = historizedGdb.OpenDataset<Table>(SdeTableParamLayerName))
                {
                    var isHistorizedTableAFeatureClass = historizedTable is FeatureClass;

                    ParamElement paramElement = new ParamElement(sourceTableName, idField, labelField, label2Field, isHistorizedTableAFeatureClass);

                    var isCreated = await Utils.CreateRowAsync(paramTable, paramElement.ToAttributes());
                    if (!isCreated)
                    {
                        throw new HistorizationException("Etape 6/6 : ajout dans la table PARAM_TEMPORALITE", null);
                    }
                    UpdatePs(ps, (uint)2, "Etape 6/6 : paramètre ajouté");
                }
            }, ps.Progressor);
        }
    }
}
