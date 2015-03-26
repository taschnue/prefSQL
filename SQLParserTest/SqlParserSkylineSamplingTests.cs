namespace prefSQL.SQLParserTest
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SQLParser;
    using SQLSkyline;

    [TestClass]
    public class SqlParserSkylineSamplingTests
    {
        private const string strConnection = "Data Source=localhost;Initial Catalog=eCommerce;Integrated Security=True";
        private const string driver = "System.Data.SqlClient";

        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestSkylineSamplingParserSyntaxValidity()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineBNL() };

            try
            {
                common.GetSkylineSamplingModelFromPreferenceSql(skylineSampleSql);
            }
            catch (Exception exception)
            {
                Assert.Fail("{0} - {1}", "Syntactically correct SQL Query should not have thrown an Exception.", exception.Message);
            }
        }

        [TestMethod]
        public void TestDifferentSamplingIntegrations()
        {
            var sql =
                "SELECT cs.price, cs.mileage, cs.horsepower, cs.enginesize, cs.consumption, cs.doors FROM cars_small cs SKYLINE OF cs.price LOW 3000 EQUAL, cs.mileage LOW 20000 EQUAL, cs.horsepower HIGH 20 EQUAL, cs.enginesize HIGH 1000 EQUAL, cs.consumption LOW 15 EQUAL, cs.doors HIGH";

            Debug.WriteLine(sql);
            var common = new SQLCommon { SkylineType = new SkylineSQL() };
            var parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);

            common = new SQLCommon { SkylineType = new SkylineBNL() };
            parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);

            common = new SQLCommon { SkylineType = new SkylineBNLSort() };
            parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);

            common = new SQLCommon { SkylineType = new MultipleSkylineBNL() };
            parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);
            
            common = new SQLCommon { SkylineType = new SkylineDQ() };
            parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);
            
            common = new SQLCommon { SkylineType = new SkylineHexagon() };
            parsedSql = common.parsePreferenceSQL(sql);
            Debug.WriteLine(parsedSql);
        }

        [TestMethod]
        public void TestSkylineSamplingParserResultSetSize()
        {
            var skylineSampleSql =
                "SELECT * FROM cars_small cs SKYLINE OF cs.price LOW, cs.mileage LOW SAMPLE BY RANDOM_SUBSETS COUNT 2 DIMENSION 1";

            var common = new SQLCommon {SkylineType = new SkylineBNL()};

            var skylineSamplingModelFromPreferenceSql = common.GetSkylineSamplingModelFromPreferenceSql(skylineSampleSql);
            var dataTableWholeQuery = common.parseAndExecutePrefSQL(strConnection, driver, skylineSampleSql);

            var dataTableFirst = new DataTable(); 
            var dataTableSecond = new DataTable(); 

                var sql= String.Format("EXEC dbo.SP_SkylineBNLSortLevel '{0} {1},* {3}', '{2}', 0",
                                skylineSamplingModelFromPreferenceSql.PreSkylineAttributesSqlString,
                                skylineSamplingModelFromPreferenceSql.SkylineAttributes[0],
                                skylineSamplingModelFromPreferenceSql.SkylineOperators[0],
                                skylineSamplingModelFromPreferenceSql.PostSkylineAttributesSqlString);

            var con1 = new SqlConnection(strConnection);
                con1.Open();
                    var da1 = new SqlDataAdapter(sql, con1);
                    da1.Fill(dataTableFirst);

            sql = String.Format("EXEC dbo.SP_SkylineBNLSortLevel '{0} {1},* {3}', '{2}', 0",
                skylineSamplingModelFromPreferenceSql.PreSkylineAttributesSqlString,
                skylineSamplingModelFromPreferenceSql.SkylineAttributes[1],
                skylineSamplingModelFromPreferenceSql.SkylineOperators[1],
                skylineSamplingModelFromPreferenceSql.PostSkylineAttributesSqlString);

            da1 = new SqlDataAdapter(sql, con1);
            da1.Fill(dataTableSecond);

            con1.Close();

            Debug.WriteLine(dataTableWholeQuery.Rows.Count);
            Debug.WriteLine(dataTableFirst.Rows.Count);
            Debug.WriteLine(dataTableSecond.Rows.Count);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestSyntaxValidityOfSyntacticallyCorrectSqlStatements()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineSQL() };

            try
            {
                common.parsePreferenceSQL(skylineSampleSql);
            }
            catch (Exception exception)
            {
                Assert.Fail("{0} - {1}", "Syntactically correct SQL Query should not have thrown an Exception.", exception.Message);
            }
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedSkylineSqlCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineSQL() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLSkylineSQLExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedSkylineBnlCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineBNL() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLSkylineBNLExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedSkylineBnlSortCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineBNLSort() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLSkylineBNLSortExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedSkylineDqCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineDQ() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLSkylineDQExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedMultipleSkylineBnlCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new MultipleSkylineBNL() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLMultipleSkylineBNLExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_CorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_CorrectSyntax.xml")]
        public void TestParsedSkylineHexagonCorrectness()
        {
            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            var testComment = TestContext.DataRow["comment"].ToString();
            Debug.WriteLine(testComment);
            Debug.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineHexagon() };

            var parsedSql = common.parsePreferenceSQL(skylineSampleSql);
            var parsedSqlExpected = TestContext.DataRow["parsePreferenceSQLSkylineHexagonExpectedResult"].ToString();
            Debug.WriteLine(parsedSql);
            Debug.WriteLine(parsedSqlExpected);

            Assert.AreEqual(parsedSqlExpected.Trim(), parsedSql.Trim(), "SQL not built correctly");
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "SQLParserSkylineSamplingTests_IncorrectSyntax.xml", "TestDataRow", DataAccessMethod.Sequential), DeploymentItem("SQLParserSkylineSamplingTests_IncorrectSyntax.xml")]
        public void TestSyntaxValidityOfSyntacticallyIncorrectSqlStatements()
        {
            var hasExceptionBeenRaised = false;

            var skylineSampleSql = TestContext.DataRow["skylineSampleSQL"].ToString();
            Console.WriteLine(skylineSampleSql);

            var common = new SQLCommon { SkylineType = new SkylineSQL() };

            try
            {
                common.parsePreferenceSQL(skylineSampleSql);
            }
            catch (Exception)
            {
                hasExceptionBeenRaised = true;
            }

            if (!hasExceptionBeenRaised)
            {
                Assert.Fail("Syntactically incorrect SQL Query should have thrown an Exception.");
            }
        }
    }
}
