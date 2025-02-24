using Google.Apis.Classroom.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pinpoint_Quiz.Controllers
{
    [Authorize] // ensure user is logged in
    [Route("[controller]/[action]")]
    public class ClassroomController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> CreateAssignment(string courseId, string quizUrl)
        {
            // Retrieve the access token from the user’s sign-in
            var result = await HttpContext.AuthenticateAsync();
            var accessToken = result.Properties.GetTokenValue("access_token");
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest("No access token available.");
            }

            // Create a credential from that token
            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken(accessToken);

            // Build a ClassroomService
            var classroomService = new ClassroomService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Pinpoint Quiz"
            });

            // Create a coursework assignment
            var courseWork = new Google.Apis.Classroom.v1.Data.CourseWork
            {
                Title = "Pinpoint Quiz Assignment",
                Description = $"Please complete the quiz: {quizUrl}",
                WorkType = "ASSIGNMENT",
                State = "PUBLISHED"
            };

            // Make the API call
            var createRequest = classroomService.Courses.CourseWork.Create(courseWork, courseId);
            var created = await createRequest.ExecuteAsync();

            return Ok(created);
        }
    }
}
