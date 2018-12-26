using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

//using System.Windows.Forms;
using System.IO;

namespace Compiler
{
    public enum Symbol
    {
        BEGINSY = 1, ENDSY = 2, IFSY = 3, THENSY = 4, ELSESY = 5, WHILESY = 6, VARSY = 7, REPEATSY = 8, CALLSY = 9, ODDSY = 10, DOSY = 11, READSY = 12, WRITESY = 13, CONSTSY = 14, PROSY = 15, UNTILSY = 16, IDSY = 20,
        INTSY = 21, PLUSSY = 22, MINUSSY = 23, STARSY = 24, DIVISY = 25, EQUSY = 26, NOEQUSY = 27, LARGESY = 28, LEQUSY = 29, LESSSY = 30, LESSEQUSY = 31, ASSIGNSY = 32,
        LPARSY = 33, RPARSY = 34, COMMASY = 35, SEMISY = 36, DOTSY = 37, NONESY = 38,
    };
    //词法分析
    public class GetSym
    {
        public char CHAR, CHAR1; //CHAR:当前读取的字符 CHAR1:用于检测的字符
        public char[] token; ///存放单词的字符串
        public float result1;//存储常数值 整数部分，小数部分
        public int type, numType;///类型  1为关键字 2为分界符 3为运算符 4为标识符 5为常数 6为空格回车 flag:记录小数点个数 numType:记录数据类型 1正数 2负数 3浮点数
        public int mark; //标记常数正负数 正数1 负数2 零0
        public string str;//存储输出单词
        public string[] keyWord;
        public Symbol symbol;//当前字符类型
        public float max = 10000;
        public string textStr;
        public int loc;//记录文件读取位置
        public Stack<int> s;
        public int row;//行号
        StreamReader sr;

        public void initial()
        {
            token = new char[100];
            keyWord = new string[16] { "begin", "end", "if", "then", "else", "while", "var", "repeat", "call", "odd", "do", "read","write", "const", "procedure", "until" };
            str = "";
            textStr = "";
            loc = 0;
            s = new Stack<int>();
            row = 1;//从1开始，最后会多1
        }

