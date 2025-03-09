using MySqlConnector;
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

        public List<AvgData> GetDailyAveragesForSchool(int schoolId, int days)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DATE(QR.QuizDate) AS QuizDay, AVG(QR.OverallProficiency) AS AvgProf
                FROM QuizResults QR
                JOIN Users U ON QR.UserId = U.Id
                WHERE U.SchoolId = @SchoolId
                  AND DATE(QR.QuizDate) >= DATE_SUB(CURDATE(), INTERVAL @Days DAY)
                GROUP BY QuizDay
                ORDER BY QuizDay ASC;
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);
            cmd.Parameters.AddWithValue("@Days", days);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string day = Convert.ToString(reader["QuizDay"]);
                double avg = reader.GetDouble("AvgProf");
                list.Add(new AvgData { DateLabel = day, AverageProf = avg });
            }
            return list;
        }

        public List<AvgData> GetWeeklyAveragesForSchool(int schoolId)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DATE_FORMAT(QR.QuizDate, '%Y-%u') AS WeekLabel, AVG(QR.OverallProficiency) AS AvgProf
                FROM QuizResults QR
                JOIN Users U ON QR.UserId = U.Id
                WHERE U.SchoolId = @SchoolId
                  AND DATE(QR.QuizDate) >= DATE_SUB(CURDATE(), INTERVAL 6 MONTH)
                GROUP BY WeekLabel
                ORDER BY WeekLabel ASC;
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string week = Convert.ToString(reader["WeekLabel"]);
                double avg = reader.GetDouble("AvgProf");
                list.Add(new AvgData { WeekLabel = week, AverageProf = avg });
            }
            return list;
        }

        public List<TeacherRow> GetTeacherRowsForSchool(int schoolId)
        {
            var list = new List<TeacherRow>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    U.Id, 
                    CONCAT(U.FirstName, ' ', U.LastName) AS FullName,
                    (SELECT COUNT(*) FROM QuizResults WHERE UserId = U.Id) AS QuizCount,
                    (SELECT MAX(QuizDate) FROM QuizResults WHERE UserId = U.Id) AS LastQuizDate
                FROM Users U
                WHERE U.SchoolId = @SchoolId
                  AND U.UserRole = 'Teacher'
                ORDER BY U.LastName, U.FirstName;
            ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var teacher = new TeacherRow
                {
                    TeacherId = reader.GetInt32(0),
                    Name = Convert.ToString(reader["FullName"]),
                    QuizCount = reader.GetInt32(2),
                    LastQuizDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                };
                list.Add(teacher);
            }
            return list;
        }

        public SchoolIndexViewModel GetSchoolPerformance(int schoolId)
        {
            var model = new SchoolIndexViewModel();

            var daily = GetDailyAveragesForSchool(schoolId, 30);
            model.DailyDates = daily.Select(x => x.DateLabel).ToList();
            model.DailyAverages = daily.Select(x => x.AverageProf).ToList();

            var weekly = GetWeeklyAveragesForSchool(schoolId);
            model.WeeklyLabels = weekly.Select(x => x.WeekLabel).ToList();
            model.WeeklyAverages = weekly.Select(x => x.AverageProf).ToList();

            model.Teachers = GetTeacherRowsForSchool(schoolId);

            return model;
        }
    }
}
