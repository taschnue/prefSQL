﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using prefSQL.SQLParser;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using prefSQL.SQLSkyline;

namespace Utility
{
    class Performance
    {
        private const string path = "E:\\Doc\\Studies\\PRJ_Thesis\\19 Performance Level\\";
        private const string cnnStringLocalhost = "Data Source=localhost;Initial Catalog=eCommerce;Integrated Security=True";
        private const string driver = "System.Data.SqlClient";

        public enum PreferenceSet
        {
            Jon,
            Mya,
            Barra
        };


        public void GeneratePerformanceQueries(SkylineStrategy strategy, bool doExecute, PreferenceSet set, int trials)
        {
            //Use the correct line, depending on how incomparable items should be compared
            string[] preferences;
            string[] columns;

            if (set == PreferenceSet.Jon)
            {
                columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.consumption", "cars.doors", "cars.seats", "cars.cylinders", "cars.gears" };
                preferences = new string[] { "cars.price LOW", "cars.mileage LOW", "cars.horsepower HIGH", "cars.enginesize HIGH", "cars.consumption LOW", "cars.doors HIGH", "cars.seats HIGH", "cars.cylinders HIGH", "cars.gears HIGH" };

            }
            else if (set == PreferenceSet.Mya)
            {
                columns = new string[] { "fuels.name", "makes.name", "bodies.name", "models.name" };
                preferences = new string[] { "fuels.name ('Benzin' >> OTHERS EQUAL)", "makes.name ('FISKER' >> OTHERS EQUAL)", "bodies.name ('Roller' >> OTHERS EQUAL)", "models.name ('123' >> OTHERS EQUAL)" };
            }
            else if (set == PreferenceSet.Barra)
            {
                columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.consumption", "cars.registration", "cars.doors", "cars.seats", "cars.cylinders", "cars.gears" };
                preferences = new string[] { "cars.price LOW 3000", "cars.mileage LOW 20000", "cars.horsepower HIGH 20", "cars.enginesize HIGH 1000", "cars.consumption LOW 10", "cars.registration HIGHDATE 525600", "cars.doors HIGH", "cars.seats HIGH 2", "cars.cylinders HIGH", "cars.gears HIGH" };
            }
            else
            {
                columns = new string[] { "TODO" };
                preferences = new string[] { "TODO" };
            }

            /*if (includeDate == true)
            {
                columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.registration", "cars.consumption", "cars.doors", "colors.name", "fuels.name", "bodies.name", "cars.title", "makes.name", "conditions.name" };
            }
            else
            {
                columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.consumption", "cars.doors", "colors.name", "fuels.name", "bodies.name", "cars.title", "makes.name", "conditions.name" };
            }
            
            
            
            if (withIncomparable == true)
            {
                preferences = new string[] { "cars.price LOW", "cars.mileage LOW", "cars.horsepower HIGH", "cars.enginesize HIGH", "cars.registration HIGHDATE", "cars.consumption LOW", "cars.doors HIGH", "colors.name ('rot' == 'blau' >> OTHERS INCOMPARABLE >> 'grau')", "fuels.name ('Benzin' >> OTHERS INCOMPARABLE >> 'Diesel')", "bodies.name ('Kleinwagen' >> 'Bus' >> 'Kombi' >> 'Roller' >> OTHERS INCOMPARABLE >> 'Pick-Up')", "cars.title ('MERCEDES-BENZ SL 600' >> OTHERS INCOMPARABLE)", "makes.name ('ASTON MARTIN' >> 'VW' == 'Audi' >> OTHERS INCOMPARABLE >> 'FERRARI')", "conditions.name ('Neu' >> OTHERS INCOMPARABLE)" };
            }
            else
            {
                if (withLeveling == true)
                {
                    preferences = new string[] { "cars.price LOW 60000", "cars.mileage LOW 60000", "cars.horsepower HIGH 80", "cars.enginesize HIGH 1000", "cars.consumption LOW 30", "colors.name ('rot' >> 'blau' >> OTHERS EQUAL >> 'grau')", "fuels.name ('Benzin' >> OTHERS EQUAL >> 'Diesel')", "bodies.name ('Kleinwagen' >> 'Bus' >> 'Kombi' >> 'Roller' >> OTHERS EQUAL >> 'Pick-Up')", "makes.name ('ASTON MARTIN' >> 'VW' == 'Audi' >> OTHERS EQUAL >> 'FERRARI')", "conditions.name ('Neu' >> OTHERS EQUAL)" };
                    columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.consumption", "colors.name", "fuels.name", "bodies.name", "makes.name", "conditions.name" };
                }
                else if(includeDate == true)
                {
                    preferences = new string[] { "cars.price LOW", "cars.mileage LOW", "cars.horsepower HIGH", "cars.enginesize HIGH", "cars.registration HIGHDATE", "cars.consumption LOW", "cars.doors HIGH", "colors.name ('rot' == 'blau' >> OTHERS EQUAL >> 'grau')", "fuels.name ('Benzin' >> OTHERS EQUAL >> 'Diesel')", "bodies.name ('Kleinwagen' >> 'Bus' >> 'Kombi' >> 'Roller' >> OTHERS EQUAL >> 'Pick-Up')", "cars.title ('MERCEDES-BENZ SL 600' >> OTHERS EQUAL)", "makes.name ('ASTON MARTIN' >> 'VW' == 'Audi' >> OTHERS EQUAL >> 'FERRARI')", "conditions.name ('Neu' >> OTHERS EQUAL)" };
                }
                else
                {
                    preferences = new string[] { "cars.price LOW", "cars.mileage LOW", "cars.horsepower HIGH", "cars.enginesize HIGH", "cars.consumption LOW", "colors.name ('rot' == 'blau' >> OTHERS EQUAL >> 'grau')", "fuels.name ('Benzin' >> OTHERS EQUAL >> 'Diesel')", "bodies.name ('Kleinwagen' >> 'Bus' >> 'Kombi' >> 'Roller' >> OTHERS EQUAL >> 'Pick-Up')", "makes.name ('ASTON MARTIN' >> 'VW' == 'Audi' >> OTHERS EQUAL >> 'FERRARI')", "conditions.name ('Neu' >> OTHERS EQUAL)" };
                    columns = new string[] { "cars.price", "cars.mileage", "cars.horsepower", "cars.enginesize", "cars.consumption", "colors.name", "fuels.name", "bodies.name", "makes.name", "conditions.name" };
                }
            }*/

            
            string[] sizes = { "small", "medium", "large", "superlarge" };
            StringBuilder sb = new StringBuilder();


            sb.AppendLine("     Algorithm:" + strategy.ToString());
            sb.AppendLine("Preference Set:" + set.ToString());
            sb.AppendLine("          Host:" + System.Environment.MachineName);
            sb.AppendLine("        Trials:" + trials);
            sb.AppendLine("");
            sb.AppendLine("dimensions|skyline size|time total|time algorithm");
            
            Debug.Write(sb);




            //Go only down two 3 dimension (because there are special algorithms for 1 and 2 dimensional skyline)
            for (int i = columns.GetUpperBound(0); i >= 2; i--)
            {
                //SELECT FROM
                string strSQL = "SELECT " + string.Join(",", columns) + " FROM cars ";
                int countJoins = 0;

                //Add Joins
                if (strSQL.IndexOf("colors") > 0)
                {
                    strSQL += "LEFT OUTER JOIN colors ON cars.color_id = colors.ID ";
                    countJoins++;
                }
                if (strSQL.IndexOf("fuels") > 0)
                {
                    strSQL += "LEFT OUTER JOIN fuels ON cars.fuel_id = fuels.ID ";
                    countJoins++;
                }
                if (strSQL.IndexOf("bodies") > 0)
                {
                    strSQL += "LEFT OUTER JOIN bodies ON cars.body_id = bodies.ID ";
                    countJoins++;
                }
                if (strSQL.IndexOf("makes") > 0)
                {
                    strSQL += "LEFT OUTER JOIN makes ON cars.make_id = makes.ID ";
                    countJoins++;
                }
                if (strSQL.IndexOf("conditions") > 0)
                {
                    strSQL += "LEFT OUTER JOIN conditions ON cars.condition_id = conditions.ID ";
                    countJoins++;
                }
                if (strSQL.IndexOf("models") > 0)
                {
                    strSQL += "LEFT OUTER JOIN models ON cars.model_id = models.ID ";
                    countJoins++;
                }


                //ADD Preferences
                strSQL += "SKYLINE OF " + string.Join(", ", preferences);


                //Convert to real SQL
                SQLCommon parser = new SQLCommon();
                parser.SkylineType = strategy; // SQLCommon.Algorithm.NativeSQL;
                //strSQL = parser.parsePreferenceSQL(strSQL);

                for (int iTrial = 0; iTrial < trials; iTrial++ )
                {

                    if (doExecute == true)
                    {
                        Stopwatch sw = new Stopwatch();

                        try
                        {
                            /*
                            Program prg = new Program();
                            prg.executeDb(strSQL, SQLCommon.Algorithm.Hexagon);
                            */

                            sw.Start();
                            DataTable dt = parser.parseAndExecutePrefSQL(cnnStringLocalhost, driver, strSQL);
                            long timeAlgorithm = parser.TimeInMilliseconds;
                            sw.Stop();


                            //Native SQL
                            /*SqlConnection connection = null;
                            connection = new SqlConnection(cnnStringLocalhost);
                            connection.Open();

                        
                            SqlDataAdapter dap = new SqlDataAdapter(strSQL, connection);
                            dap.SelectCommand.CommandTimeout = 0;
                            DataTable dt = new DataTable();
                            sw.Start();
                            dap.Fill(dt);
                            sw.Stop();
                            */



                            string strLine = (i + 1).ToString().PadLeft(10) + "|" + dt.Rows.Count.ToString().PadLeft(12) + "|" + sw.ElapsedMilliseconds.ToString().PadLeft(10) + "|" + timeAlgorithm.ToString().PadLeft(14);

                            System.Diagnostics.Debug.WriteLine(strLine);
                            sb.AppendLine(strLine);


                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                            return;
                        }
                    }
                    else
                    {
                        //Format for each of the customer profiles
                        sb.AppendLine("PRINT '----- -------------------------------------------------------- ------'");
                        sb.AppendLine("PRINT '----- " + (i + 1) + " dimensions, " + (countJoins) + " join(s) ------'");
                        sb.AppendLine("PRINT '----- -------------------------------------------------------- ------'");
                        foreach (string size in sizes)
                        {
                            sb.AppendLine("GO"); //we need this in order the profiler shows each query in a new line
                            sb.AppendLine(strSQL.Replace("cars", "cars_" + size));

                        }

                        sb.AppendLine("");
                        sb.AppendLine("");
                        sb.AppendLine("");
                    }
                }
                //Remove current column
                columns = columns.Where(w => w != columns[i]).ToArray();
                preferences = preferences.Where(w => w != preferences[i]).ToArray();
            }

            //Write in file
            string strFileName = "";
            string strFiletype = "";
            
            if(doExecute == true)
            {
                strFiletype = ".csv";
            }
            else
            {
                strFiletype = ".sql";
            }
            //create filename
            strFileName = path + "Performance_" + set.ToString() + "_" + strategy.ToString()   + strFiletype;
            
            StreamWriter outfile = new StreamWriter(strFileName);
            outfile.Write(sb.ToString());
            outfile.Close();
            Debug.WriteLine("THE END!!");

        }

    }
}
