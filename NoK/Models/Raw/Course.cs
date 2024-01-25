using Newtonsoft.Json;

namespace NoK.Models.Raw
{
    public class RawCourse
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class Alternative
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("correct")]
            public int? Correct { get; set; }

            [JsonProperty("image_url")]
            public string ImageUrl { get; set; }

            [JsonProperty("created_at")]
            public DateTime? CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("imageURL")]
            public object ImageURL { get; set; }
        }

        public class Answer
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("assignmentId")]
            public int? AssignmentId { get; set; }

            [JsonProperty("unit")]
            public string Unit { get; set; }
        }

        public class Assignment
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("answerTypeId")]
            public int? AnswerTypeId { get; set; }

            [JsonProperty("questionTypeId")]
            public int? QuestionTypeId { get; set; }

            [JsonProperty("question")]
            public string Question { get; set; }

            [JsonProperty("image_url")]
            public string ImageUrl { get; set; }

            [JsonProperty("deleted_at")]
            public object DeletedAt { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("teacher_guide")]
            public string TeacherGuide { get; set; }

            [JsonProperty("question_video")]
            public string QuestionVideo { get; set; }

            [JsonProperty("full_video")]
            public string FullVideo { get; set; }

            [JsonProperty("question_image")]
            public string QuestionImage { get; set; }

            [JsonProperty("what_have_we_learned")]
            public string WhatHaveWeLearned { get; set; }

            [JsonProperty("released")]
            public int? Released { get; set; }

            [JsonProperty("content_hierarchies")]
            public List<ContentHierarchy> ContentHierarchies { get; set; }

            [JsonProperty("alternatives")]
            public List<Alternative> Alternatives { get; set; }

            [JsonProperty("skills")]
            public List<Skill> Skills { get; set; }

            [JsonProperty("hints")]
            public List<Hint> Hints { get; set; }

            [JsonProperty("answers")]
            public List<Answer> Answers { get; set; }
        }

        public class Chapter
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("hierarchyID")]
            public int? HierarchyID { get; set; }

            [JsonProperty("max_level")]
            public int? MaxLevel { get; set; }

            [JsonProperty("series")]
            public int? Series { get; set; }

            [JsonProperty("isHidden")]
            public bool? IsHidden { get; set; }

            [JsonProperty("user_progress")]
            public List<object> UserProgress { get; set; }

            [JsonProperty("sections")]
            public List<object> Sections { get; set; }

            [JsonProperty("parts")]
            public List<Part> Parts { get; set; }
        }

        public class Content
        {
            [JsonProperty("chapters")]
            public List<Chapter> Chapters { get; set; }
        }

        public class ContentHierarchy
        {
            [JsonProperty("hierarchyId")]
            public int? HierarchyId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("parent")]
            public int? Parent { get; set; }
        }

        public class Hint
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }

            [JsonProperty("video_url")]
            public object VideoUrl { get; set; }

            [JsonProperty("assignmentId")]
            public int? AssignmentId { get; set; }

            [JsonProperty("image_url")]
            public string ImageUrl { get; set; }
        }

        public class Lesson
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("body")]
            public string Body { get; set; }

            [JsonProperty("created_at")]
            public DateTime? CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("html")]
            public string Html { get; set; }
        }

        public class Part
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("hierarchyID")]
            public int? HierarchyID { get; set; }

            [JsonProperty("max_level")]
            public int? MaxLevel { get; set; }

            [JsonProperty("series")]
            public int? Series { get; set; }

            [JsonProperty("isHidden")]
            public bool? IsHidden { get; set; }

            [JsonProperty("user_progress")]
            public List<object> UserProgress { get; set; }

            [JsonProperty("sections")]
            public List<object> Sections { get; set; }

            [JsonProperty("subParts")]
            public List<SubPart> SubParts { get; set; }
        }

        public class Peertool
        {
            [JsonProperty("assignments")]
            public List<Assignment> Assignments { get; set; }

            [JsonProperty("favorites")]
            public List<object> Favorites { get; set; }
        }

        public class Pivot
        {
            [JsonProperty("hierarchy_id")]
            public int? HierarchyId { get; set; }

            [JsonProperty("section_id")]
            public int? SectionId { get; set; }
        }

        public class Root
        {
            [JsonProperty("content")]
            public Content Content { get; set; }

            [JsonProperty("favorites")]
            public List<object> Favorites { get; set; }

            [JsonProperty("readable_course")]
            public string ReadableCourse { get; set; }

            [JsonProperty("formulas_link")]
            public string FormulasLink { get; set; }

            [JsonProperty("product_info")]
            public string ProductInfo { get; set; }

            [JsonProperty("peertool")]
            public Peertool Peertool { get; set; }
        }

        public class Section
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("lesson_id")]
            public int? LessonId { get; set; }

            [JsonProperty("created_at")]
            public DateTime? CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            [JsonProperty("pivot")]
            public Pivot Pivot { get; set; }

            [JsonProperty("lesson")]
            public Lesson Lesson { get; set; }

            [JsonProperty("section_assignment_relations")]
            public List<SectionAssignmentRelation> SectionAssignmentRelations { get; set; }
        }

        public class SectionAssignmentRelation
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("section_id")]
            public int? SectionId { get; set; }

            [JsonProperty("assignment_id")]
            public int? AssignmentId { get; set; }

            [JsonProperty("assignment_name")]
            public int? AssignmentName { get; set; }

            [JsonProperty("level")]
            public int? Level { get; set; }

            [JsonProperty("position")]
            public int? Position { get; set; }
        }

        public class Skill
        {
            [JsonProperty("skill_id")]
            public int? SkillId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class SubPart
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("hierarchyID")]
            public int? HierarchyID { get; set; }

            [JsonProperty("max_level")]
            public int? MaxLevel { get; set; }

            [JsonProperty("series")]
            public int? Series { get; set; }

            [JsonProperty("isHidden")]
            public bool? IsHidden { get; set; }

            [JsonProperty("user_progress")]
            public List<UserProgress> UserProgress { get; set; }

            [JsonProperty("sections")]
            public List<Section> Sections { get; set; }
        }

        public class UserProgress
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("userID")]
            public int? UserID { get; set; }

            [JsonProperty("hierarchyID")]
            public int? HierarchyID { get; set; }

            [JsonProperty("progress")]
            public int? Progress { get; set; }

            [JsonProperty("level_progress")]
            public List<double?> LevelProgress { get; set; }

            [JsonProperty("created_at")]
            public DateTime? CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTime? UpdatedAt { get; set; }
        }


    }
}
