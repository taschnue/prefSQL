﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;



namespace prefSQL.SQLParser
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args) 
        {
            (new Program()).Run();
        }


        public void Run()
        {
            try
            {
                //Ablauf aktuell
                /*
                    1.Pareto Front
                    2.Filter auf die Kritik (z.B. Preis < 50000')
                    3.Pareto Front aufgrund der Präferenzen
                    4.Similarity berechnen
                */

                //String strPrefSQL = "SELECT cars.id, cars.title, colors.name, fuels.name FROM cars " +
                //String strPrefSQL = "SELECT cars.id, cars.title, cars.price, colors.name, mileage FROM cars " +
                String strPrefSQL = "SELECT cars.id, cars.title, cars.Price, colors.Name FROM cars " +
                //String strPrefSQL = "SELECT cars.id, cars.Price, cars.mileage FROM cars " +
                //String strPrefSQL = "SELECT cars.id, cars.title, cars.price, cars.mileage, cars.horsepower, cars.enginesize, cars.registration, cars.consumption, cars.doors, colors.name, fuels.name FROM cars " +
                //String strPrefSQL = "SELECT cars.id, cars.title, colors.name AS colourname, fuels.name AS fuelname, cars.price FROM cars " +
                    //String strPrefSQL = "SELECT id FROM cars " +
                    "LEFT OUTER JOIN colors ON cars.color_id = colors.ID " +
                    //"LEFT OUTER JOIN fuels ON cars.fuel_id = fuels.ID " +
                    //"WHERE cars.horsepower > 10 AND cars.price < 10000 " +
                    //"PREFERENCE LOW colors.name {'rot' == 'blau' >> OTHERS >> 'grau'} AND LOW cars.price";
                    //"PREFERENCE LOW cars.title {'MERCEDES-BENZ SL 600' >> OTHERS} AND LOW cars.price";
                    //"PREFERENCE LOW colors.name {'rot' >> OTHERS} AND LOW cars.price";
                    //"PREFERENCE Low cars.price AND Low cars.mileage ";
                    //"PREFERENCE LOW cars.price AND LOW cars.mileage AND HIGH cars.horsepower AND HIGH cars.enginesize AND HIGH cars.registration AND LOW cars.consumption AND HIGH cars.doors AND LOW colors.name {'rot' == 'blau' >> OTHERS >> 'grau'} AND LOW fuels.name {'Benzin' >> OTHERS >> 'Diesel'}";
                    //"PREFERENCE LOW cars.price AND LOW cars.mileage AND LOW fuels.name {'Benzin' >> OTHERS >> 'Diesel'}";
                    "PREFERENCE LOW cars.price AND LOW colors.name {'rot' >> OTHERS}";
                   // "PREFERENCE LOW cars.price AND LOW colors.name {'pink' >> 'rot' == 'schwarz' >> 'beige' == 'gelb'}";

                    //"PREFERENCE LOW cars.price AND LOW colors.name {'pink' >> {'rot', 'schwarz'} >> 'beige' == 'gelb'}";

                    //"PREFERENCE LOW colors.name {'gelb' >> OTHERS >> 'grau'} AND LOW fuels.name {'Benzin' >> OTHERS >> 'Diesel'} AND LOW cars.price ";
                    //"PREFERENCE colors.name DISFAVOUR 'rot' ";
                    //"PREFERENCE cars.location AROUND (47.0484, 8.32629) ";
                Console.WriteLine(strPrefSQL);
                Console.WriteLine("--------------------------------------------");


                SQLCommon parser = new SQLCommon();
                parser.SkylineType = SQLCommon.Algorithm.NativeSQL;
                //parser.SkylineType = SQLCommon.Algorithm.BNL;
                String strSQL = parser.parsePreferenceSQL(strPrefSQL);

                Console.WriteLine(strSQL);

                Console.WriteLine("------------------------------------------\nDONE");
                

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex);
                Console.Write("Hit RETURN to exit: ");
            }

            Environment.Exit(0);
        }



    }
}
