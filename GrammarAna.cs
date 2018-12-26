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
    public enum TYPE
    { //符号表元素类型
        CONST, VAR, PROC
    };
    public struct sign
    { //符号表结构体
        public TYPE type;
        public string name;
        public float value;//const
        public int level;//var,proc
        public int addr;//var,proc
    };

    public enum code
    {
        LIT = 1,
        OPR = 2,
        LOD = 3,
        STO = 4,
        CAL = 5,
        INT = 6,
        JMP = 7,
        JPC = 8,
        RED = 9,
        WRT = 10
    };
    //指令结构体
    public struct PcodeInstr
    {
        public code f; // function code
        public long level;     // level
        public long addr;     // displacement address
                              // 	long src_line;	// 出现 RTE 时指出源程序位置
    };

    public class GrammarAna
    {
        public sign[] symbolTable;
        public int rowNum;//代码行号 从1开始
        public int symNum;//符号表项数 从1开始
        public int LEVEL;//层号 从0开始（主程序为0）
        public int dx;//符号地址 从3开始 程序地址为-1 常量地址为0
                      //int tx0;//记录符号表特定位置
                      //int cx0;//记录code特定位置
        public int pcodeNum;//pcode指令条数 从1开始计数
        public int checkflag;//记录是否check
        public int pdErr;//判断有无错误

        public PcodeInstr[] pCodeTable;
        public string[] err = new string[100];//存储错误信息
        public string[] pcode = new string[1000];//存储pcode指令
        public GetSym gs;
        List<Symbol> recoverSet;//恢复集
        List<Symbol> subProFirst;//分程序first
        List<Symbol> subProFollow;
        List<Symbol> stateFirst;//语句
        List<Symbol> stateFollow;
        List<Symbol> conFirst;//条件
        List<Symbol> conFollow;
        List<Symbol> exprFirst;//表达式
        List<Symbol> exprFollow;
        List<Symbol> termFirst;//项
        List<Symbol> termFollow;
        List<Symbol> factorFirst;//因子
        List<Symbol> factorFollow;
        List<Symbol> declbegsys;
        List<Symbol> statbegsys;

        StreamReader symtableRed;//符号表
        StreamWriter symtableWrt;
        FileStream symtableFs;

        StreamReader pcodeRed;//PCode
        StreamWriter pcodeWrt;
        FileStream pcodeFs;

        StreamReader errorRed;//error
        StreamWriter errorWrt;
        FileStream errorFs;

        StreamReader testRed;//源代码
        FileStream testFs;


        public GrammarAna(GetSym s)//构造函数
        {
            gs = s;
        }
        public void init()
        {
            symbolTable = new sign[1000];//符号表
            pCodeTable = new PcodeInstr[200]; //p-code指令
            recoverSet = new List<Symbol>();

            subProFirst = new List<Symbol> { Symbol.CONSTSY, Symbol.VARSY, Symbol.PROSY, Symbol.IDSY, Symbol.IFSY, Symbol.WHILESY, Symbol.READSY, Symbol.CALLSY, Symbol.BEGINSY, Symbol.WHILESY };
            subProFollow = new List<Symbol> { Symbol.DOTSY, Symbol.SEMISY };

            stateFirst = new List<Symbol> { Symbol.IDSY, Symbol.IFSY, Symbol.WHILESY, Symbol.READSY, Symbol.CALLSY, Symbol.BEGINSY, Symbol.WHILESY };
            stateFollow = new List<Symbol> { Symbol.DOTSY, Symbol.SEMISY, Symbol.ENDSY };

            conFirst = new List<Symbol> { Symbol.ODDSY, Symbol.PLUSSY, Symbol.MINUSSY, Symbol.LPARSY, Symbol.IDSY, Symbol.INTSY };
            conFollow = new List<Symbol> { Symbol.THENSY, Symbol.DOSY };

            exprFirst = new List<Symbol> { Symbol.PLUSSY, Symbol.MINUSSY, Symbol.LPARSY, Symbol.IDSY, Symbol.INTSY };
            exprFollow = new List<Symbol> { Symbol.DOTSY, Symbol.SEMISY, Symbol.ENDSY, Symbol.THENSY, Symbol.DOSY, Symbol.RPARSY, Symbol.EQUSY, Symbol.LARGESY, Symbol.LESSSY, Symbol.NOEQUSY, Symbol.LESSEQUSY, Symbol.LEQUSY };

            termFirst = new List<Symbol> { Symbol.LPARSY, Symbol.IDSY, Symbol.INTSY };
            termFollow = new List<Symbol> { Symbol.DOTSY, Symbol.SEMISY, Symbol.ENDSY, Symbol.THENSY, Symbol.PLUSSY, Symbol.MINUSSY, Symbol.DOSY, Symbol.RPARSY, Symbol.EQUSY, Symbol.LARGESY, Symbol.LESSSY, Symbol.NOEQUSY, Symbol.LESSEQUSY, Symbol.LEQUSY };

            factorFirst = new List<Symbol> { Symbol.LPARSY, Symbol.IDSY, Symbol.INTSY };
            factorFollow = new List<Symbol> { Symbol.DOTSY, Symbol.SEMISY, Symbol.ENDSY, Symbol.THENSY, Symbol.PLUSSY, Symbol.MINUSSY, Symbol.DIVISY, Symbol.STARSY, Symbol.DOSY, Symbol.RPARSY, Symbol.EQUSY, Symbol.LARGESY, Symbol.LESSSY, Symbol.NOEQUSY, Symbol.LESSEQUSY, Symbol.LEQUSY };
            declbegsys = new List<Symbol> { Symbol.CONSTSY, Symbol.VARSY, Symbol.PROSY };

            //初始化pcode指令
            pcode[(int)code.LIT] = "LIT";
            pcode[(int)code.OPR] = "OPR";
            pcode[(int)code.LOD] = "LOD";
            pcode[(int)code.STO] = "STO";
            pcode[(int)code.CAL] = "CAL";
            pcode[(int)code.INT] = "INT";
            pcode[(int)code.JMP] = "JMP";
            pcode[(int)code.JPC] = "JPC";
            pcode[(int)code.RED] = "RED";
            pcode[(int)code.WRT] = "WRT";


            //初始化错误信息
            err[1] = "应是=而不是:=";
            err[2] = "=后应为数";
            err[3] = "标识符后应为=";
            err[4] = "const,var,procedure后应为标识符";
            err[5] = "漏掉逗号或分号";
            err[6] = "过程说明后的符号不正确";
            err[7] = "应为语句";
            err[8] = "程序体内语句部分后的符号不正确";
            err[9] = "应为句号";
            err[10] = "语句之间漏分号";
            err[11] = "标识符未说明";
            err[12] = "不可向常量或过程赋值";
            err[13] = "应为赋值运算符:=";
            err[14] = "call后应为标识符";
            err[15] = "不可调用常量或变量";
            err[16] = "应为then";
            err[17] = "应为分号或end";
            err[18] = "应为do";
            err[19] = "语句后的符号不正确";
            err[20] = "应为关系运算符";
            err[21] = "表达式内不可有过程标识符";
            err[22] = "漏右括号";
            err[23] = "因子后不可为此符号";
            err[24] = "表达式不能以此符号开始";
            err[30] = "这个数太大";
            err[40] = "应为左括号";
            err[41] = "应为until";

            //初始化符号表各量
            LEVEL = -1;
            dx = 3;
            symNum = 0;
            pcodeNum = -1;
            checkflag = 1;
            pdErr = 0;//初始  无错误

            errorWrt = new StreamWriter("error.txt");

        }
        public string getToken() //token获取
        {
            string str = "";
            for (int i = 0; gs.token[i] != '\0'; i++)
            {
                str += gs.token[i];
            }
            return str;
        }

        public string getToken(string str1) //token获取
        {
            string str = str1;
            str = "";
            for (int i = 0; gs.token[i] != '\0'; i++)
            {
                str += gs.token[i];
            }
            return str;
        }

        //<程序> ::= <分程序>.
        public void procedure()//程序
        {
            Console.WriteLine("进入一个程序");
            subProcedure();
            if (getToken() == ".")
            {
                //cout << "procedure success" << endl;
                gs.getsym();

            }
            else error(9);//应为句号
        }

        public void subProcedure()//分程序
        {

            Console.WriteLine("进入一个分程序");
            for (int i = 0; i < subProFirst.Count(); i++)
                recoverSet.Add(subProFirst[i]);
            for (int i = 0; i < subProFollow.Count(); i++)
                recoverSet.Add(subProFollow[i]);
            LEVEL++;//层次++
            dx = 3;//初始化
            int tx0, cx0, dx0;
            genPcode(code.JMP, 0, 0);
            tx0 = symNum;
            dx0 = dx;
            if (LEVEL > 0)//不是第0层
            {
                symbolTable[symNum].addr = pcodeNum;//刚刚装入符号表的那一条就是当前块的记录
            }
            do
            {
                if (getToken() == "const")//常量说明部分
                {
                    consExplain();
                }
                if (getToken() == "var")//变量说明部分
                {
                    Console.WriteLine("进入一个变量说明");
                    varExplain();
                    dx0 = dx;
                }
                if (getToken() == "procedure")//过程说明部分
                {
                    proceExplain();
                }
                test(stateFirst, declbegsys, 7);
            } while (declbegsys.Contains(gs.symbol));
            
            pCodeTable[symbolTable[tx0].addr].addr = pcodeNum + 1;//回填指令开始处
            if (LEVEL > 0)
            {
                symbolTable[tx0].addr = pcodeNum + 1;
                cx0 = symNum;
            }
            genPcode(code.INT, 0, dx0);//分配空间
            statement();//语句
            genPcode(code.OPR, 0, 0);
            LEVEL--;
            Console.WriteLine("subprocedure success");
            for (int i = 0; i < subProFirst.Count(); i++)
                recoverSet.Remove(subProFirst[i]);
            for (int i = 0; i < subProFollow.Count(); i++)
                recoverSet.Remove(subProFollow[i]);
        }
        public void consExplain() //常量说明
        {
            if (getToken() == "const")
            {
                checkflag = 1;
                gs.getsym();
                consDefinite();
                while (getToken() == ",")
                {
                    checkflag = 1;
                    gs.getsym();
                    consDefinite();
                }
                if (gs.symbol == Symbol.SEMISY)//判断句尾分号
                {
                    checkflag = 1;
                    Console.WriteLine("consExplain success");
                    gs.getsym();
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(5); recover();
                    }
                    //漏掉逗号或分号 
                }
            }
        }
        public void consDefinite()//常量定义
        {
            string str = "";
            float d;
            if (gs.symbol == Symbol.IDSY)
            {
                checkflag = 1;
                str = getToken(str);
                gs.getsym();
                if (gs.symbol == Symbol.EQUSY || gs.symbol == Symbol.ASSIGNSY)
                {
                    if (gs.symbol == Symbol.ASSIGNSY) { error(1); checkflag = 1; }
                    gs.getsym();
                    if (gs.symbol == Symbol.INTSY)
                    {
                        gs.transNum();//字符串转数字
                        d = gs.result1;
                        insertSymTabC(TYPE.CONST, d, str);//插入符号表
                        Console.WriteLine("consDefinite success");
                        gs.getsym();
                    }
                    else { error(2); recover(); }//=后应为数
                }
                else { error(3); recover(); }//标识符后应为=
            }
            else
            {
                if (checkflag == 1)
                {
                    error(4); recover();//const,var,procedure后应为标识符
                }
            }

        }
        public void varExplain() // 变量说明
        {
            string str = "";
            if (getToken() == "var")
            {
                checkflag = 1;
                gs.getsym();
                if (gs.symbol == Symbol.IDSY)
                {
                    str = getToken(str);
                    insertSymTabV(TYPE.VAR, LEVEL, str);
                    gs.getsym();
                }
                else { error(3); recover(); }//const,var,procedure后应为标识符
                while (gs.symbol == Symbol.COMMASY)
                {
                    checkflag = 1;
                    gs.getsym();
                    if (gs.symbol != Symbol.IDSY) { error(4); recover(); }
                    else
                    {
                        str = getToken(str);
                        insertSymTabV(TYPE.VAR, LEVEL, str);
                        gs.getsym();
                    }
                }
                if (gs.symbol == Symbol.SEMISY)
                {
                    checkflag = 1;
                    Console.WriteLine("varExplain success");
                    gs.getsym();
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(5); recover();
                    }
                }//漏掉逗号或分号
            }
        }
        public void proceExplain()//过程说明
        {
            proHeader();
            Console.WriteLine("进入一个子程序");
            subProcedure();
            if (gs.symbol == Symbol.SEMISY)
            {
                checkflag = 1;
                gs.getsym();
                while (getToken() == "procedure")//解决右递归
                {
                    proceExplain();
                }
            }
            else
            {
                if (checkflag == 1)
                {
                    error(5); recover();
                }
            }//漏掉逗号或分号
        }
        public void proHeader()//过程首部
        {
            string str = "";
            if (getToken() == "procedure")
            {
                checkflag = 1;
                gs.getsym();
                if (gs.symbol != Symbol.IDSY) { error(4); recover(); }//const,var,procedure后应为标识符
                else
                {
                    str = getToken(str);
                    insertSymTabP(TYPE.PROC, LEVEL, str);//插入符号表
                    gs.getsym();
                    if (gs.symbol == Symbol.SEMISY)
                    {
                        Console.WriteLine("proHeader success");
                        gs.getsym();
                    }
                    else { error(5); recover(); }//漏掉逗号或分号
                }
            }
            else
            {
                if (checkflag == 1)
                {
                    error(6); recover();
                }
            }//过程说明后的符号不正确
        }
        public void statement()//语句
        {
            for (int i = 0; i < stateFirst.Count(); i++)
                recoverSet.Add(stateFirst[i]);
            for (int i = 0; i < stateFollow.Count(); i++)
                recoverSet.Add(stateFollow[i]);
            Console.WriteLine("进入一个语句");
            if (gs.symbol == Symbol.IDSY)//开头为标识符——赋值语句
            {
                assignState();
            }
            else if (getToken() == "if")//条件语句
                condiState();
            else if (getToken() == "while")//当型语句
                whileLoop();
            else if (getToken() == "call")//过程调用语句
                proceCall();
            else if (getToken() == "begin")//复合语句
                compoundState();
            else if (getToken() == "repeat")//重复语句
                repeatState();
            else if (getToken() == "read")//读语句
                readState();
            else if (getToken() == "write")//写语句
            {
                writeState();
            }
            else if (stateFollow.Contains(gs.symbol))//在follow集中 什么都不做
            {
            }
            else //其他符号 报错 恢复
            {
                error(19); recover();
            }
            for (int i = 0; i < stateFirst.Count(); i++)
                recoverSet.Remove(stateFirst[i]);
            for (int i = 0; i < stateFollow.Count(); i++)
                recoverSet.Remove(stateFollow[i]);
        }
        public void assignState()//赋值语句
        {
            string str = "";
            int l = 0, a = 0;
            int flag = 0;
            Console.WriteLine("进入一个赋值语句");
            if (gs.symbol == Symbol.IDSY)
            {
                checkflag = 1;
                str = getToken(str);
                for (int i = 1; i <= symNum; i++)
                {
                    if (symbolTable[i].name == str)
                    {
                        if (symbolTable[i].type == TYPE.VAR)
                        {
                            l = symbolTable[i].level;
                            a = symbolTable[i].addr;
                            gs.getsym();
                        }
                        else
                        {
                            error(12);//不可向常量或过程赋值
                            gs.getsym();

                        }
                        flag = 1;//在符号表中找到了该名
                    }
                }
                if (flag == 0)
                {
                    error(11);//标识符未说明 符号表中没找到
                    gs.getsym();
                }
                if (gs.symbol == Symbol.ASSIGNSY)
                {
                    checkflag = 1;
                    gs.getsym();
                    expr();
                    genPcode(code.STO, LEVEL - l, a);//栈顶内容放到次栈顶变量中
                    Console.WriteLine("assignState success");
                }
                else
                {
                    Console.WriteLine(getToken());
                    if (checkflag == 1)
                    {
                        error(13);//应为赋值运算符:=
                        recover();
                    }
                }
            }

        }
        public void condiState()
        {//条件语句
            Console.WriteLine("条件语句");
            int loc1, loc2;
            if (getToken() == "if")
            {
                checkflag = 1;
                gs.getsym();
                condition();
                //该处应当加入一条JPC 到else的开头 后面补上
                loc1 = ++pcodeNum;
                if (getToken() == "then")
                {
                    Console.WriteLine(getToken());
                    checkflag = 1;
                    gs.getsym();
                    statement();
                    if (getToken() == "else")
                    {
                        checkflag = 1;
                        loc2 = ++pcodeNum;//保存JMP位置
                        pCodeTable[loc1].f = code.JPC;
                        pCodeTable[loc1].addr = pcodeNum + 1;
                        gs.getsym();
                        statement();
                        //加入JMP
                        pCodeTable[loc2].f = code.JMP;
                        pCodeTable[loc2].addr = pcodeNum + 1;
                    }
                    else
                    {
                        pCodeTable[loc1].f = code.JPC;
                        pCodeTable[loc1].addr = pcodeNum + 1;
                    }
                    Console.WriteLine("condiState success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(16);
                        recover();
                    }
                }
            }

        }
        public void whileLoop()
        { //当型循环

            int loc1, loc2;
            if (getToken() == "while")
            {
                checkflag = 1;
                loc2 = pcodeNum + 1;
                gs.getsym();
                condition();
                loc1 = ++pcodeNum;
                if (getToken() == "do")
                {
                    checkflag = 1;
                    gs.getsym();
                    statement();
                    genPcode(code.JMP, 0, loc2);
                    pCodeTable[loc1].f = code.JPC;
                    pCodeTable[loc1].addr = pcodeNum + 1;
                    Console.WriteLine("whileLoop success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(18); recover();
                    }
                }
            }

        }
        public void proceCall()
        {//过程调用语句
            string str = "";
            int l, a, flag = 0;
            if (getToken() == "call")
            {
                checkflag = 1;
                gs.getsym();
                if (gs.symbol == Symbol.IDSY)
                {
                    checkflag = 1;
                    str = getToken(str);
                    for (int i = 1; i <= symNum; i++)
                    {
                        if (symbolTable[i].name == str)
                        {
                            if (symbolTable[i].type == TYPE.PROC)
                            {
                                l = symbolTable[i].level;
                                a = symbolTable[i].addr;
                                genPcode(code.CAL, LEVEL - l, a);
                                gs.getsym();
                            }
                            else
                            {//找到的是变量名或常量名
                                error(15); gs.getsym();
                            }
                            flag = 1;//在符号表中找到了该名
                        }
                    }
                    if (flag == 0)
                    {
                        error(11);//未找到
                        gs.getsym();
                    }
                    Console.WriteLine("proceCall 成功");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(14);
                        recover();
                    }
                }
            }
        }
        public void compoundState()//复合语句
        {
            string str = "";
            Console.WriteLine("进入一个复合语句");
            if (getToken() == "begin")
            {
                checkflag = 1;
                gs.getsym();
                statement();
                Console.WriteLine("从语句中出来啦");
                //cout<<getToken()<<endl;
                //str = getToken(str);
                while (gs.symbol == Symbol.SEMISY || gs.symbol == Symbol.IDSY || checkState(getToken()) == 1)//是分号 或者赋值语句 或者其他语句
                {
                    checkflag = 1;
                    Console.WriteLine("进入一个复合中的语句");
                    if (gs.symbol == Symbol.SEMISY)//正确
                    {
                        gs.getsym();
                        statement();
                    }
                    else//少分号 跳过不处理
                    {
                        error(10);
                        statement();
                    }
                }
                if (getToken() == "end")
                {
                    checkflag = 1;
                    gs.getsym();
                    Console.WriteLine("compoundState success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(17);
                        recover();
                    }
                }
            }
        }
        public void repeatState()//重复语句
        {
            int loc1;
            string str = "";
            if (getToken() == "repeat")
            {
                checkflag = 1;
                gs.getsym();
                loc1 = pcodeNum + 1;
                statement();
                str = getToken(str);
                while (gs.symbol == Symbol.SEMISY || gs.symbol == Symbol.IDSY || checkState(str) == 1)//是分号 或者赋值语句 或者其他语句
                {
                    checkflag = 1;
                    Console.WriteLine("进入一个重复语句中的语句");
                    if (gs.symbol == Symbol.SEMISY)//正确
                    {
                        gs.getsym();
                        statement();
                    }
                    else//少分号
                    {
                        error(10);
                        statement();
                    }
                }
                if (getToken() == "until")
                {
                    checkflag = 1;
                    gs.getsym();
                    condition();
                    genPcode(code.JPC, 0, loc1);//若假，转到repeat开始处
                    Console.WriteLine("compoundState success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(41); recover();
                    }
                }
            }

        }
        public void readState()//读语句
        {
            string str = "";
            int l, a, flag = 0;
            if (getToken() == "read")
            {
                checkflag = 1;
                gs.getsym();
                if (gs.symbol == Symbol.LPARSY)//左括号
                {
                    gs.getsym();
                    if (gs.symbol == Symbol.IDSY)//标识符
                    {
                        str = getToken(str);
                        for (int i = symNum; i >=1; i--)
                        {
                            if (symbolTable[i].name == str && symbolTable[i].level <= LEVEL)
                            {
                                if (symbolTable[i].type == TYPE.VAR)//正确
                                {
                                    l = symbolTable[i].level;
                                    a = symbolTable[i].addr;
                                    genPcode(code.RED, LEVEL - l, a);
                                    gs.getsym();
                                }
                                else
                                {//找到的是过程名或常量名

                                    error(12);
                                    gs.getsym();
                                }
                                flag = 1;//在符号表中找到了该名
                                break;
                            }
                        }
                        if (flag == 0)
                        {
                            error(11); gs.getsym();
                        }

                        while (gs.symbol == Symbol.COMMASY)//逗号
                        {
                            checkflag = 1;
                            flag = 0;
                            gs.getsym();
                            if (gs.symbol != Symbol.IDSY)//标识符
                            {
                                if (checkflag == 1)
                                {
                                    error(4); recover();
                                }
                            }
                            else
                            {
                                str = getToken(str);
                                for (int i = symNum; i >=1; i--)
                                {
                                    if (symbolTable[i].name == str && symbolTable[i].level <= LEVEL)
                                    {
                                        if (symbolTable[i].type == TYPE.VAR)
                                        {
                                            l = symbolTable[i].level;
                                            a = symbolTable[i].addr;
                                            genPcode(code.RED, LEVEL - l, a);
                                            gs.getsym();
                                        }
                                        else
                                        {//找到的是过程名或常量名
                                            error(12); gs.getsym();
                                        }
                                        flag = 1;//在符号表中找到了该名
                                        break;
                                    }
                                }
                                if (flag == 0)
                                {
                                    error(11); gs.getsym();
                                }

                            }
                        }
                        if (gs.symbol == Symbol.RPARSY)//右括号
                        {
                            checkflag = 1;
                            gs.getsym();
                            Console.WriteLine("readState success");
                        }
                        else
                        {
                            if (checkflag == 1)
                            {
                                error(22); recover();
                            }
                        }
                    }
                    else
                    {
                        if (checkflag == 1)
                        {
                            error(4); recover();
                        }
                    }
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(40); recover();
                    }
                }
            }
        }
        public void writeState()//写语句
        {
            if (getToken() == "write")
            {
                checkflag = 1;
                gs.getsym();
                if (gs.symbol == Symbol.LPARSY)//左括号
                {
                    checkflag = 1;
                    gs.getsym();
                    expr();
                    genPcode(code.WRT, 0, 0);
                    while (gs.symbol == Symbol.COMMASY)//逗号
                    {
                        gs.getsym();
                        checkflag = 1;
                        expr();
                        genPcode(code.WRT, 0, 0);
                    }
                    if (gs.symbol == Symbol.RPARSY)//右括号
                    {
                        checkflag = 1;
                        gs.getsym();
                        Console.WriteLine("writeState success");
                    }
                    else
                    {
                        if (checkflag == 1)
                        {
                            error(22); recover();
                        }
                    }
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(40); recover();
                    }
                }
            }
        }
        public void condition()//条件
        {
            for (int i = 0; i < conFirst.Count(); i++)
                recoverSet.Add(conFirst[i]);
            for (int i = 0; i < conFollow.Count(); i++)
                recoverSet.Add(conFollow[i]);
            Console.WriteLine("进入条件");
            string str = "";
            if (getToken() == "odd")
            {
                checkflag = 1;
                gs.getsym();
                expr();
                genPcode(code.OPR, 0, 6);
                Console.WriteLine("condition success");
            }
            else
            {
                expr();
                if (relaOpr())
                {
                    checkflag = 1;
                    str = getToken(str);//获取关系运算符
                    gs.getsym();
                    expr();
                    //根据关系运算符进行比较
                    if (str == "=")
                    {
                        genPcode(code.OPR, 0, 8);
                    }
                    else if (str == "<>")
                    {
                        genPcode(code.OPR, 0, 9);
                    }
                    else if (str == "<")
                    {
                        genPcode(code.OPR, 0, 10);
                    }
                    else if (str == "<=")
                    {
                        genPcode(code.OPR, 0, 13);
                    }
                    else if (str == ">")
                    {
                        genPcode(code.OPR, 0, 12);
                    }
                    else if (str == ">=")
                    {
                        genPcode(code.OPR, 0, 11);
                    }
                    Console.WriteLine("condition success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(20); recover();
                    }
                }
            }
            for (int i = 0; i < conFirst.Count(); i++)
                recoverSet.Remove(conFirst[i]);
            for (int i = 0; i < conFollow.Count(); i++)
                recoverSet.Remove(conFollow[i]);
        }
        public void factor()//因子
        {
            for (int i = 0; i < factorFirst.Count(); i++)
                recoverSet.Add(factorFirst[i]);
            for (int i = 0; i < factorFollow.Count(); i++)
                recoverSet.Add(factorFollow[i]);
            string str = "";
            int l, a, flag = 0;
            Console.WriteLine("进入一个因子");
            if (gs.symbol == Symbol.IDSY)
            {
                checkflag = 1;
                str = getToken(str);
                for (int i = symNum; i >= 1; i--)
                {
                    if (symbolTable[i].name == str&& symbolTable[i].level <= LEVEL)
                    {
                        if (symbolTable[i].type == TYPE.VAR)
                        {
                            l = symbolTable[i].level;
                            a = symbolTable[i].addr;
                            genPcode(code.LOD, LEVEL - l, a);
                            gs.getsym();
                        }
                        else if (symbolTable[i].type == TYPE.CONST)
                        {
                            l = (int)symbolTable[i].value;
                            genPcode(code.LIT, 0, l);//装入常量的值
                            gs.getsym();
                        }
                        else
                        {//找到的是过程名
                            error(21);
                            gs.getsym();
                        }
                        flag = 1;//在符号表中找到了该名
                        break;
                    }
                }
                if (flag == 0)
                {
                    error(11);
                    gs.getsym();
                }
                Console.WriteLine("factor success");
            }
            else if (gs.symbol == Symbol.INTSY)
            {
                checkflag = 1;
                gs.transNum();
                if (gs.result1 > gs.max) { error(30); checkflag = 1; gs.result1 = gs.max; }//若超过直接等于最大值

                genPcode(code.LIT, 0, (int)gs.result1);//装入常量的值
                gs.getsym();
                Console.WriteLine("factor success");

            }
            else if (gs.symbol == Symbol.LPARSY)
            {
                checkflag = 1;
                gs.getsym();
                expr();
                if (gs.symbol == Symbol.RPARSY)
                {
                    checkflag = 1;
                    gs.getsym();
                    Console.WriteLine("factor success");
                }
                else
                {
                    if (checkflag == 1)
                    {
                        error(22); recover();
                    }
                }
            }
            else
            {
                if (checkflag == 1)
                {
                    error(23); recover();
                }
            }
            for (int i = 0; i < factorFirst.Count(); i++)
                recoverSet.Remove(factorFirst[i]);
            for (int i = 0; i < factorFollow.Count(); i++)
                recoverSet.Remove(factorFollow[i]);
        }
        public void term()//项
        {
            for (int i = 0; i < termFirst.Count(); i++)
                recoverSet.Add(termFirst[i]);
            for (int i = 0; i < termFollow.Count(); i++)
                recoverSet.Add(termFollow[i]);
            string str = "";
            Console.WriteLine("进入一个项");
            factor();
            while (mutiOpr())
            {
                checkflag = 1;
                str = getToken(str);
                gs.getsym();
                factor();
                if (str == "*")
                    genPcode(code.OPR, 0, 4);
                else genPcode(code.OPR, 0, 5);
            }
            Console.WriteLine("term success");
            for (int i = 0; i < termFirst.Count(); i++)
                recoverSet.Remove(termFirst[i]);
            for (int i = 0; i < termFollow.Count(); i++)
                recoverSet.Remove(termFollow[i]);
        }
        public void expr()//表达式
        {
            for (int i = 0; i < exprFirst.Count(); i++)
                recoverSet.Add(exprFirst[i]);
            for (int i = 0; i < exprFollow.Count(); i++)
                recoverSet.Add(exprFollow[i]);
            string str = "", plus = "";
            Console.WriteLine("进入一个表达式");
            if (plusOpr())
            {
                checkflag = 1;
                plus = getToken(plus);
                term();
                if (plus == "-") genPcode(code.OPR, 0, 1);//取负
                gs.getsym();
            }
            else term();//项
            while (plusOpr())
            {
                checkflag = 1;
                str = getToken(str);
                gs.getsym();
                term();
                if (str == "+")
                    genPcode(code.OPR, 0, 2);//加
                else genPcode(code.OPR, 0, 3);
            }
            Console.WriteLine("expr success");
            for (int i = 0; i < exprFirst.Count(); i++)
                recoverSet.Remove(exprFirst[i]);
            for (int i = 0; i < exprFollow.Count(); i++)
                recoverSet.Remove(exprFollow[i]);
        }
        public bool plusOpr()//判断加法运算符
        {
            if (gs.symbol == Symbol.PLUSSY || gs.symbol == Symbol.MINUSSY)
                return true;
            return false;
        }
        public bool mutiOpr()//判断乘法运算符
        {
            if (gs.symbol == Symbol.STARSY || gs.symbol == Symbol.DIVISY)
                return true;
            return false;
        }
        public bool relaOpr()//判断关系运算符
        {
            if (gs.symbol == Symbol.EQUSY || gs.symbol == Symbol.NOEQUSY || gs.symbol == Symbol.LEQUSY || gs.symbol == Symbol.LESSEQUSY || gs.symbol == Symbol.LARGESY || gs.symbol == Symbol.LESSSY)
                return true;
            return false;
        }

        public int searchTable(string str, char s)//判断字符串中是否存在某字符
        {
            for (int i = 0; i < str.Length; i++)
                if (str[i] == s)
                    return 1;
            return 0;
        }

        public void genPcode(code pNo, int l, int a)//生成pcode指令并保存进pcode表中
        {
            pcodeNum++;
            pCodeTable[pcodeNum].f = pNo;
            pCodeTable[pcodeNum].level = l;
            pCodeTable[pcodeNum].addr = a;
        }
        public void printPcode()
        {
            for (int i = 0; i <= pcodeNum; i++)
            {
                PcodeInstr it = pCodeTable[i];
                Console.WriteLine(i + " " + pcode[(int)it.f] + " " + it.level + " " + it.addr);
            }
        }
        public void insertSymTabV(TYPE t, int l, string str)//var插入符号表
        {
            symNum++;
            symbolTable[symNum].type = t;
            symbolTable[symNum].addr = dx;
            symbolTable[symNum].level = l;
            symbolTable[symNum].name = str;
            dx++;
        }
        public void insertSymTabP(TYPE t, int l, string str)//proc插入符号表
        {
            symNum++;
            symbolTable[symNum].type = t;
            symbolTable[symNum].level = l;
            symbolTable[symNum].name = str;
        }
        public void insertSymTabC(TYPE t, float v, string str)//const插入符号表
        {
            symNum++;
            symbolTable[symNum].type = t;
            symbolTable[symNum].value = v;
            symbolTable[symNum].name = str;
        }
        public int checkState(string str)//判断除赋值语句之外的语句
        {
            if (str == "if" || str == "while" || str == "call" || str == "begin" || str == "repeat" || str == "read" || str == "write")
                return 1;
            return 0;
        }
        public void error(int i)
        {
            checkflag = 0;
            if (pdErr == 0) errorWrt.WriteLine(String.Format("{0,-6} | {1,-9} | {2,-15}", "行数", "错误类型", "错误详情"));
            errorWrt.WriteLine(String.Format("{0,-1} {1,-1} {2,-3}  {3,-9}  {4,-15}", "第", gs.row, "行", i, err[i]));
            pdErr = 1;
        }
        public void check(List<Symbol> S1, int n)
        {
            if (!S1.Contains(gs.symbol))
            {
                error(n);
                pdErr = 1;
                checkflag = 0;
                while (!recoverSet.Contains(gs.symbol))
                {
                    gs.getsym();//恢复
                }
            }
        }
        public void recover()
        {
            while (!recoverSet.Contains(gs.symbol))
            {
                gs.getsym();//恢复
            }
        }
        public void test(List<Symbol> S1, List<Symbol> S2, int n)
        {
            if (!S1.Contains(gs.symbol))
            {
                if (pdErr == 0) errorWrt.WriteLine(String.Format("{0,-6} | {1,-9} | {2,-15}", "行数","错误类型","错误详情"));
                errorWrt.WriteLine(String.Format("{0,-1} {1,-3} {2,-3}  {3,-9}  {4,-15}", "第",gs.row,"行", n , err[n]));
                pdErr = 1;
                for (int i = 0; i < S2.Count(); i++)
                    S1.Add(S2[i]);
                while (!S1.Contains(gs.symbol)) gs.getsym();
            }
        }
        public void print()
        {
            if(pdErr == 0)//无错
            {
                pcodeWrt = new StreamWriter("pcode.txt");
                for (int i = 0; i <= pcodeNum; i++)
                {
                    PcodeInstr it = pCodeTable[i];
                    if(it.f == code.LIT) pcodeWrt.WriteLine(String.Format("{0,-9}   {1,-14}   {2,-10}   {3,-9}", i, pcode[(int)it.f], it.level, it.addr));
                    else if (it.f == code.OPR|| it.f == code.LOD || it.f == code.WRT) pcodeWrt.WriteLine(String.Format("{0,-9}   {1,-11}   {2,-10}   {3,-9}", i, pcode[(int)it.f], it.level, it.addr));
                    else if (it.f == code.JPC) pcodeWrt.WriteLine(String.Format("{0,-9}   {1,-13}   {2,-10}   {3,-9}", i, pcode[(int)it.f], it.level, it.addr));
                    else pcodeWrt.WriteLine(String.Format("{0,-9}   {1,-12}   {2,-10}   {3,-9}", i, pcode[(int)it.f], it.level, it.addr));
                }
                pcodeWrt.Close();

                symtableWrt = new StreamWriter("symbol.txt");
                symtableWrt.WriteLine(String.Format("{0,-7} | {1,-10} | {2,-8} | {3,-7}| {4,-7} |{5,-7}", "序号" , "名称" ,"类型" , "值" , "层次","地址"));
                for (int i = 1; i <= symNum; i++)
                {
                    if (symbolTable[i].type == TYPE.CONST)
                        symtableWrt.WriteLine(String.Format("{0,-10}  {1,-14}  {2,-12}  {3,-9} {4,-9} {5,-9}", i, symbolTable[i].name, symbolTable[i].type, symbolTable[i].value, " ", " "));
                    else if (symbolTable[i].type == TYPE.VAR )
                        symtableWrt.WriteLine(String.Format("{0,-10}  {1,-14}  {2,-8}  {3,-17} {4,-12} {5,-9}", i, symbolTable[i].name, symbolTable[i].type, " ", symbolTable[i].level, symbolTable[i].addr));
                    else if (symbolTable[i].type == TYPE.PROC)
                        symtableWrt.WriteLine(String.Format("{0,-10}  {1,-14}  {2,-6}  {3,-17} {4,-12} {5,-9}", i, symbolTable[i].name, symbolTable[i].type, " ", symbolTable[i].level, symbolTable[i].addr));
                }
                symtableWrt.Close();

                errorWrt.Close();
            }
            else
            {
                errorWrt.Close();
            }
          
        }
    }
}