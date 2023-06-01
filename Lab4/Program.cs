using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab4
{
    internal class Program
    {
        static Random random = new Random();
        static string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

        static void Main(string[] args)
        {
            int numberOfCities = random.Next(25,36);
            int[,] distanceMatrix = GenerateDistanceMatrix(numberOfCities);
            WriteToFile(distanceMatrix);

            // Параметри мурашиного алгоритму
            int numberOfAnts = 20;
            double evaporationRate = 0.5;
            double alpha = 1;
            double beta = 10;

            int[] bestTour = SolveTSP(distanceMatrix, numberOfAnts, evaporationRate, alpha, beta);

            Console.WriteLine("Best tour found:");
            foreach (int city in bestTour)
            {
                Console.Write(city + " ");
            }
            Console.WriteLine();

            double bestTourLength = CalculateTourLength(bestTour, distanceMatrix);
            Console.WriteLine("Best tour length: " + bestTourLength);

            PlotMap(bestTour, numberOfCities);

            Console.ReadLine();
        }
        // Функція читання з файлу
        private static int[,] ReadFile(int numberOfCities)
        {
            int[,] distanceMatrix = new int[numberOfCities, numberOfCities];
            var input = File.ReadAllText(projectDirectory + "\\map.txt");
            int i = 0, j = 0;
            foreach (var row in input.Split('\n'))
            {
                j = 0;
                foreach (var col in row.Trim().Split(' '))
                {
                    distanceMatrix[i, j] = int.Parse(col.Trim());
                    j++;
                }
                i++;
            }

            return distanceMatrix;
        }
        // Функція запису у файл
        private static void WriteToFile(int[,] distanceMatrix)
        {
            using(var sw = new StreamWriter(projectDirectory + "\\map.txt"))
            {
                for (int i = 0; i < distanceMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < distanceMatrix.GetLength(1); j++)
                    {
                        sw.Write(distanceMatrix[i, j] + " ");
                    }
                    if(i < distanceMatrix.GetLength(0) - 1)
                        sw.Write("\n");
                }
                sw.Flush();
                sw.Close();
            }
            
        }

        // Функція генерації карти маршрутів в залежності від кількості міст
        static int[,] GenerateDistanceMatrix(int numberOfCities)
        {
            int[,] distanceMatrix = new int[numberOfCities, numberOfCities];
            for (int i = 0; i < numberOfCities; i++)
            {
                for (int j = 0; j < numberOfCities; j++)
                {
                    if (i == j)
                    {
                        distanceMatrix[i, j] = 0;
                    }
                    else
                    {
                        distanceMatrix[i, j] = random.Next(10, 101);
                    }
                }
            }
            return distanceMatrix;
        }
        // Функція мурашиного алгоритму
        static int[] SolveTSP(int[,] distanceMatrix, int numberOfAnts, double evaporationRate, double alpha, double beta)
        {
            int numberOfCities = distanceMatrix.GetLength(0);
            int[] bestTour = null;
            double bestTourLength = double.MaxValue;
            double[,] pheromoneMatrix = InitializePheromoneMatrix(numberOfCities);

            for (int simulation = 1; simulation <= 10; simulation++)
            {
                int[][] antTours = new int[numberOfAnts][];

                for (int ant = 0; ant < numberOfAnts; ant++)
                {
                    //Конструювання шляху мурахи
                    antTours[ant] = ConstructTour(distanceMatrix, pheromoneMatrix, alpha, beta);
                    //Оновлення ферментного сліду на шляху
                    UpdatePheromoneTrail(pheromoneMatrix, antTours[ant], evaporationRate);
                    //Обчислення довжини шляху
                    double tourLength = CalculateTourLength(antTours[ant], distanceMatrix);
                    //Порівняння з найкращим шляхом
                    if (tourLength < bestTourLength)
                    {
                        bestTourLength = tourLength;
                        bestTour = antTours[ant];
                    }
                }
            }

            return bestTour;
        }
        // Ініціалізація матриці ферментів початковими значеннями
        static double[,] InitializePheromoneMatrix(int numberOfCities)
        {
            double initialPheromone = 1.0 / numberOfCities;
            double[,] pheromoneMatrix = new double[numberOfCities, numberOfCities];

            for (int i = 0; i < numberOfCities; i++)
            {
                for (int j = 0; j < numberOfCities; j++)
                {
                    pheromoneMatrix[i, j] = initialPheromone;
                }
            }

            return pheromoneMatrix;
        }
        // Функція побудови шляху за ферментною матрицею та за константами
        static int[] ConstructTour(int[,] distanceMatrix, double[,] pheromoneMatrix, double alpha, double beta)
        {
            int numberOfCities = distanceMatrix.GetLength(0);
            int startingCity = random.Next(numberOfCities); // Вибираємо початкове місто випадковим чином
            int[] tour = new int[numberOfCities]; // Масив, що зберігатиме шлях мурахи
            bool[] visited = new bool[numberOfCities]; // Масив, що вказує, чи було відвідано місто

             
            tour[0] = startingCity; // Початкове місто
            visited[startingCity] = true; // Позначаємо початкове місто як відвідане

            for (int i = 1; i < numberOfCities; i++)
            {
                int currentCity = tour[i - 1]; // Поточне місто
                int nextCity = ChooseNextCity(distanceMatrix, pheromoneMatrix, currentCity, visited, alpha, beta); // Вибираємо наступне місто
                tour[i] = nextCity; // Додаємо наступне місто до шляху
                visited[nextCity] = true;  // Позначаємо наступне місто як відвідане
            }

            return tour; // Повертаємо шлях
        }
        //// Вибір наступного міста для мурахи на основі матриці відстаней, матриці ферментів та параметрів alpha і beta
        static int ChooseNextCity(int[,] distanceMatrix, double[,] pheromoneMatrix, int currentCity, bool[] visited, double alpha, double beta)
        {
            int numberOfCities = distanceMatrix.GetLength(0);
            List<int> unvisitedCities = new List<int>(); for (int i = 0; i < numberOfCities; i++) // невідвідані міста
            {
                if (!visited[i])
                {
                    unvisitedCities.Add(i);
                }
            }

            double[] probabilities = new double[unvisitedCities.Count]; // Масив ймовірностей вибору міст
            double probabilitiesSum = 0; // Сума ймовірностей

            for (int i = 0; i < unvisitedCities.Count; i++)
            {
                int city = unvisitedCities[i];
                double visibility = 1.0 / distanceMatrix[currentCity, city]; // Обернена відстань 
                double pheromoneLevel = pheromoneMatrix[currentCity, city]; // Рівень ферментів на дорозі
                probabilities[i] = Math.Pow(pheromoneLevel, alpha) * Math.Pow(visibility, beta); // Обчислення ймовірності вибору міста
                probabilitiesSum += probabilities[i]; // Додавання ймовірності до загальної суми
            }
            // Нормалізація ймовірностей
            for (int i = 0; i < probabilities.Length; i++)
            {
                probabilities[i] /= probabilitiesSum;
            }

            double randomValue = random.NextDouble(); // Випадкове значення для вибору міста
            double cumulativeProbability = 0;

            // Вибір наступного міста на основі накопиченої ймовірності
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    return unvisitedCities[i];// Повертаємо вибране місто
                }
            }

            return unvisitedCities[unvisitedCities.Count - 1];// Якщо не вдалося вибрати місто, повертаємо останнє місто зі списку невідвіданих
        }
        // Функція оновлення ферментної матриці
        static void UpdatePheromoneTrail(double[,] pheromoneMatrix, int[] tour, double evaporationRate)
        {
            int numberOfCities = tour.Length; for (int i = 0; i < numberOfCities - 1; i++)
            {
                int city1 = tour[i];
                int city2 = tour[i + 1];
                pheromoneMatrix[city1, city2] *= (1 - evaporationRate);
                pheromoneMatrix[city2, city1] = pheromoneMatrix[city1, city2];
            }
        }
        // Функція обрахунку довжини шляху
        static double CalculateTourLength(int[] tour, int[,] distanceMatrix)
        {
            int tourLength = 0;
            for (int i = 0; i < tour.Length - 1; i++)
            {
                int city1 = tour[i];
                int city2 = tour[i + 1];
                tourLength += distanceMatrix[city1, city2];
            }
            tourLength += distanceMatrix[tour[tour.Length - 1], tour[0]];
            return tourLength;
        }
        private static void PlotMap(int[] bestPath, int citiesAmount)
        {
            double[] bestX = new double[citiesAmount];
            double[] bestY = new double[citiesAmount];

            double cx = 0.5;
            double cy = 0.5;

            double r = 0.4;

            double[] angles = new double[citiesAmount];
            for (int i = 0; i < citiesAmount; i++)
            {
                angles[i] = 2 * Math.PI * i / citiesAmount;
            }

            double[] x = new double[citiesAmount];
            double[] y = new double[citiesAmount];
            for (int i = 0; i < citiesAmount; i++)
            {
                x[i] = r * Math.Cos(angles[i]) + cx;
                y[i] = r * Math.Sin(angles[i]) + cy;
            }

            Chart chart = new Chart();
            chart.Size = new Size(1000, 1000);

            ChartArea chartArea = new ChartArea();
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = 1;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = 1;
            chart.ChartAreas.Add(chartArea);

            for (int i = 0; i < citiesAmount; i++)
            {
                for (int j = 0; j < citiesAmount; j++)
                {
                    if (i < j)
                    {
                        chart.Series.Add(new Series());
                        chart.Series[chart.Series.Count - 1].ChartType = SeriesChartType.Line;
                        chart.Series[chart.Series.Count - 1].Points.AddXY(x[i], y[i]);
                        chart.Series[chart.Series.Count - 1].Points.AddXY(x[j], y[j]);
                        chart.Series[chart.Series.Count - 1].Color = Color.Black;
                        chart.Series[chart.Series.Count - 1].BorderWidth = 1;
                        chart.Series[chart.Series.Count - 1].BorderDashStyle = ChartDashStyle.Solid;
                        chart.Series[chart.Series.Count - 1].BorderColor = Color.FromArgb(102, 0, 0, 0);
                    }
                }
            }

            for (int i = 0; i < citiesAmount; i++)
            {
                if (i == bestPath[0])
                {
                    chart.Series.Add(new Series());
                    chart.Series[chart.Series.Count - 1].ChartType = SeriesChartType.Point;
                    chart.Series[chart.Series.Count - 1].Points.AddXY(x[i], y[i]);
                    chart.Series[chart.Series.Count - 1].MarkerSize = 18;
                    chart.Series[chart.Series.Count - 1].MarkerStyle = MarkerStyle.Circle;
                    chart.Series[chart.Series.Count - 1].Color = Color.FromArgb(210, 105, 30);
                }
                else
                {
                    chart.Series.Add(new Series());
                    chart.Series[chart.Series.Count - 1].ChartType = SeriesChartType.Point;
                    chart.Series[chart.Series.Count - 1].Points.AddXY(x[i], y[i]);
                    chart.Series[chart.Series.Count - 1].MarkerSize = 18;
                    chart.Series[chart.Series.Count - 1].MarkerStyle = MarkerStyle.Circle;
                    chart.Series[chart.Series.Count - 1].Color = Color.FromArgb(0, 0, 128);
                }
                chart.Series[chart.Series.Count - 1].Points[0].Label = (i + 1).ToString();
                chart.Series[chart.Series.Count - 1].Points[0].SetCustomProperty("LabelStyle", "Top");
                chart.Series[chart.Series.Count - 1].Points[0].LabelBackColor = Color.Black;
                chart.Series[chart.Series.Count - 1].Points[0].LabelForeColor= Color.White;
                bestX[i] = x[bestPath[i]];
                bestY[i] = y[bestPath[i]];
            }

            chart.Series.Add(new Series());
            chart.Series[chart.Series.Count - 1].ChartType = SeriesChartType.Line;
            chart.Series[chart.Series.Count - 1].Points.DataBindXY(bestX, bestY);
            chart.Series[chart.Series.Count - 1].Points.AddXY(bestX[0], bestY[0]);
            chart.Series[chart.Series.Count - 1].Color = Color.Red;
            chart.Series[chart.Series.Count - 1].BorderWidth = 3;
            chart.Series[chart.Series.Count - 1].BorderDashStyle = ChartDashStyle.Solid;

            chartArea.AxisX.Title = "X";
            chartArea.AxisY.Title = "Y";
            chartArea.AxisX.TitleFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular);
            chartArea.AxisY.TitleFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular);
            chart.Titles.Add("Traveling Salesman Problem");
            chart.Titles[0].Font = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold);

            chart.Padding = new Padding(10);
            chart.ChartAreas[0].Position = new ElementPosition(10, 10, 80, 80);
            
            chart.SaveImage(projectDirectory + "\\chart.png", ChartImageFormat.Png);
        }
    }
}