        public void getsym()
        {
            clearToken(); //清空token字符数组
            if (loc < textStr.Length)
                CHAR = Fgetc();
            else return;
            while (isSpace(CHAR)==1 || isNewline(CHAR)==1 || isTab(CHAR)==1)
            {
                if (isNewline(CHAR)==1) row++;//换行
                if (loc < textStr.Length)
                    CHAR = Fgetc();
                else return;
                /*读取字符，跳过空格换行和tab*/
                type = 6;
            }
            if (isLetter() == 1)/*判断当前字符是否是一个字母，如果是字符，则拼接（后面可能为数字也可能为字符）*/
            {
                while (isLetter() == 1 || isDigit() == 1)/*将字符拼接成字符串*/
                {
                    catToken();
                    if (loc < textStr.Length)
                        CHAR = Fgetc();
                    else break ;
                }
                retract();/*指针后退一个字符*/
                int resultValue = reserver();/*resultValue是查找保留字的返回值*/
                if (resultValue == 0)
                {
                    symbol = Symbol.IDSY;/*resultValue=0,token中的字符串为标识符*/
                    type = 4;
                }
                //else symbol=reserver();/*否则token中的字符串为保留字*/
            }
            else if (isDigit() == 1)/*判断当前字符是否是一个数字*/
            {
                while (isDigit() == 1)/*将字符拼接成整数*/
                {
                    catToken();
                    if (loc < textStr.Length)
                        CHAR = Fgetc();
                    else break;
                }
                retract();/*指针后退一个字符*/
                symbol = Symbol.INTSY;/*此时识别的单词是整数*/
                type = 5;
                mark = 1;
                //transNum();
            }
            else if (isAssign() == 1)/*判断当前字符是否是冒号*/
            {
                catToken();
                if (loc < textStr.Length)
                    CHAR = Fgetc();
                else
                {
                    symbol = Symbol.NONESY;
                    return;
                }
                if (isEqu() == 1)
                {
                    catToken();
                    type = 3;
                    symbol = Symbol.ASSIGNSY;/*判断是否是赋值符号*/
                }
                else
                {
                    symbol = Symbol.NONESY;//错误字符
                    //error();
                }
            }
            else if (isPlus() == 1)
            {
                catToken();
                type = 3;
                symbol = Symbol.PLUSSY;    /*判断是否是加号*/
            }
            else if (isMinus() == 1)
            {
                catToken();
                type = 3;
                symbol = Symbol.MINUSSY;   /*判断是否是减号*/
            }
            else if (isStar() == 1)
            {
                catToken();
                type = 3;
                symbol = Symbol.STARSY;    /*判断是否是星号*/
            }
            else if (isDivid() == 1)
            {
                catToken();
                type = 3;
                symbol = Symbol.DIVISY;/*判断是否是除号*/
            }
            else if (isLpar() == 1)
            {
                catToken();
                type = 2;
                symbol = Symbol.LPARSY;  /*判断是否是左括号*/
            }
            else if (isRpar() == 1)
            {
                catToken();
                type = 2;
                symbol = Symbol.RPARSY;  /*判断是否是右括号*/
            }
            else if (isComma() == 1)
            {
                catToken();
                type = 2;
                symbol = Symbol.COMMASY;    /*判断是否是逗号*/
            }
            else if (isSemi() == 1)
            {
                catToken();
                type = 2;
                symbol = Symbol.SEMISY;    /*判断是否是分号*/
            }
            else if (isDot() == 1)
            {
                catToken();
                type = 2;
                symbol = Symbol.DOTSY;   /*判断是否是.*/
            }
            else if (isEqu() == 1)
            {
                catToken();
                type = 3;
                symbol = Symbol.EQUSY;   /*判断是否是等号*/
            }
            else if (isLess() == 1)
            {
                catToken();
                if(loc<textStr.Length)
                {
                    CHAR = Fgetc();
                    if (isEqu() == 1)
                    {
                        catToken();
                        symbol = Symbol.LESSEQUSY;/*判断是否是<=符号*/
                        type = 3;
                    }
                    else if (isLarge() == 1)
                    {
                        catToken();
                        symbol = Symbol.NOEQUSY;/*判断是否是<>符号*/
                        type = 3;
                    }
                    else
                    {
                        retract();
                        symbol = Symbol.LESSSY;
                        type = 3;
                    }
                }
                else
                {
                    symbol = Symbol.LESSSY;
                    type = 3;
                }
            }
            else if (isLarge() == 1)
            {
                catToken();
                if (loc < textStr.Length)
                {
                    CHAR = Fgetc();
                    if (isEqu() == 1)
                    {
                        catToken();
                        symbol = Symbol.LEQUSY;
                        type = 3;
                    }
                    else
                    {
                        retract();
                        symbol = Symbol.LARGESY;
                        type = 3;
                    }
                }
                else
                {
                    symbol = Symbol.LARGESY;
                    type = 3;
                }
                   
            }
            else symbol = Symbol.NONESY;//错误符号
        }

        public void clearToken()
            {
                type = 0;
                mark = 0;//默认为零
                numType = 0;
                result1 = 0;
                while (s.Count()!=0) s.Pop();
                int i = 0;
                while (i < 100)
                {
                    token[i] = '\0';
                    i++;
                }
            }

        public void retract()//回退
            {
                loc--;
            }

        ///连接字符串
        public void catToken()
            {
                int i = 0;
                while (token[i] != '\0')
                {
                    i++;
                }
                token[i] = CHAR;
            }

        public int compare(string des, char[] Token)
            {///字符串比较，区分大小写
                int l = 0;//Token长度
                while (token[l] != '\0')
                {
                    l++;
                }
                int i;
                if (des.Length != l)
                {
                    return 0;
                }
                for (i = 0; Token[i] != '\0'; i++)
                {
                    if (des[i] != Token[i])
                    {
                        return 0;
                    }
                }
                return 1;
            }

