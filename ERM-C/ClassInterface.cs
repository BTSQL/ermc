using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ERManipulator;

namespace ERM_C
{
    [Guid("268FF582-E129-47B3-B81B-ABE546A77790")]
    public interface ERM_C_Interface
    {
        [DispId(1)]
        void Init(string userid, string password);
        [DispId(2)]
        void Start();
    }

    // Events interface Database_COMObjectEvents 
    [Guid("6520FA26-C962-43C3-8530-B4C4F5622059"),
    InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface ERM_C_Events
    {
    }


    [Guid("E34F8E38-C026-451B-899A-9711F0E60502"),
    ClassInterface(ClassInterfaceType.None),
    ComSourceInterfaces(typeof(ERM_C_Events))]
    public class Tango : ERM_C_Interface
    {
        public Tango()
        {
            //MessageBox.Show("111constructor1");

        }

        //private SqlConnection myConnection = null;
        //SqlDataReader myReader = null;
        public void Start()
        {
            try
            {

                //SCAPI.Application app = new SCAPI.Application();

                //SCAPI.PersistenceUnits units = app.PersistenceUnits;

                //MessageBox.Show("" + units.Count);

                ERManipulatorForm form = new ERManipulatorForm();
                form.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }



        }
        
        public void Init(string userid, string password)
        {
            try
            {
                MessageBox.Show(userid + "+++" + password);
                /*
                string myConnectString = "user id=" + userid + ";password=" + password +
                    ";Database=NorthWind;Server=SKYWALKER;Connect Timeout=30";
                myConnection = new SqlConnection(myConnectString);
                myConnection.Open();
                //MessageBox.Show("CONNECTED");
                 */ 
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public bool ExecuteSelectCommand(string selCommand)
        {
            /*
            if (myReader != null)
                myReader.Close();

            SqlCommand myCommand = new SqlCommand(selCommand);
            myCommand.Connection = myConnection;
            myCommand.ExecuteNonQuery();
            myReader = myCommand.ExecuteReader();
             */ 
            return true;
        }

        public bool NextRow()
        {
            /*
            if (!myReader.Read())
            {
                myReader.Close();
                return false;
            }
             */ 
            return true;
        }

        public string GetColumnData(int pos)
        {
            /*
            Object obj = myReader.GetValue(pos);
            if (obj == null) return "";
             */
            return "";
        }

        public void ExecuteNonSelectCommand(string insCommand)
        {
            /*
            SqlCommand myCommand = new SqlCommand(insCommand, myConnection);
            int retRows = myCommand.ExecuteNonQuery();
             */ 
        }

    }
}
