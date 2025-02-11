using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Models.Pinpoint_Quiz.Models;
using Pinpoint_Quiz.Dtos;

namespace Pinpoint_Quiz.Services
{
    public class LessonService
    {
        private readonly SQLiteDatabase _db;
        private readonly ILogger<LessonService> _logger;

        public LessonService(SQLiteDatabase db, ILogger<LessonService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all lessons. If a subject is provided, only lessons matching that subject are returned.
        /// Lessons are ordered by Level ascending.
        /// </summary>
        public List<Lesson> GetAllLessons(string subject = null)
        {
            var lessons = new List<Lesson>();
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();

                if (string.IsNullOrEmpty(subject))
                {
                    cmd.CommandText = @"
                        SELECT Id, Title, Subject, Level, Content, VideoUrl 
                        FROM Lessons 
                        ORDER BY Level ASC";
                }
                else
                {
                    cmd.CommandText = @"
                        SELECT Id, Title, Subject, Level, Content, VideoUrl 
                        FROM Lessons 
                        WHERE Subject = @Subject 
                        ORDER BY Level ASC";
                    cmd.Parameters.AddWithValue("@Subject", subject);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var lesson = new Lesson
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Subject = reader.GetString(2),
                        Level = reader.GetInt32(3),
                        Content = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        VideoUrl = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    };

                    lessons.Add(lesson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllLessons: {ex.Message}");
            }
            return lessons;
        }

        /// <summary>
        /// Retrieves a single lesson by its ID.
        /// </summary>
        public Lesson GetLesson(int id)
        {
            try
            {
                using var conn = _db.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Title, Subject, Level, Content, VideoUrl 
                    FROM Lessons 
                    WHERE Id = @Id";
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Lesson
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Subject = reader.GetString(2),
                        Level = reader.GetInt32(3),
                        Content = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        VideoUrl = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetLesson: {ex.Message}");
            }
            return null;
        }
    }
}
