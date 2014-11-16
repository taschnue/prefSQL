﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Diagnostics;

namespace Utility
{
    //Hinweis: Wenn mit startswith statt equals gearbeitet wird führt dies zu massiven performance problemen, z.B. large dataset 30 statt 3 Sekunden mit 13 Dimensionen!!
    //WICHTIG: Vergleiche immer mit equals und nie mit z.B. startsWith oder Contains oder so.... --> Enorme Performance Unterschiede

    //same as the SP SkyineBNL --> for testing the performance and debugging
    class SkylineBNL
    {
        //Only this parameters are different to the SQL CLR function
        private const bool bSQLCLR = false;
        private const string connectionstring = "Data Source=localhost;Initial Catalog=eCommerce;Integrated Security=True";
        private const int MaxSize = 4000;

        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void SP_SkylineBNL(SqlString strQuery, SqlString strOperators, SqlString strQueryNative, string strTable)
        {
            ArrayList idCollection = new ArrayList();
            ArrayList resultCollection = new ArrayList();
            ArrayList resultstringCollection = new ArrayList();
            string[] operators = strOperators.ToString().Split(';');


            SqlConnection connection = new SqlConnection(connectionstring);
            try
            {
                connection.Open();


                //Some checks
                if (strQuery.ToString().Length == MaxSize)
                {
                    throw new Exception("Query is too long. Maximum size is " + MaxSize);
                }


                SqlCommand sqlCommand = new SqlCommand(strQuery.ToString(), connection);
                SqlDataReader sqlReader = sqlCommand.ExecuteReader();

                //Read all records only once. (SqlDataReader works forward only!!)
                while (sqlReader.Read())
                {

                    //Check if window list is empty
                    if (resultCollection.Count == 0)
                    {
                        addToWindow(sqlReader, operators, ref resultCollection, ref idCollection, ref resultstringCollection);
                    }
                    else
                    {
                        bool bDominated = false;

                        //check if record is dominated (compare against the records in the window)
                        for (int i = resultCollection.Count - 1; i >= 0; i--)
                        {
                            int[] result = (int[])resultCollection[i];
                            string[] strResult = (string[])resultstringCollection[i];

                            //Dominanz
                            if (compare(sqlReader, operators, result, strResult) == true)
                            {
                                //New point is dominated. No further testing necessary
                                bDominated = true;
                                break;
                            }


                            //Now, check if the new point dominates the one in the window
                            //It is not possible that the new point dominates the one in the window --> Raason data is ORDERED


                        }
                        if (bDominated == false)
                        {
                            addToWindow(sqlReader, operators, ref resultCollection, ref idCollection, ref resultstringCollection);


                        }

                    }
                }

                sqlReader.Close();

                //TODO: Debug is forbidden in SQL CRL
                Debug.WriteLine("Total in Skyline: " + idCollection.Count);


                //OTHER Idea: Store current collection in temporary table and return the result of the table

                //SQLDataReader wokrs only forward. There read new with parameters
                string cmdText = strQueryNative.ToString() + " WHERE ({0})";

                ArrayList paramNames = new ArrayList();
                string strIN = "";
                string inClause = "";
                int amountOfSplits = 0;
                for (int i = 0; i < idCollection.Count; i++)
                {
                    if (i % 2000 == 0)
                    {
                        if (amountOfSplits > 0)
                        {
                            //Add OR after IN
                            strIN += " OR ";
                            //Remove first comma
                            inClause = inClause.Substring(1);
                            strIN = string.Format(strIN, inClause);
                            inClause = "";
                        }
                        strIN += strTable + ".id IN ({0})";


                        amountOfSplits++;
                    }

                    inClause += ", " + idCollection[i];

                }
                //Remove first comman
                inClause = inClause.Substring(1);
                strIN = string.Format(strIN, inClause);


                sqlCommand = new SqlCommand(string.Format(cmdText, strIN), connection);
                sqlReader = sqlCommand.ExecuteReader();



                


            }
            catch (Exception ex)
            {
                //Pack Errormessage in a SQL and return the result

                string strError = "SELECT 'Fehler in SP_SkylineBNL: ";
                strError += ex.Message.Replace("'", "''");
                strError += "'";

                SqlCommand sqlCommand = new SqlCommand(strError, connection);
                SqlDataReader sqlReader = sqlCommand.ExecuteReader();



            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }

        }


