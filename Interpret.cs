
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Compiler
{
    public class Interpret
    {
        public int [] run = new int [1000];//运行时栈
        public GrammarAna gra;
        public StreamWriter sw;
        public int p, b, t;//下一指令、当前程序基地址指针，栈顶指针
        public PcodeInstr ins;//记录指令
        public int Flag;//有无读入

        public Interpret(GrammarAna g)
        {
            gra = g;
        }
        public int base1(int l, int b) {//找到调用过程的位置
	        int b1 = b;
	        while (l > 0) {
		        b1 = run[b1];
		        l = l - 1;
	        }
	        return b1;
        }
        public void initial()//初始化
        {
            t = 0;
            b = 1;
            p = 0;
            run[1] = 0;
            run[2] = 0;
            run[3] = 0;

            Flag = 0;
            ins = new PcodeInstr();//清空

            sw = new StreamWriter("print.txt");

        }
        public void interpret()
        {
            do
            {
                ins = gra.pCodeTable[p];
                p += 1;
                switch (ins.f)
                {
                    case code.LIT:
                        t += 1;
                        run[t] = (int)ins.addr;
                        break;

                    case code.OPR:
                        switch (ins.addr)
                        {
                            case 0:
                                t = b - 1;
                                p = run[t + 3];
                                b = run[t + 2];
                                break;

                            case 1:
                                run[t] = -run[t];
                                break;

                            case 2:
                                t = t - 1;
                                run[t] = run[t] + run[t + 1];
                                break;

                            case 3:
                                t = t - 1;
                                run[t] = run[t] - run[t + 1];
                                break;

                            case 4:
                                t = t - 1;
                                run[t] = run[t] * run[t + 1];
                                break;

                            case 5:
                                t = t - 1;
                                run[t] = run[t] / run[t + 1];

                                break;

                            case 6:
                                run[t] = run[t] % 2 == 1 ? 1 : 0;
                                break;

                            case 8:
                                t = t - 1;
                                run[t] = run[t] == run[t + 1] ? 1 : 0;
                                break;

                            case 9:
                                t = t - 1;
                                run[t] = run[t] != run[t + 1] ? 1 : 0;
                                break;

                            case 10:
                                t = t - 1;
                                run[t] = run[t] < run[t + 1] ? 1 : 0;
                                break;

                            case 11:
                                t = t - 1;
                                run[t] = run[t] >= run[t + 1] ? 1 : 0;
                                break;

                            case 12:
                                t = t - 1;
                                run[t] = run[t] > run[t + 1] ? 1 : 0;
                                break;

                            case 13:
                                t = t - 1;
                                run[t] = run[t] <= run[t + 1] ? 1 : 0;
                                break;
                        }
                        break;

                    case code.LOD:
                        t = t + 1;
                        run[t] = run[base1((int)ins.level, b) + ins.addr];
                        break;

                    case code.STO:
                        run[base1((int)ins.level, b) + ins.addr] = run[t];
                        t = t - 1;
                        break;

                    case code.CAL:
                        run[t + 1] = base1((int)ins.level, b);
                        run[t + 2] = b;
                        run[t + 3] = p;
                        b = t + 1;
                        p = (int)ins.addr;
                        break;

                    case code.INT:
                        t += (int)ins.addr;
                        break;

                    case code.JMP:
                        p = (int)ins.addr;
                        break;

                    case code.JPC:
                        if (run[t] == 0)
                        {
                            p = (int)ins.addr;
                        }
                        t--;
                        break;

                    case code.RED:
                        Flag = 1;
                        break;

                    case code.WRT:
                        sw.WriteLine(run[t]);
                        t++;
                        break;
                }
                if (Flag == 1) {  break; }
            } while (p != 0);
        }
        public void readNum(int n)//读入数字
        {
            run[base1((int)ins.level, b) + ins.addr]= n;
            Flag = 0;
        }
        public void textClose()//关闭文件
        {
            sw.Close();
        }
    }
}
