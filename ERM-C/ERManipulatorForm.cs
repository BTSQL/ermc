using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MainForm.Util;
using System.IO;
using SCAPI;
using AdvancedDataGridView;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace ERManipulator
{
    public partial class ERManipulatorForm : Form
    {

        private bool isBatchMode;

        private string mv_BatchConnStr;


        private List<ALMSRqstDTL> mv_almsRqstDtl;

        private string mv_lastALMSRqstDTLID;

        private SCAPI.Application mv_app;

        private SCAPI.PersistenceUnits mv_units;

        private Sessions mv_sessions;

        private Session mv_session;

        private int mv_maxcoordX;
        private int mv_maxcoordY;
        private bool mv_isMaxCoordSet;

        private bool mv_isWorking;
        private bool is_stopIssued;

        private List<string> mv_EntityNameList;
        private Dictionary<string, StandardTerm> mv_strdterm;

        private int mv_entityCount = 0;
        private int mv_processedEntityCount = 0;
        private bool mv_isStrdTrmReady = false;
        private bool mv_isSelectedSubjOnly = false;
        private bool mv_isLogicalOnlyExcluded = true;
        private bool mv_isSpecialEntityExcluded = true;
        private bool mv_isLogicalOnlyAttributeExcluded = true;


        private bool mv_isProjectEnvCheckRequired = false;


        public ERManipulatorForm()
        {
            InitializeComponent();

        }


        public ERManipulatorForm(bool isBatchmode)
        {
            isBatchMode = true;



            InitializeComponent();



            
        }

        private void Form1_Load(object sender, EventArgs e)
        {


            try
            {


                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                string MyIp = "";

                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {

                        foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                MyIp = ip.Address.ToString();
                                break;
                            }
                        }


                        break;
                    }
                }
                if (mv_isProjectEnvCheckRequired)
                {
                    if (MyIp.StartsWith("150."))
                    {
                        mToolStripLabelConnectionStatus.Text = "프로젝트 환경이 인식되었습니다.";
                        mToolStripLabelConnectionStatus.BackColor = Color.LimeGreen;
                        button1.Enabled = true;
                    }
                    else
                    {
                        button1.Enabled = false;
                        mToolStripLabelConnectionStatus.Text = "프로젝트 환경이 아닙니다...";
                        mToolStripLabelConnectionStatus.BackColor = Color.Red;
                    }

                }
                else
                {
                    button1.Enabled = true;

                }

                
                //



                if (ConnectERDSession())
                {
                    label2.Text = "ERD 정상 인식";
                    List<string> list = getSubjectAreaList();
                    foreach (string subjName in list)
                    {
                        if (subjName == "<Main Subject Area>")
                        {
                            continue;
                        }
                        dataGridView1.Rows.Add(true,subjName);
                    }
                }
                else
                {
                    label2.Text = "ERD 인식 실패";
                }


            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }

        }

        private void startStrdTermApplyProcess()
        {
            backgroundWorker1.RunWorkerAsync();
        }



        private List<string> getSubjectAreaList()
        {
            ModelObjects EntityCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Subject Area", null, null, null);

            List<string> subjList = new List<string>();
            foreach (ModelObject subjectarea in EntityCollection)
            {
                string name = subjectarea.Properties["Name"].FormatAsString();
                subjList.Add(name);
            }

            return subjList;
        }

        private void modifyEntityAttribute(WorkResult result)
        {
//            AddLog(entity.Properties["Name"].FormatAsString());
            ModelObjects attributeCollection = mv_session.ModelObjects.Collect(result.ENTITY, "Attribute", null, null, null);
            
            foreach (ModelObject attribute in attributeCollection)
            {
                string attrName = attribute.Properties["Name"].FormatAsString();

                int cutposition = attrName.Length;
                string lastNumbers = null;
                if (mv_isLogicalOnlyAttributeExcluded)
                {

                    for (int i = 0; i < attrName.Length; i++)
                    {
                        if (i == 0)
                        {
                            continue;
                        }
                        string character = attrName.Substring(attrName.Length - i, 1);
                        int? number = null;
                        try
                        {
                            number = int.Parse(character);
                        }
                        catch (Exception ex)
                        {
                            break;
                        }
                        if (number != null)
                        {
                            cutposition = attrName.Length - i;
                        }
                    }
                    lastNumbers = attrName.Substring(cutposition);
                    if (!string.IsNullOrEmpty(lastNumbers))
                    {
                        attrName = attrName.Substring(0, cutposition);
                    }


                }



                try
                {
                    if (attribute.Properties["Logical Only"].FormatAsString() == "true")
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {

                }

                if (mv_strdterm.ContainsKey(attrName))
                {
                    string physical_name = attribute.Properties["Physical Name"].FormatAsString();
                    string logical_datatype = attribute.Properties["Logical Datatype"].FormatAsString();
                    string datatype = attribute.Properties["Datatype"].FormatAsString();

                    StandardTerm strdterm_original = mv_strdterm[attrName] as StandardTerm;

                    StandardTerm strdterm = new StandardTerm();
                    strdterm._data_type = strdterm_original._data_type;
                    strdterm._infotype_name = strdterm_original._infotype_name;
                    strdterm.physical_term_name = strdterm_original.physical_term_name;
                    strdterm.term_name = strdterm_original.term_name;

                    if (!string.IsNullOrEmpty(lastNumbers))
                    {
                        strdterm.physical_term_name += lastNumbers;
                        strdterm.term_name += lastNumbers;
                    }
                    bool isModified = false;
                    if (strdterm.physical_term_name != physical_name)
                    {
                        attribute.Properties["Physical Name"].set_Value(null, null, strdterm.physical_term_name);
                        isModified = true;
                    }
                    if (strdterm._infotype_name != logical_datatype)
                    {
                        attribute.Properties["Logical Datatype"].set_Value(null, null, strdterm._infotype_name);
                        isModified = true;
                    }
                    if (strdterm._data_type != datatype)
                    {
                        attribute.Properties["Datatype"].set_Value(null, null, strdterm._data_type);
                        isModified = true;
                    }

                    if (isModified)
                    {
                        result.MODIFIEDATTR.Add(attrName);
                    }
                }
                else
                {
                    result.NONSTRDATTR.Add(attrName);
                }

            }
            
        }




        private void button2_Click(object sender, EventArgs e)
        {
            if (mv_isStrdTrmReady)
            {
                AddLog("표준용어를 ERD에 반영합니다.");
                treeGridView1.Nodes.Clear();
                tabControl2.SelectedTab = tabPage1;
                is_stopIssued = false;
                mv_isSelectedSubjOnly = radioButton2.Checked;
                mv_isLogicalOnlyExcluded = checkBox1.Checked;
                mv_isSpecialEntityExcluded = checkBox2.Checked;
                mv_isLogicalOnlyAttributeExcluded = checkBox3.Checked;
                startStrdTermApplyProcess();
            }
            else
            {
                AddLog("표준용어가 준비되지 않았습니다.");
            }





        }



        private void button4_Click(object sender, EventArgs e)
        {
            foreach (TreeGridNode node in treeGridView1.Nodes)
            {
                node.Visible = true;
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            foreach (TreeGridNode node in treeGridView1.Nodes)
            {
                if (node.Cells["ColumnModifiedYN"].Value.ToString() == "Y")
                {
                    node.Visible = true;
                }
                else
                {
                    node.Visible = false;
                }
               
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (TreeGridNode node in treeGridView1.Nodes)
            {
                if (node.Cells["ColumnNonStrdYN"].Value.ToString() == "Y")
                {
                    node.Visible = true;
                }
                else
                {
                    node.Visible = false;
                }

            }
        }





        private void button1_Click(object sender, EventArgs e)
        {
            mv_isStrdTrmReady = false;
            mv_strdterm = new Dictionary<string, StandardTerm>();
            dataGridView2.Rows.Clear();
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                AddLog("메타시스템에 접속중입니다.");


                WebClient connRuleServer = new WebClient();


                NameValueCollection param = new NameValueCollection();


                param.Add("ID", "");

                //string url = @"https://www.nexcore-erc.com/dpump/strdterm/_list/5a16cae68f04cf1820ab1666/5a3cfea58f04cf0e90b87901";
                string url = "";

                // read JSON directly from a file
                using (StreamReader file = File.OpenText(@"ermc.conf"))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o2 = (JObject)JToken.ReadFrom(reader);
                    url = o2["stard_term_url"].ToString();
                }

                AddLog("메타 시스템 URL : " + url);

                           

                //url = @"http://10.0.2.2:5000/dpump/strdterm/_list/57442ded18768a34ccfab0f7/577f61c618768a59f16bee89";
                //url = @"http://10.0.2.2:5000/dpump/strdterm/_list/574fd1dc18768a15b3ddce42/577b154418768a6acad9701f";

                //url = @"http://localhost:5000/dpump/strdterm/_list/5748ea2b006e941ce080436c/57491f02006e941ce08043ab";
                //url = @"http://erc.skcc.com/dpump/strdterm/_list/57442ded18768a34ccfab0f7/577f61c618768a59f16bee89";

                byte[] response = connRuleServer.UploadValues(url, param);
                
                string result = Encoding.UTF8.GetString(response);


                string jsondata = @"
                {
                    '용어명1':{
                        'term_name':'용어명1',
                        'physical_term_name':'term1',
                        'infotype_name':'변동문자1',
                        'data_type':'varchar2(1)'
                    },
                    '회계구분코드':{
                        'term_name':'회계구분코드',
                        'physical_term_name':'term2',
                        'infotype_name':'변동문자10',
                        'data_type':'varchar2(10)'
                    }
                }
                ";
                //string result = jsondata;
                AddLog("표준용어 목록을 만드는 중입니다.");


                mv_strdterm = JsonConvert.DeserializeObject<Dictionary<string, StandardTerm>>(result);
                label3.Text = mv_strdterm.Count + "건의 표준용어를 가져왔습니다.";


                
                
                foreach (var item in mv_strdterm)
                {
                    dataGridView2.Rows.Add(item.Key);
                }
                if (mv_strdterm.Count > 0)
                {
                    mv_isStrdTrmReady = true;
                }
                AddLog("표준용어 받아오기가 완료되었습니다.");
                
                

            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }


        }




        private void treeGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            /*
            if (e.RowIndex > -1 && e.ColumnIndex == 0)
            {

                string entityName = treeGridView1.Rows[e.RowIndex].Cells[0].FormattedValue.ToString();
                SetViewPoint("<Main Subject Area>", entityName);
            }
             */
        }







        #region BackgroundWorker1
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            mv_isWorking = true;
            string worktype = e.Argument as string;
            mv_EntityNameList = null;

            try
            {

                object trid = null;
                

                trid = mv_session.BeginTransaction();


                List<string> targetEntities = new List<string>();
                
                if (mv_isSelectedSubjOnly)
                {
                    List<string> targetSubjs = new List<string>();
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        DataGridViewCheckBoxCell cell = row.Cells[0] as DataGridViewCheckBoxCell;
                        if (cell.Value.ToString() == "True")
                        {
                            targetSubjs.Add(row.Cells[1].Value.ToString());
                        }
                    }

                    ModelObjects SubjCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Subject Area", null, null, null);
                    foreach (ModelObject subj in SubjCollection)
                    {
                        if (targetSubjs.Contains(subj.Properties["Name"].FormatAsString()))
                        {
                            ModelObjects drawingEntityCollection = mv_session.ModelObjects.Collect(subj, "Drawing Object Entity", null, null, null);
                            foreach (ModelObject entity in drawingEntityCollection)
                            {
                                targetEntities.Add(entity.Properties["Model Object Ref"].get_Value().ToString());
                            }


                        }

                    }


                }


                ModelObjects EntityCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Entity", null, null, null);
                if (mv_isSelectedSubjOnly)
                {
                    mv_entityCount = targetEntities.Count;
                }
                else
                {
                    mv_entityCount = EntityCollection.Count;
                }
                
                mv_processedEntityCount = 0;
                foreach (ModelObject targetentity in EntityCollection)
                {
                    WorkResult result = new WorkResult();
                    if (is_stopIssued)
                    {
                        mv_session.CommitTransaction(trid);
                        result.MESSAGE = "작업을 중지했습니다.";
                        result.WORKTYPE = WorkType.SimpleMessage;
                        backgroundWorker1.ReportProgress(1, result);
                        break;
                        
                    }
                    string entName = targetentity.Properties["Name"].FormatAsString();
                    if (mv_isSelectedSubjOnly)
                    {
                        if (!targetEntities.Contains(targetentity.Properties["Long Id"].FormatAsString()))
                        {
                            continue;
                        }
                    }

                    if (mv_isSpecialEntityExcluded)
                    {

                        if (entName.Contains("[외부]") || entName.Contains("[복제]") || entName.Contains("[가상]") || entName.Contains("[제거]"))
                        {
                            result.WORKTYPE = WorkType.AddSpecialEntityResultToTree;
                            result.ENTITY = targetentity;
                            result.MESSAGE = "Skipped. 특수엔티티";
                            backgroundWorker1.ReportProgress(1, result);
                            continue;
                        }
                    }

                    if (mv_isLogicalOnlyExcluded)
                    {
                        try
                        {
                            if (targetentity.Properties["Logical Only"].FormatAsString() == "true")
                            {
                                result.WORKTYPE = WorkType.AddSpecialEntityResultToTree;
                                result.ENTITY = targetentity;
                                result.MESSAGE = "Skipped. LogicalOnly";
                                backgroundWorker1.ReportProgress(1, result);
                                continue;
                            }

                        }
                        catch
                        {

                        }
                    }





                    //result.NODE = root;
                    result.WORKTYPE = WorkType.AddEntityResultToTree;
                    result.ENTITY = targetentity;




                    modifyEntityAttribute(result);


                    backgroundWorker1.ReportProgress(1, result);

                }
                if (!is_stopIssued)
                {
                    mv_session.CommitTransaction(trid);
                }
                


            }
            catch (Exception ex)
            {

                WorkResult result = new WorkResult();
                result.MESSAGE = " 오류발생 : " + ex.Message;
                result.STATUS = WorkStatus.GeneralError;
                backgroundWorker1.ReportProgress(1, result);
            }


        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            WorkResult result = e.UserState as WorkResult;

            if (result == null)
            {
                return;
            }


            if (result.WORKTYPE == WorkType.AddEntityResultToTree)
            {
                string modifiedYN = "N";
                if (result.MODIFIEDATTR.Count > 0)
                {
                    modifiedYN = "Y";

                }

                string nonStrdYN = "N";
                if (result.NONSTRDATTR.Count > 0)
                {
                    nonStrdYN = "Y";
                }
                TreeGridNode root = treeGridView1.Nodes.Add(result.ENTITY.Properties["Name"].FormatAsString(), modifiedYN, nonStrdYN);

                if (nonStrdYN == "Y")
                {
                    root.Cells[0].Style.BackColor = Color.Red;
                }

                foreach (string name in result.MODIFIEDATTR)
                {
                    root.Nodes.Add(null, name, null);
                }
                foreach (string name in result.NONSTRDATTR)
                {
                    root.Nodes.Add(null,null, name);
                }
                mv_processedEntityCount++;

                label4.Text = mv_processedEntityCount + "/" + mv_entityCount + " 건을 처리했습니다.";




            }
            else if (result.WORKTYPE == WorkType.SimpleMessage)
            {
                AddLog(result.MESSAGE);
            }
            else if (result.WORKTYPE == WorkType.AddSpecialEntityResultToTree)
            {
                TreeGridNode root = treeGridView1.Nodes.Add(result.ENTITY.Properties["Name"].FormatAsString(), result.MESSAGE, "");
                mv_processedEntityCount++;

                label4.Text = mv_processedEntityCount + "/" + mv_entityCount + " 건을 처리했습니다.";

            }

            



        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            AddLog("작업 완료. 결과 확인 후 창을 닫아 주세요. ERD저장은 직접 하셔야 합니다.");
        }

        #endregion
























        #region LogEvent


        private System.Object ProgressLock = new System.Object();
        public void ReceiveJobProgressInfo(object sender, DetectJobProgressInfo info)
        {
            DetectJobProgressInfo e = new DetectJobProgressInfo();
            lock (ProgressLock)
            {
                e = info.Clone() as DetectJobProgressInfo;
            }
            if (this.InvokeRequired)
            {

              //  BeginInvoke(new MethodInvoker(delegate() { UpdateProgressLog(sender, e); }));
            }
            else
            {
              //  UpdateProgressLog(sender, e);
            }

        }


        #endregion



        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm about = new AboutForm();
            about.Show();
        }


        private string GetInfoTypeStrOracle(string datatypestr, string datatypelength, string datatypeprecision)
        {
            string OracleDatatypeStr = "";



            if (datatypestr.Contains("CHAR"))
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr + "()";
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else if (datatypestr == "NUMBER")
            {



                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr;
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else
            {
                OracleDatatypeStr = datatypestr;
            }



            return OracleDatatypeStr;
        }


        private string GetInfoTypeStrDB2(string datatypestr, string datatypelength, string datatypeprecision)
        {
            string OracleDatatypeStr = "";

            if (datatypestr.Contains("CHAR"))
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr + "()";
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else if (datatypestr == "NUMBER")
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr;
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else if (datatypestr == "NUMERIC")
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr;
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else if (datatypestr == "DECIMAL")
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr;
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else
            {
                OracleDatatypeStr = datatypestr;
            }



            return OracleDatatypeStr;
        }

        private void CreateSingleAttribute(ModelObject entity, ALMSRqstDTL dtl, string AttrName)
        {
            //ModelObjects entityObjCollection = mv_session.ModelObjects.Collect(entity, entity.ClassId, null, null, null);
            ModelObjects entityObjCollection = mv_session.ModelObjects.Collect(entity, "Entity", null, null, null);

            DataTable dt = dtl.ATTRDTL;
           
            
            ModelObject attrobj = entityObjCollection.Add("Attribute", null);

            DataRow dr = null;
            foreach (DataRow targetdr in dt.Rows)
            {
                if (AttrName == targetdr["ATTR_NM"].ToString())
                {
                    dr = targetdr;
                }
            }

            string Name = dr["ATTR_NM"].ToString();
            string Type = dr["PRK_SEQ"].ToString() != "0" ? "Primary Key" : "Non-Key";
            string Definition = dr["ATTR_DESC"].ToString();
            string Default = dr["BAS_VAL_CTT"].ToString();
            string datatypestr = null;
            string datatypelength = null;
            string datatypeprecision = null;
            try
            {
                datatypestr = dr["PSC_DATA_TYP_NM"].ToString();
                datatypelength = dr["PSC_EFF_PLC_CNT"].ToString();
                datatypeprecision = dr["PSC_DECIMAL_PLC_CNT"].ToString();


                if (string.IsNullOrEmpty(datatypelength))
                {
                    datatypelength = "0";
                }

                if (string.IsNullOrEmpty(datatypeprecision))
                {
                    datatypeprecision = "0";
                }

            }
            catch (Exception ex)
            {

            }


            string OracleDatatypeStr = "";


            //파일로 읽어서 만들때를 위한 특수한 속성임. DB에서 읽을때는 이게 없을거다.

            if (string.IsNullOrEmpty(dr["INFO_TYPE"].ToString()))
            {
                if (toolStripComboBox1.Text == "Oracle")
                {
                    OracleDatatypeStr = GetInfoTypeStrOracle(datatypestr, datatypelength, datatypeprecision);

                }
                else if (toolStripComboBox1.Text == "DB2")
                {
                    OracleDatatypeStr = GetInfoTypeStrDB2(datatypestr, datatypelength, datatypeprecision);
                }
                else if (toolStripComboBox1.Text == "Sybase")
                {
                    OracleDatatypeStr = GetInfoTypeStrDB2(datatypestr, datatypelength, datatypeprecision);
                }
                else if (toolStripComboBox1.Text == "Vertica")
                {
                    OracleDatatypeStr = GetInfoTypeStrDB2(datatypestr, datatypelength, datatypeprecision);
                }


                






            }
            else
            {
                OracleDatatypeStr = dr["INFO_TYPE"].ToString();
            }


            
            

            string Datatype = OracleDatatypeStr;

            string AttributeRequired = (dr["NUL_CL_CD"].ToString() == "NN" || dr["NUL_CL_CD"].ToString() == "NND") ? "1" : "0";//Attribute Required
            string NullOption = (dr["NUL_CL_CD"].ToString() == "NN" || dr["NUL_CL_CD"].ToString() == "NND") ? "1" : "0";//Null Option
            string Label = dr["ATTR_NM"].ToString();
            string Header = dr["ATTR_NM"].ToString();
            string Order = dr["ATTR_SEQ"].ToString();
            string PhysicalOrder = dr["ATTR_SEQ"].ToString();//Physical Order
            string Comment = dr["ATTR_DESC"].ToString();
            string PhysicalName = dr["COL_NM"].ToString();//Physical Name
            string LogicalDatatype = OracleDatatypeStr;//Logical Datatype

            string KeyGroupPosition = dr["PRK_SEQ"].ToString();//Key Group Position


            attrobj.Properties["Name"].set_Value(null, null, Name);
            attrobj.Properties["Type"].set_Value(null, null, Type);
            attrobj.Properties["Definition"].set_Value(null, null, Definition);
            attrobj.Properties["Datatype"].set_Value(null, null, Datatype);
            attrobj.Properties["Attribute Required"].set_Value(null, null, AttributeRequired);
            attrobj.Properties["Null Option"].set_Value(null, null, NullOption);
            attrobj.Properties["Label"].set_Value(null, null, Label);
            attrobj.Properties["Header"].set_Value(null, null, Header);
            attrobj.Properties["Order"].set_Value(null, null, Order);
            attrobj.Properties["Physical Order"].set_Value(null, null, PhysicalOrder);
            attrobj.Properties["Comment"].set_Value(null, null, Comment);
            attrobj.Properties["Physical Name"].set_Value(null, null, PhysicalName);
            attrobj.Properties["Logical Datatype"].set_Value(null, null, LogicalDatatype);


            if (!string.IsNullOrEmpty(Default))
            {
                string defaultValueId = GetDefaultValueId(Default);
                attrobj.Properties["Default"].set_Value(null, null, defaultValueId);
            }


            
        }

        private void UpdateSingleAttribute(ModelObject entity, ALMSRqstDTL dtl, string AttrName)
        {
            ModelObjects attrObjectCollection = mv_session.ModelObjects.Collect(entity, "Attribute", null, null, null);

            DataTable dt = dtl.ATTRDTL;

            DataRow dr = null;
            foreach (DataRow targetdr in dt.Rows)
            {
                //if (AttrName == targetdr["ATTR_NM"].ToString())

                if (AttrName == targetdr["BCHG_ATTR_HAN_NM"].ToString())
                {
                    dr = targetdr;
                }
            }
            /*
            ModelObject attrObject = null;
            foreach (ModelObject targetAttrObject in attrObjectCollection)
            {
                if (AttrName == targetAttrObject.Properties["Name"].FormatAsString())
                {
                    attrObject = targetAttrObject;
                }
            }
            */
            ModelObject attrObject = attrObjectCollection[AttrName, "Attribute"];


            string Name = dr["ATTR_NM"].ToString();
            string Type = dr["PRK_SEQ"].ToString() != "0" ? "Primary Key" : "Non-Key";
            string Definition = dr["ATTR_DESC"].ToString();
            string Default = dr["BAS_VAL_CTT"].ToString();
            string datatypestr = dr["PSC_DATA_TYP_NM"].ToString();
            string datatypelength = dr["PSC_EFF_PLC_CNT"].ToString();
            string datatypeprecision = dr["PSC_DECIMAL_PLC_CNT"].ToString();
            string OracleDatatypeStr = "";


            if (datatypestr.Contains("CHAR") || datatypestr == "NUMBER")
            {
                if (datatypelength == "0")
                {
                    OracleDatatypeStr = datatypestr + "()";
                }
                else if (datatypelength != "0" && datatypeprecision == "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + ")";
                }
                else if (datatypelength != "0" && datatypeprecision != "0")
                {
                    OracleDatatypeStr = datatypestr + "(" + datatypelength + "," + datatypeprecision + ")";
                }
            }
            else
            {
                OracleDatatypeStr = datatypestr;
            }

            string Datatype = OracleDatatypeStr;

            string AttributeRequired = dr["NUL_CL_CD"].ToString() == "NN" ? "1" : "0";//Attribute Required
            string NullOption = dr["NUL_CL_CD"].ToString() == "NN" ? "1" : "0";//Null Option
            string Label = dr["ATTR_NM"].ToString();
            string Header = dr["ATTR_NM"].ToString();
            string Order = dr["ATTR_SEQ"].ToString();
            string PhysicalOrder = dr["ATTR_SEQ"].ToString();//Physical Order
            string Comment = dr["ATTR_DESC"].ToString();
            string PhysicalName = dr["COL_NM"].ToString();//Physical Name
            string LogicalDatatype = OracleDatatypeStr;//Logical Datatype

            string KeyGroupPosition = dr["PRK_SEQ"].ToString();//Key Group Position


            attrObject.Properties["Name"].set_Value(null, null, Name);
            attrObject.Properties["Type"].set_Value(null, null, Type);
            attrObject.Properties["Definition"].set_Value(null, null, Definition);
            attrObject.Properties["Datatype"].set_Value(null, null, Datatype);
            attrObject.Properties["Attribute Required"].set_Value(null, null, AttributeRequired);
            attrObject.Properties["Null Option"].set_Value(null, null, NullOption);
            attrObject.Properties["Label"].set_Value(null, null, Label);
            attrObject.Properties["Header"].set_Value(null, null, Header);
            attrObject.Properties["Order"].set_Value(null, null, Order);
            attrObject.Properties["Physical Order"].set_Value(null, null, PhysicalOrder);
            attrObject.Properties["Comment"].set_Value(null, null, Comment);
            attrObject.Properties["Physical Name"].set_Value(null, null, PhysicalName);
            attrObject.Properties["Logical Datatype"].set_Value(null, null, LogicalDatatype);


            if (!string.IsNullOrEmpty(Default))
            {
                string defaultValueId = GetDefaultValueId(Default);
                attrObject.Properties["Default"].set_Value(null, null, defaultValueId);
            }



        }

        private void UpdateEntity(ALMSRqstDTL dtl)
        {

            ModelObjects EntityCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Entity", null, null, null);

            string EntityName = dtl.ENTNM;

            /*
            ModelObject entity = null;

            foreach (ModelObject targetentity in EntityCollection)
            {
                if (EntityName == targetentity.Properties["Name"].FormatAsString())
                {
                    entity = targetentity;
                }
            }
            */

            ModelObject entity = EntityCollection[EntityName, "Entity"];

            

            ModelObjects AttributeCollection = EntityCollection.Collect(entity, "Attribute", null, null, null);
            ModelObjects KeyGroupCollection = EntityCollection.Collect(entity, "Key Group", null, null, null);

             DataTable dt = dtl.ATTRDTL;

             foreach (DataRow dr in dt.Rows)
             {
                 string CHG_CL_CD = dr["CHG_CL_CD"].ToString();



                 if ("CIN" == CHG_CL_CD)//컬럼신규
                 {
                     CreateSingleAttribute(entity, dtl, dr["ATTR_NM"].ToString());
                 }
                 else if ("CDD" == CHG_CL_CD)//컬럼삭제 
                 {
                     WorkResult result = new WorkResult();
                     result.STATUS = WorkStatus.AttrRemoveSkip;
                     result.MESSAGE = "속성 삭제는 자동으로 처리하지 않습니다. 수작업 해주세요.";

                     backgroundWorker1.ReportProgress(1, result);

                 }
                 else if ("CUP" == CHG_CL_CD)//PK관련변경
                 {
                     UpdateSingleAttribute(entity, dtl, dr["ATTR_NM"].ToString());
                 }
                 else if ("CUR" == CHG_CL_CD)//컬럼명변경
                 {
                     UpdateSingleAttribute(entity, dtl, dr["BCHG_ATTR_HAN_NM"].ToString());
                 }
                 else if ("CUT" == CHG_CL_CD)//데이터타입 및 길이변경
                 {
                     UpdateSingleAttribute(entity, dtl, dr["ATTR_NM"].ToString());
                 }
                 else if ("CUN" == CHG_CL_CD)//null변경
                 {
                     UpdateSingleAttribute(entity, dtl, dr["ATTR_NM"].ToString());
                 }
                 else if ("CMO" == CHG_CL_CD)//일반컬럼순서만 변경
                 {
                     //어차피 순서변경 마지막에 함.
                 }
                 else if ("NCH" == CHG_CL_CD)//변경없음
                 {
                     //어차피 순서변경 마지막에 함.
                 }


             }

            //속성 순서 일괄 정리
             FixAttributeOrder(entity, dtl);





            /*
            foreach (ModelObject attribute in AttributeCollection)
            {
                AttributeCollection.Remove(attribute.ObjectId);
            }

            foreach (ModelObject keygroup in KeyGroupCollection)
            {
                KeyGroupCollection.Remove(keygroup.ObjectId);
            }
            */
            
            
            
            
            //CreateAttribute(entity, dtl);
            return;



            /*
            ModelObject newEntityObject = newObjectCollection.Add("Entity", null);

            DataTable dt = dtl.ENTDTL;

            //default
            string Name = dt.Rows[0]["ENT_NM"].ToString();
            string Type = "Independent";
            string Definition = dt.Rows[0]["ENT_DESC"].ToString();
            string Note2 = dt.Rows[0]["DATA_CRE_DEL_RULE_DESC"].ToString();//Note 2
            string IndexGenerate = "1"; //Index Generate
            string PhysicalName = dt.Rows[0]["TBL_NM"].ToString();//Physical Name
            string DBOwner = dt.Rows[0]["OWNR_CD"].ToString(); //DB Owner

            //Script Templates : DEF_COMMENT ???

            //only for ukey 

            string sjaname = dt.Rows[0]["SJA_NM"].ToString(); //소속주제영역


            newEntityObject.Properties["Name"].set_Value(null, null, Name);
            newEntityObject.Properties["Type"].set_Value(null, null, Type);
            newEntityObject.Properties["Definition"].set_Value(null, null, Definition);
            newEntityObject.Properties["Note 2"].set_Value(null, null, Note2);
            newEntityObject.Properties["Index Generate"].set_Value(null, null, IndexGenerate);
            newEntityObject.Properties["Physical Name"].set_Value(null, null, PhysicalName);
            newEntityObject.Properties["DB Owner"].set_Value(null, null, DBOwner);
            newEntityObject.Properties["소속주제영역"].set_Value(null, null, sjaname);




            ModelObjects subjCollections = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Subject Area", null, null, null);


            ModelObject subjcollection = subjCollections[sjaname, "Subject Area"];
            //ModelObject subjcollection = subjCollections["04.D05 고객서비스정보_기타_비정상영업관리", "Subject Area"];

            ModelObjects storedDisplayCollections = mv_session.ModelObjects.Collect(subjcollection, "Stored Display", null, null, null);

            ModelObject firstStoredDisplay = null;
            foreach (ModelObject storedisplayobject in storedDisplayCollections)
            {
                firstStoredDisplay = storedisplayobject;
                break;
            }

            ModelObjects drawingObjEntitycollections = mv_session.ModelObjects.Collect(firstStoredDisplay, "Drawing Object Entity", null, null, null);

            int maxcoordination = 100;
            foreach (ModelObject entity in drawingObjEntitycollections)
            {
                string location = entity.Properties["DO Location"].FormatAsString();
                string[] locstr = location.Split(',');

                int coord = Convert.ToInt32(locstr[0].Replace("(", ""));
                coord = coord + 100;
                if (coord > maxcoordination)
                {
                    maxcoordination = coord;
                }
            }

            //MessageBox.Show(maxcoordination + "");



            ModelObject newDeawingObjEntity = drawingObjEntitycollections.Add("Drawing Object Entity", null);

            newDeawingObjEntity.Properties["DO Text"].set_Value(null, null, Name);
            newDeawingObjEntity.Properties["DO Location"].set_Value(null, null, "(" + maxcoordination + ",100,150,150)");
            newDeawingObjEntity.Properties["DO Reference Object"].set_Value(null, null, newEntityObject.ObjectId);




            CreateAttribute(newEntityObject, dtl);
            */
        }



        private bool CheckEntityName(string entityname)
        {
            bool isOK = true;

            //임시로 기능 꺼둠.
            return true;


            //mv_isCreateEntityListRequired;

            if (mv_EntityNameList == null)
            {
                mv_EntityNameList = new List<string>();
                ModelObjects EntityObjectCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Entity", null, null, null);

                foreach (ModelObject targetEntity in EntityObjectCollection)
                {
                    string targetname = "";
                    try
                    {
                        targetname = targetEntity.Properties["Name"].FormatAsString();
                    }
                    catch (Exception ex)
                    {

                    }

                    mv_EntityNameList.Add(targetname);


                }
            }


            if (mv_EntityNameList.Contains(entityname))
            {
                isOK = false;
            }

            return isOK;
            

        }


        private void CreateEntity(ALMSRqstDTL dtl)
        {

            //이미 있는 엔티티인지 체크

            DataTable dt = dtl.ENTDTL;
            string Name = dt.Rows[0]["ENT_NM"].ToString();

            if (!CheckEntityName(Name))
            {
                throw new Exception("생성하려는것과 같은 이름의 엔티티가 이미 있습니다.");
            }





            ModelObjects newObjectCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, mv_session.ModelObjects.Root.ClassId, null, null, null);







            ModelObject newEntityObject = newObjectCollection.Add("Entity", null);


            //default
            
            string Type = "Independent";
            string Definition = dt.Rows[0]["ENT_DESC"].ToString();

            string Note2 = "";
            try
            {
                Note2 = dt.Rows[0]["DATA_CRE_DEL_RULE_DESC"].ToString();//Note 2

            }
            catch (Exception ex)
            {

            }
            
            
            string IndexGenerate = "1"; //Index Generate
            string PhysicalName = dt.Rows[0]["TBL_NM"].ToString();//Physical Name
            string DBOwner = dt.Rows[0]["OWNR_CD"].ToString(); //DB Owner

            //Script Templates : DEF_COMMENT ???

            //only for ukey 

            string sjaname = dt.Rows[0]["SJA_NM"].ToString(); //소속주제영역


            newEntityObject.Properties["Name"].set_Value(null, null, Name);
            newEntityObject.Properties["Type"].set_Value(null, null, Type);
            newEntityObject.Properties["Definition"].set_Value(null, null, Definition);
            newEntityObject.Properties["Note 2"].set_Value(null, null, Note2);
            newEntityObject.Properties["Index Generate"].set_Value(null, null, IndexGenerate);
            newEntityObject.Properties["Physical Name"].set_Value(null, null, PhysicalName);
            newEntityObject.Properties["DB Owner"].set_Value(null, null, DBOwner);

            try
            {

                newEntityObject.Properties["소속주제영역"].set_Value(null, null, sjaname);
            }
            catch (Exception ex)
            {

            }



            ModelObjects subjCollections = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Subject Area", null, null, null);

            
            ModelObject subjcollection = subjCollections[sjaname, "Subject Area"];
            //ModelObject subjcollection = subjCollections["04.D05 고객서비스정보_기타_비정상영업관리", "Subject Area"];

            ModelObjects storedDisplayCollections = mv_session.ModelObjects.Collect(subjcollection, "Stored Display", null, null, null);

            ModelObject firstStoredDisplay = null;
            foreach (ModelObject storedisplayobject in storedDisplayCollections)
            {
                firstStoredDisplay = storedisplayobject;
                break;
            }

            ModelObjects drawingObjEntitycollections = mv_session.ModelObjects.Collect(firstStoredDisplay, "Drawing Object Entity", null, null, null);



            //mv_maxcoordX = mv_maxcoordX + 100;
            if (!mv_isMaxCoordSet)
            {

                foreach (ModelObject entity in drawingObjEntitycollections)
                {
                    string location = entity.Properties["DO Location"].FormatAsString();
                    string[] locstr = location.Split(',');

                    int coord = Convert.ToInt32(locstr[0].Replace("(", ""));

                    if (coord > mv_maxcoordY)
                    {
                        mv_maxcoordY = coord;

                    }
                }


                mv_maxcoordY = mv_maxcoordY + 300;
                mv_isMaxCoordSet = true;
            }
            



            ModelObject newDeawingObjEntity = drawingObjEntitycollections.Add("Drawing Object Entity", null);

            newDeawingObjEntity.Properties["DO Text"].set_Value(null, null, Name);
            
            int rightlowy = mv_maxcoordY + 50;
            int rightlowx = mv_maxcoordX + 50;


            string coordstr = "(" + mv_maxcoordY + "," + mv_maxcoordX + "," + rightlowy + "," + rightlowx + ")";
            newDeawingObjEntity.Properties["DO Location"].set_Value(null, null, coordstr);

            newDeawingObjEntity.Properties["DO Reference Object"].set_Value(null, null, newEntityObject.ObjectId);


            mv_maxcoordX = mv_maxcoordX + 200;

            if (mv_maxcoordX > 2000)
            {
                mv_maxcoordX = 100;
                mv_maxcoordY = mv_maxcoordY + 300;
            }

            CreateAllAttributes(newEntityObject, dtl);

        }

        private void CreateAllAttributes(ModelObject entityObject , ALMSRqstDTL dtl)
        {


            DataTable dt = dtl.ATTRDTL;

            foreach (DataRow dr in dt.Rows)
            {

                if ("CDD" == dr["CHG_CL_CD"].ToString())
                {
                    continue;
                }

                string Name = dr["ATTR_NM"].ToString();

                CreateSingleAttribute(entityObject, dtl, Name);

            }
        }

        private void FixAttributeOrder(ModelObject entityObject, ALMSRqstDTL dtl)
        {
            //ModelProperties attributes = entityObject.CollectProperties("Attribute", null, null);


            DataTable dt = dtl.ATTRDTL;

            ModelObjects AttributeObjCollection = mv_session.ModelObjects.Collect(entityObject, "Attribute", null, null, null);

            foreach (ModelObject attribute in AttributeObjCollection)
            {
                string name = attribute.Properties["Name"].FormatAsString();
                foreach (DataRow dr in dt.Rows)
                {

                    string almsAttrName = dr["ATTR_NM"].ToString();
                    if (almsAttrName == name)
                    {
                        attribute.Properties["Order"].set_Value(null, null, dr["ATTR_SEQ"].ToString());
                        break;
                    }

                   
                }
                
            }


        }


        private string GetDefaultValueId(string value)
        {
            ModelObjects DefaultValueObjectCollection = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Default Value", null, null, null);


            value = value.Trim();

            if (!value.StartsWith("'"))
            {
                value = "'" + value;
            }

            if (!value.EndsWith("'"))
            {
                value = value + "'";
            }


            ModelObject defaultValueObject = null;

            foreach (ModelObject defaultValueObjectCandidate in DefaultValueObjectCollection)
            {
                string defaultvalue = "";
                try
                {
                    defaultvalue = defaultValueObjectCandidate.Properties["LogicalDefault Value"].FormatAsString();
                }
                catch (Exception ex)
                {

                }
                

                if (defaultvalue == value)
                {
                    defaultValueObject = defaultValueObjectCandidate;
                    break;
                }
                
            }

            if (defaultValueObject == null)
            {
                defaultValueObject = DefaultValueObjectCollection.Add("Default Value", null);

                string newName = Guid.NewGuid().ToString();
                defaultValueObject.Properties["Name"].set_Value(null, null, newName);
                defaultValueObject.Properties["Physical Name"].set_Value(null, null, newName);
                defaultValueObject.Properties["Server Value"].set_Value(null, null, value);
                defaultValueObject.Properties["LogicalDefault Value"].set_Value(null, null, value);
            }




            return defaultValueObject.ObjectId;

        }

        private bool ConnectERDSession()
        {
            bool isOK = false;
            mv_app = new SCAPI.Application();

            mv_units = mv_app.PersistenceUnits;
            mv_sessions = mv_app.Sessions;


            mv_session = mv_sessions.Add();


            if (mv_units.Count > 1)
            {
                MessageBox.Show("하나의 ERD만 열어놓고 작업해 주세요.");
                mv_session = null;
                return isOK;
            }
            foreach (PersistenceUnit unit in mv_units)
            {
                //    MessageBox.Show(unit.Name);
                mv_session.Open(unit, null, null);
                //erd여러개 열린경우 수정해야함
            }

            isOK = true;
            return isOK;
        }




        #region BackgroundWorker2

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {

            DataTable dt = e.Argument as DataTable;


        

        }


        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            WorkResult result = e.UserState as WorkResult;

        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {


        }








        #endregion

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        




        private void SetViewPoint(string subjectName, string entityName) 
        {
            try
            {


                ModelObjects subjCollections = mv_session.ModelObjects.Collect(mv_session.ModelObjects.Root, "Subject Area", null, null, null);
                ModelObject subjObject = subjCollections[subjectName, "Subject Area"];

                subjObject.Properties["Current Subject Area"].set_Value(null, null, true);


                ModelObjects storedDisplayCollections = mv_session.ModelObjects.Collect(subjObject, "Stored Display", null, null, null);

                ModelObject firstStoredDisplay = null;
                foreach (ModelObject storedisplayobject in storedDisplayCollections)
                {
                    firstStoredDisplay = storedisplayobject;
                    break;
                }

                //firstStoredDisplay.Properties[""].set_Value(null, null, "");



                ModelObjects drawingObjEntitycollections = mv_session.ModelObjects.Collect(firstStoredDisplay, "Drawing Object Entity", null, null, null);


                ModelObject drawingObjEntity = drawingObjEntitycollections[entityName, "Drawing Object Entity"];

                string location = drawingObjEntity.Properties["Location"].FormatAsString();

                string[] locstr = location.Split(',');

                int coordY = Convert.ToInt32(locstr[0].Replace("(", ""));

                int coordX = Convert.ToInt32(locstr[1]);

                coordY = coordY - 180;
                coordX = coordX - 380;

                if (coordY < 0)
                {
                    coordY = 0;
                }

                if (coordX < 0)
                {
                    coordX = 0;
                }

                firstStoredDisplay.Properties["V Scroll Pos"].set_Value(null, null, coordY);
                firstStoredDisplay.Properties["H Scroll Pos"].set_Value(null, null, coordX);
                firstStoredDisplay.Properties["Zoom Option"].set_Value(null, null, 0);
                firstStoredDisplay.Properties["Display Physical Level"].set_Value(null, null, 0);


            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }


        private void button2_Click_1(object sender, EventArgs e)
        {


            mv_app = new SCAPI.Application();

            mv_units = mv_app.PersistenceUnits;
            mv_sessions = mv_app.Sessions;


            mv_session = mv_sessions.Add();



            foreach (PersistenceUnit unit in mv_units)
            {
            //    MessageBox.Show(unit.Name);
                mv_session.Open(unit, null, null);

            }
            object trid = mv_session.BeginTransaction();
            mv_session.CommitTransaction(trid);
            mv_session.Close();


        }





        private void toolStripButtonOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.Filter = "txt Files|*.txt|Excel Files|*.xls|All Files|*.*";
            //ofd.FilterIndex = 1;
            ofd.RestoreDirectory = true;

            DataTable dt = new DataTable();
            dt.Columns.Add("ENT_UPD_TYP_CD");
            dt.Columns.Add("SJA_NM");
            dt.Columns.Add("OWNR_CD");
            dt.Columns.Add("ENT_NM");
            dt.Columns.Add("TBL_NM");
            dt.Columns.Add("ENT_DESC");
            dt.Columns.Add("CHG_CL_CD");
            dt.Columns.Add("ATTR_NM");
            dt.Columns.Add("COL_NM");
            dt.Columns.Add("PRK_SEQ");
            dt.Columns.Add("ATTR_SEQ");
            dt.Columns.Add("ATTR_DESC");
            dt.Columns.Add("BAS_VAL_CTT");
            dt.Columns.Add("NUL_CL_CD");
            dt.Columns.Add("INFO_TYPE");
            dt.Columns.Add("PSC_DATA_TYP_NM");
            dt.Columns.Add("PSC_EFF_PLC_CNT");
            dt.Columns.Add("PSC_DECIMAL_PLC_CNT");


            DataTable dtFile = new DataTable();
            try
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {


                    dtFile = FileUtil.ReadDataFileByDelimiter(ofd.FileName, LoaderEncoding.UTF8, dtFile);

                }
            }
            finally { }

            int attrSeq = 0;
            string lastEntityName = "";//걍 일케 하자...대강...



            for (int i = 0; i < dtFile.Rows.Count; i++)
            {
                DataRow dr = dt.NewRow();
                DataRow drFile = dtFile.Rows[i];

                /*
                string entityName = drFile[2].ToString();

                if (entityName == lastEntityName)
                {
                    attrSeq = attrSeq + 1;
                }
                else
                {
                    attrSeq = 1;
                }
                */
                dr["ENT_UPD_TYP_CD"] = drFile[0].ToString();
                dr["SJA_NM"] = drFile[1].ToString();
                dr["OWNR_CD"] = drFile[2].ToString();
                dr["ENT_NM"] = drFile[3].ToString();
                dr["TBL_NM"] = drFile[4].ToString();
                dr["ENT_DESC"] = drFile[5].ToString();
                dr["CHG_CL_CD"] = drFile[6].ToString();
                dr["ATTR_NM"] = drFile[7].ToString();
                dr["COL_NM"] = drFile[8].ToString();

                string prkseq = null;
                if (string.IsNullOrEmpty(drFile[9].ToString()))
                {
                    prkseq = "0";
                }
                else
                {
                    prkseq = drFile[9].ToString();
                }

                dr["PRK_SEQ"] = prkseq;
                //dr["ATTR_SEQ"] = attrSeq;
                dr["ATTR_DESC"] = drFile[10].ToString();
                dr["BAS_VAL_CTT"] = drFile[11].ToString();
                
                dr["NUL_CL_CD"] = drFile[12].ToString();



                try
                {
                   

                    dr["PSC_DATA_TYP_NM"] = drFile[14].ToString();
                    dr["PSC_EFF_PLC_CNT"] = drFile[15].ToString();
                    dr["PSC_DECIMAL_PLC_CNT"] = drFile[16].ToString();

                }
                catch (Exception ex)
                {
                    dr["INFO_TYPE"] = drFile[13].ToString();
                }




                dt.Rows.Add(dr);

            }


            dataGridViewEntityDtl.DataSource = dt;



            mv_almsRqstDtl = new List<ALMSRqstDTL>();

            List<string> entityNames = new List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                string entityName = dr[3].ToString();
                string owner = dr[2].ToString();
                string tableName = dr[4].ToString();
                string entityUniqueName = owner + entityName + tableName;

                if (entityNames.Contains(entityUniqueName))
                {
                    foreach (ALMSRqstDTL dtl in mv_almsRqstDtl)
                    {
                        if (dtl.ENTNM == entityName && dtl.OWNER == owner && dtl.TBLNM == tableName)
                        {
                            int attrseq = dtl.ATTRDTL.Rows.Count + 1;

                            dr["ATTR_SEQ"] = "" + attrseq;
                            dtl.ATTRDTL.ImportRow(dr);
                            break;
                        }
                    }
                }
                else
                {
                    entityNames.Add(entityUniqueName);

                    ALMSRqstDTL dtl = new ALMSRqstDTL();
                    dtl.IsTarget = true;
                    dtl.ROWID = i;
                    dtl.ENTDTL = dt.Clone();
                    dtl.ENTDTL.ImportRow(dr);
                    dtl.ROWID = i;
                    DataTable dtattr = dt.Clone();

                    dr["ATTR_SEQ"] = "1";
                        

                    dtattr.ImportRow(dr);
                    dtl.ATTRDTL = dtattr;
                    mv_almsRqstDtl.Add(dtl);
                }

            }



        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            ConnectERDSession();
            backgroundWorker1.RunWorkerAsync();
        }




        private void AddLog(string txt)
        {
            string log = System.DateTime.Now + " : " + txt + System.Environment.NewLine;
            richTextBox1.AppendText(log);

            this.richTextBox1.SelectionLength = 0;
            this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            is_stopIssued = true;
        }

        private void ERManipulatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mv_session.Close();
        }


        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Enabled = true;
        }












        



    }

    public class ALMSRqstDTL
    {
        private DataTable entDTL;
        private DataTable attrDTL;
        private string rqstID;
        private bool isTarget;
        private int rowID;

        public int ROWID
        {
            get { return rowID; }
            set { rowID = value; }
        }

        public bool IsTarget
        {
            get { return isTarget; }
            set { isTarget = value; }
        }

        public string TBLNM
        {
            get
            {
                return entDTL.Rows[0]["TBL_NM"].ToString();


            }
        }


        public string OWNER
        {
            get
            {
                return entDTL.Rows[0]["OWNR_CD"].ToString();


            }
        }



        public string ENTNM
        {
            get
            {
                return entDTL.Rows[0]["ENT_NM"].ToString();
                

            }
        }

        public string SJANM
        {
            get
            {
                return entDTL.Rows[0]["SJA_NM"].ToString();


            }
        }


        public DataTable ENTDTL
        {
            get { return entDTL; }
            set { entDTL = value; }
        }

        public DataTable ATTRDTL
        {
            get { return attrDTL; }
            set { attrDTL = value; }
        }

        public string RqstID
        {
            get { return rqstID; }
            set { rqstID = value; }
        }
    }

    public class WorkResult
    {
        string mv_MESSAGE;
        WorkStatus mv_STATUS;
        WorkType mv_WORKTYPE;
        TreeGridNode node;
        List<string> mv_modifiedAttr = new List<string>();
        List<string> mv_nonStrdAttr = new List<string>();
        ModelObject mv_entity;


        public ModelObject ENTITY
        {
            get { return mv_entity; }
            set { mv_entity = value; }
        }
        public List<string> MODIFIEDATTR
        {
            get { return mv_modifiedAttr; }
            set { mv_modifiedAttr = value; }
        }
        public List<string> NONSTRDATTR
        {
            get { return mv_nonStrdAttr; }
            set { mv_nonStrdAttr = value; }
        }
        public WorkType WORKTYPE
        {
            get { return mv_WORKTYPE; }
            set { mv_WORKTYPE = value; }
        }
        public TreeGridNode NODE
        {
            get { return node; }
            set { node = value; }
        }

        public string MESSAGE
        {
            get { return mv_MESSAGE; }
            set { mv_MESSAGE = value; }
        }
        public WorkStatus STATUS
        {
            get { return mv_STATUS; }
            set { mv_STATUS = value; }
        }

    }

    public enum WorkStatus
    {
        EntityWIP,
        EntityWorkError,
        EntityComplete,
        AttrRemoveSkip,
        GeneralError
    }
    
    public enum WorkType
    {
        AddEntityResultToTree,
        SimpleMessage,
        AddSpecialEntityResultToTree
    }

    public class StandardTerm
    {
        public string term_name { get; set; }
        public string physical_term_name { get; set; }
        public string _infotype_name { get; set; }
        public string _data_type { get; set; }
    }

}
