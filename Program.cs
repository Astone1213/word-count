using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace word_count
{
    public class CommandLineArgument
    {
        List<CommandLineArgument> _arguments;

        int _index;

        string _argumentText;

        public CommandLineArgument Next
        {
            get
            {
                if (_index < _arguments.Count - 1)
                {
                    return _arguments[_index + 1];
                }

                return null;
            }
        }
        public CommandLineArgument Previous
        {
            get
            {
                if (_index > 0)
                {
                    return _arguments[_index - 1];
                }

                return null;
            }
        }
        internal CommandLineArgument(List<CommandLineArgument> args, int index, string argument)
        {
            _arguments = args;
            _index = index;
            _argumentText = argument;
        }

        public CommandLineArgument Take()
        {
            return Next;
        }

        public IEnumerable<CommandLineArgument> Take(int count)
        {
            var list = new List<CommandLineArgument>();
            var parent = this;
            for (int i = 0; i < count; i++)
            {
                var next = parent.Next;
                if (next == null)
                    break;

                list.Add(next);

                parent = next;
            }

            return list;
        }

        public static implicit operator string(CommandLineArgument argument)
        {
            return argument._argumentText;
        }

        public override string ToString()
        {
            return _argumentText;
        }
    }

    public class CommandLineArgumentParser
    {

        List<CommandLineArgument> _arguments;
        public static CommandLineArgumentParser Parse(string[] args)
        {
            return new CommandLineArgumentParser(args);
        }

        public CommandLineArgumentParser(string[] args)
        {
            _arguments = new List<CommandLineArgument>();

            for (int i = 0; i < args.Length; i++)
            {
                _arguments.Add(new CommandLineArgument(_arguments, i, args[i]));
            }

        }

        public CommandLineArgument Get(string argumentName)
        {
            return _arguments.FirstOrDefault(p => p == argumentName);
        }

        public bool Has(string argumentName)
        {
            return _arguments.Count(p => p == argumentName) > 0;
        }
    }

    public class modes
    {
        public string path="";
        public int phrase_number=2;
        public bool count_topk = false;
        public int top_number=-1;
        public bool count_char = false;
        public bool count_word = false;
        public bool count_phrase = false;
        public bool directory = false;
        public bool recurrent = false;
        public bool stopword = false;
        public string stopword_path;
        public bool verb_origin = false;
        public string verb_path="";

        public modes(string[] args)
        {
            List<string> returnlist = new List<string>();
            var arguments = CommandLineArgumentParser.Parse(args);
            List<string> paramlist = new List<string>() { "-c", "-f", "-d", "-p", "-n", "-x", "-v" };
            if (arguments.Has("-c"))
            {
                this.count_char = true;
                var arg = arguments.Get("-c");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("Parameter error! ");
                this.path = filename;
            }
            if (arguments.Has("-f"))
            {
                this.count_word = true;
                var arg = arguments.Get("-f");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("Parameter error! ");
                this.path = filename;
            }
            if (arguments.Has("-d"))
            {
                this.count_word = true;
                this.directory = true;
                var arg = arguments.Get("-d");
                string directory = arg.Next;
                if (directory == "-s")
                {
                    this.recurrent = true;
                    directory = arg.Next;
                }
                if (!Directory.Exists(directory))
                    throw new System.ArgumentException("Parameter error! ");
                this.path = directory;
            }
            if (arguments.Has("-p"))
            {
                this.count_phrase = true;
                var arg = arguments.Get("-p");
                int number = int.Parse(arg.Next);
                if (number != 2)
                    throw new System.ArgumentException("Parameter error! ");
                this.phrase_number = number;
            }
            if (arguments.Has("-n"))
            {
                this.count_topk = true;
                var arg = arguments.Get("-n");
                int number = int.Parse(arg.Next);
                if (number < 1)
                    throw new System.ArgumentException("Parameter error! ");
                this.top_number = number;
            }
            if (arguments.Has("-x"))
            {
                this.stopword = true;
                var arg = arguments.Get("-x");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("Parameter error! ");
                this.stopword_path = filename;
            }
            if (arguments.Has("-v"))
            {
                this.verb_origin = true;
                var arg = arguments.Get("-v");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("Parameter error! ");
                this.verb_path = filename;
            }
            //todo: consider multi-mode conflict
            //      sort error to one place
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            //string testfile = "D:/ASE/word_count/test_file/pride-and-prejudice.txt";
            string verbfile = "D:/ASE/word_count/src_file/verbs_origin.txt";
            string prepfile = "D:/ASE/word_count/src_file/prepositions.txt";
            try
            {
                modes mode = new modes(args);
                string[] files;

                if (mode.directory && mode.recurrent)
                    files = Directory.GetFiles(mode.path, "*.*", SearchOption.AllDirectories);
                else if (mode.directory)
                    files = Directory.GetFiles(mode.path);
                else
                    files = new string[] { mode.path };

                foreach(string file in files)
                {
                    process_onefile(mode, file, verbfile, prepfile);
                }

                return 0;
            }
            catch (ArgumentException e) when (e.Message == "Parameter error! ")
            {
                return -1;
            }
        }

        static int process_onefile(modes mode, string file, string verbfile, string prepfile)
        {
            int num = mode.top_number;
            if (mode.count_char)
            {
                print_dictionary<char>(count_char(mode, file), num);
            }
            if (mode.count_word)
            {
                print_dictionary<string>(count_word(mode, file), num);
            }
            if (mode.count_phrase)
            {
                print_dictionary<string>(count_phrase(mode, file, verbfile, prepfile), num);
            }
            return 0;
        }

        static Dictionary<char, float> count_char(modes mode, string infile)
        {
            Dictionary<char, float> char_freq = new Dictionary<char, float>();
            for(char c='a';c<='z';c++)
            {
                char_freq[c] = 0;
            }
            for (char c = 'Z'; c >= 'A'; c--)
            {
                char_freq[c] = 0;
            }
            StreamReader sr = new StreamReader(infile);
            int read_size = 500;
            char[] buffer = new char[read_size];
            while (!sr.EndOfStream)
            {
                sr.Read(buffer, 0, read_size);
                //for (int i = 0; i < 1000; i++)
                //    buffer[i] = char(sr.Read());
                foreach (char c in buffer)
                {
                    if (!char_freq.ContainsKey(c))
                        continue;
                    char_freq[c]++;
                }
            }
            sr.Close();
            num2freq<char>(char_freq);
            return char_freq;
        }

        static Dictionary<string, float> count_word(modes mode, string infile)
        {
            Dictionary<string, float> word_freq = new Dictionary<string, float>();
            StreamReader sr = new StreamReader(infile);
            while (!sr.EndOfStream)
            {
                //TODO: line overflow
                string line = sr.ReadLine().Trim().ToLower();
                string[] words = line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    //TODO: optimize pipeline
                    if (!(word[0] > 'a' && word[0] < 'z') && !(word[0] > 'A' && word[0] < 'Z'))
                        continue;
                    if (!word_freq.ContainsKey(word))
                        word_freq[word] = 0;
                    word_freq[word]++;
                }
            }
            sr.Close();
            num2freq<string>(word_freq);
            if (mode.stopword)
                word_freq = remove_stopwords(word_freq, mode.stopword_path);
            return word_freq;
        }

        static Dictionary<string, float> remove_stopwords(Dictionary<string, float> dict, string stopword_path)
        {
            string[] stopwords = File.ReadAllLines(stopword_path);
            foreach(string word in stopwords)
            {
                dict.Remove(word);
            }
            return dict;
        }

        static Dictionary<string, float> count_phrase(modes mode, string infile, string verb_file, string prep_file)
        {
            string[] verbs = File.ReadAllLines(verb_file);
            string[] preps = File.ReadAllLines(prep_file);
            Dictionary<string, float> phrase_freq = new Dictionary<string, float>();
            StreamReader sr = new StreamReader(infile);
            string line_last_word = "";
            while (!sr.EndOfStream)
            {
                //TODO: overflow
                string line = sr.ReadLine().Trim().ToLower();
                string[] words = line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Count() < 1)
                    continue;
                if (verbs.Contains(line_last_word))
                    if (preps.Contains(words[0]))
                    {
                        string key = string.Join(" ", line_last_word, words[0]);
                        if (!phrase_freq.ContainsKey(key))
                            phrase_freq[key] = 0;
                        phrase_freq[key] += 1;
                    }
                int last_index = words.Count() - 1;
                for (int i=0 ; i< last_index; i++)
                {
                    string first_word = words[i];
                    if (!verbs.Contains(first_word))
                        continue;
                    string second_word = words[i + 1];
                    if (!preps.Contains(second_word))
                        continue;
                    string key = string.Join(" ", first_word, second_word);
                    if (!phrase_freq.ContainsKey(key))
                        phrase_freq[key] = 0;
                    phrase_freq[key] += 1;
                    i++;
                }
                line_last_word = words[last_index];
            }
            sr.Close();
            num2freq<string>(phrase_freq);
            return phrase_freq;
        }

        static Dictionary<T, float> num2freq<T>(Dictionary<T, float> dict)
        {
            float sum = 0;
            foreach (T phrase in dict.Keys)
            {
                sum += dict[phrase];
            }
            List<T> keys = new List<T>(dict.Keys);
            foreach (T phrase in keys)
            {
                dict[phrase] /= sum;
            }
            return dict;
        }

        static void print_dictionary<T>(Dictionary<T, float> dict, int num)
        {
            int cnt = 0;
            foreach(T c in dict.Keys.OrderBy(o => o).OrderByDescending(o=>dict[o]))
            {
                Console.Write(c);
                Console.Write('\t' + dict[c].ToString() + '\n');
                cnt++;
                if (cnt == num)
                    break;
            }
            return;
        }

        static void process_verb_file()
        {
            string infile = "D:/ASE/word count/verbs.txt";
            string outfile = "D:/ASE/word count/verbs_origin.txt";
            StreamReader sr = new StreamReader(infile);
            StreamWriter sw = new StreamWriter(outfile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string verb = line.Split(new string[] { "->" }, StringSplitOptions.None)[0].Trim();
                sw.WriteLine(verb);
            }
            sr.Close();
            sw.Close();
        }


    }
}
