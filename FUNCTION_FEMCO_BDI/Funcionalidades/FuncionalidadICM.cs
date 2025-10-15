using FUNCTION_FEMCO_BDI.DAO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace FUNCTION_FEMCO_BDI.Funcionalidades
{
    public class FuncionalidadICM
    {

      
        public static DataTable ICMToDataTable(JObject jsonEncabezado, JArray jsonContenido)
        {
            DataTable dataTable = new DataTable();


           
               foreach (JToken item in jsonEncabezado["columnDefinitions"])
            {
                dataTable.Columns.Add(item["name"].ToString(), TipoDe(item["type"].ToString()));
            }

          
         
            foreach (JArray item in jsonContenido)
            {
                    // Crear una nueva fila en el DataTable
                    DataRow row = dataTable.NewRow();

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        // Evitar excepciones si la fila tiene menos elementos
                        if (i < item.Count)
                        {
                            if (item[i].Type == JTokenType.Null || string.IsNullOrWhiteSpace(item[i].ToString()))
                            {
                            if (dataTable.Columns[i].DataType == typeof(DateTime) || dataTable.Columns[i].DataType == typeof(decimal))
                            {
                                row[i] = DBNull.Value;
                            }
                            else
                            {
                                row[i] = item[i];
                            }

                            //switch (Type.GetTypeCode(dataTable.Columns[i].DataType))
                            //{
                            //    case TypeCode.DateTime:
                            //    case TypeCode.Decimal:
                            //        row[i] = DBNull.Value;
                            //        break;
                            //    default:
                            //        row[i] = item[i];
                            //        break;
                            //}

                        }
                            else
                            {
                                row[i] = item[i];
                            }



                        }
                        else
                        {
                            row[i] = DBNull.Value; // Rellenar faltantes con nulos
                        }
                    }

                    dataTable.Rows.Add(row);
                }
            

                return dataTable;
        }


        public static DataTable ICMToDataTable(DataTable dt, JArray jsonContenido)
        {

            foreach (JArray item in jsonContenido)
            {
                // Crear una nueva fila en el DataTable
                DataRow row = dt.NewRow();

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    // Evitar excepciones si la fila tiene menos elementos
                    if (i < item.Count)
                    {
                        if (item[i].Type == JTokenType.Null || string.IsNullOrWhiteSpace(item[i].ToString()))
                        {
                            if (dt.Columns[i].DataType == typeof(DateTime) || dt.Columns[i].DataType == typeof(decimal))
                            {
                                row[i] = DBNull.Value;
                            }
                            else
                            {
                                row[i] = item[i];
                            }
                        }
                        else
                        {
                            row[i] = item[i];
                        }



                    }
                    else
                    {
                        row[i] = DBNull.Value; // Rellenar faltantes con nulos
                    }
                }

                dt.Rows.Add(row);
            }


            return dt;
        }
        private static Type TipoDe(string obj)
        {
            System.Type type;
            switch (obj.ToLower())
            {
                case "date":
                case "datetime":
                    type = typeof(DateTime);
                    break;
                case "decimal":
                    type = typeof(float);
                    break;
                case "string":
                default:
                    type = typeof(string);
                    break;
            }
            return type;
        }


        public static string AjustarConsulta(string consultaOriginal)
        {
            if (string.IsNullOrWhiteSpace(consultaOriginal))
            {
                throw new ArgumentException("La consulta original no puede ser nula o vacía.", nameof(consultaOriginal));
            }

            // Dividir la consulta para identificar SELECT, columnas y FROM.
            var parts = consultaOriginal.Split(new[] { "SELECT", "FROM" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new FormatException("La consulta original no tiene el formato esperado.");
            }

            // Procesar columnas y tabla
            string columnas = parts[0].Trim();
            string tabla = parts[1].Trim();

            // Escapar las columnas y la tabla
            string columnasEscapadas = string.Join(", ", columnas.Split(',')
                .Select(columna => $"\\\"{columna.Trim()}\\\""));
            string tablaEscapada = $"\\\"{tabla}\\\"";

            // Reconstruir la consulta ajustada
            return $"SELECT {columnasEscapadas} FROM {tablaEscapada}";
        }


        public static DataTable getdates()
        {

            DataTable table = new DataTable();
            table.Columns.Add("DateStart", typeof(DateTime));
            table.Columns.Add("DateEnd", typeof(DateTime));

            DateTime dateEnd = DateTime.Today;
            //DateTime dateStart = dateEnd.AddMonths(-2);
            DateTime dateStart = new DateTime(dateEnd.Year, dateEnd.Month, 1).AddMonths(-2);

            table.Rows.Add(dateStart, dateEnd);

            return table;


        }

        public static DataTable getdates(int meses)
        {

            DataTable table = new DataTable();
            table.Columns.Add("DateStart", typeof(DateTime));
            table.Columns.Add("DateEnd", typeof(DateTime));

            DateTime dateEnd = DateTime.Today;
            //DateTime dateStart = dateEnd.AddMonths(-meses);
            DateTime dateStart = new DateTime(dateEnd.Year, dateEnd.Month, 1).AddMonths(-meses);

            table.Rows.Add(dateStart, dateEnd);

            return table;


        }

    }
}
