using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.IO;

namespace MainForm.Util
{
    public enum LoaderEncoding
    {
        SystemDefault,
        UTF8,
        ASCII
    }

    public enum DelimiterType
    {
        Tab = 0,
        Comma = 1,
        User = 2,
    }

    class FileUtil
    {
        public static string[] ParseDataLineByDelimiter(string dataLine)
        {
            string delimiter = ",";

            string[] dataColumns = Regex.Split(dataLine, delimiter);
            return dataColumns;

        }

        public static void DeleteFile(string filename)
        {
            System.IO.File.Delete(filename);
        }

        public static string[] ParseDataLineByDelimiter(string dataLine, DelimiterType selectedDelimiter, string userDelimiter)
        {
            string delimiter = "";
            switch (selectedDelimiter)
            {
                case DelimiterType.Comma:
                    delimiter = ",";
                    break;
                case DelimiterType.Tab:
                    delimiter = "\t";
                    break;
                case DelimiterType.User:
                    delimiter = userDelimiter;
                    break;
                default:
                    break;
            }
            string[] dataColumns = Regex.Split(dataLine, delimiter);
            return dataColumns;

        }


        public static DataTable ReadDataFileByDelimiter(string filename, LoaderEncoding loaderEncoding, DataTable dt)
        {
            StreamReader sr = null;
            try
            {

                if (loaderEncoding == LoaderEncoding.ASCII)
                {
                    sr = new StreamReader(filename, Encoding.ASCII);
                }
                else if (loaderEncoding == LoaderEncoding.UTF8)
                {
                    sr = new StreamReader(filename, Encoding.UTF8);
                }
                else if (loaderEncoding == LoaderEncoding.SystemDefault)
                {
                    sr = new StreamReader(filename, Encoding.Default);
                }


                int sampleRownum = 0;

                //if (mCheckBoxIsFirstRowHeader.Checked == true) sr.ReadLine();


                DelimiterType selectedDelimiter = DelimiterType.Comma;

                string line = sr.ReadLine();



                string[] dataColumns = FileUtil.ParseDataLineByDelimiter(line, selectedDelimiter, null);

                /*
                List<string> list = new List<string>();
                for (int i = 0; i < dataColumns.Length; i++)
                {
                    list.Add(dataColumns[i]);
                }

                // You can convert it back to an array if you would like to
                dataColumns = list.ToArray();
                */

                for (int i = 0; i < dataColumns.Length; i++)
                {
                    /*
                    if (mCheckBoxIsFirstRowHeader.Checked == true)
                    {

                        //this.mDataGridViewColumnMapper.Columns.Add("col" + i, mv_DataColumns[i].ToString());
                        mv_DataFileTable.Columns.Add(mv_DataColumns[i].ToString());

                    }
                    else
                    {
                        //this.mDataGridViewColumnMapper.Columns.Add("col" + i, "col"+i);
                        mv_DataColumns[i] = "COLUMN" + i;
                        mv_DataFileTable.Columns.Add("COLUMN" + i);
                    }
                     * */

                    //dataColumns[i] = "COLUMN" + i;
                    dt.Columns.Add("COLUMN" + i);
                }






                DataRow dr = dt.NewRow();

                for (int i = 0; i < dataColumns.Length && i < dt.Columns.Count; i++)
                {

                    dr[i] = dataColumns[i];
                }

                dt.Rows.Add(dr);













                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();


                    string[] celldata = FileUtil.ParseDataLineByDelimiter(line, selectedDelimiter, null);

                    dr = dt.NewRow();

                    for (int i = 0; i < celldata.Length && i < dt.Columns.Count; i++)
                    {

                        dr[i] = celldata[i];
                    }

                    dt.Rows.Add(dr);

                    sampleRownum = sampleRownum + 1;
                }
                //this.mDataGridViewSampleData.DataSource = null;

                //this.mDataGridViewSampleData.DataSource = mv_DataFileTable;


            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                    sr.Dispose();
                }
            }
            return dt;

        }


    }
}
