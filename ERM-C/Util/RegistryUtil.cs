using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Permissions;
namespace ERM_C
{
    public class RegistryUtil
    {
        public string GetDLLPathFromClassID(string classID)
        {
            var regPath = @"\CLSID\" + classID + @"\InProcServer32\";
            return GetDefaultRegistryValue(Registry.ClassesRoot, regPath);
        }

        public string GetClassIDFromProgID(string progID)
        {
            var regPath = progID + @"\CLSID\";
            return GetDefaultRegistryValue(Registry.ClassesRoot, regPath);
        }

        private string GetDefaultRegistryValue(RegistryKey rootKey, string regPath)
        {
            try
            {
                var regPermission = new RegistryPermission(RegistryPermissionAccess.Read,
                                                            @"HKEY_CLASSES_ROOT\" + regPath);
                regPermission.Demand();
                using (var regKey = rootKey.OpenSubKey(regPath))
                {
                    if (regKey != null)
                    {
                        string defaultValue = (string)regKey.GetValue("");
                        {
                            if (defaultValue == @"mscoree.dll")
                            {
                                defaultValue = (string)regKey.GetValue("CodeBase");
                            }
                            return defaultValue;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //log error
            }
            return "";
        }

        public string GetDLLDirectoryFromProgID(string progID)
        {
            var classID = GetClassIDFromProgID(progID);
            var fileName = GetDLLPathFromClassID(classID);

            if (string.IsNullOrEmpty(fileName))
            {
                return "";
            }
            return Path.GetDirectoryName(fileName);
        }
    }

    
}
