using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Globalization;

namespace fitlog2tcx
{
    public partial class FrmMain : Form
    {
		private Converter _cvt = null;

        public FrmMain()
        {
            InitializeComponent();

            _cvt = new Converter();
        }

        private void BtnConvert_Click(object sender, EventArgs e)
        {
            /*
			cvt.ReadFitlog("C:\\Users\\Frank\\Dropbox\\Forerunner Dumps\\2012-10-10.fitlog");
            //cvt.ReadFitlog("C:\\Users\\Frank\\Dropbox\\Forerunner Dumps\\2012-10-10-full.fitlog");
            cvt.Convert();
            cvt.WriteTCX("C:\\Tmp\\2012-10-10.tcx");
             */
        }
    }
}
