using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Симплекс
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Инициализация начальной симплекс-таблицы
            // Пример входных данных, можно заменить на считывание из консоли или файла
            string[] variables = { "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10" };
            string[] cb = { "53M", "8M-4", "6M-1", "M", "M", "M", "M", "0", "0", "0", "0" };
            //string[] cb = { "33M", "8M-4", "3M-1", "-M", "M", "M", "M", "0", "0", "0", "0" };
            string[] cValues = { null, "4", "1", "0", "0", "0", "0", "M", "M", "M", "M" };
            string[,] table = {
                { "A7", "M", "6", "3", "2", "-1", "0", "0", "0", "1", "0", "0", "0" },
                { "A8", "M", "30", "1", "5", "0", "1", "0", "0", "0", "1", "0", "0" },
                { "A9", "M", "5", "1", "0", "0", "0", "1", "0", "0", "0", "1", "0" },
                { "A10", "M", "12", "3", "-1", "0", "0", "0", "1", "0", "0", "0", "1" }
            };
            /*string[,] table = {
                { "A7", "M", "5", "5", "1", "-1", "0", "0", "0", "1", "0", "0", "0" },
                { "A8", "M", "6", "0", "1", "0", "1", "0", "0", "0", "1", "0", "0" },
                { "A9", "M", "16", "2", "1", "0", "0", "1", "0", "0", "0", "1", "0" },
                { "A10", "M", "6", "1", "0", "0", "0", "0", "1", "0", "0", "0", "1" }
            };*/
            double[] zjMinusCj = new double[variables.Length];
            int iteration = 0;
            int choice = 0;

            while (true)
            {
                Console.WriteLine($"План {++iteration}:");

                // Шаг 2: Поиск разрешающего столбца
                int pivotColumn = FindPivotColumn(cb, choice);

                if (pivotColumn == -1)
                {
                    PrintTable(variables, cb, cValues, table, zjMinusCj, null, null);
                    if(choice == 0)
                        Console.WriteLine("План является оптимальным, так как m+1 строка содержит положительные или нулевые коэффициенты.");
                    if (choice == 1)
                        Console.WriteLine("Так как в индексной строке (m+1) находятся отрицательные коэффициенты или коэффициенты равные 0, то исследование на минимум прекращается.");
                    choice++;
                    double result = 0;
                    string formula = "";

                    for (int i = 0; i < table.GetLength(0); i++)
                    {
                        double firstValue = double.Parse(table[i, 1]);
                        double secondValue = double.Parse(table[i, 2]);
                        double product = firstValue * secondValue;
                        formula += $"{firstValue}*{secondValue} + ";
                        result += product;
                    }
                    formula = formula.TrimEnd(' ', '+');

                    Console.WriteLine($"Z= {formula} = {result} = Z(max)");
                    if (choice == 2)
                        break;
                    Console.ReadLine();
                    continue;
                }

                // Поиск разрешающей строки
                int pivotRow = FindPivotRow(table, pivotColumn + 1, choice);

                if (pivotRow == -1)
                {
                    Console.WriteLine("Нет допустимой разрешающей строки.");
                }
                else
                {
                    PrintTable(variables, cb, cValues, table, zjMinusCj, pivotRow, pivotColumn);
                    Console.WriteLine($"Разрешающий столбец: {pivotColumn}, Разрешающая строка: {pivotRow}. Разрешающий элемент = {table[pivotRow, pivotColumn + 2]}");
                }

                UpdatePivotRowAndBasis(table, variables, cValues, pivotRow, pivotColumn);

                UpdateOtherRows(table, pivotRow, pivotColumn);

                CalculateZjPlusCj(variables, cValues, table, cb);

                Console.WriteLine("ENTER для следующей итерации");
                Console.ReadLine();
            }
            Console.WriteLine("Вывод. В ходе самостоятельной работы были изучены принципы решения задач линейного программирования с помощью графического метода и симплекс метода\r\n");
            Console.ReadLine();
        }

        static void CalculateZjPlusCj(string[] variables, string[] cValues, string[,] table, string[] cb)
        {
            // Количество строк и столбцов в таблице
            int rowCount = table.GetLength(0);
            int colCount = table.GetLength(1);

            // Проходим по каждому столбцу, вычисляя zj + cj
            for (int j = 2; j < colCount; j++) // Игнорируем первые 2 колонки
            {
                string sum = ""; // Cуммарное выражение для текущего столбца

                for (int i = 0; i < rowCount; i++)
                {
                    // Значение из столбца "C базисное" и текущий элемент таблицы
                    string cValue = table[i, 1]; // C базисное
                    string tableValue = table[i, j];

                    if (double.TryParse(cValue, out double numericCValue) && double.TryParse(tableValue, out double numericTableValue))
                    {
                        // Если оба значения числовые
                        double product = numericCValue * numericTableValue;
                        sum = AppendToExpression(sum, product.ToString());
                    }
                    else if (!double.TryParse(cValue, out _) && double.TryParse(tableValue, out double numericValue))
                    {
                        // Если cValue - это "M", а tableValue - числовое
                        string product = $"{numericValue}M";
                        sum = AppendToExpression(sum, product);
                    }
                    else if (!double.TryParse(tableValue, out _) && double.TryParse(cValue, out double numericC))
                    {
                        // Если tableValue - "M", а cValue - числовое
                        string product = $"{numericC}M";
                        sum = AppendToExpression(sum, product);
                    }
                }
                if (j >= 3)
                {
                    // Учитываем Cn ("стоимость направления")
                    if (j - 3 >= cValues.Length)
                    {
                        break;
                    }
                    string cn = cValues[j - 2]; // Индекс сдвинут на 2 из-за первых двух колонок
                    if (double.TryParse(cn, out double numericCn))
                    {
                        sum = AppendToExpression(sum, (-numericCn).ToString());
                    }
                    else
                    {
                        sum = AppendToExpression(sum, $"-{cn}");
                    }
                }

                cb[j - 2] = SimplifyExpression(sum);
            }
        }

        public static string SimplifyExpression(string expression)
        {
            // Заменяем минусы перед переменными для корректной обработки
            expression = expression.Replace("-M", "-1M").Replace("+M", "+1M");

            // Разделяем выражение на отдельные части
            var terms = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            double constantSum = 0;
            double variableSum = 0;

            foreach (var term in terms)
            {
                if (term.Contains("M"))
                {
                    // Если это переменная (например, "1M", "-0.5M")
                    string coefficient = term.Replace("M", "");
                    double value = string.IsNullOrEmpty(coefficient) ? 1 : double.Parse(coefficient);
                    variableSum += value;
                }
                else if (term != "+" && term != "-")
                {
                    // Если это число
                    constantSum += double.Parse(term);
                }
            }

            // Собираем упрощённое выражение в виде "переменные+константы"
            string simplified = "";

            // Переменная часть (если не равна 0)
            if (variableSum != 0)
            {
                string variablePart = variableSum == 1 ? "M" :
                                      variableSum == -1 ? "-M" : $"{Math.Round(variableSum, 2)}M";

                simplified += $"{variablePart}";
            }

            // Константная часть (если не равна 0)
            if (constantSum != 0)
            {
                string constantPart = (constantSum > 0 && !string.IsNullOrEmpty(simplified)) ? $"+{constantSum}" : $"{constantSum}";
                simplified += constantPart;
            }

            // Если обе суммы равны 0, возвращаем "0"
            return string.IsNullOrEmpty(simplified) ? "0" : simplified;
        }

        // Метод для добавления выражения к строке суммы с учётом знаков
        static string AppendToExpression(string expression, string term)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return term; // Если строка пуста, просто возвращаем term
            }

            // Если `term` начинается с отрицательного знака, добавляем его без изменений
            if (term.StartsWith("-"))
            {
                return expression + " " + term;
            }

            // Если положительное значение, добавляем с "+"
            return expression + " + " + term;
        }

        // пересчитывание строк
        static void UpdateOtherRows( string[,] table, int pivotRow, int pivotColumn)
        {
            int rowCount = table.GetLength(0); // количество строк
            int colCount = table.GetLength(1); // количество столбцов

            // Обходим каждую строку таблицы
            for (int i = 0; i < rowCount; i++)
            {
                // Пропускаем разрешающую строку
                if (i == pivotRow) continue;

                // Получаем элемент в разрешающем столбце для текущей строки
                double pivotValue = double.Parse(table[i, pivotColumn + 2]);

                // Если pivotValue == 0, пропускаем эту строку
                if (pivotValue == 0) continue;

                // Вспомогательная матрица (3 строки)
                double[,] helperMatrix = new double[3, colCount - 2];

                // 6.1.1. Заполняем первую строку вспомогательной матрицы
                for (int j = 2; j < colCount; j++)
                {
                    helperMatrix[0, j - 2] = Math.Round(double.Parse(table[i, j]), 2);
                }

                // 6.1.2. Заполняем вторую строку (умножаем разрешающую строку на pivotValue)
                for (int j = 2; j < colCount; j++)
                {
                    double pivotRowValue = double.Parse(table[pivotRow, j]);
                    helperMatrix[1, j - 2] = Math.Round(pivotRowValue * pivotValue, 2);
                }

                // 6.1.3. Заполняем третью строку (разность первой и второй строк)
                for (int j = 0; j < helperMatrix.GetLength(1); j++)
                {
                    helperMatrix[2, j] = Math.Round(helperMatrix[0, j] - helperMatrix[1, j], 2);
                }

                // Вывод вспомогательной матрицы
                Console.WriteLine($"Строка {table[i, 0]}:");
                PrintMatrix(helperMatrix);

                // 6.2. Обновляем текущую строку в таблице
                for (int j = 2; j < colCount; j++)
                {
                    table[i, j] = helperMatrix[2, j - 2].ToString();
                }
            }
        }

        // обновление строки
        static void UpdatePivotRowAndBasis( string[,] table, string[] variables, string[] cValues, int pivotRow, int pivotColumn) {
            // Установим размерность таблицы (строки и столбцы)
            int m = table.GetLength(0); // количество строк
            int n = table.GetLength(1); // количество столбцов

            // Получим разрешающий элемент
            double pivotElement = double.Parse(table[pivotRow, pivotColumn + 2]);

            // Проверьте, что разрешающий элемент не нулевой
            if (pivotElement == 0)
            {
                throw new InvalidOperationException("Разрешающий элемент не может быть равен нулю.");
            }

            // 5.1. Разделить всю строку pivotRow на разрешающий элемент, кроме первых двух элементов
            // Перебираем только ячейки от 2 до конца строки
            for (int j = 2; j < n; j++)
            {
                double value = double.Parse(table[pivotRow, j]);
                // Округляем результат деления до двух знаков
                table[pivotRow, j] = Math.Round(value / pivotElement, 2).ToString();
            }

            // 5.2. Обновление базиса (столбец "Базис"):
            // заменим имя переменной на переменную из разрешающего столбца
            table[pivotRow, 0] = variables[pivotColumn]; // -2 из-за смещения (начального выравнивания).

            // 5.3. Обновление столбца C:
            // Найдем значение C для базисной переменной из cValues и запишем в столбец "Cb"
            table[pivotRow, 1] = cValues[pivotColumn];
        }

        // Метод для поиска разрешающего столбца
        public static int FindPivotColumn(string[] cb, int preference)
        {
            double maxSubtractedValue = double.MinValue; // Для макс. вычитания "M - k"
            int maxSubtractedIndex = -1;                // Индекс макс. "M - k"
            int simpleMIndex = -1;                      // Индекс "M"
            double maxPositiveValue = double.MinValue;  // Максимальное положительное значение
            int maxPositiveIndex = -1;                  // Индекс положительного

            for (int i = 1; i < cb.Length; i++)
            {
                string element = cb[i];

                // Логика обработки элементов с "M"
                if (element.Contains("M"))
                {
                    if (element.Contains("-") && !element.Contains("+")) // "M - k"
                    {
                        string[] parts = element.Split('M');
                        if (parts.Length > 1 && double.TryParse(parts[1], out double subtractedValue))
                        {
                            if (-subtractedValue > maxSubtractedValue)
                            {
                                maxSubtractedValue = -subtractedValue;
                                maxSubtractedIndex = i;
                            }
                        }
                    }
                    else if (element == "M") // Просто "M"
                    {
                        // Если индекс "M" ещё не установлен, устанавливаем его (только для первой найденной M)
                        if (simpleMIndex == -1)
                        {
                            simpleMIndex = i;
                        }
                    }
                }
                else // Логика выбора положительных чисел
                {
                    if (double.TryParse(element, out double value) && value > maxPositiveValue && value > 0)
                    {
                        maxPositiveValue = value;
                        maxPositiveIndex = i;
                    }
                }
            }

            // Выбор логики на основе аргумента "preference"
            if (preference == 0)
            {
                // Если есть элемент с максимальным вычитанием "M - k", возвращаем его индекс
                if (maxSubtractedIndex != -1)
                    return maxSubtractedIndex;

                // Если нет, выбираем первую найденную "M"
                if (simpleMIndex != -1)
                    return simpleMIndex;

                // Ничего не найдено
                return -1;
            }
            else if (preference == 1)
            {
                // Если предпочтение положительным числам
                if (maxPositiveIndex != -1)
                    return maxPositiveIndex;

                // Если не нашли положительное значение, возвращаем "M" логику
                if (maxSubtractedIndex != -1)
                    return maxSubtractedIndex;

                if (simpleMIndex != -1)
                    return simpleMIndex;

                // Ничего не найдено
                return -1;
            }

            return -1; // Неверный аргумент preference
        }


        // Метод для поиска разрешающей строки
        public static int FindPivotRow(string[,] table, int pivotColumn, int choise)
        {
            double minRatio = double.MaxValue;
            int pivotRow = -1;
            if(choise == 0)
                Console.WriteLine("План не является оптимальным, поскольку в индексной строке (m+1) содержаться отрицательные коэффициенты.");
            if (choise == 1)
                Console.WriteLine("План не является оптимальным, поскольку в индексной строке (m+1) содержаться положительные коэффициенты.");
            Console.WriteLine("min(");

            for (int i = 0; i < table.GetLength(0); i++)
            {
                double cValue = double.Parse(table[i, 2]); // Свободный коэффициент из столбца A0
                double aij = double.Parse(table[i, pivotColumn + 1]); // Элемент из разрешающего столбца на строке

                Console.WriteLine($" {cValue} / {aij};");

                // Проверяем, что элемент разрешающего столбца > 0
                if (aij > 0)
                {
                    //Console.WriteLine("Зашел в условие if (aij > 0)");
                    double ratio = cValue / aij;
                    //Console.WriteLine($"{ratio} = {cValue} / {aij}");
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }
            Console.WriteLine($") = {minRatio}");
            Console.WriteLine($"Значение соответствует строке {table[pivotRow, 0]}, значит {table[pivotRow, 0]} разрешающая строка.");

            return pivotRow;
        }

        // Метод для отображения двумерной матрицы в консоли
        static void PrintMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0); // количество строк
            int cols = matrix.GetLength(1); // количество столбцов

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(matrix[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine(); // для отделения матриц
        }

        // печать мелких матриц
        static void PrintTable(string[] variables, string[] cb, string[] cValues, string[,] table, double[] zjMinusCj, int? highlightRow, int? highlightColumn)
        {
            int m = table.GetLength(0); // Количество строк в таблице
            int n = table.GetLength(1); // Количество столбцов в таблице

            // Печать заголовков
            Console.Write("Базис \t");
            Console.Write("Cбазис \t");
            for (int i = 0; i < variables.Length; i++)
            {
                if (i == highlightColumn)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(variables[i] + "\t");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(variables[i] + "\t");
                }
            }
            Console.WriteLine();

            // Печать строки cValues (верхние значения)
            Console.Write(" \t \t"); // Для выравнивания под заголовком
            for (int i = 0; i < cValues.Length; i++)
            {
                if (i == highlightColumn) // Если текущий индекс совпадает с индексом выделяемого CValue
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{cValues[i]}\t");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write($"{cValues[i]}\t");
                }
            }
            Console.WriteLine();

            // Печать таблицы
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // Проверяем, если текущая ячейка совпадает с выделяемой
                    if (i == highlightRow || j == highlightColumn + 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Green; // Устанавливаем зелёный цвет
                        Console.Write($"{table[i, j]}\t");
                        Console.ResetColor(); // Сбрасываем цвет после выделения
                    }
                    else
                    {
                        Console.Write($"{table[i, j]}\t");
                    }
                }
                Console.WriteLine();
            }

            // Печать строки zj - cj
            Console.Write("zj+cj\t \t");

            // Печать значений cb
            for (int i = 0; i < cb.Length; i++)
            {
                if (i == highlightColumn) // Если текущий индекс совпадает с индексом выделяемого элемента cb
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{cb[i]}\t");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write($"{cb[i]}\t");
                }
            }

            Console.WriteLine("\n");
        }
    }
}