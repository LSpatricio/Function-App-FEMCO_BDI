using Google.Cloud.BigQuery.V2;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Funcionalidades
{
    public class FuncionalidadBigQuery
    {

        /// <summary>
        /// Convierte el resultado de una consulta BigQuery a un DataTable.
        /// </summary>
        /// <param name="results">Resultados de la consulta BigQuery.</param>
        /// <returns>DataTable generado a partir de los resultados.</returns>
      
        public static DataTable ConvertToDataTable(BigQueryResults results)
        {
            DataTable dataTable = new DataTable();

            // Verificar si hay resultados
            if (results == null || !results.Any())
            {
                return dataTable;
            }

            // Crear columnas basadas en el esquema
            dataTable.Columns.AddRange(
                results.Schema.Fields
                    .Select(field => new DataColumn(field.Name, GetColumnType(field.Type.ToString())))
                    .ToArray()
            );

            // Iterar sobre las filas y agregarlas al DataTable
            foreach (var row in results)
            {

                var rowData = new object[results.Schema.Fields.Count];
                for (int i = 0; i < results.Schema.Fields.Count; i++)
                {
                    rowData[i] = row[i]?.ToString();
                }
                dataTable.Rows.Add(rowData);
            }

            return dataTable;
        }


        public static DataTable ConvertToDataTable(BigQueryResults results, int tamaño)
        {
            DataTable dataTable = new DataTable();
            int tamañoResults = results.Count();


            // Verificar si hay resultados
            if (results == null || !results.Any())
            {
                return dataTable;
            }

            // Crear columnas basadas en el esquema
            dataTable.Columns.AddRange(
                results.Schema.Fields
                    .Select(field => new DataColumn(field.Name, GetColumnType(field.Type.ToString())))
                    .ToArray()
            );

            // Iterar sobre las filas y agregarlas al DataTable
            foreach (var row in results)
            {

                var rowData = new object[results.Schema.Fields.Count];
                for (int i = 0; i < results.Schema.Fields.Count; i++)
                {
                    rowData[i] = row[i]?.ToString();
                }
                dataTable.Rows.Add(rowData);
            }

            return dataTable;
        }


        //public static DataTable ConvertToDataTable(BigQueryResults results)
        //{
        //    DataTable dataTable = new DataTable();
        //    bool columnsInitialized = false;

        //    //Habilitar carga rápida para mejorar rendimiento
        //    dataTable.BeginLoadData();

        //    // Asignar capacidad estimada (1M registros = 1.2M para prevenir redimensionamientos)
        //    dataTable.MinimumCapacity = 2_200_000;

        //    foreach (var row in results) // Procesar en streaming
        //    {
        //        if (!columnsInitialized)
        //        {
        //            foreach (var field in row.Schema.Fields)
        //            {
        //                dataTable.Columns.Add(field.Name, GetColumnType(field.Type));
        //            }
        //            columnsInitialized = true;
        //        }

        //        // Crear y llenar el array directamente, evitando NewRow()
        //        object[] values = new object[dataTable.Columns.Count];

        //        for (int i = 0; i < values.Length; i++)
        //        {
        //            var value = row[i];

        //            // Evitar conversión innecesaria
        //            values[i] = (value == null || string.IsNullOrWhiteSpace(value.ToString())) ? DBNull.Value : value;
        //        }

        //        // Carga rápida de filas (usa un array en vez de DataRow)
        //        dataTable.Rows.Add(values);
        //    }

        //    // Finalizar modo de carga rápida
        //    dataTable.EndLoadData();

        //    return dataTable;
        //}

        public static Type GetColumnType(string bigQueryType)
        {
            bigQueryType = bigQueryType.ToLower(); // Convertir a minúsculas para evitar errores de comparación

            switch (bigQueryType)
            {
                case "date":
                case "datetime":
                    return typeof(DateTime);
                case "decimal":
                    return typeof(float);
                case "string":
                    return typeof(string);
                case "bool":
                    return typeof(bool);
                case "timestamp":
                    return typeof(DateTime);
                default:
                    return typeof(string); // Tipo por defecto
            }
        }

       

    }
}
