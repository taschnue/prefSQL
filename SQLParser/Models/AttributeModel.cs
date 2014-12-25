﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prefSQL.SQLParser.Models
{
    class AttributeModel
    {

        public AttributeModel(string strColumnExpression, string strOperator, string strTableName, string strInnerTable, string strInnerColumnExpression, string strColumnName, string strInnerColumnName, bool isComparable, string strIncomporableAttribute, string strRankColumn, string strRankColumnName, string strExpression, string strRankHexagon, string strOrderBy)
        {
            TableName = strTableName;                               //Tablename for the mainquery       (i.e. cars)
            InnerTable = strInnerTable;                             //Tablename for the subquery        (i.e. cars_INNER)
            ColumnExpression = strColumnExpression;                 //Column expression                 (i.e. CASE WHEN colors.name = 'türkis' THEN 0 WHEN colors.name = 'gelb' THEN 100 ELSE 200 END)
            InnerColumnExpression = strInnerColumnExpression;       //Inner column expression           (i.e CASE WHEN colors_INNER.name = 'türkis' THEN 0 WHEN colors_INNER.name = 'gelb' THEN 100 ELSE 200 END)
            Op = strOperator;                                       //Operator                          (<, >)
            ColumnName = strColumnName;                             //Used for the additional OR with text values (i.e. OR colors_INNER.name = colors.name)
            InnerColumnName = strInnerColumnName;                   //Dito
            Comparable = isComparable;                              //Check if at least one value is incomparable
            IncomparableAttribute = strIncomporableAttribute;       //Attribute that returns the textvalue if the value is incomparable

            Expression = strExpression;
            RankColumnName = strRankColumnName;
            RankColumn = strRankColumn;
            RankHexagon = strRankHexagon;

            OrderBy = strOrderBy;
        }


        public string ColumnName { get; set; }

        public string InnerColumnName { get; set; }

        public string ColumnExpression { get; set; }

        public string InnerColumnExpression { get; set; }


        public string InnerTable { get; set; }

        //Operator
        public string Op { get; set; }


        public bool Comparable { get; set; }

        public string IncomparableAttribute { get; set; }




        public string RankColumnName { get; set; }
        public string TableName { get; set; }
        public string Expression { get; set; }
        public string RankColumn { get; set; }

        public string RankHexagon { get; set; }


        public string OrderBy { get; set; }
    }
}
