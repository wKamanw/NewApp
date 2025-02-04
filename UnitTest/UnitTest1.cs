using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YourProject.Tests
{
    [TestClass]
    public class PrimeCalculatorTests
    {
        [TestMethod]
        public void GetNPrimes_ReturnsCorrectCount_And_FirstPrimes()
        {
            // Arrange
            var calculator = new PrimeCalculator();
            int count = 5;
            int[] expectedPrimes = new int[] { 2, 3, 5, 7, 11 };

            // Act
            List<int> result = calculator.GetNPrimes(count);

            // Assert
            Assert.AreEqual(count, result.Count, "Количество найденных простых чисел не соответствует ожидаемому.");
            CollectionAssert.AreEqual(expectedPrimes, result, "Полученные простые числа не совпадают с ожидаемыми.");
        }

        [TestMethod]
        public void GetPrimesUpToN_ReturnsCorrectPrimes()
        {
            // Arrange
            var calculator = new PrimeCalculator();
            int max = 10;
            int[] expectedPrimes = new int[] { 2, 3, 5, 7 };

            // Act
            List<int> result = calculator.GetPrimesUpToN(max);

            // Assert
            CollectionAssert.AreEqual(expectedPrimes, result, "Список простых чисел до 10 не соответствует ожидаемому.");
        }
    }

    [TestClass]
    public class SieveGeneratorTests
    {
        [TestMethod]
        public void GenerateSieveImage_ReturnsValidBase64String()
        {
            // Arrange
            var generator = new SieveGenerator();
            int max = 10;

            // Act
            string base64Image = generator.GenerateSieveImage(max);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(base64Image), "Строка Base64 пуста или null.");

            // Попытка декодировать строку — если она некорректна, возникнет исключение.
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            Assert.IsTrue(imageBytes.Length > 0, "Декодированное изображение имеет нулевую длину.");
        }
    }

    [TestClass]
    public class HistoryManagerTests
    {
        [TestMethod]
        public void SaveRequestHistory_AddsEntryToHistoryDictionary()
        {
            // Arrange
            var historyDict = new Dictionary<string, List<string>>();
            var historyManager = new HistoryManager(historyDict);
            string userId = "user1";
            string requestInfo = "Test request";

            // Act
            historyManager.SaveRequestHistory(userId, requestInfo);

            // Assert
            Assert.IsTrue(historyDict.ContainsKey(userId), "Словарь истории не содержит ключ пользователя.");
            Assert.AreEqual(1, historyDict[userId].Count, "Количество записей для пользователя не равно 1.");
            StringAssert.Contains(historyDict[userId].First(), "Test request", "Запись не содержит ожидаемого текста.");
        }
    }
}
