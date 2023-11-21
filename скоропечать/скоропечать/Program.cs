using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace скоропечать
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Добро пожаловать в тест на скоропечатание!");

            Console.Write("Введите ваше имя: ");
            string playerName = Console.ReadLine();

            TypingTest typingTest = new TypingTest();
            typingTest.StartTest(playerName);

            Console.WriteLine("Тест завершен. Ваш результат сохранен в таблице рекордов.");


            Console.ReadLine();
        }
    }

    class TypingTest
    {
        private static List<UserRecord> leaderboard = LoadLeaderboard();
        private static readonly object leaderboardLock = new object();
        private volatile bool testActive = true;
        private DateTime testEndTime;

        public void StartTest(string playerName)
        {
            string textToType = "Текст — это упорядоченный набор предложений, предназначенный для того, чтобы выразить некий смысл.\nВ лингвистике термин используется в широком значении, включая в себя и устную речь.\nВосприятие текста изучается в рамках лингвистики текста и психолингвистики. ";

            Console.WriteLine($"Начинаем тест, {playerName}! Напечатайте следующий текст:\n\n{textToType}");
            Console.WriteLine("Нажмите Enter, чтобы начать...");

            Console.ReadLine();

            Console.Clear();

            Console.WriteLine($"Тест запущен. Набирайте текст:");

            Console.WriteLine(textToType);

            Stopwatch stopwatch = Stopwatch.StartNew();
            StringBuilder userInput = new StringBuilder();

            Thread timerThread = new Thread(() =>
            {
                Thread.Sleep(60000);
                stopwatch.Stop();

                testEndTime = DateTime.Now;

                EndTest(playerName, textToType, stopwatch.Elapsed, userInput.ToString());
            });
            timerThread.Start();

            int index = 0;
            while (testActive)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                char expectedChar = textToType[index];

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                        testActive = false;
                        break;
                }
                else if (keyInfo.KeyChar == expectedChar)
                {
                    Console.Write(keyInfo.KeyChar);
                    userInput.Append(keyInfo.KeyChar);
                    index++;

                    if (index == textToType.Length)
                    {
                        index = 0;
                    }
                }
                else
                {
                    Console.Write('_');
                }
            }

            timerThread.Join();

            EndTest(playerName, textToType, stopwatch.Elapsed, userInput.ToString());
        }

        private void EndTest(string playerName, string originalText, TimeSpan elapsedTime, string userInput)
        {
            Console.Clear();

            int charactersTyped = userInput.Length;
            int charactersPerMinute = (int)(charactersTyped / elapsedTime.TotalMinutes);
            int charactersPerSecond = (int)(charactersTyped / elapsedTime.TotalSeconds);

            Console.WriteLine($"Тест завершен, {playerName}!");
            Console.WriteLine($"Время: {elapsedTime.TotalMinutes:F2} мин, Набрано символов: {charactersTyped}");
            Console.WriteLine($"Символов в минуту: {charactersPerMinute}, Символов в секунду: {charactersPerSecond}");

            UpdateLeaderboard(new UserRecord(playerName, charactersPerMinute, charactersPerSecond, testEndTime));

            DisplayLeaderboard();
        }

        private static List<UserRecord> LoadLeaderboard()
        {
            try
            {
                if (File.Exists("leaderboard.json"))
                {
                    string json = File.ReadAllText("leaderboard.json");
                    return JsonConvert.DeserializeObject<List<UserRecord>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке таблицы рекордов: {ex.Message}");
            }

            return new List<UserRecord>();
        }

        private static void SaveLeaderboard()
        {
            try
            {
                string json = JsonConvert.SerializeObject(leaderboard, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("leaderboard.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении таблицы рекордов: {ex.Message}");
            }
        }

        private static void UpdateLeaderboard(UserRecord userRecord)
        {
            lock (leaderboardLock)
            {
                leaderboard.Add(userRecord);
                leaderboard = leaderboard.OrderByDescending(r => r.CharactersPerMinute).ToList();
                SaveLeaderboard();
            }
        }

        private static void DisplayLeaderboard()
        {
            lock (leaderboardLock)
            {
                Console.WriteLine("\nТаблица рекордов:");

                foreach (var record in leaderboard)
                {
                    Console.WriteLine($"{record.Name}: {record.CharactersPerMinute} CPM, {record.CharactersPerSecond} CPS");
                }
            }
        }
    }
    class UserRecord
    {
        public string Name { get; set; }
        public int CharactersPerMinute { get; set; }
        public int CharactersPerSecond { get; set; }
        public DateTime EndTime { get; set; }

        public UserRecord(string name, int cpm, int cps, DateTime endTime)
        {
            Name = name;
            CharactersPerMinute = cpm;
            CharactersPerSecond = cps;
            EndTime = endTime;
        }
    }
}