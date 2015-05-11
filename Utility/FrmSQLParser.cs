﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using prefSQL.SQLParser;
using System.Diagnostics;
using prefSQL.SQLSkyline;

namespace Utility
{
    public partial class FrmSQLParser : Form
    {
        public FrmSQLParser()
        {
            InitializeComponent();
        }

        private void FrmSQLParser_Load(object sender, EventArgs e)
        {
            this.optSQL.Checked = true;
            string strSQL = "SELECT t1.id, t1.title, t1.price, t1.mileage, t1.enginesize FROM cars t1 " +
                "LEFT OUTER JOIN colors ON t1.color_id = colors.ID SKYLINE OF t1.price LOW, t1.mileage LOW";
            this.txtPrefSQL.Text = strSQL;


        }


        private void btnExecute_Click(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.btnExecute.Enabled = false;
            
            SQLCommon parser = new SQLCommon();
            if(this.optSQL.Checked == true)
            {
                parser.SkylineType = new SkylineSQL();
            }
            else if (this.optBNL.Checked == true)
            {
                parser.SkylineType = new SkylineBNLSort();
            }
            else if (this.optHexagon.Checked == true)
            {
                parser.SkylineType = new SkylineHexagon();
            }
            else if (this.optDQ.Checked == true)
            {
                parser.SkylineType = new SkylineDQ();
            }

            if (this.chkShowSkyline.Checked == true)
            {
                parser.ShowSkylineAttributes = true;
            }
            else
            {
                parser.ShowSkylineAttributes = false;
            }

            DataTable dt = parser.ParseAndExecutePrefSQL(Helper.ConnectionString, Helper.ProviderName, this.txtPrefSQL.Text);           

            BindingSource SBind = new BindingSource();
            SBind.DataSource = dt;
            
            gridSkyline.AutoGenerateColumns = true;
            gridSkyline.DataSource = dt;

            gridSkyline.DataSource = SBind;
            gridSkyline.Refresh();

            sw.Stop();
            
            this.txtTime.Text = sw.ElapsedMilliseconds.ToString();
            this.txtTimeAlgo.Text = parser.TimeInMilliseconds.ToString();
            this.txtRecords.Text = dt.Rows.Count.ToString();


            this.btnExecute.Enabled = true;
        }

    }
}
