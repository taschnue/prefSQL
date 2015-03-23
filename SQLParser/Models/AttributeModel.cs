﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prefSQL.SQLParser.Models
{
    class AttributeModel
    {

        public AttributeModel(string strColumnExpression, string strOperator, string strInnerColumnExpression, string strFullColumnName, string strInnerColumnName, bool isComparable, string strIncomporableAttribute, string strRankColumn, string strRankHexagon, string strOrderBy, bool isCategory, string strHexagonIncomparable, int amountIncomparable, int weightHexagonIncomparable, string strExpression)
        {
            ColumnExpression = strColumnExpression;                 //Column expression                 (i.e. CASE WHEN colors.name = 'türkis' THEN 0 WHEN colors.name = 'gelb' THEN 100 ELSE 200 END)
            InnerColumnExpression = strInnerColumnExpression;       //Inner column expression           (i.e CASE WHEN colors_INNER.name = 'türkis' THEN 0 WHEN colors_INNER.name = 'gelb' THEN 100 ELSE 200 END)
            Op = strOperator;                                       //Operator                          (<, >)
            FullColumnName = strFullColumnName;                     //Used for the additional OR with text values (i.e. OR colors_INNER.name = colors.name)
            InnerColumnName = strInnerColumnName;                   //Dito
            
            IsCategory = isCategory;                                //Defines if it is categorical preference (Used for the additional OR-Clause in native SQL)
            Expression = strExpression;                             //
            RankColumn = strRankColumn;
            OrderBy = strOrderBy;

            Comparable = isComparable;                              //Check if at least one value is incomparable
            IncomparableAttribute = strIncomporableAttribute;       //Attribute that returns the textvalue if the value is incomparable
            AmountOfIncomparables = amountIncomparable;


            HexagonRank = strRankHexagon;
            HexagonIncomparable = strHexagonIncomparable;
            HexagonWeightIncomparable = weightHexagonIncomparable;
        }

        public AttributeModel(string strColumnExpression, string strOperator, string strInnerColumnExpression, string strFullColumnName, string strInnerColumnName, bool isComparable, string strIncomporableAttribute, string strRankColumn, string strRankHexagon, string strOrderBy, bool isCategory, string strHexagonIncomparable, int amountIncomparable, string strExpression)
            : this(strColumnExpression, strOperator, strInnerColumnExpression, strFullColumnName, strInnerColumnName, isComparable, strIncomporableAttribute, strRankColumn, strRankHexagon, strOrderBy, isCategory, strHexagonIncomparable, amountIncomparable, 0, strExpression)
        {
        }

        public string Expression { get; set; }

        public int AmountOfIncomparables { get; set; }
        public string FullColumnName { get; set; }

        public string InnerColumnName { get; set; }

        public string ColumnExpression { get; set; }

        public string InnerColumnExpression { get; set; }

        //Operator
        public string Op { get; set; }

        public bool Comparable { get; set; }

        public string IncomparableAttribute { get; set; }

        public string RankColumn { get; set; }
        


        public string OrderBy { get; set; }

        public bool IsCategory { get; set; }

        public int HexagonWeightIncomparable { get; set; }

        public string HexagonRank { get; set; }

        public string HexagonIncomparable { get; set; }
    }
}