        public int reserver()
            {
            /*
            for (int i = 0; token[i] != '\0'; i++) {
                cout<<token[i];
            }
            */
            Console.WriteLine();
                if (compare(keyWord[0], token) == 1)
                {
                    symbol = Symbol.BEGINSY;
                    type = 1;
                    return (int)Symbol.BEGINSY;
                }
                else if (compare(keyWord[1], token) == 1)
                {
                    symbol = Symbol.ENDSY;
                    type = 1;
                    return (int)Symbol.ENDSY;
                }
                else if (compare(keyWord[2], token) == 1)
                {
                    symbol = Symbol.IFSY;
                    type = 1;
                    return (int)Symbol.IFSY;
                }
                else if (compare(keyWord[3], token) == 1)
                {
                    symbol = Symbol.THENSY;
                    type = 1;
                    return (int)Symbol.THENSY;
                }
                else if (compare(keyWord[4], token) == 1)
                {
                    symbol = Symbol.ELSESY;
                    type = 1;
                    return (int)Symbol.ELSESY;
                }
                else if (compare(keyWord[5], token) == 1)
                {
                    symbol = Symbol.WHILESY;
                    type = 1;
                    return (int)Symbol.WHILESY;
                }
                else if (compare(keyWord[6], token) == 1)
                {
                    symbol = Symbol.VARSY;
                    type = 1;
                    return (int)Symbol.VARSY;
                }
                else if (compare(keyWord[7], token) == 1)
                {
                    symbol = Symbol.REPEATSY;
                    type = 1;
                    return (int)Symbol.REPEATSY;
                }
                else if (compare(keyWord[8], token) == 1)
                {
                    symbol = Symbol.CALLSY;
                    type = 1;
                    return (int)Symbol.CALLSY;
                }
                else if (compare(keyWord[9], token) == 1)
                {
                    symbol = Symbol.ODDSY;
                    type = 1;
                    return (int)Symbol.ODDSY;
                }
                else if (compare(keyWord[10], token) == 1)
                {
                    symbol = Symbol.DOSY;
                    type = 1;
                    return (int)Symbol.DOSY;
                }
                else if (compare(keyWord[11], token) == 1)
                {
                    symbol = Symbol.READSY;
                    type = 1;
                    return (int)Symbol.READSY;
                }
                else if (compare(keyWord[12], token) == 1)
                {
                    symbol = Symbol.WRITESY;
                    type = 1;
                    return (int)Symbol.WRITESY;
                }
                else if (compare(keyWord[13], token) == 1)
                {
                    symbol = Symbol.CONSTSY;
                    type = 1;
                    return (int)Symbol.CONSTSY;
                }
                else if (compare(keyWord[14], token) == 1)
                {
                    symbol = Symbol.PROSY;
                    type = 1;
                    return (int)Symbol.PROSY;
                }
                else if (compare(keyWord[15], token) == 1)
                {
                    symbol = Symbol.UNTILSY;
                    type = 1;
                    return (int)Symbol.UNTILSY;
                }
                else
                {
                    return 0;
                }
            }

        public int isSpace(char CHAR)
            {
                return CHAR == ' ' ? 1 : 0;
            }
        public int isNewline(char CHAR)
            {
                return CHAR == '`' ? 1 : 0;
            }
        public int isTab(char CHAR)
            {
                return CHAR == '\t' ? 1 : 0;
            }
        public int isLetter()
            {
                return (CHAR >= 'a' && CHAR <= 'z') ||
                    (CHAR >= 'A' && CHAR <= 'Z') ? 1 : 0;
            }
        public int isDigit()
            {
                return CHAR <= '9' && CHAR >= '0' ? 1 : 0;
            }
        public int isPlus()
            {
                return CHAR == '+' ? 1 : 0;
            }
        public int isMinus()
            {
                return CHAR == '-' ? 1 : 0;
            }
        public int isStar()
            {
                return CHAR == '*' ? 1 : 0;
            }
        public int isLpar()
            {
                return CHAR == '(' ? 1 : 0;
            }
        public int isRpar()
            {
                return CHAR == ')' ? 1 : 0;
            }
        public int isComma()
            {
                return CHAR == ',' ? 1 : 0;
            }
        public int isSemi()
            {
                return CHAR == ';' ? 1 : 0;
            }
        public int isDot()
            {
                return CHAR == '.' ? 1 : 0;
            }
        public int isDivid()
            {
                return CHAR == '/' ? 1 : 0;
            }
        public int isEqu()
            {
                return CHAR == '=' ? 1 : 0;
            }
        public int isLess()
            {
                return CHAR == '<' ? 1 : 0;
            }
        public int isLarge()
            {
                return CHAR == '>' ? 1 : 0;
            }
        public int isAssign() ///:=前一步
            {
                return CHAR == ':' ? 1 : 0;
            }


