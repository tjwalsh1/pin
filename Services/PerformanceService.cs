using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Services
{
    public class PerformanceService
    {
        private readonly MySqlDatabase _database;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(MySqlDatabase database, ILogger<PerformanceService> logger)
        {
            _database = database;
            _logger = logger;
        }

        public (List<string> Dates, List<double> MathProficiencies, List<double> EbrwProficiencies)
            GetPerformanceHistory(int userId)
        {
            var dates = new List<string>();
            var mathProficiencies = new List<double>();
            var ebrwProficiencies = new List<double>();

            try
            {
                using var conn = _database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Week, ProficiencyMath, ProficiencyEbrw
                    FROM Performances
                    WHERE UserId = @UserId
                    ORDER BY Week ASC
                ";
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var weekDate = reader.GetDateTime(0);
                    double math = reader.GetDouble(1);
                    double ebrw = reader.GetDouble(2);

                    dates.Add(weekDate.ToShortDateString());
                    mathProficiencies.Add(math);
                    ebrwProficiencies.Add(ebrw);
                }

                _logger.LogInformation($"Retrieved performance history for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPerformanceHistory for user {userId}: {ex.Message}");
            }

            return (dates, mathProficiencies, ebrwProficiencies);
        }

        public double CalculateProficiencyDelta(List<AnswerDto> answers, string subject)
        {
            double total = 0;
            foreach (var ans in answers)
            {
                if (ans.IsCorrect) total += 1;
            }
            double average = total / Math.Max(1, answers.Count);
            return average - 0.5; // example
        }

        public void UpdateProficiency(int userId, double delta, string subject)
        {
            string columnName = subject switch
            {
                "Math" => "ProficiencyMath",
                "EBRW" => "ProficiencyEbrw",
                _ => "OverallProficiency"
            };

            try
            {
                using var conn = _database.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    UPDATE Users
                    SET {columnName} = {columnName} + @Delta
                    WHERE Id = @UserId
                ";
                cmd.Parameters.AddWithValue("@Delta", delta);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating {columnName} for user {userId}: {ex.Message}");
            }
        }
    }
}
