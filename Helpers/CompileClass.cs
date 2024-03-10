using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Laboratory_6_Oper_sys.Helpers
{
    class LecsemClass
    {
        public uint lineNumber;
        public byte lecsemNumber;
        public string value;

        public LecsemClass(uint lineNumber, byte lecsemNumber, string value)
        {
            this.lineNumber = lineNumber;
            this.lecsemNumber = lecsemNumber;
            this.value = value;
        }
    }

    class CompileClass
    {
        private string file;
        private List<LecsemClass> lecsemsTable;

        public CompileClass(string file)
        {
            this.file = file;
            this.lecsemsTable = new List<LecsemClass>();
        }

        public void Compile()
        {
            SetLecsemTable();
            CodeAnalyzer();
        }

        private void CallErrorFunc(uint lineNumber)
        {
            MessageBox.Show(
                $"Ошибка в строке {lineNumber}",
                "Ошибка компиляции",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);
            return;
        }

        private bool GetNumber(string word, out int value)
        {
            if (word.EndsWith("H"))
            {
                try
                {
                    value = Convert.ToInt32(word.Substring(0, word.Length - 1), 16);
                    return true;
                }
                catch
                {
                    value = 0;
                    return false;
                }
            }
            else
            {
                try
                {
                    value = Convert.ToInt32(word, 10);
                    return true;
                }
                catch
                {
                    value = 0;
                    return false;
                }
            } 
        }

        private byte GetLecsemValue(string value)
        {
            bool isNumber, isCorrectString = false;

            isNumber = GetNumber(value, out int _);
            
            if (!isNumber)
            {
                var regex = new Regex(@"[a-zA-Z@?$][a-zA-Z0-9@?$_.-]*$");
                
                if (regex.IsMatch(value)) isCorrectString = true;
                else isCorrectString = false;
            }

            switch (value)
            {
                case ",": 
                    return 1;
                case "DB":
                    return 2;
                case "DW":
                    return 3;
                case "SUB":
                    return 4;
                case "IMUL":
                    return 5;
                case "POP":
                    return 6;
                case "AL": case "AH":
                case "BL": case "BH":
                case "CL": case "CH":
                case "DL": case "DH":
                    return 7;
                case "AX":
                case "BX":
                case "CX":
                case "DX":
                    return 8;
                case "DS":
                case "SS":
                case "ES":
                    return 9;
                case "CS":
                    return 10;
                default:
                    if (isNumber) return 11;
                    else if (isCorrectString) return 12;
                    else return 0;
            }
        }

        private void SetLecsemTable()
        {
            var lines = File.ReadLines(this.file);
            
            uint lineNumber = 0;

            foreach (var line in lines)
            {
                string word = string.Empty;
                lineNumber++;

                uint symbolNumber = 0;

                foreach (var symbol in line)
                {
                    symbolNumber++;

                    if (!char.IsWhiteSpace(symbol) && symbol != ',' && symbolNumber != line.Length) word += symbol;
                    else
                    {
                        if (!string.IsNullOrEmpty(word))
                        {
                            if (symbolNumber == line.Length) word += symbol;

                            word = word.ToUpper();

                            byte lecsemNumber = GetLecsemValue(word);
                            string lecsemValue = (lecsemNumber > 6) ? word : string.Empty;

                            lecsemsTable.Add(new LecsemClass(lineNumber, lecsemNumber, lecsemValue));

                            word = string.Empty;
                        }

                        if (symbol == ',') lecsemsTable.Add(new LecsemClass(lineNumber, GetLecsemValue(","), String.Empty));
                    }
                }
            }
        }

        private void CodeAnalyzer()
        {
            uint countRows = lecsemsTable[lecsemsTable.Count - 1].lineNumber;
            int startIndex = 0, endIndex = 0;

            bool isNeedVars = true;
            var variables = new List<string[]>();

            var lecsemsCorrect = new byte[] { 8, 9, 12 };

            for (int i = 0; i < countRows - 1; i++)
            {
                for (int j = startIndex; j < lecsemsTable.Count - 1; j++)
                {
                    if (lecsemsTable[startIndex].lineNumber != lecsemsTable[j + 1].lineNumber) break;
                    endIndex++;
                }

                switch (lecsemsTable[startIndex].lecsemNumber)
                {
                    case 0:
                        CallErrorFunc(lecsemsTable[startIndex].lecsemNumber);
                        return;
                    case 4:
                        if (endIndex - startIndex != 3)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        bool checkResult = false;

                        byte firstSubLecsemNumber = lecsemsTable[startIndex + 1].lecsemNumber;
                        byte secondSubLecsemNumber = lecsemsTable[startIndex + 3].lecsemNumber;

                        if ((!lecsemsCorrect.Contains(firstSubLecsemNumber) && firstSubLecsemNumber != 7)
                            || lecsemsTable[startIndex + 2].lecsemNumber != 1
                            || (!lecsemsCorrect.Contains(secondSubLecsemNumber) && secondSubLecsemNumber !=7 && secondSubLecsemNumber != 11))
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        if (firstSubLecsemNumber == 12 || secondSubLecsemNumber == 12)
                        {
                            string[] firstSubValue = null;
                            string[] secondSubVlaue = null;

                            for (int k = 0; k < variables.Count; k++)
                            {
                                if (variables[k][0] == lecsemsTable[startIndex + 1].value)
                                    firstSubValue = variables[k];
                                if (variables[k][0] == lecsemsTable[startIndex + 3].value)
                                    secondSubVlaue = variables[k];
                            }

                            if (firstSubValue != null && secondSubVlaue != null)
                            {
                                checkResult = firstSubValue[1] == secondSubVlaue[1];
                            }
                            else if (firstSubValue != null)
                            {
                                if (secondSubLecsemNumber == 11)
                                {
                                    GetNumber(lecsemsTable[startIndex + 3].value, out int number);

                                    if (firstSubValue[1] == GetLecsemValue("DB").ToString())
                                        checkResult = number > -129 && number < 257;
                                    else
                                        checkResult = number > -32768 && number < 65537;
                                }
                                else
                                {
                                    if (secondSubLecsemNumber == 7)
                                        checkResult = firstSubValue[1] == GetLecsemValue("DB").ToString();
                                    else
                                        checkResult = firstSubValue[1] == GetLecsemValue("DW").ToString();
                                }
                            }
                            else if (secondSubVlaue != null)
                            {
                                if (firstSubLecsemNumber == 7)
                                    checkResult = GetLecsemValue("DB").ToString() == secondSubVlaue[1];
                                else
                                    checkResult = GetLecsemValue("DW").ToString() == secondSubVlaue[1];
                            }
                            else
                                checkResult = false;
                        }
                        else
                        {
                            if (secondSubLecsemNumber == 11)
                            {
                                GetNumber(lecsemsTable[startIndex + 3].value, out int number);

                                if (firstSubLecsemNumber == 7)
                                    checkResult = number > -129 && number < 257;
                                else
                                    checkResult = number > -32768 && number < 65537;
                            }
                            else
                                checkResult = firstSubLecsemNumber == secondSubLecsemNumber;
                        }

                        if (!checkResult)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        break;
                    case 5:
                        if (endIndex - startIndex < 1 || endIndex - startIndex >  5)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        byte firstLecsemNumber = lecsemsTable[startIndex + 1].lecsemNumber;

                        if (endIndex - startIndex == 1)
                        {
                            if (!lecsemsCorrect.Contains(firstLecsemNumber) && firstLecsemNumber != 7)
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }

                            if (firstLecsemNumber == 12)
                            {
                                bool isVariableCorrect = false;

                                for (int k = 0; k < variables.Count; k++)
                                    if (variables[k][0] == lecsemsTable[startIndex + 1].value)
                                        isVariableCorrect = true;

                                if (!isVariableCorrect)
                                {
                                    CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                    return;
                                }
                            }

                            break;
                        }

                        byte secondLecsemNumber = lecsemsTable[startIndex + 3].lecsemNumber;

                        if (endIndex - startIndex == 3)
                        {
                            if (!lecsemsCorrect.Where((lecsem) => lecsem != 12).Contains(firstLecsemNumber)
                                || lecsemsTable[startIndex + 2].lecsemNumber != 1
                                || (!lecsemsCorrect.Contains(secondLecsemNumber) && secondLecsemNumber != 11))
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }

                            bool isVariableCorrect = false;

                            if (secondLecsemNumber == 12)
                            {
                                for (int k = 0; k < variables.Count; k++)
                                {
                                    if (variables[k][0] == lecsemsTable[startIndex + 3].value && variables[k][1] == GetLecsemValue("DW").ToString())
                                    {
                                        isVariableCorrect = true;
                                        break;
                                    }
                                }
                            }
                            else if (secondLecsemNumber == 11)
                            {
                                GetNumber(lecsemsTable[startIndex + 3].value, out int number);

                                isVariableCorrect = number > -32768 && number < 65537;
                            }
                            else
                                isVariableCorrect = firstLecsemNumber == secondLecsemNumber;

                            if (!isVariableCorrect)
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }

                            break;
                        }

                        byte thirdLecsemNumber = lecsemsTable[startIndex + 5].lecsemNumber;

                        if (!lecsemsCorrect.Where((lecsem) => lecsem != 12).Contains(firstLecsemNumber)
                            || lecsemsTable[startIndex + 2].lecsemNumber != 1
                            || !lecsemsCorrect.Contains(secondLecsemNumber)
                            || lecsemsTable[startIndex + 4].lecsemNumber != 1
                            || thirdLecsemNumber != 11)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        bool isCorrect = false;

                        GetNumber(lecsemsTable[startIndex + 5].value, out int constNumber);

                        if (secondLecsemNumber == 12)
                        {
                            for (int k = 0; k < variables.Count; k++)
                                if (variables[k][0] == lecsemsTable[startIndex + 3].value && variables[k][1] == GetLecsemValue("DW").ToString())
                                    isCorrect = constNumber > -32768 && constNumber < 65537;
                        }
                        else
                            isCorrect = (firstLecsemNumber == secondLecsemNumber) && (constNumber > -32768 && constNumber < 65537);

                        if (!isCorrect)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        break;
                    case 6:
                        if (endIndex - startIndex != 1)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        if (!lecsemsCorrect.Contains(lecsemsTable[startIndex + 1].lecsemNumber))
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        if (lecsemsTable[startIndex + 1].lecsemNumber == 12)
                        {
                            bool isVariableCorrect = false;

                            for (int k = 0; k < variables.Count; k++)
                                if (variables[k][0] == lecsemsTable[startIndex + 1].value && variables[k][1] == GetLecsemValue("DW").ToString())
                                    isVariableCorrect = true;

                            if (!isVariableCorrect)
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }
                        }

                        break;
                    case 12:
                        if (!isNeedVars || endIndex - startIndex < 2)
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        if (lecsemsTable[startIndex + 1].lecsemNumber != GetLecsemValue("DB") && lecsemsTable[startIndex + 1].lecsemNumber != GetLecsemValue("DW"))
                        {
                            CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                            return;
                        }

                        for (int k = startIndex + 2; k < endIndex; k++)
                        {
                            bool isNumber = GetNumber(lecsemsTable[k].value, out int number);

                            if (!isNumber)
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }

                            if (!(lecsemsTable[startIndex + 1].lecsemNumber == GetLecsemValue("DB") && number > -129 && number < 257) &&
                                !(lecsemsTable[startIndex + 1].lecsemNumber == GetLecsemValue("DW") && number > -32768 && number < 65537))
                            {
                                CallErrorFunc(lecsemsTable[startIndex].lineNumber);
                                return;
                            }
                        }

                        string[] item = new string[]
                        {
                            lecsemsTable[startIndex].value,
                            lecsemsTable[startIndex + 1].lecsemNumber.ToString()
                        };

                        variables.Add(item);

                        break;
                    default:
                        CallErrorFunc(lecsemsTable[startIndex].lecsemNumber);
                        return;
                }

                endIndex++;
                startIndex = endIndex;
            }

            MessageBox.Show(
                $"Файл успешно прошел проверку!",
                "Успех!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);

            return;
        }
    }
}
