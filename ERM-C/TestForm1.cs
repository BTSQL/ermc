using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SCAPI;

namespace ERM_C
{
    public partial class TestForm1 : Form
    {
        public TestForm1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {

                SCAPI.Application app = new SCAPI.Application();

                SCAPI.PersistenceUnits units = app.PersistenceUnits;
                Sessions sessions = app.Sessions;


                Session session = sessions.Add();


                foreach (PersistenceUnit unit in units)
                {
                    MessageBox.Show(unit.Name);
                    session.Open(unit, null, null);
                    
                }

                ModelObjects models = session.ModelObjects;
                MessageBox.Show("갯수 "+models.Count);

                foreach (ModelObject model in models)
                {
                    if (model.ClassName == "Entity" && !string.IsNullOrEmpty(model.Name))
                    {
                        ModelProperties properties = model.Properties;


                        

                        
                        foreach (ModelProperty property in properties)
                        {
                            //MessageBox.Show(property.ClassName + "," + property.FormatAsString());
                            

                            object trid = session.BeginTransaction();

                            if (property.ClassName == "Name")
                            {
                                property.set_Value(null, null, "TEST");
                                //PropertyValue value = new PropertyValue();
                                
                                 //property.FormatAsString()
                            }
                            //MessageBox.Show(model.Name + "," + property.ClassName + "," + property.PropertyValues.ToString());

                            session.CommitTransaction(trid);

                        }
                          

                    }
                    
                }
                


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }
    }
}
