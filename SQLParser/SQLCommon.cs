﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Tree.Pattern;
using prefSQL.SQLParser.Models;
using System.Diagnostics;
using System.Data;
using prefSQL.SQLSkyline;
using System.Text.RegularExpressions;

namespace prefSQL.SQLParser
{
    /// <summary>
    /// Entry point of the library for parsing a PREFERENCE SQL to an ANSI-SQL Statement
    /// </summary>
    /// <remarks>
    /// You can choose the Skyline algorithm, the maxium level in multiple skyline algorithms and if the SELECT List should be extended with the Skyline Values
    /// </remarks>
    public class SQLCommon
    {
        private const string SkylineOf = "SKYLINE OF";
        private SkylineStrategy _SkylineType = new SkylineSQL();    //Defines with which Algorithm the Skyline should be calculated
        private bool _ShowSkylineAttributes = false;                //Defines if the skyline attributes should be added to the SELECT list
        private int _SkylineUpToLevel = 3;                          //Defines the maximum level that should be returned for the multiple skyline algorithnmm

        /*
        public enum Algorithm
        {
            NativeSQL,              //Works with ANSI-SQL syntax
            BNL,                    //Block nested loops
            BNLSort,                //Block nested loops with presort
            DQ,                     //Divide and Conquer
            Hexagon,                //Hexagon Augsburg
            MultipleBNL,            //Multiple Skyline
        };*/

        public enum Ordering
        {
            AttributePosition,      //Sorted according to the attribute position in the select list
            RankingSummarize,       //Sorted according to the the sum of the rank of all attributes
            RankingBestOf,          //Sorted according to the best rank of all attributes
            Random,                 //Randomly sorted, Every query results in a different sort order
            AsIs                    //Without OrderBy-Clause as it comes from the database
        }

        public bool ShowSkylineAttributes
        {
            get { return _ShowSkylineAttributes; }
            set { _ShowSkylineAttributes = value; }
        }

        public SkylineStrategy SkylineType
        {
            get { return _SkylineType; }
            set { _SkylineType = value; }
        }

        public int SkylineUpToLevel
        {
            get { return _SkylineUpToLevel; }
            set { _SkylineUpToLevel = value; }
        }

        /// <summary>
        /// Parse a prefSQL Query and return the result as a DataTable.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="driverString"></param>
        /// <param name="strPrefSQL"></param>
        /// <param name="algorithm"></param>
        /// <param name="upToLevel"></param>
        /// <returns>Returns a DataTable with the requested values</returns>
        public DataTable parseAndExecutePrefSQL(string connectionString, string driverString, String strPrefSQL)
        {
            bool withIncomparable = false;
            string strSQL = parsePreferenceSQL(strPrefSQL, ref withIncomparable);
            Debug.WriteLine(strSQL);
            Helper helper = new Helper();
            helper.ConnectionString = connectionString;
            helper.DriverString = driverString;
            return helper.getResults(strSQL, _SkylineType, withIncomparable);
        }

        /// <summary>Parses a PREFERENE SQL Statement in an ANSI SQL Statement</summary>
        /// <param name="strInput">Preference SQL Statement</param>
        /// <returns>Return the ANSI SQL Statement</returns>
        public string parsePreferenceSQL(string strInput)
        {
            bool withIncomparable = false;
            string strSQL = parsePreferenceSQL(strInput, ref withIncomparable);
            return strSQL;
        }


