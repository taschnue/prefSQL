﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Tree;
using prefSQL.SQLParser.Models;


namespace prefSQL.SQLParser
{
    
    class SQLVisitor : SQLBaseVisitor<PrefSQLModel>
    {
        private Dictionary<string, string> tables = new Dictionary<string, string>();
        private bool includesTOP = false;
        private const string InnerTableSuffix = "_INNER"; //Table suffix for the inner query
        private const string RankingFunction = "ROW_NUMBER()";

        public override PrefSQLModel VisitTable_or_subquery(SQLParser.Table_or_subqueryContext context)
        {
            string strTable = context.GetChild(0).GetText();
            string strTableAlias = "";
            if (context.ChildCount == 2)
            {
                strTableAlias = context.GetChild(1).GetText();
            }
            tables.Add(strTable, strTableAlias);

            return base.VisitTable_or_subquery(context);
        }

        public override PrefSQLModel VisitTop_keyword(SQLParser.Top_keywordContext context)
        {
            includesTOP = true;
            return base.VisitTop_keyword(context);
        }


        public override PrefSQLModel VisitPreferenceLOWHIGH(SQLParser.PreferenceLOWHIGHContext context)
        {
            string strSQL = "";
            PrefSQLModel pref = new PrefSQLModel();
            string strColumn = "";
            string strFullColumnName = "";
            string strTable = "";
            string strOperator = "";
            string strRankExpression = "";
            string strRankColumn = "";

            //With only 2 expressions it is a numeric LOW preference 
            if (context.ChildCount == 2)
            {
                //Separate Column and Table
                strColumn = getColumn(context.GetChild(1));
                strTable = getTable(context.GetChild(1));
                strFullColumnName = strTable + "." + strColumn;
                //Keyword LOW or HIGH, build ORDER BY
                if (context.op.Type == SQLParser.K_LOW)
                {
                    strSQL = strColumn + " ASC";
                    strOperator = "<";
                    strRankExpression = RankingFunction + " over (ORDER BY " + strFullColumnName + " ASC) AS Rank" + strColumn;
                    strRankColumn = RankingFunction + " over (ORDER BY " + strFullColumnName + " ASC)";
                }
                else if (context.op.Type == SQLParser.K_HIGH)
                {
                    strSQL = strColumn + " DESC";
                    strOperator = ">";
                    strRankExpression = RankingFunction + " over (ORDER BY " + strFullColumnName + " DESC) AS Rank" + strColumn;
                    strRankColumn = RankingFunction + " over (ORDER BY " + strFullColumnName + " DESC)";

                }


                //Add the preference to the list               
                pref.Skyline.Add(new AttributeModel(strTable + "." + strColumn, strOperator, strTable, strTable + InnerTableSuffix, strTable + InnerTableSuffix + "." + strColumn, "", "", true, ""));
                pref.Rank.Add(new RankModel(strRankExpression, strTable, strColumn, strRankColumn));

            }
            //Otherwise it is a text LOW/HIGH preference --> Text text must be converted in a given sortorder
            else
            {

                //Build CASE ORDER with arguments
                string strExpr = context.expr().GetText();
                strColumn = getColumn(context.GetChild(1));
                strTable = getTable(context.GetChild(1));
                string[] strTemp = Regex.Split(strExpr, @"(==|>>)"); //Split signs are == and >>
                string strSQLOrderBy = "";
                string strSQLELSE = "";
                string strSQLInnerELSE = "";
                string strSQLInnerOrderBy = "";
                string strInnerColumn = "";
                string strSingleColumn = strTable + "." + getColumn(context.GetChild(1));
                string strInnerSingleColumn = strTable + InnerTableSuffix + "." + getColumn(context.GetChild(1));
                string strSQLIncomparableAttribute = "";
                string strIncomporableAttribute = "";
                string strIncomporableAttributeELSE = "";
                bool bComparable = true;

                //Define sort order value for each attribute
                int iWeight = 0;
                for (int i = 0; i < strTemp.GetLength(0); i++)
                {
                    switch (strTemp[i])
                    {
                        case ">>":
                            iWeight+=100; //Gewicht erhöhen, da >> Operator
                            break;
                        case "==":
                            break;  //Gewicht bleibt gleich da == Operator
                        case "OTHERS":
                            ////Add one, so that equal-clause cannot be true with same level-values, but other names
                            strSQLELSE = " ELSE " + (iWeight);
                            strSQLInnerELSE = " ELSE " + (iWeight + 1);
                            strIncomporableAttributeELSE = "ELSE " + strTable + "." + strColumn; //Not comparable --> give string value of field
                            bComparable = false;
                            break;
                        case "OTHERSEQUAL":
                            //Special word OTHERSEQUAL = all other attributes are defined with this order by value
                            strSQLELSE = " ELSE " + iWeight;
                            strSQLInnerELSE = " ELSE " + iWeight;
                            strIncomporableAttributeELSE = " ELSE ''"; //Comparable give empty column
                            break;
                        default:
                            //Check if it contains multiple values
                            if (strTemp[i].StartsWith("{"))
                            {
                                //Multiple values --> construct IN statement
                                strTemp[i] = strTemp[i].Replace("{", "(").Replace("}", ")");
                                strSQLOrderBy += " WHEN " + strTable + "." + strColumn + " IN " + strTemp[i] + " THEN " + iWeight.ToString();
                                //This values are always incomparable (otherwise the = should be used)
                                strSQLInnerOrderBy += " WHEN " + strTable + InnerTableSuffix + "." + strColumn + " IN " + strTemp[i] + " THEN " + (iWeight + 1);
                                //Not comparable --> give string value of field
                                strSQLIncomparableAttribute += " WHEN " + strTable + "." + strColumn + " IN " + strTemp[i] + " THEN " + strTable + "." + strColumn;
                            }
                            else
                            {
                                //Single value --> construct = statement
                                strSQLOrderBy += " WHEN " + strTable + "." + strColumn + " = " + strTemp[i] + " THEN " + iWeight.ToString();
                                //This values are always comparable (otherwise the {x, y} should be used)
                                strSQLInnerOrderBy += " WHEN " + strTable + InnerTableSuffix + "." + strColumn + " = " + strTemp[i] + " THEN " + iWeight.ToString();
                                strSQLIncomparableAttribute += " WHEN " + strTable + "." + strColumn + " = " + strTemp[i] + " THEN ''"; //comparable
                            }
                            break;
                    }

                }
                strSQL = "CASE" + strSQLOrderBy + strSQLELSE + " END";
                strInnerColumn = "CASE" + strSQLInnerOrderBy + strSQLInnerELSE + " END";
                strIncomporableAttribute = "CASE" + strSQLIncomparableAttribute + strIncomporableAttributeELSE + " END";
                strColumn = strSQL;

                //Depending on LOW or HIGH do an ASCENDING or DESCENDING sort
                if (context.op.Type == SQLParser.K_HIGH)
                {

                    strSQL += " ASC";
                    strOperator = "<";

                }
                else if (context.op.Type == SQLParser.K_LOW)
                {
                    strSQL += " DESC";
                    strOperator = ">";
                }
                strRankExpression = RankingFunction + " over (ORDER BY " + strSQL + ") AS Rank" + strSingleColumn.Replace(".", "");
                strRankColumn = RankingFunction + " over (ORDER BY " + strSQL + ")";
                //Add the preference to the list               
                pref.Skyline.Add(new AttributeModel(strColumn, strOperator, strTable, strTable + "_" + "INNER", strInnerColumn, strSingleColumn, strInnerSingleColumn, bComparable, strIncomporableAttribute));
                pref.Rank.Add(new RankModel(strRankExpression, strTable, strSingleColumn.Replace(".", ""), strRankColumn));
            }


            
            pref.OrderBy.Add(strSQL);
            return pref;

        }

