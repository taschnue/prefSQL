using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using Microsoft.SqlServer.Server;

//Caution: Attention small changes in this code can lead to performance issues, i.e. using a startswith instead of an equal can increase by 10 times
//Important: Only use equal for comparing text (otherwise performance issues)
namespace prefSQL.SQLSkyline
{
    public class SPMultipleSkylineBNL
    {
        /// <summary>
        /// Calculate the skyline points from a dataset
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="strOperators"></param>
        /// <param name="numberOfRecords"></param>
        /// <param name="sortType"></param>
        /// <param name="upToLevel"></param>
        [SqlProcedure(Name = "SP_MultipleSkylineBNL")]
        public static void GetSkyline(SqlString strQuery, SqlString strOperators, SqlInt32 numberOfRecords, SqlInt32 sortType, SqlInt32 upToLevel)
        {
            SPMultipleSkylineBNL skyline = new SPMultipleSkylineBNL();
            SqlContext.Pipe.Send("upto: " +upToLevel.Value.ToString());
            skyline.GetSkylineTable(strQuery.ToString(), strOperators.ToString(), numberOfRecords.Value, false, Helper.CnnStringSqlclr, Helper.ProviderClr, sortType.Value, upToLevel.Value);

        }


        public DataTable GetSkylineTable(string strQuery, string strOperators, string strConnection, string strProvider, int numberOfRecords, int sortType, int upToLevel)
        {
            return GetSkylineTable(strQuery, strOperators, numberOfRecords, true, strConnection, strProvider, sortType, upToLevel);
        }


        private DataTable GetSkylineTable(String strQuery, String strOperators, int numberOfRecords, bool isIndependent, string strConnection, string strProvider, int sortType, int upToLevel)
        {
            ArrayList resultCollection = new ArrayList();
            ArrayList resultstringCollection = new ArrayList();
            string[] operators = strOperators.Split(';');
            DataTable dtResult = new DataTable();

            DbProviderFactory factory = DbProviderFactories.GetFactory(strProvider);

            // use the factory object to create Data access objects.
            DbConnection connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = strConnection;
                

                try
                {
                    connection.Open();

                    DbDataAdapter dap = factory.CreateDataAdapter();
                    DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandTimeout = 0; //infinite timeout
                    selectCommand.CommandText = strQuery;
                    if (dap != null)
                    {
                        dap.SelectCommand = selectCommand;
                        DataTable dt = new DataTable();
                        dap.Fill(dt);


                        //trees erstellen mit n nodes (n = anzahl tupels)
                        //int[] levels = new int[dt.Rows.Count];
                        List<int> levels = new List<int>();


                        // Build our record schema 
                        List<SqlMetaData> outputColumns = Helper.BuildRecordSchema(dt, operators, dtResult);
                        //Add Level column
                        SqlMetaData outputColumnLevel = new SqlMetaData("Level", SqlDbType.Int);
                        outputColumns.Add(outputColumnLevel);
                        dtResult.Columns.Add("level", typeof(int));
                        SqlDataRecord record = new SqlDataRecord(outputColumns.ToArray());
                        if (isIndependent == false)
                        {
                            SqlContext.Pipe.SendResultsStart(record);
                        }

                        int iMaxLevel = 0;
                
                        List<object[]> listObjects = Helper.GetItemArraysAsList(dt);               

                        foreach (object[] dbValuesObject in listObjects)
                        {
                            //Check if window list is empty
                            if (resultCollection.Count == 0)
                            {
                                // Build our SqlDataRecord and start the results 
                                levels.Add(0);
                                iMaxLevel = 0;
                                AddToWindow(dbValuesObject, operators, resultCollection, resultstringCollection, record, isIndependent, levels[levels.Count - 1], dtResult);
                            }
                            else
                            {

                                //Insert the new record to the tree
                                bool bFound = false;

                                //Start wie level 0 nodes (until uptolevels or maximum levels)
                                for (int iLevel = 0; iLevel <= iMaxLevel && iLevel < upToLevel; iLevel++)
                                {
                                    bool isDominated = false;
                                    for (int i = 0; i < resultCollection.Count; i++)
                                    {
                                        if (levels[i] == iLevel)
                                        {
                                            long?[] result = (long?[])resultCollection[i];
                                            string[] strResult = (string[])resultstringCollection[i];

                                            //Dominanz
                                            /*if (Helper.IsTupleDominated(operators, result, strResult, dbValuesObject))
                                            {
                                                //Dominated in this level. Next level
                                                isDominated = true;
                                                break;
                                            }*/
                                        }
                                    }
                                    //Check if the record is dominated in this level
                                    if (isDominated == false)
                                    {
                                        levels.Add(iLevel);
                                        bFound = true;
                                        break;
                                    }
                                }
                                if (bFound == false)
                                {
                                    iMaxLevel++;
                                    if (iMaxLevel < upToLevel)
                                    {
                                        levels.Add(iMaxLevel);
                                        AddToWindow(dbValuesObject, operators, resultCollection, resultstringCollection, record, isIndependent, levels[levels.Count - 1], dtResult);
                                    }
                                }
                                else
                                {
                                    AddToWindow(dbValuesObject, operators, resultCollection, resultstringCollection, record, isIndependent, levels[levels.Count - 1], dtResult);
                                }
                            }
                        }
                    }


                    if (isIndependent == false)
                    {
                        SqlContext.Pipe.SendResultsEnd();
                    }


                }
                catch (Exception ex)
                {
                    //Pack Errormessage in a SQL and return the result
                    string strError = "Fehler in SP_MultipleSkylineBNL: ";
                    strError += ex.Message;

                    if (isIndependent)
                    {
                        Debug.WriteLine(strError);

                    }
                    else
                    {
                        SqlContext.Pipe.Send(strError);
                    }

                }
                finally
                {
                    connection.Close();
                }
            }
            return dtResult;
        }


