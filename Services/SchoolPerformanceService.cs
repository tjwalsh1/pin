using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinpoint_Quiz.Services
{
    public class SchoolPerformanceService
    {
        private readonly MySqlDatabase _db;
        private readonly ILogger<SchoolPerformanceService> _logger;

        public SchoolPerformanceService(MySqlDatabase db, ILogger<SchoolPerformanceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Returns daily average overall proficiency for a given school (last 'days' days)
        public List<AvgData> GetDailyAveragesForSchool(int schoolId, int days)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT date(QR.QuizDate) as QuizDay, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U ON QR.UserId = U.Id
                WHERE U.SchoolId = @SchoolId
                  AND date(QR.QuizDate) >= date('now', '-' || @Days || ' days')
                GROUP BY QuizDay
                ORDER BY QuizDay ASC
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);
            cmd.Parameters.AddWithValue("@Days", days);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string day = reader.GetString(0);
                double avg = reader.GetDouble(1);
                list.Add(new AvgData { DateLabel = day, AverageProf = avg });
            }
            return list;
        }

        // Returns weekly average overall proficiency for a given school (last 6 months)
        public List<AvgData> GetWeeklyAveragesForSchool(int schoolId)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            // Group by week using SQLite's strftime
            cmd.CommandText = @"
                SELECT strftime('%Y-%W', QR.QuizDate) as WeekLabel, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U ON QR.UserId = U.Id
                WHERE U.SchoolId = @SchoolId
                  AND date(QR.QuizDate) >= date('now', '-6 months')
                GROUP BY WeekLabel
                ORDER BY WeekLabel ASC
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string week = reader.GetString(0);
                double avg = reader.GetDouble(1);
                list.Add(new AvgData { WeekLabel = week, AverageProf = avg });
            }
            return list;
        }

        // Retrieves teacher rows for the school.
        public List<TeacherRow> GetTeacherRowsForSchool(int schoolId)
        {
            var list = new List<TeacherRow>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            // Make sure the SQL syntax is correct (no trailing commas)
            cmd.CommandText = @"
                SELECT 
                    U.Id, 
                    U.FirstName || ' ' || U.LastName AS FullName,
                    (SELECT COUNT(*) FROM QuizResults WHERE UserId = U.Id) AS QuizCount,
                    (SELECT MAX(QuizDate) FROM QuizResults WHERE UserId = U.Id) AS LastQuizDate
                FROM Users U
                WHERE U.SchoolId = @SchoolId
                  AND U.UserRole = 'Teacher'
                ORDER BY U.LastName, U.FirstName
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var teacher = new TeacherRow
                {
                    TeacherId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    QuizCount = reader.GetInt32(2),
                    LastQuizDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                };
                list.Add(teacher);
            }
            return list;
        }

        // Build a SchoolIndexViewModel for the entire school.
        public SchoolIndexViewModel GetSchoolPerformance(int schoolId)
        {
            var model = new SchoolIndexViewModel();

            // Daily averages (last 30 days)
            var daily = GetDailyAveragesForSchool(schoolId, 30);
            model.DailyDates = daily.Select(x => x.DateLabel).ToList();
            model.DailyAverages = daily.Select(x => x.AverageProf).ToList();

            // Weekly averages (last 6 months)
            var weekly = GetWeeklyAveragesForSchool(schoolId);
            model.WeeklyLabels = weekly.Select(x => x.WeekLabel).ToList();
            model.WeeklyAverages = weekly.Select(x => x.AverageProf).ToList();

            // Teacher list for the school
            model.Teachers = GetTeacherRowsForSchool(schoolId);

            return model;
        }
    }

    // Helper model for averages
    public class AvgData
    {
        public string DateLabel { get; set; }
        public string WeekLabel { get; set; }
        public double AverageProf { get; set; }
    }
}
