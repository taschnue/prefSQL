using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;


namespace prefSQL.SQLSkyline
{
    public class SP_SkylineBNL : TemplateBNL
    {
        /// <summary>
        /// Calculate the skyline points from a dataset
        /// </summary>
        /// <param name="strQuery"></param>
        /// <param name="strOperators"></param>
        [Microsoft.SqlServer.Server.SqlProcedure(Name = "SP_SkylineBNL")]
        public static void getSkyline(SqlString strQuery, SqlString strOperators, SqlInt32 numberOfRecords)
        {
            SP_SkylineBNL skyline = new SP_SkylineBNL();
            skyline.getSkylineTable(strQuery.ToString(), strOperators.ToString(), numberOfRecords.Value, false, "");
        }


        public override void addtoWindow(DataTableReader sqlReader, string[] operators, ref ArrayList resultCollection, ref ArrayList resultstringCollection, SqlDataRecord record, bool isFrameworkMode, ref DataTable dtResult)
        {
            Helper.addToWindow(sqlReader, operators, ref resultCollection, ref resultstringCollection, record, ref dtResult);
        }

        public override bool tupleDomination(ref ArrayList resultCollection, ref ArrayList resultstringCollection, DataTableReader sqlReader, string[] operators, ref DataTable dtResult, int i)
        {
            long?[] result = (long?[])resultCollection[i];
            string[] strResult = (string[])resultstringCollection[i];

            //Dominanz
            if (Helper.isTupleDominated(sqlReader, operators, result, strResult) == true)
            {
                //New point is dominated. No further testing necessary
                return true;
            }


            //Now, check if the new point dominates the one in the window
            //This is only possible with not sorted data
            if (Helper.doesTupleDominate(sqlReader, operators, result, strResult) == true)
            {
                //The new record dominates the one in the windows. Remove point from window and test further
                resultCollection.RemoveAt(i);
                resultstringCollection.RemoveAt(i);
                dtResult.Rows.RemoveAt(i);
            }
            return false;
        }

        
    }
}