        public override PrefSQLModel VisitExprPrioritize(SQLParser.ExprPrioritizeContext context)
        {
            //And was used --> visit left and right node
            PrefSQLModel left = Visit(context.expr(0));
            PrefSQLModel right = Visit(context.expr(1));

            //Add the columns to the preference model
            PrefSQLModel pref = new PrefSQLModel();
            pref.Rank.AddRange(left.Rank);
            pref.Rank.AddRange(right.Rank);
            pref.OrderBy.AddRange(left.OrderBy);
            pref.OrderBy.AddRange(right.OrderBy);
            pref.Tables = tables;
            pref.IncludesTOP = includesTOP;
            pref.HasPrioritize = true;
            return pref;

            //return base.VisitExprPrioritize(context);
        }
        public override PrefSQLModel VisitExprand(SQLParser.ExprandContext context)
        {
            //And was used --> visit left and right node
            PrefSQLModel left = Visit(context.expr(0));
            PrefSQLModel right = Visit(context.expr(1));
            
            //Add the columns to the preference model
            PrefSQLModel pref = new PrefSQLModel();
            pref.Skyline.AddRange(left.Skyline);
            pref.Skyline.AddRange(right.Skyline);
            pref.Rank.AddRange(left.Rank);
            pref.Rank.AddRange(right.Rank);
            pref.OrderBy.AddRange(left.OrderBy);
            pref.OrderBy.AddRange(right.OrderBy);
            pref.Tables = tables;
            pref.IncludesTOP = includesTOP;
            pref.HasSkyline = true;
            return pref;

        }

        

