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
        /// Método para insertar un registro en la base de datos.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<int> Insert<T>(string query, string requestBody) where T : class
        {
            int r = 0;


            try
            {

            T datosBody = JsonConvert.DeserializeObject<T>(requestBody);

            using (SqlConnection conn = new SqlConnection(cadenaConexion))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    var propierties = typeof(T).GetProperties();

                    foreach (var property in propierties) 
                    {
                          
                        SqlParameter sqlParameter = new SqlParameter($"@{property.Name}", property.GetValue(datosBody));
                        cmd.Parameters.Add(sqlParameter);

                    }

                     r = await cmd.ExecuteNonQueryAsync();
                     

                }

            }
            
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);
            }

            return r;
        }

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

                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);

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

                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);

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

                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);

            }
        }

        /// <summary>
        /// BulkInsert es utilizado cuando la tabla SQL tiene ID Identity
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task BulkInsertAsync(DataTable dataTable, string nombreTabla)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = nombreTabla;

                        // Excluir la columna IDENTITY al mapear las columnas
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            if (column.ColumnName != "ID") // Ajusta "ID" al nombre real de la columna IDENTITY
                            {
                                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                            }
                        }

                        await bulkCopy.WriteToServerAsync(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Método bulk insert por hilos
        /// </summary>
        /// <param name="dataTableOriginal"></param>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        public async Task bulkInsert(DataTable dataTableOriginal, string nombreTabla,int tamaño)
        {

            List<DataTable> dataTables = FuncionalidadSQL.DividirDataTable(dataTableOriginal, tamaño);

            var tasks = new List<Task>();

            foreach (var dataTable in dataTables)
            {
                tasks.Add(BulkInsertWithSemaphore(dataTable, nombreTabla));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Método Semaphore que ejecuta los múltiples hilos del método bulk
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task BulkInsertWithSemaphore(DataTable dataTable, string nombreTabla)
        {
            await semaphore.WaitAsync();
            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = nombreTabla;
                        await bulkCopy.WriteToServerAsync(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);
            }
            finally
            {
                semaphore.Release(); 
            }
        }


        /// <summary>
        /// Método sobrecarga para hacer un bulk insert a la base de datos evitando duplicados.
        /// </summary>
        /// <param name="dataTableInsertar"></param>
        /// <param name="dataTableActual"></param>
        /// <param name="nombreTabla"></param>
        /// <param name="columnaUnique"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>        
        public async Task bulkInsert(DataTable dataTableInsertar, DataTable dataTableActual,string nombreTabla, string columnaUnique)
        {

            try
            {


                var valoresExistentes = dataTableActual.AsEnumerable()
                .Select(row => row[columnaUnique])  // Asumiendo que "Columna1" es una columna de tipo adecuado
                .ToHashSet();
               
                var filasFiltradas = dataTableInsertar.AsEnumerable()
                    .Where(newRow => !valoresExistentes.Contains(newRow[columnaUnique]));

                if (!filasFiltradas.Any())
                {
                    throw new ArgumentException("No hay valores nuevos por insertar");

                }

                var valoresInsertar = filasFiltradas.CopyToDataTable();

                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.DestinationTableName = nombreTabla;
                        await bulkCopy.WriteToServerAsync(valoresInsertar);
                    }


                }

            }
            catch (Exception ex)
            {

                throw new InvalidOperationException($"Error al insertar los datos: {ex.Message}", ex);

            }
        }



        //Primer método de regreso de lista con el que se trabajo.

        /// <summary>
        /// Método para ver tabla o vista, regresa una lista
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        //public async Task<List<T>> getAllRows<T>(string nombreTabla) where T : class, new()
        //{

        //    List<T> lista = new List<T>();

        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(cadenaConexion))
        //        {
        //            await conn.OpenAsync();

        //            PropertyInfo[] properties = typeof(T).GetProperties();

        //            string query = $"SELECT {string.Join(",", properties.Select(p => p.Name))} FROM {nombreTabla}";

        //            using (SqlCommand cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.CommandTimeout = 0;
        //                using (SqlDataReader r = await cmd.ExecuteReaderAsync())
        //                {
        //                    while (await r.ReadAsync())
        //                    {

        //                        T item = new T();

        //                        foreach (var prop in properties)
        //                        {

        //                            object value = r[prop.Name];

        //                            if (value == DBNull.Value)
        //                            {
        //                                // Verificar si la propiedad es Nullable
        //                                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        //                                {
        //                                    // Asignar null si es Nullable
        //                                    prop.SetValue(item, null);
        //                                }
        //                                else if (prop.PropertyType == typeof(string))
        //                                {
        //                                    // Asignar null si es string
        //                                    prop.SetValue(item, null);
        //                                }
        //                                else
        //                                {
        //                                    // Para otros tipos (por ejemplo, int, DateTime), asignar el valor por defecto
        //                                    prop.SetValue(item, Activator.CreateInstance(prop.PropertyType));
        //                                }
        //                            }
        //                            else
        //                            {
        //                                // Si no es DBNull, asignamos el valor normalmente
        //                                prop.SetValue(item, value);
        //                            }

        //                        }

        //                        lista.Add(item);

        //                    }

        //                }
        //            }
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        throw new InvalidOperationException($"Error: {ex.Message}", ex);
        //    }

        //    return lista;

        //}



        //Método optimizado del regreso de la información en lista.

    

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
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
            }
            return lista;
        }



        public async Task<List<T>> getRowsParams<T>(string nombreTabla, string parametros) where T : class, new()
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
                    string query = $"SELECT {columnas} {parametros}";


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
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
            }
            return lista;
        }

        /// <summary>
        /// Método para obtener la tabla en un DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nombreTabla"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<DataTable> getAllRowsDataTable<T>(string nombreTabla) where T : class
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    PropertyInfo[] properties = typeof(T).GetProperties();

                    foreach (var prop in properties)
                    {
                        dt.Columns.Add(prop.Name);
                    }

                    string query = $"SELECT {string.Join(",", properties.Select(p => p.Name))} FROM {nombreTabla}";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader r = await cmd.ExecuteReaderAsync())
                        {

                            while (await r.ReadAsync())
                            {
                                DataRow row = dt.NewRow();


                                foreach (var prop in properties)
                                {
                                    row[prop.Name] = r[prop.Name];
                                }

                                dt.Rows.Add(row);



                            }

                        }
                    }


                }


            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
            }

            return dt;

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
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
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
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
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
                throw new InvalidOperationException($"Error: {ex.Message}", ex);
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
                throw new InvalidOperationException($"Error: {ex.Message}" , ex);
            }
            return r;


        }

        public async Task LogFunctionExecution(MethodBase method, string logLevel, string message, string exception = null)
        {
            if (method.DeclaringType == null)
            {
                throw new ArgumentNullException(nameof(method.DeclaringType), "DeclaringType es nulo.");
            }

            // Obtener la clase donde está definido el método
            Type declaringType = method.DeclaringType;

            // Obtener el nombre completo de la clase
            string className = declaringType.FullName;

            // Obtener el ensamblado donde está definida la clase
            string assemblyName = declaringType.Assembly.GetName().Name;




            var query = "INSERT INTO FunctionLogs (FunctionName, LogLevel, Message, Exception, Timestamp) VALUES (@FunctionName, @LogLevel, @Message, @Exception, GETDATE())";

            using (var connection = new SqlConnection(cadenaConexion))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FunctionName", className);
                    command.Parameters.AddWithValue("@LogLevel", logLevel);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@Exception", (object)exception ?? DBNull.Value);
                    await command.ExecuteNonQueryAsync();
                }
            }

        }


        public async Task<DataTable> Execute_Stored_Procedure_Datatable(string storedProcedureName)
        {
            DataTable datatable = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(cadenaConexion))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            datatable.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al ejecutar {storedProcedureName}: {ex.Message}", ex);
            }

            return datatable;
        }



    }


}