        private static void addToWindow(SqlDataReader sqlReader, string[] operators, ref ArrayList resultCollection, ref ArrayList idCollection, ref ArrayList resultstringCollection)
        {


            //Liste ist leer --> Das heisst erster Eintrag ins Window werfen
            //Erste Spalte ist die ID
            int[] record = new int[sqlReader.FieldCount];
            string[] recordstring = new string[sqlReader.FieldCount];
            for (int i = 0; i <= record.GetUpperBound(0); i++)
            {
                //LOW und HIGH Spalte in record abfüllen
                if (operators[i].Equals("LOW") || operators[i].Equals("HIGH"))
                {
                    Type type = sqlReader.GetFieldType(i);
                    if (type == typeof(int))
                    {
                        record[i] = sqlReader.GetInt32(i);
                    }
                    else if (type == typeof(DateTime))
                    {
                        record[i] = sqlReader.GetDateTime(i).Year * 10000 + sqlReader.GetDateTime(i).Month * 100 + sqlReader.GetDateTime(i).Day;
                    }

                    //Check if long value is incomparable
                    if (i+1 <= record.GetUpperBound(0) && operators[i+1].Equals("INCOMPARABLE"))
                    {
                        //Incomparable field is always the next one
                        type = sqlReader.GetFieldType(i+1);
                        if (type == typeof(string))
                        {
                            recordstring[i] = sqlReader.GetString(i + 1);
                        }

                    }




                }
               
            }
            resultCollection.Add(record);
            idCollection.Add(sqlReader.GetInt32(0));
            resultstringCollection.Add(recordstring);
        }


        private static bool compare(SqlDataReader sqlReader, string[] operators, int[] result, string[] stringResult) 
        {
            bool greaterThan = false;

            //bool equalThan = false;
            //bool greaterThan = false;
            for (int iCol = 0; iCol <= result.GetUpperBound(0); iCol++)
            {
                string op = operators[iCol];
                //Compare only LOW and HIGH attributes
                if (op.Equals("LOW") || op.Equals("HIGH"))
                {
                    //Convert value if it is a date
                    int value = 0;
                    Type type = sqlReader.GetFieldType(iCol);
                    if (type == typeof(int))
                    {
                        value = sqlReader.GetInt32(iCol);
                    }
                    else if (type == typeof(DateTime))
                    {
                        value = sqlReader.GetDateTime(iCol).Year * 10000 + sqlReader.GetDateTime(iCol).Month * 100 + sqlReader.GetDateTime(iCol).Day;
                    }

                    int comparison = compareValue(op, value, result[iCol]);

                    if (comparison >= 1)
                    {
                        if (comparison == 2)
                        {
                            //at least one must be greater than
                            greaterThan = true;
                        }
                        else
                        {
                            //It is the same long value
                            //Check if the value must be text compared
                            if(iCol+1 <= result.GetUpperBound(0) && operators[iCol+1].Equals("INCOMPARABLE"))
                            {
                                //string value is always the next field
                                string strValue = sqlReader.GetString(iCol + 1);
                                //If it is not the same string value, the values are incomparable!!
                                if (!strValue.Equals(stringResult[iCol]))
                                {
                                    //Value is incomparable --> return false
                                    return false;
                                }

                                
                            }
                        }
                    }
                    else
                    {
                        //Value is smaller --> return false
                        return false;
                    }
                    
                    
                }
            }


            //all equal and at least one must be greater than
            //if (equalTo == true && greaterThan == true)
            if (greaterThan == true)
                return true;
            else
                return false;



        }
        



        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void SP_SkylineBNL_Level(SqlString strQuery, SqlString strOperators, SqlString strQueryNative, string strTable)
        {
            ArrayList idCollection = new ArrayList();
            ArrayList resultCollection = new ArrayList();
            string[] operators = strOperators.ToString().Split(';');
            

            SqlConnection connection = new SqlConnection(connectionstring);
            try
            {
                //Some checks
                if (strQuery.ToString().Length == MaxSize)
                {
                    throw new Exception("Query is too long. Maximum size is " + MaxSize);
                }

                connection.Open();
                SqlCommand sqlCommand = new SqlCommand(strQuery.ToString(), connection);
                SqlDataReader sqlReader = sqlCommand.ExecuteReader();
                DataTable dt = new DataTable();

                

                dt.Load(sqlReader);

                sqlReader = sqlCommand.ExecuteReader();

                //int iRow = 0;
                //Read all records only once. (SqlDataReader works forward only!!)
                while (sqlReader.Read())
                {
                    

                    //Check if window list is empty
                    if (resultCollection.Count == 0)
                    {
                    addToWindow_Level(sqlReader, operators, ref resultCollection, ref idCollection);
                    }
                    else
                    {
                        bool bDominated = false;

                        //check if record is dominated (compare against the records in the window)
                        for (int i = resultCollection.Count - 1; i >= 0; i--)
                        {
                            int[] result = (int[])resultCollection[i];

                            //Dominanz
                            if (compare_Level(sqlReader, operators, result) == true)
                            {
                                //New point is dominated. No further testing necessary
                                bDominated = true;
                                break;

                            }

                            //Now, check if the new point dominates the one in the window
                            //It is not possible that the new point dominates the one in the window --> Raason data is ORDERED

                        }
                        if (bDominated == false)
                        {
                            addToWindow_Level(sqlReader, operators, ref resultCollection, ref idCollection);
                        }

                    }
                }

                sqlReader.Close();

                //TODO: Debug is forbidden in SQL CRL
                Debug.WriteLine("Total in Skyline: " + idCollection.Count);


                //OTHER Idea: Store current collection in temporary table and return the result of the table

                //SQLDataReader wokrs only forward. There read new with parameters
                string cmdText = strQueryNative.ToString() + " WHERE ({0})";

                ArrayList paramNames = new ArrayList();
                string strIN = "";
                string inClause = "";
                int amountOfSplits = 0;
                for (int i = 0; i < idCollection.Count; i++)
                {
                    if (i % 2000 == 0)
                    {
                        if (amountOfSplits > 0)
                        {
                            //Add OR after IN
                            strIN += " OR ";
                            //Remove first comma
                            inClause = inClause.Substring(1);
                            strIN = string.Format(strIN, inClause);
                            inClause = "";
                        }
                        strIN += strTable + ".id IN ({0})";


                        amountOfSplits++;
                    }

                    inClause += ", " + idCollection[i];

                }
                //Remove first comman
                inClause = inClause.Substring(1);
                strIN = string.Format(strIN, inClause);


                sqlCommand = new SqlCommand(string.Format(cmdText, strIN), connection);
                sqlReader = sqlCommand.ExecuteReader();



            }
            catch (Exception ex)
            {
                //Pack Errormessage in a SQL and return the result

                string strError = "SELECT 'Fehler in SP_SkylineBNL: ";
                strError += ex.Message.Replace("'", "''");
                strError += "'";

                
                SqlCommand sqlCommand = new SqlCommand(strError, connection);
                SqlDataReader sqlReader = sqlCommand.ExecuteReader();

                SqlContext.Pipe.Send(sqlReader);


            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }

        }


