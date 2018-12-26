using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using System.Forms;
using System.IO;
using System.Threading;

namespace Compiler
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public string path;
        public GetSym word;
        public GrammarAna GA;
        public Interpret inter;
        public int first;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)//选择文件按钮 显示原始代码
        {
            path = SelectPath();
            //输出代码
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            StreamReader m_streamReader = new StreamReader(fs, Encoding.Default);
            //使用StreamReader类来读取文件
            m_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            //从数据流中读取每一行，直到文件的最后一行，并在textBox中显示出内容，其中textBox为文本框，如果不用可以改为别的
            this.PL0TextBox.Text = "";
            string strLine = m_streamReader.ReadLine();
            while (strLine != null)
            {
                this.PL0TextBox.Text += strLine + "\n";
                strLine = m_streamReader.ReadLine();
            }
            //关闭此StreamReader对象
            fs.Close();
            m_streamReader.Close();

            word = new GetSym();
            GA = new GrammarAna(word);//初始化
            inter = new Interpret(GA);

        }

        private string SelectPath()//选择文件函数
        {
            string path = string.Empty;
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Files (*.*)|*.*"//如果需要筛选txt文件（"Files (*.txt)|*.txt"）
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                path = openFileDialog.FileName;
            }
            return path;
            
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e)//编译按钮
        {
            //清空test.txt文件
            FileStream fs = new FileStream(@"test.txt", FileMode.Truncate, FileAccess.ReadWrite);
            fs.Close();
            String conpath = path;//定义要获取的文件内容地址
            getContentToFile(conpath);//将path中内容写入到自己的test文件中

            //清空pcode error symbol文件
            //清空pcode.txt文件
            FileStream fs2 = new FileStream(@"pcode.txt", FileMode.Truncate, FileAccess.ReadWrite);
            fs2.Close();
            //清空symbol.txt文件
            FileStream fs3 = new FileStream(@"symbol.txt", FileMode.Truncate, FileAccess.ReadWrite);
            fs3.Close();
            //清空error.txt文件
            FileStream fs4 = new FileStream(@"error.txt", FileMode.Truncate, FileAccess.ReadWrite);
            fs4.Close();
            //输出运行结果
            //清空print.txt文件
            FileStream fs5 = new FileStream(@"print.txt", FileMode.Truncate, FileAccess.ReadWrite);
            fs5.Close();

            first = 0;


            word.initial();
            word.read();
            word.getsym();
            GA.init();
            GA.procedure();
            GA.print();//信息存入相应文件

            


            //打印pcode
            FileStream pcodefs = new FileStream("pcode.txt", FileMode.Open, FileAccess.Read);
            StreamReader p_streamReader = new StreamReader(pcodefs, Encoding.Default);
            //使用StreamReader类来读取文件
            p_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            this.PcodeTextBox.Text = "";
            string strLine = p_streamReader.ReadLine();
            while (strLine != null)
            {
                this.PcodeTextBox.Text += strLine + "\n";
                strLine = p_streamReader.ReadLine();
            }
            //关闭此StreamReader对象
            pcodefs.Close();
            p_streamReader.Close();

            //打印符号表
            FileStream symbolfs = new FileStream("symbol.txt", FileMode.Open, FileAccess.Read);
            StreamReader s_streamReader = new StreamReader(symbolfs, Encoding.UTF8);
            //使用StreamReader类来读取文件
            s_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            //从数据流中读取每一行，直到文件的最后一行，并在textBox中显示出内容，其中textBox为文本框，如果不用可以改为别的
            this.symbolTextBox.Text = "";
            string strLine1 = s_streamReader.ReadLine();
            while (strLine1 != null)
            {
                this.symbolTextBox.Text += strLine1 + "\n";
                strLine1 = s_streamReader.ReadLine();
            }
            //关闭此StreamReader对象
            symbolfs.Close();
            s_streamReader.Close();

            //打印error
            FileStream errorfs = new FileStream("error.txt", FileMode.Open, FileAccess.Read);
            StreamReader e_streamReader = new StreamReader(errorfs, Encoding.UTF8);
            //使用StreamReader类来读取文件
            e_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            //从数据流中读取每一行，直到文件的最后一行，并在textBox中显示出内容，其中textBox为文本框，如果不用可以改为别的
            this.errorTextBox.Text = "";
            string strLine2 = e_streamReader.ReadLine();
            while (strLine2 != null)
            {
                this.errorTextBox.Text += strLine2 + "\n";
                strLine2 = e_streamReader.ReadLine();
            }
            //关闭此StreamReader对象
            errorfs.Close();
            e_streamReader.Close();
        }

        public static void getContentToFile(String conpath)//拷贝到test.txt
        {

            FileStream fs = null;
            StreamReader sr = null;
            StreamWriter sw = new StreamWriter(@"test.txt", true);
            try
            {
                String content = String.Empty;
                fs = new FileStream(conpath, FileMode.Open);
                sr = new StreamReader(fs);
                while ((content = sr.ReadLine()) != null)
                {
                    content = content.Trim().ToString();
                    sw.WriteLine(content);
                }
            }
            catch
            {
                Console.WriteLine("读取内容到文件方法错误");
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    sr.Close();
                }
                if (sw != null)
                {
                    sw.Close();
                }
            }

        }

        private void RunButton_Click(object sender, RoutedEventArgs e)//运行按钮
        {
            if (GA.pdErr == 0)
            {
                first++;
                if (first == 1) inter.initial();//解释程序初始化
                if (inter.Flag == 0)
                {
                    inter.interpret();
                }
                else if (inter.Flag == 1)
                {
                    MessageBox.Show("输入成功");
                    string str = this.inputTextBox.Text.ToString();
                    int num = Convert.ToInt32(str);
                    inter.readNum(num);
                    inter.interpret();
                }
                if (inter.Flag == 0)//运行完毕
                {
                    inter.sw.WriteLine("运行完毕");
                    inter.textClose();//关闭文件 
                                      //打印
                    FileStream printfs = new FileStream("print.txt", FileMode.Open, FileAccess.Read);
                    StreamReader p_streamReader = new StreamReader(printfs, Encoding.UTF8);
                    //使用StreamReader类来读取文件
                    p_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    this.runTextBox.Text = "";
                    string strLine = p_streamReader.ReadLine();
                    while (strLine != null)
                    {
                        this.runTextBox.Text += strLine + "\n";
                        strLine = p_streamReader.ReadLine();
                    }
                    //关闭此StreamReader对象
                    printfs.Close();
                    p_streamReader.Close();
                }
                else if (inter.Flag == 1)
                {
                   
                    MessageBox.Show("请在输入框输入数字");
                }
            }
            else
            {
                FileStream printfs = new FileStream("print.txt", FileMode.Open, FileAccess.Read);
                StreamReader p_streamReader = new StreamReader(printfs, Encoding.UTF8);
                //使用StreamReader类来读取文件
                p_streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                this.runTextBox.Text = "";
                string strLine = p_streamReader.ReadLine();
                while (strLine != null)
                {
                    this.runTextBox.Text += strLine + "\n";
                    strLine = p_streamReader.ReadLine();
                }
                //关闭此StreamReader对象
                printfs.Close();
                p_streamReader.Close();
                MessageBox.Show("请修正错误！"); }


        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
