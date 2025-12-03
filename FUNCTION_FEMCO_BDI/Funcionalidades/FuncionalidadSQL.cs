using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.NewFolder
{
    public class FuncionalidadSQL
    {
        //Crear query insert de un registro
        public string insertQuery<T>() where T : class
        {
        
                string nombreTabla = typeof(T).Name.Remove(0, 3);

                var propierties = typeof(T).GetProperties();


                string query = $"INSERT INTO {nombreTabla} (";

                foreach (var property in propierties)
                {
                    query = $@"{query}
                            {property.Name},";
                }

                query = query.Remove(query.Length - 1);

                query = $@"{query})
                        VALUES(";

                foreach (var property in propierties)
                {
                    query = $@"{query}
                            @{property.Name},";
                }

                query = query.Remove(query.Length - 1);

                query = $"{query})";

                return query;

        }

        public static DataTable getdates()
        {

            DataTable table = new DataTable();
            table.Columns.Add("DateStart", typeof(DateTime));
            table.Columns.Add("DateEnd", typeof(DateTime));

            DateTime dateEnd = DateTime.Today;
            DateTime dateStart = dateEnd.AddMonths(-3);

            table.Rows.Add(dateStart, dateEnd);

            return table;


        }
        public static Action<SqlDataReader, T> CreateTypedSetter<T>(PropertyInfo prop, int columnIndex, System.Type sqlType)
        {
            // Obtener el tipo subyacente si es Nullable<T>
            System.Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // Verificar si la propiedad es Nullable
            bool isNullable = prop.PropertyType.IsGenericType &&
                              prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

            // Crear setters específicos por tipo
            if (targetType == typeof(int))
            {
                if (isNullable)
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? (int?)null : r.GetInt32(columnIndex));
                }
                else
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? default(int) : r.GetInt32(columnIndex));
                }
            }
            else if (targetType == typeof(decimal))
            {
                if (isNullable)
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? (decimal?)null : r.GetDecimal(columnIndex));
                }
                else
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? default(decimal) : r.GetDecimal(columnIndex));
                }
            }
            else if (targetType == typeof(DateTime))
            {
                if (isNullable)
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? (DateTime?)null : r.GetDateTime(columnIndex));
                }
                else
                {
                    return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? default(DateTime) : r.GetDateTime(columnIndex));
                }
            }
            else if (targetType == typeof(string))
            {
                return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? null : r.GetString(columnIndex));
            }

            // Fallback para otros tipos (usando GetValue)
            return (r, obj) => prop.SetValue(obj, r.IsDBNull(columnIndex) ? null : r.GetValue(columnIndex));
        }

    }
}