        /// <summary>Parses a PREFERENE SQL Statement in an ANSI SQL Statement</summary>
        /// <param name="strInput">Preference SQL Statement</param>
        /// <returns>Return the ANSI SQL Statement</returns>
        private string parsePreferenceSQL(string strInput, ref bool withIncomparable)
        {
            AntlrInputStream inputStream = new AntlrInputStream(strInput);
            SQLLexer sqlLexer = new SQLLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(sqlLexer);
            SQLParser parser = new SQLParser(commonTokenStream);
            SQLSort sqlSort = new SQLSort();
            SQLCriterion sqlCriterion = new SQLCriterion();
            string strSQLReturn = ""; //The SQL-Query that is built on the basis of the prefSQL 


            try
            {
                //Add error listener to parser (helps to return detailed parser syntax errors)
                ErrorListener listener = new ErrorListener();
                parser.AddErrorListener(listener);

                //Parse query
                IParseTree tree = parser.parse();
                Debug.WriteLine("Parse Tree: " + tree.ToStringTree(parser));

                //Visit parsetree (PrefSQL model is built during the visit of the parse tree)
                SQLVisitor visitor = new SQLVisitor();
                visitor.IsNative = _SkylineType.isNative();
                visitor.Visit(tree);
                PrefSQLModel prefSQL = visitor.Model;
                
                
                //Check if parse was successful and query contains PrefSQL syntax
                if (prefSQL != null && strInput.IndexOf(SkylineOf) > 0)
                {
                    //Mark as incomparable if needed (to choose the correct algorithm)
                    withIncomparable = prefSQL.WithIncomparable;

                    //Add all Syntax before the Skyline-Clause
                    strSQLReturn = strInput.Substring(0, strInput.IndexOf(SkylineOf) - 1);

                    if (prefSQL.HasSkyline == true)
                    {
                        //Add Skyline Attributes to select list. This option is i.e. useful to create a dominance graph.
                        //With help of the skyline values it is easier to create this graph
                        if (_ShowSkylineAttributes == true)
                        {
                            //Add the attributes to the existing SELECT clause
                            string strSQLSelectClause = getSelectClauseForSkylineAttributes(prefSQL);
                            string strSQLBeforeFrom = strSQLReturn.Substring(0, strSQLReturn.IndexOf("FROM"));
                            string strSQLAfterFromShow = strSQLReturn.Substring(strSQLReturn.IndexOf("FROM"));
                            strSQLReturn = strSQLBeforeFrom + strSQLSelectClause + " " + strSQLAfterFromShow;
                            
                        }

                        //Add ORDER BY Clause
                        string strOrderBy = "";
                        if (strInput.IndexOf("ORDER BY") > 0)
                        {
                            if (prefSQL.Ordering == Ordering.AsIs)
                            {
                                string strTmpInput = strInput;

                                //Replace category clauses
                                //Start with latest order by (otherwise substring start, stop position are changed)
                                for (int iIndex = prefSQL.OrderBy.Count - 1; iIndex >= 0; iIndex--)
                                {
                                    OrderByModel model = prefSQL.OrderBy[iIndex];
                                    strTmpInput = strTmpInput.Substring(0, model.start) + model.text + strTmpInput.Substring(model.stop);
                                }

                                strOrderBy = strTmpInput.Substring(strInput.IndexOf("ORDER BY"));
                            }
                            else
                            {
                                strOrderBy = sqlSort.getSortClause(prefSQL, prefSQL.Ordering); // sqlSort.getSortClause(prefSQL, _OrderType);
                            }
                        }


                        ////////////////////////////////////////////
                        //attributes used for native sql algorithm
                        string strWHERE = sqlCriterion.getCriterionClause(prefSQL, strSQLReturn);
                        
                        ////////////////////////////////////////////
                        //attributes used for other algorithms
                        string strOperators = "";
                        string strAttributesSkyline = buildPreferencesBNL(prefSQL, ref strOperators);
                        //Without SELECT 

                        //Remove TOP keyword, expect for the native SQL algorithm
                        if (prefSQL.HasTop == true && SkylineType.isNative() == false)
                        {
                            //Remove Top Keyword in inner clause
                            int iPosTop = strSQLReturn.IndexOf("TOP");
                            int iPosTopEnd = strSQLReturn.Substring(iPosTop + 3).TrimStart().IndexOf(" ");
                            string strSQLAfterTOP = strSQLReturn.Substring(iPosTop + 3).TrimStart();
                            strSQLReturn = strSQLReturn.Substring(0, iPosTop) + strSQLAfterTOP.Substring(iPosTopEnd + 1);
                        }


                        string strAttributesOutput = ", " + strSQLReturn.Substring(7, strSQLReturn.IndexOf("FROM") - 7);
                        string strSQLAfterFrom = strSQLReturn.Substring(strSQLReturn.IndexOf("FROM"));

                        string strFirstSQL = "SELECT " + strAttributesSkyline + " " + strAttributesOutput + strSQLAfterFrom;

                        string strOrderByAttributes = sqlSort.getSortClause(prefSQL, SQLCommon.Ordering.AttributePosition);


                        ////////////////////////////////////////////
                        //attributes used for hexagon
                        string[] additionalParameters = new string[5];

                        string strOperatorsHexagon = "";
                        string strAttributesSkylineHexagon = buildSELECTHexagon(prefSQL, strSQLReturn, ref strOperatorsHexagon);
                        //Without SELECT 

                        
                        //Quote quotes because it is a parameter of the stored procedure
                        string strFirstSQLHexagon = "SELECT " + strAttributesSkylineHexagon + " " + strAttributesOutput + strSQLAfterFrom;
                        strFirstSQLHexagon = strFirstSQLHexagon.Replace("'", "''");

                        //Quote quotes because it is a parameter of the stored procedure
                        string strSelectDistinctIncomparable = "";
                        int weightHexagonIncomparable = 0;
                        string strHexagon = buildSELECTMaxHexagon(prefSQL, strSQLReturn, ref strSelectDistinctIncomparable, ref weightHexagonIncomparable);
                        strSelectDistinctIncomparable = strSelectDistinctIncomparable.Replace("'", "''");
                        strHexagon = strHexagon.Replace("'", "''");

                        additionalParameters[0] = strFirstSQLHexagon;
                        additionalParameters[1] = strOperatorsHexagon;
                        additionalParameters[2] = strHexagon;
                        additionalParameters[3] = strSelectDistinctIncomparable;
                        additionalParameters[4] = weightHexagonIncomparable.ToString();

                        
                        
                        //Now create the query depending on the Skyline algorithm
                        strSQLReturn = _SkylineType.getStoredProcedureCommand(strSQLReturn, strWHERE, strOrderBy, prefSQL.NumberOfRecords, strFirstSQL, strOperators, _SkylineUpToLevel, prefSQL.WithIncomparable, strOrderByAttributes, additionalParameters);

                    }
                }
                else
                {
                    //Query does not contain a preference --> return original query
                    strSQLReturn = strInput;
                }
            }

            catch (Exception e)
            {
                //Parse syntaxerror
                /// <exception cref="Exception">This is exception is thrown because the String is not a valid PrefSQL Query</exception>
                throw new Exception(e.Message);
            }
            
            return strSQLReturn;

        }



