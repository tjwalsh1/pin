using System;
using System.Collections.Generic;

namespace Pinpoint_Quiz.Models
{
    public class ClassIndexViewModel
    {
        // Indicates if the current user is an administrator
        public bool IsAdmin { get; set; }
        // If admin: allow selection of a teacher or whole school.
        public int? SelectedTeacherId { get; set; }
        public bool ShowWholeSchool { get; set; }
        public List<TeacherOption> TeacherDropdown { get; set; } = new List<TeacherOption>();

        // Chart data for daily averages (last 30 days)
        public List<string> DailyDates { get; set; } = new List<string>();
        public List<double> DailyAverages { get; set; } = new List<double>();

        // Chart data for weekly averages (last 6 months)
        public List<string> WeeklyLabels { get; set; } = new List<string>();
        public List<double> WeeklyAverages { get; set; } = new List<double>();

        // Table data: a list of students in the class or school
        public List<StudentRow> Students { get; set; } = new List<StudentRow>();
    }

    public class StudentRow
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public int QuizCount { get; set; }
        public DateTime? LastQuizDate { get; set; }
    }

    namespace Pinpoint_Quiz.Models
    {
        public class AvgData
        {
            // For daily averages, use DateLabel; for weekly, use WeekLabel.
            public string DateLabel { get; set; }
            public string WeekLabel { get; set; }
            public double AverageProf { get; set; }
        }
    }

}
