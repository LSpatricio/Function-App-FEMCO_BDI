using System;

namespace FUNCTION_FEMCO_BDI.Table.Ventas.LOGINTERFASES.Classes
{
    public class CL_LOGINTERFASES
    {
        public int IdLog { get; set; }

        public string NombreInterfase { get; set; }

        public int Ejercicio { get; set; }

        public string Periodo { get; set; }

        public DateTime FechaEvento { get; set; }

        public string CodigoError { get; set; }

        public string Mensaje { get; set; }

        public string TipoEvento { get; set; }

        public string Usuario { get; set; }

    }
}