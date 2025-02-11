using System;
using System.Collections.Generic;

namespace Pinpoint_Quiz.Models
{
    public class SchoolIndexViewModel
    {
        // Indicates if the view is for the whole school (default) or a particular teacher.
        public bool ShowWholeSchool { get; set; }
        public int? SelectedTeacherId { get; set; }
        // Dropdown for teachers
        public List<TeacherOption> TeacherDropdown { get; set; } = new List<TeacherOption>();

        // Chart data for daily averages (last 30 days)
        public List<string> DailyDates { get; set; } = new List<string>();
        public List<double> DailyAverages { get; set; } = new List<double>();

        // Chart data for weekly averages (last 6 months)
        public List<string> WeeklyLabels { get; set; } = new List<string>();
        public List<double> WeeklyAverages { get; set; } = new List<double>();

        // Table data: list of teachers in the school
        public List<TeacherRow> Teachers { get; set; } = new List<TeacherRow>();
    }

    public class TeacherOption
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
    }

    public class TeacherRow
    {
        public int TeacherId { get; set; }
        public string Name { get; set; }
        public int QuizCount { get; set; }
        public DateTime? LastQuizDate { get; set; }
    }
}