        /// <summary>TODO</summary>
        /// <param name="model">model of parsed Preference SQL Statement</param>
        /// <param name="strPreSQL">Preference SQL Statement WITHOUT PREFERENCES</param>
        /// <param name="strOperators">Returns the operators</param>
        /// <returns>TODO</returns>
        private string buildSELECTHexagon(PrefSQLModel model, string strPreSQL, ref string strOperators)
        {
            strOperators = "";
            string strSQL = "";

            //Add a RankColumn for each PRIORITIZE preference
            for (int iChild = 0; iChild < model.Skyline.Count; iChild++)
            {
                //Replace ROW_NUMBER with Rank, for the reason that multiple tuples can have the same value (i.e. mileage=0)
                string strRank = model.Skyline[iChild].RankHexagon;
                strSQL += ", " + strRank;
                strOperators += "LOW" + ";";
                if (model.Skyline[iChild].Comparable == false && model.Skyline[iChild].AmountOfIncomparables > 0)
                {
                    strSQL += ", " + model.Skyline[iChild].HexagonIncomparable;
                    if (model.Skyline[iChild].AmountOfIncomparables == 99)
                    {
                        strOperators += "CALCULATEINCOMPARABLE;";
                    }
                    else
                    {
                        //CASE WHEN  colors.name IN ('blau') THEN '001' WHEN colors.name IN ('silber') THEN '010' ELSE '100' END AS RankColorNew
                        for (int iIncomparable = 0; iIncomparable < model.Skyline[iChild].AmountOfIncomparables; iIncomparable++)
                        {
                            strOperators += "INCOMPARABLE;";
                        }
                    }
                }
            }

            //Add the ranked column before the FROM keyword
            strSQL = strSQL.TrimStart(',');
            strOperators = strOperators.TrimEnd(';');

            return strSQL;
        }



