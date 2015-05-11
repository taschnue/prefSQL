using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.SqlServer.Server;

//!!!Caution: Attention small changes in this code can lead to remarkable performance issues!!!!
namespace prefSQL.SQLSkyline
{

    /// <summary>
    /// BNL Algorithm implemented according to algorithm pseudocode in B�rzs�nyi et al. (2001)
    /// </summary>
    /// <remarks>
    /// B�rzs�nyi, Stephan; Kossmann, Donald; Stocker, Konrad (2001): The Skyline Operator. In : 
    /// Proceedings of the 17th International Conference on Data Engineering. Washington, DC, USA: 
    /// IEEE Computer Society, pp. 421�430. Available online at http://dl.acm.org/citation.cfm?id=645484.656550.
    /// 
    /// Profiling considersations:
    /// - Always use equal when comparins test --> i.e. using a startswith instead of an equal can decrease performance by 10 times
    /// - Write objects from DataReader into an object[] an work with the object. 
    /// - Explicity convert (i.e. (int)reader[0]) value from DataReader and don't use the given methods (i.e. reader.getInt32(0))
    /// </remarks>
    public abstract class TemplateBNL : TemplateStrategy
    {
        /*protected override DataTable GetSkylineTable(String strQuery, String strOperators, int numberOfRecords,
            bool isIndependent, string strConnection, string strProvider)
        {
            string[] operators = strOperators.Split(';');
            DataTable dt = Helper.GetSkylineDataTable(strQuery, isIndependent, strConnection, strProvider);
            List<object[]> listObjects = Helper.GetObjectArrayFromDataTable(dt);
            DataTable dtResult = new DataTable();
            SqlDataRecord record = Helper.BuildDataRecord(dt, operators, dtResult);

            return GetSkylineTable(listObjects, dtResult, record, strOperators, numberOfRecords, isIndependent);
        }*/

        /*public DataTable GetSkylineTable(List<object[]> database, SqlDataRecord dataRecordTemplate, string operators,
            int numberOfRecords, DataTable dataTableTemplate)
        {
            return GetSkylineTable(database, dataTableTemplate, dataRecordTemplate, operators, numberOfRecords, true);
        }*/


        protected override DataTable GetCompleteSkylineTable(List<object[]> database, DataTable dataTableTemplate, SqlDataRecord dataRecordTemplate, string operators, int numberOfRecords, bool isIndependent, string[] additionalParameters)
        {
            ArrayList resultCollection = new ArrayList();
            ArrayList resultstringCollection = new ArrayList();
            string[] operatorsArray = operators.Split(';');
            DataTable dataTableReturn = dataTableTemplate.Clone();
            int[] resultToTupleMapping = Helper.ResultToTupleMapping(operatorsArray);

            try
            {


                //For each tuple
                foreach (object[] dbValuesObject in database)
                {

                    //Check if window list is empty
                    if (resultCollection.Count == 0)
                    {
                        // Build our SqlDataRecord and start the results 
                        AddtoWindow(dbValuesObject, operatorsArray, resultCollection, resultstringCollection, dataRecordTemplate, true, dataTableReturn);
                    } else
                    {
                        bool isDominated = false;

                        //check if record is dominated (compare against the records in the window)
                        for (int i = resultCollection.Count - 1; i >= 0; i--)
                        {
                            if (TupleDomination(dbValuesObject, resultCollection, resultstringCollection, operatorsArray, dataTableReturn, i, resultToTupleMapping))
                            {
                                isDominated = true;
                                break;
                            }
                        }
                        if (isDominated == false)
                        {
                            AddtoWindow(dbValuesObject, operatorsArray, resultCollection, resultstringCollection, dataRecordTemplate, true, dataTableReturn);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return dataTableReturn;
        }



        protected abstract bool TupleDomination(object[] dataReader, ArrayList resultCollection, ArrayList resultstringCollection, string[] operators, DataTable dtResult, int i, int[] resultToTupleMapping);

        protected abstract void AddtoWindow(object[] dataReader, string[] operators, ArrayList resultCollection, ArrayList resultstringCollection, SqlDataRecord record, bool isFrameworkMode, DataTable dtResult);

    }
}
