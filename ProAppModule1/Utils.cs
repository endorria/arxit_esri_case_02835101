using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProAppModule1
{
    public class Utils
    {
        private const string CreateTableToolName = "CreateTable_management";
        private const string DeleteTableToolName = "Delete_management";
        private const string CreateFeatureClassToolName = "CreateFeatureclass_management";
        private const string AddFieldsToolName = "AddFields_management";
        private const string AddIndexToolName = "AddIndex_management";
        private const string AddRebuildSpatialIndexToolName = "AddSpatialIndex_management";
        private const string CreateRelationshipTableToolName = "CreateRelationshipClass_management";
        private const string ObjectIdField = "objectid";
        public const int MaxLengthFeatureClass = 30;
        public const string TempoSuffix = "_H";

        public static string GetTempoLayerNameFromFClass(Table table)
        {
            var tableName = table.GetName();
            return GetTempoLayerNameFromFClassName(tableName);
        }

        public static string GetTempoLayerNameFromFClassName(string tableName)
        {
            var fClassNameShortName = tableName
                .Substring(tableName.LastIndexOf(".", StringComparison.Ordinal) + 1);

            if (fClassNameShortName.Length <= MaxLengthFeatureClass - TempoSuffix.Length)
            {
                return fClassNameShortName + TempoSuffix;
            }
            else
            {
                // keeps first 28 chars
                return fClassNameShortName
                    .Substring(0, MaxLengthFeatureClass - TempoSuffix.Length) + TempoSuffix;
            }
        }

        public static string GetLogin()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// Appel au Geoprocessing pour créer une FeatureClass ou une Table dans une BDD
        /// ref: https://pro.arcgis.com/fr/pro-app/tool-reference/data-management/create-table.htm
        /// </summary>
        /// <param name="gdb">Target geo db</param>
        /// <param name="tableName">Target table name</param>
        /// <param name="templateTable">Original table, used to copy the necessary fields</param>
        /// <param name="fieldDescription">Add extra fields to the new table</param>
        /// <returns></returns>
        public static async Task<IGPResult> CreateFeatureClassAsync(
            Geodatabase gdb,
            string tableName,
            Table templateTable,
            string fieldDescription)
        {
            var geoDatabaseFullPath = gdb.GetPath().AbsolutePath;
            var isFeatureClass = templateTable is FeatureClass;

            IReadOnlyList<string> parameters;
            if (isFeatureClass)
            {
                var shapeType = Enum.GetName(typeof(GeometryType),
                    ((FeatureClass)templateTable).GetDefinition().GetShapeType());
                parameters = Geoprocessing.MakeValueArray(
                    geoDatabaseFullPath,
                    tableName,
                    shapeType,
                    null,
                    null,
                    null,
                    Utils.GetTableFullPath(templateTable)
                );
            }
            else
            {
                parameters = Geoprocessing.MakeValueArray(
                    geoDatabaseFullPath,
                    tableName
                );
            }

            var result = await Geoprocessing.ExecuteToolAsync(
                isFeatureClass ? CreateFeatureClassToolName : CreateTableToolName,
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.RefreshProjectItems);

            if (result.IsFailed)
            {
                return result;
            }

            var newTableFullPath = Path.Combine(
                geoDatabaseFullPath,
                tableName
                );

            using (var def = templateTable.GetDefinition())
            {
                var geoField = isFeatureClass ? (def as FeatureClassDefinition)?.GetShapeField() : "";

                var fields = def.GetFields();

                fieldDescription += string.Join(";", fields
                    .Where(x => geoField != null &&
                                !x.Name.Equals(ObjectIdField, StringComparison.OrdinalIgnoreCase) &&
                                Enum.GetName(typeof(FieldType), x.FieldType) != null &&
                                !x.Name.Equals(geoField, StringComparison.OrdinalIgnoreCase) &&
                                !x.Name.StartsWith(geoField + ".", StringComparison.OrdinalIgnoreCase)
                                )
                    .Select(x =>
                    {
                        string typeStr;
                        switch (x.FieldType)
                        {
                            case FieldType.GlobalID:
                                typeStr = "GUID";
                                break;
                            case FieldType.XML:
                            case FieldType.String:
                                typeStr = "TEXT";
                                break;
                            case FieldType.OID:
                            case FieldType.Integer:
                                typeStr = "LONG";
                                break;
                            case FieldType.SmallInteger:
                                typeStr = "SHORT";
                                break;
                            case FieldType.Single:
                                typeStr = "FLOAT";
                                break;
                            default:
                                typeStr = Enum.GetName(typeof(FieldType), x.FieldType)?.ToUpper();
                                break;
                        }
                        var domain = x.GetDomain();

                        return domain == null ?
                            $"{x.Name} {typeStr} '{x.AliasName}' # # #" :
                            $"{x.Name} TEXT '{x.AliasName}' # # #";
                    }
                    ));

            }

            System.Diagnostics.Debug.WriteLine("CreateFeatureClassAsync fields description : " + fieldDescription);

            return await AddFields(newTableFullPath, fieldDescription);
        }

        /// <summary>
        /// Add fields to the table
        /// </summary>
        /// <param name="tableAbsolutePath"></param>
        /// <param name="fieldDescription">Should be like Name1 Type1 Alias1 Length1 Default1 Domain1;Name2 Type2 Alias2 Length2 Default2 Domain2;
        /// Use # to let the default value</param>
        /// <returns></returns>
        private static Task<IGPResult> AddFields(string tableAbsolutePath, string fieldDescription)
        {
            var parameters = Geoprocessing.MakeValueArray(
                tableAbsolutePath,
                fieldDescription
            );

            return Geoprocessing.ExecuteToolAsync(
                AddFieldsToolName,
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.RefreshProjectItems);
        }

        public static string GetTableFullPath(Table table)
        {
            return Path.Combine(table.GetDatastore().GetPath().AbsolutePath, table.GetName());
        }

        /// <summary>
        /// Create a relationship class using the Geo processing tool
        /// https://pro.arcgis.com/en/pro-app/tool-reference/data-management/create-relationship-class.htm
        /// </summary>
        /// <param name="originTablePath"></param>
        /// <param name="destinationTablePath"></param>
        /// <param name="outRelationshipClassName"></param>
        /// <param name="relationshipType"></param>
        /// <param name="forwardLabel"></param>
        /// <param name="backwardLabel"></param>
        /// <param name="messageDirection"></param>
        /// <param name="cardinality"></param>
        /// <param name="attributed"></param>
        /// <param name="originPrimaryKey"></param>
        /// <param name="originForeignKey"></param>
        /// <param name="destinationPrimaryKey"></param>
        /// <param name="destinationForeignKey"></param>
        /// <returns></returns>
        public static Task<IGPResult> CreateRelationShipClass(
            string originTablePath,
            string destinationTablePath,
            string outRelationshipClassName,
            string relationshipType,
            string forwardLabel,
            string backwardLabel,
            string messageDirection,
            string cardinality,
            bool attributed,
            string originPrimaryKey,
            string originForeignKey,
            string destinationPrimaryKey = null,
            string destinationForeignKey = null
            )
        {
            var parameters = Geoprocessing.MakeValueArray(
                originTablePath,
                destinationTablePath,
                outRelationshipClassName,
                relationshipType,
                forwardLabel,
                backwardLabel,
                messageDirection,
                cardinality,
                attributed,
                originPrimaryKey,
                originForeignKey,
                destinationPrimaryKey,
                destinationForeignKey
            );

            return Geoprocessing.ExecuteToolAsync(
                CreateRelationshipTableToolName,
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.RefreshProjectItems);
        }

        public static Task<bool> CreateRowAsync(
            DatabaseConnectionProperties properties,
            string tableName,
            Dictionary<string, object> attributes)
        {
            var editOperation = new EditOperation();

            using (var gdb = new Geodatabase(properties))
            using (var enterpriseTable = gdb.OpenDataset<Table>(tableName))
            {
                var geoField = enterpriseTable is FeatureClass fClass ?
                    fClass.GetDefinition().GetShapeField() :
                    "";

                editOperation.Callback(context =>
                {
                    if (enterpriseTable != null)
                        using (var rowBuffer = enterpriseTable.CreateRowBuffer())
                        {
                            foreach (var attribute in attributes)
                            {
                                if (attribute.Key.Equals(ObjectIdField, StringComparison.OrdinalIgnoreCase) ||
                                    attribute.Key.StartsWith(geoField + ".", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                rowBuffer[attribute.Key] = attribute.Value;
                            }

                            using (var row = enterpriseTable.CreateRow(rowBuffer))
                            {
                                // To Indicate that the attribute table has to be updated.
                                context.Invalidate(row);
                            }
                        }
                }, enterpriseTable);

                return editOperation.ExecuteAsync();
            }
        }

        public static async Task<bool> CreateRowsAsync(
            Table enterpriseTable,
            List<Dictionary<string, object>> listOfAttributes)
        {
            var result = false;
            var editOperation = new EditOperation();

            using (var definition = enterpriseTable.GetDefinition())
            using (var rowBuffer = enterpriseTable.CreateRowBuffer())
            {
                var fields = definition.GetFields();

                editOperation.Callback(context =>
                {
                    foreach (var attributes in listOfAttributes)
                    {
                        foreach (var field in fields)
                        {
                            if (field.IsEditable && attributes.ContainsKey(field.Name))
                            {
                                rowBuffer[field.Name] = attributes[field.Name];
                            }
                        }

                        var row = enterpriseTable.CreateRow(rowBuffer);
                        context.Invalidate(row);
                        row.Dispose();
                    }
                }, enterpriseTable);

                result = await editOperation.ExecuteAsync();
            }

            return result;
        }

        public static Task<bool> CreateRowAsync(
            Table enterpriseTable,
            Dictionary<string, object> attributes)
        {
            var editOperation = new EditOperation();

            var geoField = enterpriseTable is FeatureClass fClass ?
                fClass.GetDefinition().GetShapeField() :
                "";

            editOperation.Callback(context =>
            {
                using (var rowBuffer = enterpriseTable.CreateRowBuffer())
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute.Key.Equals(ObjectIdField, StringComparison.OrdinalIgnoreCase) ||
                            attribute.Key.StartsWith(geoField + ".", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        rowBuffer[attribute.Key] = attribute.Value;
                    }

                    using (var row = enterpriseTable.CreateRow(rowBuffer))
                    {
                        // To Indicate that the attribute table has to be updated.
                        context.Invalidate(row);
                    }
                }
            }, enterpriseTable);

            return editOperation.ExecuteAsync();
        }

        public static Task<IGPResult> AddAttributeIndex(string tableAbsolutePath,
            string[] fields, string indexName)
        {
            var parameters = Geoprocessing.MakeValueArray(
                tableAbsolutePath,
                string.Join(";", fields),
                indexName,
                false,
                false
            );

            return Geoprocessing.ExecuteToolAsync(
                AddIndexToolName,
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.RefreshProjectItems);
        }

        public static Task<IGPResult> AddOrRebuildSpatialIndex(string featureClassAbsolutePath)
        {
            var parameters = Geoprocessing.MakeValueArray(
                featureClassAbsolutePath
            );

            return Geoprocessing.ExecuteToolAsync(
                AddRebuildSpatialIndexToolName,
                parameters,
                null,
                null,
                null,
                GPExecuteToolFlags.RefreshProjectItems);
        }

    }
}
