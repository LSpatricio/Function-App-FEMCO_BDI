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
                else { 
           
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {

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


        public async Task<string> bulkInserWithtDelete(DataTable dataTable, string nombreTabla)
        {
            try
            {

                if (dataTable.Rows.Count == 0)
                {
                    return "Sin datos por insertar";
                }

                else
                {

                    int r = await deleteAll(nombreTabla);

                    using (SqlConnection conn = new SqlConnection(cadenaConexion))
                    {
                        await conn.OpenAsync();

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                        {

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


        public async Task<string> bulkInserWithtDelete(DataTable dataTable, string nombreTabla, string parametros)
        {
            try
            {

                if (dataTable.Rows.Count == 0)
                {
                    return "Sin datos por insertar";
                }

                else
                {

                    int r = await deleteAllWithParams(nombreTabla, parametros);

                    using (SqlConnection conn = new SqlConnection(cadenaConexion))
                    {
                        await conn.OpenAsync();

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                        {

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

        /// <summary>
        /// Eliminar por FECHAINICIO y FECHAFIN o FECHAINICIAL FECHAFINAL
        /// </summary>
        /// <param name="nombreTabla"></param>
        /// <param name="fechaInicio"></param>
        /// <param name="fechaFin"></param>
        /// <param name="fechaInicioNombre"></param>
        /// <param name="fechaFinNombre"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        //Formato fecha MM-DD-YYYY o YYYY-MM-DD 
        public async Task<int> deleteRangeDates(string nombreTabla,DateTime? fechaInicio, DateTime? fechaFin, string fechaInicioNombre, string fechaFinNombre)
        {
            

            int r = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    string query = $@"DELETE FROM {nombreTabla} WHERE {fechaInicioNombre} >= @{fechaInicioNombre} AND {fechaFinNombre} <= @{fechaFinNombre}";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue($"@{fechaInicioNombre}", fechaInicio);
                        cmd.Parameters.AddWithValue($"@{fechaFinNombre}", fechaFin);


                        r = await cmd.ExecuteNonQueryAsync();


                    }


                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error", ex);
            }
           

            return r;
        }

        /// <summary>
        /// Método que eliminar tabla completa.
        /// </summary>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        public async Task<int> deleteAll(string nombreTabla)
        {
            int r = 0;

            try 
            {

                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();


                    string query = $"DELETE FROM {nombreTabla}";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 1200; // 1200 segundos = 20 minutos

                        r = await cmd.ExecuteNonQueryAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error", ex);
            }

            return r;

        }


        public async Task<int> deleteAllWithParams(string nombreTabla, string parametros)
        {
            int r = 0;

            try
            {

                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();


                    string query = $"DELETE FROM {nombreTabla} WHERE {parametros}";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        r = await cmd.ExecuteNonQueryAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error", ex);
            }

            return r;

        }


        /// <summary>
        /// Eliminar por FECHA
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nombreTabla"></param>
        /// <param name="fecha"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        //Puede ser tipo Date o tipo String
        public async Task<int> deleteDate<T>(string nombreTabla, T fecha )
        {

            int r = 0;

            try 
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    string query = $@"DELETE FROM {nombreTabla} WHERE FECHA = @FECHA";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {

                        cmd.Parameters.AddWithValue("@FECHA", fecha);

                        r = await cmd.ExecuteNonQueryAsync();


                    }
                }
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Error" , ex);
            }
            return r;


        }

    }


}
