using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AxEpsonFPHostControlX;
using Epson_Custom;
using MetodosDeExtension;
using PrinterFiscal.Class;
using System.IO;
using System.Security.Permissions;
using Newtonsoft.Json.Linq;

namespace PrinterFiscal
{
    public partial class Form2 : Form
    {
        static AxEpsonFPHostControl Impresora = null;
        static string error = "0";
        public Form2()
        {
            InitializeComponent();
            FileSystemWatcher watcher = new FileSystemWatcher("C:\\printerfiscal\\");
            watcher.Created += onChange;
            //watcher.Changed += onChange;
            watcher.EnableRaisingEvents = true;



        }

        private static void onChange(object source, FileSystemEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(4000);
                var json = File.ReadAllText(e.FullPath);
                var jObject = JObject.Parse(json);
                var jsonHeader = jObject["header"];
                JArray jsonItems = (JArray)jObject["items"];
                Header header = new Header();
                header.typeDocument = Convert.ToByte(jsonHeader["typeDocument"].ToString());
                header.copy = Convert.ToByte(jsonHeader["copy"].ToString());
                header.propina = Convert.ToBoolean(jsonHeader["propina"].ToString());
                header.densidad = jsonHeader["densidad"].ToString();
                header.sucursal = jsonHeader["sucursal"].ToString();
                header.caja = jsonHeader["caja"].ToString();
                header.ncf = jsonHeader["ncf"].ToString();
                header.clientName = jsonHeader["clientName"].ToString();
                header.clientRnc = jsonHeader["clientRnc"].ToString();
                header.ncfRef = jsonHeader["ncfRref"].ToString();
                header.monto = jsonHeader["monto"].ToString();
                header.methodPaid = jsonHeader["methodPaid"].ToString();

                List<Items> items = new List<Items>();

                foreach(var i in jsonItems)
                {
                    items.Add(new Items() { item = i["item"].ToString(), quantity = i["quantity"].ToString(), price = i["price"].ToString(), iva = i["iva"].ToString() });
                }
                imprimrDocumentoFiscal(header, items);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Mensaje(string error)
        {
            MessageBox.Show(error, Text);
            this.EscribirEnArchivoLog(error);
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
            ConectarConImpresora();
        }

        private void ConectarConImpresora()
        {
           // progressBar1.Visible = true;
            //this.Enabled = false;
            Impresora = CrearImpresoraFiscal.Impresora();
            if(Impresora == null)
            {
                Impresora = CrearImpresoraFiscal2.Impresora();
                if(Impresora == null)
                {
                    Impresora = CrearImpresoraFiscal3.Impresora();
                    if(Impresora == null)
                    {
                        Impresora = CrearImpresoraFiscal4.Impresora();

                        if(Impresora == null)
                        {
                            button1.Enabled = true;
                            Mensaje("Impresora Fuera de linea");
                        }
                    }
                }
                
            }

            if(Impresora != null)
            {
                this.Enabled = true;
                button1.Enabled = false;
                Comandos_de_comprobante_fiscal.cancelarComprobante(Impresora);
            }

            progressBar1.Visible = false;

        }
        /*
            1 Consumidor Final
            2 Facturas con derecho a credito fiscal
            3 Notas de Credito a consumidor final
            4 Notas de credito con derecho a credito fiscal
            5 factura consumidor final con exoneracion de ITBIS
            6 Facturas con derecho a credito fiscal con exoneracion de ITBIS
            7 Nota de Credito a Consumidor Final con exoneracion de ITBIS
            8 Nota de Credito con Derecho a credito fiscal con exoneracion de ITBIS
         */


        public  void imprimirDocumentoNoFiscal()
        {
            if (Impresora == null)
            {
                Mensaje("Mensaje Fuera de linea");
                return;
            }
            Comandosdedocumentonofiscal.Abrirdocumentonofiscal(Impresora);

            for (int i = 0; i < 10; i++)
            {
                error =
                Comandosdedocumentonofiscal.Imprimirlineaendocumentonofiscal(Impresora,
                    "Linea " + i);
                if (error != "0")
                {
                    Mensaje(error);
                }
            }
            error = Comandosdedocumentonofiscal.Informaciondedocumentonofiscal(Impresora);

            if (error != "0")
            {
                Mensaje(error);
            }

            Mensaje(string.Format("Lineas impresas {0}, Id{1}",
                Comandosdedocumentonofiscal.CantidadDelineasimpresas,
                Comandosdedocumentonofiscal.Numerodedocumentonofiscal));
            error = Comandosdedocumentonofiscal.Cerrardocumentonofiscal(Impresora);

            if (error != "0")
            {
                Mensaje(error);
            }
        }

        public static void imprimrDocumentoFiscal(Header header, List<Items> items)
        {
            if (Impresora == null)
            {
                MessageBox.Show("Impresora fuera de linea");
            }

            error = Comandos_de_comprobante_fiscal.AbrirDocumentoFiscal(Impresora, header.typeDocument, header.copy, header.propina, "", header.densidad, header.sucursal, header.caja, 
                header.ncf, header.clientName, header.clientRnc, header.ncfRef);
            if (error != "0")
            {
                MessageBox.Show(error);
            }

            ProcesoFacturacion(items);
            cerrarFactura(header.monto, header.methodPaid);
        }

        private static void ProcesoFacturacion(List<Items> items)
        {
            foreach (Items item in items)
            {
                error = Comandos_de_comprobante_fiscal.EnviarItem(Impresora, item.item, item.quantity, item.price, item.iva);
                if (error != "0")
                {
                    MessageBox.Show(error);
                    return;
                }

            }
        }

        public static void cerrarFactura(string monto, string methodPaid = "1")
        {
            error = Comandos_de_comprobante_fiscal.ObtenerSubTotal(Impresora);
            if (error != "0")
            {
                MessageBox.Show(error);
                return;
            }

            error = Comandos_de_comprobante_fiscal.Pagar(Impresora, methodPaid, monto, "", "", "");
            if (error != "0")
            {
                MessageBox.Show(error);
            }
            error = Comandos_de_comprobante_fiscal.ImprimirLineaEnComprobante(Impresora, "");
            if (error != "0")
            {
                MessageBox.Show(error);
                return;
            }


            error = Comandos_de_comprobante_fiscal.CerrarComprobante(Impresora);
            if (error != "0")
            {
                MessageBox.Show(error);
                return;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Desea Conectar con la Impresora", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ConectarConImpresora();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Desea imprimir el cierre z?", "",MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ComandosDeJornadaFiscal.CierreZImpreso(Impresora);
            }
            else
            {
                ComandosDeJornadaFiscal.CierreZSinImprimir(Impresora);
            }

            Mensaje("Numero de Cierre Z " + ComandosDeJornadaFiscal.NumeroDeCierreZ);
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Comandos_de_comprobante_fiscal.cancelarComprobante(Impresora);

        }
    }
}