        ///字符串转数字
        public void transNum()
            {
                int i = 0, Num = 0; //i:数字位数 Num:token下表位数
                if (token[0] == '-')
                {
                    Num = 1;
                    mark = 2;//负数
                    while (token[i + 1] != '\0' && token[i + 1] != '.')
                    {
                        i++;
                    }
                }
                else
                {
                    mark = 1;//正数
                    while (token[i] != '\0' && token[i] != '.')
                    {
                        i++;
                    }
                }
                while (i!=0)
                {
                    result1 = (token[Num] - '0') + result1 * 10;
                    i--; Num++;
                }//i=0
                
            }

        //数字转二进制表示
        public double topow(int x)
            {
                int i;
                double result = 1;
                for (i = 0; i < x; i++)
                {
                    result *= 0.1;
                }
                return result;
            }

        //输出词法分析结果
        public void print()
            {
                str.Remove(0);

                for (int i = 0; token[i] != '\0'; i++)
                {
                    str += token[i];
                }
                //outFile << std::left << setw((30 - str.length()) / 2) << str;
                //outFile << setw((30 - str.length()) / 2) << ' ';
                switch (type)
                {
                    case 1:
                        //outFile << std::left << setw(30) << "关键字";
                        break;

                    case 2:
                        //outFile << std::left << setw(30) << "分界符";
                        break;

                    case 3:
                        //outFile << std::left << setw(30) << "运算符";
                        break;

                    case 4:
                        //outFile << std::left << setw(30) << "标识符";
                        break;

                    case 5:
                        //outFile << std::left << setw(30) << "常数";
                        break;

                    case 6:
                        break;

                    case 0:
                        //outFile << std::left << setw(30) << "错误字符！！";
                        break;
                }
                if (type == 5)//常数输出二进制
                {
                    transNum();
                    if (pdZero()==1) mark = 0;
                if (mark == 0) Console.WriteLine("0");
                else if (mark == 1)
                {
                    //outFile<<0;
                    intTen2two((int)result1);

                }
                else if (mark == 2)//负数
                {
                    //cout << 1;
                    intTen2two((int)result1);

                }
                }
                //else
                    //outFile << std::left << str << " ";
                //outFile << endl;
            }

        public void intTen2two(int number)
            {
                int b;
                while (number != 0)
                {
                    b = number % 2;
                    number /= 2;
                    s.Push(b);
                }
                //输出
                while (s.Count !=0)
                {
                    //outFile << s.top();
                    s.Pop();
                }
            }

        public int pdZero()//判断0
            {
                if (result1 == 0 ) return 1;
                return 0;
            }

        public char Fgetc()//读取单个字符
        {
            return textStr[loc++];
        }
        public void read()
        {
            StreamReader sr = new StreamReader("test.txt");
            string content = sr.ReadToEnd();
            string[] str1 = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            for (int i = 0; i < str1.Length; i++)
            {
                str1[i] += "`";//加换行标识符
                
            }
            for (int i = 0; i < str1.Length; i++)
            {
                textStr += str1[i];
            }
            sr.Close();
        }
        public bool checkEnd()//判断是不是最后一个  不是true
        {
            if (loc < textStr.Length - 1) return true;
            else return false;
        }

    }
}
