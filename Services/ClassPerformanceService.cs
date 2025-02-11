using Microsoft.Data.Sqlite;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Models.Pinpoint_Quiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinpoint_Quiz.Services
{
    public class ClassPerformanceService
    {
        private readonly SQLiteDatabase _db;

        public ClassPerformanceService(SQLiteDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets daily average overall proficiency for a given class for the past 'days' days.
        /// </summary>
        private List<AvgData> GetDailyAveragesForClass(int classId, int days)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT date(QR.QuizDate) as QuizDay, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U on QR.UserId = U.Id
                WHERE U.ClassId = @ClassId
                  AND date(QR.QuizDate) >= date('now', '-' || @Days || ' days')
                GROUP BY QuizDay
                ORDER BY QuizDay ASC
            ";
            cmd.Parameters.AddWithValue("@ClassId", classId);
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

        /// <summary>
        /// Gets weekly average overall proficiency for a given class for the past 6 months.
        /// </summary>
        private List<AvgData> GetWeeklyAveragesForClass(int classId, int weeks)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            // Using strftime('%Y-%W', QuizDate) to group by week (year-week)
            cmd.CommandText = @"
                SELECT strftime('%Y-%W', QR.QuizDate) as WeekLabel, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U on QR.UserId = U.Id
                WHERE U.ClassId = @ClassId
                  AND date(QR.QuizDate) >= date('now', '-6 months')
                GROUP BY WeekLabel
                ORDER BY WeekLabel ASC
            ";
            cmd.Parameters.AddWithValue("@ClassId", classId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string week = reader.GetString(0);
                double avg = reader.GetDouble(1);
                list.Add(new AvgData { WeekLabel = week, AverageProf = avg });
            }
            return list;
        }

        /// <summary>
        /// Gets daily average overall proficiency for a given school for the past 'days' days.
        /// </summary>
        private List<AvgData> GetDailyAveragesForSchool(int schoolId, int days)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT date(QR.QuizDate) as QuizDay, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U on QR.UserId = U.Id
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

        /// <summary>
        /// Gets weekly average overall proficiency for a given school for the past 6 months.
        /// </summary>
        private List<AvgData> GetWeeklyAveragesForSchool(int schoolId, int weeks)
        {
            var list = new List<AvgData>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT strftime('%Y-%W', QR.QuizDate) as WeekLabel, AVG(QR.OverallProficiency) as AvgProf
                FROM QuizResults QR
                JOIN Users U on QR.UserId = U.Id
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

        /// <summary>
        /// Retrieves student rows for a given class.
        /// </summary>
        public List<StudentRow> GetStudentRowsForClass(int classId)
        {
            var list = new List<StudentRow>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT 
            U.Id, 
            U.FirstName || ' ' || U.LastName AS FullName,
            (SELECT COUNT(*) FROM QuizResults WHERE UserId = U.Id) AS QuizCount,
            (SELECT MAX(QuizDate) FROM QuizResults WHERE UserId = U.Id) AS LastQuizDate
        FROM Users U
        WHERE U.ClassId = @ClassId
          AND U.UserRole = 'Student'
        ORDER BY U.LastName, U.FirstName
    ";
            cmd.Parameters.AddWithValue("@ClassId", classId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var student = new StudentRow
                {
                    StudentId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    QuizCount = reader.GetInt32(2),
                    LastQuizDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                };
                list.Add(student);
            }
            return list;
        }


        /// <summary>
        /// Retrieves student rows for a given school.
        /// </summary>
        public List<StudentRow> GetStudentRowsForSchool(int schoolId)
        {
            var list = new List<StudentRow>();
            using var conn = _db.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT 
            U.Id, 
            U.FirstName || ' ' || U.LastName AS FullName,
            (SELECT COUNT(*) FROM QuizResults WHERE UserId = U.Id) AS QuizCount,
            (SELECT MAX(QuizDate) FROM QuizResults WHERE UserId = U.Id) AS LastQuizDate
        FROM Users U
        WHERE U.SchoolId = @SchoolId
          AND U.UserRole = 'Student'
        ORDER BY U.LastName, U.FirstName
    ";
            cmd.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var student = new StudentRow
                {
                    StudentId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    QuizCount = reader.GetInt32(2),
                    LastQuizDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                };
                list.Add(student);
            }
            return list;
        }


        // Public methods to fill the view model

        public ClassIndexViewModel GetSchoolPerformance(int schoolId)
        {
            var model = new ClassIndexViewModel();

            // Get daily averages (last 30 days)
            var daily = GetDailyAveragesForSchool(schoolId, 30);
            model.DailyDates = daily.Select(x => x.DateLabel).ToList();
            model.DailyAverages = daily.Select(x => x.AverageProf).ToList();

            // Get weekly averages (last 6 months)
            var weekly = GetWeeklyAveragesForSchool(schoolId, 24);
            model.WeeklyLabels = weekly.Select(x => x.WeekLabel).ToList();
            model.WeeklyAverages = weekly.Select(x => x.AverageProf).ToList();

            // Get student rows for the whole school
            model.Students = GetStudentRowsForSchool(schoolId);

            return model;
        }

        public ClassIndexViewModel GetClassPerformance(int classId, bool isAdmin)
        {
            var model = new ClassIndexViewModel();

            // Get daily averages for the class (last 30 days)
            var daily = GetDailyAveragesForClass(classId, 30);
            model.DailyDates = daily.Select(x => x.DateLabel).ToList();
            model.DailyAverages = daily.Select(x => x.AverageProf).ToList();

            // Get weekly averages for the class (last 6 months)
            var weekly = GetWeeklyAveragesForClass(classId, 24);
            model.WeeklyLabels = weekly.Select(x => x.WeekLabel).ToList();
            model.WeeklyAverages = weekly.Select(x => x.AverageProf).ToList();

            // Get student rows for the class
            model.Students = GetStudentRowsForClass(classId);

            return model;
        }
    }
}
