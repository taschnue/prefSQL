﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using prefSQL.SQLParser;

namespace prefSQL.SQLParserTest
{
    [TestClass]
    public class SQLParserTests
    {

        [TestMethod]
        public void TestSyntaxError()
        {
            string strPrefSQL = "SELECT * FROM cars SKYLINE OF cars.price LOW";

            SQLCommon common = new SQLCommon();
            try
            {
                string actual = common.ParsePreferenceSQL(strPrefSQL);
                Assert.Fail("Preference SQL Query should throw an Error");
            }
            catch(Exception e)
            {
                String strError = e.Message;
                Assert.IsTrue(true);
            }
        }


        [TestMethod]
        public void TestWithoutPreference()
        {
            string strPrefSQL = "SELECT cars.id, cars.title, cars.Price FROM cars";

            string expected = "SELECT cars.id, cars.title, cars.Price FROM cars";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected, actual, true, "SQL not built correctly");
        }


        [TestMethod]
        public void TestLowLevel()
        {
            string strPrefSQL = "SELECT * FROM cars SKYLINE OF cars.price LOW 10000 EQUAL";

            string expected = "SELECT * FROM cars WHERE NOT EXISTS(SELECT * FROM cars cars_INNER WHERE cars_INNER.price / 10000 <= cars.price / 10000 AND ( cars_INNER.price / 10000 < cars.price / 10000) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");
        }

        [TestMethod]
        public void TestLow()
        {
            string strPrefSQL = "SELECT * FROM cars SKYLINE OF cars.mileage LOW";

            string expected = "SELECT * FROM cars WHERE NOT EXISTS(SELECT * FROM cars cars_INNER WHERE cars_INNER.mileage <= cars.mileage AND ( cars_INNER.mileage < cars.mileage) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");
        }


        [TestMethod]
        public void TestHigh()
        {
            string strPrefSQL = "SELECT * FROM cars SKYLINE OF cars.horsepower HIGH";

            string expected = "SELECT * FROM cars WHERE NOT EXISTS(SELECT * FROM cars cars_INNER WHERE cars_INNER.horsepower * -1 <= cars.horsepower * -1 AND ( cars_INNER.horsepower * -1 < cars.horsepower * -1) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");
        }

        [TestMethod]
        public void TestHighCustom()
        {
            string strPrefSQL = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID SKYLINE OF colors.name ('red' >> 'blue' >> OTHERS EQUAL >> 'gray') ";

            string expected = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID WHERE NOT EXISTS(SELECT * FROM cars cars_INNER LEFT OUTER JOIN colors colors_INNER ON cars_INNER.color_id = colors_INNER.ID WHERE (CASE WHEN colors_INNER.name = 'red' THEN 0 WHEN colors_INNER.name = 'blue' THEN 100 WHEN colors_INNER.name = 'gray' THEN 300 ELSE 200 END <= CASE WHEN colors.name = 'red' THEN 0 WHEN colors.name = 'blue' THEN 100 WHEN colors.name = 'gray' THEN 300 ELSE 200 END OR colors_INNER.name = colors.name) AND ( CASE WHEN colors_INNER.name = 'red' THEN 0 WHEN colors_INNER.name = 'blue' THEN 100 WHEN colors_INNER.name = 'gray' THEN 300 ELSE 200 END < CASE WHEN colors.name = 'red' THEN 0 WHEN colors.name = 'blue' THEN 100 WHEN colors.name = 'gray' THEN 300 ELSE 200 END) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");
        }


        [TestMethod]
        public void TestAround()
        {
            string strPrefSQL = "SELECT * FROM cars SKYLINE OF cars.price AROUND 15000";

            string expected = "SELECT * FROM cars WHERE NOT EXISTS(SELECT * FROM cars cars_INNER WHERE ABS(cars_INNER.price - 15000) <= ABS(cars.price - 15000) AND ( ABS(cars_INNER.price - 15000) < ABS(cars.price - 15000)) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");

        }



        [TestMethod]
        public void TestFavour()
        {
            string strPrefSQL = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID SKYLINE OF colors.name FAVOUR 'red'";

            string expected = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID WHERE NOT EXISTS(SELECT * FROM cars cars_INNER LEFT OUTER JOIN colors colors_INNER ON cars_INNER.color_id = colors_INNER.ID WHERE CASE WHEN colors_INNER.name = 'red' THEN 1 ELSE 2 END <= CASE WHEN colors.name = 'red' THEN 1 ELSE 2 END AND ( CASE WHEN colors_INNER.name = 'red' THEN 1 ELSE 2 END < CASE WHEN colors.name = 'red' THEN 1 ELSE 2 END) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected, actual, true, "SQL not built correctly");

        }



        [TestMethod]
        public void TestDisfavour()
        {
            string strPrefSQL = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID SKYLINE OF colors.name DISFAVOUR 'red'";

            string expected = "SELECT * FROM cars LEFT OUTER JOIN colors ON cars.color_id = colors.ID WHERE NOT EXISTS(SELECT * FROM cars cars_INNER LEFT OUTER JOIN colors colors_INNER ON cars_INNER.color_id = colors_INNER.ID WHERE CASE WHEN colors_INNER.name = 'red' THEN 1 ELSE 2 END * -1 <= CASE WHEN colors.name = 'red' THEN 1 ELSE 2 END * -1 AND ( CASE WHEN colors_INNER.name = 'red' THEN 1 ELSE 2 END * -1 < CASE WHEN colors.name = 'red' THEN 1 ELSE 2 END * -1) ) ";
            SQLCommon common = new SQLCommon();
            string actual = common.ParsePreferenceSQL(strPrefSQL);

            Assert.AreEqual(expected.Trim(), actual.Trim(), true, "SQL not built correctly");

        }

    }
}