        private static void addToWindow_Level(SqlDataReader sqlReader, string[] operators, ref ArrayList resultCollection, ref ArrayList idCollection)
        {
            //Liste ist leer --> Das heisst erster Eintrag ins Window werfen
            //Erste Spalte ist die ID
            int[] record = new int[sqlReader.FieldCount];
            string[] recordstring = new string[sqlReader.FieldCount];
            for (int i = 0; i <= record.GetUpperBound(0); i++)
            {
                //LOW und HIGH Spalte in record abfüllen
                if (operators[i].Equals("LOW") || operators[i].Equals("HIGH"))
                {
                    Type type = sqlReader.GetFieldType(i);
                    if (type == typeof(int))
                    {
                        record[i] = sqlReader.GetInt32(i);
                    }
                    else if (type == typeof(DateTime))
                    {
                        //record[i] = sqlReader.GetDateTime(i).Ticks; 
                        record[i] = sqlReader.GetDateTime(i).Year * 10000 + sqlReader.GetDateTime(i).Month * 100 + sqlReader.GetDateTime(i).Day;
                    }


                }

            }
            resultCollection.Add(record);
            idCollection.Add(sqlReader.GetInt32(0));
        }



        private static bool compare_Level(SqlDataReader sqlReader, string[] operators, int[] result)
        {
            bool greaterThan = false;
            
            for (int iCol = 0; iCol <= result.GetUpperBound(0); iCol++)
            {
                string op = operators[iCol];
                //Compare only LOW and HIGH attributes
                if (op.Equals("LOW") || op.Equals("HIGH"))
                {
                    //Convert value if it is a date
                    int value = 0;
                    Type type = sqlReader.GetFieldType(iCol);
                    if (type == typeof(int))
                    {
                        value = sqlReader.GetInt32(iCol);
                    }
                    else if (type == typeof(DateTime))
                    {
                        //value = sqlReader.GetDateTime(iCol).Ticks;
                        value = sqlReader.GetDateTime(iCol).Year * 10000 + sqlReader.GetDateTime(iCol).Month * 100 + sqlReader.GetDateTime(iCol).Day;
                    }

                    int comparison = compareValue(op, value, result[iCol]);
                    
                    if (comparison >= 1)
                    {
                        if (comparison == 2)
                        {
                            //at least one must be greater than
                            greaterThan = true;
                        }
                  
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            //all equal and at least one must be greater than
            if (greaterThan == true)
                return true;
            else
                return false;


        }





        /*
         * 0 = false
         * 1 = equal
         * 2 = greater than
         * */
        private static int compareValue(string op, int value1, int value2)
        {

            //Switch numbers on certain case
            if(op.Equals("HIGH"))
            {
                int tmpValue = value1;
                value1 = value2;
                value2 = tmpValue;
            }


            if (value1 >= value2)
            {
                if (value1 > value2)
                    return 2;
                else
                    return 1;

            }
            else
            {
                return 0;
            }

        }








    }
}
