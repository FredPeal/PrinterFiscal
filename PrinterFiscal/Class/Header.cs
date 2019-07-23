using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrinterFiscal.Class
{
    public class Header
    {
        public byte typeDocument { get; set; }
        public byte copy { get; set; }

        public bool propina { get; set; }

        public string densidad { get; set; }

        public string sucursal { get; set; }

        public string caja { get; set; }
        public string ncf { get; set; }

        public string clientName { get; set; }

        public string clientRnc { get; set; }

        public string ncfRef { get; set; }
        public string monto { get; set; }

        public string methodPaid { get; set; }

    }
}
