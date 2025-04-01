using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FuelConsumptionCalculator
{
    class Journey
    {
        public double StartMercedes { get; set; }
        public double EndMercedes { get; set; }
        public double StartTrimble { get; set; }
        public double EndTrimble { get; set; }
        public double StartMileage { get; set; }
        public double EndMileage { get; set; }
        public double CargoWeightKg { get; set; }

        // Obliczenia
        public double Distance => EndMileage - StartMileage;
        public double ConsumedMercedes => EndMercedes - StartMercedes;
        public double ConsumedTrimble => EndTrimble - StartTrimble;

        // Norma spalania: baseNorm dm³/100km + extraPerTon dm³/100km za każdą 1 tonę ładunku (kg/1000)
        // Przeliczamy normę na litry (1 dm³ = 1 l)
        public double NormPer100Km(double baseNorm, double extraPerTon) => baseNorm + extraPerTon * (CargoWeightKg / 1000.0);
        public double NormTotal(double baseNorm, double extraPerTon) => NormPer100Km(baseNorm, extraPerTon) * (Distance / 100.0);

        // Różnice (oszczędność / nadmierne zużycie) – dodatnia wartość oznacza zużycie więcej niż norma.
        public double DifferenceMercedes(double baseNorm, double extraPerTon) => ConsumedMercedes - NormTotal(baseNorm, extraPerTon);
        public double DifferenceTrimble(double baseNorm, double extraPerTon) => ConsumedTrimble - NormTotal(baseNorm, extraPerTon);

        // Funkcja do "zaokrąglania w górę" do 2 miejsc po przecinku
        public static double RoundUp(double input)
        {
            return Math.Ceiling(input * 100) / 100;
        }
    }

    class Program
    {
        // Lista przejazdów
        static List<Journey> journeys = new List<Journey>();

        static double DefaultBaseNorm = 20;
        static double DefaultExtraPerTon = 0.4;

        static void Main(string[] args)
        {
            Console.WriteLine("Czy norma spalania 20dm³/100km + (0,4dm³/100km za każdą 1 tonę ładunku) jest aktualna? (t/n)");
            string normaInput = Console.ReadLine().Trim().ToLower();
            if (normaInput != "t")
            {
                Console.WriteLine("Podaj nową normę spalania na 100km (w litrach) dla pojazdu bez ładunku:");
                DefaultBaseNorm = Convert.ToDouble(Console.ReadLine(), CultureInfo.InvariantCulture);
                Console.WriteLine("Podaj dodatek (w litrach/100km) za każdą tonę ładunku:");
                DefaultExtraPerTon = Convert.ToDouble(Console.ReadLine(), CultureInfo.InvariantCulture);
            }

            bool exit = false;
            // Dane startowe – początkowe odczyty liczników
            double currentMercedes = 0, currentTrimble = 0, currentMileage = 0;
            bool haveInitialData = false;

            while (!exit)
            {
                Console.WriteLine("\nWybierz opcję:");
                Console.WriteLine("1. Rozpocznij osobny przejazd (podaj nowe dane startowe)");
                Console.WriteLine("2. Kontynuuj przejazd (dane końcowe poprzedniego przejazdu jako startowe)");
                Console.WriteLine("3. Wyświetl wyniki");
                Console.WriteLine("4. Zakończ program");

                string option = Console.ReadLine().Trim();

                switch (option)
                {
                    case "1":
                        // Nowy przejazd – pobieramy wszystkie dane startowe, zaczynając od przebiegu
                        currentMileage = PobierzWartosc("Podaj stan licznika przebiegu [km]:");
                        currentMercedes = PobierzWartosc("Podaj stan licznika paliwa (MercedesBenz) [l]:");
                        currentTrimble = PobierzWartosc("Podaj stan licznika paliwa (Trimble) [l]:");
                        haveInitialData = true;
                        // Pobieramy dane końcowe dla przejazdu
                        Journey journey1 = GetJourneyData(currentMileage, currentMercedes, currentTrimble);
                        journeys.Add(journey1);
                        // Uaktualniamy dane startowe do kolejnego przejazdu
                        currentMileage = journey1.EndMileage;
                        currentMercedes = journey1.EndMercedes;
                        currentTrimble = journey1.EndTrimble;
                        break;

                    case "2":
                        // Kontynuacja przejazdu – sprawdzamy czy mamy dane startowe
                        if (!haveInitialData)
                        {
                            Console.WriteLine("Brak danych startowych. Wybierz opcję 1.");
                        }
                        else
                        {
                            Journey journey2 = GetJourneyData(currentMileage, currentMercedes, currentTrimble);
                            journeys.Add(journey2);
                            currentMileage = journey2.EndMileage;
                            currentMercedes = journey2.EndMercedes;
                            currentTrimble = journey2.EndTrimble;
                        }
                        break;

                    case "3":
                        DisplayResults();
                        break;

                    case "4":
                        exit = true;
                        break;

                    default:
                        Console.WriteLine("Niepoprawna opcja. Spróbuj jeszcze raz.");
                        break;
                }
            }

            // Po zakończeniu programu – zapis do pliku
            Console.WriteLine("\nCzy zapisać dane do pliku? (t/n)");
            if (Console.ReadLine().Trim().ToLower() == "t")
            {
                Console.WriteLine("Czy zapisać w domyślnej lokalizacji (Pulpit)? (t/n)");
                string path = "";
                if (Console.ReadLine().Trim().ToLower() == "t")
                {
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    Console.WriteLine("Podaj nazwę pliku (bez rozszerzenia):");
                    string fileName = Console.ReadLine().Trim();
                    path = Path.Combine(desktopPath, fileName + ".txt");
                }
                else
                {
                    Console.WriteLine("Podaj pełną ścieżkę z nazwą i rozszerzeniem pliku:");
                    path = Console.ReadLine().Trim();
                }
                SaveResultsToFile(path);
            }

            Console.WriteLine("Koniec programu. Naciśnij dowolny klawisz...");
            Console.ReadKey();
        }

        // Metoda do pobierania danych z przejazdu (pierwsze pytanie zawsze o stan licznika przebiegu)
        static Journey GetJourneyData(double startMileage, double startMercedes, double startTrimble)
        {
            Journey journey = new Journey();
            journey.StartMileage = startMileage;
            journey.StartMercedes = startMercedes;
            journey.StartTrimble = startTrimble;

            // Najpierw pobieramy stan licznika przebiegu po przejeździe
            journey.EndMileage = PobierzWartosc("Podaj stan licznika przebiegu po przejeździe [km]:");
            journey.EndMercedes = PobierzWartosc("Podaj stan licznika paliwa (MercedesBenz) po przejeździe [l]:");
            journey.EndTrimble = PobierzWartosc("Podaj stan licznika paliwa (Trimble) po przejeździe [l]:");
            journey.CargoWeightKg = PobierzWartosc("Podaj ładunek podczas przejazdu [kg]:");

            return journey;
        }

        // Funkcja pomocnicza do pobierania wartości liczbowych z konsoli
        static double PobierzWartosc(string prompt)
        {
            double value;
            while (true)
            {
                Console.WriteLine(prompt);
                string input = Console.ReadLine().Trim().Replace(',', '.');
                if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    break;
                }
                Console.WriteLine("Błędna wartość. Spróbuj jeszcze raz.");
            }
            return value;
        }

        // Wyświetlenie wyników – dla każdego przejazdu oraz podsumowanie całościowe
        static void DisplayResults()
        {
            Console.WriteLine("\n--- Wyniki przejazdów ---");
            double sumNorm = 0;
            double sumMercedes = 0;
            double sumTrimble = 0;
            int count = 1;

            foreach (var journey in journeys)
            {
                double normTotal = journey.NormTotal(DefaultBaseNorm, DefaultExtraPerTon);
                double consumedMercedes = journey.ConsumedMercedes;
                double consumedTrimble = journey.ConsumedTrimble;
                double diffMercedes = journey.DifferenceMercedes(DefaultBaseNorm, DefaultExtraPerTon);
                double diffTrimble = journey.DifferenceTrimble(DefaultBaseNorm, DefaultExtraPerTon);

                // Zaokrąglamy wyniki
                normTotal = Journey.RoundUp(normTotal);
                consumedMercedes = Journey.RoundUp(consumedMercedes);
                consumedTrimble = Journey.RoundUp(consumedTrimble);
                diffMercedes = Journey.RoundUp(diffMercedes);
                diffTrimble = Journey.RoundUp(diffTrimble);

                Console.WriteLine($"\nPrzejazd {count}:");
                Console.WriteLine($"Przebyty dystans: {journey.Distance} km");
                Console.WriteLine($"Zużycie paliwa (MercedesBenz): {consumedMercedes} l");
                Console.WriteLine($"Zużycie paliwa (Trimble): {consumedTrimble} l");
                Console.WriteLine($"Norma paliwa dla przejazdu: {normTotal} l");
                if (diffMercedes > 0)
                    Console.WriteLine($"System MercedesBenz: zużyto {diffMercedes} l więcej niż norma");
                else
                    Console.WriteLine($"System MercedesBenz: zaoszczędzono {Math.Abs(diffMercedes)} l paliwa");
                if (diffTrimble > 0)
                    Console.WriteLine($"System Trimble: zużyto {diffTrimble} l więcej niż norma");
                else
                    Console.WriteLine($"System Trimble: zaoszczędzono {Math.Abs(diffTrimble)} l paliwa");

                sumNorm += normTotal;
                sumMercedes += consumedMercedes;
                sumTrimble += consumedTrimble;
                count++;
            }

            // Podsumowanie całościowe
            Console.WriteLine("\n--- Podsumowanie całościowe ---");
            sumNorm = Journey.RoundUp(sumNorm);
            sumMercedes = Journey.RoundUp(sumMercedes);
            sumTrimble = Journey.RoundUp(sumTrimble);
            double totalDiffMercedes = Journey.RoundUp(sumMercedes - sumNorm);
            double totalDiffTrimble = Journey.RoundUp(sumTrimble - sumNorm);
            Console.WriteLine($"Łączna norma paliwa: {sumNorm} l");
            Console.WriteLine($"Łączne zużycie paliwa (MercedesBenz): {sumMercedes} l");
            Console.WriteLine($"Łączne zużycie paliwa (Trimble): {sumTrimble} l");
            if (totalDiffMercedes > 0)
                Console.WriteLine($"System MercedesBenz: przekroczono normę o {totalDiffMercedes} l");
            else
                Console.WriteLine($"System MercedesBenz: zaoszczędzono {Math.Abs(totalDiffMercedes)} l paliwa");
            if (totalDiffTrimble > 0)
                Console.WriteLine($"System Trimble: przekroczono normę o {totalDiffTrimble} l");
            else
                Console.WriteLine($"System Trimble: zaoszczędzono {Math.Abs(totalDiffTrimble)} l paliwa");
        }

        // Zapis wyników do pliku według podanej ścieżki
        static void SaveResultsToFile(string fullPath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(fullPath))
                {
                    sw.WriteLine("Podsumowanie przejazdów:");
                    int count = 1;
                    double sumNorm = 0;
                    double sumMercedes = 0;
                    double sumTrimble = 0;
                    foreach (var journey in journeys)
                    {
                        double normTotal = Journey.RoundUp(journey.NormTotal(DefaultBaseNorm, DefaultExtraPerTon));
                        double consumedMercedes = Journey.RoundUp(journey.ConsumedMercedes);
                        double consumedTrimble = Journey.RoundUp(journey.ConsumedTrimble);
                        double diffMercedes = Journey.RoundUp(journey.DifferenceMercedes(DefaultBaseNorm, DefaultExtraPerTon));
                        double diffTrimble = Journey.RoundUp(journey.DifferenceTrimble(DefaultBaseNorm, DefaultExtraPerTon));
                        sw.WriteLine($"\nPrzejazd {count}:");
                        sw.WriteLine($"Przebyty dystans: {journey.Distance} km");
                        sw.WriteLine($"Zużycie paliwa (MercedesBenz): {consumedMercedes} l");
                        sw.WriteLine($"Zużycie paliwa (Trimble): {consumedTrimble} l");
                        sw.WriteLine($"Norma paliwa: {normTotal} l");
                        if (diffMercedes > 0)
                            sw.WriteLine($"System MercedesBenz: przekroczono normę o {diffMercedes} l");
                        else
                            sw.WriteLine($"System MercedesBenz: zaoszczędzono {Math.Abs(diffMercedes)} l paliwa");
                        if (diffTrimble > 0)
                            sw.WriteLine($"System Trimble: przekroczono normę o {diffTrimble} l");
                        else
                            sw.WriteLine($"System Trimble: zaoszczędzono {Math.Abs(diffTrimble)} l paliwa");

                        sumNorm += normTotal;
                        sumMercedes += consumedMercedes;
                        sumTrimble += consumedTrimble;
                        count++;
                    }

                    double totalDiffMercedes = Journey.RoundUp(sumMercedes - sumNorm);
                    double totalDiffTrimble = Journey.RoundUp(sumTrimble - sumNorm);
                    sw.WriteLine("\n--- Podsumowanie całościowe ---");
                    sw.WriteLine($"Łączna norma paliwa: {sumNorm} l");
                    sw.WriteLine($"Łączne zużycie paliwa (MercedesBenz): {sumMercedes} l");
                    sw.WriteLine($"Łączne zużycie paliwa (Trimble): {sumTrimble} l");
                    if (totalDiffMercedes > 0)
                        sw.WriteLine($"System MercedesBenz: przekroczono normę o {totalDiffMercedes} l");
                    else
                        sw.WriteLine($"System MercedesBenz: zaoszczędzono {Math.Abs(totalDiffMercedes)} l paliwa");
                    if (totalDiffTrimble > 0)
                        sw.WriteLine($"System Trimble: przekroczono normę o {totalDiffTrimble} l");
                    else
                        sw.WriteLine($"System Trimble: zaoszczędzono {Math.Abs(totalDiffTrimble)} l paliwa");
                }
                Console.WriteLine($"Dane zapisano poprawnie w pliku: {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas zapisywania pliku: " + ex.Message);
            }
        }
    }
}
