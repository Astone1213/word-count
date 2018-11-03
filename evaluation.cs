using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace evaluation
{
    class Program
    {

        static Dictionary<string, int> one_process(string app_path, string cmd)
        {
            Dictionary<string, int> freq_dict = new Dictionary<string, int>();
            System.Diagnostics.Process exep = new System.Diagnostics.Process();
            exep.StartInfo.Arguments = cmd;
            exep.StartInfo.CreateNoWindow = false;
            exep.StartInfo.RedirectStandardOutput = true;
            exep.StartInfo.UseShellExecute = false;
            exep.StartInfo.FileName = app_path;
            exep.Start();
            StreamReader reader = exep.StandardOutput;
            string line = reader.ReadLine();
            string first_line = line;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();
                if (line == "")
                    break;
                string[] words = line.Split('\t');
                freq_dict[words[0]] = int.Parse(words[1]);
            }
            exep.WaitForExit();
            exep.Close();
            return freq_dict;
        }

        static Dictionary<string, float> one_process_count_char(string app_path, string cmd)
        {
            Dictionary<string, float> freq_dict = new Dictionary<string, float>();
            System.Diagnostics.Process exep = new System.Diagnostics.Process();
            exep.StartInfo.Arguments = cmd;
            exep.StartInfo.CreateNoWindow = false;
            exep.StartInfo.RedirectStandardOutput = true;
            exep.StartInfo.UseShellExecute = false;
            exep.StartInfo.FileName = app_path;
            exep.Start();
            StreamReader reader = exep.StandardOutput;
            string line = reader.ReadLine();
            string first_line = line;
            float sum = 0;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();
                if (line == "")
                    break;
                string[] words = line.Split('\t');
                freq_dict[words[0]] = float.Parse(words[1].Trim('%'));
                sum += freq_dict[words[0]];
            }
            List<string> keys = new List<string>(freq_dict.Keys);
            foreach (string k in keys)
            {
                freq_dict[k] /= sum;
            }
            exep.WaitForExit();
            exep.Close();
            return freq_dict;
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceCounter(ref long count);
        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(ref long count);

        static void Main(string[] args)
        {
            string path = "D:/ASE/word_count/WordFrequency - Reference Implementation/Release/WordFrequencyFSharpFramework.exe";
            string cmd = "-p 3 D:/ASE/word_count/test_file/pride-and-prejudice.txt";
            int times = 9;
            long count = 0;
            long count1 = 0;
            long freq = 0;
            double result = 0;
            QueryPerformanceFrequency(ref freq);
            QueryPerformanceCounter(ref count);
            var ta_result = one_process(path, cmd);
            for (int i = 0; i < times; i ++)
                ta_result = one_process(path, cmd);
            QueryPerformanceCounter(ref count1);
            count = count1 - count;
            result = (double)(count) / (double)freq;
            Console.WriteLine("time TA:" + result.ToString());
            path = "D:/ASE/word_count/word_count/word_count/bin/Release/word_count.exe";
            QueryPerformanceFrequency(ref freq);
            QueryPerformanceCounter(ref count);
            var my_result = one_process(path, cmd);
            for (int i = 0; i < times; i++)
                my_result = one_process(path, cmd);
            QueryPerformanceCounter(ref count1);
            count = count1 - count;
            result = (double)(count) / (double)freq;
            Console.WriteLine("time MY:" + result.ToString());
            List<string> keys = new List<string>();
            string outfile = "D:/ASE/word_count/compare.txt";
            StreamWriter sw = new StreamWriter(outfile);
            foreach (string k in ta_result.Keys.OrderByDescending(o => ta_result[o]))
            {
                var ta_num = ta_result[k];
                float my_num;
                if (my_result.ContainsKey(k))
                {
                    my_num = my_result[k];
                    my_result.Remove(k);
                }
                else
                    my_num = 0;
                sw.WriteLine("{0, 40}\t{1}\t{2}", k, ta_num.ToString(), my_num.ToString());
                if(ta_num!=my_num)
                    Console.WriteLine("{0, 40}\t{1}\t{2}", k, ta_num.ToString(), my_num.ToString());
            }
            foreach(string k in my_result.Keys.OrderByDescending(o => my_result[o]))
            {
                var ta_num = 0;
                var my_num = my_result[k];
                sw.WriteLine("{0, 40}\t{1}\t{2}", k, ta_num.ToString(), my_num.ToString());
                Console.WriteLine("{0, 40}\t{1}\t{2}", k, ta_num.ToString(), my_num.ToString());
            }
            sw.Close();
        }
    }
}
