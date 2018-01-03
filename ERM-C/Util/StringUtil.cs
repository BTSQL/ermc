using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainForm.Util
{
    public class StringUtil
    {


        public static string GeneratePassword(int Lenght, int NonAlphaNumericChars, int numbers)
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ";
            string allowedNumbers = "0123456789";
            //string allowedNonAlphaNum = "!@#$%^&*()_-+=[{]};:<>|./?";
            string allowedNonAlphaNum = "!@#$%^*-([<=>])+";
            Random rd = new Random();

            if (NonAlphaNumericChars > Lenght || Lenght <= 0 || NonAlphaNumericChars < 0)
                throw new ArgumentOutOfRangeException();

            char[] pass = new char[Lenght];
            int[] pos = new int[Lenght];
            int i = 0, j = 0, temp = 0;
            bool flag = false;

            //Random the position values of the pos array for the string Pass
            while (i < Lenght - 1)
            {
                j = 0;
                flag = false;
                temp = rd.Next(0, Lenght);
                for (j = 0; j < Lenght; j++)
                    if (temp == pos[j])
                    {
                        flag = true;
                        j = Lenght;
                    }

                if (!flag)
                {
                    pos[i] = temp;
                    i++;
                }
            }

            //Random the AlphaNumericChars
            for (i = 0; i < Lenght - NonAlphaNumericChars - numbers; i++)
                pass[i] = allowedChars[rd.Next(0, allowedChars.Length)];

            //Random the NonAlphaNumericChars
            for (i = Lenght - NonAlphaNumericChars - numbers; i < Lenght - numbers; i++)
                pass[i] = allowedNonAlphaNum[rd.Next(0, allowedNonAlphaNum.Length)];


            for (i = Lenght - numbers; i < Lenght; i++)
                pass[i] = allowedNumbers[rd.Next(0, allowedNumbers.Length)];


            //Set the sorted array values by the pos array for the rigth posistion
            char[] sorted = new char[Lenght];
            for (i = 0; i < Lenght; i++)
                sorted[i] = pass[pos[i]];

            string Pass = new String(sorted);

            return Pass;
        }




    }
}
