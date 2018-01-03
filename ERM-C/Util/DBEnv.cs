using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;  // 레지스트리관련 클래스를 쓰기위해서 추가
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MainForm.Util
{
    class DBEnv
    {
        public enum OracleVersion
        {
            Oracle9,
            Oracle10,
            Oracle0
        };

        private OracleVersion GetOracleVersion()
        {
            RegistryKey rgkLM = Registry.LocalMachine;
            RegistryKey rgkAllHome = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE\ALL_HOMES");

            /* 
             * 10g Installationen don't have an ALL_HOMES key
             * Try to find HOME at SOFTWARE\ORACLE\
             * 10g homes start with KEY_
             */
            string[] okeys = null;
            try
            {
                okeys = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE").GetSubKeyNames();
                foreach (string okey in okeys)
                {
                    if (okey.StartsWith("KEY_"))
                        return OracleVersion.Oracle10;
                }
            }
            catch (Exception ex)
            {

            }





            if (rgkAllHome != null)
            {
                string strLastHome = "";
                object objLastHome = rgkAllHome.GetValue("LAST_HOME");
                strLastHome = objLastHome.ToString();
                RegistryKey rgkActualHome = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ORACLE\HOME" + strLastHome);
                string strOraHome = "";
                object objOraHome = rgkActualHome.GetValue("ORACLE_HOME");
                string strOracleHome = strOraHome = objOraHome.ToString();
                return OracleVersion.Oracle9;
            }
            return OracleVersion.Oracle0;
        }

        private string GetOracleHome()
        {
            RegistryKey rgkLM = Registry.LocalMachine;
            RegistryKey rgkAllHome = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE\ALL_HOMES");
            OracleVersion ov = this.GetOracleVersion();

            switch (ov)
            {
                case OracleVersion.Oracle10:
                    {
                        string[] okeys = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE").GetSubKeyNames();
                        foreach (string okey in okeys)
                        {
                            if (okey.StartsWith("KEY_"))
                            {
                                return rgkLM.OpenSubKey(@"SOFTWARE\ORACLE\" + okey).GetValue("ORACLE_HOME") as string;
                            }
                        }
                        throw new Exception("No Oracle Home found");
                    }
                case OracleVersion.Oracle9:
                    {
                        string strLastHome = "";
                        object objLastHome = rgkAllHome.GetValue("LAST_HOME");
                        strLastHome = objLastHome.ToString();
                        RegistryKey rgkActualHome = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ORACLE\HOME" + strLastHome);
                        string strOraHome = "";
                        object objOraHome = rgkActualHome.GetValue("ORACLE_HOME");
                        string strOracleHome = strOraHome = objOraHome.ToString();
                        return strOraHome;
                    }
                default:
                    {
                        throw new Exception("No supported Oracle Installation found");
                    }
            }
        }

        public string GetTnsNamesOraFilePath()
        {
            string strOracleHome = GetOracleHome();
            if (strOracleHome != "")
            {
                string strTNSNAMESORAFilePath = strOracleHome + @"\NETWORK\ADMIN\TNSNAMES.ORA";
                if (File.Exists(strTNSNAMESORAFilePath))
                {
                    return strTNSNAMESORAFilePath;
                }
                else
                {
                    strTNSNAMESORAFilePath = strOracleHome + @"\NET80\ADMIN\TNSNAMES.ORA";
                    if (File.Exists(strTNSNAMESORAFilePath))
                    {
                        return strTNSNAMESORAFilePath;
                    }
                    else
                    {
                        throw new SystemException("Could not find tnsnames.ora");
                    }
                }
            }
            else
            {
                throw new SystemException("Could not determine ORAHOME");
            }
        }

        public static string GetTnsNameDescription(string tnsName, string tnsPath)
        {
            string tnsDescription = "";
            string inputString;
            try
            {
                StreamReader streamReader = File.OpenText(tnsPath.Trim()); // FP is the filepath of TNS file

                inputString = streamReader.ReadToEnd();
                string[] temp = inputString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                bool decription = false;
                bool find_tns = false;

                string entry = "";
                string entry_description = "";
                int balance = 0;

                for (int i = 0; i < temp.Length; i++)
                {

                    char[] line = temp[i].Trim().ToCharArray();

                    foreach (char str_chr in line)
                    {
                        if (str_chr == '#')
                        {
                            break;
                        }

                        if (!decription)
                        {
                            if (str_chr != '=')
                                entry += str_chr;
                            else
                            {
                                entry = entry.Trim();
                                if (entry.ToLower() == tnsName.ToLower())
                                    find_tns = true;

                                entry = "";
                                decription = true;
                            }
                        }
                        else
                        {
                            entry_description += str_chr;
                            switch (str_chr)
                            {
                                case '(':
                                    balance++;
                                    break;
                                case ')':
                                    --balance;
                                    if (balance == 0)
                                    {
                                        decription = false;

                                        if (find_tns == true)
                                            return entry_description;

                                        entry_description = "";
                                    }

                                    break;
                            }
                        }
                    }

                }


                streamReader.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("" + ex.ToString());
            }

            return tnsDescription;
        }


        public List<string> GetTnsNames(string filepath)
        {

            //string inputString;
            //List<string> List = new List<string>();

            //try
            //{
            //    StreamReader streamReader = File.OpenText(filepath.Trim()); // FP is the filepath of TNS file

            //    inputString = streamReader.ReadToEnd();
            //    string[] temp = inputString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            //    for (int i = 0; i < temp.Length; i++)
            //    {
            //        if (temp[i].Trim(' ', '(').Contains("DESCRIPTION") || temp[i].Trim(' ', '(').Contains("DESCRIPTION_LIST"))
            //        {
            //            string DS = temp[i - 1].Trim('=', ' ');
            //            List.Add(DS);
            //        }

            //    }
            //    streamReader.Close();
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("" + ex.ToString() );
            //}



            List<string> DBNamesCollection = new List<string>();
            string RegExPattern = @"[\n][\s]*[^\(][a-zA-Z0-9_.]+[\s]*=[\s]*\(";
            string strTNSNAMESORAFilePath = filepath;

            if (!strTNSNAMESORAFilePath.Equals(""))
            {
                //check out that file does physically exists
                System.IO.FileInfo fiTNS = new System.IO.FileInfo(strTNSNAMESORAFilePath);
                if (fiTNS.Exists)
                {
                    if (fiTNS.Length > 0)
                    {
                        //read tnsnames.ora file
                        int iCount;
                        for (iCount = 0; iCount < Regex.Matches(
                            System.IO.File.ReadAllText(fiTNS.FullName),
                            RegExPattern).Count; iCount++)
                        {

                            string tmpTns = Regex.Matches(
                                System.IO.File.ReadAllText(fiTNS.FullName),
                                RegExPattern)[iCount].Value.Trim();

                            string tmpTnsName = Regex.Split(tmpTns, @"(\s|=)")[0];

                            //DBNamesCollection.Add(tmpTns.Substring(0,
                            //    Regex.Matches(System.IO.File.ReadAllText(fiTNS.FullName),
                            //    RegExPattern)[iCount].Value.Trim().IndexOf(" ")));

                            DBNamesCollection.Add(tmpTnsName);
                        }
                    }
                }
            }


            return DBNamesCollection;

        }
    }
}
