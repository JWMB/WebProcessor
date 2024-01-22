using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NoK.Models.Raw
{
    public class Assignment
    {
        [JsonProperty("assignmentID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentID")]
        public int? AssignmentID { get; set; }

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonProperty("bridgeID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("bridgeID")]
        public string BridgeID { get; set; }

        [JsonProperty("manualLevel", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("manualLevel")]
        public int? ManualLevel { get; set; }

        [JsonProperty("published", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("published")]
        public int? Published { get; set; }

        [JsonProperty("updated_by", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_by")]
        public int? UpdatedBy { get; set; }

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("created_at")]
        public object CreatedAt { get; set; }

        [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("hasSolution", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hasSolution")]
        public bool? HasSolution { get; set; }

        [JsonProperty("hasAnswer", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hasAnswer")]
        public bool? HasAnswer { get; set; }

        [JsonProperty("hasHint", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hasHint")]
        public bool? HasHint { get; set; }

        [JsonProperty("user_solution", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("user_solution")]
        public bool? UserSolution { get; set; }

        [JsonProperty("highlighted", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("highlighted")]
        public bool? Highlighted { get; set; }

        [JsonProperty("completedAt", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("completedAt")]
        public int? CompletedAt { get; set; }

        [JsonProperty("template_data", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("template_data")]
        public TemplateData TemplateData { get; set; }

        [JsonProperty("assignmentVideoUrl", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentVideoUrl")]
        public object AssignmentVideoUrl { get; set; }

        [JsonProperty("levels", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("levels")]
        public List<int?> Levels { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonProperty("template_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("template_id")]
        public int? TemplateId { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonProperty("assignment_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_id")]
        public int? AssignmentId { get; set; }

        [JsonProperty("section_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("section_id")]
        public int? SectionId { get; set; }

        [JsonProperty("points", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("points")]
        public int? Points { get; set; }

        [JsonProperty("latest_status", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("latest_status")]
        public int? LatestStatus { get; set; }

        [JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("difficulty")]
        public Difficulty Difficulty { get; set; }

        [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pivot")]
        public Pivot Pivot { get; set; }

        [JsonProperty("assignment_content", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_content")]
        public AssignmentContent AssignmentContent { get; set; }

        [JsonProperty("solutions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("solutions")]
        public List<Solution> Solutions { get; set; }

        [JsonProperty("hints", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hints")]
        public List<Hint> Hints { get; set; }
    }

    public class AssignmentContent
    {
        [JsonProperty("assignmentID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentID")]
        public int? AssignmentID { get; set; }

        [JsonProperty("templateID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("templateID")]
        public int? TemplateID { get; set; }

        [JsonProperty("templateData", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("templateData")]
        public string TemplateData { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("presentation_title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("presentation_title")]
        public string PresentationTitle { get; set; }

        [JsonProperty("videoUrl", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("videoUrl")]
        public string VideoUrl { get; set; }

        [JsonProperty("image_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("image_id")]
        public object ImageId { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("image")]
        public object Image { get; set; }
    }

    public class Difficulty
    {
        [JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("difficulty")]
        public double? Difficulty_ { get; set; }

        [JsonProperty("assignmentId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentId")]
        public int? AssignmentId { get; set; }

        [JsonProperty("batch", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("batch")]
        public object Batch { get; set; }
    }

    public class Hint
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonProperty("assignmentID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentID")]
        public int? AssignmentID { get; set; }

        [JsonProperty("subtask", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("subtask")]
        public string Subtask { get; set; }

        [JsonProperty("hints", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hints")]
        public string Hints { get; set; }

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class Lesson
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("html", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("html")]
        public string Html { get; set; }
    }

    public class Pivot
    {
        [JsonProperty("section_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("section_id")]
        public int? SectionId { get; set; }

        [JsonProperty("assignment_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_id")]
        public int? AssignmentId { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonProperty("assignment_name", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_name")]
        public string AssignmentName { get; set; }

        [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonProperty("hierarchy_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("hierarchy_id")]
        public int? HierarchyId { get; set; }
    }

    public class Respon
    {
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Root
    {
        [JsonProperty("subpart_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("subpart_id")]
        public int? SubpartId { get; set; }

        [JsonProperty("subpart", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("subpart")]
        public List<Subpart> Subpart { get; set; }

        [JsonProperty("current_assignment_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("current_assignment_id")]
        public string CurrentAssignmentId { get; set; }
    }

    public class SectionAssignmentRelation
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonProperty("section_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("section_id")]
        public int? SectionId { get; set; }

        [JsonProperty("assignment_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_id")]
        public int? AssignmentId { get; set; }

        [JsonProperty("assignment_name", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignment_name")]
        public string AssignmentName { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("position")]
        public int? Position { get; set; }
    }

    public class Solution
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonProperty("assignmentID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentID")]
        public int? AssignmentID { get; set; }

        [JsonProperty("subtask", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("subtask")]
        public string Subtask { get; set; }

        [JsonProperty("solutions", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("solutions")]
        public string Solutions { get; set; }

        [JsonProperty("answers", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("answers")]
        public string Answers { get; set; }

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class Subpart
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("lesson_id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("lesson_id")]
        public int? LessonId { get; set; }

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updated_at", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("assignments", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignments")]
        public List<Assignment> Assignments { get; set; }

        [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("pivot")]
        public Pivot Pivot { get; set; }

        [JsonProperty("lesson", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("lesson")]
        public Lesson Lesson { get; set; }

        [JsonProperty("section_assignment_relations", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("section_assignment_relations")]
        public List<SectionAssignmentRelation> SectionAssignmentRelations { get; set; }
    }

    public class TemplateData
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonProperty("variable", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("variable")]
        public string Variable { get; set; }

        [JsonProperty("sign", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("sign")]
        public string Sign { get; set; }

        [JsonProperty("illustration", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("illustration")]
        public string Illustration { get; set; }

        [JsonProperty("responseType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("responseType")]
        public string ResponseType { get; set; }

        [JsonProperty("geolocation", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("geolocation")]
        public string Geolocation { get; set; }

        [JsonProperty("suggestion", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; }

        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("settings")]
        public object Settings { get; set; }

        [JsonProperty("teacher-guide", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("teacher-guide")]
        public string TeacherGuide { get; set; }

        [JsonProperty("assignmentID", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("assignmentID")]
        public int? AssignmentID { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("presentation_title", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("presentation_title")]
        public string PresentationTitle { get; set; }

        [JsonProperty("respons", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("respons")]
        public List<Respon> Respons { get; set; }

        [JsonProperty("responsType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("responsType")]
        public string ResponsType { get; set; }
    }


}
