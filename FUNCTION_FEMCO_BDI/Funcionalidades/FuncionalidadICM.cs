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
                case "int":
                    type = typeof(float);
                    break;
                case "string":
                default:
                    type = typeof(string);
                    break;
            }
            return type;
        }
     
        public static string ConsultaAjustada(string tabla, string parametros ="",string columnas="")
        {
            string consultaAjustada = "";

            if (string.IsNullOrWhiteSpace(tabla))
            {
                throw new ArgumentException("La tabla no puede ser nulas o vacías.");
            }

            string columnasConsulta = string.IsNullOrEmpty(columnas) ? "COUNT(*)" : columnas;
            
            consultaAjustada = $"SELECT {columnasConsulta} FROM \\\"{tabla}\\\" {parametros}";

            return consultaAjustada;
        }

        public static string FormatearColumnas(List<string> columnas)
        {
            if(string.IsNullOrEmpty(columnas.ToString()))
            {
                throw new ArgumentException("La lista de columnas no puede ser nula o vacía.", nameof(columnas));
            }

            string columnasFormateadas = String.Join(", ", columnas.Select(c => $"\\\"{c}\\\""));

            return columnasFormateadas;
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
        public static DataTable CrearColumnasQuerytool(JArray jsonEncabezados)
        {
            DataTable dt = new DataTable();

            foreach (JObject columnDefinition in jsonEncabezados)
            {
                DataColumn column = new DataColumn(columnDefinition["name"].ToString());
                //Regresa typeOf
                column.DataType = FuncionalidadICM.TipoDe(columnDefinition["type"].ToString());
                dt.Columns.Add(column);

            }
            return dt;
        }

        public static void LlenarDataTableQuerytool(JArray jsonData, DataTable dt)
        {
            foreach (JArray dataItem in jsonData)
            {
                DataRow row = dt.NewRow();

                for (int i = 0; i < dt.Columns.Count; i++)
                {

                    var token = dataItem[i];

                    // Si es null o vacío → DBNull
                    if (token.Type == JTokenType.Null || string.IsNullOrWhiteSpace(token.ToString()))
                    {
                        if (dt.Columns[i].DataType==typeof(string))
                        {
                            row[i] = token.ToString();
                        }
                        else
                        {
                            row[i] = DBNull.Value;
                        }
                    }
                    else
                    {
                        var colType = dt.Columns[i].DataType;

                        if (colType == typeof(Int32))
                            row[i] = token.ToObject<int>();
                        else if (colType == typeof(decimal))
                            row[i] = token.ToObject<decimal>();
                        else if (colType == typeof(DateTime))
                            row[i] = token.ToObject<DateTime>();
                        else
                            row[i] = token.ToString(); // para strings u otros tipos
                    }
                }
                dt.Rows.Add(row);
            }
        }

    }
}