        private void AddToWindow(object[] newTuple, string[] operators, ArrayList resultCollection, ArrayList resultstringCollection, SqlDataRecord record, SqlBoolean isFrameworkMode, int level, DataTable dtResult)
        {

            //Erste Spalte ist die ID
            long?[] recordInt = new long?[operators.GetUpperBound(0) + 1];
            string[] recordstring = new string[operators.GetUpperBound(0) + 1];
            DataRow row = dtResult.NewRow();

            for (int iCol = 0; iCol <= newTuple.GetUpperBound(0); iCol++)
            {
                //Only the real columns (skyline columns are not output fields)
                if (iCol <= operators.GetUpperBound(0))
                {
                    //LOW und HIGH Spalte in record abf�llen
                    if (operators[iCol].Equals("LOW"))
                    {
                        if (newTuple[iCol] == DBNull.Value)
                            recordInt[iCol] = null;
                        else
                            recordInt[iCol] = (long)newTuple[iCol];
                        
                        //Check if long value is incomparable
                        if (iCol + 1 <= recordInt.GetUpperBound(0) && operators[iCol + 1].Equals("INCOMPARABLE"))
                        {
                            //Incomparable field is always the next one
                            recordstring[iCol] = (string)newTuple[iCol + 1];
                        }
                    }

                }
                else
                {
                    row[iCol - (operators.GetUpperBound(0) + 1)] = newTuple[iCol];
                    record.SetValue(iCol - (operators.GetUpperBound(0) + 1), newTuple[iCol]);
                }
            }
            row[record.FieldCount - 1] = level;
            record.SetValue(record.FieldCount - 1, level);

            if (isFrameworkMode == true)
            {
                dtResult.Rows.Add(row);
            }
            else
            {
                SqlContext.Pipe.SendResultsRow(record);
            }
            resultCollection.Add(recordInt);
            resultstringCollection.Add(recordstring);
        }




    }
}