        /// <summary>TODO</summary>
        /// <param name="model">model of parsed Preference SQL Statement</param>
        /// <param name="strPreSQL">Preference SQL Statement WITHOUT PREFERENCES</param>
        /// <param name="strOperators">Returns the operators</param>
        /// <returns>TODO</returns>
        private string buildSELECTMaxHexagon(PrefSQLModel model, string strPreSQL, ref string strDistinctSelect, ref int weight)
        {
            string strSQL = "";
            string strMaxSQL = "";
            int posOfFROM = 0;

            //Add a RankColumn for each PRIORITIZE preference
            for (int iChild = 0; iChild < model.Skyline.Count; iChild++)
            {
                //Replace ROW_NUMBER with Rank, for the reason that multiple tuples can have the same value (i.e. mileage=0)
                string strRank = model.Skyline[iChild].RankHexagon;
                strSQL += ", " + strRank;
                strMaxSQL += ", MAX(Rank" + model.Skyline[iChild].RankColumnName + ")";
                
                //Add additional columns if attribute is incomparable
                if (model.Skyline[iChild].Comparable == false && model.Skyline[iChild].AmountOfIncomparables > 0)
                {
                    //strMaxSQL += "+1";
                    //99 means OTHER INCOMPARABLE --> not clear at the moment how many distinct values exists
                    if (model.Skyline[iChild].AmountOfIncomparables == 99)
                    {
                        strMaxSQL += "CALCULATEINCOMPARABLE";
                        strDistinctSelect = model.Skyline[iChild].IncomparableAttribute;
                        weight = model.Skyline[iChild].WeightHexagonIncomparable;
                    }
                    else
                    {
                        strMaxSQL += "+1";
                        for (int iIncomparable = 0; iIncomparable < model.Skyline[iChild].AmountOfIncomparables; iIncomparable++)
                        {
                            strMaxSQL += ", 1";
                        }
                    }
                    
                    
                }
            }
            strMaxSQL = strMaxSQL.TrimStart(',');
            strSQL = strSQL.TrimStart(',');
            strSQL = "SELECT " + strMaxSQL + " FROM (SELECT " + strSQL;

            //Add the ranked column before the FROM keyword
            posOfFROM = strPreSQL.IndexOf("FROM");
            strSQL = strSQL + strPreSQL.Substring(posOfFROM - 1) + ") MyQuery";

            return strSQL;
        }


        /// <summary>This method is used for Pareto Dominance Grahps. It adds the Skyline Attributes to the SELECT List.</summary>
        /// <remarks>
        /// For the reason that comparing the values is easier, smaller values are always better than higher.
        /// Therefore HIGH preferences are multiplied with -1 
        /// Every preference gets 2 output values. the 1st declares the level, the second the exact value if an additional comparison
        /// is needed, because of incomparability
        /// </remarks>
        /// <param name="model">model of parsed Preference SQL Statement</param>
        /// <returns>Return the extended SQL Statement</returns>
        private string getSelectClauseForSkylineAttributes(PrefSQLModel model)
        {
            string strSQL = "";

            //Build Skyline only if more than one attribute
            if (model.Skyline.Count > 0)
            {
                //Build the where clause with each column in the skyline
                for (int iChild = 0; iChild < model.Skyline.Count; iChild++)
                {
                    string strFullColumnName = model.Skyline[iChild].FullColumnName.Replace(".", "_");
                    if (model.Skyline[iChild].Op.Equals("<"))
                    {
                        strSQL += ", " + model.Skyline[iChild].ColumnExpression + " AS SkylineAttribute" + strFullColumnName;
                    }
                    else
                    {
                        //Multiply HIGH preferences with -1 --> small values are always better than high 
                        strSQL += ", " + model.Skyline[iChild].ColumnExpression + "*-1 AS SkylineAttribute" + strFullColumnName;
                    }

                    //Incomparable field --> Add string field
                    if (model.Skyline[iChild].Comparable == false)
                    {
                        strSQL += ", " + model.Skyline[iChild].IncomparableAttribute + " AS SkylineAttributeIncomparable" + strFullColumnName;
                    }

                }
            }

            return strSQL;
        }