        public override PrefSQLModel VisitPreferenceAROUND(SQLParser.PreferenceAROUNDContext context)
        {
            string strSQL = "";
            PrefSQLModel pref = new PrefSQLModel();
            string strColumn = "";
            string strTable = "";
            string strOperator = "";
            string strInnerColumnExpression = "";

            //Query Keywords AROUND, FAVOUR and DISFAVOUR, after that create an ORDER BY of it

            strColumn = getColumn(context.GetChild(0));
            strTable = getTable(context.GetChild(0));

            switch (context.op.Type)
            {
                case SQLParser.K_AROUND:
                    
                    //Value should be as close as possible to a given numeric value
                    //Check if its a geocoordinate
                    if (context.GetChild(2).GetType().ToString() == "prefSQL.SQLParser.SQLParser+GeocoordinateContext")
                    {
                        strSQL = "ABS(DISTANCE(" + context.GetChild(0).GetText() + ", \"" + context .GetChild(2).GetChild(1).GetText() + "," + context.GetChild(2).GetChild(3).GetText() + "\")) ASC";
                        strColumn = "ABS(DISTANCE(" + context.GetChild(0).GetText() + ", \"" + context.GetChild(2).GetChild(1).GetText() + "," + context.GetChild(2).GetChild(3).GetText() + "\"))";
                        strInnerColumnExpression = strColumn.Replace(strTable, strTable + InnerTableSuffix);
                    }
                    else
                    {
                        strSQL = "ABS(" + context.GetChild(0).GetText() + " - " + context.GetChild(2).GetText() + ") ASC";
                        strColumn = "ABS(" + context.GetChild(0).GetText() + " - " + context.GetChild(2).GetText() + ")";
                        strInnerColumnExpression = strColumn.Replace(strTable, strTable + InnerTableSuffix);
                    }
                    strOperator = "<";

                    pref.Skyline.Add(new AttributeModel(strColumn, strOperator, strTable, strTable + InnerTableSuffix, strInnerColumnExpression, "", "", true, ""));

                    break;

                case SQLParser.K_FAVOUR:
                    //Value should be as close as possible to a given string value
                    strSQL = "CASE WHEN " + context.GetChild(0).GetText() + " = " + context.GetChild(2).GetText() + " THEN 1 ELSE 2 END ASC";
                    strColumn = "CASE WHEN " + context.GetChild(0).GetText() + " = " + context.GetChild(2).GetText() + " THEN 1 ELSE 2 END";
                    strInnerColumnExpression = strColumn.Replace(strTable, strTable + InnerTableSuffix);
                    strOperator = "<";

                    pref.Skyline.Add(new AttributeModel(strColumn, strOperator, strTable, strTable + InnerTableSuffix, strInnerColumnExpression, "", "", true, ""));

                    break;

                case SQLParser.K_DISFAVOUR:
                    //Value should be as far away as possible to a given string value
                    strSQL = "CASE WHEN " + context.GetChild(0).GetText() + " = " + context.GetChild(2).GetText() + " THEN 1 ELSE 2 END DESC";
                    strColumn = "CASE WHEN " + context.GetChild(0).GetText() + " = " + context.GetChild(2).GetText() + " THEN 1 ELSE 2 END";
                    strInnerColumnExpression = strColumn.Replace(strTable, strTable + InnerTableSuffix);
                    strOperator = ">";


                    pref.Skyline.Add(new AttributeModel(strColumn, strOperator, strTable, strTable + InnerTableSuffix, strInnerColumnExpression, "", "", true, ""));

                    break;

            }



            //Add the preference to the list               
            pref.OrderBy.Add(strSQL);
            return pref;
        }




        private string getColumn(IParseTree tree)
        {
            if (tree.ChildCount == 1)
            {
                //Syntax column only (column)
                return tree.GetText();
            }
            else
            {
                //Syntax Table with column (table.column)
                return tree.GetChild(2).GetText();
            }
        }


        private string getTable(IParseTree tree)
        {
            if (tree.ChildCount == 1)
            {
                //Syntax column only (column)
                return "";
            }
            else
            {
                //Syntax Table with column (table.column)
                return tree.GetChild(0).GetText();
            }
        }

    }
}
