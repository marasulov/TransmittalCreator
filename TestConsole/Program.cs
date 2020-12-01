using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace TestConsole
{
    class Program
    {
        interface IPrinter
        {
            void Print(string text);
        }
        class ConsolePrinter : IPrinter
        {
            public void Print(string text)
            {
                Console.WriteLine(text);
            }
        }
        class Report
        {
            public string Text { get; set; }
            public void GoToFirstPage()
            {
                Console.WriteLine("Переход к первой странице");
            }
            public void GoToLastPage()
            {
                Console.WriteLine("Переход к последней странице");
            }
            public void GoToPage(int pageNumber)
            {
                Console.WriteLine("Переход к странице {0}", pageNumber);
            }
            public void Print(IPrinter printer)
            {
                printer.Print(this.Text);
            }
        }

        static void Main(string[] args)
        {




            File.Delete(@"C:\Users\yusufzhon.marasulov\Desktop\ВОР\new\UZLE-59-030-OPN-SCH-080103-RU-A1.docx");

            MobileStore store = new MobileStore(
                new ConsolePhoneReader(), new GeneralPhoneBinder(),
                new GeneralPhoneValidator(), new TextPhoneSaver());
            store.Process();

            IPrinter printer = new ConsolePrinter();
            Report report = new Report();
            report.Text = "Hello Wolrd";
            report.Print(printer);

            Console.Read();

            string filename = @"C:\Users\yusufzhon.marasulov\Desktop\1.txt";
            List<string> text = File.ReadAllLines(filename).ToList();
            
            List<MatchCollection> matchCollections= new List<MatchCollection>();


            string str3 = "ISO_expand_A1_(841.00_x_594.00_MM)";
            string str = "UserDefinedMetric (2378.00 x 841.00мм)";
            double width = 841.00;
            double height = 594.00;
            string pat = @"\d{1,}?\.\d{2}";

            foreach (var line in text)
            {
                Regex pattern = new Regex(pat, RegexOptions.Compiled |
                                               RegexOptions.Singleline);
                //string str2 = Regex.Split(str, pattern);
                if (pattern.IsMatch(line))
                {
                    
                    MatchCollection str2 = pattern.Matches(line, 0);
                    
                    string strWidth = str2[0].ToString();
                    string strheight = str2[1].ToString();
                    double strWidthD = Convert.ToDouble(strWidth, System.Globalization.CultureInfo.InvariantCulture);
                    double strheightD = Convert.ToDouble(strheight, System.Globalization.CultureInfo.InvariantCulture);

                    Console.WriteLine(strWidthD);

                    //double strheight = Convert.ToDouble(str2[1]);


                    if (strWidthD == width & strheightD == height)
                    {
                        Console.WriteLine("{0} ширина {1}-{2}  высота {3}-{4}", line, strWidthD, width, strheightD, height);
                        return;
                    }
                      
                    matchCollections.Add(str2);
                }
            }
            string list = GetWithIn(str);
            Console.ReadLine();
        }


        public static double ConvertToDouble(string Value)
        {
            if (Value == null)
            {
                return 0;
            }
            else
            {
                double OutVal;
                double.TryParse(Value, out OutVal);

                if (double.IsNaN(OutVal) || double.IsInfinity(OutVal))
                {
                    return 0;
                }
                return OutVal;
            }
        }

        public static string GetWithIn(string str)
        {
            string rez = "";

            Regex pattern =
                new Regex(@"\d{1,}?\.[00]{1,2}",
                    RegexOptions.Compiled |
                    RegexOptions.Singleline);

            foreach (Match m in pattern.Matches(str))
                if (m.Success)
                {
                    rez = m.Groups["val"].Value;

                }

            //меж скобок 
            return rez;
        }
    }




    class Account
    {
        int _sum; // Переменная для хранения суммы

        public Account(int sum)
        {
            _sum = sum;
        }

        public int CurrentSum
        {
            get { return _sum; }
        }

        public void Put(int sum)
        {
            _sum += sum;
            Notify?.Invoke($"На счет поступило: {sum}");
        }



        // Объявляем делегат
        public delegate void AccountStateHandler(string message);
        public event AccountStateHandler Notify;

        // Создаем переменную делегата
        AccountStateHandler _del;

        // Регистрируем делегат
        public void RegisterHandler(AccountStateHandler del)
        {
            _del += del; // добавляем делегат
        }

        // Отмена регистрации делегата
        public void UnregisterHandler(AccountStateHandler del)
        {
            _del -= del; // удаляем делегат
        }
        public void Withdraw(int sum)
        {
            if (sum <= _sum)
            {
                _sum -= sum;
                Notify?.Invoke($"Сумма {_sum} снята со счета");
                //if (_del != null)
                //    _del($"Сумма {sum} снята со счета");
            }
            else
            {
              
                    Notify?.Invoke($"Сумма {_sum} снята со счета Недостаточно денег на счете");
            }
        }
    }
    class Phone
    {
        public string Model { get; set; }
        public int Price { get; set; }
    }
    class MobileStore
    {
        List<Phone> phones = new List<Phone>();
        public IPhoneReader Reader { get; set; }
        public IPhoneBinder Binder { get; set; }
        public IPhoneValidator Validator { get; set; }
        public IPhoneSaver Saver { get; set; }
        public MobileStore(IPhoneReader reader, IPhoneBinder binder, IPhoneValidator validator, IPhoneSaver saver)
        {
            this.Reader = reader;
            this.Binder = binder;
            this.Validator = validator;
            this.Saver = saver;
        }
        public void Process()
        {
            string[] data = Reader.GetInputData();
            Phone phone = Binder.CreatePhone(data);
            if (Validator.IsValid(phone))
            {
                phones.Add(phone);
                Saver.Save(phone, "store.txt");
                Console.WriteLine("Данные успешно обработаны");
            }
            else
            {
                Console.WriteLine("Некорректные данные");
            }
        }
    }
    interface IPhoneReader
    {
        string[] GetInputData();
    }
    class ConsolePhoneReader : IPhoneReader
    {
        public string[] GetInputData()
        {
            Console.WriteLine("Введите модель:");
            string model = Console.ReadLine();
            Console.WriteLine("Введите цену:");
            string price = Console.ReadLine();
            return new string[] { model, price };
        }
    }

    interface IPhoneBinder
    {
        Phone CreatePhone(string[] data);
    }
    class GeneralPhoneBinder : IPhoneBinder
    {
        public Phone CreatePhone(string[] data)
        {
            if (data.Length >= 2)
            {
                int price = 0;
                if (Int32.TryParse(data[1], out price))
                {
                    return new Phone { Model = data[0], Price = price };
                }
                else
                {
                    throw new Exception("Ошибка привязчика модели Phone. Некорректные данные для свойства Price");
                }
            }
            else
            {
                throw new Exception("Ошибка привязчика модели Phone. Недостаточно данных для создания модели");
            }
        }
    }
    interface IPhoneValidator
    {
        bool IsValid(Phone phone);
    }
    class GeneralPhoneValidator : IPhoneValidator
    {
        public bool IsValid(Phone phone)
        {
            if (String.IsNullOrEmpty(phone.Model) || phone.Price <= 0)
                return false;
            return true;
        }
    }
    interface IPhoneSaver
    {
        void Save(Phone phone, string fileName);
    }
    class TextPhoneSaver : IPhoneSaver
    {
        public void Save(Phone phone, string fileName)
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName, true))
            {
                writer.WriteLine(phone.Model);
                writer.WriteLine(phone.Price);
            }
        }
    }



    public class Params
    {
        public string Pc3 { get; set; }
        public string Pmp { get; set; }


    }
}