        /// <summary>TODO</summary>
        /// <param name="model">model of parsed Preference SQL Statement</param>
        /// <param name="strPreSQL">Preference SQL Statement WITHOUT PREFERENCES</param>
        /// <param name="strOperators">Returns the operators</param>
        /// <returns>TODO</returns>
        private string buildPreferencesBNL(PrefSQLModel model, ref string strOperators)
        {
            string strSQL = "";
            strOperators = "";

            //Build Skyline only if more than one attribute
            if (model.Skyline.Count > 0)
            {
                //Build the where clause with each column in the skyline
                for (int iChild = 0; iChild < model.Skyline.Count; iChild++)
                {
                    if (model.Skyline[iChild].Op.Equals("<"))
                    {
                        strSQL += ", " + model.Skyline[iChild].ColumnExpression + " AS SkylineAttribute" + iChild;
                    }
                    else
                    {
                        //Trick: Convert HIGH attributes in negative values (leads to better performance)
                        strSQL += ", " + model.Skyline[iChild].ColumnExpression + "*-1 AS SkylineAttribute" + iChild;

                    }
                    strOperators += "LOW;";

                    //Incomparable field --> Add string field
                    if (model.Skyline[iChild].Comparable == false)
                    {
                        strSQL += ", " + model.Skyline[iChild].IncomparableAttribute;
                        strOperators += "INCOMPARABLE" + ";";
                    }
                }
            }

            strOperators = strOperators.TrimEnd(';');
            strSQL = strSQL.TrimStart(',');
            return strSQL;
        }

        public SkylineSamplingModel GetSkylineSamplingModelFromPreferenceSql(string preferenceSql)
        {
            if (SkylineType.GetType() != typeof(SkylineBNL))
            {
                throw new Exception(
                    "Not yet implemented for other SkylineStrategies than SkylineBNL. Please set SQLCommon#SkylineType to 'new SkylineBNL()'.");
            }

            var ansiSql = parsePreferenceSQL(preferenceSql);

            string strQuery = "";
            string strOperators = "";
            int numberOfRecords = 0;
            string[] parameter = null;

            int iPosStart = ansiSql.IndexOf("'");
            string strtmp = ansiSql.Substring(iPosStart);
            parameter = Regex.Split(strtmp, ",(?=(?:[^']*'[^']*')*[^']*$)");

            strQuery = parameter[0].Trim();
            strOperators = parameter[1].Trim();
            numberOfRecords = int.Parse(parameter[2].Trim());
            strQuery = strQuery.Replace("''", "'").Trim('\'');
            strOperators = strOperators.Replace("''", "'").Trim('\'');

            string[] operators = strOperators.Split(';');

            var allAttributes = Regex.Match(strQuery, "(SELECT )(.*)( FROM.*$)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var preString = allAttributes.Groups[1].ToString();
            string[] attributes = allAttributes.Groups[2].ToString().Split(',');
            var postString = allAttributes.Groups[3].ToString();

            const string randomSubsetsCountDimensionPattern =
                "SAMPLE BY RANDOM_SUBSETS COUNT ([1-9][0-9]*) DIMENSION ([1-9][0-9]*)";

            var m = Regex.Match(preferenceSql, randomSubsetsCountDimensionPattern,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (!m.Success)
            {
                throw new Exception(
                    "No 'SAMPLE BY RANDOM_SUBSETS COUNT x DIMENSION y' found. If you do not want to use the Skyline Sampling heuristics, please consider calling SQLCommon#parsePreferenceSQL resp. SQLCommon#parseAndExecutePrefSQL instead.");
            }

            var randomSubsetsCount = int.Parse(m.Groups[1].ToString());
            var randomSubsetsDimension = int.Parse(m.Groups[2].ToString());
            var skylineSamplingModelReturn = new SkylineSamplingModel(randomSubsetsCount, randomSubsetsDimension,
                preString.Trim(), postString.Trim());

            foreach (var op in operators)
            {
                skylineSamplingModelReturn.SkylineOperators.Add(op.Trim());
            }

            foreach (var attribute in attributes)
            {
                if (attribute.Contains("SkylineAttribute"))
                {
                    skylineSamplingModelReturn.SkylineAttributes.Add(attribute.Trim());
                }                
            }

            return skylineSamplingModelReturn;
        }
    }
}

