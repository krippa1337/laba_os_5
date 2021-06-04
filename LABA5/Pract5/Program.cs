using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pract5
{
    class Program
    {
        static readonly int nMemory = 16000;
        static readonly int sizeInt = sizeof(int);
        static int[] memory;
        static SortedList<int, int> freeMemory;
        static SortedList<int, int> occupiedMemory;
        static Queue<TaskInfo> queueTasksToWait;
        static Queue<TaskInfo> queueTasksToFree;
        static List<Thread> listTaskDoing;
        static bool isExit;
        static void PrintMemory()
        {
            Console.WriteLine("");
            Console.WriteLine("Память перераспределена");
            Console.WriteLine("Всего памяти " + memory.Length * sizeInt + " байт");
            if (freeMemory.Count > 0)
            {
                Console.Write("Свободные области памяти: ");
                for (int i = 0; i < freeMemory.Count; i++)
                {
                    Console.WriteLine( freeMemory.Keys[i] * sizeInt + " - " + (freeMemory.Values[i] * sizeInt)) ;
                }
            }
            else
                Console.WriteLine("Нет свободных областей памяти");

            if (occupiedMemory.Count > 0)
            {
                Console.Write("Занятые области памяти: ");
                for (int i = 0; i < occupiedMemory.Count; i++)
                {
                    Console.WriteLine( occupiedMemory.Keys[i] * sizeInt + " - " + ((occupiedMemory.Values[i] + 1) * sizeInt) );
                }
            }
            else
                Console.WriteLine("Нет занятых областей памяти");
        }

        static TaskInfo LookingForFree(string file, int n)//Поиск свободной области памяти
                                                          //n-объем необходимой памяти
    {
      if (freeMemory.Count > 0)
            {
                for (int i = 0; i < freeMemory.Count; i++)
                {
                    if (freeMemory.Values[i] - freeMemory.Keys[i] > n)
                    {
                        TaskInfo something = new TaskInfo(file, i, n);
                        SetMemory(something);
                        return something;
                    }
                }
            }
            return null;
        }
        static void UniteMemory(SortedList<int, int> pairs)//Объединение нескольких областей сежных памяти в одну область
    {
            int tempBegin = -1, tempEnd = -1;
            for (int i = 0; i < pairs.Count - 1; i++)
            {
                for (int j = i + 1; j < pairs.Count; j++)
                {
                    if (pairs.Values[i] == (pairs.Keys[j] - 1))
                    {
                        tempBegin = pairs.Keys[i];
                        tempEnd = pairs.Values[j];
                        pairs.RemoveAt(j);
                        pairs.RemoveAt(i);
                        pairs.Add(tempBegin, tempEnd);
                        if (i != 0)
                        {
                            i--;
                            j = i;
                        }
                        else
                        {
                            j--;
                        }
                    }
                }
            }
        }

        static TaskInfo SetMemory(TaskInfo info)//Выделение памяти под задачу
    {
            int n;
            using (StreamReader sr = new StreamReader(info.File))
            {
                n = int.Parse(sr.ReadLine());

                info.MemBegin = freeMemory.Keys[info.IndexFreeMemory];
                info.MemEnd = freeMemory.Keys[info.IndexFreeMemory] + n;

                occupiedMemory.Add(info.MemBegin, info.MemEnd - 1);

                int maxMemory = freeMemory.Values[info.IndexFreeMemory];
                freeMemory.RemoveAt(info.IndexFreeMemory);
                if (info.MemEnd < maxMemory)
                {
                    freeMemory.Add(info.MemEnd, maxMemory);
                }
                for (int i = info.MemBegin; i < info.MemEnd; i++)
                {
                    string line = sr.ReadLine();
                    memory[i] = int.Parse(line);
                }
            }
            UniteMemory(occupiedMemory);
            UniteMemory(freeMemory);
            PrintMemory();
            return info;
        }

        static void FreeMemory(TaskInfo info)//Освобождение памяти
    {
            occupiedMemory.Remove(info.MemBegin);
            freeMemory.Add(info.MemBegin, info.MemEnd - 1);
            UniteMemory(occupiedMemory);
            UniteMemory(freeMemory);
            PrintMemory();
            CheckQueue();
        }

    static void ThreadDoSomething(object obj)//Выполнение задачи
    {
            TaskInfo info = (TaskInfo)obj;
            int sum = 0;
            for (int i = info.MemBegin; i < info.MemEnd; i++)
            {
                sum += memory[i];//Суммируем
            }
            listTaskDoing.Remove(Thread.CurrentThread);
            queueTasksToFree.Enqueue(info);
        }

        static void ThreadFreeMemory()//Поток работающий с задачами
        {
            while (!isExit)
            {
                if (queueTasksToFree.Count > 0)
                {
                    FreeMemory(queueTasksToFree.Dequeue());
                }
            }
        }

        static void PrintThread()//Вывод очереди
        {
            Console.WriteLine("");
            Console.WriteLine("Задач на выполнении: " + listTaskDoing.Count);
            Console.WriteLine("Задач в очереди: " + queueTasksToWait.Count);
            foreach (var item in queueTasksToWait)
                Console.WriteLine("Задача: " + item.File + ". Размер: "  + item.SizeOfTask);
        }

        static void StartTask(string file, TaskInfo something)//Запуск задачи
    {
            Thread thread = new Thread(new ParameterizedThreadStart(ThreadDoSomething));
            listTaskDoing.Add(thread);
            thread.Start(something);
        }

        static void CheckQueue() //Проверка возможности перенести задачу из очереди ожидания в список на выполнение
    {
      if (queueTasksToWait.Count != 0 && queueTasksToFree.Count == 0)
            {
                var task = queueTasksToWait.Peek();
                var res = LookingForFree(task.File, task.SizeOfTask);
                if (res != null && queueTasksToWait.Count != 0)
                {
                    StartTask(queueTasksToWait.Dequeue().File, res);
                }
            }
            PrintThread();
        }

        static void ReadFile(string file) //Чтение данных файла и проверка возможности запуска задачи
    {
      using (StreamReader sr = new StreamReader(file))
            {
                int n = int.Parse(sr.ReadLine());
                var res = LookingForFree(file, n);
                if (res != null)
                {
                    StartTask(file, res);
                }
                else
                {
                    TaskInfo something = new TaskInfo(file, n);
                    queueTasksToWait.Enqueue(something);
                }
            }
        }

        static void PrintMenu()//Меню
        {
            Console.WriteLine("Вариант");
            Console.WriteLine("1 - состояние памяти");
            Console.WriteLine("2 - возможность выполнения задачи из очереди");
            Console.WriteLine("3 - добавить файл на выполнение");
            Console.WriteLine("4 - выход");
        }

        static void Main(string[] args)
        {
            isExit = false;
            PrintMenu();

            memory = new int[nMemory];//Моделируемая память
            freeMemory = new SortedList<int, int>();//Таблица свободной памяти
            occupiedMemory = new SortedList<int, int>(); //Таблица занятой памяти
            listTaskDoing = new List<Thread>();
            queueTasksToWait = new Queue<TaskInfo>();
            queueTasksToFree = new Queue<TaskInfo>();

            freeMemory.Add(0, nMemory);
            PrintMemory();

            Thread thread = new Thread(new ThreadStart(ThreadFreeMemory));
            thread.Start();

            for (int i = 1; i <= 5; i++)
                ReadFile(i + ".txt");

            int key;
            do
            {
                key = int.Parse(Console.ReadLine());
        Console.Clear();
        switch (key)
                {
                    case 1:
                        {
                            PrintMemory();
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                    case 2:
                        {
                            CheckQueue();
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                    case 3:
                        {
                          int keySubMenu;
                            do
                            {
                                Console.WriteLine("Введите номер файла на выполнение (число от 1 до 5):");
                                keySubMenu = int.Parse(Console.ReadLine());
                            } while (keySubMenu > 6 || keySubMenu < 1);
                            ReadFile(keySubMenu + ".txt");
                            Console.ReadKey();
                            Console.Clear(); 
                            break;
                        }
                }
                if (key != 4)PrintMenu();
        
      } while (key != 4);
            isExit = true;
        }
    }
}
