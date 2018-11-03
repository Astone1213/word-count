using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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

                throw new System.ArgumentException("Need more parameters for " + _arguments[_index]._argumentText);
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
        public int length;
        List<CommandLineArgument> _arguments;
        public static CommandLineArgumentParser Parse(string[] args)
        {
            return new CommandLineArgumentParser(args);
        }

        public CommandLineArgumentParser(string[] args)
        {
            _arguments = new List<CommandLineArgument>();
            length = args.Length;
            for (int i = 0; i < length; i++)
            {
                _arguments.Add(new CommandLineArgument(_arguments, i, args[i]));
            }

        }

        public CommandLineArgument Get_by_index(int index)
        {
            return _arguments[index];
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
        public bool verb_prep_phrase = false;
        public string prep_path = "";

        public modes(string[] args)
        {
            List<string> returnlist = new List<string>();
            var arguments = CommandLineArgumentParser.Parse(args);
            List<string> paramlist = new List<string>() { "-c", "-f", "-q", "-p"};
            int num = 0;
            foreach(string param in paramlist)
            {
                if (arguments.Has(param))
                    num++;
            }
            if(num!=1)
                throw new System.ArgumentException("Parameter conflict: use only one of -c, -f, -q and -p");
            if (arguments.Has("-c"))
            {
                this.count_char = true;
            }
            else if (arguments.Has("-f"))
            {
                this.count_word = true;
            }
            else if (arguments.Has("-p"))
            {
                this.count_phrase = true;
                var arg = arguments.Get("-p");
                int number = int.Parse(arg.Next);
                if (!(number > 0))
                    throw new System.ArgumentException("Phrase length error! Must be larger than zero.");
                this.phrase_number = number;
            }
            else if (arguments.Has("-q"))
            {
                this.verb_prep_phrase = true;
                if(!arguments.Has("-v"))
                    throw new System.ArgumentException("-q must be used with -v!");
                var arg = arguments.Get("-q");
                string path = arg.Next;
                if (!File.Exists(path))
                    throw new System.ArgumentException("File " + path + " not exists!");
                this.prep_path = path;
            }
            if (arguments.Has("-d"))
            {
                this.directory = true;
                var arg = arguments.Get("-d");
                string directory = arg.Next;
                if (directory == "-s")
                {
                    this.recurrent = true;
                    directory = arg.Next;
                }
                if (!Directory.Exists(directory))
                    throw new System.ArgumentException(("Directory " + directory +" not exists!"));
                this.path = directory;
            }
            else
            {
                int path_index = arguments.length - 1;
                string path = arguments.Get_by_index(path_index);
                if (!File.Exists(path))
                    throw new System.ArgumentException("File " + path + " not exists!");
                this.path = path;
            }
            if (arguments.Has("-n"))
            {
                this.count_topk = true;
                var arg = arguments.Get("-n");
                int number = int.Parse(arg.Next);
                if (number < 1)
                    throw new System.ArgumentException("You must assign a number which is larger than zero");
                this.top_number = number;
            }
            if (arguments.Has("-x"))
            {
                this.stopword = true;
                var arg = arguments.Get("-x");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("File " + path + " not exists!");
                this.stopword_path = filename;
            }
            if (arguments.Has("-v"))
            {
                this.verb_origin = true;
                var arg = arguments.Get("-v");
                string filename = arg.Next;
                if (!File.Exists(filename))
                    throw new System.ArgumentException("File " + path + " not exists!");
                this.verb_path = filename;
            }
            //todo: consider multi-mode conflict
            //      sort error to one place
        }
    }

    class VocabTree
    {
        public char name;
        public string originverb;
        Dictionary<char, VocabTree> childs;

        public VocabTree(char name)
        {
            this.name = name;
            this.originverb = null;
            this.childs = new Dictionary<char, VocabTree>();
        }

        public bool Contains(string word)
        {
            search_word(ref word);
            if (word.Count() == 0)
                return true;
            return false;
        }

        public string verb_map(string word)
        {
            VocabTree tree = search_word(ref word);
            if (word.Count() != 0)
                return null;
            return tree.originverb;
        }

        VocabTree search_word(ref string word)
        {
            if (word.Count() == 0)
                return this;
            char c = word[0];
            if (this.childs.ContainsKey(c))
            {
                //Console.WriteLine(word);
                word = word.Substring(1);
                //Console.WriteLine(word);
                return childs[c].search_word(ref word);
            }
            return this;
        }

        public int add_word(string word)
        {
            VocabTree tree = search_word(ref word);
            if (word.Count() == 0)
                return 0;
            char c = word[0];
            tree.childs[c] = new VocabTree(c);
            word = word.Substring(1);
            add_word(word);
            return 1;
        }

        public int add_word(string word, string originword)
        {
            VocabTree tree = search_word(ref word);
            if (word.Count() == 0)
            {
                this.originverb = originword;
                return 0;
            }
            char c = word[0];
            tree.childs[c] = new VocabTree(c);
            word = word.Substring(1);
            tree.childs[c].add_word(word, originword);
            return 1;
        }


    }

    class Program
    {
        public static char[] SPACES = new char[] {' ', '\t', '\r', '\n' };
        public static char[] SPLITOR;
        public static char[] PHRASE_SPLITOR;

        static int Main(string[] args)
        {
            //string testfile = "D:/ASE/word_count/test_file/pride-and-prejudice.txt";
            //string verbfile = "D:/ASE/word_count/src_file/verbs_all.txt";
            //string prepfile = "D:/ASE/word_count/src_file/prepositions.txt";

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

                foreach (string file in files)
                {
                    process_onefile(mode, file);
                }

                 return 0;
                }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
}

        static void process_onefile(modes mode, string file)
        {
            Console.WriteLine("Processing file " + file);
            int num = mode.top_number;
            if (mode.count_char)
            {
                print_dictionary<char>(count_char(mode, file), num);
                return;
            }
            find_splitor(file);
            if (mode.count_word)
            {
                print_dictionary<string>(count_word(mode, file), num);
            }
            else if (mode.count_phrase)
            {
                print_dictionary<string>(count_phrase(mode, file), num);
            }
            else if (mode.verb_prep_phrase)
            {
                print_dictionary<string>(count_verbprep_phrase(mode, file), num);
            }
            return;
        }

        static void find_splitor(string infile)
        {
            HashSet<char> splitors = new HashSet<char>();
            HashSet<char> phrase_splitors = new HashSet<char>();
            StreamReader sr = new StreamReader(infile);
            int read_size = 1000;
            char[] buffer = new char[read_size];
            while (!sr.EndOfStream)
            {
                sr.Read(buffer, 0, read_size);
                //for (int i = 0; i < 1000; i++)
                //    buffer[i] = char(sr.Read());
                foreach (char c in buffer)
                {
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                        continue;
                    if (splitors.Contains(c))
                        continue;
                    splitors.Add(c);
                    if (!SPACES.Contains(c))
                        phrase_splitors.Add(c);
                }
                SPLITOR = splitors.ToArray();
                PHRASE_SPLITOR = phrase_splitors.ToArray();
            }
        }

        static Dictionary<char, int> count_char(modes mode, string infile)
        {
            Dictionary<char, int> char_freq = new Dictionary<char, int>();
            for (char c='a';c<='z';c++)
            {
                char_freq[c] = 0;
            }
            for (char c = 'A'; c <= 'Z'; c++)
            {
                char_freq[c] = 0;
            }
            StreamReader sr = new StreamReader(infile);
            int read_size = 10000;
            char[] buffer = new char[read_size];
            while (!sr.EndOfStream)
            {
                int num = sr.Read(buffer, 0, read_size);
                //for (int i = 0; i < 1000; i++)
                //    buffer[i] = char(sr.Read());
                for(int i=0; i<num; i++)
                {
                    char c = buffer[i];
                    if (!char_freq.ContainsKey(c))
                        continue;
                    char_freq[c]++;
                }
            }
            for(char c='A'; c<='Z'; c++)
            {
                char tmp = c.ToString().ToLower()[0];
                char_freq[tmp] += char_freq[c];
                char_freq.Remove(c);
            }
            sr.Close();
            //num2freq<char>(char_freq);
            return char_freq;
        }

        static Dictionary<string, int> count_word(modes mode, string infile)
        {
            Dictionary<string, int> word_freq = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(infile);
            while (!sr.EndOfStream)
            {
                //TODO: line overflow
                string line = sr.ReadLine().Trim().ToLower();
                string[] words = line.Split(SPLITOR, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    //TODO: optimize pipeline (1)judge a-z (2)contains (3)verb origin
                    char c = word[0];
                    if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z'))
                        continue;
                    if (!word_freq.ContainsKey(word))
                        word_freq[word] = 0;
                    word_freq[word]++;
                }
            }
            sr.Close();
            //num2freq<string>(word_freq);
            if (mode.stopword)
                word_freq = remove_stopwords(word_freq, mode.stopword_path);
            //if (mode.verb_origin)
            //    word_freq = verb2origin(word_freq, mode.verb_path, 0);
            return word_freq;
        }

        static Dictionary<string, int> remove_stopwords(Dictionary<string, int> dict, string stopword_path)
        {
            string[] stopwords = File.ReadAllLines(stopword_path);
            foreach(string word in stopwords)
            {
                dict.Remove(word);
            }
            return dict;
        }

        static Dictionary<string, string> verb2origin_map(string verb_path)
        {
            Dictionary<string, string> verb_origin = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(verb_path);
            while (!sr.EndOfStream)
            {
                string[] words = sr.ReadLine().Split(new string[] { " -> " }, StringSplitOptions.None);
                string origin = words[0];
                string[] verbs = words[1].Split(',');
                foreach (string verb in verbs)
                {
                    verb_origin[verb] = origin;
                }
            }
            sr.Close();
            return verb_origin;
        }

        static Dictionary<string, int> verb2origin(Dictionary<string, int> dict, string verb_path, int phrase_mode)
        {
            Dictionary<string, string> verb_origin = verb2origin_map(verb_path);

            //TODO: search verb_origin or dict, find complexity of diction[index]
            List<string> keys = new List<string>(dict.Keys);

            if (phrase_mode == 0)
            {
                foreach (string verb in keys)
                {
                    if (verb_origin.ContainsKey(verb))
                    {
                        string origin = verb_origin[verb];
                        if (!dict.ContainsKey(origin))
                            dict[origin] = dict[verb];
                        else
                            dict[origin] += dict[verb];
                        dict.Remove(verb);
                    }
                }
            }
            else if (phrase_mode == 1)
            {
                foreach (string key in keys)
                {
                    string[] words = key.Split(SPACES);  //words[0] verb, words[1] preposition
                    if (verb_origin.ContainsKey(words[0]))
                    {
                        string origin = verb_origin[words[0]];
                        string newkey = string.Join(" ", origin, words[1]);
                        if (!dict.ContainsKey(newkey))
                            dict[newkey] = dict[key];
                        else
                            dict[newkey] += dict[key];
                        dict.Remove(key);
                    }
                }
            }
            else if(phrase_mode==2)
            {
                foreach(string key in keys)
                {
                    string[] words = key.Split(SPACES);
                    int max_index = words.Count();
                    for(int i=0;i<max_index;i++)
                    {
                        string word = words[i];
                        if (!verb_origin.ContainsKey(word))
                            continue;
                        string origin = verb_origin[word];
                        words[i] = origin;
                    }
                    string newkey = string.Join(" ", words);
                    if(key!=newkey)
                    {
                        if (!dict.ContainsKey(newkey))
                            dict[newkey] = dict[key];
                        else
                            dict[newkey] += dict[key];
                        dict.Remove(key);
                    }
                }
            }

            return dict;
        }

        static Dictionary<string, int> count_phrase(modes mode, string infile)
        {
            Dictionary<string, int> phrase_freq = new Dictionary<string, int>();
            Dictionary<string, string> verb_map = new Dictionary<string, string>();
            if(mode.verb_origin)
                verb_map = gen_verb_map(mode.verb_path);
            HashSet<string> stopwords = new HashSet<string>();
            if (mode.stopword)
                stopwords = gen_wordlist(mode.stopword_path);
            int len = mode.phrase_number;
            StreamReader sr = new StreamReader(infile);
            string line_last_words = "";
            int buffer_size = 1000 * len;
            while (!sr.EndOfStream)
            {
                string buffer = line_last_words;
                while (!sr.EndOfStream && buffer.Count()<buffer_size)
                {
                    string line = sr.ReadLine().Trim().ToLower();
                    buffer = string.Join(" ", buffer, line);
                }
                string[] sentences = buffer.Split(PHRASE_SPLITOR, StringSplitOptions.None);
                //string[] words = new string[] { };
                List<string> words = new List<string>();
                foreach (string sentence in sentences)
                {
                    words = new List<string>(sentence.Split(SPACES, StringSplitOptions.RemoveEmptyEntries));
                    int last_index = words.Count();
                    for (int i=0; i<last_index;)
                    {
                        string word = words[i];
                        char c = word[0];
                       
                        if (c >= '0' && c <= '9')
                        {
                            words.Remove(word);
                            last_index -= 1;
                            continue;
                        }
                        if (mode.stopword)
                        {
                            if (stopwords.Contains(word))
                            {
                                words.Remove(word);
                                last_index -= 1;
                                continue;
                            }
                        }
                        if (mode.verb_origin)
                        {
                            if (verb_map.ContainsKey(word))
                                words[i] = verb_map[word];
                        }
                        i++;
                    }
                    int tmp = words.Count();
                    if (tmp < len)
                    {
                        continue;
                    }
                    last_index = tmp - len + 1;
                    for (int i = 0; i < last_index; i++)
                    {
                        string phrase = "";
                        int max = i + len;
                        for (int j = i; j < max; j++)
                            phrase = string.Join(" ", phrase, words[j]).Trim();
                        if (!phrase_freq.ContainsKey(phrase))
                            phrase_freq[phrase] = 0;
                        phrase_freq[phrase] += 1;
                        //if (phrase.Contains("sight"))
                        //    Console.WriteLine();
                    }
                    
                }
                line_last_words = "";
                if (words.Count() < len)
                {
                    line_last_words = sentences.Last();
                }
                else
                {
                    words.RemoveRange(0, words.Count() - len + 1);
                    //words = words.Skip(words.Count() - len).ToArray();
                    line_last_words = string.Join(" ", words);
                }
            }
            sr.Close();
            //num2freq<string>(phrase_freq);
            //if (mode.verb_origin)
            //    phrase_freq = verb2origin(phrase_freq, mode.verb_path, 2);
            return phrase_freq;
        }

        static Dictionary<string, int> count_verbprep_phrase(modes mode, string infile)
        {
            string verb_file = mode.verb_path;
            Dictionary<string, string> verb_map = gen_verb_map(verb_file);
            HashSet<string> stopwords = new HashSet<string>();
            if (mode.stopword)
                stopwords = gen_wordlist(mode.stopword_path);
            string prep_file = mode.prep_path;
            HashSet<string> preps = gen_wordlist(prep_file);
            Dictionary<string, int> phrase_freq = new Dictionary<string, int>();
            StreamReader sr = new StreamReader(infile);
            string line_last_word = "";
            int buffer_size = 1000;
            while (!sr.EndOfStream)
            {
                string buffer = line_last_word;
                int cnt = 0;
                while (!sr.EndOfStream && buffer.Count() < buffer_size)
                {
                    cnt++;
                    string line = sr.ReadLine().Trim().ToLower();
                    buffer = string.Join(" ", buffer, line);
                }
                string[] sentences = buffer.Split(PHRASE_SPLITOR, StringSplitOptions.None);


                List<string> words = new List<string>();
                int last_index;
                foreach (string sentence in sentences)
                {
                    words = new List<string>(sentence.Split(SPACES, StringSplitOptions.RemoveEmptyEntries));
                    last_index = words.Count(); 
                    for (int i = 0; i < last_index;)
                    {
                        string word = words[i];
                        char c = word[0];
                        if (c >= '0' && c <= '9')
                        {
                            words.Remove(word);
                            last_index -= 1;
                            continue;
                        }
                        if (mode.stopword)
                        {
                            if (stopwords.Contains(word))
                            {
                                words.Remove(word);
                                last_index -= 1;
                                continue;
                            }
                        }
                        if (mode.verb_origin)
                        {
                            if (verb_map.ContainsKey(word))
                                words[i] = verb_map[word];
                        }
                        i++;
                    }
                    int tmp = words.Count();
                    if (tmp < 2)
                    {
                        continue;
                    }
                    last_index = tmp - 1;
                    for (int i = 0; i < last_index; i++)
                    {
                        string first_word = words[i];
                        string second_word = words[i + 1];
                        if (!preps.Contains(second_word))
                            continue;
                        if (!verb_map.ContainsKey(first_word))
                            continue;

                        if (mode.verb_origin)
                            first_word = verb_map[first_word];
                        //Console.WriteLine(first_word);
                        string key = string.Join(" ", first_word, second_word);
                        if (!phrase_freq.ContainsKey(key))
                            phrase_freq[key] = 0;
                        phrase_freq[key] += 1;
                        //if (key.Contains("sight with"))
                        //    Console.WriteLine();
                        //i++;
                    }
                }
                last_index = words.Count() - 1;
                if (last_index >= 0)
                    line_last_word = words[last_index];
                else
                    line_last_word = "";
            }
            sr.Close();
            //num2freq<string>(phrase_freq);
            //if (mode.verb_origin)
            //    phrase_freq = verb2origin(phrase_freq, mode.verb_path, 1);
            return phrase_freq;
        }

        //static Dictionary<T, int> num2freq<T>(Dictionary<T, int> dict)
        //{
        //    int sum = 0;
        //    foreach (T phrase in dict.Keys)
        //    {
        //        sum += dict[phrase];
        //    }
        //    List<T> keys = new List<T>(dict.Keys);
        //    foreach (T phrase in keys)
        //    {
        //        dict[phrase] /= sum;
        //    }
        //    return dict;
        //}

        static void print_dictionary<T>(Dictionary<T, int> dict, int num)
        {
            int cnt = 0;
            List<T> keys = new List<T>(dict.Keys);
            keys.Sort();
            keys = new List<T>(keys.OrderByDescending(o => dict[o]));
            foreach (T c in keys)
            {
                string new_c = c.ToString();
                Console.WriteLine("{0, 40}\t{1}", new_c, dict[c].ToString());
                //Console.Write('\t' + dict[c].ToString() + '\n');
                cnt++;
                if (cnt == num)
                    break;
            }
            return;
        }

        static VocabTree gen_verb_tree(string infile)
        {
            VocabTree tree = new VocabTree('\0');
            StreamReader sr = new StreamReader(infile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] words = line.Split(new string[] { " -> " }, StringSplitOptions.None);
                string verb_origin = words[0];
                string[] verbs = words[1].Split(',');
                //sw.WriteLine(verb_origin);
                tree.add_word(verb_origin, verb_origin);
                foreach (string verb in verbs)
                {
                    tree.add_word(verb, verb_origin);
                }
            }
            sr.Close();
            return tree;
        }

        static Dictionary<string, string> gen_verb_map(string infile)
        {
            Dictionary<string, string> verb_map = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(infile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] words = line.Split(new string[] { " -> " }, StringSplitOptions.None);
                string verb_origin = words[0].Trim();
                string[] verbs = words[1].Split(',');
                verb_map[verb_origin] = verb_origin;
                foreach (string verb in verbs)
                {
                    verb_map[verb.Trim()] = verb_origin;
                }
            }
            sr.Close();
            return verb_map;
        }

        static Dictionary<string, string> gen_prep_map(string infile)
        {
            Dictionary<string, string> prep_map = new Dictionary<string, string>();
            string text = File.ReadAllText(infile);
            string[] words = text.Split(SPACES);
            foreach (string word in words)
                prep_map[word] = word;
            return prep_map;
        }

        static HashSet<string> gen_wordlist(string infile)
        {
            HashSet<string> wordlist = new HashSet<string>();
            string text = File.ReadAllText(infile);
            string[] words = text.Split(SPACES);
            foreach (string word in words)
                wordlist.Add(word);
            return wordlist;
        }

        static VocabTree gen_vocab_tree(string infile)
        {
            VocabTree tree = new VocabTree('\0');
            string text = File.ReadAllText(infile);
            string[] words = text.Split(SPACES);
            foreach (string word in words)
                tree.add_word(word);
            return tree;
        }

        static List<string> process_verb_file(string infile)
        {
            ////TODO: possible speed slower because of file IO
            //string base_dir = System.Environment.CurrentDirectory;
            //string outfile = Path.Combine(base_dir, "verblist.txt");
            //StreamWriter sw = new StreamWriter(outfile);
            List<string> verblist = new List<string>();
            StreamReader sr = new StreamReader(infile);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] words = line.Split(new string[] { " -> " }, StringSplitOptions.None);
                string verb_origin = words[0];
                string[] verbs = words[1].Split(',');
                //sw.WriteLine(verb_origin);
                verblist.Add(verb_origin);
                foreach(string verb in verbs)
                {
                    verblist.Add(verb);
                }
            }
            sr.Close();
            //sw.Close();
            return verblist;
        }


    }
}
