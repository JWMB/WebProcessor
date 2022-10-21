using Shouldly;
using System.Diagnostics;
using Organization.Roles;
using Organization.Entities;
using Organization.Repositories;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace Organization.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void TestRepo()
        {
            var store = new DocumentStore();
            var rs = new RepositoryService(store);

            var schoolA = new School { Name = "school a" };
            
            rs.Schools.Add(schoolA);
            rs.Schools.Query().Count().ShouldBe(1);

            store.GetValues().Count().ShouldBe(1);

            var person = new Person();
            var @class = schoolA.Classes.Add(new ClassDef { Name = "A", Grade = 1 });
            var student = @class.Students.Add(new ClassStudentDef(person));

            schoolA.Classes.ToList().ForEach(o => rs.Classes.Add(o));
            rs.Classes.Query().Count().ShouldBe(1);

            store.GetValues().Count().ShouldBe(2);
            store.GetValues().GroupBy(o => o.Value.Type).Count().ShouldBe(2);

            // Removal:
            var schoolB = new School { Name = "school b" };
            rs.Schools.Add(schoolB);
            store.GetValues().Count().ShouldBe(3);
            rs.Schools.Remove(schoolB);
            store.GetValues().Count().ShouldBe(2);

            // Verify deserialization:
            var rs2 = new RepositoryService(store);
            rs2.Schools.Query().Count().ShouldBe(0);
            rs2.Init();
            rs2.Schools.Query().Count().ShouldBe(rs.Schools.Query().Count());
            rs2.Classes.Query().Count().ShouldBe(rs.Classes.Query().Count());

            rs2.Schools.Query().First().Classes.Count.ShouldBe(1);

            // Removal again:
            var toDelete = rs2.Classes.Query().First();
            store.Delete(toDelete.Id.ToString());
            rs2.Refresh(new[] { toDelete.Id });

            rs2.Classes.Query().Count().ShouldBe(0);
            //rs.Schools.Query().First().Classes.Count.ShouldBe(0);
        }

        [Fact]
        public void Test1()
        {
            //var x = 180 & (190 | "200");
            var classesPerSchool = 10;
            var studentsPerClass = 30;
            var numSchools = 1000;

            var schools = GetAndMeasure(() =>
    Enumerable.Range(0, numSchools).Select(o => CreateSchool($"School {o}", classesPerSchool, studentsPerClass)), out var msCreate).ToList();

            //schools.ForEach(o => schoolRepo.Add(o));
            //schools.SelectMany(o => o.Classes).ToList().ForEach(o => classRepo.Add(o));

            var teachers = GetAndMeasure(() =>
                schools.SelectMany(o => o.Teachers).Distinct(), out var msGetTeachers).ToList();

            var trainings = GetAndMeasure(() =>
                schools.SelectMany(o => o.Students).Distinct().SelectMany(o => o.Assignments), out var msGetTrainings).ToList();

            var teacherPersonA = teachers.First().Person;
            var teacherPersonAStudentTraining = teacherPersonA.Roles.OfType<ClassTeacher>().SelectMany(t => t.Class.Students).First().Assignments.Single();

            HasPersonReadAccess(teacherPersonA, teacherPersonAStudentTraining)
                .ShouldBe(true);
            HasPersonReadAccess(teacherPersonA, trainings.Last())
                .ShouldBe(false);
            teachers.Count
                .ShouldBe(schools.Count * classesPerSchool);
            trainings.Count
                .ShouldBe(schools.Count * classesPerSchool * studentsPerClass);

            bool HasPersonReadAccess(Person p, Assignment t)
            {
                return p.Roles.SelectMany(o => o.ConnectedRoles())
                    .Where(o => o.Qualifier >= RoleQualifier.Read)
                    .Select(o => o.Role)
                    .OfType<IHasOwnAssignments>()
                    .Any(o => o.AssignmentsCollection.Contains(t));
            }
        }

        [Fact]
        public void Test2()
        {
            var school = new School { Name = "skolan" };

            var @class = school.Classes.Add(new ClassDef { Name = "1A" });

            var teacherPerson = new Person();
            var teacherRole = @class.Teachers.Add(new ClassTeacherDef(teacherPerson));

            var studenPerson = new Person();
            var student = @class.Students.Add(new ClassStudentDef(studenPerson) { Assignments = new[] { new Assignment { Id = "abc" } } });

            var thoseClasses = teacherPerson.Roles.OfType<ClassTeacher>().Where(o => o.Class.School.Name == school.Name).Select(o => o.Class);//Single(o => o.School.Name == school.Name).Classes;
            var allTrainings = teacherPerson.Roles.SelectMany(o => o.ConnectedRoles())
                .Where(o => o.Qualifier >= RoleQualifier.Read)
                .Select(o => o.Role).OfType<IHasOwnAssignments>()
                .Select(o => o.AssignmentsCollection);
        }

        private IEnumerable<T> GetAndMeasure<T>(Func<IEnumerable<T>> func, out double elapsedMs)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = func().ToList();
            sw.Stop();
            elapsedMs = sw.ElapsedMilliseconds;
            return result;
        }

        private School CreateSchool(string name, int numClasses, int studentsPerClass)
        {
            var school = new School { Name = name };
            for (int i = 0; i < numClasses; i++)
            {
                var @class = school.Classes.Add(new ClassDef { Grade = 1, Name = $"School{school.Name} Class{i}"});

                var teacherPerson = new Person();
                var teacherRole = @class.Teachers.Add(new ClassTeacherDef(teacherPerson));

                for (int j = 0; j < studentsPerClass; j++)
                {
                    var studentPerson = new Person();

                    var student = @class.Students.Add(new ClassStudentDef(studentPerson));

                    student.Assignments.Add(new Assignment { Id = Guid.NewGuid().ToString() });
                }
            }
            return school;
        }

        [Fact]
        public void ASTTest()
        {
            var input = @"-1 && A && ""1"" && (2 || 3) && !4";
            input = "!1";
            input = "!(1 || 2)";
            var str = BooleanExpressionTree.ParseExperiment(input, new SqlGroupExpressionRenderer());
        }

        public class SqlGroupExpressionRenderer : BooleanExpressionTree.ExpressionRenderer
        {
            private readonly string baseQuery = "account_id {0} IN (SELECT account_id FROM accounts_groups WHERE";
            private string GroupNamePredicate(string groupName) => $"{baseQuery} group_id IN (SELECT id FROM groups WHERE name = '{groupName}')) )";

            public override string Render(LiteralExpressionSyntax exp)
            {
                if (exp.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                    return GroupNamePredicate(exp.ToString().Trim('\"'));
                return $"{baseQuery} group_id = {exp})";
            }

            public override string Render(IdentifierNameSyntax exp) => GroupNamePredicate($"{exp}");

            public override string Render(BinaryExpressionSyntax exp)
            {
                switch (exp.OperatorToken.ToString())
                {
                    case "&&":
                        return "AND";
                    case "||":
                        return "OR";
                    default:
                        throw new Exception($"Unhandled operator {exp.OperatorToken}");
                }
            }

            public override string Render(PrefixUnaryExpressionSyntax exp)
            {
                if (exp.OperatorToken.ToString() == "!")
                    return "NOT";
                return String.Empty;
                //TODO: throw new Exception($"Unhandled operator {exp.OperatorToken}");
            }
        }
    }
}