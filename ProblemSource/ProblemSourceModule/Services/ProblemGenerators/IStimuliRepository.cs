﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json;

namespace ProblemSourceModule.Services.ProblemGenerators
{
    public interface IProblemDomain
    {
        ISolutionChecker SolutionChecker { get; }
        IStimuliRepository StimuliRepository { get; }

        public static T DeserializeWithId<T>(object obj) where T: class, IStimulusId
        {
            var stim = obj switch
            {
                T t => t,
                string str => JsonConvert.DeserializeObject<T>(str),
                JObject jo => jo.ToObject<T>(),
                JsonElement je => JsonConvert.DeserializeObject<T>(je.ToString()), //je.Deserialize<T>(),
                _ => null
            };
            if (stim == null)
                throw new NotImplementedException(obj.GetType().Name);
            return stim;
        }
    }

    public interface IStimuliRepository
    {
        Task<IStimulus?> GetById(string id);
        Task<List<string>> GetAllIds();
        Task<List<IStimulus>> GetAll();
        IStimulus Deserialize(object obj);
    }

    public interface ISolutionChecker
    {
        Task<ISolutionAnalysis> Check(IStimulus stimulus, IUserResponse response);
        IUserResponse Deserialize(object obj);
    }

    public interface ISolutionAnalysis
    {
        bool IsCorrect { get; }
        string? Feedback { get; }
    }

    public interface IHintProvider
    {
    }

    public interface IStimulusId
    {
        string Id { get; }
        string SourceId { get; }
    }

    public interface IStimulus : IStimulusId
    {
        string Presentation { get; }
    }

    public interface IUserResponse : IStimulusId
    {
        string ResponseText { get; }
    }

    public class SimpleUserResponse : IUserResponse
    {
        public string ResponseText { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string SourceId { get; set; } = string.Empty;
    }

    public class SimpleSolutionAnalysis : ISolutionAnalysis
    {
        public bool IsCorrect { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
