using FUNCTION_FEMCO_BDI.NewFolder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace FUNCTION_FEMCO_BDI.DAO
{
    public class DAO_SQL
    {

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(5);
        private string cadenaConexion = Environment.GetEnvironmentVariable("SqlConnectionString");

        /// <summary>
        /// Método para hacer un bulk insert a la base de datos.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> bulkInsert(DataTable dataTable, string nombreTabla)
        {
            try
            {
                if (dataTable.Rows.Count == 0)
                {
                    return "Sin datos por insertar";
                }
                else
                {

                    using (SqlConnection conn = new SqlConnection(cadenaConexion))
                    {
                        await conn.OpenAsync();
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                        {
                            bulkCopy.BulkCopyTimeout = 0;
                            bulkCopy.DestinationTableName = nombreTabla;
                            await bulkCopy.WriteToServerAsync(dataTable);

                        }
                    }
                    return "Inserción completada correctamente";

                }

            }

            catch (Exception ex)
            {

                throw new InvalidOperationException($"Error al insertar los datos", ex);

            }
        }

        public async Task TruncateTable(string nombreTabla)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    string query = $"TRUNCATE TABLE {nombreTabla}";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 1200; // 1200 segundos = 20 minutos

                        await cmd.ExecuteNonQueryAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error", ex);
            }

        }

        public async Task<List<T>> getAllRows<T>(string nombreTabla) where T : class, new()
        {
            var lista = new List<T>();
            try
            {
                using (var conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    var properties = typeof(T).GetProperties();
                    string columnas = $"{string.Join(",", properties.Select(p => p.Name))} FROM {nombreTabla}";
                    string querycolumnas = $"SELECT TOP 0 {columnas}";
                    string query = $"SELECT {columnas}";


                    // 1. Obtener metadatos de columnas (índices y tipos)
                    var columnMap = new Dictionary<string, (int Index, System.Type DataType)>();
                    using (var cmd = new SqlCommand(querycolumnas, conn))
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly))
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columnMap[reader.GetName(i)] = (i, reader.GetFieldType(i));
                        }
                    }

                    // 2. Crear setters optimizados para cada propiedad
                    var setters = new Action<SqlDataReader, T>[properties.Length];

                    for (int i = 0; i < properties.Length; i++)
                    {
                        var prop = properties[i];
                        if (columnMap.TryGetValue(prop.Name, out var columnInfo))
                        {
                            setters[i] = FuncionalidadSQL.CreateTypedSetter<T>(prop, columnInfo.Index, columnInfo.DataType);
                        }
                    }

                    // 3. Ejecutar consulta y mapear resultados
                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new T();
                            foreach (var setter in setters)
                            {
                                setter?.Invoke(reader, item);
                            }
                            lista.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error", ex);
            }
            return lista;
        }

    }

}
